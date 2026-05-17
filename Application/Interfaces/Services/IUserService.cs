using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
    Task<UserResponseDto?> GetUserByIdAsync(int id);
    Task<UserResponseDto?> GetUserByUserNameAsync(string userName);
    Task<(bool IsSuccess, string[] Errors)> CreateUserAsync(RegisterUserRequest request);
    Task<(bool IsSuccess, string[] Errors)> UpdateUserAsync(UpdateUserRequest request);
    Task<(bool IsSuccess, string[] Errors)> ReactivateUserAsync(int userId, UpdateUserRequest request);
    Task<bool> UnlockUserAsync(int userId);
    Task<(bool IsSuccess, string[] Errors)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> DeleteUserAsync(int id);
    Task<IEnumerable<string>> GetRolesAsync();
}
