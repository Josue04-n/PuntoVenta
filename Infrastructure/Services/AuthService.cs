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
                return new AuthResponseDto { IsSuccess = false, Message = "Token de Microsoft inválido o mal formado." };
            }

            var jwtToken = handler.ReadJwtToken(microsoftToken);

            // Priorizar claims que contengan realmente el correo (evitando nombres completos que causan error de formato)
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                     ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                     ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                     ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "upn")?.Value
                     ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value
                     ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                // Si aún no tenemos un email válido, buscamos en cualquier claim que tenga un formato de email
                email = jwtToken.Claims.FirstOrDefault(c => c.Value.Contains("@"))?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    return new AuthResponseDto { IsSuccess = false, Message = "No se pudo obtener el email válido del token. Revise los Scopes en Blazor." };
                }
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var nameParts = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value?.Split(' ') ?? new[] { "Usuario", "Microsoft" };

                var firstName = jwtToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value
                             ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value
                             ?? "USUARIO";

                var lastName = jwtToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value
                            ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?.Value
                            ?? "MICROSOFT";

                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName.ToUpper(), 
                    LastName = lastName.ToUpper(),   
                    IsActive = true,
                    MustChangePassword = false
                };

                // Generamos una contraseña de entre 8 y 10 caracteres que cumpla las restricciones de Identity
                var passwordTemporal = Guid.NewGuid().ToString("N").Substring(0, 4) + "Ab1!@";

                var createResult = await _userManager.CreateAsync(user, passwordTemporal);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return new AuthResponseDto { IsSuccess = false, Message = $"No se pudo crear la cuenta local vinculada: {errors}" };
                }

                await _userManager.AddToRoleAsync(user, "Vendedor");
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Su cuenta local se encuentra desactivada." };
            }

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
