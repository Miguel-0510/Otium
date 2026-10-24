using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class CodigoAmigoServiceTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public CodigoAmigoServiceTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Reintenta_cuando_el_codigo_ya_existe()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var fabricaDb = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        await using (var db = await fabricaDb.CreateDbContextAsync())
        {
            db.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"existente-{Guid.NewGuid():N}@example.com",
                Email = $"existente-{Guid.NewGuid():N}@example.com",
                CodigoAmigo = "AAAAAAAA"
            });
            await db.SaveChangesAsync();
        }

        var generador = new GeneradorFalso("AAAAAAAA", "BBBBBBBB");
        var servicio = new CodigoAmigoService(fabricaDb, generador);

        var codigo = await servicio.GenerarCodigoUnicoAsync();

        Assert.Equal("BBBBBBBB", codigo);
        Assert.Equal(2, generador.Llamadas);
    }

    [Fact]
    public async Task Falla_tras_agotar_los_intentos()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var fabricaDb = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        await using (var db = await fabricaDb.CreateDbContextAsync())
        {
            db.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"ocupado-{Guid.NewGuid():N}@example.com",
                Email = $"ocupado-{Guid.NewGuid():N}@example.com",
                CodigoAmigo = "CCCCCCCC"
            });
            await db.SaveChangesAsync();
        }

        var generador = new GeneradorFalso(Enumerable.Repeat("CCCCCCCC", 20).ToArray());
        var servicio = new CodigoAmigoService(fabricaDb, generador);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.GenerarCodigoUnicoAsync());
        Assert.Equal(20, generador.Llamadas);
    }

    private sealed class GeneradorFalso : ICodigoAmigoGenerator
    {
        private readonly Queue<string> _codigos;

        public GeneradorFalso(params string[] codigos)
        {
            _codigos = new Queue<string>(codigos);
        }

        public int Llamadas { get; private set; }

        public string Generar()
        {
            Llamadas++;
            return _codigos.Dequeue();
        }
    }
}

