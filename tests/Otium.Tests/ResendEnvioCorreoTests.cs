using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class ResendEnvioCorreoTests
{
    [Fact]
    public async Task Envia_por_resend_con_la_clave_en_la_cabecera_authorization()
    {
        using var handler = new ResendFalso(HttpStatusCode.OK, "{\"id\":\"correo-1\"}");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        var transporte = CrearTransporte(http, apiKey: "re_clave_de_prueba");

        var enviado = await transporte.EnviarAsync(
            "destino@example.com",
            "Asunto de prueba",
            "<p>Hola</p>");

        Assert.True(enviado);
        Assert.Equal("Bearer re_clave_de_prueba", handler.Autorizacion);
        Assert.Equal(HttpMethod.Post, handler.Metodo);
        Assert.Equal("https://api.resend.com/emails", handler.Uri);

        Assert.NotNull(handler.Cuerpo);
        using var documento = JsonDocument.Parse(handler.Cuerpo!);
        var raiz = documento.RootElement;
        Assert.Equal("Otium <no-reply@otium.test>", raiz.GetProperty("from").GetString());
        Assert.Equal("destino@example.com", raiz.GetProperty("to")[0].GetString());
        Assert.Equal("Asunto de prueba", raiz.GetProperty("subject").GetString());
        Assert.Equal("<p>Hola</p>", raiz.GetProperty("html").GetString());
    }

    [Fact]
    public async Task Sin_clave_no_llama_a_resend()
    {
        using var handler = new ResendFalso(HttpStatusCode.OK, "{}");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        var transporte = CrearTransporte(http, apiKey: "");

        Assert.False(transporte.EstaConfigurado);
        Assert.False(await transporte.EnviarAsync("destino@example.com", "Asunto", "<p>Hola</p>"));
        Assert.Equal(0, handler.Llamadas);
    }

    [Fact]
    public async Task Un_error_http_devuelve_false_sin_lanzar_excepcion()
    {
        using var handler = new ResendFalso(HttpStatusCode.UnprocessableEntity, "{\"message\":\"dominio no verificado\"}");
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        var transporte = CrearTransporte(http, apiKey: "re_clave");

        Assert.False(await transporte.EnviarAsync("destino@example.com", "Asunto", "<p>Hola</p>"));
    }

    [Fact]
    public async Task Un_error_de_red_devuelve_false_sin_lanzar_excepcion()
    {
        using var handler = new ResendFalso(new HttpRequestException("sin red"));
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        var transporte = CrearTransporte(http, apiKey: "re_clave");

        Assert.False(await transporte.EnviarAsync("destino@example.com", "Asunto", "<p>Hola</p>"));
    }

    private static ResendEnvioCorreo CrearTransporte(HttpClient http, string apiKey) =>
        new(
            http,
            Options.Create(new ResendOptions
            {
                ApiKey = apiKey,
                Remitente = "Otium <no-reply@otium.test>"
            }),
            Options.Create(new CorreoOptions()),
            NullLogger<ResendEnvioCorreo>.Instance);

    private sealed class ResendFalso : HttpMessageHandler
    {
        private readonly HttpStatusCode? _estado;
        private readonly string? _respuesta;
        private readonly Exception? _excepcion;

        public ResendFalso(HttpStatusCode estado, string respuesta)
        {
            _estado = estado;
            _respuesta = respuesta;
        }

        public ResendFalso(Exception excepcion)
        {
            _excepcion = excepcion;
        }

        public int Llamadas { get; private set; }

        public string? Autorizacion { get; private set; }

        public HttpMethod? Metodo { get; private set; }

        public string? Uri { get; private set; }

        public string? Cuerpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Llamadas++;
            Autorizacion = request.Headers.Authorization?.ToString();
            Metodo = request.Method;
            Uri = request.RequestUri?.ToString();

            if (request.Content is not null)
            {
                Cuerpo = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            if (_excepcion is not null)
            {
                throw _excepcion;
            }

            return new HttpResponseMessage(_estado!.Value)
            {
                Content = new StringContent(_respuesta ?? "", Encoding.UTF8, "application/json")
            };
        }
    }
}
