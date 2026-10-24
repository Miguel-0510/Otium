using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;

namespace Otium.Services;

public class LibroService : ILibroService
{
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;

    public LibroService(IDbContextFactory<ApplicationDbContext> fabrica)
    {
        _fabrica = fabrica;
    }

    public async Task<List<Libro>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
        return await db.Libros
            .Where(l => l.UsuarioId == usuarioId)
            .OrderBy(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Exito, string Mensaje, Libro? Libro)> AgregarAsync(
        string usuarioId,
        Libro libro,
        CancellationToken cancellationToken = default)
    {
        var error = NormalizarYValidar(libro);
        if (error is not null)
        {
            return (false, error, null);
        }

        libro.UsuarioId = usuarioId;

        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
        db.Libros.Add(libro);
        await db.SaveChangesAsync(cancellationToken);

        return (true, "", libro);
    }

    public async Task<(bool Exito, string Mensaje)> ActualizarAsync(
        string usuarioId,
        Libro libro,
        CancellationToken cancellationToken = default)
    {
        var error = NormalizarYValidar(libro);
        if (error is not null)
        {
            return (false, error);
        }

        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

        var existente = await db.Libros
            .FirstOrDefaultAsync(l => l.Id == libro.Id && l.UsuarioId == usuarioId, cancellationToken);

        if (existente is null)
        {
            return (false, "No se encontró el libro.");
        }

        existente.Titulo = libro.Titulo;
        existente.Autor = libro.Autor;
        existente.Estante = libro.Estante;
        existente.Puntuacion = libro.Puntuacion;
        existente.Resena = libro.Resena;
        existente.PortadaUrl = libro.PortadaUrl;
        existente.IdExterno = libro.IdExterno;

        await db.SaveChangesAsync(cancellationToken);
        return (true, "");
    }

    public async Task<bool> EliminarAsync(string usuarioId, int libroId, CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

        var filas = await db.Libros
            .Where(l => l.Id == libroId && l.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(cancellationToken);

        return filas > 0;
    }

    public async Task EliminarTodosDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
        await db.Libros
            .Where(l => l.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string? NormalizarYValidar(Libro libro)
    {
        libro.Titulo = (libro.Titulo ?? "").Trim();
        libro.Autor = (libro.Autor ?? "").Trim();
        libro.Resena = (libro.Resena ?? "").Trim();
        libro.PortadaUrl = (libro.PortadaUrl ?? "").Trim();
        libro.IdExterno = (libro.IdExterno ?? "").Trim();

        if (!Enum.IsDefined(libro.Estante))
        {
            return "El estante seleccionado no es válido.";
        }

        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(libro);
        if (Validator.TryValidateObject(libro, contexto, resultados, validateAllProperties: true))
        {
            return null;
        }

        return resultados.FirstOrDefault()?.ErrorMessage ?? "Los datos del libro no son válidos.";
    }
}
