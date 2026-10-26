# Changelog - Otium

Todos los cambios notables de este proyecto se documentarán en este archivo.

## [No publicado] - 2026-10-25

### Despliegue en Render
- Servicio de correo unificado en SMTP de Brevo (`SmtpEnvioCorreo`) tras la abstracción `IEnvioCorreo`. Se eliminó la integración previa de correo por HTTP.
- `EmailSender` conserva las plantillas y delega el envío en el transporte SMTP.
- El transporte SMTP usa MailKit con STARTTLS en `smtp-relay.brevo.com` (587/2525) o SSL en 465, autenticación explícita, timeout, cancelación y captura de errores sin bloquear la petición. Corrige `5.7.0 Please authenticate first` de `System.Net.Mail.SmtpClient`.
- Data Protection persiste las claves en la tabla `DataProtectionKeys` de la base de datos cuando no se configura `DataProtection:KeysPath`; las sesiones y los enlaces de recuperación sobreviven a reinicios y despliegues en Render. Migración `AgregarClavesProteccionDatos` y creación idempotente para PostgreSQL.
- Gemini normaliza la API key (espacios) y acepta `GEMINI_API_KEY` como alias.
- Añadido `DiagnosticoConfiguracion`: al arrancar registra presencia y longitud de las claves, los valores de `Smtp:Host`, `Port`, `Username` y `SenderEmail`, y advierte de campos vacíos o inválidos sin exponer secretos.
- Añadida verificación de DI al arrancar para `IGeminiClient`, `IEnvioCorreo` e `IEmailSender<ApplicationUser>`.
- Gemini reintenta `429` y trata los timeouts como error controlado en el glosario. Los errores HTTP registran `StatusCode` y `ResponseBody`; los fallos de red, timeout y JSON inválido quedan detallados en el log.
- `SmtpEnvioCorreo` registra el mensaje exacto de Brevo y clasifica `SmtpCommandException`, `SmtpProtocolException`, `AuthenticationException`, `SslHandshakeException`, `SocketException`, fallos de autenticación, remitente/destinatario rechazado y bloqueo del puerto SMTP saliente.
- Eliminado `.github/workflows/docker.yml`.

### Telemetría y archivos estáticos
- Añadido `Sentry.AspNetCore` para telemetría de errores. Solo se activa si `Sentry:Dsn` está configurado (variables de entorno/User Secrets); en desarrollo y tests no envía nada.
- Añadido `wwwroot/robots.txt` (evita indexar rutas privadas: /Account/, /glosario, /amigos).
- Añadido `wwwroot/.well-known/security.txt` (contacto de seguridad RFC 9116).
- Añadida página `/terminos` (Términos de uso).
- Añadida página `/soporte` (contacto, reportar errores, vulnerabilidades, preguntas frecuentes).

### P2.8 - Manejo de errores y logging
- Añadido endpoint `/health` con comprobación de conexión a SQLite (`SaludDbCheck`).
- Corregido ciclo de vida de `SaludDbCheck` de `Singleton` a `Scoped` (depende de `IDbContextFactory` scoped).
- `ConfirmEmail.razor`: ya no expone el `UserId` interno al usuario; mensajes traducidos al español.

### P2.9 - Respaldos y operación
- Añadido `IBackupService` / `BackupService`: respaldo de SQLite con retención de 5 archivos, `PRAGMA wal_checkpoint` y verificación de integridad (`integrity_check` + `foreign_key_check`).

### P2.10 - Endurecer Docker
- `Dockerfile`: usuario no root (`otium`), `VOLUME /data`, `HEALTHCHECK` contra `GET /health` con `curl`, imágenes base `mcr.microsoft.com/dotnet/aspnet:10.0`.

### P2.11 - Accesibilidad e internacionalización
- `NotFound.razor` y `Error.razor` traducidos al español.
- Revisados `alt`, `aria-*` y botones sin texto (correctos).
- Contraste, foco y navegación por teclado revisados (estándar del navegador/Bootstrap).

### P2.12 - Rendimiento y resiliencia
- `Services/CircuitBreaker.cs` (nuevo): circuit breaker propio (estados Cerrado/Abierto/MedioAbierto) sin dependencias externas.
- Aplicado `CircuitBreaker` a `BuscadorLibros` (Open Library) y `GeminiClient` (Gemini) encima de los reintentos existentes.
- `GeminiClient`: caché de definiciones (7 días) mediante `IMemoryCache` para evitar llamadas repetidas y consumo de cuota.

### P3 bajo - Pulido de UX
- `Amigos.razor`: `CopiarCodigo` envuelve `navigator.clipboard.writeText` en try-catch y loguea fallos.
- `BotonInstalar.razor`: inyecta `ILogger` y protege las llamadas JS de la primera renderización con try-catch.

### Glosario (P3 bajo)
- `GlosarioService`: evita duplicados (consulta existente antes de llamar a Gemini, no consume cuota).
- `GlosarioService`: nuevo método `EditarDefinicionAsync` para modificar definiciones guardadas.
- `Glosario.razor`: botón "Editar" en cada entrada expandida, textarea para editar, guardar/cancelar.
- `Glosario.razor.css`: estilos para el área de edición y botones de guardar/cancelar.

## [0.1.0-beta] - 2026-10-23
- Versión base auditada. P0/P1 corregidos (código de amigo, recuperación de contraseña, secretos, eliminación/exportación de cuenta, autorización por propietario, rate limiting, códigos de amigo robustos, función social).
- Véase `AGENTS.md` para el detalle de hallazgos y estado.
