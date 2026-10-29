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
            new DefinicionFalsa(new ResultadoDefinicion("efímero", "Definición de prueba.", "")),
            limites,
            logger);

        var resultado = await servicio.BuscarYGuardarAsync(usuario.Usuario!.Id, "efímero", "Libro de prueba", false);

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Contains("Guardada en tu glosario.", resultado.Mensaje, StringComparison.Ordinal);
        Assert.NotNull(resultado.Palabra);
        Assert.Equal("efímero", resultado.Palabra!.Palabra);
        Assert.Equal("Definición de prueba.", resultado.Palabra.Definicion);

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

        var definiciones = new DefinicionFalsa(new ResultadoDefinicion("efímero", "Definición de prueba.", ""));
        var servicio = new GlosarioService(fabricaDb, definiciones, limites, logger);

        var primero = await servicio.BuscarYGuardarAsync(usuario.Usuario!.Id, "Efímero", "", false);
        var segundo = await servicio.BuscarYGuardarAsync(usuario.Usuario.Id, "efímero", "", false);

        Assert.True(primero.Exito, primero.Mensaje);
        Assert.True(segundo.Exito, segundo.Mensaje);
        Assert.Equal("Ya está en tu glosario.", segundo.Mensaje);
        // La segunda consulta pasa por la vía rápida de deduplicación sin gastar cuota de API.
        Assert.Equal(1, definiciones.Llamadas);
        Assert.Single(await servicio.ObtenerDeUsuarioAsync(usuario.Usuario.Id));
    }

    [Fact]
    public async Task Extrae_el_termino_de_una_frase_larga()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var fabricaDb = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var limites = scope.ServiceProvider.GetRequiredService<ILimiteIntentosService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlosarioService>>();

        var usuario = await registro.RegistrarAsync($"glosario-frase-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var servicio = new GlosarioService(
            fabricaDb,
            new DefinicionFalsa(new ResultadoDefinicion("resiliencia", "Capacidad de adaptarse ante la adversidad.", "")),
            limites,
            logger);

        var resultado = await servicio.BuscarYGuardarAsync(
            usuario.Usuario!.Id,
            "demostró una resiliencia formidable",
            "",
            false);

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Contains("Término analizado: resiliencia", resultado.Mensaje, StringComparison.Ordinal);
        Assert.NotNull(resultado.Palabra);
        Assert.Equal("resiliencia", resultado.Palabra!.Palabra);
        Assert.Equal("Capacidad de adaptarse ante la adversidad.", resultado.Palabra.Definicion);
    }

    [Fact]
    public async Task Anade_el_contexto_de_lectura_cuando_aporta_un_matiz()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var fabricaDb = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var limites = scope.ServiceProvider.GetRequiredService<ILimiteIntentosService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlosarioService>>();

        var usuario = await registro.RegistrarAsync($"glosario-lectura-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        const string contexto = "En la novela designa el alma de los antagonistas.";
        var servicio = new GlosarioService(
            fabricaDb,
            new DefinicionFalsa(new ResultadoDefinicion("resiliencia", "Capacidad de adaptarse ante la adversidad.", contexto)),
            limites,
            logger);

        var resultado = await servicio.BuscarYGuardarAsync(
            usuario.Usuario!.Id,
            "resiliencia",
            "Libro de prueba",
            true);

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.NotNull(resultado.Palabra);
        Assert.Contains(
            $"En el contexto de la lectura: {contexto}",
            resultado.Palabra!.Definicion,
            StringComparison.Ordinal);
    }

    private sealed class DefinicionFalsa : IDefinicionClient
    {
        private readonly ResultadoDefinicion _resultado;

        public DefinicionFalsa(ResultadoDefinicion resultado)
        {
            _resultado = resultado;
        }

        public bool EstaConfigurado => true;

        public int Llamadas { get; private set; }

        public Task<ResultadoDefinicion?> DefinirAsync(string texto, string libroTitulo, bool incluirContextoLectura, CancellationToken cancellationToken = default)
        {
            Llamadas++;
            return Task.FromResult<ResultadoDefinicion?>(_resultado);
        }
    }
}
