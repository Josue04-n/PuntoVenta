using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;
public class Sale 
{
    public int Id { get; private set; }
    public DateTime IssueDate { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; private set; }

    public decimal SubTotal { get; private set; }
    public decimal VatPercentage { get; private set; } 
    public decimal VatAmount { get; private set; }
    public decimal Total { get; private set; }

    private readonly List<SaleDetail> _details = new();
    public IReadOnlyCollection<SaleDetail> Details => _details.AsReadOnly();

    private const decimal Vat = 0.15m;

    public Sale()
    {
        
    }

    public Sale(string invoiceNumber, int customerId, decimal vatRate = 15.00m)
    {
        if (customerId<=0)
        {
            throw new ArgumentException("El Cliente es obligatorio para realizar la venta");
        }
        InvoiceNumber = invoiceNumber;
        CustomerId = customerId;
        VatPercentage = vatRate;

        var ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        IssueDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorTimeZone);
    }

    public void AddDetail(Product product, int amount)
    {
        product.DecreaseStock(amount);
        var detail = new SaleDetail(product.Id, amount, product.UnitPrice);
        _details.Add(detail);
        UpdateTotals();
    }

    public void UpdateTotals()
    {
        SubTotal = _details.Sum(d => d.SubTotal.Worth);
        VatAmount = Math.Round(SubTotal * (VatPercentage / 100m), 2, MidpointRounding.AwayFromZero);
        Total = SubTotal + VatAmount;
    }

}
