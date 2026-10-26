using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Otium.Services.Extensions;

public enum ProveedorBaseDatos
{
    Sqlite,
    PostgreSql
}

/// <summary>
/// Resuelve el proveedor de base de datos y normaliza la cadena de conexión.
/// Acepta el formato URL que usan plataformas como Render
/// (<c>postgresql://usuario:clave@host:puerto/base</c>) y lo convierte al formato ADO.NET
/// que espera Npgsql.
/// </summary>
public sealed record ConfiguracionBaseDatos(ProveedorBaseDatos Proveedor, string CadenaConexion)
{
    private const int PuertoPostgreSqlPorDefecto = 5432;

    public static ConfiguracionBaseDatos Crear(string? cadenaConexion, IConfiguration configuracion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "No hay cadena de conexión configurada. Define ConnectionStrings__DefaultConnection.");
        }

        var cadena = cadenaConexion.Trim();
        var proveedor = DetectarProveedor(cadena, configuracion);

        return proveedor == ProveedorBaseDatos.PostgreSql
            ? new ConfiguracionBaseDatos(proveedor, NormalizarPostgres(cadena))
            : new ConfiguracionBaseDatos(proveedor, cadena);
    }

    public static ProveedorBaseDatos DetectarProveedor(string cadenaConexion, IConfiguration configuracion)
    {
        var explicito = configuracion["Database:Provider"];
        if (!string.IsNullOrWhiteSpace(explicito))
        {
            return explicito.Trim().ToLowerInvariant() switch
            {
                "postgres" or "postgresql" or "npgsql" => ProveedorBaseDatos.PostgreSql,
                "sqlite" => ProveedorBaseDatos.Sqlite,
                _ => throw new InvalidOperationException(
                    $"Database:Provider '{explicito}' no es válido. Usa 'PostgreSql' o 'Sqlite'.")
            };
        }

        if (EsUrlPostgres(cadenaConexion))
        {
            return ProveedorBaseDatos.PostgreSql;
        }

        DbConnectionStringBuilder constructor;
        try
        {
            constructor = new DbConnectionStringBuilder { ConnectionString = cadenaConexion };
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                "La cadena de conexión no tiene un formato válido. " +
                "Usa el formato ADO.NET ('Host=...;Database=...;Username=...;Password=...') " +
                "o una URL 'postgresql://usuario:clave@host:puerto/base'.",
                ex);
        }

        return constructor.ContainsKey("Host")
            || constructor.ContainsKey("Server")
            || constructor.ContainsKey("Username")
            || constructor.ContainsKey("User ID")
            || constructor.ContainsKey("User Id")
                ? ProveedorBaseDatos.PostgreSql
                : ProveedorBaseDatos.Sqlite;
    }

    public static string NormalizarPostgres(string cadenaConexion)
    {
        var cadena = cadenaConexion.Trim();
        if (!EsUrlPostgres(cadena))
        {
            return cadena;
        }

        if (!Uri.TryCreate(cadena, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("La URL de PostgreSQL no es válida.");
        }

        var credenciales = uri.UserInfo.Split(':', 2);
        var usuario = Uri.UnescapeDataString(credenciales[0]);
        var clave = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : string.Empty;
        var baseDatos = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        if (string.IsNullOrWhiteSpace(uri.Host)
            || string.IsNullOrWhiteSpace(usuario)
            || string.IsNullOrWhiteSpace(baseDatos))
        {
            throw new InvalidOperationException(
                "La URL de PostgreSQL debe incluir host, usuario y nombre de base de datos.");
        }

        var constructor = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : PuertoPostgreSqlPorDefecto,
            Database = baseDatos,
            Username = usuario,
            Password = clave,
            // En Npgsql, SslMode.Require cifra sin validar el certificado del servidor.
            SslMode = SslMode.Require,
            ApplicationName = "Otium"
        };

        foreach (var parametro in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var partes = parametro.Split('=', 2);
            if (partes.Length == 2
                && partes[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse<SslMode>(Uri.UnescapeDataString(partes[1]), ignoreCase: true, out var modo))
            {
                constructor.SslMode = modo;
            }
        }

        return constructor.ConnectionString;
    }

    private static bool EsUrlPostgres(string cadenaConexion) =>
        cadenaConexion.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || cadenaConexion.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
}
