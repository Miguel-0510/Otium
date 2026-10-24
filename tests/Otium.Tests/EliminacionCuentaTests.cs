using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;
using Otium.Models;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class EliminacionCuentaTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public EliminacionCuentaTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Eliminar_cuenta_borra_libros_travesias_glosario_y_amistades()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sufijo = Guid.NewGuid().ToString("N");
        var correoA = $"borrar-a-{sufijo}@example.com";
        var correoB = $"borrar-b-{sufijo}@example.com";

        var usuarioA = await registro.RegistrarAsync(correoA, "Password1");
        var usuarioB = await registro.RegistrarAsync(correoB, "Password1");
        Assert.True(usuarioA.Exito, Describe(usuarioA));
        Assert.True(usuarioB.Exito, Describe(usuarioB));

        var libro = new Libro
        {
            UsuarioId = usuarioA.Usuario!.Id,
            Titulo = "Libro de prueba",
            Autor = "Autora",
            Estante = Estante.Leido,
            Puntuacion = 4,
            Resena = "Reseña de prueba"
        };
        db.Libros.Add(libro);
        await db.SaveChangesAsync();

        db.EntradasLectura.Add(new EntradaLectura
        {
            LibroId = libro.Id,
            Numero = 1,
            PaginaInicio = 1,
            PaginaFin = 20,
            Resumen = "Travesía de prueba"
        });
        db.PalabrasGlosario.Add(new PalabraGlosario
        {
            UsuarioId = usuarioA.Usuario.Id,
            Palabra = "prueba",
            Definicion = "Definición de prueba",
            LibroTitulo = libro.Titulo
        });
        db.Amistades.Add(new Amistad
        {
            SolicitanteId = usuarioA.Usuario.Id,
            ReceptorId = usuarioB.Usuario!.Id,
            ClavePar = $"{usuarioA.Usuario.Id}|{usuarioB.Usuario.Id}",
            Estado = EstadoAmistad.Aceptada
        });
        db.Bloqueos.Add(new Bloqueo
        {
            UsuarioId = usuarioA.Usuario.Id,
            BloqueadoId = usuarioB.Usuario.Id
        });
        await db.SaveChangesAsync();

        var eliminacion = scope.ServiceProvider.GetRequiredService<IEliminacionCuentaService>();
        var resultado = await eliminacion.EliminarAsync(usuarioA.Usuario.Id, "Password1");

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.False(await db.Users.AnyAsync(u => u.Id == usuarioA.Usuario.Id));
        Assert.False(await db.Libros.AnyAsync(l => l.UsuarioId == usuarioA.Usuario.Id));
        Assert.False(await db.EntradasLectura.AnyAsync(e => e.LibroId == libro.Id));
        Assert.False(await db.PalabrasGlosario.AnyAsync(p => p.UsuarioId == usuarioA.Usuario.Id));
        Assert.False(await db.Amistades.AnyAsync(a => a.SolicitanteId == usuarioA.Usuario.Id || a.ReceptorId == usuarioA.Usuario.Id));
        Assert.False(await db.Bloqueos.AnyAsync(b => b.UsuarioId == usuarioA.Usuario.Id || b.BloqueadoId == usuarioA.Usuario.Id));
        Assert.True(await db.Users.AnyAsync(u => u.Id == usuarioB.Usuario.Id));
    }

    [Fact]
    public async Task Una_contrasena_incorrecta_no_elimina_la_cuenta()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var eliminacion = scope.ServiceProvider.GetRequiredService<IEliminacionCuentaService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var correo = $"borrar-clave-{Guid.NewGuid():N}@example.com";
        var usuario = await registro.RegistrarAsync(correo, "Password1");
        Assert.True(usuario.Exito, Describe(usuario));

        var resultado = await eliminacion.EliminarAsync(usuario.Usuario!.Id, "Incorrecta1");

        Assert.False(resultado.Exito);
        Assert.True(await db.Users.AnyAsync(u => u.Id == usuario.Usuario.Id));
    }

    private static string Describe(ResultadoRegistro resultado) =>
        string.Join(" | ", resultado.Errores.Select(e => $"{e.Code}: {e.Description}"));
}
