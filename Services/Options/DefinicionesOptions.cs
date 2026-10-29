namespace Otium.Services.Options;

public sealed class DefinicionesOptions
{
    public const string Seccion = "Definiciones";
    public const string EndpointPorDefecto = "https://api.z.ai/api/paas/v4/";
    public const string ModeloPorDefecto = "glm-5.3-flash";

    public string ApiKey { get; set; } = "";

    public string Endpoint { get; set; } = EndpointPorDefecto;

    public string Modelo { get; set; } = ModeloPorDefecto;
}
