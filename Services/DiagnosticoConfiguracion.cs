using Otium.Services.Options;

namespace Otium.Services;

public static class DiagnosticoConfiguracion
{
    public static void Registrar(
        IConfiguration configuracion,
        ILogger logger,
        GeminiOptions? gemini,
        SmtpOptions? smtp)
    {
        RegistrarGemini(configuracion, logger, gemini);
        RegistrarSmtp(configuracion, logger, smtp);
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

    private static void RegistrarSmtp(IConfiguration configuracion, ILogger logger, SmtpOptions? opciones)
    {
        var host = (opciones?.Host ?? configuracion["Smtp:Host"] ?? "smtp-relay.brevo.com").Trim();
        var puerto = opciones?.Port ?? LeerPuerto(configuracion["Smtp:Port"]);
        var usuarioConfigurado = configuracion["Smtp:Username"] ?? opciones?.Username ?? "";
        var claveConfigurada = configuracion["Smtp:Password"] ?? opciones?.Password ?? "";
        var usuario = usuarioConfigurado.Trim();
        var clave = claveConfigurada.Trim();
        var usuarioRecortado = usuarioConfigurado.Length != usuario.Length;
        var claveRecortada = claveConfigurada.Length != clave.Length;
        var remitente = (opciones?.SenderEmail ?? configuracion["Smtp:SenderEmail"] ?? "").Trim();
        var nombre = (opciones?.SenderName ?? configuracion["Smtp:SenderName"] ?? "").Trim();
        var puertoConfigurado = !string.IsNullOrWhiteSpace(configuracion["Smtp:Port"]);

        logger.LogInformation(
            "Diagnóstico Brevo SMTP: Host='{Host}', Puerto={Puerto}, Username='{Username}', " +
            "SenderEmail='{SenderEmail}', SenderName='{SenderName}', " +
            "Password configurada={PasswordConfigurada} (longitud {LongitudPassword}).",
            host,
            puerto,
            SmtpEnvioCorreo.EnmascararUsuario(usuario),
            remitente,
            nombre,
            !string.IsNullOrWhiteSpace(clave),
            clave.Length);

        if (usuarioRecortado)
        {
            logger.LogWarning(
                "Smtp:Username tenía espacios o saltos de línea al inicio/fin; se recortaron. " +
                "Revisa la variable Smtp__Username en el entorno de despliegue.");
        }

        if (claveRecortada)
        {
            logger.LogWarning(
                "Smtp:Password tenía espacios o saltos de línea al inicio/fin; se recortaron. " +
                "Revisa la variable Smtp__Password en el entorno de despliegue.");
        }

        var faltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(host))
        {
            faltantes.Add("Smtp:Host");
        }

        if (string.IsNullOrWhiteSpace(usuario))
        {
            faltantes.Add("Smtp:Username");
        }

        if (string.IsNullOrWhiteSpace(clave))
        {
            faltantes.Add("Smtp:Password");
        }

        if (string.IsNullOrWhiteSpace(remitente))
        {
            faltantes.Add("Smtp:SenderEmail");
        }

        if (faltantes.Count > 0)
        {
            logger.LogWarning(
                "Brevo SMTP está incompleto. Faltan o están vacíos: {Claves}. " +
                "Configúralos como variables de entorno (por ejemplo Smtp__Password) " +
                "para que la recuperación de contraseña pueda enviar correos.",
                string.Join(", ", faltantes));
            return;
        }

        if (!puertoConfigurado)
        {
            logger.LogWarning(
                "Smtp:Port no está configurado; se usará el valor por defecto {Puerto}. " +
                "Brevo usa 587 con STARTTLS.",
                puerto);
        }
        else if (!int.TryParse(configuracion["Smtp:Port"], out _))
        {
            logger.LogWarning(
                "Smtp:Port='{Valor}' no es un número válido; se usará {Puerto}.",
                configuracion["Smtp:Port"],
                puerto);
        }

        if (puerto is < 1 or > 65535)
        {
            logger.LogWarning(
                "Smtp:Port={Puerto} está fuera del rango válido (1-65535). " +
                "Brevo usa 587 con STARTTLS.",
                puerto);
        }

        if (puerto != 587 && puerto != 465 && puerto != 2525)
        {
            logger.LogWarning(
                "Smtp:Port={Puerto} no es un puerto habitual de Brevo. " +
                "Los valores esperados son 587 (STARTTLS), 465 (SSL) o 2525.",
                puerto);
        }

        logger.LogInformation(
            "Brevo SMTP configurado. Se usará {Host}:{Puerto} con STARTTLS y remitente {Remitente}.",
            host,
            puerto,
            remitente);
    }

    private static int LeerPuerto(string? valor) =>
        int.TryParse(valor, out var puerto) ? puerto : 587;
}
