using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options) 
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Capturar el nombre del usuario real logueado desde el contexto HTTP
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

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
        modelBuilder.Entity<InventoryMovement>().HasQueryFilter(im => im.IsActive);

        // --- CONFIGURACIÓN DE MOVIMIENTOS DE INVENTARIO ---
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(im => im.Id);
            entity.Property(im => im.UnitCost).HasPrecision(18, 2);
            entity.Property(im => im.Reference).HasMaxLength(250);
            
            entity.HasOne(im => im.Product)
                  .WithMany()
                  .HasForeignKey(im => im.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- CONFIGURACIÓN DE PRODUCTOS ---
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TR_PreventNegativeStockAndPrice"));
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(p => p.Name); // Índice para búsquedas por nombre

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
            entity.HasIndex(e => e.LastName); // Índice para ordenamiento y búsqueda
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
            entity.Property(s => s.Status).HasConversion<int>().IsRequired();
            entity.HasIndex(s => s.InvoiceNumber); // Índice para búsqueda de facturas
            entity.HasIndex(s => s.IssueDate);     // Índice para reportes y ordenamiento

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

        // --- CONFIGURACIÓN DE USUARIOS ---
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(u => u.LastName); // Índice para paginación de usuarios
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
