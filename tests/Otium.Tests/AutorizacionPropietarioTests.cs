using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;
using Otium.Models;
using Otium.Models.DTOs;
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

        var crear = new LibroCrearDTO
        {
            Titulo = "Propiedad de A",
            Autor = "Autora",
            Estante = Estante.Leyendo
        };
        var (exito, _, creado) = await libros.AgregarAsync(usuarioA.Usuario!.Id, crear);
        Assert.True(exito);
        Assert.NotNull(creado);

        Assert.Single(await libros.ObtenerDeUsuarioAsync(usuarioA.Usuario.Id));
        Assert.Empty(await libros.ObtenerDeUsuarioAsync(usuarioB.Usuario!.Id));

        var actualizarB = new LibroActualizarDTO
        {
            Id = creado.Id,
            Titulo = "Intento de B",
            Autor = "Autora",
            Estante = Estante.Leyendo,
            Puntuacion = 0,
            Resena = ""
        };
        Assert.False((await libros.ActualizarAsync(usuarioB.Usuario.Id, actualizarB)).Exito);
        Assert.False(await libros.EliminarAsync(usuarioB.Usuario.Id, creado.Id));

        var enBase = await db.Libros.AsNoTracking().FirstAsync(l => l.Id == creado.Id);
        Assert.Equal("Propiedad de A", enBase.Titulo);
        Assert.Equal(usuarioA.Usuario.Id, enBase.UsuarioId);

        var actualizarA = new LibroActualizarDTO
        {
            Id = creado.Id,
            Titulo = "Titulo actualizado por A",
            Autor = "Autora",
            Estante = Estante.Leyendo,
            Puntuacion = 0,
            Resena = ""
        };
        Assert.True((await libros.ActualizarAsync(usuarioA.Usuario.Id, actualizarA)).Exito);
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

        var (_, _, creado) = await libros.AgregarAsync(usuarioA.Usuario!.Id, new LibroCrearDTO
        {
            Titulo = "Libro de A",
            Autor = "Autora",
            Estante = Estante.Leyendo
        });
        Assert.NotNull(creado);

        var (exitoEntrada, _, entrada) = await entradas.AgregarAsync(usuarioA.Usuario.Id, creado!.Id, 1, 10, "Travesía de A");
        Assert.True(exitoEntrada);
        Assert.NotNull(entrada);

        Assert.Empty(await entradas.ObtenerDeLibroAsync(usuarioB.Usuario!.Id, creado!.Id));

        var intento = await entradas.AgregarAsync(usuarioB.Usuario.Id, creado!.Id, 11, 20, "Intento de B");
        Assert.False(intento.Exito);
        Assert.Null(intento.Entrada);

        Assert.False(await entradas.EliminarAsync(usuarioB.Usuario.Id, entrada!.Id));

        Assert.Single(await entradas.ObtenerDeLibroAsync(usuarioA.Usuario.Id, creado!.Id));
        Assert.True(await entradas.EliminarAsync(usuarioA.Usuario.Id, entrada.Id));
        Assert.Empty(await entradas.ObtenerDeLibroAsync(usuarioA.Usuario.Id, creado!.Id));
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
