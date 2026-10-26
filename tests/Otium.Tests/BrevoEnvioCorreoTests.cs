using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class BrevoEnvioCorreoTests
{
    [Fact]
    public void Los_valores_por_defecto_apuntan_a_la_api_de_brevo()
    {
        var opciones = new BrevoOptions();

        Assert.Equal("https://api.brevo.com/v3/smtp/email", opciones.Endpoint);
        Assert.Equal("Otium", opciones.SenderName);
        Assert.Equal("otium.lectura@gmail.com", opciones.SenderEmail);
    }

    [Fact]
    public async Task Sin_configuracion_no_intenta_enviar()
    {
        var handler = new HandlerPrueba(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var opciones = Options.Create(new BrevoOptions
        {
            ApiKey = "",
            SenderEmail = ""
        });

        var transporte = new BrevoEnvioCorreo(
            new HttpClient(handler),
            opciones,
            NullLogger<BrevoEnvioCorreo>.Instance);

        Assert.False(transporte.EstaConfigurado);
        Assert.False(await transporte.EnviarAsync("destino@example.com", "Asunto", "<p>Hola</p>"));
        Assert.Null(handler.Peticion);
    }

    [Fact]
    public async Task Envia_post_con_api_key_y_cuerpo_json()
    {
        var handler = new HandlerPrueba(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                "{\"messageId\":\"abc-123\"}",
                Encoding.UTF8,
                "application/json")
        });

        var opciones = Options.Create(new BrevoOptions
        {
            ApiKey = "xkeysib-prueba",
            SenderEmail = "otium.lectura@gmail.com",
            SenderName = "Otium"
        });

        var transporte = new BrevoEnvioCorreo(
            new HttpClient(handler),
            opciones,
            NullLogger<BrevoEnvioCorreo>.Instance);

        var enviado = await transporte.EnviarAsync(
            "destino@example.com",
            "Asunto",
            "<p>Hola</p>");

        Assert.True(enviado);
        Assert.Equal(HttpMethod.Post, handler.Peticion!.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.Peticion.RequestUri!.ToString());
        Assert.Equal("xkeysib-prueba", handler.ApiKey);

        using var documento = JsonDocument.Parse(handler.Cuerpo!);
        var raiz = documento.RootElement;

        Assert.Equal("otium.lectura@gmail.com", raiz.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("Otium", raiz.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("destino@example.com", raiz.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Asunto", raiz.GetProperty("subject").GetString());
        Assert.Equal("<p>Hola</p>", raiz.GetProperty("htmlContent").GetString());
    }

    [Fact]
    public async Task Registra_codigo_y_mensaje_cuando_brevo_rechaza()
    {
        var handler = new HandlerPrueba(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                "{\"code\":\"unauthorized\",\"message\":\"Key not found\"}",
                Encoding.UTF8,
                "application/json")
        });

        var logger = new LoggerCapturador<BrevoEnvioCorreo>();
        var opciones = Options.Create(new BrevoOptions
        {
            ApiKey = "xkeysib-prueba",
            SenderEmail = "otium.lectura@gmail.com"
        });

        var transporte = new BrevoEnvioCorreo(new HttpClient(handler), opciones, logger);

        var enviado = await transporte.EnviarAsync(
            "destino@example.com",
            "Asunto",
            "<p>Hola</p>");

        Assert.False(enviado);
        Assert.Contains("401", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("Key not found", logger.Texto, StringComparison.Ordinal);
        Assert.Contains("API key", logger.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain("xkeysib-prueba", logger.Texto, StringComparison.Ordinal);
    }

    private sealed class HandlerPrueba : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respuesta;

        public HandlerPrueba(Func<HttpRequestMessage, HttpResponseMessage> respuesta)
        {
            _respuesta = respuesta;
        }

        public HttpRequestMessage? Peticion { get; private set; }

        public string? ApiKey { get; private set; }

        public string? Cuerpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Peticion = request;
            ApiKey = request.Headers.TryGetValues("api-key", out var valores)
                ? valores.FirstOrDefault()
                : null;
            Cuerpo = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _respuesta(request);
        }
    }
}
