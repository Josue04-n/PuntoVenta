using Application.DTOs;
using Application.DTOs.Common;
using Application.Interfaces.Repositories;

namespace Application.UseCases;

public class ProductoUseCases
{
        private readonly IProductRepository _productRepository;
    public async Task<PagedResponse<ProductResponseDto>> GetPages(int pageNumber, int pageSize, string? term, string searchBy)
    {
        var resultado = await _productRepository.ListarPaginadoAsync(pageNumber, pageSize, term, searchBy);

        var itemsDto = resultado.Items.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth, // Conversión de ValueObject a tipo primitivo para el DTO
            Stock = p.Stock
        }).ToList();

        return new PagedResponse<ProductResponseDto>(itemsDto, resultado.TotalCount, resultado.PageNumber, resultado.PageSize);
    }
}
