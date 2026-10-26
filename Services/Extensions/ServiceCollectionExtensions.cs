using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;

namespace Otium.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static ProveedorBaseDatos AddBaseDeDatos(
        this IServiceCollection servicios,
        string cadenaConexion,
        IConfiguration configuracion)
    {
        var configuracionBaseDatos = ConfiguracionBaseDatos.Crear(cadenaConexion, configuracion);

        Action<DbContextOptionsBuilder> opciones = configuracionBaseDatos.Proveedor switch
        {
            ProveedorBaseDatos.PostgreSql => builder => builder
                .UseNpgsql(configuracionBaseDatos.CadenaConexion, npgsql => npgsql.EnableRetryOnFailure()),
            _ => builder => builder.UseSqlite(configuracionBaseDatos.CadenaConexion)
        };

        servicios.AddDbContext<ApplicationDbContext>(opciones);
        servicios.AddDbContextFactory<ApplicationDbContext>(opciones, ServiceLifetime.Scoped);

        return configuracionBaseDatos.Proveedor;
    }
}
