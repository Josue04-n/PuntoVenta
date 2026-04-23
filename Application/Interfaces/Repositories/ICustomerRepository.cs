using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> SearchAsync(string term, string searchBY);
    Task<Customer?> GetByIdAsync(int id);
}
