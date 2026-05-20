using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.UseCases;

public class PerformSaleHandler
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PerformSaleHandler(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Sale> ExecuteAsync(CreateSaleRequest request)
    {
        // 1. Validaciones iniciales
        if (!request.Details.Any())
            throw new InvalidOperationException("La venta debe contener al menos un producto.");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new KeyNotFoundException("El cliente no existe.");

        var stockErrors = new List<StockValidationError>();
        var deletedProducts = new List<DeletedProductInfo>(); 
        var validatedProducts = new List<(Product product, int amount)>();

        foreach (var item in request.Details)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);

            if (product == null)
            {
                deletedProducts.Add(new DeletedProductInfo(item.ProductId, "Producto no encontrado"));
                continue;
            }

            if (product.Stock < item.Amount)
            {
                stockErrors.Add(new StockValidationError(product.Id, product.Name, item.Amount, product.Stock));
            }
            else
            {
                validatedProducts.Add((product, item.Amount));
            }
        }

        if (deletedProducts.Any()) throw new ProductDeletedException(deletedProducts);
        if (stockErrors.Any()) throw new BulkStockException(stockErrors);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            string invoiceNumber = await _saleRepository.GenerateInvoiceNumberAsync();
            var sale = new Sale(invoiceNumber, request.CustomerId);

            foreach (var (product, amount) in validatedProducts)
            {
                int previousStock = product.Stock;
                
                // 2. Descontar stock (lógica de dominio)
                sale.AddDetail(product, amount);

                // 3. Registrar movimiento en Kárdex
                var movement = new InventoryMovement(
                    product.Id,
                    MovementType.Sale,
                    amount,
                    previousStock,
                    product.Stock,
                    $"Factura: {invoiceNumber}"
                );
                
                await _inventoryRepository.AddMovementAsync(movement);
            }

            // 4. Guardar todo en una sola transacción
            await _saleRepository.AddAsync(sale);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return sale;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
