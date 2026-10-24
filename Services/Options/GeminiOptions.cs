namespace Otium.Services.Options;

public sealed class GeminiOptions
{
    public const string Seccion = "Gemini";

    public string ApiKey { get; set; } = "";

    public string Modelo { get; set; } = "gemini-flash-lite-latest";
}
