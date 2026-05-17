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
}
