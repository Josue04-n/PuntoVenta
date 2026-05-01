using Application.DTOs;
using Application.UseCases;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using static Application.DTOs.SaleResponseDto;

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
    public async Task<ActionResult<SaleResponseDTO>> RegisterSale([FromBody] CreateSaleRequest request)
    {
        try
        {
            var result = await _performSaleUseCase.ExecuteAsync(request);
            return CreatedAtAction(nameof(RegisterSale), new { id = result.Id }, result);
        }
        catch (BulkStockException ex)
        {
            return BadRequest(new { type = "BulkStock", message = ex.Message, errors = ex.Errors });
        }
        catch (ProductDeletedException ex)
        {
            return BadRequest(new { type = "ProductDeleted", message = ex.Message, deletedProducts = ex.DeletedProducts });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }


    }
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] string searchBy = "numero")
    {
        var sales = await _searchSalesUseCase.ExecuteAsync(term, searchBy);
        var response = sales.Select(s => new SaleResponseDto
        {
            InvoiceNumber = s.InvoiceNumber,
            IssueDate = s.IssueDate.ToString("yyyy-MM-dd HH:mm:ss"),
            CustomerName = s.Customer != null ? $"{s.Customer.Name} {s.Customer.LastName}" : "Consumidor Final",
            CustomerIDCard = s.Customer != null ? s.Customer.IDCard : string.Empty,
            CustomerPhone = s.Customer != null ? s.Customer.Phone : string.Empty,
            CustomerAddress = s.Customer != null ? s.Customer.Address : string.Empty,
            CustomerEmail = s.Customer != null ? s.Customer.Email : string.Empty,
            SubTotal = s.SubTotal,
            VatAmount = s.VatAmount,
            Total = s.Total,
            Details = s.Details.Select(d => new SaleDetailResponseDto
            {
                ProductId = d.ProductId,
                ProductName = d.Product.Name,
                Amount = d.Amount,
                UnitPrice = d.UnitPrice.Worth, 
                SubTotal = d.SubTotal
            }).ToList()
        });

        return Ok(response);

    }
}
