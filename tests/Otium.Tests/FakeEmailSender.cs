using Microsoft.AspNetCore.Identity;
using Otium.Data;

namespace Otium.Tests;

public sealed class FakeEmailSender : IEmailSender<ApplicationUser>
{
    public List<CorreoEnviado> Enviados { get; } = new();

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        Enviados.Add(new CorreoEnviado(email, confirmationLink, TipoCorreo.Confirmacion));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        Enviados.Add(new CorreoEnviado(email, resetLink, TipoCorreo.Recuperacion));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        Enviados.Add(new CorreoEnviado(email, resetCode, TipoCorreo.CodigoRecuperacion));
        return Task.CompletedTask;
    }

    public sealed record CorreoEnviado(string Destinatario, string Enlace, TipoCorreo Tipo);

    public enum TipoCorreo
    {
        Confirmacion,
        Recuperacion,
        CodigoRecuperacion
    }
}
