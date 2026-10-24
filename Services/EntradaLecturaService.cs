using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    public async Task<List<EntradaLectura>> ObtenerDeLibroAsync(
        string usuarioId,
        int libroId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

        var pertenece = await db.Libros.AnyAsync(l => l.Id == libroId && l.UsuarioId == usuarioId, cancellationToken);
        if (!pertenece)
        {
            return new List<EntradaLectura>();
        }

        return await db.EntradasLectura
            .Where(e => e.LibroId == libroId)
            .OrderBy(e => e.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Exito, string Mensaje, EntradaLectura? Entrada)> AgregarAsync(
        string usuarioId,
        int libroId,
        int? paginaInicio,
        int? paginaFin,
        string resumen,
        CancellationToken cancellationToken = default)
    {
        var entrada = new EntradaLectura
        {
            LibroId = libroId,
            PaginaInicio = paginaInicio,
            PaginaFin = paginaFin,
            Resumen = (resumen ?? "").Trim()
        };

        var error = Validar(entrada);
        if (error is not null)
        {
            return (false, error, null);
        }

        const int maxReintentos = 3;
        for (int intento = 0; intento < maxReintentos; intento++)
        {
            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

            var pertenece = await db.Libros.AnyAsync(l => l.Id == libroId && l.UsuarioId == usuarioId, cancellationToken);
            if (!pertenece)
            {
                return (false, "No se encontró el libro.", null);
            }

            var ultimoNumero = await db.EntradasLectura
                .Where(e => e.LibroId == libroId)
                .MaxAsync(e => (int?)e.Numero, cancellationToken) ?? 0;

            entrada.Numero = ultimoNumero + 1;

            db.EntradasLectura.Add(entrada);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return (true, "", entrada);
            }
            catch (DbUpdateException ex) when (EsViolacionIndiceUnico(ex))
            {
                if (intento == maxReintentos - 1)
                {
                    return (false, "No se pudo guardar la travesía por concurrencia. Inténtalo de nuevo.", null);
                }
                await Task.Delay(50 * (intento + 1), cancellationToken);
            }
        }

        return (false, "No se pudo guardar la travesía por concurrencia. Inténtalo de nuevo.", null);
    }

    private static bool EsViolacionIndiceUnico(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("IX_EntradasLectura_LibroId_Numero", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true
            || ex.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> EliminarAsync(string usuarioId, int entradaId, CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

        var filas = await db.EntradasLectura
            .Where(e => e.Id == entradaId && db.Libros.Any(l => l.Id == e.LibroId && l.UsuarioId == usuarioId))
            .ExecuteDeleteAsync(cancellationToken);

        return filas > 0;
    }

    private static string? Validar(EntradaLectura entrada)
    {
        if (entrada.PaginaInicio is null && entrada.PaginaFin is null && entrada.Resumen.Length == 0)
        {
            return "Escribe un resumen o indica al menos una página.";
        }

        if (entrada.PaginaInicio is int inicio && entrada.PaginaFin is int fin && fin < inicio)
        {
            return "La página final no puede ser anterior a la inicial.";
        }

        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(entrada);
        if (Validator.TryValidateObject(entrada, contexto, resultados, validateAllProperties: true))
        {
            return null;
        }

        return resultados.FirstOrDefault()?.ErrorMessage ?? "Los datos de la travesía no son válidos.";
    }
}
