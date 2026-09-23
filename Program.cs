using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Otium.Components;
using Otium.Components.Account;
using Otium.Data;
using Otium.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString), ServiceLifetime.Scoped);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<ILibroService, LibroService>();
builder.Services.AddScoped<IAmistadService, AmistadService>();

// En el servidor, las claves que protegen las sesiones se guardan en una carpeta
// persistente, para que un reinicio no cierre la sesión de todos.
var rutaClaves = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(rutaClaves))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(rutaClaves))
        .SetApplicationName("Otium");
}

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<DescriptorErroresEs>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddHttpClient<BuscadorLibros>(cliente =>
{
    cliente.BaseAddress = new Uri("https://openlibrary.org/");
    cliente.DefaultRequestHeaders.UserAgent.ParseAdd("Otium/1.0 (tu-correo@ejemplo.com)");
    cliente.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// Crea o actualiza la base de datos al arrancar (no hay terminal en el servidor).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Asigna un codigo unico a usuarios que quedaron sin uno (por ejemplo, creados antes de esta funcion)
    const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    var aleatorio = new Random();
    var sinCodigo = db.Users.Where(u => u.CodigoAmigo == "" || u.CodigoAmigo == null).ToList();
    foreach (var usuario in sinCodigo)
    {
        string codigo;
        do
        {
            codigo = new string(Enumerable.Range(0, 6).Select(_ => caracteres[aleatorio.Next(caracteres.Length)]).ToArray());
        }
        while (db.Users.Any(u => u.CodigoAmigo == codigo));

        usuario.CodigoAmigo = codigo;
    }
    if (sinCodigo.Count > 0)
    {
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' https: data:; " +
        "connect-src 'self' wss: https://openlibrary.org https://covers.openlibrary.org; " +
        "frame-ancestors 'self';");
    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();




