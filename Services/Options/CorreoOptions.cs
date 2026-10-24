using System.ComponentModel.DataAnnotations;

namespace Otium.Services.Options;

public sealed class CorreoOptions
{
    public const string Seccion = "Correo";

    [Required(ErrorMessage = "Falta configurar Correo:Host.")]
    public string Host { get; set; } = "";

    [Range(1, 65535, ErrorMessage = "Correo:Puerto debe estar entre 1 y 65535.")]
    public int Puerto { get; set; } = 587;

    [Required(ErrorMessage = "Falta configurar Correo:Usuario.")]
    public string Usuario { get; set; } = "";

    [Required(ErrorMessage = "Falta configurar Correo:Clave.")]
    public string Clave { get; set; } = "";

    [Required(ErrorMessage = "Falta configurar Correo:Remitente.")]
    public string Remitente { get; set; } = "";
}
