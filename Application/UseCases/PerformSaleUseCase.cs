using Application.DTOs;
using Application.Interfaces.Repositories;
using Domain.Entities;

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
        if (!request.Details.Any())
            throw new InvalidOperationException("La venta debe contener al menos un producto.");

        // Usamos el repositorio en lugar de AppDbContext
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new KeyNotFoundException("El cliente no existe.");

        string invoiceNumber = await _saleRepository.GenerateInvoiceNumberAsync();

        var sale = new Sale(invoiceNumber, request.CustomerId);

        decimal subTotalVenta = 0;
        var productsToUpdate = new List<Product>();

        foreach (var item in request.Details)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId)
                ?? throw new KeyNotFoundException($"El producto con id {item.ProductId} no existe.");

            if (product.Stock < item.Amount)
                throw new InvalidOperationException($"Stock insuficiente para {product.Name}. Solicitado: {item.Amount}. Disponible: {product.Stock}");

            sale.AddDetail(product, item.Amount);
            productsToUpdate.Add(product);
        }

        await _productRepository.UpdateRangeAsync(productsToUpdate);
        await _saleRepository.AddAsync(sale); 

        return sale;
    }
}