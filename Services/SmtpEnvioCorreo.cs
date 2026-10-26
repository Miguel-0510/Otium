using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Otium.Services.Options;

namespace Otium.Services;

public sealed class SmtpEnvioCorreo : IEnvioCorreo
{
    private const int TimeoutMilisegundos = 15_000;

    private readonly CorreoOptions _opciones;
    private readonly ILogger<SmtpEnvioCorreo> _logger;

    public SmtpEnvioCorreo(IOptions<CorreoOptions> opciones, ILogger<SmtpEnvioCorreo> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(_opciones.Host)
        && !string.IsNullOrWhiteSpace(_opciones.Usuario)
        && !string.IsNullOrWhiteSpace(_opciones.Clave)
        && !string.IsNullOrWhiteSpace(_opciones.Remitente);

    public async Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default)
    {
        if (!EstaConfigurado)
        {
            _logger.LogWarning(
                "SMTP no está configurado; se omite el envío a {Destinatario}. " +
                "Configura Correo:Host, Correo:Usuario, Correo:Clave y Correo:Remitente.",
                destinatario);
            return false;
        }

        using var mensaje = new MailMessage();
        mensaje.From = new MailAddress(_opciones.Remitente, "Otium");
        mensaje.To.Add(destinatario);
        mensaje.Subject = asunto;
        mensaje.Body = cuerpoHtml;
        mensaje.IsBodyHtml = true;

        using var cliente = new SmtpClient(_opciones.Host, _opciones.Puerto)
        {
            Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Clave),
            EnableSsl = true,
            Timeout = TimeoutMilisegundos
        };

        try
        {
            await cliente.SendMailAsync(mensaje, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError(
                "El envío SMTP a {Destinatario} superó el tiempo límite de {Timeout} ms.",
                destinatario,
                TimeoutMilisegundos);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo SMTP a {Destinatario}", destinatario);
            return false;
        }
    }
}
