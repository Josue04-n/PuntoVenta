using Application.Interfaces.Repositories;
using Domain.Entities;

namespace Application.UseCases;

public class SearchSalesHandler
{
    private readonly ISaleRepository _saleRepository;
    public SearchSalesHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<IEnumerable<Sale>> ExecuteAsync(string term, string searchBy, string? sellerName = null)
    {
        return await _saleRepository.SearchAsync(term, searchBy, sellerName);
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> ExecutePagedAsync(
        int page, 
        int pageSize, 
        string? term, 
        string? searchBy, 
        string? sellerName = null)
    {
        return await _saleRepository.GetPagedAsync(page, pageSize, term, searchBy, sellerName);
    }
}
