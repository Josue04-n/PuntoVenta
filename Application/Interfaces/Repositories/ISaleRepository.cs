using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ISaleRepository
{
    Task<string> GenerateInvoiceNumberAsync();
    Task AddAsync(Sale sale);
    Task<IEnumerable<Sale>> SearchAsync(string term, string searchBy);

}
