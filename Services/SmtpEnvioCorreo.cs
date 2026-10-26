using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
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

        var opcionesSeguridad = _opciones.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        _logger.LogInformation(
            "Iniciando envío SMTP con MailKit a {Destinatario} por {Host}:{Puerto} " +
            "(seguridad={Seguridad}, usuario {Usuario}, remitente {Remitente}).",
            destinatario,
            _opciones.Host,
            _opciones.Port,
            opcionesSeguridad,
            EnmascararUsuario(_opciones.Username),
            _opciones.SenderEmail);

        try
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_opciones.SenderName, _opciones.SenderEmail));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;
            mensaje.Body = new TextPart("html")
            {
                Text = cuerpoHtml,
                ContentType = { Charset = "utf-8" }
            };

            using var tiempoLimite = new CancellationTokenSource(TimeoutMilisegundos);
            using var tokenVinculado = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                tiempoLimite.Token);

            using var cliente = new SmtpClient
            {
                Timeout = TimeoutMilisegundos
            };

            await cliente.ConnectAsync(
                _opciones.Host,
                _opciones.Port,
                opcionesSeguridad,
                tokenVinculado.Token);
            await cliente.AuthenticateAsync(
                _opciones.Username,
                _opciones.Password,
                tokenVinculado.Token);
            await cliente.SendAsync(mensaje, tokenVinculado.Token);

            try
            {
                using var cierre = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await cliente.DisconnectAsync(quit: true, cierre.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "El correo se envió a {Destinatario}, pero no se pudo cerrar la conexión SMTP " +
                    "con {Host}:{Puerto} limpiamente.",
                    destinatario,
                    _opciones.Host,
                    _opciones.Port);
            }

            _logger.LogInformation(
                "Correo SMTP enviado a {Destinatario} mediante {Host}:{Puerto}.",
                destinatario,
                _opciones.Host,
                _opciones.Port);
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
        catch (AuthenticationException ex)
        {
            _logger.LogError(
                ex,
                "Brevo rechazó la autenticación SMTP al enviar a {Destinatario} mediante {Host}:{Puerto}. " +
                "Mensaje del servidor: {Detalle}. Usuario: {Usuario}; longitud de la clave configurada: {LongitudClave}. " +
                "Causa probable: {Causa}.",
                destinatario,
                _opciones.Host,
                _opciones.Port,
                ex.Message,
                EnmascararUsuario(_opciones.Username),
                _opciones.Password.Length,
                Clasificar(ex));
            return false;
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(
                ex,
                "Brevo SMTP devolvió un error al enviar a {Destinatario} mediante {Host}:{Puerto}. " +
                "SmtpStatusCode: {Estado}; SmtpErrorCode: {CodigoError}; " +
                "buzón rechazado: {Buzon}. Mensaje del servidor: {Detalle}. Causa probable: {Causa}. " +
                "Excepción interna: {Interno}.",
                destinatario,
                _opciones.Host,
                _opciones.Port,
                ex.StatusCode,
                ex.ErrorCode,
                ex.Mailbox?.Address ?? "ninguno",
                ex.Message,
                Clasificar(ex),
                DescribirCadena(ex));
            return false;
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogError(
                ex,
                "Error de protocolo SMTP con {Host}:{Puerto} al enviar a {Destinatario}. " +
                "Mensaje: {Detalle}. Causa probable: {Causa}. Excepción interna: {Interno}.",
                _opciones.Host,
                _opciones.Port,
                destinatario,
                ex.Message,
                Clasificar(ex),
                DescribirCadena(ex));
            return false;
        }
        catch (SslHandshakeException ex)
        {
            _logger.LogError(
                ex,
                "Falló el handshake TLS con {Host}:{Puerto} al enviar a {Destinatario}. " +
                "Mensaje: {Detalle}. Causa probable: {Causa}. Excepción interna: {Interno}.",
                _opciones.Host,
                _opciones.Port,
                destinatario,
                ex.Message,
                Clasificar(ex),
                DescribirCadena(ex));
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

        if (Buscar<SmtpCommandException>(ex) is { } comando)
        {
            switch (comando.StatusCode)
            {
                case SmtpStatusCode.AuthenticationRequired:
                case SmtpStatusCode.AuthenticationMechanismTooWeak:
                case SmtpStatusCode.AuthenticationInvalidCredentials:
                case SmtpStatusCode.EncryptionRequiredForAuthenticationMechanism:
                    return "autenticación rechazada: revisa Smtp:Username y Smtp:Password " +
                           "(usa la clave SMTP de Brevo, no la contraseña de la cuenta).";
                case SmtpStatusCode.MailboxUnavailable:
                case SmtpStatusCode.MailboxNameNotAllowed:
                case SmtpStatusCode.UserNotLocalTryAlternatePath:
                case SmtpStatusCode.ExceededStorageAllocation:
                    return "destinatario rechazado: verifica que el correo exista y que no tenga " +
                           "la bandeja llena.";
                case SmtpStatusCode.ServiceNotAvailable:
                case SmtpStatusCode.MailboxBusy:
                case SmtpStatusCode.ErrorInProcessing:
                case SmtpStatusCode.InsufficientStorage:
                    return "el servidor SMTP no está disponible temporalmente; reintenta más tarde.";
            }
        }

        if (Buscar<SslHandshakeException>(ex) is not null)
        {
            return "problema de TLS: el certificado del servidor no es válido o el puerto " +
                   "no ofrece STARTTLS/SSL.";
        }

        if (Buscar<SmtpProtocolException>(ex) is not null)
        {
            return "el servidor SMTP no respetó el protocolo esperado; revisa el puerto y " +
                   "el modo de seguridad (STARTTLS en 587/2525, SSL en 465).";
        }

        var mensajes = new List<string>();
        for (var actual = ex; actual is not null; actual = actual.InnerException)
        {
            mensajes.Add(actual.Message);
        }

        var texto = string.Join(" | ", mensajes).ToLowerInvariant();

        if (texto.Contains("authentication") || texto.Contains("authenticate")
            || texto.Contains("535") || texto.Contains("5.7.8")
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
            return "problema de TLS/STARTTLS: confirma el puerto y el modo de seguridad " +
                   "(STARTTLS en 587/2525, SSL en 465).";
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

    internal static string EnmascararUsuario(string usuario)
    {
        var arroba = usuario.IndexOf('@');
        if (arroba <= 0)
        {
            return $"*** (longitud {usuario.Length})";
        }

        var local = usuario[..arroba];
        var dominio = usuario[arroba..];
        var visible = local.Length <= 3 ? local[..1] : local[..3];

        return $"{visible}***{dominio} (longitud {usuario.Length})";
    }

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
