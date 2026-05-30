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

        // 2. Guardar el Borrador (Draft) o Actualizar existente
        Sale sale;
        if (request.DraftId.HasValue && request.DraftId.Value > 0)
        {
            sale = await _saleRepository.GetByIdWithDetailsAsync(request.DraftId.Value);
            if (sale == null || sale.Status != Domain.Enums.SaleStatus.Draft)
                throw new InvalidOperationException("El borrador no existe o ya no está en estado Borrador.");

            sale.UpdateCustomer(request.CustomerId);
            sale.ClearDetails();
        }
        else
        {
            string invoiceNumber = await _saleRepository.GenerateInvoiceNumberAsync();
            sale = new Sale(invoiceNumber, request.CustomerId);
            await _saleRepository.AddAsync(sale);
        }

        foreach (var (product, amount) in validatedProducts)
        {
            sale.AddDetail(product, amount);
        }

        await _unitOfWork.SaveChangesAsync();

        return sale;
    }
}
