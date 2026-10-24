using Otium.Services;

namespace Otium.Tests;

public sealed class CodigoAmigoGeneratorTests
{
    [Fact]
    public void Generar_devuelve_ocho_caracteres_alfanumericos()
    {
        var generador = new CodigoAmigoGenerator();

        var codigo = generador.Generar();

        Assert.Equal(8, codigo.Length);
        Assert.All(codigo, c => Assert.Contains(c, "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"));
    }

    [Fact]
    public void Generar_no_repite_codigos_en_una_muestra_amplia()
    {
        var generador = new CodigoAmigoGenerator();

        var codigos = Enumerable.Range(0, 500).Select(_ => generador.Generar()).ToList();

        Assert.Equal(codigos.Count, codigos.Distinct().Count());
    }
}
