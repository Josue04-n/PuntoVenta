using Blazor;
using Blazor.Security;
using Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- SEGURIDAD Y JWT ---
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>(sp => (CustomAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());
builder.Services.AddTransient<JwtInterceptor>();

// --- CONFIGURACIÓN DE HTTPCLIENT ---
builder.Services.AddScoped(sp => 
{
    var interceptor = sp.GetRequiredService<JwtInterceptor>();
    interceptor.InnerHandler = new HttpClientHandler();
    return new HttpClient(interceptor) { BaseAddress = new Uri("http://localhost:5055/") };
});

builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<UserApiService>();
builder.Services.AddScoped<PosApiService>();
builder.Services.AddScoped<CustomerApiService>();
builder.Services.AddScoped<NotificationApiService>();

await builder.Build().RunAsync();
