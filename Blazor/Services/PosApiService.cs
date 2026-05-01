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

    // 1. OBTENER CLIENTES
    public async Task<List<CustomerModel>> SearchCustomersAsync(string term, string searchBy = "cedula")
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<CustomerModel>>($"api/Customer/search?term={term}&searchBy={searchBy}");
            return response ?? new List<CustomerModel>();
        }
        catch
        {
            return new List<CustomerModel>();
        }
    }

    // 2. OBTENER PRODUCTOS
    //public async Task<List<ProductModel>> SearchProductsAsync(string term, string searchBy = "name")
    //{
    //    try
    //    {
    //        var response = await _http.GetFromJsonAsync<List<ProductModel>>($"api/Products/search?term={term}&searchBy={searchBy}");
    //        return response ?? new List<ProductModel>();
    //    }
    //    catch
    //    {
    //        return new List<ProductModel>();


    //    }
    //}

    // 3. CREAR VENTA

    public async Task<HttpResponseMessage> CreateSaleAsync(CreateSaleRequest request)
    {
        return await _http.PostAsJsonAsync("api/Sales", request);
    }

    // 4. BUSCAR VENTAS
    public async Task<List<SaleResponseDto>> SearchSalesAsync(string term, string searchBy = "numero")
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<SaleResponseDto>>($"api/Sales/search?term={term}&searchBy={searchBy}");
            return response ?? new List<SaleResponseDto>();
        }
        catch
        {
            return new List<SaleResponseDto>();
        }
    }

    public async Task<PagedResponse<ProductModel>?> GetProductosPaginadosAsync(int page, int size, string? term, string searchBy)
    {
        try
        {
            var url = $"api/Products/paginado?pageNumber={page}&pageSize={size}&searchBy={searchBy}";
            if (!string.IsNullOrWhiteSpace(term))
            {
                url += $"&term={Uri.EscapeDataString(term)}";
            }

            return await _http.GetFromJsonAsync<PagedResponse<ProductModel>>(url);
        }
        catch { return null; }
    }

}
