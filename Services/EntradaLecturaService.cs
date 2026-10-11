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

    public async Task<List<EntradaLectura>> ObtenerDeLibroAsync(int libroId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        return await db.EntradasLectura
            .Where(e => e.LibroId == libroId)
            .OrderBy(e => e.Numero)
            .ToListAsync();
    }

    public async Task<EntradaLectura> AgregarAsync(int libroId, int? paginaInicio, int? paginaFin, string resumen)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var siguienteNumero = await db.EntradasLectura
            .Where(e => e.LibroId == libroId)
            .CountAsync() + 1;

        var entrada = new EntradaLectura
        {
            LibroId = libroId,
            Numero = siguienteNumero,
            PaginaInicio = paginaInicio,
            PaginaFin = paginaFin,
            Resumen = resumen
        };

        db.EntradasLectura.Add(entrada);
        await db.SaveChangesAsync();
        return entrada;
    }

    public async Task EliminarAsync(EntradaLectura entrada)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        db.EntradasLectura.Remove(entrada);
        await db.SaveChangesAsync();
    }
}
