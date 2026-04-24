namespace Blazor.Models;

public class PosModels
{
}

public class CustomerModel
{
    public int Id { get; set; }
    public string IDCard { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

}

public class ProductModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
}

public class CreateSaleRequest
{
    public int CustomerId { get; set; }
    public List<CreateSaleDetailRequest> Details { get; set; } = new();
}

public class CreateSaleDetailRequest
{
    public int ProductId { get; set; }
    public int Amount { get; set; }
}

public class SaleResponseDto
{
    public int Id { get; set; }
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
}

public class SaleDetailResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}