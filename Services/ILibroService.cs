using Otium.Models;

namespace Otium.Services;

public interface ILibroService
{
    Task<List<Libro>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<(bool Exito, string Mensaje, Libro? Libro)> AgregarAsync(string usuarioId, Libro libro, CancellationToken cancellationToken = default);
    Task<(bool Exito, string Mensaje)> ActualizarAsync(string usuarioId, Libro libro, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(string usuarioId, int libroId, CancellationToken cancellationToken = default);
    Task EliminarTodosDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);
}
