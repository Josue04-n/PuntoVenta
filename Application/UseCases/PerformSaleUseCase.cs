using Application.DTOs;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.UseCases;

public class PerformSaleUseCase
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;

    public PerformSaleUseCase(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
    }

    public async Task<Sale> ExecuteAsync(CreateSaleRequest request)
    {
        // 1. Validaciones iniciales
        if (!request.Details.Any())
            throw new InvalidOperationException("La venta debe contener al menos un producto.");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new KeyNotFoundException("El cliente no existe.");

        var errorsStock = new List<StockValidationError>();
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
                errorsStock.Add(new StockValidationError(product.Id, product.Name, item.Amount, product.Stock));
            }
            else
            {
                validatedProducts.Add((product, item.Amount));
            }
        }

        if (deletedProducts.Any())
        {
            throw new ProductDeletedException(deletedProducts);
        }

        if (errorsStock.Any())
        {
            throw new BulkStockException(errorsStock);
        }

        string invoiceNumber = await _saleRepository.GenerateInvoiceNumberAsync();
        var sale = new Sale(invoiceNumber, request.CustomerId);

        foreach (var (product, amount) in validatedProducts)
        {
            sale.AddDetail(product, amount);
            product.RemoveStock(amount);
        }

        await _productRepository.UpdateRangeAsync(validatedProducts.Select(p => p.product).ToList());
        await _saleRepository.AddAsync(sale);

        return sale;
    }
}