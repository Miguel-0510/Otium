using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;

namespace Otium.Services;

public class AmistadService : IAmistadService
{
    private const int MaxIntentosCodigo = 5;

    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICodigoAmigoService _codigos;
    private readonly ILogger<AmistadService> _logger;

    public AmistadService(
        IDbContextFactory<ApplicationDbContext> fabrica,
        UserManager<ApplicationUser> userManager,
        ICodigoAmigoService codigos,
        ILogger<AmistadService> logger)
    {
        _fabrica = fabrica;
        _userManager = userManager;
        _codigos = codigos;
        _logger = logger;
    }

    public async Task<string> ObtenerOCrearCodigoAsync(string usuarioId)
    {
        var user = await _userManager.FindByIdAsync(usuarioId);
        if (user is null) return "";

        if (!string.IsNullOrEmpty(user.CodigoAmigo))
            return user.CodigoAmigo;

        for (var intento = 1; intento <= MaxIntentosCodigo; intento++)
        {
            user.CodigoAmigo = await _codigos.GenerarCodigoUnicoAsync();
            var resultado = await _userManager.UpdateAsync(user);

            if (resultado.Succeeded)
                return user.CodigoAmigo;

            if (resultado.Errors.Any(e => e.Code == "DuplicateUserName"))
            {
                _logger.LogWarning("Colisión al asignar CodigoAmigo para {UsuarioId}; reintentando.", usuarioId);
                continue;
            }

            throw new InvalidOperationException(
                "No se pudo asignar el código de amigo: " + string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        throw new InvalidOperationException("No se pudo asignar un código de amigo único tras varios intentos.");
    }

    public async Task<(bool Exito, string Mensaje)> EnviarSolicitudAsync(string usuarioId, string codigoIngresado)
    {
        var codigo = (codigoIngresado ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(codigo))
            return (false, "Escribe un código.");

        await using var db = await _fabrica.CreateDbContextAsync();

        var receptor = await db.Users.FirstOrDefaultAsync(u => u.CodigoAmigo == codigo);
        if (receptor is null)
            return (false, "No encontré a nadie con ese código.");

        if (receptor.Id == usuarioId)
            return (false, "Ese es tu propio código.");

        var yaExiste = await db.Amistades.AnyAsync(a =>
            (a.SolicitanteId == usuarioId && a.ReceptorId == receptor.Id) ||
            (a.SolicitanteId == receptor.Id && a.ReceptorId == usuarioId));

        if (yaExiste)
            return (false, "Ya existe una solicitud o amistad con esa persona.");

        db.Amistades.Add(new Amistad
        {
            SolicitanteId = usuarioId,
            ReceptorId = receptor.Id
        });
        await db.SaveChangesAsync();

        return (true, "Solicitud enviada.");
    }

    public async Task<List<SolicitudPendiente>> ObtenerSolicitudesPendientesAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        return await db.Amistades
            .Where(a => a.ReceptorId == usuarioId && a.Estado == EstadoAmistad.Pendiente)
            .Join(db.Users, a => a.SolicitanteId, u => u.Id, (a, u) => new SolicitudPendiente(a.Id, a.SolicitanteId, u.Email ?? u.UserName ?? ""))
            .ToListAsync();
    }

    public async Task ResponderSolicitudAsync(int solicitudId, string usuarioId, bool aceptar)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var solicitud = await db.Amistades.FirstOrDefaultAsync(a =>
            a.Id == solicitudId && a.ReceptorId == usuarioId && a.Estado == EstadoAmistad.Pendiente);

        if (solicitud is null) return;

        solicitud.Estado = aceptar ? EstadoAmistad.Aceptada : EstadoAmistad.Rechazada;
        solicitud.FechaRespuesta = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<AmigoInfo>> ObtenerAmigosAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var amistades = await db.Amistades
            .Where(a => (a.SolicitanteId == usuarioId || a.ReceptorId == usuarioId) && a.Estado == EstadoAmistad.Aceptada)
            .ToListAsync();

        var idsAmigos = amistades
            .Select(a => a.SolicitanteId == usuarioId ? a.ReceptorId : a.SolicitanteId)
            .ToList();

        return await db.Users
            .Where(u => idsAmigos.Contains(u.Id))
            .Select(u => new AmigoInfo(u.Id, u.Email ?? u.UserName ?? ""))
            .ToListAsync();
    }

    public async Task EliminarTodosDeUsuarioAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        await db.Amistades
            .Where(a => a.SolicitanteId == usuarioId || a.ReceptorId == usuarioId)
            .ExecuteDeleteAsync();
    }
}

