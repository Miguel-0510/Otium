using Otium.Models;

namespace Otium.Services;

public record SolicitudPendiente(int Id, string SolicitanteId, string Correo);
public record AmigoInfo(string UsuarioId, string Correo);

public interface IAmistadService
{
    Task<string> ObtenerOCrearCodigoAsync(string usuarioId);
    Task<(bool Exito, string Mensaje)> EnviarSolicitudAsync(string usuarioId, string codigoIngresado);
    Task<List<SolicitudPendiente>> ObtenerSolicitudesPendientesAsync(string usuarioId);
    Task ResponderSolicitudAsync(int solicitudId, string usuarioId, bool aceptar);
    Task<List<AmigoInfo>> ObtenerAmigosAsync(string usuarioId);
    Task EliminarTodosDeUsuarioAsync(string usuarioId);
}
