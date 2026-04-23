namespace Application.DTOs;

public class CreateSaleRequest
{
    public int CustomerId { get; set; }
    public List<CreateRequestDetail> Details { get; set; } = new();

    public class CreateRequestDetail()
    { 
        public int ProductId { get; set; }
        public int Amount { get; set; } 
    }
}
