using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- CONFIGURACIÓN DE PRODUCTOS ---
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TR_PreventNegativeStockAndPrice"));
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();

            entity.OwnsOne(p => p.UnitPrice, price =>
            {
                price.Property(p => p.Worth).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired();
            });
        });

        // --- CONFIGURACIÓN DE CLIENTES ---
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IDCard).IsUnique(); // Índice para búsquedas masivas rápidas
            entity.Property(e => e.IDCard).HasMaxLength(13).IsRequired(); // Ajustado a 13 para RUC Ecuador
        });

        // --- CONFIGURACIÓN DE VENTAS (EL CORAZÓN) ---
        modelBuilder.HasSequence<int>("InvoiceSequence").StartsAt(1).IncrementsBy(1);

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(s => s.Id);

            // Relación con Cliente (DIP & SRP)
            entity.HasOne(s => s.Customer)
                  .WithMany()
                  .HasForeignKey(s => s.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relación con Detalles (Composición)
            entity.HasMany(v => v.Details)
                  .WithOne()
                  .HasForeignKey("SaleId")
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- CONFIGURACIÓN DE DETALLES ---
        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.HasKey(vd => vd.Id);

            entity.OwnsOne(vd => vd.UnitPrice, price =>
            {
                price.Property(p => p.Worth).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired();
            });

            entity.HasOne(sd => sd.Product)
                  .WithMany()
                  .HasForeignKey(sd => sd.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}