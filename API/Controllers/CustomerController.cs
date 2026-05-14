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
    private readonly ICustomerRepository _customerRepository;

    public CustomerController(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<CustomerResponseDto>>> Search(
        [FromQuery] string? term = null,
        [FromQuery] string criterion = "card",
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _customerRepository.GetPagedAsync(page, size, term, criterion);

        var itemsDto = result.Items.Select(c => new CustomerResponseDto
        {
            Id = c.Id,
            IDCard = c.IDCard,
            Name = c.Name,
            LastName = c.LastName,
            Phone = c.Phone,
            Address = c.Address,
            Email = c.Email
        }).ToList();

        return Ok(new PagedResponse<CustomerResponseDto>(itemsDto, result.TotalCount, page, size));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponseDto>> GetById(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer == null) return NotFound(new { message = "Customer not found" });

        return Ok(new CustomerResponseDto
        {
            Id = customer.Id,
            IDCard = customer.IDCard,
            Name = customer.Name,
            LastName = customer.LastName
        });
    }

}
