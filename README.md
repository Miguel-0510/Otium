# Otium

Diario de lectura, biblioteca y **lector de libros integrado**.

> **Nombre comercial en evaluación.** `Otium` colisiona con competidores del mismo nicho y se va a
> sustituir, pero la decisión no está cerrada: hay lista corta y evidencia en
> [`DOCS/marca.md`](DOCS/marca.md). La candidata principal es **Elicona**, sin ninguna inversión en
> marca hasta que R-90 cierre el nombre. El código, los ensamblados y la carpeta siguen usando
> `Otium` hasta el renombrado (R-93).

Otium reúne dos cosas que normalmente viven separadas: el cuaderno donde anotas lo que lees y
el sitio donde lo lees. Trae tus EPUB sin DRM o lee obras de dominio público, con progreso
sincronizado, glosario en contexto y travesías de lectura.

- **Stack:** ASP.NET Core `net10.0`, Blazor Web App (`InteractiveServer`), Identity + cookies,
  EF Core 10 con SQLite (desarrollo) y PostgreSQL (producción).
- **Cliente:** web + PWA + TWA en Play Store.
- **Escala objetivo:** ≤ 100 usuarios registrados, 10–25 circuitos concurrentes.

## Estado

| Área | Estado |
|---|---|
| Biblioteca, estantes, reseñas, travesías, glosario, amigos | Funcional (beta privada) |
| Seguridad P0/P1 (registro, recuperación, autorización, rate limiting, respaldos) | Cerrado |
| Lector EPUB, catálogo, cobro, TWA | **Planificado, no implementado** |

El plan completo, con fases, tareas e hitos, está en [`ROADMAP.md`](ROADMAP.md). El índice de
documentación y qué está obsoleto, en [`DOCS/README.md`](DOCS/README.md).

## Funciones actuales

- Registro e inicio de sesión con ASP.NET Core Identity.
- Estantes `Leyendo`, `Leído`, `Quiero leer`.
- Búsqueda en Open Library con portadas.
- Puntuación de 1 a 5 y reseñas.
- Travesías: entradas con páginas y resumen.
- Glosario personal con definiciones de GLM-5.3-Flash (Z.ai), con caché y circuit breaker.
- Amigos por código de 6 caracteres; compartir estantes opcional; bloquear.
- Tema claro/oscuro, PWA instalable, eliminación de cuenta y exportación de datos.
- `/health`, Sentry opcional, `robots.txt` y `security.txt`.

## Arranque rápido

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

## Configuración

Los valores se leen de variables de entorno, User Secrets o `appsettings.Development.json`
(ignorado por Git).

| Clave | Uso |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQLite o PostgreSQL (detecta el proveedor por la cadena) |
| `DataProtection:KeysPath` | Llavero en filesystem; si no se define, se persiste en la base |
| `Definiciones:ApiKey` | API key de Z.ai (alias `GLM_API_KEY`, `ZAI_API_KEY`, `OPENROUTER_API_KEY`) |
| `Definiciones:Endpoint` / `Definiciones:Modelo` | Endpoint compatible OpenAI y modelo (`glm-5.3-flash`) |
| `Brevo:ApiKey` | API key v3 (`xkeysib-`); no es una SMTP key |
| `Brevo:SenderEmail` / `Brevo:SenderName` | Remitente verificado |
| `Sitio:UrlPublica` | URL externa para enlaces de recuperación |
| `Seguridad:PermitirHttp` | Permite HTTP en `Production` (modo Tailscale) |
| `Sentry:Dsn` | Opcional; sin él no se envía telemetría |

**Nunca** se commitean secretos, `appsettings.Development.json` ni archivos `.db`.

## Despliegue

- **Hoy:** local/Tailscale, Render y un VPS Oracle heredado.
- **Objetivo:** Hetzner CX22 + Docker Compose (Caddy, app, PostgreSQL) + restic → Backblaze B2.
  Detalle en [`DOCS/despliegue.md`](DOCS/despliegue.md) y [`DOCS/respaldos.md`](DOCS/respaldos.md).

## Documentación

| Documento | Contenido |
|---|---|
| [`DOCS/README.md`](DOCS/README.md) | Índice, estado y política documental |
| [`ROADMAP.md`](ROADMAP.md) | Plan por fases, tareas, hitos y riesgos |
| [`DOCS/producto.md`](DOCS/producto.md) | Alcance, no-objetivos y modelo de negocio |
| [`DOCS/lector.md`](DOCS/lector.md) | Motor de lectura EPUB y progreso |
| [`DOCS/catalogo.md`](DOCS/catalogo.md) | Catálogo de dominio público |
| [`DOCS/monetizacion.md`](DOCS/monetizacion.md) | Planes, cuotas y cobro |
| [`DOCS/despliegue.md`](DOCS/despliegue.md) | Infraestructura y operación |
| [`DOCS/respaldos.md`](DOCS/respaldos.md) | RPO/RTO y restauración |
| [`DOCS/licencias.md`](DOCS/licencias.md) | Licencias y atribuciones |
| [`DOCS/marca.md`](DOCS/marca.md) | Nombre, evidencia y registro |
| [`AGENTS.md`](AGENTS.md) | Arquitectura actual y convenciones de trabajo |
| [`CHANGELOG.md`](CHANGELOG.md) | Historial de cambios |
| [`DOCS/hallazgos.md`](DOCS/hallazgos.md) | Histórico de deuda técnica (F-xx) |
