using Microsoft.Extensions.Configuration;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class DiagnosticoConfiguracionTests
{
    [Fact]
    public void Registra_longitudes_y_no_expone_los_secretos()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "clave-gemini-secreta-1234567890",
                ["Smtp:Host"] = "smtp-relay.brevo.com",
                ["Smtp:Port"] = "587",
                ["Smtp:Username"] = "usuario@smtp-brevo.com",
                ["Smtp:Password"] = "password-smtp-super-secreto",
                ["Smtp:SenderEmail"] = "remitente@example.com",
                ["Smtp:SenderName"] = "Otium"
            })
            .Build();

        const string claveGemini = "clave-gemini-secreta-1234567890";
        var logger = new LoggerCapturador<object>();

        DiagnosticoConfiguracion.Registrar(
            configuracion,
            logger,
            new GeminiOptions { ApiKey = claveGemini, Modelo = "gemini-test" },
            new SmtpOptions
            {
                Host = "smtp-relay.brevo.com",
                Port = 587,
                Username = "usuario@smtp-brevo.com",
                Password = "password-smtp-super-secreto",
                SenderEmail = "remitente@example.com",
                SenderName = "Otium"
            });

        Assert.Contains($"longitud={claveGemini.Length}", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("smtp-relay.brevo.com", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("usuario@smtp-brevo.com", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("remitente@example.com", logger.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain("clave-gemini-secreta", logger.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain("password-smtp-super-secreto", logger.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Advierte_por_las_claves_smtp_que_faltan()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var logger = new LoggerCapturador<object>();

        DiagnosticoConfiguracion.Registrar(
            configuracion,
            logger,
            new GeminiOptions { ApiKey = "", Modelo = "gemini-test" },
            new SmtpOptions { Username = "", Password = "", SenderEmail = "" });

        Assert.Contains("[Warning]", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("Smtp:Username", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("Smtp:Password", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("Smtp:SenderEmail", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("Gemini no tiene API key", logger.Texto, StringComparison.Ordinal);
    }
}
