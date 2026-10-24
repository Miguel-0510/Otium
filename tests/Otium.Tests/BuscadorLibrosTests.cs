using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Otium.Services;

namespace Otium.Tests;

public sealed class BuscadorLibrosTests
{
    [Fact]
    public async Task Busca_en_open_library_y_cachea_los_resultados()
    {
        using var handler = new OpenLibraryFalso();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://openlibrary.org/")
        };
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var buscador = new BuscadorLibros(http, cache, NullLogger<BuscadorLibros>.Instance);

        var primera = await buscador.BuscarAsync("dune");
        var segunda = await buscador.BuscarAsync("dune");

        var libro = Assert.Single(primera);
        Assert.Equal("Dune", libro.Titulo);
        Assert.Equal("Frank Herbert", libro.Autor);
        Assert.Equal(1965, libro.Anio);
        Assert.Single(segunda);
        Assert.Equal(1, handler.Llamadas);
    }

    [Fact]
    public async Task Una_consulta_vacia_no_llama_a_la_api()
    {
        using var handler = new OpenLibraryFalso();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://openlibrary.org/")
        };
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var buscador = new BuscadorLibros(http, cache, NullLogger<BuscadorLibros>.Instance);

        Assert.Empty(await buscador.BuscarAsync("   "));
        Assert.Equal(0, handler.Llamadas);
    }

    private sealed class OpenLibraryFalso : HttpMessageHandler
    {
        public int Llamadas { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Llamadas++;

            const string json = """
                {
                  "docs": [
                    {
                      "key": "/works/OL1W",
                      "title": "Dune",
                      "author_name": ["Frank Herbert"],
                      "cover_i": 123,
                      "first_publish_year": 1965
                    }
                  ]
                }
                """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
