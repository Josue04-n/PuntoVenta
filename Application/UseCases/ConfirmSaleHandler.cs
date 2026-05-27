using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.UseCases;

public class ConfirmSaleHandler
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmSaleHandler(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int saleId)
    {
        var sale = await _saleRepository.GetByIdWithDetailsAsync(saleId)
            ?? throw new KeyNotFoundException("Venta no encontrada.");

        if (sale.Status != SaleStatus.Draft)
            throw new InvalidOperationException("Solo se pueden confirmar ventas en estado Borrador.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var stockErrors = new List<StockValidationError>();

            foreach (var detail in sale.Details)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId)
                    ?? throw new KeyNotFoundException($"Producto '{detail.ProductId}' no encontrado durante la confirmación.");

                if (product.Stock < detail.Amount)
                {
                    stockErrors.Add(new StockValidationError(product.Id, product.Name, detail.Amount, product.Stock));
                }
                else
                {
                    int previousStock = product.Stock;
                    
                    // 1. Descontar stock real
                    product.DecreaseStock(detail.Amount);

                    // 2. Registrar movimiento en Kárdex
                    var movement = new InventoryMovement(
                        product.Id,
                        MovementType.Sale,
                        detail.Amount,
                        previousStock,
                        product.Stock,
                        $"Confirmación Factura: {sale.InvoiceNumber}"
                    );
                    
                    await _inventoryRepository.AddMovementAsync(movement);
                }
            }

            if (stockErrors.Any()) throw new BulkStockException(stockErrors);

            // 3. Cambiar estado de la venta
            sale.Confirm();

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
