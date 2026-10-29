using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class DefinicionesClientTests
{
    [Fact]
    public async Task Envia_la_clave_en_cabecera_y_parsea_la_definicion_json()
    {
        const string definicion = "Definición de prueba.";
        using var handler = new ServicioDefinicionesFalso(
            """{"definicion": "Definición de prueba.", "contexto_lectura": ""}""");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefinicionesOptions.EndpointPorDefecto)
        };

        var cliente = new DefinicionesClient(
            http,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new DefinicionesOptions { ApiKey = "clave-de-prueba", Modelo = "glm-test" }),
            NullLogger<DefinicionesClient>.Instance);

        var definicionObtenida = await cliente.DefinirAsync("efímero", "Libro de prueba");

        Assert.Equal(definicion, definicionObtenida);
        Assert.Equal("Bearer clave-de-prueba", handler.Clave);
        Assert.EndsWith("chat/completions", handler.Ruta, StringComparison.Ordinal);

        using var documento = System.Text.Json.JsonDocument.Parse(handler.Cuerpo!);
        var raiz = documento.RootElement;
        Assert.Equal("glm-test", raiz.GetProperty("model").GetString());
        Assert.Contains(
            "efímero",
            raiz.GetProperty("messages")[1].GetProperty("content").GetString()!,
            StringComparison.Ordinal);
        Assert.Contains(
            "Libro de prueba",
            raiz.GetProperty("messages")[1].GetProperty("content").GetString()!,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sin_clave_no_llama_al_servicio()
    {
        using var handler = new ServicioDefinicionesFalso(
            """{"definicion": "No debe usarse.", "contexto_lectura": ""}""");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefinicionesOptions.EndpointPorDefecto)
        };

        var cliente = new DefinicionesClient(
            http,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new DefinicionesOptions { ApiKey = "", Modelo = "glm-test" }),
            NullLogger<DefinicionesClient>.Instance);

        Assert.False(cliente.EstaConfigurado);
        Assert.Null(await cliente.DefinirAsync("efímero", ""));
        Assert.Equal(0, handler.Llamadas);
    }

    [Fact]
    public async Task Separa_definicion_global_del_contexto_de_lectura()
    {
        const string definicion = "Que dura poco tiempo.";
        const string contexto = "En la novela suele aplicarse a sentimientos pasajeros.";

        using var handler = new ServicioDefinicionesFalso(
            """{"definicion": "Que dura poco tiempo.", "contexto_lectura": "En la novela suele aplicarse a sentimientos pasajeros."}""");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefinicionesOptions.EndpointPorDefecto)
        };

        var cliente = new DefinicionesClient(
            http,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new DefinicionesOptions { ApiKey = "clave-de-prueba", Modelo = "glm-test" }),
            NullLogger<DefinicionesClient>.Instance);

        var definicionObtenida = await cliente.DefinirAsync("efímero", "Libro de prueba");

        Assert.NotNull(definicionObtenida);
        Assert.StartsWith(definicion, definicionObtenida!, StringComparison.Ordinal);
        Assert.Contains("\n\nEn el contexto de la lectura: " + contexto, definicionObtenida!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Registra_estado_y_cuerpo_cuando_el_servicio_devuelve_error()
    {
        const string cuerpoError = """{"error":{"code":403,"message":"API key not valid"}}""";
        using var handler = new ServicioErrorFalso(HttpStatusCode.Forbidden, cuerpoError);
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefinicionesOptions.EndpointPorDefecto)
        };

        const string clave = "clave-secreta-de-prueba";
        var logger = new LoggerCapturador<DefinicionesClient>();

        var cliente = new DefinicionesClient(
            http,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new DefinicionesOptions { ApiKey = clave, Modelo = "glm-test" }),
            logger);

        var definicion = await cliente.DefinirAsync("efímero", "");

        Assert.Null(definicion);
        Assert.Contains("403", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("API key not valid", logger.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(clave, logger.Texto, StringComparison.Ordinal);
    }

    private sealed class ServicioErrorFalso : HttpMessageHandler
    {
        private readonly HttpStatusCode _estado;
        private readonly string _cuerpo;

        public ServicioErrorFalso(HttpStatusCode estado, string cuerpo)
        {
            _estado = estado;
            _cuerpo = cuerpo;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_estado)
            {
                Content = new StringContent(_cuerpo, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ServicioDefinicionesFalso : HttpMessageHandler
    {
        private readonly string _salidaJson;

        public ServicioDefinicionesFalso(string salidaJson)
        {
            _salidaJson = salidaJson;
        }

        public string? Clave { get; private set; }

        public string? Ruta { get; private set; }

        public string? Cuerpo { get; private set; }

        public int Llamadas { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Llamadas++;
            Clave = request.Headers.TryGetValues("Authorization", out var valores)
                ? valores.FirstOrDefault()
                : null;
            Ruta = request.RequestUri?.ToString();

            if (request.Content is not null)
            {
                Cuerpo = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var escapada = System.Text.Json.JsonSerializer.Serialize(_salidaJson);
            var json = $$"""
                {
                  "choices": [
                    {
                      "message": {
                        "content": {{escapada}}
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
