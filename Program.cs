using Microsoft.EntityFrameworkCore;
using Otium.Components;
using Otium.Data;

var builder = WebApplication.CreateBuilder(args);

// Servicios: lo que la app necesita para funcionar
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<OtiumContext>(opciones =>
    opciones.UseSqlite("Data Source=otium.db"));

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
