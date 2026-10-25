using System.Net;
using Otium.Tests;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class AccesibilidadTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public AccesibilidadTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_pagina_de_error_tiene_contenido_en_espanol()
    {
        var client = _fabrica.CreateClient();
        var respuesta = await client.GetAsync("/Error");
        respuesta.EnsureSuccessStatusCode();

        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("Ocurrió un error", html);
        Assert.Contains("ID de solicitud", html);
        Assert.DoesNotContain("An error occurred", html);
        Assert.DoesNotContain("Request ID", html);
    }

    [Fact]
    public async Task La_pagina_de_acceso_denegado_tiene_contenido_en_espanol()
    {
        var client = _fabrica.CreateClient();
        var respuesta = await client.GetAsync("/Account/AccessDenied");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("Acceso denegado", html);
        Assert.Contains("No tienes acceso a este recurso", html);
        Assert.DoesNotContain("Access denied", html);
        Assert.DoesNotContain("You do not have access", html);
    }

    [Fact]
    public async Task La_pagina_de_usuario_invalido_tiene_contenido_en_espanol()
    {
        var client = _fabrica.CreateClient();
        var respuesta = await client.GetAsync("/Account/InvalidUser");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("Usuario inválido", html);
        Assert.DoesNotContain("Invalid user", html);
    }

    [Fact]
    public async Task La_pagina_no_encontrada_tiene_contenido_en_espanol()
    {
        var client = _fabrica.CreateClient();
        var respuesta = await client.GetAsync("/pagina-que-no-existe");
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("No encontrado", html);
        Assert.DoesNotContain("Not found", html);
    }
}
