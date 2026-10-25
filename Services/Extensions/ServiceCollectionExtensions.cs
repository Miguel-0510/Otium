using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;

namespace Otium.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBaseDeDatos(
        this IServiceCollection servicios,
        string cadenaConexion,
        IConfiguration configuracion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cadenaConexion);

        var esPostgreSql = string.Equals(configuracion["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase)
            || cadenaConexion.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || cadenaConexion.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || cadenaConexion.Contains("User ID=", StringComparison.OrdinalIgnoreCase);

        if (esPostgreSql)
        {
            Action<DbContextOptionsBuilder> opcionesPostgres = options =>
                options.UseNpgsql(cadenaConexion);

            servicios.AddDbContext<ApplicationDbContext>(opcionesPostgres);
            servicios.AddDbContextFactory<ApplicationDbContext>(opcionesPostgres, ServiceLifetime.Scoped);
        }
        else
        {
            Action<DbContextOptionsBuilder> opcionesSqlite = options =>
                options.UseSqlite(cadenaConexion);

            servicios.AddDbContext<ApplicationDbContext>(opcionesSqlite);
            servicios.AddDbContextFactory<ApplicationDbContext>(opcionesSqlite, ServiceLifetime.Scoped);
        }

        return servicios;
    }
}
