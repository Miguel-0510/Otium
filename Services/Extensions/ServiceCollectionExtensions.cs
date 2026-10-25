using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;

namespace Otium.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBaseDeDatos(
        this IServiceCollection servicios,
        string cadenaConexion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cadenaConexion);

        Action<DbContextOptionsBuilder> opcionesSqlite = options =>
            options.UseSqlite(cadenaConexion);

        servicios.AddDbContext<ApplicationDbContext>(opcionesSqlite);
        servicios.AddDbContextFactory<ApplicationDbContext>(opcionesSqlite, ServiceLifetime.Scoped);

        return servicios;
    }
}
