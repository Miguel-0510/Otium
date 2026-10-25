using Otium.Models;
using Otium.Models.DTOs;

namespace Otium.Services;

public interface ILibroService
{
    Task<List<LibroDTO>> ObtenerDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<(bool Exito, string Mensaje, LibroDTO? Libro)> AgregarAsync(string usuarioId, LibroCrearDTO datos, CancellationToken cancellationToken = default);
    Task<(bool Exito, string Mensaje)> ActualizarAsync(string usuarioId, LibroActualizarDTO datos, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(string usuarioId, int libroId, CancellationToken cancellationToken = default);
    Task EliminarTodosDeUsuarioAsync(string usuarioId, CancellationToken cancellationToken = default);
}
