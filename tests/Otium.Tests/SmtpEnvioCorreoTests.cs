using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Otium.Services;
using Otium.Services.Options;

namespace Otium.Tests;

public sealed class SmtpEnvioCorreoTests
{
    [Fact]
    public void Los_valores_por_defecto_apuntan_al_rele_de_brevo()
    {
        var opciones = new SmtpOptions();

        Assert.Equal("smtp-relay.brevo.com", opciones.Host);
        Assert.Equal(587, opciones.Port);
        Assert.Equal("Otium", opciones.SenderName);
    }

    [Fact]
    public async Task Sin_configuracion_no_intenta_enviar()
    {
        var opciones = Options.Create(new SmtpOptions
        {
            Username = "",
            Password = "",
            SenderEmail = ""
        });

        var transporte = new SmtpEnvioCorreo(opciones, NullLogger<SmtpEnvioCorreo>.Instance);

        Assert.False(transporte.EstaConfigurado);
        Assert.False(await transporte.EnviarAsync("destino@example.com", "Asunto", "<p>Hola</p>"));
    }
}
