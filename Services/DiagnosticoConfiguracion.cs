using Otium.Services.Options;

namespace Otium.Services;

public static class DiagnosticoConfiguracion
{
    public static void Registrar(
        IConfiguration configuracion,
        ILogger logger,
        GeminiOptions? gemini,
        BrevoOptions? brevo)
    {
        RegistrarGemini(configuracion, logger, gemini);
        RegistrarBrevo(configuracion, logger, brevo);
    }

    private static void RegistrarGemini(IConfiguration configuracion, ILogger logger, GeminiOptions? opciones)
    {
        var valorConfiguracion = configuracion["Gemini:ApiKey"];
        var valorEntornoDoble = Environment.GetEnvironmentVariable("Gemini__ApiKey");
        var valorAlias = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        var claveEfectiva = (opciones?.ApiKey
            ?? valorConfiguracion
            ?? valorAlias
            ?? "").Trim();

        var modelo = string.IsNullOrWhiteSpace(opciones?.Modelo)
            ? "gemini-flash-lite-latest"
            : opciones!.Modelo;

        var origen = !string.IsNullOrWhiteSpace(valorConfiguracion)
            ? "Gemini:ApiKey/Gemini__ApiKey"
            : !string.IsNullOrWhiteSpace(valorAlias)
                ? "GEMINI_API_KEY"
                : "ninguna";

        logger.LogInformation(
            "Diagnóstico Gemini: Gemini:ApiKey={PresenteConfiguracion} (longitud {LongitudConfiguracion}); " +
            "Gemini__ApiKey={PresenteEntornoDoble} (longitud {LongitudEntornoDoble}); " +
            "GEMINI_API_KEY={PresenteAlias} (longitud {LongitudAlias}); " +
            "clave efectiva longitud={LongitudEfectiva}, origen='{Origen}', modelo='{Modelo}'.",
            !string.IsNullOrWhiteSpace(valorConfiguracion),
            valorConfiguracion?.Length ?? 0,
            !string.IsNullOrWhiteSpace(valorEntornoDoble),
            valorEntornoDoble?.Length ?? 0,
            !string.IsNullOrWhiteSpace(valorAlias),
            valorAlias?.Length ?? 0,
            claveEfectiva.Length,
            origen,
            modelo);

        if (claveEfectiva.Length == 0)
        {
            logger.LogWarning(
                "Gemini no tiene API key configurada. Define Gemini__ApiKey o GEMINI_API_KEY " +
                "en el entorno del servidor; el glosario inteligente no funcionará.");
            return;
        }

        if (claveEfectiva.Length < 20)
        {
            logger.LogWarning(
                "Gemini:ApiKey tiene una longitud atípica ({Longitud}). " +
                "Verifica que la variable de entorno no esté truncada o contenga comillas.",
                claveEfectiva.Length);
        }
    }

    private static void RegistrarBrevo(IConfiguration configuracion, ILogger logger, BrevoOptions? opciones)
    {
        var claveConfiguracion = configuracion["Brevo:ApiKey"];
        var claveEntorno = Environment.GetEnvironmentVariable("BREVO_API_KEY");
        var claveSmtp = configuracion["Smtp:Password"];

        var clave = (opciones?.ApiKey
            ?? claveConfiguracion
            ?? claveEntorno
            ?? claveSmtp
            ?? "").Trim();

        var origen = !string.IsNullOrWhiteSpace(claveConfiguracion)
            ? "Brevo:ApiKey"
            : !string.IsNullOrWhiteSpace(claveEntorno)
                ? "BREVO_API_KEY"
                : !string.IsNullOrWhiteSpace(claveSmtp)
                    ? "Smtp:Password (legado)"
                    : "ninguna";

        var tipoClave = ClasificarClave(clave);
        var remitente = (opciones?.SenderEmail
            ?? configuracion["Brevo:SenderEmail"]
            ?? configuracion["Smtp:SenderEmail"]
            ?? "otium.lectura@gmail.com").Trim();
        var nombre = string.IsNullOrWhiteSpace(opciones?.SenderName)
            ? (configuracion["Brevo:SenderName"] ?? configuracion["Smtp:SenderName"] ?? "Otium").Trim()
            : opciones!.SenderName.Trim();
        var endpoint = (opciones?.Endpoint
            ?? configuracion["Brevo:Endpoint"]
            ?? "https://api.brevo.com/v3/smtp/email").Trim();

        logger.LogInformation(
            "Diagnóstico Brevo HTTP: Endpoint='{Endpoint}', SenderEmail='{SenderEmail}', " +
            "SenderName='{SenderName}', ApiKey configurada={ApiKeyConfigurada} " +
            "(longitud {LongitudApiKey}, tipo={TipoApiKey}, origen='{Origen}').",
            endpoint,
            remitente,
            nombre,
            clave.Length > 0,
            clave.Length,
            tipoClave,
            origen);

        if (clave.Length == 0)
        {
            logger.LogWarning(
                "Brevo HTTP no tiene API key configurada. Define Brevo:ApiKey " +
                "(variable de entorno Brevo__ApiKey) con una API key v3 de Brevo " +
                "(empieza por xkeysib-); el correo no se enviará.");
        }
        else if (tipoClave == "SMTP")
        {
            logger.LogWarning(
                "La clave configurada parece una SMTP key (xsmtpsib-). La API REST de Brevo " +
                "requiere una API key v3 (xkeysib-); genera una en SMTP & API → API Keys & MCP.");
        }
        else if (tipoClave == "desconocida")
        {
            logger.LogWarning(
                "No se pudo identificar el tipo de la API key de Brevo por su prefijo. " +
                "Si el envío devuelve HTTP 401, usa una API key v3 (xkeysib-).");
        }

        if (origen.StartsWith("Smtp:Password", StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Brevo:ApiKey no está configurada; se usa Smtp:Password como respaldo. " +
                "Ese valor debe ser una API key v3 de Brevo, no una clave SMTP.");
        }

        if (string.IsNullOrWhiteSpace(remitente))
        {
            logger.LogWarning(
                "Brevo:SenderEmail está vacío. Configura un remitente verificado en Brevo.");
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            logger.LogWarning(
                "Brevo:Endpoint='{Endpoint}' no es una URL HTTPS válida. " +
                "Se espera https://api.brevo.com/v3/smtp/email.",
                endpoint);
        }
    }

    private static string ClasificarClave(string clave)
    {
        if (clave.StartsWith("xkeysib-", StringComparison.OrdinalIgnoreCase))
        {
            return "API v3";
        }

        if (clave.StartsWith("xsmtpsib-", StringComparison.OrdinalIgnoreCase))
        {
            return "SMTP";
        }

        return clave.Length == 0 ? "ninguna" : "desconocida";
    }
}
