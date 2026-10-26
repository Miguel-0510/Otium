using Microsoft.EntityFrameworkCore;
using Otium.Services.Extensions;

namespace Otium.Data;

/// <summary>
/// Prepara el esquema al arrancar. Reintenta ante fallos transitorios de red
/// (habituales al desplegar en plataformas gestionadas) y, si no lo consigue,
/// deja que la excepción detenga el arranque: servir con el esquema incompleto
/// provoca errores 500 en todos los flujos de Identity.
/// </summary>
public static class BaseDatosInicializador
{
    private const int IntentosMaximos = 5;

    public static async Task InicializarAsync(
        IServiceProvider proveedorServicios,
        ProveedorBaseDatos proveedor,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        for (var intento = 1; intento <= IntentosMaximos; intento++)
        {
            try
            {
                using var ambito = proveedorServicios.CreateScope();
                var db = ambito.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await CrearEsquemaAsync(db, proveedor, logger, cancellationToken);
                return;
            }
            catch (Exception ex) when (intento < IntentosMaximos)
            {
                var espera = TimeSpan.FromSeconds(Math.Pow(2, intento));
                logger.LogWarning(
                    ex,
                    "No se pudo preparar la base de datos {Proveedor} (intento {Intento}/{Total}). Reintentando en {Segundos} s.",
                    proveedor,
                    intento,
                    IntentosMaximos,
                    espera.TotalSeconds);

                await Task.Delay(espera, cancellationToken);
            }
        }
    }

    private static async Task CrearEsquemaAsync(
        ApplicationDbContext db,
        ProveedorBaseDatos proveedor,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (proveedor == ProveedorBaseDatos.PostgreSql)
        {
            // Las migraciones del proyecto se generaron con SQLite (tipos TEXT/INTEGER/BLOB
            // y anotaciones Sqlite:Autoincrement) y no son portables a PostgreSQL.
            // Hasta disponer de un ensamblado de migraciones propio para Npgsql, el esquema
            // se crea desde el modelo con el proveedor correcto.
            var creado = await db.Database.EnsureCreatedAsync(cancellationToken);
            if (creado)
            {
                logger.LogInformation("Esquema de PostgreSQL creado desde el modelo.");
                return;
            }

            // EnsureCreated no modifica una base existente. Verificamos que el esquema
            // realmente contiene las tablas de Identity para no arrancar con uno incompleto.
            _ = await db.Users.Select(usuario => usuario.Id).FirstOrDefaultAsync(cancellationToken);
            logger.LogInformation("El esquema de PostgreSQL ya existía; no se aplicaron cambios.");
            return;
        }

        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migraciones de SQLite aplicadas correctamente.");
    }
}
