using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class RateLimitingTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public RateLimitingTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_login_devuelve_429_al_exceder_el_limite_por_ip()
    {
        var cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 11; i++)
        {
            ultima = await cliente.PostAsync("/Account/Login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Input.Email"] = "nadie@example.com",
                    ["Input.Password"] = "Password1"
                }));
        }

        Assert.NotNull(ultima);
        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
    }

    [Fact]
    public void La_cuota_diaria_se_agota_y_no_se_resetea_dentro_del_dia()
    {
        using var scope = _fabrica.Services.CreateScope();
        var limites = scope.ServiceProvider.GetRequiredService<ILimiteIntentosService>();
        var clave = $"prueba-{Guid.NewGuid():N}";

        Assert.True(limites.IntentarCuotaDiaria(clave, 2));
        Assert.True(limites.IntentarCuotaDiaria(clave, 2));
        Assert.False(limites.IntentarCuotaDiaria(clave, 2));
    }
}
