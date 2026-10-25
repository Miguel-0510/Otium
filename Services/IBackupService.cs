using System.Threading;
using System.Threading.Tasks;

namespace Otium.Services;

public interface IBackupService
{
    Task<string> CrearRespaldoAsync(CancellationToken cancellationToken = default);
    Task<bool> VerificarIntegridadAsync(CancellationToken cancellationToken = default);
}
