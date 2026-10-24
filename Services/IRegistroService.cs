using Microsoft.AspNetCore.Identity;
using Otium.Data;

namespace Otium.Services;

public sealed record ResultadoRegistro(bool Exito, IReadOnlyList<IdentityError> Errores, ApplicationUser? Usuario);

public interface IRegistroService
{
    Task<ResultadoRegistro> RegistrarAsync(string email, string password, CancellationToken cancellationToken = default);
}
