using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;

namespace Otium.Services;

public class AmistadService : IAmistadService
{
    private const int MaxIntentosCodigo = 5;
    private const string MensajeSolicitudNoEnviada = "No se pudo enviar la solicitud. Revisa el código e inténtalo de nuevo.";

    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICodigoAmigoService _codigos;
    private readonly ILimiteIntentosService _limites;
    private readonly ILogger<AmistadService> _logger;

    public AmistadService(
        IDbContextFactory<ApplicationDbContext> fabrica,
        UserManager<ApplicationUser> userManager,
        ICodigoAmigoService codigos,
        ILimiteIntentosService limites,
        ILogger<AmistadService> logger)
    {
        _fabrica = fabrica;
        _userManager = userManager;
        _codigos = codigos;
        _limites = limites;
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
        if (!_limites.Intentar($"amistad:{usuarioId}", 10, TimeSpan.FromHours(1)))
        {
            return (false, "Demasiados intentos. Espera un momento antes de volver a probar.");
        }

        var codigo = (codigoIngresado ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(codigo))
            return (false, "Escribe un código.");

        await using var db = await _fabrica.CreateDbContextAsync();

        var receptor = await db.Users.FirstOrDefaultAsync(u => u.CodigoAmigo == codigo);
        if (receptor is null)
            return (false, MensajeSolicitudNoEnviada);

        if (receptor.Id == usuarioId)
            return (false, "Ese es tu propio código.");

        var bloqueado = await db.Bloqueos.AnyAsync(b =>
            (b.UsuarioId == usuarioId && b.BloqueadoId == receptor.Id) ||
            (b.UsuarioId == receptor.Id && b.BloqueadoId == usuarioId));

        if (bloqueado)
            return (false, MensajeSolicitudNoEnviada);

        var yaExiste = await db.Amistades.AnyAsync(a =>
            (a.SolicitanteId == usuarioId && a.ReceptorId == receptor.Id) ||
            (a.SolicitanteId == receptor.Id && a.ReceptorId == usuarioId));

        if (yaExiste)
            return (false, MensajeSolicitudNoEnviada);

        db.Amistades.Add(new Amistad
        {
            SolicitanteId = usuarioId,
            ReceptorId = receptor.Id,
            ClavePar = ConstruirClavePar(usuarioId, receptor.Id)
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "No se pudo guardar la solicitud de amistad entre {Solicitante} y {Receptor}.", usuarioId, receptor.Id);
            return (false, MensajeSolicitudNoEnviada);
        }

        return (true, "Solicitud enviada.");
    }

    private static string ConstruirClavePar(string usuarioId, string otroId) =>
        string.CompareOrdinal(usuarioId, otroId) < 0
            ? $"{usuarioId}|{otroId}"
            : $"{otroId}|{usuarioId}";

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
            .Select(u => new AmigoInfo(u.Id, u.Email ?? u.UserName ?? "", u.CompartirEstantes))
            .ToListAsync();
    }

    public async Task<AmigoInfo?> ObtenerAmigoAsync(string usuarioId, string amigoId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var esAmigo = await db.Amistades.AnyAsync(a =>
            a.Estado == EstadoAmistad.Aceptada &&
            ((a.SolicitanteId == usuarioId && a.ReceptorId == amigoId) ||
             (a.SolicitanteId == amigoId && a.ReceptorId == usuarioId)));

        if (!esAmigo)
        {
            return null;
        }

        var bloqueado = await db.Bloqueos.AnyAsync(b =>
            (b.UsuarioId == usuarioId && b.BloqueadoId == amigoId) ||
            (b.UsuarioId == amigoId && b.BloqueadoId == usuarioId));

        if (bloqueado)
        {
            return null;
        }

        return await db.Users
            .Where(u => u.Id == amigoId)
            .Select(u => new AmigoInfo(u.Id, u.Email ?? u.UserName ?? "", u.CompartirEstantes))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SonAmigosAsync(string usuarioId, string otroId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        return await db.Amistades.AnyAsync(a =>
            a.Estado == EstadoAmistad.Aceptada &&
            ((a.SolicitanteId == usuarioId && a.ReceptorId == otroId) ||
             (a.SolicitanteId == otroId && a.ReceptorId == usuarioId)));
    }

    public async Task<bool> PuedeVerEstantesAsync(string usuarioId, string amigoId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var bloqueado = await db.Bloqueos.AnyAsync(b =>
            (b.UsuarioId == usuarioId && b.BloqueadoId == amigoId) ||
            (b.UsuarioId == amigoId && b.BloqueadoId == usuarioId));

        if (bloqueado)
        {
            return false;
        }

        var comparte = await db.Users
            .Where(u => u.Id == amigoId)
            .Select(u => u.CompartirEstantes)
            .FirstOrDefaultAsync();

        if (!comparte)
        {
            return false;
        }

        return await db.Amistades.AnyAsync(a =>
            a.Estado == EstadoAmistad.Aceptada &&
            ((a.SolicitanteId == usuarioId && a.ReceptorId == amigoId) ||
             (a.SolicitanteId == amigoId && a.ReceptorId == usuarioId)));
    }

    public async Task<bool> ObtenerCompartirEstantesAsync(string usuarioId)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId);
        return usuario?.CompartirEstantes ?? false;
    }

    public async Task<bool> EstablecerCompartirEstantesAsync(string usuarioId, bool comparte)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId);
        if (usuario is null)
        {
            return false;
        }

        usuario.CompartirEstantes = comparte;
        var resultado = await _userManager.UpdateAsync(usuario);

        if (!resultado.Succeeded)
        {
            _logger.LogError(
                "No se pudo actualizar la preferencia de compartir estantes de {UsuarioId}: {Errores}",
                usuarioId,
                string.Join(" | ", resultado.Errors.Select(e => e.Description)));
        }

        return resultado.Succeeded;
    }

    public async Task<(bool Exito, string Mensaje)> EliminarAmigoAsync(string usuarioId, string amigoId)
    {
        if (usuarioId == amigoId)
        {
            return (false, "No puedes eliminarte a ti mismo.");
        }

        await using var db = await _fabrica.CreateDbContextAsync();

        var filas = await db.Amistades
            .Where(a => a.Estado == EstadoAmistad.Aceptada &&
                        ((a.SolicitanteId == usuarioId && a.ReceptorId == amigoId) ||
                         (a.SolicitanteId == amigoId && a.ReceptorId == usuarioId)))
            .ExecuteDeleteAsync();

        return filas > 0
            ? (true, "Amigo eliminado.")
            : (false, "No se encontró esa amistad.");
    }

    public async Task<(bool Exito, string Mensaje)> BloquearAsync(string usuarioId, string bloqueadoId)
    {
        if (usuarioId == bloqueadoId)
        {
            return (false, "No puedes bloquearte a ti mismo.");
        }

        await using var db = await _fabrica.CreateDbContextAsync();

        if (!await db.Users.AnyAsync(u => u.Id == bloqueadoId))
        {
            return (false, "No se encontró ese usuario.");
        }

        await using var transaccion = await db.Database.BeginTransactionAsync();

        try
        {
            await db.Amistades
                .Where(a => (a.SolicitanteId == usuarioId && a.ReceptorId == bloqueadoId) ||
                            (a.SolicitanteId == bloqueadoId && a.ReceptorId == usuarioId))
                .ExecuteDeleteAsync();

            var yaBloqueado = await db.Bloqueos.AnyAsync(b =>
                b.UsuarioId == usuarioId && b.BloqueadoId == bloqueadoId);

            if (!yaBloqueado)
            {
                db.Bloqueos.Add(new Bloqueo
                {
                    UsuarioId = usuarioId,
                    BloqueadoId = bloqueadoId
                });
                await db.SaveChangesAsync();
            }

            await transaccion.CommitAsync();
            return (true, "Usuario bloqueado.");
        }
        catch (DbUpdateException ex)
        {
            await transaccion.RollbackAsync();
            _logger.LogWarning(ex, "No se pudo bloquear a {BloqueadoId} por {UsuarioId}.", bloqueadoId, usuarioId);
            return (false, "No se pudo bloquear al usuario.");
        }
    }

    public async Task<(bool Exito, string Mensaje)> DesbloquearAsync(string usuarioId, string bloqueadoId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var filas = await db.Bloqueos
            .Where(b => b.UsuarioId == usuarioId && b.BloqueadoId == bloqueadoId)
            .ExecuteDeleteAsync();

        return filas > 0
            ? (true, "Usuario desbloqueado.")
            : (false, "Ese usuario no estaba bloqueado.");
    }

    public async Task<List<BloqueadoInfo>> ObtenerBloqueadosAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        return await db.Bloqueos
            .Where(b => b.UsuarioId == usuarioId)
            .Join(db.Users, b => b.BloqueadoId, u => u.Id,
                (b, u) => new { u.Id, u.Email, u.UserName })
            .OrderBy(b => b.Email ?? b.UserName)
            .Select(b => new BloqueadoInfo(b.Id, b.Email ?? b.UserName ?? ""))
            .ToListAsync();
    }

    public async Task EliminarTodosDeUsuarioAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        await db.Amistades
            .Where(a => a.SolicitanteId == usuarioId || a.ReceptorId == usuarioId)
            .ExecuteDeleteAsync();

        await db.Bloqueos
            .Where(b => b.UsuarioId == usuarioId || b.BloqueadoId == usuarioId)
            .ExecuteDeleteAsync();
    }
}

