using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Otium.Services;

public record ResultadoLibro(string IdExterno, string Titulo, string Autor, string PortadaUrl, int? Anio)
{
    public string Detalle => Anio is int a ? $"{Autor}, {a}" : Autor;
}

public interface IBuscadorLibros
{
    Task<List<ResultadoLibro>> BuscarAsync(string texto, CancellationToken cancellationToken = default);
}

public class BuscadorLibros : IBuscadorLibros
{
    private const int MaxIntentos = 3;
    private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromMilliseconds(200);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BuscadorLibros> _logger;
    private readonly CircuitBreaker _circuitBreaker;

    /// <summary>Constructor para pruebas: crea un circuito propio.</summary>
    public BuscadorLibros(HttpClient http, IMemoryCache cache, ILogger<BuscadorLibros> logger)
        : this(http, cache, logger, new CircuitBreaker())
    {
    }

    /// <summary>Constructor para DI: recibe el circuito compartido (singleton) del buscador.</summary>
    [ActivatorUtilitiesConstructor]
    public BuscadorLibros(
        HttpClient http,
        IMemoryCache cache,
        ILogger<BuscadorLibros> logger,
        [FromKeyedServices(ClavesCircuito.Buscador)] CircuitBreaker circuitBreaker)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
    }

    public async Task<List<ResultadoLibro>> BuscarAsync(string texto, CancellationToken cancellationToken = default)
    {
        var consulta = (texto ?? "").Trim();
        if (consulta.Length == 0)
        {
            return new();
        }

        var claveCache = $"openlibrary:{consulta.ToLowerInvariant()}";
        if (_cache.TryGetValue(claveCache, out List<ResultadoLibro>? guardados) && guardados is not null)
        {
            return guardados;
        }

        var url = "search.json?limit=8&fields=key,title,author_name,cover_i,first_publish_year&q="
                  + Uri.EscapeDataString(consulta);

        using var respuesta = await _circuitBreaker.EjecutarAsync(_ => EnviarConReintentosAsync(url, cancellationToken), cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Open Library respondió {Estado} al buscar {Consulta}.",
                (int)respuesta.StatusCode,
                consulta);

            throw new InvalidOperationException(
                "No pude consultar Open Library en este momento. Inténtalo más tarde.");
        }

        var datos = await respuesta.Content.ReadFromJsonAsync<RespuestaBusqueda>(cancellationToken);

        var resultados = datos?.Docs
            .Select(d => new ResultadoLibro(
                d.Key ?? "",
                d.Title ?? "",
                d.AuthorName is { Count: > 0 } ? string.Join(", ", d.AuthorName.Take(2)) : "Autor desconocido",
                d.CoverId is int id ? $"https://covers.openlibrary.org/b/id/{id}-M.jpg" : "",
                d.FirstPublishYear))
            .ToList() ?? new();

        _cache.Set(claveCache, resultados, DuracionCache);
        return resultados;
    }

    private async Task<HttpResponseMessage> EnviarConReintentosAsync(string url, CancellationToken cancellationToken)
    {
        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            HttpResponseMessage respuesta;
            try
            {
                respuesta = await _http.GetAsync(url, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (intento < MaxIntentos)
            {
                await Task.Delay(EsperaEntreIntentos * intento, cancellationToken);
                continue;
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException("La búsqueda en Open Library superó el tiempo de espera.", ex);
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

        throw new InvalidOperationException("No se pudo completar la búsqueda en Open Library.");
    }

    private static bool EsTransitorio(HttpStatusCode estado) =>
        estado == HttpStatusCode.RequestTimeout || (int)estado >= 500;
}

internal class RespuestaBusqueda
{
    [JsonPropertyName("docs")]
    public List<DocLibro> Docs { get; set; } = new();
}

internal class DocLibro
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("author_name")]
    public List<string>? AuthorName { get; set; }

    [JsonPropertyName("cover_i")]
    public int? CoverId { get; set; }

    [JsonPropertyName("first_publish_year")]
    public int? FirstPublishYear { get; set; }
}
