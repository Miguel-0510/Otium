using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;

namespace Otium.Services;

public class EntradaLecturaService : IEntradaLecturaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;

    public EntradaLecturaService(IDbContextFactory<ApplicationDbContext> fabrica)
    {
        _fabrica = fabrica;
    }

    public async Task<List<EntradaLectura>> ObtenerDeLibroAsync(string usuarioId, int libroId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var pertenece = await db.Libros.AnyAsync(l => l.Id == libroId && l.UsuarioId == usuarioId);
        if (!pertenece)
        {
            return new List<EntradaLectura>();
        }

        return await db.EntradasLectura
            .Where(e => e.LibroId == libroId)
            .OrderBy(e => e.Numero)
            .ToListAsync();
    }

    public async Task<EntradaLectura?> AgregarAsync(string usuarioId, int libroId, int? paginaInicio, int? paginaFin, string resumen)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var pertenece = await db.Libros.AnyAsync(l => l.Id == libroId && l.UsuarioId == usuarioId);
        if (!pertenece)
        {
            return null;
        }

        var ultimoNumero = await db.EntradasLectura
            .Where(e => e.LibroId == libroId)
            .MaxAsync(e => (int?)e.Numero) ?? 0;

        var entrada = new EntradaLectura
        {
            LibroId = libroId,
            Numero = ultimoNumero + 1,
            PaginaInicio = paginaInicio,
            PaginaFin = paginaFin,
            Resumen = resumen
        };

        db.EntradasLectura.Add(entrada);
        await db.SaveChangesAsync();
        return entrada;
    }

    public async Task<bool> EliminarAsync(string usuarioId, int entradaId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var filas = await db.EntradasLectura
            .Where(e => e.Id == entradaId && db.Libros.Any(l => l.Id == e.LibroId && l.UsuarioId == usuarioId))
            .ExecuteDeleteAsync();

        return filas > 0;
    }
}
