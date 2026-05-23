using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.UseCases;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;


namespace API.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // --- IDENTITY CONFIGURATION ---
        services.AddIdentity<ApplicationUser, ApplicationRole>(options => 
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            // Un bloqueo permanente de unos 100 años (o puedes usar un valor fijo grande)
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(36500);
            options.Lockout.MaxFailedAccessAttempts = 3;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<PerformSaleHandler>();
        services.AddScoped<SearchSalesHandler>();
        services.AddScoped<ProductHandlers>();
        services.AddScoped<CustomerHandlers>();
        services.AddScoped<InventoryHandlers>();

        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddMicrosoftAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuramos la autenticación de Microsoft (Azure AD / Entra ID)
        // Usamos un esquema diferente ("Microsoft") para no colisionar con nuestro JWT predeterminado
        services.AddAuthentication()
            .AddMicrosoftIdentityWebApi(configuration, "AzureAd", "Microsoft");

        return services;
    }
}
