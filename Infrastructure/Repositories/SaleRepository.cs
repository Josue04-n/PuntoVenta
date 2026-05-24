using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
        bool wasClosed = connection.State == System.Data.ConnectionState.Closed;
        
        if (wasClosed)
        {
            await connection.OpenAsync();
        }

        try
        {
            using var command = connection.CreateCommand();
            if (_context.Database.CurrentTransaction != null)
            {
                command.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();
            }
            command.CommandText = "SELECT NEXT VALUE FOR InvoiceSequence";

            var result = await command.ExecuteScalarAsync();
            int sequential = Convert.ToInt32(result);
            return $"FAC-{sequential:D7}";
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task AddAsync(Sale sale)
    {
        await _context.Sales.AddAsync(sale);
    }

    public async Task<IEnumerable<Sale>> SearchAsync(string term, string searchBy, string? sellerName = null)
    {
        term = term.Trim().ToLower();
        searchBy = searchBy.Trim().ToLower();   

        IQueryable<Sale> query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsNoTracking();

        // Aplicar filtro de vendedor si se proporciona
        if (!string.IsNullOrEmpty(sellerName))
        {
            query = query.Where(s => s.CreatedBy == sellerName);
        }

        if (searchBy == "id" && int.TryParse(term, out int searchId))
        {
            return await query
                .Where(s => s.Id == searchId)
                .ToListAsync();
        }

        if (searchBy == "recent")
        {
            return await query
                .OrderByDescending(s => s.IssueDate)
                .Take(50)
                .ToListAsync();
        }

        if (searchBy == "number")
        {
            return await query
                .Where(s => s.InvoiceNumber.ToLower().Contains(term))
                .OrderByDescending(s => s.IssueDate)
                .Take(30)
                .ToListAsync();
        }
        if (searchBy == "customer")
        {
            return await query
                .Where(s => s.Customer != null && (s.Customer.Name.ToLower().Contains(term) || s.Customer.LastName.ToLower().Contains(term)))
                .OrderByDescending(s => s.IssueDate)
                .Take(30)   
                .ToListAsync();
        }
        return Enumerable.Empty<Sale>();
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? term = null, 
        string? searchBy = null, 
        string? sellerName = null)
    {
        IQueryable<Sale> query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsNoTracking();

        // Aplicar filtro de vendedor si se proporciona
        if (!string.IsNullOrEmpty(sellerName))
        {
            query = query.Where(s => s.CreatedBy == sellerName);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            searchBy = searchBy?.Trim().ToLower() ?? "number";

            if (searchBy == "id" && int.TryParse(term, out int searchId))
            {
                query = query.Where(s => s.Id == searchId);
            }
            else if (searchBy == "number")
            {
                query = query.Where(s => s.InvoiceNumber.ToLower().Contains(term));
            }
            else if (searchBy == "customer")
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

    public async Task<IEnumerable<Sale>> GetMonthlySalesAsync(DateTime firstDayOfMonth)
    {
        return await _context.Sales
            .Where(s => s.IssueDate >= firstDayOfMonth)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> SaveSaleAsync(Sale sale)
    {
        await _context.Sales.AddAsync(sale);
        return sale.Id;
    }
}
 