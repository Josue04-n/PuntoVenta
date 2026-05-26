using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequest request);
    Task<AuthResponseDto> MicrosoftLoginAsync(string microsoftToken);
    Task<AuthResponseDto> RefreshTokenAsync(TokenRequestDto request);
}
