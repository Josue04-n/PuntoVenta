using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;

    public SaleRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT NEXT VALUE FOR InvoiceSequence";

        var result = await command.ExecuteScalarAsync();
        int sequential = Convert.ToInt32(result);
        return $"FAC-{sequential:D7}";
    }

    public async Task AddAsync(Sale sale)
    {
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

    }
    // Implementación de búsqueda por número de factura o fecha
    public async Task<IEnumerable<Sale>> SearchAsync(string term, string searchBy)
    {
        if (string.IsNullOrWhiteSpace(term)) return Enumerable.Empty<Sale>();

        term = term.Trim().ToLower();
        searchBy = searchBy.Trim().ToLower();   

        //ñvar query = _context.Sales
        IQueryable<Sale> query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsNoTracking();

        if (searchBy == "id" && int.TryParse(term, out int searchId))
        {
            return await query
                .Where(s => s.Id == searchId)
                .ToListAsync();
        }

        if (searchBy == "recientes")
        {
            return await query
                .OrderByDescending(s => s.IssueDate)
                .Take(50)
                .ToListAsync();
        }

        if (searchBy == "numero")
        {
            return await query
                .Where(s => s.InvoiceNumber.ToLower().Contains(term))
                .OrderByDescending(s => s.IssueDate)
                .Take(30)
                .ToListAsync();
        }
        if (searchBy == "cliente")
        {
            var customerIds = await _context.Customers
                .Where(c => c.Name.ToLower().Contains(term) || c.LastName.ToLower().Contains(term))
                .Select(c => c.Id)
                .ToListAsync();

            return await query
                .Where(s => customerIds.Contains(s.CustomerId))
                .OrderByDescending(s => s.IssueDate)
                .Take(30)   
                .ToListAsync();
        }
        return Enumerable.Empty<Sale>();
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? term = null, string? searchBy = null)
    {
        IQueryable<Sale> query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            searchBy = searchBy?.Trim().ToLower() ?? "numero";

            if (searchBy == "id" && int.TryParse(term, out int searchId))
            {
                query = query.Where(s => s.Id == searchId);
            }
            else if (searchBy == "numero")
            {
                query = query.Where(s => s.InvoiceNumber.ToLower().Contains(term));
            }
            else if (searchBy == "cliente")
            {
                query = query.Where(s => s.Customer != null && 
                    (s.Customer.Name.ToLower().Contains(term) || s.Customer.LastName.ToLower().Contains(term)));
            }
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Sale?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<int> SaveSaleAsync(Sale sale)
    {
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();
        return sale.Id;
    }
}
