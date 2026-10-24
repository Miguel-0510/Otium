using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Otium.Data;

namespace Otium.Services;

public sealed class EliminacionCuentaService : IEliminacionCuentaService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EliminacionCuentaService> _logger;

    public EliminacionCuentaService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ILogger<EliminacionCuentaService> logger)
    {
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    public async Task<ResultadoEliminacion> EliminarAsync(string usuarioId, string? password, CancellationToken cancellationToken = default)
    {
        _db.ChangeTracker.Clear();

        var usuario = await _userManager.FindByIdAsync(usuarioId);
        if (usuario is null)
        {
            return new ResultadoEliminacion(false, "No se encontró la cuenta.");
        }

        if (await _userManager.HasPasswordAsync(usuario))
        {
            if (string.IsNullOrEmpty(password) || !await _userManager.CheckPasswordAsync(usuario, password))
            {
                return new ResultadoEliminacion(false, "La contraseña no es correcta.");
            }
        }

        await using var transaccion = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _db.PalabrasGlosario
                .Where(p => p.UsuarioId == usuarioId)
                .ExecuteDeleteAsync(cancellationToken);

            await _db.Amistades
                .Where(a => a.SolicitanteId == usuarioId || a.ReceptorId == usuarioId)
                .ExecuteDeleteAsync(cancellationToken);

            await _db.Bloqueos
                .Where(b => b.UsuarioId == usuarioId || b.BloqueadoId == usuarioId)
                .ExecuteDeleteAsync(cancellationToken);

            var libroIds = await _db.Libros
                .Where(l => l.UsuarioId == usuarioId)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            if (libroIds.Count > 0)
            {
                await _db.EntradasLectura
                    .Where(e => libroIds.Contains(e.LibroId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _db.Libros
                .Where(l => l.UsuarioId == usuarioId)
                .ExecuteDeleteAsync(cancellationToken);

            var resultado = await _userManager.DeleteAsync(usuario);
            if (!resultado.Succeeded)
            {
                await transaccion.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "No se pudo eliminar la cuenta {UsuarioId}: {Errores}",
                    usuarioId,
                    string.Join(" | ", resultado.Errors.Select(e => e.Description)));
                return new ResultadoEliminacion(false, "No se pudo eliminar la cuenta.");
            }

            await transaccion.CommitAsync(cancellationToken);
            return new ResultadoEliminacion(true, "");
        }
        catch (Exception ex)
        {
            await transaccion.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error al eliminar la cuenta {UsuarioId}.", usuarioId);
            return new ResultadoEliminacion(false, "No se pudo eliminar la cuenta.");
        }
    }
}
