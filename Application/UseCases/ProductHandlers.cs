using Application.DTOs;
using Application.DTOs.Common;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.ValueObjects;
using Domain.Enums;

namespace Application.UseCases;

public class ProductHandlers
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductHandlers(
        IProductRepository productRepository, 
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<ProductResponseDto>> GetPagedProducts(int pageNumber, int pageSize, string? term, string searchBy)
    {
        var result = await _productRepository.GetPagedAsync(pageNumber, pageSize, term, searchBy);

        var itemsDto = result.Items.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock,
            IsActive = p.IsActive
        }).ToList();

        return new PagedResponse<ProductResponseDto>(itemsDto, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        // Validación de Duplicados
        var existingProduct = await _productRepository.GetByNameAsync(request.Name);
        if (existingProduct != null)
        {
            if (existingProduct.IsActive)
            {
                throw new ArgumentException($"Ya existe un producto activo con el nombre '{request.Name}'.");
            }
            else
            {
                throw new InvalidOperationException($"EXISTING_INACTIVE|{existingProduct.Id}");
            }
        }

        var product = new Product(
            request.Name,
            new Price(request.UnitPrice),
            request.Stock
        );

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _productRepository.AddAsync(product);

            // Registrar el stock inicial como el primer movimiento
            if (request.Stock > 0)
            {
                var movement = new InventoryMovement(
                    product.Id,
                    MovementType.InitialStock,
                    request.Stock,
                    0,
                    request.Stock,
                    "Carga inicial de inventario"
                );
                await _inventoryRepository.AddMovementAsync(movement);
            }
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return product;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task UpdateProductAsync(UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Producto no encontrado.");

        var productWithSameName = await _productRepository.GetByNameAsync(request.Name);
        if (productWithSameName != null && productWithSameName.Id != request.Id)
        {
             throw new ArgumentException($"El nombre '{request.Name}' ya está siendo usado por otro producto.");
        }

        product.UpdateInfo(
            request.Name,
            new Price(request.UnitPrice)
        );

        await _productRepository.UpdateAsync(product);
    }

    public async Task DeleteProductAsync(int id)
    {
        await _productRepository.DeleteAsync(id);
    }
}
