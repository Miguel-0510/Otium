using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Otium.Data;

namespace Otium.Services;

public sealed class BackupService : IBackupService
{
    private readonly string _rutaBaseDeDatos;
    private readonly string _directorioRespaldo;
    private readonly int _maxRespaldos = 5;
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;

    public BackupService(
        IConfiguration configuracion,
        IWebHostEnvironment entorno,
        IDbContextFactory<ApplicationDbContext> fabrica)
    {
        _fabrica = fabrica;
        var connectionString = configuracion.GetConnectionString("DefaultConnection") ?? "";
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var rutaDb = builder.DataSource;
        if (!Path.IsPathRooted(rutaDb))
        {
            rutaDb = Path.Combine(entorno.ContentRootPath, rutaDb);
        }

        _rutaBaseDeDatos = Path.GetFullPath(rutaDb);
        _directorioRespaldo = Path.Combine(Path.GetDirectoryName(_rutaBaseDeDatos)!, "Backups");
        Directory.CreateDirectory(_directorioRespaldo);
    }

    public async Task<string> CrearRespaldoAsync(CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var nombreArchivo = $"otium_{timestamp}.db";
        var destino = Path.Combine(_directorioRespaldo, nombreArchivo);

        using var db = _fabrica.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken);

        File.Copy(_rutaBaseDeDatos, destino, overwrite: true);

        AplicarRetencion();

        return destino;
    }

    public async Task<bool> VerificarIntegridadAsync(CancellationToken cancellationToken = default)
    {
        using var db = _fabrica.CreateDbContext();
        var integridad = await db.Database.SqlQueryRaw<string>("PRAGMA integrity_check").ToListAsync(cancellationToken);
        var problemasClavesForaneas = await db.Database.SqlQueryRaw<object>("PRAGMA foreign_key_check").ToListAsync(cancellationToken);
        return integridad.All(r => string.Equals(r, "ok", StringComparison.OrdinalIgnoreCase))
            && !problemasClavesForaneas.Any();
    }

    private void AplicarRetencion()
    {
        var respaldos = Directory.GetFiles(_directorioRespaldo, "otium_*.db")
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .ToList();
        foreach (var archivo in respaldos.Skip(_maxRespaldos))
        {
            try
            {
                File.Delete(archivo);
            }
            catch
            {
                // Se ignora si el archivo ya no existe o esta bloqueado.
            }
        }
    }
}
