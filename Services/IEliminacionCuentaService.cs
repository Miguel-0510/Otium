namespace Otium.Services;

public sealed record ResultadoEliminacion(bool Exito, string Mensaje);

public interface IEliminacionCuentaService
{
    Task<ResultadoEliminacion> EliminarAsync(string usuarioId, string? password, CancellationToken cancellationToken = default);
}
