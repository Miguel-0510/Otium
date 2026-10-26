# Otium

Aplicación web personal de diario de lectura y gestión de biblioteca. Stack: ASP.NET Core `net10.0`, Blazor Web App (InteractiveServer), SQLite + EF Core, Identity.

## Funciones principales

- Registro e inicio de sesión con ASP.NET Core Identity.
- Estantes personales: `Leyendo`, `Leído`, `Quiero Leer`.
- Búsqueda de libros por título o autor en Open Library con portadas.
- Puntuación de 1 a 5 y reseñas.
- "Travesías": entradas de lectura con páginas y resumen.
- Glosario personal con definiciones generadas por Gemini (caché y circuit breaker).
- Amigos mediante código alfanumérico, solicitudes, aceptar/rechazar, eliminar, bloquear.
- Compartir estantes opcional (desactivado por defecto).
- Tema claro/oscuro.
- PWA instalable con página de aviso sin conexión.
- Eliminación de cuenta y exportación de datos (JSON).
- Health check en `/health`.
- Telemetría de errores con Sentry (solo si `Sentry:Dsn` está configurado).
- `robots.txt` y `.well-known/security.txt`.
- Páginas de `/terminos` y `/soporte`.

## Stack

| Elemento | Valor |
|---|---|
| Framework | ASP.NET Core `net10.0` |
| UI | Blazor Web App, `InteractiveServer` |
| Autenticación | ASP.NET Core Identity + cookies |
| Base de datos | SQLite + EF Core |
| APIs externas | Open Library, Google Gemini, Brevo SMTP |
| PWA | `manifest.json`, `sw.js`, `instalar.js` |

## Comandos

Desde la carpeta del proyecto:

```powershell
dotnet restore
dotnet build -c Release
dotnet run
dotnet test Otium.slnx -c Release
```

Publicar:

```powershell
dotnet publish Otium.csproj -c Release -o <carpeta>
```

Migraciones:

```powershell
dotnet ef migrations add <Nombre>
dotnet ef database update
```

## Configuración y secretos

Las claves se leen de variables de entorno / User Secrets / `appsettings.Development.json` (ignorada por Git):

- `ConnectionStrings:DefaultConnection` — SQLite
- `DataProtection:KeysPath` — persistencia opcional de claves en filesystem; sin ella se usa la tabla `DataProtectionKeys` de la base de datos
- `Smtp:Host` / `Smtp:Port` — relé SMTP de Brevo (`smtp-relay.brevo.com`, `587`/`2525` con STARTTLS, `465` con SSL)
- `Smtp:Username` / `Smtp:Password` — credenciales SMTP de Brevo
- `Smtp:SenderEmail` / `Smtp:SenderName` — remitente verificado y nombre visible
- `Gemini:ApiKey` / `Gemini:Modelo` — Gemini
- `Sitio:UrlPublica` — URL pública para enlaces de recuperación
- `Seguridad:PermitirHttp` — permite HTTP en modo Production (Tailscale)

El correo se envía por el relé SMTP de Brevo con MailKit (STARTTLS en `587`/`2525`, SSL en `465`). Configura `Smtp:SenderEmail` con un remitente verificado en Brevo. En Render free, usa `Smtp__Port=2525` porque los puertos `25`, `465` y `587` están bloqueados.

Nunca commitear secretos ni archivos `.db`.

## Despliegue

- **Local/Tailscale:** ejecutar en `Development` o `Production` con `Seguridad__PermitirHttp=true`. Kestrel escucha en todas las interfaces.
- **VPS Oracle:** `Production`, Kestrel en `http://127.0.0.1:5000`, proxy inverso (Caddy/Nginx) con TLS, `Seguridad__PermitirHttp=false`, `Proxy__KnownProxies__0=127.0.0.1`.

El proyecto no está listo para lanzamiento público hasta completar los hallazgos P2 (CI, respaldos automáticos, validación de entrada, endeurecimiento operativo) descritos en `AGENTS.md`.
