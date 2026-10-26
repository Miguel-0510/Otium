using Microsoft.Extensions.Configuration;
using Otium.Services.Extensions;
using System.Data.Common;

namespace Otium.Tests;

public sealed class ConfiguracionBaseDatosTests
{
    private static IConfiguration SinConfiguracion() =>
        new ConfigurationBuilder().Build();

    [Fact]
    public void Convierte_la_url_de_render_al_formato_ado_net()
    {
        var configuracion = ConfiguracionBaseDatos.Crear(
            "postgresql://otium_user:secreto123@dpg-abc123-a.oregon-postgres.render.com:5432/otium_db",
            SinConfiguracion());

        Assert.Equal(ProveedorBaseDatos.PostgreSql, configuracion.Proveedor);

        var constructor = new DbConnectionStringBuilder
        {
            ConnectionString = configuracion.CadenaConexion
        };

        Assert.Equal("dpg-abc123-a.oregon-postgres.render.com", constructor["Host"]);
        Assert.Equal(5432, Convert.ToInt32(constructor["Port"]));
        Assert.Equal("otium_db", constructor["Database"]);
        Assert.Equal("otium_user", constructor["Username"]);
        Assert.Equal("secreto123", constructor["Password"]);
        Assert.Equal("Require", constructor["SSL Mode"]);
    }

    [Fact]
    public void Usa_el_puerto_por_defecto_si_la_url_no_lo_indica()
    {
        var configuracion = ConfiguracionBaseDatos.Crear(
            "postgres://usuario:clave@dpg-interno-a/otium",
            SinConfiguracion());

        var constructor = new DbConnectionStringBuilder
        {
            ConnectionString = configuracion.CadenaConexion
        };

        Assert.Equal(5432, Convert.ToInt32(constructor["Port"]));
    }

    [Fact]
    public void Decodifica_credenciales_con_caracteres_escapados()
    {
        var configuracion = ConfiguracionBaseDatos.Crear(
            "postgresql://usuario:p%40ss%3Aword@host:5432/base",
            SinConfiguracion());

        var constructor = new DbConnectionStringBuilder
        {
            ConnectionString = configuracion.CadenaConexion
        };

        Assert.Equal("p@ss:word", constructor["Password"]);
    }

    [Fact]
    public void Respeta_sslmode_de_la_query()
    {
        var configuracion = ConfiguracionBaseDatos.Crear(
            "postgresql://usuario:clave@host:5432/base?sslmode=Disable",
            SinConfiguracion());

        var constructor = new DbConnectionStringBuilder
        {
            ConnectionString = configuracion.CadenaConexion
        };

        Assert.Equal("Disable", constructor["SSL Mode"]);
    }

    [Fact]
    public void Mantiene_una_cadena_ado_net_de_postgres()
    {
        const string cadena = "Host=localhost;Port=5432;Database=otium;Username=otium;Password=clave";

        var configuracion = ConfiguracionBaseDatos.Crear(cadena, SinConfiguracion());

        Assert.Equal(ProveedorBaseDatos.PostgreSql, configuracion.Proveedor);
        Assert.Equal(cadena, configuracion.CadenaConexion);
    }

    [Fact]
    public void Detecta_sqlite_en_desarrollo()
    {
        var configuracion = ConfiguracionBaseDatos.Crear(
            "DataSource=Data/app.db;Cache=Shared",
            SinConfiguracion());

        Assert.Equal(ProveedorBaseDatos.Sqlite, configuracion.Proveedor);
        Assert.Equal("DataSource=Data/app.db;Cache=Shared", configuracion.CadenaConexion);
    }

    [Fact]
    public void Database_Provider_tiene_prioridad_sobre_la_deteccion()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "PostgreSql"
            })
            .Build();

        var resultado = ConfiguracionBaseDatos.Crear(
            "Host=localhost;Database=otium;Username=otium;Password=clave",
            configuracion);

        Assert.Equal(ProveedorBaseDatos.PostgreSql, resultado.Proveedor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_cadenas_vacias(string cadena)
    {
        Assert.Throws<InvalidOperationException>(
            () => ConfiguracionBaseDatos.Crear(cadena, SinConfiguracion()));
    }

    [Fact]
    public void Rechaza_url_sin_nombre_de_base_de_datos()
    {
        Assert.Throws<InvalidOperationException>(
            () => ConfiguracionBaseDatos.Crear(
                "postgresql://usuario:clave@host:5432",
                SinConfiguracion()));
    }
}
