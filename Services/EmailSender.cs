using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Otium.Data;

namespace Otium.Services;

public class EmailSender : IEmailSender<ApplicationUser>
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        EnviarAsync(email, "Confirma tu cuenta de Otium",
            $"<p>Hola,</p><p>Confirma tu cuenta de Otium haciendo clic en el siguiente enlace:</p><p><a href=\"{confirmationLink}\">Confirmar mi cuenta</a></p><p>Si no creaste esta cuenta, ignora este correo.</p>");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        EnviarAsync(email, "Recupera tu contraseña de Otium",
            $"<p>Hola,</p><p>Recibimos una solicitud para restablecer tu contraseña en Otium. Haz clic en el siguiente enlace para elegir una nueva:</p><p><a href=\"{resetLink}\">Restablecer mi contraseña</a></p><p>Si no pediste esto, ignora este correo; tu contraseña seguirá siendo la misma.</p>");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        EnviarAsync(email, "Tu código para restablecer la contraseña",
            $"<p>Hola,</p><p>Tu código para restablecer la contraseña en Otium es:</p><p style=\"font-size:1.4em;font-weight:bold;\">{resetCode}</p>");

    private async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var host = _config["Correo:Host"];
        var puerto = int.Parse(_config["Correo:Puerto"] ?? "587");
        var usuario = _config["Correo:Usuario"];
        var clave = _config["Correo:Clave"];
        var remitente = _config["Correo:Remitente"] ?? usuario;

        using var mensaje = new MailMessage();
        mensaje.From = new MailAddress(remitente!, "Otium");
        mensaje.To.Add(destinatario);
        mensaje.Subject = asunto;
        mensaje.Body = cuerpoHtml;
        mensaje.IsBodyHtml = true;

        using var cliente = new SmtpClient(host, puerto)
        {
            Credentials = new NetworkCredential(usuario, clave),
            EnableSsl = true
        };

        try
        {
            await cliente.SendMailAsync(mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo a {Destinatario}", destinatario);
            throw;
        }
    }
}
