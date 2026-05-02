using Application.DTOs;
using Application.UseCases;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly PerformSaleUseCase _performSaleUseCase;
    private readonly SearchSalesUseCase _searchSalesUseCase;

    public SalesController(PerformSaleUseCase performSaleUseCase, SearchSalesUseCase searchSalesUseCase)
    {
        _performSaleUseCase = performSaleUseCase;
        _searchSalesUseCase = searchSalesUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponseDto>> RegisterSale([FromBody] CreateSaleRequest request)
    {
        // Fail Fast: Validación de entrada
        if (request == null || !request.Details.Any())
            return BadRequest("La solicitud de venta está vacía o es inválida.");

        try
        {
            var result = await _performSaleUseCase.ExecuteAsync(request);

            // Mapeo manual para asegurar que los totales persistidos se envíen correctamente
            var response = new SaleResponseDto
            {
                Id = result.Id,
                InvoiceNumber = result.InvoiceNumber,
                IssueDate = result.IssueDate,
                // Datos del cliente para la factura (Eager loading asumido en Use Case)
                CustomerName = result.Customer != null ? $"{result.Customer.Name} {result.Customer.LastName}" : "Consumidor Final",
                CustomerIDCard = result.Customer?.IDCard ?? "9999999999",
                CustomerPhone = result.Customer?.Phone ?? string.Empty,
                CustomerAddress = result.Customer?.Address ?? string.Empty,
                CustomerEmail = result.Customer?.Email ?? string.Empty,
                SubTotal = result.SubTotal,
                VatAmount = result.VatAmount,
                VatPercentage = result.VatPercentage,
                Total = result.Total,
                Details = result.Details.Select(d => new SaleDetailResponseDto
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? "Producto no especificado",
                    Amount = d.Amount,
                    UnitPrice = d.UnitPrice.Worth,
                    SubTotal = d.SubTotal.Worth
                }).ToList()
            };

            return CreatedAtAction(nameof(RegisterSale), new { id = response.Id }, response);
        }
        catch (BulkStockException ex)
        {
            return Conflict(new { type = "BulkStock", message = ex.Message, errors = ex.Errors });
        }
        catch (ProductDeletedException ex)
        {
            return BadRequest(new { type = "ProductDeleted", message = ex.Message, deletedProducts = ex.DeletedProducts });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ocurrió un error inesperado.", details = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] string searchBy = "numero")
    {
        var sales = await _searchSalesUseCase.ExecuteAsync(term, searchBy);

        // Mapeo masivo optimizado para búsquedas históricas
        var response = sales.Select(s => new SaleResponseDto
        {
            Id = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            IssueDate = s.IssueDate,
            CustomerName = s.Customer != null ? $"{s.Customer.Name} {s.Customer.LastName}" : "Consumidor Final",
            CustomerIDCard = s.Customer?.IDCard ?? "9999999999",
            CustomerPhone = s.Customer?.Phone ?? string.Empty,
            CustomerAddress = s.Customer?.Address ?? string.Empty,
            CustomerEmail = s.Customer?.Email ?? string.Empty,
            SubTotal = s.SubTotal,
            VatAmount = s.VatAmount,
            VatPercentage = s.VatPercentage,
            Total = s.Total,
            Details = s.Details.Select(d => new SaleDetailResponseDto
            {
                ProductId = d.ProductId,
                ProductName = d.Product?.Name ?? "Producto no especificado",
                Amount = d.Amount,
                UnitPrice = d.UnitPrice.Worth,
                SubTotal = d.SubTotal.Worth
            }).ToList()
        });

        return Ok(response);
    }
}