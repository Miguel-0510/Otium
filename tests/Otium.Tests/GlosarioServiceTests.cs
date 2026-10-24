using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Otium.Data;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class GlosarioServiceTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public GlosarioServiceTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Gemini_se_consulta_con_cabecera_y_la_palabra_se_guarda()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var fabricaDb = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var limites = scope.ServiceProvider.GetRequiredService<ILimiteIntentosService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlosarioService>>();

        var usuario = await registro.RegistrarAsync($"glosario-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        using var handler = new GeminiFalso();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        var servicio = new GlosarioService(
            fabricaDb,
            http,
            Options.Create(new GeminiOptions { ApiKey = "clave-de-prueba", Modelo = "gemini-test" }),
            limites,
            logger);

        var resultado = await servicio.BuscarYGuardarAsync(usuario.Usuario!.Id, "efímero", "Libro de prueba");

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.NotNull(resultado.Palabra);
        Assert.Equal("Definición de prueba.", resultado.Palabra!.Definicion);
        Assert.Equal("clave-de-prueba", handler.ClaveRecibida);

        var palabras = await servicio.ObtenerDeUsuarioAsync(usuario.Usuario.Id);
        var palabra = Assert.Single(palabras);
        Assert.Equal("efímero", palabra.Palabra);

        Assert.True(await servicio.EliminarAsync(usuario.Usuario.Id, palabra.Id));
        Assert.Empty(await servicio.ObtenerDeUsuarioAsync(usuario.Usuario.Id));
    }

    private sealed class GeminiFalso : HttpMessageHandler
    {
        public string? ClaveRecibida { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ClaveRecibida = request.Headers.TryGetValues("x-goog-api-key", out var valores)
                ? valores.FirstOrDefault()
                : null;

            const string json = """
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          { "text": "Definición de prueba." }
                        ]
                      }
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
