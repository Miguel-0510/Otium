using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Otium.Services.Options;

namespace Otium.Services;

public sealed class BrevoEnvioCorreo : IEnvioCorreo
{
    private const int MaxLongitudDetalle = 1200;
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _cliente;
    private readonly BrevoOptions _opciones;
    private readonly ILogger<BrevoEnvioCorreo> _logger;

    public BrevoEnvioCorreo(
        HttpClient cliente,
        IOptions<BrevoOptions> opciones,
        ILogger<BrevoEnvioCorreo> logger)
    {
        _cliente = cliente;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(_opciones.ApiKey)
        && !string.IsNullOrWhiteSpace(_opciones.SenderEmail)
        && Uri.TryCreate(_opciones.Endpoint, UriKind.Absolute, out var endpoint)
        && endpoint.Scheme == Uri.UriSchemeHttps;

    public async Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default)
    {
        var clavesFaltantes = ObtenerClavesFaltantes();
        if (clavesFaltantes.Count > 0)
        {
            _logger.LogWarning(
                "Brevo HTTP no está configurado; se omite el envío a {Destinatario}. " +
                "Faltan o son inválidos: {Claves}. Configura Brevo:ApiKey (API key v3, " +
                "empieza por xkeysib-), Brevo:SenderEmail y Brevo:Endpoint.",
                destinatario,
                string.Join(", ", clavesFaltantes));
            return false;
        }

        _logger.LogInformation(
            "Iniciando envío HTTP a {Destinatario} mediante {Endpoint} (remitente {Remitente}).",
            destinatario,
            _opciones.Endpoint,
            _opciones.SenderEmail);

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, _opciones.Endpoint)
        {
            Content = JsonContent.Create(
                new BrevoEmailRequest(
                    new BrevoRemitente(_opciones.SenderEmail, _opciones.SenderName),
                    [new BrevoDestinatario(destinatario)],
                    asunto,
                    cuerpoHtml),
                options: OpcionesJson)
        };

        solicitud.Headers.TryAddWithoutValidation("api-key", _opciones.ApiKey);
        solicitud.Headers.Accept.ParseAdd("application/json");

        try
        {
            using var respuesta = await _cliente.SendAsync(
                solicitud,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var detalle = await LeerDetalleAsync(respuesta, cancellationToken);

            if (respuesta.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Correo enviado por la API HTTP de Brevo a {Destinatario}. " +
                    "HTTP {Codigo} ({Estado}). {Detalle}",
                    destinatario,
                    (int)respuesta.StatusCode,
                    respuesta.StatusCode,
                    detalle);
                return true;
            }

            _logger.LogError(
                "Brevo rechazó el correo a {Destinatario}. HTTP {Codigo} ({Estado}). " +
                "Detalle: {Detalle}. Causa probable: {Causa}.",
                destinatario,
                (int)respuesta.StatusCode,
                respuesta.StatusCode,
                detalle,
                Clasificar(respuesta.StatusCode));
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(
                ex,
                "El envío a {Destinatario} mediante la API HTTP de Brevo superó el tiempo " +
                "límite de {Timeout} segundos. Detalle: {Detalle}.",
                destinatario,
                _cliente.Timeout.TotalSeconds,
                ex.Message);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Error HTTP al enviar a {Destinatario} mediante la API de Brevo ({Endpoint}). " +
                "StatusCode: {StatusCode}. Detalle: {Detalle}.",
                destinatario,
                _opciones.Endpoint,
                ex.StatusCode,
                ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo enviar el correo a {Destinatario} mediante la API de Brevo " +
                "({Endpoint}). Detalle: {Detalle}.",
                destinatario,
                _opciones.Endpoint,
                ex.Message);
            return false;
        }
    }

    private List<string> ObtenerClavesFaltantes()
    {
        var faltantes = new List<string>();

        if (string.IsNullOrWhiteSpace(_opciones.ApiKey))
        {
            faltantes.Add("Brevo:ApiKey");
        }

        if (string.IsNullOrWhiteSpace(_opciones.SenderEmail))
        {
            faltantes.Add("Brevo:SenderEmail");
        }

        if (!Uri.TryCreate(_opciones.Endpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            faltantes.Add("Brevo:Endpoint (HTTPS)");
        }

        return faltantes;
    }

    private async Task<string> LeerDetalleAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            return "sin cuerpo";
        }

        try
        {
            using var documento = JsonDocument.Parse(cuerpo);
            var raiz = documento.RootElement;

            var codigo = raiz.TryGetProperty("code", out var propiedadCodigo)
                ? propiedadCodigo.GetString()
                : null;
            var mensaje = raiz.TryGetProperty("message", out var propiedadMensaje)
                ? propiedadMensaje.GetString()
                : null;
            var idMensaje = raiz.TryGetProperty("messageId", out var propiedadId)
                ? propiedadId.GetString()
                : null;

            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                partes.Add($"code={codigo}");
            }

            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                partes.Add($"message={mensaje}");
            }

            if (!string.IsNullOrWhiteSpace(idMensaje))
            {
                partes.Add($"messageId={idMensaje}");
            }

            if (partes.Count > 0)
            {
                return string.Join("; ", partes);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "La respuesta de Brevo no es JSON; se usa el cuerpo crudo.");
        }

        return cuerpo.Length <= MaxLongitudDetalle
            ? cuerpo
            : $"{cuerpo[..MaxLongitudDetalle]}...";
    }

    private static string Clasificar(HttpStatusCode codigo)
    {
        if (codigo is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "API key rechazada: usa una API key v3 de Brevo (xkeysib-...), " +
                   "no una clave SMTP (xsmtpsib-...) ni la contraseña de la cuenta.";
        }

        if (codigo == HttpStatusCode.BadRequest)
        {
            return "solicitud inválida: revisa el remitente verificado, el destinatario y el contenido.";
        }

        if (codigo == HttpStatusCode.TooManyRequests)
        {
            return "límite de envío de Brevo alcanzado; reintenta más tarde.";
        }

        if ((int)codigo >= 500)
        {
            return "Brevo no está disponible temporalmente; reintenta más tarde.";
        }

        return "revisa la configuración de Brevo y el detalle de la respuesta.";
    }

    private sealed record BrevoEmailRequest(
        [property: JsonPropertyName("sender")] BrevoRemitente Sender,
        [property: JsonPropertyName("to")] IReadOnlyList<BrevoDestinatario> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("htmlContent")] string HtmlContent);

    private sealed record BrevoRemitente(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string Name);

    private sealed record BrevoDestinatario(
        [property: JsonPropertyName("email")] string Email);
}
