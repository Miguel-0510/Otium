namespace Otium.Services;

public interface ICodigoAmigoService
{
    Task<string> GenerarCodigoUnicoAsync(CancellationToken cancellationToken = default);
}
