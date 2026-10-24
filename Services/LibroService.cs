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

    public async Task<List<Libro>> ObtenerDeUsuarioAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        return await db.Libros
            .Where(l => l.UsuarioId == usuarioId)
            .OrderBy(l => l.Id)
            .ToListAsync();
    }

    public async Task AgregarAsync(string usuarioId, Libro libro)
    {
        libro.UsuarioId = usuarioId;

        await using var db = await _fabrica.CreateDbContextAsync();
        db.Libros.Add(libro);
        await db.SaveChangesAsync();
    }

    public async Task<bool> ActualizarAsync(string usuarioId, Libro libro)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var existente = await db.Libros
            .FirstOrDefaultAsync(l => l.Id == libro.Id && l.UsuarioId == usuarioId);

        if (existente is null)
        {
            return false;
        }

        existente.Titulo = libro.Titulo;
        existente.Autor = libro.Autor;
        existente.Estante = libro.Estante;
        existente.Puntuacion = libro.Puntuacion;
        existente.Resena = libro.Resena;
        existente.PortadaUrl = libro.PortadaUrl;
        existente.IdExterno = libro.IdExterno;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(string usuarioId, int libroId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var filas = await db.Libros
            .Where(l => l.Id == libroId && l.UsuarioId == usuarioId)
            .ExecuteDeleteAsync();

        return filas > 0;
    }

    public async Task EliminarTodosDeUsuarioAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        await db.Libros.Where(l => l.UsuarioId == usuarioId).ExecuteDeleteAsync();
    }
}
