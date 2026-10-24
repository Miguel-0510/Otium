namespace Otium.Services;

public interface IRecuperacionContrasenaService
{
    Task SolicitarAsync(string email, Uri resetUri, CancellationToken cancellationToken = default);
}
