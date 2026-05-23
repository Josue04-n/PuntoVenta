using API.Configurations;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirBlazor", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 1. Agregar servicios al contenedor (Dependency Injection)
builder.Services.AddControllers();
builder.Services.AddProjectDependencies(builder.Configuration);
builder.Services.AddMicrosoftAuthentication(builder.Configuration);

// --- JWT AUTHENTICATION CONFIGURATION ---
var jwtConfig = builder.Configuration.GetSection("JWT");
var secretKey = Encoding.UTF8.GetBytes(jwtConfig["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = jwtConfig["ValidAudience"],
        ValidIssuer = jwtConfig["ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

// Configuración de Swagger / OpenAPI mejorada para JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Punto de Venta API", Version = "v1" });
    
    // Configuración para usar el botón "Authorize" en Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer' [espacio] y su token JWT.\n\nEjemplo: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    
    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    };

    c.AddSecurityRequirement(securityRequirement);
});

// 2. Construir la aplicación
var app = builder.Build();

// 3. Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("PermitirBlazor");

app.UseAuthentication(); // IMPORTANTE: Antes de Authorization
app.UseAuthorization();

app.MapControllers();

// --- SEEDING DE DATOS INICIALES ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<ApplicationRole>>();

        // 1. Crear Roles si no existen
        string[] roles = { "Administrador", "Vendedor" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        // 2. Crear Usuario Administrador inicial
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@pos.com",
                FirstName = "Admin",
                LastName = "Maestro",
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Administrador");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al sembrar los datos iniciales.");
    }
}

app.Run();
