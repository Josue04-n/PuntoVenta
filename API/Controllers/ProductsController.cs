using Application.DTOs;
using Application.DTOs.Common;
using Application.Interfaces.Repositories;
using Application.UseCases;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productsRepository;
    private readonly ProductHandlers _productHandlers;

    public ProductsController(IProductRepository productsRepository, ProductHandlers productHandlers)
    {
        _productsRepository = productsRepository;
        _productHandlers = productHandlers;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] string searchBy = "name")
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { message = "Search term is mandatory." });

        var results = await _productsRepository.SearchAsync(term, searchBy);

        var response = results.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock
        });

        return Ok(response);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<ProductResponseDto>>> GetPaged(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? term = null,
    [FromQuery] string searchBy = "name")
    {
        var result = await _productsRepository.GetPagedAsync(pageNumber, pageSize, term, searchBy);

        // Map domain products to flat DTOs to avoid ValueObjects in response
        var itemsDto = result.Items.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock
        }).ToList();

        var pagedDto = new PagedResponse<ProductResponseDto>
            (itemsDto, 
            result.TotalCount, 
            result.PageNumber, 
            result.PageSize);

        return Ok(pagedDto);
    }

 

}