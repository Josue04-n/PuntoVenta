using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ISaleRepository
{
    Task<string> GenerateInvoiceNumberAsync();
    Task AddAsync(Sale sale);
    Task<IEnumerable<Sale>> SearchAsync(string term, string searchBy, string? sellerName = null);
    Task<(IEnumerable<Sale> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? term = null, string? searchBy = null, string? sellerName = null);
    Task<Sale?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Sale>> GetMonthlySalesAsync(DateTime firstDayOfMonth);
    Task<int> SaveSaleAsync(Sale sale);
}
