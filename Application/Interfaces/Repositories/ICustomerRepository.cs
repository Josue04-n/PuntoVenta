using Application.DTOs.Common;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer> GetByIdCardAsync(string IDCard);

    Task<IEnumerable<Customer>> SearchAsync(string term, string searchBY);

    Task<PagedResponse<Customer>> ListAsyncPaginatedClients(
        int pageNumber, 
        int pageSize, 
        string? term = null,
        string searchBy = "name");
}
