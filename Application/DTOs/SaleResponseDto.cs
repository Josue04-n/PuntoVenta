using Domain.Enums;

namespace Application.DTOs;

public class SaleResponseDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerIDCard { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal VatPercentage { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public SaleStatus Status { get; set; }
    public string StatusName => Status switch
    {
        SaleStatus.Draft => "Borrador",
        SaleStatus.Confirmed => "Confirmada",
        SaleStatus.Cancelled => "Anulada",
        _ => "Desconocido"
    };
    public List<SaleDetailResponseDto> Details { get; set; } = new();
}

public class SaleDetailDto
{
    public int ProductId { get; set; }
    public int Amount { get; set; }
    public decimal UnitPrice { get; set; }
}
