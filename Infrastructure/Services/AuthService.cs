using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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

        // 1. Verificar si ya está bloqueado (temporal o permanentemente)
        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var timeLeft = lockoutEnd.Value - DateTimeOffset.UtcNow;

            // Si el tiempo es muy largo (ej: > 365 días), es un bloqueo permanente
            if (timeLeft.TotalDays > 365)
            {
                 return new AuthResponseDto { IsSuccess = false, Message = "Tu cuenta ha sido bloqueada permanentemente por seguridad. Contacta al administrador." };
            }

            return new AuthResponseDto { IsSuccess = false, Message = $"Demasiados intentos. Intente de nuevo en {Math.Ceiling(timeLeft.TotalMinutes)} minutos." };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        var isAdmin = await _userManager.IsInRoleAsync(user, "Administrador");

        if (!isPasswordValid)
        {
            // Registrar el fallo en Identity
            await _userManager.AccessFailedAsync(user);
            var failedCount = await _userManager.GetAccessFailedCountAsync(user);

            if (isAdmin)
            {
                // --- POLÍTICA PARA ADMINISTRADOR (Progresiva) ---
                if (failedCount <= 3)
                {
                    return new AuthResponseDto { IsSuccess = false, Message = "Contraseña incorrecta." };
                }
                else if (failedCount == 4)
                {
                    // Retardo artificial de 10 segundos para mitigar ataques automatizados
                    await Task.Delay(10000);
                    return new AuthResponseDto { IsSuccess = false, Message = "Contraseña incorrecta. Reintento disponible en 10 segundos." };
                }
                else // 5 o más intentos
                {
                    // Bloqueo temporal de 15 minutos para evitar DoS (Denegación de Servicio)
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                    await _userManager.ResetAccessFailedCountAsync(user); // Reiniciar contador para el próximo ciclo
                    return new AuthResponseDto { IsSuccess = false, Message = "Seguridad: Cuenta suspendida por 15 minutos debido a actividad sospechosa." };
                }
            }
            else
            {
                // --- POLÍTICA PARA USUARIOS NORMALES (Vendedores) ---
                var remaining = 3 - failedCount;
                if (remaining > 0)
                {
                    return new AuthResponseDto { IsSuccess = false, Message = $"Contraseña incorrecta. Le quedan {remaining} intentos antes de bloquear su cuenta." };
                }
                else
                {
                    // Bloqueo permanente (100 años) requiere desbloqueo manual del Admin
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                    return new AuthResponseDto { IsSuccess = false, Message = "Ha superado el límite de intentos. Su cuenta ha sido bloqueada. Contacte al administrador." };
                }
            }
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
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Configuración de AzureAD incompleta en el servidor." };
            }

            // 1. Obtener llaves públicas de Microsoft para validar la firma
            var stsDiscoveryEndpoint = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
            var config = await configManager.GetConfigurationAsync();

            var handler = new JwtSecurityTokenHandler();

            // 2. Parámetros estrictos de validación
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",

                ValidateAudience = true,
                ValidAudience = clientId,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys // Firma real
            };

            // 3. Valida y extrae (Si la firma, audiencia o emisor no cuadran, esto lanza excepción)
            var principal = handler.ValidateToken(microsoftToken, validationParameters, out var validatedToken);

            // 4. Extracción segura de Claims validados
            var email = principal.FindFirst("email")?.Value
                     ?? principal.FindFirst(ClaimTypes.Email)?.Value
                     ?? principal.FindFirst("preferred_username")?.Value
                     ?? principal.FindFirst("upn")?.Value;

            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Token válido, pero no se encontró un email asociado. Revise los Scopes en Blazor." };
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var firstName = principal.FindFirst("given_name")?.Value
                             ?? principal.FindFirst(ClaimTypes.GivenName)?.Value
                             ?? "USUARIO";

                var lastName = principal.FindFirst("family_name")?.Value
                            ?? principal.FindFirst(ClaimTypes.Surname)?.Value
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

                // Contraseña temporal fuerte (10 caracteres, letras, números, símbolos)
                var passwordTemporal = Guid.NewGuid().ToString("N").Substring(0, 8) + "Ab1!@";

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
        catch (SecurityTokenExpiredException)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "El token de Microsoft ha expirado." };
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "La firma del token es inválida. Posible intento de suplantación." };
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Emisor del token inválido. El token no proviene del Tenant esperado." };
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Audiencia del token inválida. El token no fue emitido para esta aplicación." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Fallo en la autenticación externa: " + ex.Message };
        }
    }
}
