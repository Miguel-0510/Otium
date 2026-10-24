using Microsoft.Extensions.DependencyInjection;
using Otium.Models;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class TravesiasTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public TravesiasTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Agregar_listar_y_eliminar_travesias_de_un_libro_propio()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var libros = scope.ServiceProvider.GetRequiredService<ILibroService>();
        var entradas = scope.ServiceProvider.GetRequiredService<IEntradaLecturaService>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuario = await registro.RegistrarAsync($"travesias-{sufijo}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var libro = new Libro { Titulo = "Libro con travesías", Autor = "Autora", Estante = Estante.Leyendo };
        await libros.AgregarAsync(usuario.Usuario!.Id, libro);

        var primera = await entradas.AgregarAsync(usuario.Usuario.Id, libro.Id, 1, 20, "Primera travesía");
        var segunda = await entradas.AgregarAsync(usuario.Usuario.Id, libro.Id, 21, 40, "Segunda travesía");

        Assert.NotNull(primera);
        Assert.NotNull(segunda);
        Assert.Equal(1, primera!.Numero);
        Assert.Equal(2, segunda!.Numero);

        var lista = await entradas.ObtenerDeLibroAsync(usuario.Usuario.Id, libro.Id);
        Assert.Equal(2, lista.Count);
        Assert.Equal("Primera travesía", lista[0].Resumen);

        Assert.True(await entradas.EliminarAsync(usuario.Usuario.Id, primera.Id));
        Assert.Single(await entradas.ObtenerDeLibroAsync(usuario.Usuario.Id, libro.Id));
    }

    [Fact]
    public async Task No_se_pueden_agregar_travesias_a_un_libro_inexistente()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var entradas = scope.ServiceProvider.GetRequiredService<IEntradaLecturaService>();

        var usuario = await registro.RegistrarAsync($"travesias-sin-libro-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        Assert.Null(await entradas.AgregarAsync(usuario.Usuario!.Id, 999_999, 1, 10, "No debe guardarse"));
    }
}
