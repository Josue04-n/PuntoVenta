namespace Application.DTOs;

public class SaleResponseDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerIDCard { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public List<SaleDetailResponseDto> Details { get; set; } = new();

    public class SaleDetailResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Amount { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }
}
