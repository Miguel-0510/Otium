# Otium

Aplicación web personal de diario de lectura y gestión de biblioteca. Stack: ASP.NET Core `net10.0`, Blazor Web App (InteractiveServer), SQLite + EF Core, Identity.

## Funciones principales

- Registro e inicio de sesión con ASP.NET Core Identity.
- Estantes personales: `Leyendo`, `Leído`, `Quiero Leer`.
- Búsqueda de libros por título o autor en Open Library con portadas.
- Puntuación de 1 a 5 y reseñas.
- "Travesías": entradas de lectura con páginas y resumen.
- Glosario personal con definiciones generadas por GLM-5.3-Flash (Z.ai), con caché y circuit breaker.
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
| APIs externas | Open Library, Z.ai (GLM-5.3-Flash), Brevo HTTP API |
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

- `ConnectionStrings:DefaultConnection` - SQLite o PostgreSQL.
- `DataProtection:KeysPath` - persistencia opcional de claves en filesystem; sin ella se usa la tabla `DataProtectionKeys` de la base de datos.
- `Brevo:ApiKey` - API key v3 de Brevo (empieza por `xkeysib-`; **no** es la SMTP key).
- `Brevo:SenderEmail` / `Brevo:SenderName` - remitente verificado y nombre visible.
- `Brevo:Endpoint` - endpoint HTTP; por defecto `https://api.brevo.com/v3/smtp/email`.
- `Definiciones:ApiKey` / `Definiciones:Endpoint` / `Definiciones:Modelo` - servicio de definiciones del glosario (Z.ai/GLM; alias de entorno `GLM_API_KEY` o `ZAI_API_KEY`; endpoint por defecto `https://api.z.ai/api/paas/v4/`; modelo por defecto `glm-5.3-flash`).
- `Sitio:UrlPublica` - URL pública para enlaces de recuperación.
- `Seguridad:PermitirHttp` - permite HTTP en modo Production (Tailscale).

El correo se envía con un `POST` a la API HTTP de Brevo. Configura `Brevo:SenderEmail` con un remitente verificado en Brevo y `Brevo:ApiKey` con una API key v3. En Render, define `Brevo__ApiKey`, `Brevo__SenderEmail`, `Brevo__SenderName` y `Brevo__Endpoint`.

La aplicación acepta `Smtp:Password`, `Smtp:SenderEmail` y `Smtp:SenderName` como respaldo temporal de configuración, pero el valor de `Smtp:Password` debe ser una **API key v3 de Brevo**, no una SMTP key. Lo recomendable es migrar a las claves `Brevo:*`.

Nunca commitear secretos ni archivos `.db`.

## Despliegue

- **Local/Tailscale:** ejecutar en `Development` o `Production` con `Seguridad__PermitirHttp=true`. Kestrel escucha en todas las interfaces.
- **VPS Oracle:** `Production`, Kestrel en `http://127.0.0.1:5000`, proxy inverso (Caddy/Nginx) con TLS, `Seguridad__PermitirHttp=false`, `Proxy__KnownProxies__0=127.0.0.1`.
- **Render:** `Production`; usar HTTPS, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, PostgreSQL y `Brevo__ApiKey`. No se necesitan puertos SMTP.

El proyecto no está listo para lanzamiento público hasta completar los hallazgos P2 (CI, respaldos automáticos, validación de entrada, endurecimiento operativo) descritos en `AGENTS.md`.
