using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Otium.Services;

public record ResultadoLibro(string IdExterno, string Titulo, string Autor, string PortadaUrl, int? Anio)
{
    public string Detalle => Anio is int a ? $"{Autor}, {a}" : Autor;
}

public class BuscadorLibros
{
    private readonly HttpClient _http;

    public BuscadorLibros(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ResultadoLibro>> BuscarAsync(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return new();

        var url = "search.json?limit=8&fields=key,title,author_name,cover_i,first_publish_year&q="
                  + Uri.EscapeDataString(texto.Trim());

        var respuesta = await _http.GetFromJsonAsync<RespuestaBusqueda>(url);

        return respuesta?.Docs
            .Select(d => new ResultadoLibro(
                d.Key ?? "",
                d.Title ?? "",
                d.AuthorName is { Count: > 0 } ? string.Join(", ", d.AuthorName.Take(2)) : "Autor desconocido",
                d.CoverId is int id ? $"https://covers.openlibrary.org/b/id/{id}-M.jpg" : "",
                d.FirstPublishYear))
            .ToList() ?? new();
    }
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
