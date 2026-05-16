using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Domain.Entities;

namespace Application.UseCases;

public class CustomerHandlers
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerHandlers(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Verificar si ya existe un cliente con esa cédula, incluso si está inactivo
        var existing = await _customerRepository.GetByIdCardIncludingInactiveAsync(request.IDCard);
        
        if (existing != null)
        {
            if (!existing.IsActive)
            {
                throw new InactiveCustomerExistsException(existing.Id, existing.IDCard);
            }
            throw new InvalidOperationException("Ya existe un cliente con esta identificación.");
        }

        var customer = new Customer(
            request.IDCard,
            request.Name,
            request.LastName,
            request.Phone,
            request.Address,
            request.Email
        );

        await _customerRepository.AddAsync(customer);
        return customer;
    }

    public async Task ReactivateCustomerAsync(int customerId, UpdateCustomerRequest request)
    {
        // We use GetByIdCardIncludingInactiveAsync to bypass global filter since the customer is inactive
        // Wait, GetByIdAsync in repo uses FindAsync, which respects global filters by default?
        // Actually, FindAsync DOES respect global filters. We need to fetch it without filters to reactivate it.
        // Let's implement a clean way or just update it via repository if the repository's Add/Update bypasses it.
        // Wait, let's fix FindAsync behavior or use GetByIdCardIncludingInactiveAsync to get it.
        var customer = await _customerRepository.GetByIdCardIncludingInactiveAsync(request.IDCard) 
            ?? throw new KeyNotFoundException("Cliente inactivo no encontrado.");

        if (customer.Id != customerId)
        {
             throw new InvalidOperationException("Los IDs no coinciden.");
        }

        customer.Activate();

        customer.Update(
            request.IDCard,
            request.Name,
            request.LastName,
            request.Phone,
            request.Address,
            request.Email
        );

        await _customerRepository.UpdateAsync(customer);
    }

    public async Task UpdateCustomerAsync(UpdateCustomerRequest request)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // Verificar si el cambio de cédula colisiona con otro cliente
        if (customer.IDCard != request.IDCard)
        {
            var existing = await _customerRepository.GetByIdCardIncludingInactiveAsync(request.IDCard);
            if (existing != null)
            {
                 if (!existing.IsActive)
                    throw new InactiveCustomerExistsException(existing.Id, existing.IDCard);
                 else
                    throw new InvalidOperationException("Ya existe otro cliente con esta identificación.");
            }
        }

        customer.Update(
            request.IDCard,
            request.Name,
            request.LastName,
            request.Phone,
            request.Address,
            request.Email
        );

        await _customerRepository.UpdateAsync(customer);
    }

    public async Task DeleteCustomerAsync(int id)
    {
        await _customerRepository.DeleteAsync(id);
    }
}
