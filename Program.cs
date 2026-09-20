using Microsoft.EntityFrameworkCore;
using Otium.Components;
using Otium.Data;
using Otium.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios: lo que la app necesita para funcionar
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<OtiumContext>(opciones =>
    opciones.UseSqlite("Data Source=otium.db"));

builder.Services.AddHttpClient<BuscadorLibros>(cliente =>
{
    cliente.BaseAddress = new Uri("https://openlibrary.org/");
    cliente.DefaultRequestHeaders.UserAgent.ParseAdd("Otium/1.0 (tu-correo@ejemplo.com)");
    cliente.Timeout = TimeSpan.FromSeconds(10);
});

// Construye la aplicación con lo anterior
var app = builder.Build();

// Configuración del servidor
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
