using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class DefinicionesClientTests
{
    private static DefinicionesOptions Opciones() =>
        new() { ApiKey = "clave-de-prueba", Modelo = "glm-test" };

    private static DefinicionesClient Cliente(HttpClient http, ILogger<DefinicionesClient>? logger = null) =>
        new(
            http,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(Opciones()),
            logger ?? NullLogger<DefinicionesClient>.Instance);

    [Fact]
    public async Task Envia_la_clave_en_cabecera_y_parsea_termino_y_definicion_json()
    {
        using var handler = new ServicioDefinicionesFalso(
            """{"termino": "efímero", "definicion": "Que dura poco tiempo.", "contexto_lectura": ""}""");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefinicionesOptions.EndpointPorDefecto)
        };

        var resultado = await Cliente(http).DefinirAsync("efímero", "", incluirContextoLectura: false);

        Assert.NotNull(resultado);
        Assert.Equal("efímero", resultado.Termino);
        Assert.Equal("Que dura poco tiempo.", resultado.Definicion);
        Assert.Equal("", resultado.ContextoLectura);
        Assert.Equal("Bearer clave-de-prueba", handler.Clave);
        Assert.EndsWith("chat/completions", handler.Ruta, StringComparison.Ordinal);

        using var documento = System.Text.Json.JsonDocument.Parse(handler.Cuerpo!);
        var raiz = documento.RootElement;
        Assert.Equal("glm-test", raiz.GetProperty("model").GetString());
        var sistema = raiz.GetProperty("messages")[0].GetProperty("content").GetString()!;
        var usuario = raiz.GetProperty("messages")[1].GetProperty("content").GetString()!;
        Assert.Contains("efímero", usuario, StringComparison.Ordinal);
        // MODO 1: no solicita contexto de lectura.
        Assert.DoesNotContain("contexto_lectura", sistema, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Con_contexto_pide_el_matiz_del_libro()
    {
        using var handler = new ServicioDefinicionesFalso(
            """{"termino": "resiliencia", "definicion": "Capacidad de adaptarse ante la adversidad.", "contexto_lectura": ""}""");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefinicionesOptions.EndpointPorDefecto)
        };

        var resultado = await Cliente(http)
            .DefinirAsync("demostró una resiliencia formidable", "Libro de prueba", incluirContextoLectura: true);

        Assert.NotNull(resultado);

        using var documento = System.Text.Json.JsonDocument.Parse(handler.Cuerpo!);
        var mensajes = documento.RootElement.GetProperty("messages");
        var sistema = mensajes[0].GetProperty("content").GetString()!;
        var usuario = mensajes[1].GetProperty("content").GetString()!;
        Assert.Contains("contexto_lectura", sistema, StringComparison.Ordinal);
        Assert.Contains("Libro de prueba", usuario, StringComparison.Ordinal);
        Assert.Contains("resiliencia", usuario, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Recorta_el_texto_a_200_caracteres_antes_de_enviarlo()
    {
        using var handler = new ServicioDefinicionesFalso(
            """{"termino": "palabra", "definicion": "Definición de prueba.", "contexto_lectura": ""}""");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefinicionesOptions.EndpointPorDefecto)
        };

        var texto = new string('a', 150) + new string('b', 150);
        var resultado = await Cliente(http).DefinirAsync(texto, "", incluirContextoLectura: false);

        Assert.NotNull(resultado);
        using var documento = System.Text.Json.JsonDocument.Parse(handler.Cuerpo!);
        var usuario = documento.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!;
        Assert.Contains(new string('a', 150), usuario, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('b', 51), usuario, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sin_clave_no_llama_al_servicio()
    {
        using var handler = new ServicioDefinicionesFalso(
            """{"termino": "efímero", "definicion": "No debe usarse.", "contexto_lectura": ""}""");
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
        Assert.Null(await cliente.DefinirAsync("efímero", "", incluirContextoLectura: false));
        Assert.Equal(0, handler.Llamadas);
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

        var resultado = await cliente.DefinirAsync("efímero", "", incluirContextoLectura: false);

        Assert.Null(resultado);
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
