using Application.DTOs;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Components.RouteAttribute;

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
    public async Task<IActionResult> Search([FromQuery]string term, [FromQuery] string searchBy = "name")
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

        return Ok(results);

    }

}
