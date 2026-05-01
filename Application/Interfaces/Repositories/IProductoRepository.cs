using Application.DTOs.Common;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IProductoRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task UpdateRangeAsync(IEnumerable<Product> products);
    Task<IEnumerable<Product>> SearchAsync(string term, string searchBy);
    Task<PagedResponse<Product>> ListarPaginadoAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "name");
}
