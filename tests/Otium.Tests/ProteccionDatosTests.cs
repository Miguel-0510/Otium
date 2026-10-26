using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class ProteccionDatosTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public ProteccionDatosTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Las_claves_se_persisten_en_la_base_de_datos()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var proveedor = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector = proveedor.CreateProtector("prueba-persistencia");

        var cifrado = protector.Protect("contenido");
        Assert.Equal("contenido", protector.Unprotect(cifrado));

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.DataProtectionKeys.AnyAsync());
    }
}
