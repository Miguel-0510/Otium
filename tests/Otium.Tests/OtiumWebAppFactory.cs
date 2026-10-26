using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;

namespace Otium.Tests;

[CollectionDefinition("Otium", DisableParallelization = true)]
public sealed class OtiumCollection : ICollectionFixture<OtiumWebAppFactory> { }

public sealed class OtiumWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _rutaBaseDatos = Path.Combine(
        Path.GetTempPath(),
        $"otium-tests-{Guid.NewGuid():N}.db");

    public OtiumWebAppFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"DataSource={_rutaBaseDatos};Cache=Shared");
        Environment.SetEnvironmentVariable("Brevo__ApiKey", "xkeysib-pruebas");
        Environment.SetEnvironmentVariable("Brevo__SenderEmail", "pruebas@example.com");
        Environment.SetEnvironmentVariable("Brevo__SenderName", "Otium");
        Environment.SetEnvironmentVariable("Brevo__Endpoint", "https://api.brevo.com/v3/smtp/email");
        Environment.SetEnvironmentVariable("Gemini__ApiKey", "");
        Environment.SetEnvironmentVariable("Gemini__Modelo", "gemini-flash-lite-latest");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<FakeEmailSender>());
        });
    }

    public FakeEmailSender Correos => Services.GetRequiredService<FakeEmailSender>();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();

        foreach (var ruta in new[] { _rutaBaseDatos, $"{_rutaBaseDatos}-wal", $"{_rutaBaseDatos}-shm" })
        {
            if (File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }
    }
}
