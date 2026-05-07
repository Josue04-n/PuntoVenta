using Application.Interfaces.Repositories;
using Domain.Entities;

namespace Application.UseCases;

public class SearchSalesUseCase
{
    private readonly ISaleRepository _saleRepository;
    public SearchSalesUseCase(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<IEnumerable<Sale>> ExecuteAsync(string term, string searchBy)
    {
        return await _saleRepository.SearchAsync(term, searchBy);
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> ExecutePagedAsync(int page, int pageSize, string? term, string? searchBy)
    {
        return await _saleRepository.GetPagedAsync(page, pageSize, term, searchBy);
    }
}
