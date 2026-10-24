using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;
using Otium.Models;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class AutorizacionPropietarioTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public AutorizacionPropietarioTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_usuario_no_puede_leer_editar_ni_borrar_libros_ajenos()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var libros = scope.ServiceProvider.GetRequiredService<ILibroService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuarioA = await registro.RegistrarAsync($"dueno-{sufijo}@example.com", "Password1");
        var usuarioB = await registro.RegistrarAsync($"intruso-{sufijo}@example.com", "Password1");
        Assert.True(usuarioA.Exito);
        Assert.True(usuarioB.Exito);

        var libro = new Libro
        {
            Titulo = "Propiedad de A",
            Autor = "Autora",
            Estante = Estante.Leyendo
        };
        await libros.AgregarAsync(usuarioA.Usuario!.Id, libro);

        Assert.Single(await libros.ObtenerDeUsuarioAsync(usuarioA.Usuario.Id));
        Assert.Empty(await libros.ObtenerDeUsuarioAsync(usuarioB.Usuario!.Id));

        libro.Titulo = "Intento de B";
        Assert.False(await libros.ActualizarAsync(usuarioB.Usuario.Id, libro));
        Assert.False(await libros.EliminarAsync(usuarioB.Usuario.Id, libro.Id));

        var enBase = await db.Libros.AsNoTracking().FirstAsync(l => l.Id == libro.Id);
        Assert.Equal("Propiedad de A", enBase.Titulo);
        Assert.Equal(usuarioA.Usuario.Id, enBase.UsuarioId);

        libro.Titulo = "Titulo actualizado por A";
        Assert.True(await libros.ActualizarAsync(usuarioA.Usuario.Id, libro));
    }

    [Fact]
    public async Task Un_usuario_no_puede_crear_ni_borrar_travesias_de_libros_ajenos()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var libros = scope.ServiceProvider.GetRequiredService<ILibroService>();
        var entradas = scope.ServiceProvider.GetRequiredService<IEntradaLecturaService>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuarioA = await registro.RegistrarAsync($"dueno-t-{sufijo}@example.com", "Password1");
        var usuarioB = await registro.RegistrarAsync($"intruso-t-{sufijo}@example.com", "Password1");
        Assert.True(usuarioA.Exito);
        Assert.True(usuarioB.Exito);

        var libro = new Libro { Titulo = "Libro de A", Autor = "Autora", Estante = Estante.Leyendo };
        await libros.AgregarAsync(usuarioA.Usuario!.Id, libro);

        var entrada = await entradas.AgregarAsync(usuarioA.Usuario.Id, libro.Id, 1, 10, "Travesía de A");
        Assert.NotNull(entrada);

        Assert.Empty(await entradas.ObtenerDeLibroAsync(usuarioB.Usuario!.Id, libro.Id));
        Assert.Null(await entradas.AgregarAsync(usuarioB.Usuario.Id, libro.Id, 11, 20, "Intento de B"));
        Assert.False(await entradas.EliminarAsync(usuarioB.Usuario.Id, entrada!.Id));

        Assert.Single(await entradas.ObtenerDeLibroAsync(usuarioA.Usuario.Id, libro.Id));
        Assert.True(await entradas.EliminarAsync(usuarioA.Usuario.Id, entrada.Id));
        Assert.Empty(await entradas.ObtenerDeLibroAsync(usuarioA.Usuario.Id, libro.Id));
    }

    [Fact]
    public async Task Un_usuario_no_puede_borrar_palabras_de_glosario_ajenas()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var glosario = scope.ServiceProvider.GetRequiredService<IGlosarioService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sufijo = Guid.NewGuid().ToString("N");
        var usuarioA = await registro.RegistrarAsync($"dueno-g-{sufijo}@example.com", "Password1");
        var usuarioB = await registro.RegistrarAsync($"intruso-g-{sufijo}@example.com", "Password1");
        Assert.True(usuarioA.Exito);
        Assert.True(usuarioB.Exito);

        var palabra = new PalabraGlosario
        {
            UsuarioId = usuarioA.Usuario!.Id,
            Palabra = "ajena",
            Definicion = "Definición de A",
            LibroTitulo = "Libro de A"
        };
        db.PalabrasGlosario.Add(palabra);
        await db.SaveChangesAsync();

        Assert.False(await glosario.EliminarAsync(usuarioB.Usuario!.Id, palabra.Id));
        Assert.True(await db.PalabrasGlosario.AnyAsync(p => p.Id == palabra.Id));

        Assert.True(await glosario.EliminarAsync(usuarioA.Usuario.Id, palabra.Id));
        Assert.False(await db.PalabrasGlosario.AnyAsync(p => p.Id == palabra.Id));
    }
}
