namespace Application.DTOs;

public class RestockRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Reference { get; set; } = string.Empty;
}
