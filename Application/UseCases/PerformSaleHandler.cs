using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.UseCases;

public class PerformSaleHandler
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PerformSaleHandler(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Sale> ExecuteAsync(CreateSaleRequest request)
    {
        // 1. Validaciones iniciales
        if (!request.Details.Any())
            throw new InvalidOperationException("La venta debe contener al menos un producto.");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new KeyNotFoundException("El cliente no existe.");

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

            validatedProducts.Add((product, item.Amount));
        }

        if (deletedProducts.Any()) throw new ProductDeletedException(deletedProducts);

        // 2. Guardar el Borrador (Draft)
        // NOTA: No iniciamos transacción aquí porque solo estamos insertando una entidad.
        // El stock NO se descuenta en este paso.
        
        string invoiceNumber = await _saleRepository.GenerateInvoiceNumberAsync();
        var sale = new Sale(invoiceNumber, request.CustomerId);

        foreach (var (product, amount) in validatedProducts)
        {
            sale.AddDetail(product, amount);
        }

        await _saleRepository.AddAsync(sale);
        await _unitOfWork.SaveChangesAsync();

        return sale;
    }
}
