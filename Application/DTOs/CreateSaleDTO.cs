namespace Application.DTOs;

public record CreateSaleDetailDto (int ProductId, int Amount);
public record CreateSaleDto(int CustomerId, List<CreateSaleDetailDto> Details);
public record SaleHeaderResponseDto (string InvoiceNumber, decimal SubTotal, decimal VatAmount, decimal Total);

