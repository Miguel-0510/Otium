using Microsoft.Extensions.Options;
using Otium.Services.Options;

namespace Otium.Services;

public interface IEnlacePublicoService
{
    Uri Construir(string rutaRelativa, Uri baseActual);
}

public sealed class EnlacePublicoService : IEnlacePublicoService
{
    private readonly SitioOptions _opciones;

    public EnlacePublicoService(IOptions<SitioOptions> opciones)
    {
        _opciones = opciones.Value;
    }

    public Uri Construir(string rutaRelativa, Uri baseActual)
    {
        var configurada = (_opciones.UrlPublica ?? "").Trim();

        if (configurada.Length > 0)
        {
            if (!configurada.EndsWith('/'))
            {
                configurada += "/";
            }

            if (Uri.TryCreate(configurada, UriKind.Absolute, out var basePublica)
                && (basePublica.Scheme == Uri.UriSchemeHttp || basePublica.Scheme == Uri.UriSchemeHttps))
            {
                return new Uri(basePublica, rutaRelativa);
            }
        }

        return new Uri(baseActual, rutaRelativa);
    }
}
