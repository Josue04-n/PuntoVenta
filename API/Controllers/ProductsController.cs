using Application.DTOs;
using Application.DTOs.Common;
using Application.Interfaces.Repositories;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productsRepository;

    public ProductsController(IProductRepository productsRepository)
    {
        _productsRepository = productsRepository;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] string searchBy = "name")
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { mensaje = "El término de búsqueda es obligatorio." });

        var results = await _productsRepository.SearchAsync(term, searchBy);

        // Mapeo al DTO para que Blazor lo pueda leer perfectamente
        var response = results.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock
        });

        // ¡CORRECCIÓN AQUÍ! Devolvemos 'response', no 'results'
        return Ok(response);
    }

    [HttpGet("paginado")]
    public async Task<ActionResult<PagedResponse<ProductResponseDto>>> GetPaginado(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? term = null)
    {
        var resultado = await _productsRepository.ListarPaginadoAsync(pageNumber, pageSize, term);

        // Mapear los productos del dominio a DTOs planos para evitar objetos ValueObjects en la respuesta
        var itemsDto = resultado.Items.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock
        }).ToList();

        var pagedDto = new PagedResponse<ProductResponseDto>(itemsDto, resultado.TotalCount, resultado.PageNumber, resultado.PageSize);

        return Ok(pagedDto);
    }


    }