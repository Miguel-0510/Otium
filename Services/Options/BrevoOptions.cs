using System.ComponentModel.DataAnnotations;

namespace Otium.Services.Options;

public sealed class BrevoOptions
{
    public const string Seccion = "Brevo";

    public string ApiKey { get; set; } = "";

    [EmailAddress(ErrorMessage = "Brevo:SenderEmail debe ser un correo válido.")]
    public string SenderEmail { get; set; } = "otium.lectura@gmail.com";

    public string SenderName { get; set; } = "Otium";

    [Url(ErrorMessage = "Brevo:Endpoint debe ser una URL válida.")]
    public string Endpoint { get; set; } = "https://api.brevo.com/v3/smtp/email";
}
