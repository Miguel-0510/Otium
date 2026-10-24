using Otium.Models;

namespace Otium.Services;

public interface IEntradaLecturaService
{
    Task<List<EntradaLectura>> ObtenerDeLibroAsync(string usuarioId, int libroId);
    Task<EntradaLectura?> AgregarAsync(string usuarioId, int libroId, int? paginaInicio, int? paginaFin, string resumen);
    Task<bool> EliminarAsync(string usuarioId, int entradaId);
}
