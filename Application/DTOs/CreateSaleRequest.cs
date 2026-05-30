namespace Application.DTOs;

public record CreateSaleRequest
{
    public int? DraftId { get; set; }
    public int CustomerId { get; set; }
    public List<CreateRequestDetail> Details { get; set; } = new();
}
public record CreateRequestDetail
{ 
        public int ProductId { get; set; }
        public int Amount { get; set; } 
}



