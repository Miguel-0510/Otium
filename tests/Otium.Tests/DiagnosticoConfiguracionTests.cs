using Microsoft.Extensions.Configuration;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class DiagnosticoConfiguracionTests
{
    [Fact]
    public void Registra_longitudes_y_no_expone_los_secretos()
    {
        const string claveGemini = "clave-gemini-secreta-1234567890";
        const string claveBrevo = "xkeysib-clave-brevo-secreta-1234567890";

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = claveGemini,
                ["Brevo:ApiKey"] = claveBrevo,
                ["Brevo:SenderEmail"] = "remitente@example.com",
                ["Brevo:SenderName"] = "Otium",
                ["Brevo:Endpoint"] = "https://api.brevo.com/v3/smtp/email"
            })
            .Build();

        var logger = new LoggerCapturador<object>();

        DiagnosticoConfiguracion.Registrar(
            configuracion,
            logger,
            new GeminiOptions { ApiKey = claveGemini, Modelo = "gemini-test" },
            new BrevoOptions
            {
                ApiKey = claveBrevo,
                SenderEmail = "remitente@example.com",
                SenderName = "Otium",
                Endpoint = "https://api.brevo.com/v3/smtp/email"
            });

        Assert.Contains($"longitud={claveGemini.Length}", logger.Texto, StringComparison.Ordinal);
        Assert.Contains($"longitud {claveBrevo.Length}", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("https://api.brevo.com/v3/smtp/email", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("remitente@example.com", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("API v3", logger.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(claveGemini, logger.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(claveBrevo, logger.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Advierte_por_las_claves_de_brevo_y_gemini_que_faltan()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var logger = new LoggerCapturador<object>();

        DiagnosticoConfiguracion.Registrar(
            configuracion,
            logger,
            new GeminiOptions { ApiKey = "", Modelo = "gemini-test" },
            new BrevoOptions { ApiKey = "", SenderEmail = "" });

        Assert.Contains("[Warning]", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("Brevo:ApiKey", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("Brevo:SenderEmail", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("Gemini no tiene API key", logger.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Advierte_si_la_clave_parece_una_smtp_key()
    {
        const string claveSmtp = "xsmtpsib-clave-smtp-secreta";

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Brevo:ApiKey"] = claveSmtp,
                ["Brevo:SenderEmail"] = "remitente@example.com",
                ["Brevo:Endpoint"] = "https://api.brevo.com/v3/smtp/email"
            })
            .Build();

        var logger = new LoggerCapturador<object>();

        DiagnosticoConfiguracion.Registrar(
            configuracion,
            logger,
            new GeminiOptions { ApiKey = "clave-gemini-suficientemente-larga", Modelo = "gemini-test" },
            new BrevoOptions
            {
                ApiKey = claveSmtp,
                SenderEmail = "remitente@example.com",
                Endpoint = "https://api.brevo.com/v3/smtp/email"
            });

        Assert.Contains("SMTP key", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("xkeysib-", logger.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(claveSmtp, logger.Texto, StringComparison.Ordinal);
    }
}
