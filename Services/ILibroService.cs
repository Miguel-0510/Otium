using Otium.Models;

namespace Otium.Services;

public interface ILibroService
{
    Task<List<Libro>> ObtenerDeUsuarioAsync(string usuarioId);
    Task AgregarAsync(Libro libro);
    Task ActualizarAsync(Libro libro);
    Task EliminarAsync(Libro libro);
}
