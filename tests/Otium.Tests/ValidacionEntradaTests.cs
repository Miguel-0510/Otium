using Microsoft.Extensions.DependencyInjection;
using Otium.Models;
using Otium.Models.DTOs;
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

        var sinTitulo = await libros.AgregarAsync(usuario.Usuario!.Id, new LibroCrearDTO
        {
            Titulo = "   ",
            Autor = "Autora",
            Estante = Estante.Leyendo
        });
        Assert.False(sinTitulo.Exito);
        Assert.Null(sinTitulo.Libro);

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

        var crear = new LibroCrearDTO { Titulo = "Original", Autor = "Autora", Estante = Estante.Leyendo };
        var (exito, _, creado) = await libros.AgregarAsync(usuario.Usuario!.Id, crear);
        Assert.True(exito);
        Assert.NotNull(creado);

        var actualizar = new LibroActualizarDTO
        {
            Id = creado!.Id,
            Titulo = "Original",
            Autor = "Autora",
            Estante = Estante.Leyendo,
            Puntuacion = 0,
            Resena = new string('x', 4001)
        };
        var resultado = await libros.ActualizarAsync(usuario.Usuario.Id, actualizar);

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

        var (_, _, creado) = await libros.AgregarAsync(usuario.Usuario!.Id, new LibroCrearDTO
        {
            Titulo = "Libro",
            Autor = "Autora",
            Estante = Estante.Leyendo
        });
        Assert.NotNull(creado);

        var invertida = await entradas.AgregarAsync(usuario.Usuario.Id, creado!.Id, 50, 10, "Invertida");
        Assert.False(invertida.Exito);
        Assert.Null(invertida.Entrada);

        var negativa = await entradas.AgregarAsync(usuario.Usuario.Id, creado!.Id, -1, 10, "Negativa");
        Assert.False(negativa.Exito);

        var larga = await entradas.AgregarAsync(usuario.Usuario.Id, creado!.Id, 1, 10, new string('x', 2001));
        Assert.False(larga.Exito);

        Assert.Empty(await entradas.ObtenerDeLibroAsync(usuario.Usuario.Id, creado!.Id));
    }

    [Fact]
    public async Task El_glosario_rechaza_textos_demasiado_largos()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var glosario = scope.ServiceProvider.GetRequiredService<IGlosarioService>();

        var usuario = await registro.RegistrarAsync($"validacion-g-{Guid.NewGuid():N}@example.com", "Password1");
        Assert.True(usuario.Exito);

        var resultado = await glosario.BuscarYGuardarAsync(
            usuario.Usuario!.Id,
            new string('x', 501),
            "Libro",
            false);

        Assert.False(resultado.Exito);
        Assert.Null(resultado.Palabra);
        Assert.Empty(await glosario.ObtenerDeUsuarioAsync(usuario.Usuario.Id));
    }
}
