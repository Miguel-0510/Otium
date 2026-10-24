using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;
using Otium.Models;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class ExportacionDatosTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public ExportacionDatosTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_exportacion_incluye_libros_travesias_glosario_y_amistades()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var exportacion = scope.ServiceProvider.GetRequiredService<IExportacionDatosService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sufijo = Guid.NewGuid().ToString("N");
        var correoA = $"exportar-a-{sufijo}@example.com";
        var correoB = $"exportar-b-{sufijo}@example.com";
        var correoC = $"exportar-c-{sufijo}@example.com";

        var usuarioA = await registro.RegistrarAsync(correoA, "Password1");
        var usuarioB = await registro.RegistrarAsync(correoB, "Password1");
        var usuarioC = await registro.RegistrarAsync(correoC, "Password1");
        Assert.True(usuarioA.Exito);
        Assert.True(usuarioB.Exito);
        Assert.True(usuarioC.Exito);

        var usuarioDb = await db.Users.FindAsync(usuarioA.Usuario!.Id);
        usuarioDb!.CompartirEstantes = true;

        var libro = new Libro
        {
            UsuarioId = usuarioA.Usuario!.Id,
            Titulo = "Libro exportado",
            Autor = "Autora",
            Estante = Estante.Leyendo,
            Puntuacion = 5,
            Resena = "Reseña exportada"
        };
        db.Libros.Add(libro);
        await db.SaveChangesAsync();

        db.EntradasLectura.Add(new EntradaLectura
        {
            LibroId = libro.Id,
            Numero = 1,
            PaginaInicio = 3,
            PaginaFin = 15,
            Resumen = "Travesía exportada"
        });
        db.PalabrasGlosario.Add(new PalabraGlosario
        {
            UsuarioId = usuarioA.Usuario.Id,
            Palabra = "exportar",
            Definicion = "Definición exportada",
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
            BloqueadoId = usuarioC.Usuario!.Id
        });
        await db.SaveChangesAsync();

        var datos = await exportacion.ObtenerAsync(usuarioA.Usuario);

        Assert.Equal(correoA, datos.Cuenta.Correo);
        var libroExportado = Assert.Single(datos.Libros);
        Assert.Equal("Libro exportado", libroExportado.Titulo);
        Assert.Equal("Leyendo", libroExportado.Estante);

        var travesia = Assert.Single(datos.Travesias);
        Assert.Equal(libro.Id, travesia.LibroId);
        Assert.Equal("Libro exportado", travesia.LibroTitulo);

        var palabra = Assert.Single(datos.Glosario);
        Assert.Equal("exportar", palabra.Palabra);

        var amistad = Assert.Single(datos.Amistades);
        Assert.Equal(correoB, amistad.CorreoOtroUsuario);
        Assert.Equal("Aceptada", amistad.Estado);

        Assert.True(datos.Cuenta.ComparteEstantes);
        var bloqueo = Assert.Single(datos.Bloqueos);
        Assert.Equal(correoC, bloqueo.CorreoBloqueado);
    }
}
