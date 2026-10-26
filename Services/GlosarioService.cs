using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;

namespace Otium.Services;

public interface IGlosarioService
{
    Task<List<PalabraGlosario>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<(bool Exito, string Mensaje, PalabraGlosario? Palabra)> BuscarYGuardarAsync(string usuarioId, string palabra, string libroTitulo, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(string usuarioId, int palabraId, CancellationToken cancellationToken = default);
    Task<bool> EditarDefinicionAsync(string usuarioId, int palabraId, string nuevaDefinicion, CancellationToken cancellationToken = default);
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

        if (texto.Length > 100)
        {
            return (false, "La palabra no puede superar 100 caracteres.", null);
        }

        var titulo = (libroTitulo ?? "").Trim();
        if (titulo.Length > 300)
        {
            return (false, "El título del libro no puede superar 300 caracteres.", null);
        }

        if (!_gemini.EstaConfigurado)
        {
            return (false, "Falta configurar la llave de Gemini en el servidor.", null);
        }

        if (!_limites.IntentarCuotaDiaria($"glosario:{usuarioId}", 50))
        {
            return (false, "Alcanzaste el límite diario de definiciones. Inténtalo mañana.", null);
        }

        var existente = await ObtenerPalabraExistenteAsync(usuarioId, texto, cancellationToken);
        if (existente is not null)
        {
            return (true, "Ya está en tu glosario.", existente);
        }

        try
        {
            var definicion = await _gemini.DefinirAsync(texto, titulo, cancellationToken);
            if (string.IsNullOrWhiteSpace(definicion))
            {
                return (false, "No encontré una definición para esa palabra.", null);
            }

            if (definicion.Length > 1000)
            {
                return (false, "La definición recibida es demasiado larga.", null);
            }

            var nueva = new PalabraGlosario
            {
                UsuarioId = usuarioId,
                Palabra = texto,
                Definicion = definicion,
                LibroTitulo = titulo
            };

            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
            db.PalabrasGlosario.Add(nueva);
            await db.SaveChangesAsync(cancellationToken);

            return (true, "Guardada en tu glosario.", nueva);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "La consulta a Gemini agotó el tiempo de espera.");
            return (false, "La consulta tardó demasiado. Inténtalo de nuevo.", null);
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

    private async Task<PalabraGlosario?> ObtenerPalabraExistenteAsync(string usuarioId, string palabra, CancellationToken cancellationToken)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
        return await db.PalabrasGlosario
            .Where(p => p.UsuarioId == usuarioId && EF.Functions.Collate(p.Palabra, "NOCASE") == palabra)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EditarDefinicionAsync(string usuarioId, int palabraId, string nuevaDefinicion, CancellationToken cancellationToken = default)
    {
        var texto = (nuevaDefinicion ?? "").Trim();
        if (string.IsNullOrEmpty(texto) || texto.Length > 1000)
        {
            return false;
        }

        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
        var filas = await db.PalabrasGlosario
            .Where(p => p.Id == palabraId && p.UsuarioId == usuarioId)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.Definicion, texto), cancellationToken);

        return filas > 0;
    }
}
