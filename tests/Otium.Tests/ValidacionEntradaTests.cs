using Microsoft.Extensions.DependencyInjection;
using Otium.Models;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class ValidacionEntradaTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public ValidacionEntradaTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task No_se_guarda_un_libro_sin_titulo_ni_con_puntuacion_invalida()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var libros = scope.ServiceProvider.GetRequiredService<ILibroService>();

        var usuario = await registro.RegistrarAsync($"validacion-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var sinTitulo = await libros.AgregarAsync(usuario.Usuario!.Id, new Libro
        {
            Titulo = "   ",
            Autor = "Autora",
            Estante = Estante.Leyendo
        });
        Assert.False(sinTitulo.Exito);
        Assert.Null(sinTitulo.Libro);

        var puntuacionInvalida = await libros.AgregarAsync(usuario.Usuario.Id, new Libro
        {
            Titulo = "Título válido",
            Autor = "Autora",
            Estante = Estante.Leyendo,
            Puntuacion = 6
        });
        Assert.False(puntuacionInvalida.Exito);
        Assert.Null(puntuacionInvalida.Libro);

        Assert.Empty(await libros.ObtenerDeUsuarioAsync(usuario.Usuario.Id));
    }

    [Fact]
    public async Task No_se_actualiza_un_libro_con_datos_invalidos()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var libros = scope.ServiceProvider.GetRequiredService<ILibroService>();

        var usuario = await registro.RegistrarAsync($"validacion-act-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var libro = new Libro { Titulo = "Original", Autor = "Autora", Estante = Estante.Leyendo };
        Assert.True((await libros.AgregarAsync(usuario.Usuario!.Id, libro)).Exito);

        libro.Resena = new string('x', 4001);
        var resultado = await libros.ActualizarAsync(usuario.Usuario.Id, libro);

        Assert.False(resultado.Exito);
        var guardado = Assert.Single(await libros.ObtenerDeUsuarioAsync(usuario.Usuario.Id));
        Assert.Equal("Original", guardado.Titulo);
        Assert.Equal("", guardado.Resena);
    }

    [Fact]
    public async Task No_se_guarda_una_travesia_con_paginas_incoherentes_o_resumen_largo()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var libros = scope.ServiceProvider.GetRequiredService<ILibroService>();
        var entradas = scope.ServiceProvider.GetRequiredService<IEntradaLecturaService>();

        var usuario = await registro.RegistrarAsync($"validacion-t-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var libro = new Libro { Titulo = "Libro", Autor = "Autora", Estante = Estante.Leyendo };
        Assert.True((await libros.AgregarAsync(usuario.Usuario!.Id, libro)).Exito);

        var invertida = await entradas.AgregarAsync(usuario.Usuario.Id, libro.Id, 50, 10, "Invertida");
        Assert.False(invertida.Exito);
        Assert.Null(invertida.Entrada);

        var negativa = await entradas.AgregarAsync(usuario.Usuario.Id, libro.Id, -1, 10, "Negativa");
        Assert.False(negativa.Exito);

        var larga = await entradas.AgregarAsync(usuario.Usuario.Id, libro.Id, 1, 10, new string('x', 2001));
        Assert.False(larga.Exito);

        Assert.Empty(await entradas.ObtenerDeLibroAsync(usuario.Usuario.Id, libro.Id));
    }

    [Fact]
    public async Task El_glosario_rechaza_palabras_demasiado_largas()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var glosario = scope.ServiceProvider.GetRequiredService<IGlosarioService>();

        var usuario = await registro.RegistrarAsync($"validacion-g-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var resultado = await glosario.BuscarYGuardarAsync(
            usuario.Usuario!.Id,
            new string('x', 101),
            "Libro");

        Assert.False(resultado.Exito);
        Assert.Null(resultado.Palabra);
        Assert.Empty(await glosario.ObtenerDeUsuarioAsync(usuario.Usuario.Id));
    }
}
