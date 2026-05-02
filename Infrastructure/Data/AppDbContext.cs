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

    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    base.OnModelCreating(modelBuilder);

    //    modelBuilder.Entity<Product>()
    //    .ToTable(tb => tb.HasTrigger("TR_PreventNegativeStockAndPrice"));

    //    modelBuilder.HasSequence<int>("InvoiceSequence")
    //                .StartsAt(1)
    //                .IncrementsBy(1);

    //    modelBuilder.Entity<Sale>()
    //        .HasOne(s => s.Customer) // Una venta tiene UN cliente
    //        .WithMany()              // Un cliente puede tener MUCHAS ventas
    //        .HasForeignKey(s => s.CustomerId)
    //        .OnDelete(DeleteBehavior.Restrict);

    //    modelBuilder.Entity<Product>().OwnsOne(p => p.UnitPrice, price =>
    //    {
    //        price.Property(p => p.Worth).HasColumnName("UnitPrice").HasPrecision(18, 2);
    //    });

    //    modelBuilder.Entity<SaleDetail>().OwnsOne(d => d.UnitPrice, price =>
    //    {
    //        price.Property(p => p.Worth).HasColumnName("UnitPrice").HasPrecision(18, 2);
    //    });

    //    modelBuilder.Entity<Sale>()
    //        .HasMany(v => v.Details) 
    //        .WithOne()                
    //        .HasForeignKey("SaleId") 
    //        .IsRequired()
    //        .OnDelete(DeleteBehavior.Cascade); 

    //    modelBuilder.Entity<SaleDetail>()
    //        .HasOne(sd => sd.Product)
    //        .WithMany()
    //        .HasForeignKey(sd => sd.ProductId)
    //        .IsRequired()
    //        .OnDelete(DeleteBehavior.Restrict);

    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);

            entity.OwnsOne(p => p.UnitPrice, price =>
            {
                price.Property(p => p.Worth)
                     .HasColumnName("UnitPrice")
                     .HasPrecision(18, 2)
                     .IsRequired();
            });

            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.ToTable("SaleDetails");
            entity.HasKey(vd => vd.Id);

            entity.OwnsOne(vd => vd.UnitPrice, price =>
            {
                price.Property(p => p.Worth)
                     .HasColumnName("UnitPrice") 
                     .HasPrecision(18, 2)
                     .IsRequired();
            });
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IDCard).IsUnique();

            entity.Property(e => e.IDCard).HasMaxLength(10).IsRequired();

        });

    }
}
