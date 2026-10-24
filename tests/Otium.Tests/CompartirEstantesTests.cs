using Microsoft.Extensions.DependencyInjection;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class CompartirEstantesTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public CompartirEstantesTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Solo_los_amigos_ven_los_estantes_si_el_dueno_los_comparte()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var amistades = scope.ServiceProvider.GetRequiredService<IAmistadService>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuarioA = await registro.RegistrarAsync($"comparte-a-{sufijo}@example.com", "Password1");
        var usuarioB = await registro.RegistrarAsync($"comparte-b-{sufijo}@example.com", "Password1");
        var usuarioC = await registro.RegistrarAsync($"comparte-c-{sufijo}@example.com", "Password1");
        Assert.True(usuarioA.Exito);
        Assert.True(usuarioB.Exito);
        Assert.True(usuarioC.Exito);

        await HacerAmigosAsync(amistades, usuarioA.Usuario!.Id, usuarioB.Usuario!.Id, usuarioB.Usuario.CodigoAmigo);

        Assert.False(await amistades.PuedeVerEstantesAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id));
        Assert.True(await amistades.EstablecerCompartirEstantesAsync(usuarioB.Usuario.Id, true));

        Assert.True(await amistades.PuedeVerEstantesAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id));
        Assert.False(await amistades.PuedeVerEstantesAsync(usuarioB.Usuario.Id, usuarioA.Usuario.Id));
        Assert.False(await amistades.PuedeVerEstantesAsync(usuarioC.Usuario!.Id, usuarioB.Usuario.Id));

        var amigo = await amistades.ObtenerAmigoAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id);
        Assert.NotNull(amigo);
        Assert.True(amigo!.ComparteEstantes);

        Assert.True(await amistades.EstablecerCompartirEstantesAsync(usuarioB.Usuario.Id, false));
        Assert.False(await amistades.PuedeVerEstantesAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id));
    }

    [Fact]
    public async Task Bloquear_elimina_la_amistad_y_el_acceso_a_los_estantes()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var amistades = scope.ServiceProvider.GetRequiredService<IAmistadService>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuarioA = await registro.RegistrarAsync($"bloquea-a-{sufijo}@example.com", "Password1");
        var usuarioB = await registro.RegistrarAsync($"bloquea-b-{sufijo}@example.com", "Password1");
        Assert.True(usuarioA.Exito);
        Assert.True(usuarioB.Exito);

        await HacerAmigosAsync(amistades, usuarioA.Usuario!.Id, usuarioB.Usuario!.Id, usuarioB.Usuario.CodigoAmigo);
        Assert.True(await amistades.EstablecerCompartirEstantesAsync(usuarioA.Usuario.Id, true));
        Assert.True(await amistades.PuedeVerEstantesAsync(usuarioB.Usuario.Id, usuarioA.Usuario.Id));

        var bloqueo = await amistades.BloquearAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id);
        Assert.True(bloqueo.Exito, bloqueo.Mensaje);

        Assert.False(await amistades.SonAmigosAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id));
        Assert.False(await amistades.PuedeVerEstantesAsync(usuarioB.Usuario.Id, usuarioA.Usuario.Id));
        Assert.Single(await amistades.ObtenerBloqueadosAsync(usuarioA.Usuario.Id));

        var solicitud = await amistades.EnviarSolicitudAsync(usuarioB.Usuario.Id, usuarioA.Usuario.CodigoAmigo);
        Assert.False(solicitud.Exito);

        var desbloqueo = await amistades.DesbloquearAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id);
        Assert.True(desbloqueo.Exito, desbloqueo.Mensaje);
        var solicitudTrasDesbloqueo = await amistades.EnviarSolicitudAsync(usuarioB.Usuario.Id, usuarioA.Usuario.CodigoAmigo);
        Assert.True(solicitudTrasDesbloqueo.Exito, solicitudTrasDesbloqueo.Mensaje);
    }

    [Fact]
    public async Task Eliminar_amigo_corta_la_relacion_sin_bloquear()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var amistades = scope.ServiceProvider.GetRequiredService<IAmistadService>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuarioA = await registro.RegistrarAsync($"elimina-a-{sufijo}@example.com", "Password1");
        var usuarioB = await registro.RegistrarAsync($"elimina-b-{sufijo}@example.com", "Password1");
        Assert.True(usuarioA.Exito);
        Assert.True(usuarioB.Exito);

        await HacerAmigosAsync(amistades, usuarioA.Usuario!.Id, usuarioB.Usuario!.Id, usuarioB.Usuario.CodigoAmigo);

        var eliminacion = await amistades.EliminarAmigoAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id);
        Assert.True(eliminacion.Exito, eliminacion.Mensaje);
        Assert.False(await amistades.SonAmigosAsync(usuarioA.Usuario.Id, usuarioB.Usuario.Id));
        Assert.Empty(await amistades.ObtenerBloqueadosAsync(usuarioA.Usuario.Id));
    }

    private static async Task HacerAmigosAsync(IAmistadService amistades, string solicitanteId, string receptorId, string codigoReceptor)
    {
        var solicitud = await amistades.EnviarSolicitudAsync(solicitanteId, codigoReceptor);
        Assert.True(solicitud.Exito, solicitud.Mensaje);

        var pendientes = await amistades.ObtenerSolicitudesPendientesAsync(receptorId);
        var pendiente = Assert.Single(pendientes);
        await amistades.ResponderSolicitudAsync(pendiente.Id, receptorId, true);
    }
}
