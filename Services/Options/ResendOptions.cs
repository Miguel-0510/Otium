namespace Otium.Services.Options;

public sealed class ResendOptions
{
    public const string Seccion = "Resend";

    public string ApiKey { get; set; } = "";

    public string Remitente { get; set; } = "";

    public string UrlApi { get; set; } = "https://api.resend.com/";
}
