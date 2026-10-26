using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Otium.Services.Options;

namespace Otium.Services;

public sealed class ResendEnvioCorreo : IEnvioCorreo
{
    private const string RemitentePorDefecto = "Otium <onboarding@resend.dev>";

    private readonly HttpClient _http;
    private readonly ResendOptions _opciones;
    private readonly CorreoOptions _correo;
    private readonly ILogger<ResendEnvioCorreo> _logger;

    public ResendEnvioCorreo(
        HttpClient http,
        IOptions<ResendOptions> opciones,
        IOptions<CorreoOptions> correo,
        ILogger<ResendEnvioCorreo> logger)
    {
        _http = http;
        _opciones = opciones.Value;
        _correo = correo.Value;
        _logger = logger;
    }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_opciones.ApiKey);

    private string Remitente =>
        !string.IsNullOrWhiteSpace(_opciones.Remitente) ? _opciones.Remitente.Trim()
        : !string.IsNullOrWhiteSpace(_correo.Remitente) ? _correo.Remitente.Trim()
        : RemitentePorDefecto;

    public async Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default)
    {
        if (!EstaConfigurado)
        {
            _logger.LogWarning(
                "Resend no está configurado (falta Resend:ApiKey); se omite el envío a {Destinatario}.",
                destinatario);
            return false;
        }

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                from = Remitente,
                to = new[] { destinatario },
                subject = asunto,
                html = cuerpoHtml
            })
        };

        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opciones.ApiKey.Trim());
        solicitud.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var respuesta = await _http.SendAsync(solicitud, cancellationToken);

            if (respuesta.IsSuccessStatusCode)
            {
                return true;
            }

            var detalle = await respuesta.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Resend rechazó el correo a {Destinatario} con estado {Estado}. Detalle: {Detalle}",
                destinatario,
                (int)respuesta.StatusCode,
                detalle);
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError(
                "El envío con Resend a {Destinatario} superó el tiempo límite del cliente HTTP.",
                destinatario);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al enviar el correo con Resend a {Destinatario}", destinatario);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar el correo con Resend a {Destinatario}", destinatario);
            return false;
        }
    }

}
