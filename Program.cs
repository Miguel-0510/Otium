using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Otium.Components;
using Otium.Components.Account;
using Otium.Data;
using Otium.Services;
using Otium.Services.Options;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

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

var exigirHttps = builder.Environment.IsProduction()
    && !builder.Configuration.GetValue("Seguridad:PermitirHttp", false);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;

    var proxies = builder.Configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? [];
    if (proxies.Length == 0)
    {
        return;
    }

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    foreach (var proxy in proxies)
    {
        if (IPAddress.TryParse(proxy, out var direccion))
        {
            options.KnownProxies.Add(direccion);
        }
    }
});

void ConfigurarCookie(CookieAuthenticationOptions options)
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = exigirHttps
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
}

builder.Services.ConfigureApplicationCookie(ConfigurarCookie);
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme, ConfigurarCookie);
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, ConfigurarCookie);
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorRememberMeScheme, ConfigurarCookie);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

Action<DbContextOptionsBuilder> opcionesSqlite = options =>
    options.UseSqlite(connectionString);

builder.Services.AddDbContext<ApplicationDbContext>(opcionesSqlite);
builder.Services.AddDbContextFactory<ApplicationDbContext>(opcionesSqlite, ServiceLifetime.Scoped);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddOptions<ProteccionDatosOptions>()
    .Bind(builder.Configuration.GetSection(ProteccionDatosOptions.Seccion));

var dataProtection = builder.Configuration
    .GetSection(ProteccionDatosOptions.Seccion)
    .Get<ProteccionDatosOptions>() ?? new ProteccionDatosOptions();

if (!string.IsNullOrWhiteSpace(dataProtection.KeysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtection.KeysPath))
        .SetApplicationName(dataProtection.ApplicationName);
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

builder.Services.AddOptions<CorreoOptions>()
    .Bind(builder.Configuration.GetSection(CorreoOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<GeminiOptions>()
    .Bind(builder.Configuration.GetSection(GeminiOptions.Seccion))
    .Validate(opciones => !string.IsNullOrWhiteSpace(opciones.Modelo),
        "Gemini:Modelo no puede estar vacío.")
    .ValidateOnStart();

builder.Services.AddOptions<SitioOptions>()
    .Bind(builder.Configuration.GetSection(SitioOptions.Seccion))
    .Validate(opciones => string.IsNullOrWhiteSpace(opciones.UrlPublica)
        || (Uri.TryCreate(opciones.UrlPublica, UriKind.Absolute, out var url)
            && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)),
        "Sitio:UrlPublica debe ser una URL absoluta http o https.")
    .ValidateOnStart();

builder.Services.AddSingleton<IEnlacePublicoService, EnlacePublicoService>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILimiteIntentosService, LimiteIntentosService>();

builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
    {
        var ruta = contexto.Request.Path.Value?.ToLowerInvariant() ?? "";
        var ip = contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        return ruta switch
        {
            "/account/login" => RateLimitPartition.GetFixedWindowLimiter(
                $"login:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }),
            "/account/register" => RateLimitPartition.GetFixedWindowLimiter(
                $"registro:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }),
            "/account/forgotpassword" => RateLimitPartition.GetFixedWindowLimiter(
                $"recuperacion:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }),
            "/account/resetpassword" => RateLimitPartition.GetFixedWindowLimiter(
                $"reset:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }),
            _ => RateLimitPartition.GetNoLimiter("libre")
        };
    });

    opciones.OnRejected = (contexto, _) =>
    {
        var logger = contexto.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");

        logger.LogWarning(
            "Solicitud limitada por tasa en {Ruta} desde {Ip}.",
            contexto.HttpContext.Request.Path,
            contexto.HttpContext.Connection.RemoteIpAddress);

        return ValueTask.CompletedTask;
    };
});

builder.Services.AddSingleton<ICodigoAmigoGenerator, CodigoAmigoGenerator>();
builder.Services.AddScoped<ICodigoAmigoService, CodigoAmigoService>();
builder.Services.AddScoped<IRegistroService, RegistroService>();
builder.Services.AddScoped<IRecuperacionContrasenaService, RecuperacionContrasenaService>();
builder.Services.AddScoped<IEliminacionCuentaService, EliminacionCuentaService>();
builder.Services.AddScoped<IExportacionDatosService, ExportacionDatosService>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, EmailSender>();

builder.Services.AddHttpClient<IBuscadorLibros, BuscadorLibros>(cliente =>
{
    cliente.BaseAddress = new Uri("https://openlibrary.org/");
    cliente.DefaultRequestHeaders.UserAgent.ParseAdd("Otium/1.0");
    cliente.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<ILibroService, LibroService>();
builder.Services.AddScoped<IAmistadService, AmistadService>();
builder.Services.AddScoped<IEntradaLecturaService, EntradaLecturaService>();

builder.Services.AddHttpClient<IGeminiClient, GeminiClient>(cliente =>
{
    cliente.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    cliente.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddScoped<IGlosarioService, GlosarioService>();

var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

if (exigirHttps)
{
    app.UseHsts();
}

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
        "connect-src 'self' ws: wss: https://openlibrary.org https://covers.openlibrary.org;");
    await next();
});

if (exigirHttps)
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program { }
