using Otium.Models;

namespace Otium.Services;

public interface IEntradaLecturaService
{
    Task<List<EntradaLectura>> ObtenerDeLibroAsync(string usuarioId, int libroId, CancellationToken cancellationToken = default);
    Task<(bool Exito, string Mensaje, EntradaLectura? Entrada)> AgregarAsync(
        string usuarioId,
        int libroId,
        int? paginaInicio,
        int? paginaFin,
        string resumen,
        CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(string usuarioId, int entradaId, CancellationToken cancellationToken = default);
}
