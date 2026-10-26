namespace Otium.Services;

public interface IEnvioCorreo
{
    bool EstaConfigurado { get; }

    Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default);
}
