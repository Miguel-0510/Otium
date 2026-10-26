using System.ComponentModel.DataAnnotations;

namespace Otium.Services.Options;

public sealed class SmtpOptions
{
    public const string Seccion = "Smtp";

    public string Host { get; set; } = "smtp-relay.brevo.com";

    [Range(1, 65535, ErrorMessage = "Smtp:Port debe estar entre 1 y 65535.")]
    public int Port { get; set; } = 587;

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    [EmailAddress(ErrorMessage = "Smtp:SenderEmail debe ser un correo válido.")]
    public string SenderEmail { get; set; } = "";

    public string SenderName { get; set; } = "Otium";
}
