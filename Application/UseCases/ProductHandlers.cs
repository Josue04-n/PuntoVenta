using Application.DTOs;
using Application.DTOs.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.UseCases;

public class ProductHandlers
{
    private readonly IProductRepository _productRepository;

    public ProductHandlers(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PagedResponse<ProductResponseDto>> GetPagedProducts(int pageNumber, int pageSize, string? term, string searchBy)
    {
        var result = await _productRepository.GetPagedAsync(pageNumber, pageSize, term, searchBy);

        var itemsDto = result.Items.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock
        }).ToList();

        return new PagedResponse<ProductResponseDto>(itemsDto, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product(
            request.Name,
            new Price(request.UnitPrice),
            request.Stock
        );

        await _productRepository.AddAsync(product);
        return product;
    }

    public async Task UpdateProductAsync(UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Producto no encontrado.");

        product.Update(
            request.Name,
            new Price(request.UnitPrice),
            request.Stock
        );

        await _productRepository.UpdateAsync(product);
    }

    public async Task DeleteProductAsync(int id)
    {
        await _productRepository.DeleteAsync(id);
    }
}
