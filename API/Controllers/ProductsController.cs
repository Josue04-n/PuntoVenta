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
            Stock = p.Stock,
            IsActive = p.IsActive
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

        var itemsDto = result.Items.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock,
            IsActive = p.IsActive
        }).ToList();

        var pagedDto = new PagedResponse<ProductResponseDto>
            (itemsDto, 
            result.TotalCount, 
            result.PageNumber, 
            result.PageSize);

        return Ok(pagedDto);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id)
    {
        var product = await _productsRepository.GetByIdAsync(id);

        if (product == null) return NotFound(new { message = "Producto no encontrado" });

        return Ok(new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            UnitPrice = product.UnitPrice.Worth,
            Stock = product.Stock,
            IsActive = product.IsActive
        });
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> Create(CreateProductRequest request)
    {
        try
        {
            var product = await _productHandlers.CreateProductAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                UnitPrice = product.UnitPrice.Worth,
                Stock = product.Stock,
                IsActive = product.IsActive
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request)
    {
        if (id != request.Id) return BadRequest(new { message = "ID mismatch" });

        try
        {
            await _productHandlers.UpdateProductAsync(request);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _productHandlers.DeleteProductAsync(id);
        return NoContent();
    }
}
