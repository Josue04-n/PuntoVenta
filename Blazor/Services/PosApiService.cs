using Application.DTOs;
using Application.DTOs.Common;
using Blazor.Models;
using System.Net.Http.Json;

namespace Blazor.Services;

public class PosApiService
{
    private readonly HttpClient _http;
    public PosApiService(HttpClient http)
    {
        _http = http;
    }
    // IMPLEMENTAR LOS MÉTODOS PARA CONSUMIR LA API

    // 3. CREAR VENTA

    public async Task<HttpResponseMessage> CreateSaleAsync(Application.DTOs.CreateSaleRequest request)
    {
        return await _http.PostAsJsonAsync("api/Sales", request);
    }

    // 4. BUSCAR VENTAS
    public async Task<List<Blazor.Models.SaleResponseDto>> SearchSalesAsync(string term, string searchBy = "number")
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<Blazor.Models.SaleResponseDto>>($"api/Sales/search?term={term}&searchBy={searchBy}");
            return response ?? new List<Blazor.Models.SaleResponseDto>();
        }
        catch
        {
            return new List<Blazor.Models.SaleResponseDto>();
        }
    }

    public async Task<PagedResponse<ProductResponseDto>?> GetPagedProductsAsync(int page, int size, string? term, string searchBy, string status = "active")
    {
        try
        {
            var url = $"api/Products/paged?pageNumber={page}&pageSize={size}&searchBy={searchBy}&status={status}";
            if (!string.IsNullOrWhiteSpace(term))
            {
                url += $"&term={Uri.EscapeDataString(term)}";
            }

            return await _http.GetFromJsonAsync<PagedResponse<ProductResponseDto>>(url);
        }
        catch { return null; }
    }

    public async Task<PagedResponse<Blazor.Models.SaleResponseDto>?> GetPagedSalesAsync(int pageNumber, int pageSize, string? term, string? searchBy)
    {
        try
        {
            var url = $"api/Sales/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(term))
            {
                url += $"&term={Uri.EscapeDataString(term)}";
            }
            if (!string.IsNullOrWhiteSpace(searchBy))
            {
                url += $"&searchBy={Uri.EscapeDataString(searchBy)}";
            }

            return await _http.GetFromJsonAsync<PagedResponse<Blazor.Models.SaleResponseDto>>(url);
        }
        catch
        {
            return null;
        }
    }

    // CRUD PARA PRODUCTOS
    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<ProductResponseDto>($"api/Products/{id}");
    }

    public async Task<HttpResponseMessage> CreateProductAsync(CreateProductRequest request)
    {
        return await _http.PostAsJsonAsync("api/Products", request);
    }

    public async Task<HttpResponseMessage> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        return await _http.PutAsJsonAsync($"api/Products/{id}", request);
    }

    public async Task<HttpResponseMessage> ReactivateProductAsync(int id)
    {
        return await _http.PutAsync($"api/Products/reactivate/{id}", null);
    }

    public async Task<HttpResponseMessage> DeleteProductAsync(int id)
    {
        return await _http.DeleteAsync($"api/Products/{id}");
    }
}
