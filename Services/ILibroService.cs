using Otium.Models;

namespace Otium.Services;

public interface ILibroService
{
    Task<List<Libro>> ObtenerDeUsuarioAsync(string usuarioId);
    Task AgregarAsync(string usuarioId, Libro libro);
    Task<bool> ActualizarAsync(string usuarioId, Libro libro);
    Task<bool> EliminarAsync(string usuarioId, int libroId);
    Task EliminarTodosDeUsuarioAsync(string usuarioId);
}
