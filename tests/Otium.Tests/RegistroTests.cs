using Microsoft.Extensions.DependencyInjection;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class RegistroTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public RegistroTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Dos_registros_consecutivos_funcionan_y_los_codigos_son_unicos()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();

        var primero = await registro.RegistrarAsync("primero@example.com", "Password1");
        var segundo = await registro.RegistrarAsync("segundo@example.com", "Password1");

        Assert.True(primero.Exito, Describe(primero));
        Assert.True(segundo.Exito, Describe(segundo));
        Assert.NotNull(primero.Usuario);
        Assert.NotNull(segundo.Usuario);
        Assert.False(string.IsNullOrWhiteSpace(primero.Usuario!.CodigoAmigo));
        Assert.False(string.IsNullOrWhiteSpace(segundo.Usuario!.CodigoAmigo));
        Assert.NotEqual(primero.Usuario.CodigoAmigo, segundo.Usuario.CodigoAmigo);
        Assert.Equal(6, primero.Usuario.CodigoAmigo.Length);
        Assert.Equal(6, segundo.Usuario.CodigoAmigo.Length);
    }

    [Fact]
    public async Task Un_correo_duplicado_no_se_registra()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();

        var primero = await registro.RegistrarAsync("duplicado@example.com", "Password1");
        var segundo = await registro.RegistrarAsync("duplicado@example.com", "Password1");

        Assert.True(primero.Exito, Describe(primero));
        Assert.False(segundo.Exito);
        Assert.Null(segundo.Usuario);
    }

    private static string Describe(ResultadoRegistro resultado) =>
        string.Join(" | ", resultado.Errores.Select(e => $"{e.Code}: {e.Description}"));
}
