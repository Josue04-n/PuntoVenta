using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ISaleRepository
{
    Task<string> GenerateInvoiceNumberAsync();
    Task AddAsync(Sale sale);
    Task<IEnumerable<Sale>> SearchAsync(string term, string searchBy); //POSIBLEMENTE ELIMINAR CUANDO SE HAGA LA PAGINACION
    Task<(IEnumerable<Sale> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? term = null, string? searchBy = null);
    Task<Sale?> GetByIdWithDetailsAsync(int id);
    Task<int> SaveSaleAsync(Sale sale);
}
