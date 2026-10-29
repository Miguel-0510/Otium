using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Otium.Components;
using Otium.Components.Account;
using Otium.Data;
using Otium.Services;
using Otium.Services.Extensions;
using Otium.Services.Options;
using Sentry;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var puerto = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");

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

var proveedorBaseDatos = builder.Services.AddBaseDeDatos(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

builder.Services.AddScoped<SaludDbCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<SaludDbCheck>("db");

builder.Services.AddOptions<ProteccionDatosOptions>()
    .Bind(builder.Configuration.GetSection(ProteccionDatosOptions.Seccion));

var dataProtection = builder.Configuration
    .GetSection(ProteccionDatosOptions.Seccion)
    .Get<ProteccionDatosOptions>() ?? new ProteccionDatosOptions();

var proteccionDatos = builder.Services.AddDataProtection()
    .SetApplicationName(dataProtection.ApplicationName);

var proteccionDatosEnBaseDatos = false;
if (!string.IsNullOrWhiteSpace(dataProtection.KeysPath))
{
    try
    {
        var directorio = new DirectoryInfo(dataProtection.KeysPath);
        directorio.Create();

        // Comprueba que el directorio es escribible: si el volumen es de solo lectura,
        // la generación de claves fallaría en la primera petición con un error 500.
        var prueba = Path.Combine(directorio.FullName, $".probe-{Guid.NewGuid():N}");
        using (var flujo = new FileStream(prueba, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
        {
            flujo.WriteByte(0);
        }

        proteccionDatos.PersistKeysToFileSystem(directorio);
    }
    catch (Exception ex)
    {
        // Una ruta configurada que no se puede usar invalidaría las sesiones en cada
        // reinicio sin que nadie se entere: es preferible fallar al arrancar.
        throw new InvalidOperationException(
            $"No se pudo preparar Data Protection en '{dataProtection.KeysPath}'.", ex);
    }
}
else
{
    // Render free no ofrece discos persistentes. Guardar el llavero en la base de datos
    // evita invalidar sesiones y enlaces de recuperación en cada reinicio o despliegue.
    proteccionDatos.PersistKeysToDbContext<ApplicationDbContext>();
    proteccionDatosEnBaseDatos = true;
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

builder.Services.AddOptions<BrevoOptions>()
    .Bind(builder.Configuration.GetSection(BrevoOptions.Seccion))
    .PostConfigure(opciones =>
    {
        opciones.ApiKey = (opciones.ApiKey ?? "").Trim();

        if (string.IsNullOrWhiteSpace(opciones.ApiKey))
        {
            opciones.ApiKey = (builder.Configuration["BREVO_API_KEY"] ?? "").Trim();
        }

        if (string.IsNullOrWhiteSpace(opciones.ApiKey))
        {
            opciones.ApiKey = (builder.Configuration["Smtp:Password"] ?? "").Trim();
        }

        opciones.SenderEmail = (opciones.SenderEmail ?? "").Trim();

        if (string.IsNullOrWhiteSpace(opciones.SenderEmail))
        {
            opciones.SenderEmail = (builder.Configuration["Smtp:SenderEmail"]
                ?? "otium.lectura@gmail.com").Trim();
        }

        opciones.SenderName = string.IsNullOrWhiteSpace(opciones.SenderName)
            ? (builder.Configuration["Smtp:SenderName"] ?? "Otium").Trim()
            : opciones.SenderName.Trim();

        opciones.Endpoint = string.IsNullOrWhiteSpace(opciones.Endpoint)
            ? "https://api.brevo.com/v3/smtp/email"
            : opciones.Endpoint.Trim();
    })
    .ValidateDataAnnotations()
    .Validate(opciones => Uri.TryCreate(opciones.Endpoint, UriKind.Absolute, out var endpoint)
        && endpoint.Scheme == Uri.UriSchemeHttps,
        "Brevo:Endpoint debe ser una URL HTTPS.");

builder.Services.AddOptions<DefinicionesOptions>()
    .Bind(builder.Configuration.GetSection(DefinicionesOptions.Seccion))
    .PostConfigure(opciones =>
    {
        if (string.IsNullOrWhiteSpace(opciones.ApiKey))
        {
            opciones.ApiKey = (builder.Configuration["GLM_API_KEY"]
                ?? Environment.GetEnvironmentVariable("ZAI_API_KEY")
                ?? "").Trim();
        }

        opciones.ApiKey = (opciones.ApiKey ?? "").Trim();
        opciones.Endpoint = string.IsNullOrWhiteSpace(opciones.Endpoint)
            ? DefinicionesOptions.EndpointPorDefecto
            : opciones.Endpoint.Trim();
        opciones.Modelo = string.IsNullOrWhiteSpace(opciones.Modelo)
            ? DefinicionesOptions.ModeloPorDefecto
            : opciones.Modelo.Trim();
    })
    .Validate(opciones => !string.IsNullOrWhiteSpace(opciones.Modelo),
        "Definiciones:Modelo no puede estar vacío.")
    .Validate(opciones => Uri.TryCreate(opciones.Endpoint, UriKind.Absolute, out var urlEndpoint)
        && urlEndpoint.Scheme == Uri.UriSchemeHttps,
        "Definiciones:Endpoint debe ser una URL HTTPS.");

builder.Services.AddOptions<SitioOptions>()
    .Bind(builder.Configuration.GetSection(SitioOptions.Seccion))
    .Validate(opciones => string.IsNullOrWhiteSpace(opciones.UrlPublica)
        || (Uri.TryCreate(opciones.UrlPublica, UriKind.Absolute, out var url)
            && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)),
        "Sitio:UrlPublica debe ser una URL absoluta http o https.");

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
builder.Services.AddHttpClient<IEnvioCorreo, BrevoEnvioCorreo>(cliente =>
{
    cliente.Timeout = TimeSpan.FromSeconds(15);
    cliente.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddScoped<IEmailSender<ApplicationUser>, EmailSender>();

builder.Services.AddHttpClient<IBuscadorLibros, BuscadorLibros>(cliente =>
{
    cliente.BaseAddress = new Uri("https://openlibrary.org/");
    cliente.DefaultRequestHeaders.UserAgent.ParseAdd("Otium/1.0");
    cliente.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<ILibroService, LibroService>();
builder.Services.AddScoped<IAmistadService, AmistadService>();
builder.Services.AddScoped<IEntradaLecturaService, EntradaLecturaService>();

var endpointDefiniciones = builder.Configuration[DefinicionesOptions.Seccion + ":Endpoint"]?.Trim() is { Length: > 0 } valor
    ? valor
    : DefinicionesOptions.EndpointPorDefecto;
if (!endpointDefiniciones.EndsWith('/'))
{
    endpointDefiniciones += "/";
}
builder.Services.AddHttpClient<IDefinicionClient, DefinicionesClient>(cliente =>
{
    cliente.BaseAddress = new Uri(endpointDefiniciones);
    cliente.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<IGlosarioService, GlosarioService>();
builder.Services.AddScoped<IBackupService, BackupService>();

var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.Logging.AddSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.TracesSampleRate = builder.Environment.IsDevelopment() ? 1.0 : 0.1;
        options.SendDefaultPii = false;
        options.IsEnvironmentUser = true;
    });
}

var app = builder.Build();

DefinicionesOptions? opcionesDefiniciones = null;
BrevoOptions? opcionesBrevo = null;

try
{
    opcionesDefiniciones = app.Services.GetRequiredService<IOptions<DefinicionesOptions>>().Value;
}
catch (Exception ex) when (ex is OptionsValidationException or InvalidOperationException)
{
    app.Logger.LogError(ex, "La configuración del servicio de definiciones no es válida.");
}

try
{
    opcionesBrevo = app.Services.GetRequiredService<IOptions<BrevoOptions>>().Value;
}
catch (Exception ex) when (ex is OptionsValidationException or InvalidOperationException)
{
    app.Logger.LogError(ex, "La configuración de Brevo HTTP no es válida.");
}

DiagnosticoConfiguracion.Registrar(app.Configuration, app.Logger, opcionesDefiniciones, opcionesBrevo);

using (var ambito = app.Services.CreateScope())
{
    try
    {
        var definiciones = ambito.ServiceProvider.GetService<IDefinicionClient>();
        var envio = ambito.ServiceProvider.GetService<IEnvioCorreo>();
        var correo = ambito.ServiceProvider.GetService<IEmailSender<ApplicationUser>>();

        app.Logger.LogInformation(
            "Inyección de dependencias verificada: IDefinicionClient -> {Definiciones}; " +
            "IEnvioCorreo -> {Envio}; IEmailSender<ApplicationUser> -> {Correo}.",
            definiciones?.GetType().Name ?? "NO REGISTRADO",
            envio?.GetType().Name ?? "NO REGISTRADO",
            correo?.GetType().Name ?? "NO REGISTRADO");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "No se pudieron resolver los servicios externos desde el contenedor de dependencias.");
    }
}

app.UseForwardedHeaders();

await BaseDatosInicializador.InicializarAsync(app.Services, proveedorBaseDatos, app.Logger);

if (proteccionDatosEnBaseDatos)
{
    app.Logger.LogInformation(
        "Data Protection persiste las claves en la base de datos ({Proveedor}).",
        proveedorBaseDatos);
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

app.MapHealthChecks("/health");

app.Run();

public partial class Program { }

sealed class SaludDbCheck : IHealthCheck
{
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;

    public SaludDbCheck(IDbContextFactory<ApplicationDbContext> fabrica)
    {
        _fabrica = fabrica;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext contexto, CancellationToken token)
    {
        using var db = _fabrica.CreateDbContext();
        var conectado = await db.Database.CanConnectAsync(token);
        return conectado
            ? HealthCheckResult.Healthy("Base de datos conectada.")
            : HealthCheckResult.Unhealthy("Base de datos sin conexión.");
    }
}
