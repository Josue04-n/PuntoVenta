using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Repositories;
using Application.DTOs.Common;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id) 
    { 
        return await _context.Products.FindAsync(id);
    }

    public Task UpdateRangeAsync(IEnumerable<Product> products)
    {
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Product>> SearchAsync(string term, string searchBy)
    {
        term = term.Trim().ToLower();

        if (searchBy.ToLower() == "id")
        {
            if (int.TryParse(term, out int idSearch))
            {
                return await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == idSearch)
                    .Take(1)
                    .ToListAsync();
            }
            return Enumerable.Empty<Product>();
        }

        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.ToLower().StartsWith(term))
            .OrderBy(p => p.Name)
            .Take(20)
            .ToListAsync();
    }

    public async Task<List<Product>> SearchByNameAsync(string name)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.StartsWith(name))
            .Take(50)
            .ToListAsync();
    }

    public async Task<PagedResponse<Product>> ListarPaginadoAsync(int pageNumber, int pageSize, string? term = null)
    {
        var query = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(p => p.Name.Contains(term) || p.Id.ToString().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<Product>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResponse<Product>> ListarPaginadoAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "name")
    {
        IQueryable<Product> query = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim();

            query = searchBy.ToLower() switch
            {
                "id" when int.TryParse(term, out int idValue) => query.Where(p => p.Id == idValue),
                "id" => query.Where(p => p.Id.ToString().StartsWith(term)),

                _ => query.Where(p => p.Name.StartsWith(term))
            };
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<Product>(items, totalCount, pageNumber, pageSize);
    }
}
