using Application.DTOs;
using System.Net.Http.Json;

namespace Blazor.Services;

public class UserApiService
{
    private readonly HttpClient _http;

    public UserApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        try {
            return await _http.GetFromJsonAsync<List<UserResponseDto>>("api/Users") ?? new();
        } catch { return new(); }
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<UserResponseDto>($"api/Users/{id}");
    }

    public async Task<HttpResponseMessage> CreateAsync(RegisterUserRequest request)
    {
        return await _http.PostAsJsonAsync("api/Users", request);
    }

    public async Task<HttpResponseMessage> UpdateAsync(int id, UpdateUserRequest request)
    {
        return await _http.PutAsJsonAsync($"api/Users/{id}", request);
    }

    public async Task<HttpResponseMessage> ReactivateAsync(int id, UpdateUserRequest request)
    {
        return await _http.PutAsJsonAsync($"api/Users/reactivate/{id}", request);
    }

    public async Task<HttpResponseMessage> DeleteAsync(int id)
    {
        return await _http.DeleteAsync($"api/Users/{id}");
    }

    public async Task<List<string>> GetRolesAsync()
    {
        try {
            return await _http.GetFromJsonAsync<List<string>>("api/Users/roles") ?? new();
        } catch { return new(); }
    }

    public async Task<UserResponseDto?> GetProfileAsync()
    {
        return await _http.GetFromJsonAsync<UserResponseDto>("api/Users/profile");
    }

    public async Task<HttpResponseMessage> UpdateProfileAsync(UpdateUserRequest request)
    {
        return await _http.PutAsJsonAsync("api/Users/profile", request);
    }

    public async Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordRequest request)
    {
        return await _http.PutAsJsonAsync("api/Users/change-password", request);
    }
}
