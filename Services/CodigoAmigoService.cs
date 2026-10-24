using Microsoft.EntityFrameworkCore;
using Otium.Data;

namespace Otium.Services;

public sealed class CodigoAmigoService : ICodigoAmigoService
{
    private const int MaxIntentos = 20;

    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;
    private readonly ICodigoAmigoGenerator _generador;

    public CodigoAmigoService(IDbContextFactory<ApplicationDbContext> fabrica, ICodigoAmigoGenerator generador)
    {
        _fabrica = fabrica;
        _generador = generador;
    }

    public async Task<string> GenerarCodigoUnicoAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _fabrica.CreateDbContextAsync(cancellationToken);

        for (var intento = 0; intento < MaxIntentos; intento++)
        {
            var codigo = _generador.Generar();

            var existe = await db.Users.AnyAsync(u => u.CodigoAmigo == codigo, cancellationToken);
            if (!existe)
            {
                return codigo;
            }
        }

        throw new InvalidOperationException("No se pudo generar un código de amigo único.");
    }
}
