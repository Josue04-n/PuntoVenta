namespace Application.Features.Users;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequest request);
    Task<AuthResponseDto> MicrosoftLoginAsync(string microsoftToken);
    Task<AuthResponseDto> RefreshTokenAsync(TokenRequestDto request);
}
