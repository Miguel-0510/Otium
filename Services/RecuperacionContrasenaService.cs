using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Otium.Data;

namespace Otium.Services;

public sealed class RecuperacionContrasenaService : IRecuperacionContrasenaService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender<ApplicationUser> _emailSender;

    public RecuperacionContrasenaService(UserManager<ApplicationUser> userManager, IEmailSender<ApplicationUser> emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    public async Task SolicitarAsync(string email, Uri resetUri, CancellationToken cancellationToken = default)
    {
        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario is null)
        {
            return;
        }

        var codigo = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        codigo = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(codigo));
        var enlace = QueryHelpers.AddQueryString(resetUri.ToString(), "code", codigo);

        await _emailSender.SendPasswordResetLinkAsync(usuario, email, HtmlEncoder.Default.Encode(enlace));
    }
}
