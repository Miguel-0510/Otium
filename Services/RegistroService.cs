using Microsoft.AspNetCore.Identity;
using Otium.Data;

namespace Otium.Services;

public sealed class RegistroService : IRegistroService
{
    private const int MaxIntentos = 5;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICodigoAmigoService _codigos;
    private readonly ILogger<RegistroService> _logger;

    public RegistroService(
        UserManager<ApplicationUser> userManager,
        ICodigoAmigoService codigos,
        ILogger<RegistroService> logger)
    {
        _userManager = userManager;
        _codigos = codigos;
        _logger = logger;
    }

    public async Task<ResultadoRegistro> RegistrarAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var correo = (email ?? "").Trim();

        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            var codigo = await _codigos.GenerarCodigoUnicoAsync(cancellationToken);
            var usuario = new ApplicationUser
            {
                UserName = correo,
                Email = correo,
                CodigoAmigo = codigo
            };

            var resultado = await _userManager.CreateAsync(usuario, password);
            if (resultado.Succeeded)
            {
                return new ResultadoRegistro(true, Array.Empty<IdentityError>(), usuario);
            }

            if (await EsColisionDeCodigoAsync(correo, resultado, cancellationToken))
            {
                _logger.LogWarning("Colisión de CodigoAmigo en el intento {Intento}; reintentando.", intento);
                continue;
            }

            return new ResultadoRegistro(false, resultado.Errors.ToList(), null);
        }

        _logger.LogError("No se pudo registrar la cuenta tras {MaxIntentos} intentos por colisiones de código.", MaxIntentos);
        return new ResultadoRegistro(
            false,
            new[] { new IdentityError { Code = "CodigoAmigo", Description = "No se pudo crear la cuenta. Inténtalo de nuevo." } },
            null);
    }

    private async Task<bool> EsColisionDeCodigoAsync(string correo, IdentityResult resultado, CancellationToken cancellationToken)
    {
        if (!resultado.Errors.Any(e => e.Code == "DuplicateUserName"))
        {
            return false;
        }

        var existente = await _userManager.FindByEmailAsync(correo);
        return existente is null;
    }
}
