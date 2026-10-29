using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Otium.Services.Options;

namespace Otium.Services;

public interface IDefinicionClient
{
    bool EstaConfigurado { get; }

    /// <summary>
    /// Define un texto (palabra o frase). Si envía una frase, el modelo extrae el término principal.
    /// El modo "incluirContextoLectura" pide además el matiz contextual del libro indicado.
    /// </summary>
    Task<ResultadoDefinicion?> DefinirAsync(string texto, string libroTitulo, bool incluirContextoLectura, CancellationToken cancellationToken = default);
}

/// <summary>Resultado del modelo: término analizado, definición global y contexto de lectura (puede quedar vacío).</summary>
public sealed record ResultadoDefinicion(string Termino, string Definicion, string ContextoLectura);

public class DefinicionesClient : IDefinicionClient
{
    private const int MaxIntentos = 2;
    private const int LimiteCuerpoLog = 4000;
    private const int MaxTokensSalida = 300;
    private const int LongitudMaximaTexto = 200;
    private static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan DuracionCacheDefinicion = TimeSpan.FromDays(7);

    private const string InstruccionesBase =
        "Eres un diccionario conciso en español para una app de lectura. " +
        "Si el texto dado es una sola palabra, el término es esa palabra; si es una frase u oración, " +
        "identifica la palabra clave o término principal que merece definición. " +
        "No añadas campos extra, introducciones, saludos ni texto de relleno.";

    private const string InstruccionesSinContexto =
        InstruccionesBase +
        " Responde ÚNICAMENTE con un objeto JSON válido, sin markdown y sin cercas de código, " +
        "con la forma exacta {\"termino\": \"...\", \"definicion\": \"...\"}. " +
        "\"termino\" es la palabra clave (una o dos palabras, en minúscula). " +
        "\"definicion\" es la acepción global más común del término: de 15 a 20 palabras como máximo, " +
        "súper directa al punto, sin introducciones, sin repetir la palabra y sin comillas dentro del valor.";

    private const string InstruccionesConContexto =
        InstruccionesBase +
        " Responde ÚNICAMENTE con un objeto JSON válido, sin markdown y sin cercas de código, " +
        "con la forma exacta {\"termino\": \"...\", \"definicion\": \"...\", \"contexto_lectura\": \"...\"}. " +
        "\"termino\" es la palabra clave (una o dos palabras, en minúscula). " +
        "\"definicion\" es la acepción global más común del término: de 15 a 20 palabras como máximo, " +
        "súper directa al punto, sin introducciones, sin repetir la palabra y sin comillas dentro del valor. " +
        "\"contexto_lectura\" es una frase de máximo 20 palabras que explica el matiz o uso relevante " +
        "del término en el libro indicado, SOLO si ese contexto aporta un significado situacional clave para la obra; " +
        "si la acepción general ya aplica igual o el contexto no aporta nada relevante, responde con cadena vacía.";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly DefinicionesOptions _opciones;
    private readonly ILogger<DefinicionesClient> _logger;
    private readonly CircuitBreaker _circuitBreaker = new();

    public DefinicionesClient(
        HttpClient http,
        IMemoryCache cache,
        IOptions<DefinicionesOptions> opciones,
        ILogger<DefinicionesClient> logger)
    {
        _http = http;
        _cache = cache;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_opciones.ApiKey);

    public async Task<ResultadoDefinicion?> DefinirAsync(
        string texto,
        string libroTitulo,
        bool incluirContextoLectura,
        CancellationToken cancellationToken = default)
    {
        if (!EstaConfigurado)
        {
            _logger.LogWarning("El servicio de definiciones no está configurado; no se puede definir '{Texto}'.", texto);
            return null;
        }

        var textoRecortado = RecortarTexto(texto);
        if (textoRecortado.Length == 0)
        {
            return null;
        }

        var modelo = (_opciones.Modelo ?? "").Trim();
        if (modelo.Length == 0)
        {
            _logger.LogError("Definiciones:Modelo está vacío; no se puede consultar el servicio.");
            return null;
        }

        var titulado = (libroTitulo ?? "").Trim();
        var modoConContexto = incluirContextoLectura && titulado.Length > 0;
        var claveCache = $"definicion:{textoRecortado.ToLowerInvariant()}:{(modoConContexto ? titulado.ToLowerInvariant() : "")}:{incluirContextoLectura}";
        if (_cache.TryGetValue(claveCache, out ResultadoDefinicion? cacheado) && cacheado is not null)
        {
            return cacheado;
        }

        var prompt = modoConContexto
            ? $"Texto o palabra: \"{textoRecortado}\"\nLibro de lectura: \"{titulado}\""
            : $"Texto o palabra: \"{textoRecortado}\"";

        var cuerpo = new
        {
            model = modelo,
            messages = new[]
            {
                new { role = "system", content = modoConContexto ? InstruccionesConContexto : InstruccionesSinContexto },
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            max_tokens = MaxTokensSalida
        };

        try
        {
            using var respuesta = await _circuitBreaker.EjecutarAsync(
                _ => EnviarConReintentosAsync(() => CrearSolicitud(cuerpo), cancellationToken),
                cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                await RegistrarErrorHttpAsync(respuesta, modelo, textoRecortado, cancellationToken);
                return null;
            }

            RespuestaChat? datos;
            try
            {
                datos = await respuesta.Content.ReadFromJsonAsync<RespuestaChat>(OpcionesJson, cancellationToken);
            }
            catch (JsonException ex)
            {
                var cuerpoRespuesta = await LeerCuerpoAsync(respuesta, cancellationToken);
                _logger.LogError(
                    ex,
                    "El servicio de definiciones devolvió HTTP 200 con JSON inválido al definir '{Texto}' con el modelo '{Modelo}'. " +
                    "ResponseBody: {ResponseBody}",
                    textoRecortado,
                    modelo,
                    Truncar(cuerpoRespuesta));
                return null;
            }

            var contenido = datos?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                var cuerpoRespuesta = await LeerCuerpoAsync(respuesta, cancellationToken);
                _logger.LogWarning(
                    "El servicio de definiciones respondió HTTP 200 sin texto utilizable al definir '{Texto}' con el modelo '{Modelo}'. " +
                    "Posible bloqueo de seguridad o respuesta vacía. ResponseBody: {ResponseBody}",
                    textoRecortado,
                    modelo,
                    Truncar(cuerpoRespuesta));
                return null;
            }

            var resultado = ExtraerResultado(contenido, textoRecortado, modelo);
            if (resultado is null)
            {
                return null;
            }

            _cache.Set(claveCache, resultado, DuracionCacheDefinicion);
            return resultado;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(
                ex,
                "La consulta al servicio de definiciones para '{Texto}' con el modelo '{Modelo}' superó el tiempo límite de {Segundos} s.",
                textoRecortado,
                modelo,
                _http.Timeout.TotalSeconds);
            return null;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(
                ex,
                "La consulta al servicio de definiciones para '{Texto}' con el modelo '{Modelo}' agotó los reintentos por tiempo de espera.",
                textoRecortado,
                modelo);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Error de red o TLS al contactar el servicio de definiciones para '{Texto}' con el modelo '{Modelo}'. " +
                "Endpoint: {Endpoint}; HTTP status: {Estado}.",
                textoRecortado,
                modelo,
                DefinicionesOptions.EndpointPorDefecto,
                ex.StatusCode?.ToString() ?? "sin respuesta");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo consultar el servicio de definiciones para '{Texto}': {Mensaje}",
                textoRecortado,
                ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error inesperado al consultar el servicio de definiciones para '{Texto}' con el modelo '{Modelo}'.",
                textoRecortado,
                modelo);
            return null;
        }
    }

    /// <summary>
    /// Extrae el JSON pedido al modelo y devuelve término, definición global y contexto de lectura.
    /// La composición del texto final (con "En el contexto de la lectura: ...") la hace GlosarioService.
    /// Devuelve null solo cuando no hay definición utilizable y registra el motivo.
    /// </summary>
    private ResultadoDefinicion? ExtraerResultado(string contenido, string textoRecortado, string modelo)
    {
        SalidaDefinicion? salida = null;
        try
        {
            var inicio = contenido.IndexOf('{');
            var fin = contenido.LastIndexOf('}');
            if (inicio >= 0 && fin > inicio)
            {
                salida = JsonSerializer.Deserialize<SalidaDefinicion>(
                    contenido[inicio..(fin + 1)],
                    OpcionesJson);
            }
        }
        catch (JsonException)
        {
            // El contenido entero se usará como texto limpio de respaldo.
        }

        var definicion = salida?.Definicion is not null
            && salida.Definicion.Trim().Length > 0
            ? salida.Definicion.Trim()
            : contenido
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

        if (string.IsNullOrWhiteSpace(definicion))
        {
            _logger.LogWarning(
                "El modelo '{Modelo}' no devolvió una definición utilizable para '{Texto}'.",
                modelo,
                textoRecortado);
            return null;
        }

        var termino = salida?.Termino?.Trim() ?? "";
        if (termino.Length == 0)
        {
            // Respaldo: usa la palabra principal del texto enviado.
            termino = textoRecortado.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        }

        if (termino.Length > 100)
        {
            termino = termino[..100];
        }

        return new ResultadoDefinicion(
            termino,
            definicion,
            (salida?.ContextoLectura?.Trim() ?? "").Trim());
    }

    /// <summary>Recorta el texto a 200 caracteres antes de enviarlo a la API para no desperdiciar tokens.</summary>
    private static string RecortarTexto(string? texto)
    {
        var limpio = (texto ?? "").Trim();
        return limpio.Length > LongitudMaximaTexto
            ? limpio[..LongitudMaximaTexto].Trim()
            : limpio;
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
            "El servicio de definiciones devolvió StatusCode={Estado} ({Motivo}) al definir '{Palabra}' con el modelo '{Modelo}'. " +
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
        401 => "API key ausente o inválida: verifica Definiciones__ApiKey/GLM_API_KEY/ZAI_API_KEY.",
        403 => "API key sin permisos o sin saldo para el modelo configurado.",
        404 => "Modelo o endpoint no encontrado: revisa Definiciones:Modelo (por ejemplo glm-5.3-flash) y Definiciones:Endpoint.",
        429 => "Cuota o límite de peticiones excedido en el servicio de definiciones.",
        >= 500 => "Error interno o indisponibilidad temporal del servicio de definiciones.",
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

    private HttpRequestMessage CrearSolicitud(object cuerpo)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(cuerpo)
        };
        solicitud.Headers.Add("Authorization", $"Bearer {_opciones.ApiKey.Trim()}");
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
                    "Timeout al contactar el servicio de definiciones (intento {Intento}/{Total}); se reintentará.",
                    intento,
                    MaxIntentos);
                await Task.Delay(EsperaEntreIntentos * intento, cancellationToken);
                continue;
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException("La consulta al servicio de definiciones superó el tiempo de espera.", ex);
            }
            catch (HttpRequestException ex) when (intento < MaxIntentos)
            {
                _logger.LogWarning(
                    ex,
                    "Error de red al contactar el servicio de definiciones (intento {Intento}/{Total}): {Mensaje}; se reintentará.",
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
                "El servicio de definiciones respondió HTTP {Estado} (intento {Intento}/{Total}); se reintentará.",
                (int)respuesta.StatusCode,
                intento,
                MaxIntentos);

            respuesta.Dispose();
            await Task.Delay(EsperaEntreIntentos * intento, cancellationToken);
        }

        throw new InvalidOperationException("No se pudo completar la consulta al servicio de definiciones.");
    }

    private static bool EsTransitorio(HttpStatusCode estado) =>
        estado == HttpStatusCode.RequestTimeout
        || estado == HttpStatusCode.TooManyRequests
        || (int)estado >= 500;
}

internal sealed class RespuestaChat
{
    [JsonPropertyName("choices")]
    public List<OpcionChat>? Choices { get; set; }
}

internal sealed class OpcionChat
{
    [JsonPropertyName("message")]
    public MensajeChat? Message { get; set; }
}

internal sealed class MensajeChat
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

internal sealed class SalidaDefinicion
{
    [JsonPropertyName("termino")]
    public string? Termino { get; set; }

    [JsonPropertyName("definicion")]
    public string? Definicion { get; set; }

    [JsonPropertyName("contexto_lectura")]
    public string? ContextoLectura { get; set; }
}
