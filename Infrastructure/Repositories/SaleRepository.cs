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
            .Include(s => s.Details)
            .Include(s => s.Customer)
            .AsNoTracking();

        if (searchBy == "numero")
        {
            return await query
                .Where(s => s.InvoiceNumber.ToLower().Contains(term))
                .OrderByDescending(s => s.IssueDate)
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
                .ToListAsync();
        }
        return Enumerable.Empty<Sale>();
    }
}
