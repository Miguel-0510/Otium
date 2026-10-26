using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
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
    private const int LimiteCuerpoLog = 4000;
    private static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan DuracionCacheDefinicion = TimeSpan.FromDays(7);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly GeminiOptions _opciones;
    private readonly ILogger<GeminiClient> _logger;
    private readonly CircuitBreaker _circuitBreaker = new();

    public GeminiClient(HttpClient http, IMemoryCache cache, IOptions<GeminiOptions> opciones, ILogger<GeminiClient> logger)
    {
        _http = http;
        _cache = cache;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_opciones.ApiKey);

    public async Task<string?> DefinirAsync(string palabra, string libroTitulo, CancellationToken cancellationToken = default)
    {
        if (!EstaConfigurado)
        {
            _logger.LogWarning("Gemini no está configurado; no se puede definir '{Palabra}'.", palabra);
            return null;
        }

        var texto = (palabra ?? "").Trim();
        if (texto.Length == 0)
        {
            return null;
        }

        var modelo = (_opciones.Modelo ?? "").Trim();
        if (modelo.Length == 0)
        {
            _logger.LogError("Gemini:Modelo está vacío; no se puede consultar el servicio.");
            return null;
        }

        var claveCache = $"gemini:{texto.ToLowerInvariant()}:{(libroTitulo ?? "").ToLowerInvariant()}";
        if (_cache.TryGetValue(claveCache, out string? definicionCacheada) && definicionCacheada is not null)
        {
            return definicionCacheada;
        }

        var contexto = string.IsNullOrWhiteSpace(libroTitulo) ? "" : $" en el contexto del libro \"{libroTitulo}\"";
        var prompt = $"Da una definicion breve (maximo dos frases) y clara en espanol de la palabra \"{texto}\"{contexto}. " +
                     "Si tiene varios significados, elige el mas comun o el que mejor encaje con el contexto dado. " +
                     "Responde solo con la definicion, sin repetir la palabra, sin comillas y sin comentarios adicionales.";

        var url = $"v1beta/models/{Uri.EscapeDataString(modelo)}:generateContent";
        var cuerpo = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { maxOutputTokens = 150, temperature = 0.2 }
        };

        try
        {
            using var respuesta = await _circuitBreaker.EjecutarAsync(
                _ => EnviarConReintentosAsync(() => CrearSolicitud(url, cuerpo), cancellationToken),
                cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                await RegistrarErrorHttpAsync(respuesta, modelo, texto, cancellationToken);
                return null;
            }

            RespuestaGemini? datos;
            try
            {
                datos = await respuesta.Content.ReadFromJsonAsync<RespuestaGemini>(cancellationToken);
            }
            catch (JsonException ex)
            {
                var cuerpoRespuesta = await LeerCuerpoAsync(respuesta, cancellationToken);
                _logger.LogError(
                    ex,
                    "Gemini devolvió HTTP 200 con JSON inválido al definir '{Palabra}' con el modelo '{Modelo}'. " +
                    "ResponseBody: {ResponseBody}",
                    texto,
                    modelo,
                    Truncar(cuerpoRespuesta));
                return null;
            }

            var definicion = datos?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(definicion))
            {
                var cuerpoRespuesta = await LeerCuerpoAsync(respuesta, cancellationToken);
                _logger.LogWarning(
                    "Gemini respondió HTTP 200 sin texto utilizable al definir '{Palabra}' con el modelo '{Modelo}'. " +
                    "Posible bloqueo de seguridad o respuesta vacía. ResponseBody: {ResponseBody}",
                    texto,
                    modelo,
                    Truncar(cuerpoRespuesta));
                return null;
            }

            _cache.Set(claveCache, definicion, DuracionCacheDefinicion);
            return definicion;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(
                ex,
                "La consulta a Gemini para '{Palabra}' con el modelo '{Modelo}' superó el tiempo límite de {Segundos} s.",
                texto,
                modelo,
                _http.Timeout.TotalSeconds);
            return null;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(
                ex,
                "La consulta a Gemini para '{Palabra}' con el modelo '{Modelo}' agotó los reintentos por tiempo de espera.",
                texto,
                modelo);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Error de red o TLS al contactar Gemini para '{Palabra}' con el modelo '{Modelo}'. " +
                "Endpoint: https://generativelanguage.googleapis.com/{Url}; HTTP status: {Estado}.",
                texto,
                modelo,
                url,
                ex.StatusCode?.ToString() ?? "sin respuesta");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo consultar Gemini para '{Palabra}': {Mensaje}",
                texto,
                ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error inesperado al consultar Gemini para '{Palabra}' con el modelo '{Modelo}'.",
                texto,
                modelo);
            return null;
        }
    }

    private async Task RegistrarErrorHttpAsync(
        HttpResponseMessage respuesta,
        string modelo,
        string palabra,
        CancellationToken cancellationToken)
    {
        var cuerpo = await LeerCuerpoAsync(respuesta, cancellationToken);
        var estado = (int)respuesta.StatusCode;
        var motivo = string.IsNullOrWhiteSpace(respuesta.ReasonPhrase)
            ? "sin motivo"
            : respuesta.ReasonPhrase;

        _logger.LogWarning(
            "Gemini devolvió StatusCode={Estado} ({Motivo}) al definir '{Palabra}' con el modelo '{Modelo}'. " +
            "{Pista} ResponseBody: {ResponseBody}",
            estado,
            motivo,
            palabra,
            modelo,
            ObtenerPista(estado),
            Truncar(cuerpo));
    }

    private static string ObtenerPista(int estado) => estado switch
    {
        400 => "Petición inválida: revisa el formato del prompt o los parámetros del modelo.",
        401 => "API key ausente o inválida: verifica Gemini__ApiKey/GEMINI_API_KEY.",
        403 => "API key sin permisos o API de Gemini deshabilitada para el proyecto.",
        404 => "Modelo no encontrado: revisa Gemini:Modelo (por ejemplo gemini-flash-lite-latest).",
        429 => "Cuota o límite de peticiones excedido en Gemini.",
        >= 500 => "Error interno o indisponibilidad temporal de Gemini.",
        _ => "Revisa la clave, el modelo y la cuota de la API."
    };

    private static async Task<string> LeerCuerpoAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        try
        {
            return await respuesta.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"(no se pudo leer el cuerpo de la respuesta: {ex.Message})";
        }
    }

    private static string Truncar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "(cuerpo vacío)";
        }

        return texto.Length <= LimiteCuerpoLog
            ? texto
            : $"{texto[..LimiteCuerpoLog]}... (truncado; {texto.Length} caracteres en total)";
    }

    private HttpRequestMessage CrearSolicitud(string url, object cuerpo)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(cuerpo)
        };
        solicitud.Headers.Add("x-goog-api-key", _opciones.ApiKey.Trim());
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex) when (intento < MaxIntentos)
            {
                _logger.LogWarning(
                    ex,
                    "Timeout al contactar Gemini (intento {Intento}/{Total}); se reintentará.",
                    intento,
                    MaxIntentos);
                await Task.Delay(EsperaEntreIntentos * intento, cancellationToken);
                continue;
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException("La consulta a Gemini superó el tiempo de espera.", ex);
            }
            catch (HttpRequestException ex) when (intento < MaxIntentos)
            {
                _logger.LogWarning(
                    ex,
                    "Error de red al contactar Gemini (intento {Intento}/{Total}): {Mensaje}; se reintentará.",
                    intento,
                    MaxIntentos,
                    ex.Message);
                await Task.Delay(EsperaEntreIntentos * intento, cancellationToken);
                continue;
            }

            if (respuesta.IsSuccessStatusCode || intento == MaxIntentos || !EsTransitorio(respuesta.StatusCode))
            {
                return respuesta;
            }

            _logger.LogWarning(
                "Gemini respondió HTTP {Estado} (intento {Intento}/{Total}); se reintentará.",
                (int)respuesta.StatusCode,
                intento,
                MaxIntentos);

            respuesta.Dispose();
            await Task.Delay(EsperaEntreIntentos * intento, cancellationToken);
        }

        throw new InvalidOperationException("No se pudo completar la consulta a Gemini.");
    }

    private static bool EsTransitorio(HttpStatusCode estado) =>
        estado == HttpStatusCode.RequestTimeout
        || estado == HttpStatusCode.TooManyRequests
        || (int)estado >= 500;
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
