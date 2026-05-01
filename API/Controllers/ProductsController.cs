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
    private readonly ProductoUseCases _productoUseCases;

    public ProductsController(IProductRepository productsRepository, ProductoUseCases productoUseCases)
    {
        _productsRepository = productsRepository;
        _productoUseCases = productoUseCases;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] string searchBy = "name")
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { mensaje = "El término de búsqueda es obligatorio." });

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

    [HttpGet("paginado")]
    public async Task<ActionResult<PagedResponse<ProductResponseDto>>> GetPaginado(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? term = null,
    [FromQuery] string searchBy = "name")
    {
        var resultado = await _productsRepository.ListarPaginadoAsync(pageNumber, pageSize, term, searchBy);

        // Mapear los productos del dominio a DTOs planos para evitar objetos ValueObjects en la respuesta
        var itemsDto = resultado.Items.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock
        }).ToList();

        var pagedDto = new PagedResponse<ProductResponseDto>
            (itemsDto, 
            resultado.TotalCount, 
            resultado.PageNumber, 
            resultado.PageSize);

        return Ok(pagedDto);
    }

 

}