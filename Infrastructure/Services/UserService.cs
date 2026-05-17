using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
    {
        // Solo usuarios activos (respetando el Global Query Filter y filtro explícito por seguridad)
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        var userDtos = new List<UserResponseDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(MapToDto(user, roles.FirstOrDefault() ?? "Sin Rol"));
        }

        return userDtos;
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles.FirstOrDefault() ?? "Sin Rol");
    }

    public async Task<UserResponseDto?> GetUserByUserNameAsync(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles.FirstOrDefault() ?? "Sin Rol");
    }

    private UserResponseDto MapToDto(ApplicationUser user, string role)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IDCard = user.IDCard,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? "",
            Address = user.Address,
            Role = role,
            IsActive = user.IsActive,
            LastLogin = user.LastLogin
        };
    }

    public async Task<(bool IsSuccess, string[] Errors)> CreateUserAsync(RegisterUserRequest request)
    {
        // 1. Verificar si existe un usuario inactivo con ese UserName o Email
        var existingUser = await _userManager.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == request.UserName.ToUpper() || u.NormalizedEmail == request.Email.ToUpper());

        if (existingUser != null)
        {
            if (!existingUser.IsActive)
            {
                // Retornamos un error especial que el controlador interpretará
                return (false, new[] { $"INACTIVE_USER_EXISTS|{existingUser.Id}" });
            }
            return (false, new[] { "Ya existe un usuario activo con el mismo nombre de usuario o correo electrónico." });
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IDCard = request.IDCard,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber
        };

        user.Activate();

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(request.Role))
            {
                if (!await _roleManager.RoleExistsAsync(request.Role))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(request.Role));
                }
                await _userManager.AddToRoleAsync(user, request.Role);
            }
            return (true, Array.Empty<string>());
        }

        return (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool IsSuccess, string[] Errors)> UpdateUserAsync(UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null) return (false, new[] { "Usuario no encontrado." });

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IDCard = request.IDCard;
        user.Address = request.Address;
        user.Email = request.Email;
        user.UserName = request.UserName;
        user.PhoneNumber = request.PhoneNumber;
        
        if (request.IsActive) user.Activate();
        else user.Deactivate("Admin");

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(request.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!await _roleManager.RoleExistsAsync(request.Role))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(request.Role));
                }
                await _userManager.AddToRoleAsync(user, request.Role);
            }

            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (!passResult.Succeeded)
                {
                    return (false, passResult.Errors.Select(e => e.Description).ToArray());
                }
            }

            return (true, Array.Empty<string>());
        }

        return (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool IsSuccess, string[] Errors)> ReactivateUserAsync(int userId, UpdateUserRequest request)
    {
        request.Id = userId;
        request.IsActive = true; // Forzamos activación
        return await UpdateUserAsync(request);
    }

    public async Task<(bool IsSuccess, string[] Errors)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, new[] { "Usuario no encontrado." });

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            return (true, Array.Empty<string>());
        }

        return (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        user.Deactivate("System");
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<IEnumerable<string>> GetRolesAsync()
    {
        return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
    }
}
