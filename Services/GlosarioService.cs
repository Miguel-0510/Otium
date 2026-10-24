using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;

namespace Otium.Services;

public interface IGlosarioService
{
    Task<List<PalabraGlosario>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<(bool Exito, string Mensaje, PalabraGlosario? Palabra)> BuscarYGuardarAsync(string usuarioId, string palabra, string libroTitulo, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(string usuarioId, int palabraId, CancellationToken cancellationToken = default);
    Task EliminarTodosDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);
}

public class GlosarioService : IGlosarioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;
    private readonly IGeminiClient _gemini;
    private readonly ILogger<GlosarioService> _logger;
    private readonly ILimiteIntentosService _limites;

    public GlosarioService(
        IDbContextFactory<ApplicationDbContext> fabrica,
        IGeminiClient gemini,
        ILimiteIntentosService limites,
        ILogger<GlosarioService> logger)
    {
        _fabrica = fabrica;
        _gemini = gemini;
        _logger = logger;
        _limites = limites;
    }

    public async Task<List<PalabraGlosario>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
        return await db.PalabrasGlosario
            .Where(p => p.UsuarioId == usuarioId)
            .OrderBy(p => p.Palabra)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Exito, string Mensaje, PalabraGlosario? Palabra)> BuscarYGuardarAsync(
        string usuarioId,
        string palabra,
        string libroTitulo,
        CancellationToken cancellationToken = default)
    {
        var texto = (palabra ?? "").Trim();
        if (string.IsNullOrEmpty(texto))
        {
            return (false, "Escribe una palabra.", null);
        }

        if (!_gemini.EstaConfigurado)
        {
            return (false, "Falta configurar la llave de Gemini en el servidor.", null);
        }

        if (!_limites.IntentarCuotaDiaria($"glosario:{usuarioId}", 50))
        {
            return (false, "Alcanzaste el límite diario de definiciones. Inténtalo mañana.", null);
        }

        try
        {
            var definicion = await _gemini.DefinirAsync(texto, libroTitulo, cancellationToken);
            if (string.IsNullOrWhiteSpace(definicion))
            {
                return (false, "No encontré una definición para esa palabra.", null);
            }

            var nueva = new PalabraGlosario
            {
                UsuarioId = usuarioId,
                Palabra = texto,
                Definicion = definicion,
                LibroTitulo = libroTitulo ?? ""
            };

            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
            db.PalabrasGlosario.Add(nueva);
            await db.SaveChangesAsync(cancellationToken);

            return (true, "Guardada en tu glosario.", nueva);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar Gemini para el glosario.");
            return (false, "No pude consultar la definición en este momento. Inténtalo más tarde.", null);
        }
    }

    public async Task<bool> EliminarAsync(string usuarioId, int palabraId, CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

        var filas = await db.PalabrasGlosario
            .Where(p => p.Id == palabraId && p.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(cancellationToken);

        return filas > 0;
    }

    public async Task EliminarTodosDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
        await db.PalabrasGlosario
            .Where(p => p.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
