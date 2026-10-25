using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Otium.Data;
using Otium.Models;
using Otium.Models.DTOs;

namespace Otium.Services;

public class LibroService : ILibroService
{
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;
    private readonly ILogger<LibroService> _logger;

    public LibroService(IDbContextFactory<ApplicationDbContext> fabrica, ILogger<LibroService> logger)
    {
        _fabrica = fabrica;
        _logger = logger;
    }

    public async Task<List<LibroDTO>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
            return await db.Libros
                .Where(l => l.UsuarioId == usuarioId)
                .OrderBy(l => l.Id)
                .Select(l => new LibroDTO
                {
                    Id = l.Id,
                    UsuarioId = l.UsuarioId,
                    Titulo = l.Titulo,
                    Autor = l.Autor,
                    Estante = l.Estante,
                    Puntuacion = l.Puntuacion,
                    Resena = l.Resena,
                    PortadaUrl = l.PortadaUrl,
                    IdExterno = l.IdExterno
                })
                .ToListAsync(cancellationToken);
        }
        finally
        {
            cronometro.Stop();
            _logger.LogDebug("ObtenerDeUsuarioAsync({UsuarioId}) completado en {ElapsedMs} ms", usuarioId, cronometro.ElapsedMilliseconds);
        }
    }

    public async Task<LibroPaginaDTO> ObtenerDeUsuarioPaginadoAsync(
        string usuarioId,
        int pagina = 1,
        int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 1) tamanoPagina = 20;
        if (tamanoPagina > 100) tamanoPagina = 100;

        var cronometro = Stopwatch.StartNew();
        try
        {
            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

            var consulta = db.Libros.Where(l => l.UsuarioId == usuarioId).OrderBy(l => l.Id);
            var total = await consulta.LongCountAsync(cancellationToken);

            var datos = await consulta
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .Select(l => new LibroDTO
                {
                    Id = l.Id,
                    UsuarioId = l.UsuarioId,
                    Titulo = l.Titulo,
                    Autor = l.Autor,
                    Estante = l.Estante,
                    Puntuacion = l.Puntuacion,
                    Resena = l.Resena,
                    PortadaUrl = l.PortadaUrl,
                    IdExterno = l.IdExterno
                })
                .ToListAsync(cancellationToken);

            return new LibroPaginaDTO
            {
                Datos = datos,
                Total = (int)total,
                Pagina = pagina,
                TamanoPagina = tamanoPagina
            };
        }
        finally
        {
            cronometro.Stop();
            _logger.LogDebug("ObtenerDeUsuarioPaginadoAsync({UsuarioId}, pagina={Pagina}, tamano={TamanoPagina}) completado en {ElapsedMs} ms",
                usuarioId, pagina, tamanoPagina, cronometro.ElapsedMilliseconds);
        }
    }

    public async Task<(bool Exito, string Mensaje, LibroDTO? Libro)> AgregarAsync(
        string usuarioId,
        LibroCrearDTO datos,
        CancellationToken cancellationToken = default)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            var error = NormalizarYValidar(datos);
            if (error is not null)
            {
                return (false, error, null);
            }

            var libro = new Libro
            {
                UsuarioId = usuarioId,
                Titulo = datos.Titulo,
                Autor = datos.Autor,
                Estante = datos.Estante,
                Puntuacion = 0,
                Resena = "",
                PortadaUrl = datos.PortadaUrl ?? "",
                IdExterno = datos.IdExterno ?? ""
            };

            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
            db.Libros.Add(libro);
            await db.SaveChangesAsync(cancellationToken);

            var dto = new LibroDTO
            {
                Id = libro.Id,
                UsuarioId = libro.UsuarioId,
                Titulo = libro.Titulo,
                Autor = libro.Autor,
                Estante = libro.Estante,
                Puntuacion = libro.Puntuacion,
                Resena = libro.Resena,
                PortadaUrl = libro.PortadaUrl,
                IdExterno = libro.IdExterno
            };

            return (true, "", dto);
        }
        finally
        {
            cronometro.Stop();
            _logger.LogDebug("AgregarAsync({UsuarioId}) completado en {ElapsedMs} ms", usuarioId, cronometro.ElapsedMilliseconds);
        }
    }

    public async Task<(bool Exito, string Mensaje)> ActualizarAsync(
        string usuarioId,
        LibroActualizarDTO datos,
        CancellationToken cancellationToken = default)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            var error = NormalizarYValidar(datos);
            if (error is not null)
            {
                return (false, error);
            }

            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

            var existente = await db.Libros
                .FirstOrDefaultAsync(l => l.Id == datos.Id && l.UsuarioId == usuarioId, cancellationToken);

            if (existente is null)
            {
                return (false, "No se encontró el libro.");
            }

            existente.Titulo = datos.Titulo;
            existente.Autor = datos.Autor;
            existente.Estante = datos.Estante;
            existente.Puntuacion = datos.Puntuacion;
            existente.Resena = datos.Resena ?? "";
            existente.PortadaUrl = datos.PortadaUrl ?? "";
            existente.IdExterno = datos.IdExterno ?? "";

            await db.SaveChangesAsync(cancellationToken);
            return (true, "");
        }
        finally
        {
            cronometro.Stop();
            _logger.LogDebug("ActualizarAsync({UsuarioId}, libro={LibroId}) completado en {ElapsedMs} ms",
                usuarioId, datos.Id, cronometro.ElapsedMilliseconds);
        }
    }

    public async Task<bool> EliminarAsync(string usuarioId, int libroId, CancellationToken cancellationToken = default)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

            var filas = await db.Libros
                .Where(l => l.Id == libroId && l.UsuarioId == usuarioId)
                .ExecuteDeleteAsync(cancellationToken);

            return filas > 0;
        }
        finally
        {
            cronometro.Stop();
            _logger.LogDebug("EliminarAsync({UsuarioId}, libro={LibroId}) completado en {ElapsedMs} ms",
                usuarioId, libroId, cronometro.ElapsedMilliseconds);
        }
    }

    public async Task EliminarTodosDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);
            await db.Libros
                .Where(l => l.UsuarioId == usuarioId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        finally
        {
            cronometro.Stop();
            _logger.LogDebug("EliminarTodosDeUsuarioAsync({UsuarioId}) completado en {ElapsedMs} ms",
                usuarioId, cronometro.ElapsedMilliseconds);
        }
    }

    private static string? NormalizarYValidar(LibroCrearDTO datos)
    {
        datos.Titulo = (datos.Titulo ?? "").Trim();
        datos.Autor = (datos.Autor ?? "").Trim();
        datos.PortadaUrl = (datos.PortadaUrl ?? "").Trim();
        datos.IdExterno = (datos.IdExterno ?? "").Trim();

        if (!Enum.IsDefined(datos.Estante))
        {
            return "El estante seleccionado no es válido.";
        }

        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(datos);
        if (Validator.TryValidateObject(datos, contexto, resultados, validateAllProperties: true))
        {
            return null;
        }

        return resultados.FirstOrDefault()?.ErrorMessage ?? "Los datos del libro no son válidos.";
    }

    private static string? NormalizarYValidar(LibroActualizarDTO datos)
    {
        datos.Titulo = (datos.Titulo ?? "").Trim();
        datos.Autor = (datos.Autor ?? "").Trim();
        datos.Resena = (datos.Resena ?? "").Trim();
        datos.PortadaUrl = (datos.PortadaUrl ?? "").Trim();
        datos.IdExterno = (datos.IdExterno ?? "").Trim();

        if (!Enum.IsDefined(datos.Estante))
        {
            return "El estante seleccionado no es válido.";
        }

        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(datos);
        if (Validator.TryValidateObject(datos, contexto, resultados, validateAllProperties: true))
        {
            return null;
        }

        return resultados.FirstOrDefault()?.ErrorMessage ?? "Los datos del libro no son válidos.";
    }
}
