using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class AmistadAntiabusoTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public AmistadAntiabusoTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Solicitudes_duplicadas_y_codigos_inexistentes_no_revelan_si_el_codigo_existe()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var amistades = scope.ServiceProvider.GetRequiredService<IAmistadService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuarioA = await registro.RegistrarAsync($"social-a-{sufijo}@example.com", "Password1");
        var usuarioB = await registro.RegistrarAsync($"social-b-{sufijo}@example.com", "Password1");
        Assert.True(usuarioA.Exito);
        Assert.True(usuarioB.Exito);

        var primera = await amistades.EnviarSolicitudAsync(usuarioA.Usuario!.Id, usuarioB.Usuario!.CodigoAmigo);
        Assert.True(primera.Exito, primera.Mensaje);

        var duplicada = await amistades.EnviarSolicitudAsync(usuarioA.Usuario.Id, usuarioB.Usuario.CodigoAmigo);
        var inexistente = await amistades.EnviarSolicitudAsync(usuarioA.Usuario.Id, "ZZZZZZZZ");
        var inversa = await amistades.EnviarSolicitudAsync(usuarioB.Usuario.Id, usuarioA.Usuario.CodigoAmigo);

        Assert.False(duplicada.Exito);
        Assert.False(inexistente.Exito);
        Assert.False(inversa.Exito);
        Assert.Equal(inexistente.Mensaje, duplicada.Mensaje);
        Assert.Equal(inexistente.Mensaje, inversa.Mensaje);

        var clave = string.CompareOrdinal(usuarioA.Usuario.Id, usuarioB.Usuario.Id) < 0
            ? $"{usuarioA.Usuario.Id}|{usuarioB.Usuario.Id}"
            : $"{usuarioB.Usuario.Id}|{usuarioA.Usuario.Id}";

        Assert.Equal(1, await db.Amistades.CountAsync(a => a.ClavePar == clave));
    }

    [Fact]
    public async Task Las_solicitudes_de_amistad_se_limitan_por_usuario()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var amistades = scope.ServiceProvider.GetRequiredService<IAmistadService>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuario = await registro.RegistrarAsync($"limite-a-{sufijo}@example.com", "Password1");
        Assert.True(usuario.Exito);

        (bool Exito, string Mensaje) ultima = default;
        for (var i = 0; i < 11; i++)
        {
            ultima = await amistades.EnviarSolicitudAsync(usuario.Usuario!.Id, "ZZZZZZZZ");
        }

        Assert.False(ultima.Exito);
        Assert.Contains("Demasiados intentos", ultima.Mensaje);
    }
}
