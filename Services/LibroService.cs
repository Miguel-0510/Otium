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

    public async Task AgregarAsync(Libro libro)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        db.Libros.Add(libro);
        await db.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Libro libro)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        db.Libros.Update(libro);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(Libro libro)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        db.Libros.Remove(libro);
        await db.SaveChangesAsync();
    }
}
