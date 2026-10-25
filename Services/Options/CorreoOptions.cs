using System.ComponentModel.DataAnnotations;

namespace Otium.Services.Options;

public sealed class CorreoOptions
{
    public const string Seccion = "Correo";

    public string Host { get; set; } = "";

    [Range(1, 65535, ErrorMessage = "Correo:Puerto debe estar entre 1 y 65535.")]
    public int Puerto { get; set; } = 587;

    public string Usuario { get; set; } = "";

    public string Clave { get; set; } = "";

    public string Remitente { get; set; } = "";
}
