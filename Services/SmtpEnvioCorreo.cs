using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Otium.Services.Options;

namespace Otium.Services;

public sealed class SmtpEnvioCorreo : IEnvioCorreo
{
    private const int TimeoutMilisegundos = 15_000;

    private readonly SmtpOptions _opciones;
    private readonly ILogger<SmtpEnvioCorreo> _logger;

    public SmtpEnvioCorreo(IOptions<SmtpOptions> opciones, ILogger<SmtpEnvioCorreo> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(_opciones.Host)
        && !string.IsNullOrWhiteSpace(_opciones.Username)
        && !string.IsNullOrWhiteSpace(_opciones.Password)
        && !string.IsNullOrWhiteSpace(_opciones.SenderEmail);

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
                "Configura Smtp:Host, Smtp:Username, Smtp:Password y Smtp:SenderEmail.",
                destinatario);
            return false;
        }

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(_opciones.SenderEmail, _opciones.SenderName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mensaje.To.Add(destinatario);

            using var cliente = new SmtpClient(_opciones.Host, _opciones.Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_opciones.Username, _opciones.Password),
                EnableSsl = true,
                Timeout = TimeoutMilisegundos
            };

            await cliente.SendMailAsync(mensaje, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(
                ex,
                "El envío SMTP a {Destinatario} mediante {Host}:{Puerto} superó el tiempo límite de {Timeout} ms.",
                destinatario,
                _opciones.Host,
                _opciones.Port,
                TimeoutMilisegundos);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo enviar el correo SMTP a {Destinatario} mediante {Host}:{Puerto}.",
                destinatario,
                _opciones.Host,
                _opciones.Port);
            return false;
        }
    }
}
