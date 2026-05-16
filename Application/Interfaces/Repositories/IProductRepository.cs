using Application.DTOs.Common;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task UpdateRangeAsync(IEnumerable<Product> products);
    Task<IEnumerable<Product>> SearchAsync(string term, string searchBy);
    Task<PagedResponse<Product>> GetPagedAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "name");
    
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}
