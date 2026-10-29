using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;
using Otium.Models.DTOs;

namespace Otium.Services;

public interface IGlosarioService
{
    Task<List<PalabraGlosarioDTO>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta y guarda una palabra o frase. Si <paramref name="incluirContextoLectura"/> está activo y hay
    /// libro, la definición incluye el matiz "En el contexto de la lectura: ..." solo cuando aporta valor.
    /// </summary>
    Task<(bool Exito, string Mensaje, PalabraGlosarioDTO? Palabra)> BuscarYGuardarAsync(
        string usuarioId,
        string texto,
        string libroTitulo,
        bool incluirContextoLectura,
        CancellationToken cancellationToken = default);

    Task<bool> EliminarAsync(string usuarioId, int palabraId, CancellationToken cancellationToken = default);
    Task<bool> EditarDefinicionAsync(string usuarioId, int palabraId, string nuevaDefinicion, CancellationToken cancellationToken = default);
    Task EliminarTodosDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);
}

public class GlosarioService : IGlosarioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;
    private readonly IDefinicionClient _definiciones;
    private readonly ILogger<GlosarioService> _logger;
    private readonly ILimiteIntentosService _limites;

    public GlosarioService(
        IDbContextFactory<ApplicationDbContext> fabrica,
        IDefinicionClient definiciones,
        ILimiteIntentosService limites,
        ILogger<GlosarioService> logger)
    {
        _fabrica = fabrica;
        _definiciones = definiciones;
        _logger = logger;
        _limites = limites;
    }

    public async Task<List<PalabraGlosarioDTO>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
        return await db.PalabrasGlosario
            .Where(p => p.UsuarioId == usuarioId)
            .OrderBy(p => p.Palabra)
            .Select(PalabraGlosarioDTO.Proyeccion)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Exito, string Mensaje, PalabraGlosarioDTO? Palabra)> BuscarYGuardarAsync(
        string usuarioId,
        string texto,
        string libroTitulo,
        bool incluirContextoLectura,
        CancellationToken cancellationToken = default)
    {
        var entrada = (texto ?? "").Trim();
        if (string.IsNullOrEmpty(entrada))
        {
            return (false, "Escribe una palabra o frase.", null);
        }

        if (entrada.Length > 500)
        {
            return (false, "El texto no puede superar 500 caracteres.", null);
        }

        var titulo = incluirContextoLectura ? (libroTitulo ?? "").Trim() : "";
        if (titulo.Length > 300)
        {
            return (false, "El título del libro no puede superar 300 caracteres.", null);
        }

        var esPalabraUnica = entrada
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 1
            && entrada.Length <= 100;
        if (esPalabraUnica)
        {
            var existenteUnico = await ObtenerPalabraExistenteAsync(usuarioId, entrada, cancellationToken);
            if (existenteUnico is not null)
            {
                return (true, "Ya está en tu glosario.", PalabraGlosarioDTO.DesdeEntidad(existenteUnico));
            }
        }

        if (!_definiciones.EstaConfigurado)
        {
            return (false, "El servicio de definiciones no está configurado en el servidor.", null);
        }

        if (!_limites.IntentarCuotaDiaria($"glosario:{usuarioId}", 50))
        {
            return (false, "Alcanzaste el límite diario de definiciones. Inténtalo mañana.", null);
        }

        try
        {
            var resultado = await _definiciones.DefinirAsync(entrada, titulo, incluirContextoLectura, cancellationToken);
            if (resultado is null)
            {
                return (false, "No encontré una definición para esa palabra.", null);
            }

            var definicion = resultado.Definicion;
            if (string.IsNullOrWhiteSpace(definicion))
            {
                return (false, "No encontré una definición para esa palabra.", null);
            }

            if (definicion.Length > 1000)
            {
                return (false, "La definición recibida es demasiado larga.", null);
            }

            var textoFinal = resultado.ContextoLectura.Length == 0
                ? definicion
                : $"{definicion}\n\nEn el contexto de la lectura: {resultado.ContextoLectura}";

            var existenteTermino = await ObtenerPalabraExistenteAsync(usuarioId, resultado.Termino, cancellationToken);
            if (existenteTermino is not null)
            {
                var avisoDuplicado = esPalabraUnica
                    ? "Ya está en tu glosario."
                    : $"Término analizado: {resultado.Termino}. Ya está en tu glosario.";
                return (true, avisoDuplicado, PalabraGlosarioDTO.DesdeEntidad(existenteTermino));
            }

            var nueva = new PalabraGlosario
            {
                UsuarioId = usuarioId,
                Palabra = resultado.Termino,
                Definicion = textoFinal,
                LibroTitulo = titulo
            };

            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
            db.PalabrasGlosario.Add(nueva);
            await db.SaveChangesAsync(cancellationToken);

            var aviso = esPalabraUnica && resultado.Termino.Equals(entrada.Trim(), StringComparison.OrdinalIgnoreCase)
                ? "Guardada en tu glosario."
                : $"Término analizado: {resultado.Termino}. Guardada en tu glosario.";
            return (true, aviso, PalabraGlosarioDTO.DesdeEntidad(nueva));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "La consulta al servicio de definiciones agotó el tiempo de espera.");
            return (false, "La consulta tardó demasiado. Inténtalo de nuevo.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar el servicio de definiciones para el glosario.");
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
        var normalizada = palabra.ToLowerInvariant();

        return await db.PalabrasGlosario
            .Where(p => p.UsuarioId == usuarioId && p.Palabra.ToLower() == normalizada)
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
