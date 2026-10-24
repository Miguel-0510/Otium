using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Otium.Data;
using Otium.Services;

namespace Otium.Tests;

[Collection("Otium")]
public sealed class RecuperacionContrasenaTests
{
    private readonly OtiumWebAppFactory _fabrica;

    public RecuperacionContrasenaTests(OtiumWebAppFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_usuario_sin_correo_confirmado_recibe_el_enlace_de_recuperacion()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var recuperacion = scope.ServiceProvider.GetRequiredService<IRecuperacionContrasenaService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var resultado = await registro.RegistrarAsync("sin-confirmar@example.com", "Password1");
        Assert.True(resultado.Exito);

        var usuario = await userManager.FindByEmailAsync("sin-confirmar@example.com");
        Assert.NotNull(usuario);
        Assert.False(await userManager.IsEmailConfirmedAsync(usuario!));

        await recuperacion.SolicitarAsync(
            "sin-confirmar@example.com",
            new Uri("https://localhost/Account/ResetPassword"));

        var correo = Assert.Single(
            _fabrica.Correos.Enviados,
            c => c.Destinatario == "sin-confirmar@example.com");
        Assert.Equal(FakeEmailSender.TipoCorreo.Recuperacion, correo.Tipo);
        Assert.Contains("code=", correo.Enlace);
    }

    [Fact]
    public async Task El_enlace_enviado_permite_cambiar_la_contrasena()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var registro = scope.ServiceProvider.GetRequiredService<IRegistroService>();
        var recuperacion = scope.ServiceProvider.GetRequiredService<IRecuperacionContrasenaService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var resultado = await registro.RegistrarAsync("reset@example.com", "Password1");
        Assert.True(resultado.Exito);

        await recuperacion.SolicitarAsync(
            "reset@example.com",
            new Uri("https://localhost/Account/ResetPassword"));

        var correo = _fabrica.Correos.Enviados.Last(c => c.Destinatario == "reset@example.com");
        var codigo = QueryHelpers.ParseQuery(new Uri(correo.Enlace).Query)["code"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(codigo));

        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(codigo!));
        var usuario = await userManager.FindByEmailAsync("reset@example.com");
        Assert.NotNull(usuario);

        var reset = await userManager.ResetPasswordAsync(usuario!, token, "NuevaClave1");
        Assert.True(reset.Succeeded, string.Join(" | ", reset.Errors.Select(e => e.Description)));
        Assert.True(await userManager.CheckPasswordAsync(usuario!, "NuevaClave1"));
    }

    [Fact]
    public async Task Un_correo_inexistente_no_envia_nada()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var recuperacion = scope.ServiceProvider.GetRequiredService<IRecuperacionContrasenaService>();
        var antes = _fabrica.Correos.Enviados.Count;

        await recuperacion.SolicitarAsync(
            "no-existe@example.com",
            new Uri("https://localhost/Account/ResetPassword"));

        Assert.Equal(antes, _fabrica.Correos.Enviados.Count);
    }
}
