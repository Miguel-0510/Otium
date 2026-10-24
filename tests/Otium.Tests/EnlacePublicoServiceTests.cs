using Microsoft.Extensions.Options;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class EnlacePublicoServiceTests
{
    [Fact]
    public void Usa_la_url_configurada_para_el_reset()
    {
        var servicio = new EnlacePublicoService(
            Options.Create(new SitioOptions { UrlPublica = "https://pc.tail29325a.ts.net" }));

        var enlace = servicio.Construir(
            "Account/ResetPassword",
            new Uri("http://127.0.0.1:5235/"));

        Assert.Equal("https://pc.tail29325a.ts.net/Account/ResetPassword", enlace.ToString());
    }

    [Fact]
    public void Respeta_una_ruta_base_configurada()
    {
        var servicio = new EnlacePublicoService(
            Options.Create(new SitioOptions { UrlPublica = "https://ejemplo.com/otium/" }));

        var enlace = servicio.Construir(
            "Account/ResetPassword",
            new Uri("http://localhost:5000/"));

        Assert.Equal("https://ejemplo.com/otium/Account/ResetPassword", enlace.ToString());
    }

    [Fact]
    public void Sin_configuracion_usa_la_url_de_la_peticion()
    {
        var servicio = new EnlacePublicoService(
            Options.Create(new SitioOptions { UrlPublica = "" }));

        var enlace = servicio.Construir(
            "Account/ResetPassword",
            new Uri("http://100.85.111.109:5235/"));

        Assert.Equal("http://100.85.111.109:5235/Account/ResetPassword", enlace.ToString());
    }
}
