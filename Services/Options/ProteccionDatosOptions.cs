namespace Otium.Services.Options;

public sealed class ProteccionDatosOptions
{
    public const string Seccion = "DataProtection";

    public string? KeysPath { get; set; }

    public string ApplicationName { get; set; } = "Otium";
}
