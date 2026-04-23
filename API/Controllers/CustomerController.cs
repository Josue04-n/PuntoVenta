using Application.DTOs;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerRepository _clienteRepository;

    public CustomerController(ICustomerRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] string searchBy = "cedula")
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { mensaje = "El término de búsqueda es obligatorio." });

        var results = await _clienteRepository.SearchAsync(term, searchBy);

        var response = results.Select(c => new CustomerResponseDto
        {
            Id = c.Id,
            IDCard = c.IDCard,
            Name = c.Name,
            LastName = c.LastName,
            Phone = c.Phone,
            Address = c.Address,
            Email = c.Email
        });

        return Ok(results);

    }

}
