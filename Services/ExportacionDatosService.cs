using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;

namespace Otium.Services;

public sealed class ExportacionDatosService : IExportacionDatosService
{
    private readonly ApplicationDbContext _db;

    public ExportacionDatosService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ExportacionDatos> ObtenerAsync(ApplicationUser usuario, CancellationToken cancellationToken = default)
    {
        var usuarioId = usuario.Id;

        var libros = await _db.Libros
            .AsNoTracking()
            .Where(l => l.UsuarioId == usuarioId)
            .OrderBy(l => l.Id)
            .ToListAsync(cancellationToken);

        var librosExportados = libros
            .Select(l => new LibroExportado(
                l.Id,
                l.Titulo,
                l.Autor,
                l.Estante.ToString(),
                l.Puntuacion,
                l.Resena,
                l.PortadaUrl,
                l.IdExterno))
            .ToList();

        var libroIds = libros.Select(l => l.Id).ToList();
        var titulos = libros.ToDictionary(l => l.Id, l => l.Titulo);

        var entradas = libroIds.Count == 0
            ? new List<EntradaLectura>()
            : await _db.EntradasLectura
                .AsNoTracking()
                .Where(e => libroIds.Contains(e.LibroId))
                .OrderBy(e => e.LibroId)
                .ThenBy(e => e.Numero)
                .ToListAsync(cancellationToken);

        var travesias = entradas
            .Select(e => new TravesiaExportada(
                e.Id,
                e.LibroId,
                titulos.GetValueOrDefault(e.LibroId, ""),
                e.Numero,
                e.PaginaInicio,
                e.PaginaFin,
                e.Resumen,
                e.Fecha))
            .ToList();

        var glosario = await _db.PalabrasGlosario
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .OrderBy(p => p.Palabra)
            .Select(p => new PalabraGlosarioExportada(p.Palabra, p.Definicion, p.LibroTitulo, p.Fecha))
            .ToListAsync(cancellationToken);

        var amistades = await _db.Amistades
            .AsNoTracking()
            .Where(a => a.SolicitanteId == usuarioId || a.ReceptorId == usuarioId)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        var otrosIds = amistades
            .Select(a => a.SolicitanteId == usuarioId ? a.ReceptorId : a.SolicitanteId)
            .Distinct()
            .ToList();

        var correos = otrosIds.Count == 0
            ? new Dictionary<string, string>()
            : await _db.Users
                .AsNoTracking()
                .Where(u => otrosIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? "", cancellationToken);

        var amistadesExportadas = amistades
            .Select(a =>
            {
                var otroId = a.SolicitanteId == usuarioId ? a.ReceptorId : a.SolicitanteId;
                return new AmistadExportada(
                    correos.GetValueOrDefault(otroId, ""),
                    a.Estado.ToString(),
                    a.FechaSolicitud,
                    a.FechaRespuesta);
            })
            .ToList();

        var cuenta = new DatosCuentaExportados(
            usuario.Id,
            usuario.Email,
            usuario.UserName,
            usuario.EmailConfirmed,
            usuario.PhoneNumber,
            usuario.PhoneNumberConfirmed,
            usuario.TwoFactorEnabled,
            usuario.LockoutEnd,
            usuario.CompartirEstantes);

        var bloqueos = await _db.Bloqueos
            .AsNoTracking()
            .Where(b => b.UsuarioId == usuarioId)
            .OrderBy(b => b.Fecha)
            .ToListAsync(cancellationToken);

        var idsBloqueados = bloqueos.Select(b => b.BloqueadoId).Distinct().ToList();
        var correosBloqueados = idsBloqueados.Count == 0
            ? new Dictionary<string, string>()
            : await _db.Users
                .AsNoTracking()
                .Where(u => idsBloqueados.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? "", cancellationToken);

        var bloqueosExportados = bloqueos
            .Select(b => new BloqueoExportado(
                correosBloqueados.GetValueOrDefault(b.BloqueadoId, ""),
                b.Fecha))
            .ToList();

        return new ExportacionDatos(cuenta, librosExportados, travesias, glosario, amistadesExportadas, bloqueosExportados);
    }
}
