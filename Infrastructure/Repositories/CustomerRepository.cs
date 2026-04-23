using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string term, string searchBy)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Enumerable.Empty<Customer>();

        term = term.Trim().ToLower();
        searchBy = searchBy.Trim().ToLower();

        if (searchBy == "id")
        {
            if (int.TryParse(term, out int idSearch))
            {
                return await _context.Customers
                 .AsNoTracking()
                 .Where(c => c.Id == idSearch)
                 .Take(1)
                 .ToListAsync();
            }
            return Enumerable.Empty<Customer>();
        }

        if (searchBy == "cedula")
        {
            return await _context.Customers
             .AsNoTracking()
             .Where(c => c.IDCard == term)
             .Take(1)
             .ToListAsync();
        }

        return await _context.Customers
            .AsNoTracking()
            .Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term))
            .Take(15)
            .ToListAsync();
    }
}
