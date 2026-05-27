using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.UseCases;

public class CancelSaleHandler
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSaleHandler(
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

        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("La venta ya se encuentra anulada.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Solo devolvemos stock si la venta ya estaba confirmada
            if (sale.Status == SaleStatus.Confirmed)
            {
                foreach (var detail in sale.Details)
                {
                    var product = await _productRepository.GetByIdAsync(detail.ProductId)
                        ?? throw new KeyNotFoundException("Producto no encontrado durante la anulación.");

                    int previousStock = product.Stock;
                    
                    // 1. Devolver stock
                    product.IncreaseStock(detail.Amount);

                    // 2. Registrar movimiento de Devolución en Kárdex
                    var movement = new InventoryMovement(
                        product.Id,
                        MovementType.Return,
                        detail.Amount,
                        previousStock,
                        product.Stock,
                        $"Anulación Factura: {sale.InvoiceNumber}"
                    );
                    
                    await _inventoryRepository.AddMovementAsync(movement);
                }
            }

            // 3. Cambiar estado a Anulada
            sale.Cancel();

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
