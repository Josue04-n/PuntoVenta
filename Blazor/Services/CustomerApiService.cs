using Application.DTOs;
using Application.DTOs.Common;
using Blazor.Models;
using System.Net.Http.Json;

namespace Blazor.Services;

public class CustomerApiService
{
    private readonly HttpClient _http;
    public CustomerApiService(HttpClient http) => _http = http;
    public async Task<PagedResponse<CustomerResponseDto>?> SearchCustomersAsync(
        string? term,
        string criterion,
        int page,
        int size = 10)
    {
        var escapedTerm = System.Uri.EscapeDataString(term ?? string.Empty);
        var url = $"api/Customer/search?term={escapedTerm}&criterion={criterion}&page={page}&size={size}";
        return await _http.GetFromJsonAsync<PagedResponse<CustomerResponseDto>>(url);
    }
}
