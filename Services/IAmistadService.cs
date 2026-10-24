using Otium.Models;

namespace Otium.Services;

public record SolicitudPendiente(int Id, string SolicitanteId, string Correo);
public record AmigoInfo(string UsuarioId, string Correo, bool ComparteEstantes);
public record BloqueadoInfo(string UsuarioId, string Correo);

public interface IAmistadService
{
    Task<string> ObtenerOCrearCodigoAsync(string usuarioId);
    Task<(bool Exito, string Mensaje)> EnviarSolicitudAsync(string usuarioId, string codigoIngresado);
    Task<List<SolicitudPendiente>> ObtenerSolicitudesPendientesAsync(string usuarioId);
    Task ResponderSolicitudAsync(int solicitudId, string usuarioId, bool aceptar);
    Task<List<AmigoInfo>> ObtenerAmigosAsync(string usuarioId);
    Task<AmigoInfo?> ObtenerAmigoAsync(string usuarioId, string amigoId);
    Task<bool> SonAmigosAsync(string usuarioId, string otroId);
    Task<bool> PuedeVerEstantesAsync(string usuarioId, string amigoId);
    Task<bool> ObtenerCompartirEstantesAsync(string usuarioId);
    Task<bool> EstablecerCompartirEstantesAsync(string usuarioId, bool comparte);
    Task<(bool Exito, string Mensaje)> EliminarAmigoAsync(string usuarioId, string amigoId);
    Task<(bool Exito, string Mensaje)> BloquearAsync(string usuarioId, string bloqueadoId);
    Task<(bool Exito, string Mensaje)> DesbloquearAsync(string usuarioId, string bloqueadoId);
    Task<List<BloqueadoInfo>> ObtenerBloqueadosAsync(string usuarioId);
    Task EliminarTodosDeUsuarioAsync(string usuarioId);
}
