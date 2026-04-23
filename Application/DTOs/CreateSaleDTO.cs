namespace Application.DTOs;

public record CreateSaleDetailDTO (int ProductId, int Amount);
public record CreateSaleDTO(int ClientId, List<CreateSaleDetailDTO> details);
public record SaleResponseDTO (string InvoiceNumber, decimal SubTotal, decimal VatAmount, decimal Total);

