using Application.DTOs;
using Application.DTOs.Common;
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
    public async Task<ActionResult<PagedResponse<CustomerResponseDto>>> Search(
        [FromQuery] string? termino = null,
        [FromQuery] string criterio = "cedula",
        [FromQuery] int pagina = 1,
        [FromQuery] int tamaño = 10)
    {
        var resultado = await _clienteRepository.ListAsyncPaginatedClients(pagina, tamaño, termino, criterio);

        var itemsDto = resultado.Items.Select(c => new CustomerResponseDto
        {
            Id = c.Id,
            IDCard = c.IDCard,
            Name = $"{c.LastName} {c.Name}",
            Phone = c.Phone,
            Address = c.Address,
            Email = c.Email
        }).ToList();

        return Ok(new PagedResponse<CustomerResponseDto>(itemsDto, resultado.TotalCount, pagina, tamaño));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponseDto>> GetById(int id)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id);

        if (cliente == null) return NotFound(new { mensaje = "Cliente no encontrado" });

        return Ok(new CustomerResponseDto
        {
            Id = cliente.Id,
            IDCard = cliente.IDCard,
            Name = cliente.Name,
            LastName = cliente.LastName
        });
    }

}
