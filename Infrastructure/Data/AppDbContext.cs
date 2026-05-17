using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = "System"; // TODO: Implement real user tracking later

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.Deactivate(currentUser);
                    
                    foreach (var reference in entry.References.Where(r => r.TargetEntry != null && r.TargetEntry.Metadata.IsOwned()))
                    {
                        if (reference.TargetEntry != null)
                        {
                            reference.TargetEntry.State = EntityState.Modified;
                        }
                    }
                    break;
            }

        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- FILTROS GLOBALES PARA ENTIDADES AUDITABLES ---
        modelBuilder.Entity<Product>().HasQueryFilter(p => p.IsActive);
        modelBuilder.Entity<Customer>().HasQueryFilter(c => c.IsActive);
        modelBuilder.Entity<Sale>().HasQueryFilter(s => s.IsActive);
        modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => u.IsActive);

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
            entity.HasIndex(e => e.IDCard).IsUnique(); 
            entity.Property(e => e.IDCard).HasMaxLength(13).IsRequired(); 
        });

        // --- CONFIGURACIÓN DE VENTAS ---
        modelBuilder.HasSequence<int>("InvoiceSequence").StartsAt(1).IncrementsBy(1);

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(v => v.SubTotal).HasPrecision(18, 2);
            entity.Property(v => v.VatPercentage).HasPrecision(5, 2);
            entity.Property(v => v.VatAmount).HasPrecision(18, 2);
            entity.Property(v => v.Total).HasPrecision(18, 2);

            entity.ToTable(t => t.HasCheckConstraint("CK_Sale_Total_Consistency", "Total = SubTotal + VatAmount"));

            entity.HasKey(s => s.Id);

            entity.HasOne(s => s.Customer)
                  .WithMany()
                  .HasForeignKey(s => s.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

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
