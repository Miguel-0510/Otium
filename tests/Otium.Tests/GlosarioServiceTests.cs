using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Otium.Data;
using Otium.Services;

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
    public async Task La_palabra_se_guarda_y_se_elimina_para_el_propietario()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var fabricaDb = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var limites = scope.ServiceProvider.GetRequiredService<ILimiteIntentosService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlosarioService>>();

        var usuario = await registro.RegistrarAsync($"glosario-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var servicio = new GlosarioService(
            fabricaDb,
            new GeminiFalso("Definición de prueba."),
            limites,
            logger);

        var resultado = await servicio.BuscarYGuardarAsync(usuario.Usuario!.Id, "efímero", "Libro de prueba");

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.NotNull(resultado.Palabra);
        Assert.Equal("Definición de prueba.", resultado.Palabra!.Definicion);

        var palabras = await servicio.ObtenerDeUsuarioAsync(usuario.Usuario.Id);
        var palabra = Assert.Single(palabras);
        Assert.Equal("efímero", palabra.Palabra);

        Assert.True(await servicio.EliminarAsync(usuario.Usuario.Id, palabra.Id));
        Assert.Empty(await servicio.ObtenerDeUsuarioAsync(usuario.Usuario.Id));
    }

    [Fact]
    public async Task No_duplica_palabras_ignorando_mayusculas()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var fabricaDb = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var limites = scope.ServiceProvider.GetRequiredService<ILimiteIntentosService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlosarioService>>();

        var usuario = await registro.RegistrarAsync($"glosario-duplicado-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var gemini = new GeminiFalso("Definición de prueba.");
        var servicio = new GlosarioService(fabricaDb, gemini, limites, logger);

        var primero = await servicio.BuscarYGuardarAsync(usuario.Usuario!.Id, "Efímero", "");
        var segundo = await servicio.BuscarYGuardarAsync(usuario.Usuario.Id, "efímero", "");

        Assert.True(primero.Exito, primero.Mensaje);
        Assert.True(segundo.Exito, segundo.Mensaje);
        Assert.Equal("Ya está en tu glosario.", segundo.Mensaje);
        Assert.Equal(1, gemini.Llamadas);
        Assert.Single(await servicio.ObtenerDeUsuarioAsync(usuario.Usuario.Id));
    }

    private sealed class GeminiFalso : IGeminiClient
    {
        private readonly string _definicion;

        public GeminiFalso(string definicion)
        {
            _definicion = definicion;
        }

        public bool EstaConfigurado => true;

        public int Llamadas { get; private set; }

        public Task<string?> DefinirAsync(string palabra, string libroTitulo, CancellationToken cancellationToken = default)
        {
            Llamadas++;
            return Task.FromResult<string?>(_definicion);
        }
    }
}
