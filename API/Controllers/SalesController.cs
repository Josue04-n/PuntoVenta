using Application.DTOs;
using Application.DTOs.Common;
using Application.UseCases;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly PerformSaleHandler _performSaleHandler;
    private readonly SearchSalesHandler _searchSalesHandler;

    public SalesController(PerformSaleHandler performSaleHandler, SearchSalesHandler searchSalesHandler)
    {
        _performSaleHandler = performSaleHandler;
        _searchSalesHandler = searchSalesHandler;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<SaleResponseDto>>> SearchSales([FromQuery] string term, [FromQuery] string searchBy = "number")
    {
        var sales = await _searchSalesHandler.ExecuteAsync(term, searchBy);

        var responseItems = sales.Select(s => new SaleResponseDto
        {
            Id = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            IssueDate = s.IssueDate,
            CustomerName = s.Customer != null ? $"{s.Customer.Name} {s.Customer.LastName}" : "Final Consumer",
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
                ProductName = d.Product?.Name ?? "Product not specified",
                Amount = d.Amount,
                UnitPrice = d.UnitPrice.Worth,
                SubTotal = d.SubTotal.Worth
            }).ToList()
        });

        return Ok(responseItems);
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponseDto>> RegisterSale([FromBody] CreateSaleRequest request)
    {
        // Fail Fast: Validation of input
        if (request == null || !request.Details.Any())
            return BadRequest("The sale request is empty or invalid.");

        try
        {
            var result = await _performSaleHandler.ExecuteAsync(request);

            // Manual mapping to ensure persisted totals are sent correctly
            var response = new SaleResponseDto
            {
                Id = result.Id,
                InvoiceNumber = result.InvoiceNumber,
                IssueDate = result.IssueDate,
                // Customer data for invoice (Eager loading assumed in Handler)
                CustomerName = result.Customer != null ? $"{result.Customer.Name} {result.Customer.LastName}" : "Final Consumer",
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
                    ProductName = d.Product?.Name ?? "Product not specified",
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
            return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<SaleResponseDto>>> GetPagedSales([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? term = null, [FromQuery] string? searchBy = null)
    {
        var (sales, totalCount) = await _searchSalesHandler.ExecutePagedAsync(pageNumber, pageSize, term, searchBy);

        var responseItems = sales.Select(s => new SaleResponseDto
        {
            Id = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            IssueDate = s.IssueDate,
            CustomerName = s.Customer != null ? $"{s.Customer.Name} {s.Customer.LastName}" : "Final Consumer",
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
                ProductName = d.Product?.Name ?? "Product not specified",
                Amount = d.Amount,
                UnitPrice = d.UnitPrice.Worth,
                SubTotal = d.SubTotal.Worth
            }).ToList()
        });

        return Ok(new PagedResponse<SaleResponseDto>(responseItems.ToList(), totalCount, pageNumber, pageSize));
    }
}