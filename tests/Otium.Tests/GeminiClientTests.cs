using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class GeminiClientTests
{
    [Fact]
    public async Task Envia_la_clave_en_cabecera_y_parsea_la_definicion()
    {
        using var handler = new GeminiFalso("Definición de prueba.");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        var cliente = new GeminiClient(
            http,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new GeminiOptions { ApiKey = "clave-de-prueba", Modelo = "gemini-test" }),
            NullLogger<GeminiClient>.Instance);

        var definicion = await cliente.DefinirAsync("efímero", "Libro de prueba");

        Assert.Equal("Definición de prueba.", definicion);
        Assert.Equal("clave-de-prueba", handler.Clave);

        using var documento = System.Text.Json.JsonDocument.Parse(handler.Prompt!);
        var prompt = documento.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        Assert.Contains("efímero", prompt!);
        Assert.Contains("Libro de prueba", prompt!);
    }

    [Fact]
    public async Task Sin_clave_no_llama_al_servicio()
    {
        using var handler = new GeminiFalso("No debe usarse.");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        var cliente = new GeminiClient(
            http,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new GeminiOptions { ApiKey = "", Modelo = "gemini-test" }),
            NullLogger<GeminiClient>.Instance);

        Assert.False(cliente.EstaConfigurado);
        Assert.Null(await cliente.DefinirAsync("efímero", ""));
        Assert.Equal(0, handler.Llamadas);
    }

    private sealed class GeminiFalso : HttpMessageHandler
    {
        private readonly string _definicion;

        public GeminiFalso(string definicion)
        {
            _definicion = definicion;
        }

        public string? Clave { get; private set; }

        public string? Prompt { get; private set; }

        public int Llamadas { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Llamadas++;
            Clave = request.Headers.TryGetValues("x-goog-api-key", out var valores)
                ? valores.FirstOrDefault()
                : null;

            if (request.Content is not null)
            {
                Prompt = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var json = $$"""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          { "text": "{{_definicion}}" }
                        ]
                      }
                    }
                  ]
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
