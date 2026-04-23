using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<int>("InvoiceSequence")
                    .StartsAt(1)
                    .IncrementsBy(1);

        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Customer) // Una venta tiene UN cliente
            .WithMany()              // Un cliente puede tener MUCHAS ventas
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>().OwnsOne(p => p.UnitPrice, price =>
        {
            price.Property(p => p.Worth).HasColumnName("UnitPrice").HasPrecision(18, 2);
        });

        modelBuilder.Entity<SaleDetail>().OwnsOne(d => d.UnitPrice, price =>
        {
            price.Property(p => p.Worth).HasColumnName("UnitPrice").HasPrecision(18, 2);
        });

        modelBuilder.Entity<Sale>()
            .HasMany(v => v.Details) 
            .WithOne()                
            .HasForeignKey("SaleId") 
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); 

        modelBuilder.Entity<SaleDetail>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(vd => vd.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
