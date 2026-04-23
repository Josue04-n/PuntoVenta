using Application.DTOs;
using Application.UseCases;
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
    public async Task<ActionResult<SaleResponseDTO>> RegisterSale([FromBody] CreateSaleRequest request)
    {
        try
        {
            var result = await _performSaleUseCase.ExecuteAsync(request);
            return CreatedAtAction(nameof(RegisterSale), new { id = result.Id }, result);
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
            SubTotal = s.SubTotal,
            VatAmount = s.VatAmount,
            Total = s.Total
        });

        return Ok(response);

    }
}
