using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Options;
using Otium.Services.Options;

namespace Otium.Services;

public sealed class SmtpEnvioCorreo : IEnvioCorreo
{
    private const int TimeoutMilisegundos = 15_000;

    private readonly SmtpOptions _opciones;
    private readonly ILogger<SmtpEnvioCorreo> _logger;

    public SmtpEnvioCorreo(IOptions<SmtpOptions> opciones, ILogger<SmtpEnvioCorreo> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(_opciones.Host)
        && !string.IsNullOrWhiteSpace(_opciones.Username)
        && !string.IsNullOrWhiteSpace(_opciones.Password)
        && !string.IsNullOrWhiteSpace(_opciones.SenderEmail);

    public async Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default)
    {
        var clavesFaltantes = ObtenerClavesFaltantes();
        if (clavesFaltantes.Count > 0)
        {
            _logger.LogWarning(
                "SMTP no está configurado; se omite el envío a {Destinatario}. " +
                "Faltan o están vacíos: {Claves}. Configura Smtp:Host, Smtp:Username, " +
                "Smtp:Password y Smtp:SenderEmail.",
                destinatario,
                string.Join(", ", clavesFaltantes));
            return false;
        }

        _logger.LogInformation(
            "Iniciando envío SMTP a {Destinatario} por {Host}:{Puerto} " +
            "(STARTTLS={StartTls}, usuario longitud {LongitudUsuario}, remitente {Remitente}).",
            destinatario,
            _opciones.Host,
            _opciones.Port,
            true,
            _opciones.Username.Length,
            _opciones.SenderEmail);

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(_opciones.SenderEmail, _opciones.SenderName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mensaje.To.Add(destinatario);

            using var cliente = new SmtpClient(_opciones.Host, _opciones.Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_opciones.Username, _opciones.Password),
                EnableSsl = true,
                Timeout = TimeoutMilisegundos
            };

            using var tiempoLimite = new CancellationTokenSource(TimeoutMilisegundos);
            using var tokenVinculado = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                tiempoLimite.Token);

            await cliente.SendMailAsync(mensaje, tokenVinculado.Token);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(
                ex,
                "El envío SMTP a {Destinatario} mediante {Host}:{Puerto} superó el tiempo límite de {Timeout} ms. " +
                "Detalle: {Detalle}. Causa probable: {Causa}.",
                destinatario,
                _opciones.Host,
                _opciones.Port,
                TimeoutMilisegundos,
                ex.Message,
                Clasificar(ex));
            return false;
        }
        catch (SmtpFailedRecipientsException ex)
        {
            _logger.LogError(
                ex,
                "Brevo rechazó uno o varios destinatarios al enviar a {Destinatario} mediante {Host}:{Puerto}. " +
                "SmtpStatusCode: {Estado}. Detalle del servidor: {Detalle}. Causa probable: {Causa}.",
                destinatario,
                _opciones.Host,
                _opciones.Port,
                ex.StatusCode,
                ex.Message,
                Clasificar(ex));
            return false;
        }
        catch (SmtpFailedRecipientException ex)
        {
            _logger.LogError(
                ex,
                "Brevo rechazó el destinatario {Rechazado} al enviar a {Destinatario} mediante {Host}:{Puerto}. " +
                "SmtpStatusCode: {Estado}. Detalle del servidor: {Detalle}. Causa probable: {Causa}.",
                ex.FailedRecipient,
                destinatario,
                _opciones.Host,
                _opciones.Port,
                ex.StatusCode,
                ex.Message,
                Clasificar(ex));
            return false;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(
                ex,
                "Brevo SMTP devolvió un error al enviar a {Destinatario} mediante {Host}:{Puerto}. " +
                "SmtpStatusCode: {Estado}. Mensaje del servidor: {Detalle}. Causa probable: {Causa}. " +
                "Excepción interna: {Interno}.",
                destinatario,
                _opciones.Host,
                _opciones.Port,
                ex.StatusCode,
                ex.Message,
                Clasificar(ex),
                DescribirCadena(ex));
            return false;
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(
                ex,
                "Brevo rechazó la autenticación SMTP al enviar a {Destinatario} mediante {Host}:{Puerto}. " +
                "Mensaje del servidor: {Detalle}. Causa probable: {Causa}.",
                destinatario,
                _opciones.Host,
                _opciones.Port,
                ex.Message,
                Clasificar(ex));
            return false;
        }
        catch (SocketException ex)
        {
            _logger.LogError(
                ex,
                "Error de socket al conectar con {Host}:{Puerto} para enviar a {Destinatario}. " +
                "SocketErrorCode: {SocketError}; NativeErrorCode: {NativeError}; mensaje: {Detalle}. " +
                "Causa probable: {Causa}.",
                _opciones.Host,
                _opciones.Port,
                destinatario,
                ex.SocketErrorCode,
                ex.NativeErrorCode,
                ex.Message,
                DescribirSocket(ex));
            return false;
        }
        catch (IOException ex) when (ex.InnerException is SocketException socket)
        {
            _logger.LogError(
                ex,
                "Error de E/S durante la conexión SMTP con {Host}:{Puerto} para enviar a {Destinatario}. " +
                "SocketErrorCode: {SocketError}; NativeErrorCode: {NativeError}; mensaje: {Detalle}. " +
                "Causa probable: {Causa}.",
                _opciones.Host,
                _opciones.Port,
                destinatario,
                socket.SocketErrorCode,
                socket.NativeErrorCode,
                ex.Message,
                DescribirSocket(socket));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo enviar el correo SMTP a {Destinatario} mediante {Host}:{Puerto}. " +
                "Mensaje: {Detalle}. Causa probable: {Causa}. Excepción interna: {Interno}.",
                destinatario,
                _opciones.Host,
                _opciones.Port,
                ex.Message,
                Clasificar(ex),
                DescribirCadena(ex));
            return false;
        }
    }

    private List<string> ObtenerClavesFaltantes()
    {
        var faltantes = new List<string>();

        if (string.IsNullOrWhiteSpace(_opciones.Host))
        {
            faltantes.Add("Smtp:Host");
        }

        if (string.IsNullOrWhiteSpace(_opciones.Username))
        {
            faltantes.Add("Smtp:Username");
        }

        if (string.IsNullOrWhiteSpace(_opciones.Password))
        {
            faltantes.Add("Smtp:Password");
        }

        if (string.IsNullOrWhiteSpace(_opciones.SenderEmail))
        {
            faltantes.Add("Smtp:SenderEmail");
        }

        return faltantes;
    }

    private static string Clasificar(Exception ex)
    {
        var socket = Buscar<SocketException>(ex);
        if (socket is not null)
        {
            return DescribirSocket(socket);
        }

        if (Buscar<AuthenticationException>(ex) is not null)
        {
            return "autenticación rechazada: revisa Smtp:Username y Smtp:Password " +
                   "(usa la clave SMTP de Brevo, no la contraseña de la cuenta).";
        }

        var mensajes = new List<string>();
        for (var actual = ex; actual is not null; actual = actual.InnerException)
        {
            mensajes.Add(actual.Message);

            if (actual is SmtpFailedRecipientException rechazo)
            {
                mensajes.Add($"rechazo del destinatario {rechazo.FailedRecipient}");
            }
        }

        var texto = string.Join(" | ", mensajes).ToLowerInvariant();

        if (texto.Contains("authentication") || texto.Contains("535") || texto.Contains("5.7.8")
            || texto.Contains("credentials") || texto.Contains("username and password"))
        {
            return "autenticación rechazada: revisa Smtp:Username y Smtp:Password " +
                   "(usa la clave SMTP de Brevo, no la contraseña de la cuenta).";
        }

        if (texto.Contains("sender") || texto.Contains("from address") || texto.Contains("5.7.1")
            || texto.Contains("relay") || texto.Contains("not verified"))
        {
            return "remitente rechazado: verifica que Smtp:SenderEmail esté validado en Brevo.";
        }

        if (texto.Contains("ssl") || texto.Contains("tls") || texto.Contains("secure connection"))
        {
            return "problema de TLS/STARTTLS: confirma el puerto 587 y EnableSsl=true.";
        }

        if (texto.Contains("timed out") || texto.Contains("timeout") || texto.Contains("connection refused"))
        {
            return "conectividad bloqueada: el proveedor de hosting puede estar bloqueando el puerto SMTP saliente.";
        }

        return "revisa conectividad, puerto, credenciales y remitente verificado en Brevo.";
    }

    private static string DescribirSocket(SocketException ex) => ex.SocketErrorCode switch
    {
        SocketError.ConnectionRefused =>
            "conexión rechazada: puerto cerrado, bloqueado por firewall o host incorrecto.",
        SocketError.TimedOut =>
            "tiempo de espera agotado: posible bloqueo del puerto SMTP saliente por el proveedor de hosting.",
        SocketError.HostNotFound or SocketError.NoData =>
            "no se pudo resolver el host SMTP (DNS).",
        SocketError.NetworkUnreachable or SocketError.HostUnreachable =>
            "red o host inalcanzable desde el contenedor.",
        SocketError.ConnectionReset =>
            "conexión reiniciada por el servidor SMTP.",
        _ => $"error de socket {ex.SocketErrorCode} (código nativo {ex.NativeErrorCode})."
    };

    private static T? Buscar<T>(Exception? ex) where T : Exception
    {
        for (var actual = ex; actual is not null; actual = actual.InnerException)
        {
            if (actual is T encontrada)
            {
                return encontrada;
            }
        }

        return null;
    }

    private static string DescribirCadena(Exception ex)
    {
        var partes = new List<string>();

        for (var actual = ex.InnerException; actual is not null; actual = actual.InnerException)
        {
            partes.Add($"{actual.GetType().Name}: {actual.Message}");
        }

        return partes.Count == 0 ? "sin excepción interna" : string.Join(" -> ", partes);
    }
}
