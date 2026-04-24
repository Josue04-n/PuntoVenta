using API.Configurations;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirBlazor", policy =>
    {
        policy.AllowAnyOrigin()    // Permite cualquier puerto (Blazor)
              .AllowAnyHeader()    // Permite cualquier tipo de dato (JSON)
              .AllowAnyMethod();   // Permite GET, POST, PUT, DELETE
    });
});

// 1. Agregar servicios al contenedor (Dependency Injection)
builder.Services.AddControllers();
// Inyectar tus dependencias personalizadas
builder.Services.AddProyectDependencies(builder.Configuration);
// Configuración de Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Punto de Venta API", Version = "v1" });
});

// 2. Construir la aplicación (Solo usamos 'builder')
var app = builder.Build();

// 3. Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("PermitirBlazor");
app.UseAuthorization();
app.MapControllers();

app.Run();