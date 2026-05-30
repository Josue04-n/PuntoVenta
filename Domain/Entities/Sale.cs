using Domain.Common;
using Domain.ValueObjects;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;
public class Sale : AuditableEntity
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
    public SaleStatus Status { get; private set; }

    private readonly List<SaleDetail> _details = new();
    public IReadOnlyCollection<SaleDetail> Details => _details.AsReadOnly();

    public Sale()
    {
        
    }

    public Sale(string invoiceNumber, int customerId, decimal vatRate = 15.00m)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("El Cliente es obligatorio para realizar la venta");
        }
        InvoiceNumber = invoiceNumber;
        CustomerId = customerId;
        VatPercentage = vatRate;
        Status = SaleStatus.Draft; // Inicia siempre en Borrador

        var ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        IssueDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorTimeZone);
    }

    public void AddDetail(Product product, int amount)
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("No se pueden agregar productos a una venta ya confirmada o anulada.");

        var detail = new SaleDetail(product.Id, amount, product.UnitPrice);
        _details.Add(detail);
        UpdateTotals();
    }

    public void ClearDetails()
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("Solo se pueden modificar productos de una venta en Borrador.");
        _details.Clear();
        UpdateTotals();
    }

    public void UpdateCustomer(int customerId)
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("Solo se pueden modificar el cliente de una venta en Borrador.");
        CustomerId = customerId;
    }

    public void UpdateTotals()
    {
        SubTotal = _details.Sum(d => d.SubTotal.Worth);
        VatAmount = Math.Round(SubTotal * (VatPercentage / 100m), 2, MidpointRounding.AwayFromZero);
        Total = SubTotal + VatAmount;
    }

    public void Confirm()
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("Solo se puede confirmar una venta que esté en estado Borrador.");
        
        if (!_details.Any())
            throw new InvalidOperationException("No se puede confirmar una venta sin productos.");

        Status = SaleStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("La venta ya está anulada.");

        Status = SaleStatus.Cancelled;
    }
}
