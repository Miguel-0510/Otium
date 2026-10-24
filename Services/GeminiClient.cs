using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Otium.Services.Options;

namespace Otium.Services;

public interface IGeminiClient
{
    bool EstaConfigurado { get; }

    Task<string?> DefinirAsync(string palabra, string libroTitulo, CancellationToken cancellationToken = default);
}

public class GeminiClient : IGeminiClient
{
    private const int MaxIntentos = 3;
    private static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromMilliseconds(300);

    private readonly HttpClient _http;
    private readonly GeminiOptions _opciones;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient http, IOptions<GeminiOptions> opciones, ILogger<GeminiClient> logger)
    {
        _http = http;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_opciones.ApiKey);

    public async Task<string?> DefinirAsync(string palabra, string libroTitulo, CancellationToken cancellationToken = default)
    {
        if (!EstaConfigurado)
        {
            return null;
        }

        var texto = (palabra ?? "").Trim();
        if (texto.Length == 0)
        {
            return null;
        }

        var contexto = string.IsNullOrWhiteSpace(libroTitulo) ? "" : $" en el contexto del libro \"{libroTitulo}\"";
        var prompt = $"Da una definicion breve (maximo dos frases) y clara en espanol de la palabra \"{texto}\"{contexto}. " +
                     "Si tiene varios significados, elige el mas comun o el que mejor encaje con el contexto dado. " +
                     "Responde solo con la definicion, sin repetir la palabra, sin comillas y sin comentarios adicionales.";

        var url = $"v1beta/models/{_opciones.Modelo}:generateContent";
        var cuerpo = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { maxOutputTokens = 150, temperature = 0.2 }
        };

        using var respuesta = await EnviarConReintentosAsync(() => CrearSolicitud(url, cuerpo), cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Gemini respondió {Estado} al definir una palabra. Detalle: {Detalle}",
                (int)respuesta.StatusCode,
                detalle);

            return null;
        }

        var datos = await respuesta.Content.ReadFromJsonAsync<RespuestaGemini>(cancellationToken);
        return datos?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();
    }

    private HttpRequestMessage CrearSolicitud(string url, object cuerpo)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(cuerpo)
        };
        solicitud.Headers.Add("x-goog-api-key", _opciones.ApiKey);
        return solicitud;
    }

    private async Task<HttpResponseMessage> EnviarConReintentosAsync(
        Func<HttpRequestMessage> crearSolicitud,
        CancellationToken cancellationToken)
    {
        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            using var solicitud = crearSolicitud();

            HttpResponseMessage respuesta;
            try
            {
                respuesta = await _http.SendAsync(solicitud, cancellationToken);
            }
            catch (HttpRequestException) when (intento < MaxIntentos)
            {
                await Task.Delay(EsperaEntreIntentos * intento, cancellationToken);
                continue;
            }

            if (respuesta.IsSuccessStatusCode || intento == MaxIntentos || !EsTransitorio(respuesta.StatusCode))
            {
                return respuesta;
            }

            respuesta.Dispose();
            await Task.Delay(EsperaEntreIntentos * intento, cancellationToken);
        }

        throw new InvalidOperationException("No se pudo completar la consulta a Gemini.");
    }

    private static bool EsTransitorio(HttpStatusCode estado) =>
        estado == HttpStatusCode.RequestTimeout || (int)estado >= 500;
}

internal class RespuestaGemini
{
    [JsonPropertyName("candidates")]
    public List<CandidatoGemini>? Candidates { get; set; }
}

internal class CandidatoGemini
{
    [JsonPropertyName("content")]
    public ContenidoGemini? Content { get; set; }
}

internal class ContenidoGemini
{
    [JsonPropertyName("parts")]
    public List<ParteGemini>? Parts { get; set; }
}

internal class ParteGemini
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
