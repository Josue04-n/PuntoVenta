using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user == null || !user.IsActive)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Usuario no encontrado o inactivo." };
        }

        // 1. Verificar si la cuenta está bloqueada antes de intentar nada
        if (await _userManager.IsLockedOutAsync(user))
        {
            return new AuthResponseDto 
            { 
                IsSuccess = false, 
                Message = "Tu cuenta ha sido bloqueada por seguridad. Contacta al administrador." 
            };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            // 2. Incrementar contador de fallos si no es la cuenta de admin
            if (user.UserName?.ToLower() != "admin")
            {
                await _userManager.AccessFailedAsync(user);
                
                var failedCount = await _userManager.GetAccessFailedCountAsync(user);
                var remaining = 3 - failedCount;
                
                if (remaining > 0)
                {
                    return new AuthResponseDto { IsSuccess = false, Message = $"Contraseña incorrecta. Le quedan {remaining} intentos antes de bloquear su cuenta." };
                }
                else
                {
                    return new AuthResponseDto { IsSuccess = false, Message = "Ha superado el límite de intentos. Su cuenta ha sido bloqueada." };
                }
            }
            
            return new AuthResponseDto { IsSuccess = false, Message = "Contraseña incorrecta." };
        }

        // 3. Login exitoso -> Resetear contador de fallos
        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);

        // Actualizar última conexión
        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Token = token,
            UserName = user.UserName,
            Role = roles.FirstOrDefault(),
            Expiration = DateTime.UtcNow.AddHours(8)
        };
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
            new Claim("MustChangePassword", user.MustChangePassword.ToString().ToLower())
        };

        foreach (var role in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            expires: DateTime.UtcNow.AddHours(8),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto> MicrosoftLoginAsync(string microsoftToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            if (!handler.CanReadToken(microsoftToken))
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Token de Microsoft inválido." };
            }

            var jwtToken = handler.ReadJwtToken(microsoftToken);

            // 1. Extraer Email (Identity Institucional)
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value 
                     ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value 
                     ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return new AuthResponseDto { IsSuccess = false, Message = "No se pudo obtener el email del token de Microsoft." };
            }

            // 2. Buscar usuario en base de datos local
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // AUTOPROVISIÓN: Si no existe, lo creamos automáticamente como Vendedor
                var nameParts = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value?.Split(' ') ?? new[] { "Usuario", "Microsoft" };
                
                user = new ApplicationUser
                {
                    UserName = email.Split('@')[0].ToUpper(), // El nombre de usuario será la primera parte del correo
                    Email = email,
                    FirstName = nameParts[0].ToUpper(),
                    LastName = (nameParts.Length > 1 ? nameParts[1] : "MS").ToUpper(),
                    IsActive = true,
                    MustChangePassword = false // Al ser de Microsoft, no manejamos nosotros su clave
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new AuthResponseDto { IsSuccess = false, Message = "No se pudo crear la cuenta local vinculada." };
                }

                // Asignar el rol de Vendedor por defecto
                await _userManager.AddToRoleAsync(user, "Vendedor");
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Su cuenta local se encuentra desactivada." };
            }

            // 3. Intercambio de Token: Emitimos nuestro propio JWT con nuestros Roles y Lógica
            var roles = await _userManager.GetRolesAsync(user);
            var ourToken = GenerateJwtToken(user, roles);

            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Token = ourToken,
                UserName = user.UserName,
                Role = roles.FirstOrDefault(),
                Expiration = DateTime.UtcNow.AddHours(8)
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Fallo en la autenticación externa: " + ex.Message };
        }
    }
}
