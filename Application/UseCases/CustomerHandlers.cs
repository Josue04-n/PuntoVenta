using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;

namespace Application.UseCases;

public class CustomerHandlers
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerHandlers(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync();
        return customer;
    }

    public async Task ReactivateCustomerAsync(int customerId, UpdateCustomerRequest request)
    {
        // We use GetByIdCardIncludingInactiveAsync to bypass global filter since the customer is inactive
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
        await _unitOfWork.SaveChangesAsync();
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
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCustomerAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        if (await _customerRepository.HasRelatedRecordsAsync(id))
        {
            customer.Deactivate();
            await _customerRepository.UpdateAsync(customer);
        }
        else
        {
            await _customerRepository.DeleteAsync(id);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
