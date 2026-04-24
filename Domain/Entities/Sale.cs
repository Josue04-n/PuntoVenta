using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class SaleDetail
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public int Amount { get; private set; }
    public Price UnitPrice { get; private set; }
    public virtual Product Product { get; set; }
    [ForeignKey("ProductId")]
    public Price SubTotal => UnitPrice * Amount;

    private SaleDetail(){}

    public SaleDetail(int productId, int amount, Price unitPrice)
    {
        ProductId = productId;
        Amount = amount;
        UnitPrice = unitPrice;
    }

}

public class Sale 
{
    public int Id { get; private set; }
    public DateTime IssueDate { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; private set; }
    private readonly List<SaleDetail> _details = new();

    public IReadOnlyCollection<SaleDetail> Details => _details.AsReadOnly();

    private const decimal Vat = 0.15m;

    public decimal SubTotal => _details.Sum(d => d.SubTotal.Worth);
    public decimal VatAmount => SubTotal * Vat;
    public decimal Total => SubTotal + VatAmount;

    public Sale(string invoiceNumber, int customerId)
    {
        if (customerId<=0)
        {
            throw new ArgumentException("El Cliente es obligatorio para realizar la venta");
        }
        InvoiceNumber = invoiceNumber;
        CustomerId = customerId;
        var ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        IssueDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorTimeZone);
    }

    public void AddDetail(Product product, int amount)
    {
        product.DecreaseStock(amount);
        var detail = new SaleDetail(product.Id, amount, product.UnitPrice);
        _details.Add(detail);
    }

}
