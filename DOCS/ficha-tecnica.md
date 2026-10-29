# Ficha técnica — Otium

> Estado: beta privada. Última actualización: 2026-10-28. Fuente de verdad: `C:\Proyectos\Otium` (rama `main`).

## 1. Arquitectura y tech stack

| Elemento | Valor |
|---|---|
| Framework | ASP.NET Core **10.0** (`net10.0`) |
| UI | **Blazor Web App** con renderizado **Interactive Server** (circuito SignalR; no usa WebAssembly) |
| Identidad | ASP.NET Core Identity + cookies (`RequireConfirmedAccount = false`, flujo de recuperación sin exigir confirmación — Opción B) |
| Datos | EF Core **10.0.14**; **SQLite** (local/Docker) y **PostgreSQL** (Render) con detección de proveedor por cadena de conexión; migraciones para SQLite y `EnsureCreated` controlado para PostgreSQL |
| Despliegue | **Render** (PaaS): Kestrel escuchando en loopback, TLS terminado en el borde; `ForwardedHeaders` confía en known proxies; HTTPS exigido en `Production` (HSTS, cookies `Secure`, `Seguridad__PermitirHttp=false`) |
| Local/Tailscale | HTTP en la interfaz Tailscale con `Seguridad__PermitirHttp=true`, o `Development` |
| Observabilidad | Sentry (`Sentry.AspNetCore`, activo solo si `Sentry:Dsn`), health check `/health` con comprobación de BD, respaldos con retención de 5 copias y verificación de integridad |
| CI | GitHub Actions (`ci.yml`): restore, build Release, tests con cobertura (mínimo 70 % de líneas, coverlet) y auditoría de paquetes vulnerables en cada push/PR a `main` |

### Patrones de diseño aplicados

- **Inyección de dependencias** exhaustiva: interfaces para todos los servicios (`ILibroService`, `IAmistadService`, `IEntradaLecturaService`, `IGlosarioService`, `IBuscadorLibros`, `IDefinicionClient`, `IEnvioCorreo`, `IEmailSender<T>`), tipados y registrados con `IDbContextFactory`.
- **Opciones tipadas y validadas** con `IOptions<T>` + `AddOptions().Bind().Validate()` (`BrevoOptions`, `DefinicionesOptions`, `SitioOptions`, `DataProtection`): validación al arranque, fail-fast.
- **Repository/Service** pragmático: los servicios contienen la lógica y el acceso a datos; los componentes Razor delegan y no tocan EF directamente. Autorización a nivel de dato: los servicios filtran siempre por `usuarioId` (anti-IDOR).
- **DTOs** para superficie externa: exportación de datos personal (`PalabraGlosarioExportada`, records inmutables); las entidades EF no se exponen fuera de la aplicación.
- **Adapter**: `IEnvioCorreo` abstrae el transporte de correo y `IDefinicionClient`/`IBuscadorLibros` abstraen proveedores externos (el proveedor de IA puede conmutarse Z.ai ⇄ OpenRouter por configuración sin tocar código).
- **Detección de proveedor de BD:** PostgreSQL en Render vía cadena de conexión normalizada (`postgresql://` → ADO.NET con `SslMode=Require`); arranque con reintentos exponenciales (5) y parada controlada si no se puede preparar el esquema.
- **Dominio**: entidades con validación in-line (DataAnnotations) y reglas en servicios; sin manejadores de eventos de dominio (alcance consciente y limitado a una app personal).

## 2. Servicios integrados e infraestructura

### 2.1 Glosario inteligente (IA)

- Contrato `IDefinicionClient` → implementación `DefinicionesClient`, cliente **HTTP/1.1 TLS sobre puerto 443** a una API **OpenAI-compatible** (`POST {endpoint}/chat/completions`).
- Proveedor activo: **OpenRouter** con el modelo **`z-ai/glm-5.3-flash`** (endpoint por defecto del código: Z.ai `https://api.z.ai/api/paas/v4/`); conmutable vía `Definiciones__Endpoint`/`Definiciones__Modelo`. Autenticación `Authorization: Bearer`.
- **Entrada dual:** palabra única o frase larga. Si llega un fragmento, el prompt instruye al modelo para extraer el término principal; el texto se recorta a **200 caracteres** antes de enviarlo (control anti-desperdicio de tokens).
- **Respuesta estructurada:** el modelo responde solo JSON `{"termino", "definicion", "contexto_lectura"}` con parser tolerante (extracción del bloque `{...}` y fallback limpio al texto plano).
- **Modo de contexto:** parámetro `incluirContextoLectura` (toggle de UI, desactivado por defecto). Modo 1: prompt ultracorto sin libro ni campo de contexto. Modo 2: incluye la ficha/libro activo y añade "En el contexto de la lectura: ..." **solamente** si el modelo reporta un matiz relevante.
- **Serialización:** `System.Net.Http.Json` para envío (`JsonContent.Create`) y parsing tipado de la respuesta (`ReadFromJsonAsync` con `JsonSerializerOptions` Web).
- **Resiliencia:** `CancellationToken` propagado en todas las I/O, timeout HTTP de 10 s, reintentos acotados (máx. 2) con reintento por `HttpRequestException`/timeout y estados transitorios (429/5xx), **circuit breaker** propio (estados Cerrado/Abierto/MedioAbierto), caché en memoria de **7 días** por clave texto+libro+modo, y cuota por usuario (50 definiciones/día) con rate limiting HTTP en endpoints de Identity.
- **Limitación de tokens y latencia:** `max_tokens = 300`, `temperature = 0.2`, definición global acotada a 15–20 palabras por prompt. Deduplicación por vía rápida en BD antes de gastar cuota de API.

### 2.2 Emailing

- Transporte migrado de **SMTP/MailKit a la API REST de Brevo v3** sobre `POST https://api.brevo.com/v3/smtp/email` (HTTP/HTTPS 443). Motivo: los puertos SMTP salientes (25/465/587) están bloqueados en entornos PaaS como Render; la API HTTP evita ese bloqueo.
- `EmailSender` conserva plantillas y delega en `IEnvioCorreo` → `BrevoEnvioCorreo`: cabecera `api-key`, cuerpo JSON (`sender/to/subject/htmlContent`), timeout 15 s, cancelación, captura del código/mensaje HTTP y registro en logs sin interrumpir la petición.
- Se mantiene respaldo temporal desde las claves `Smtp:*` (legado) validando que el valor sea una API key v3 (`xkeysib-`); los puertos SMTP bloqueados en PaaS quedan superados por diseño.
- Data Protection persiste el llavero en la tabla `DataProtectionKeys` de la BD (o en `DataProtection__KeysPath`), para que sesiones y enlaces de recuperación sobrevivan a redeploys.

## 3. Seguridad, rendimiento y calidad

### Variables de entorno y protección de credenciales

- Nombres reales en Render (doble guion bajo = separador de nivel de configuración .NET):
  - `Definiciones__ApiKey` (alias: `GLM_API_KEY`, `ZAI_API_KEY`, `OPENROUTER_API_KEY`)
  - `Definiciones__Endpoint`, `Definiciones__Modelo`
  - `Brevo__ApiKey`, `Brevo__SenderEmail`, `Brevo__SenderName`, `Brevo__Endpoint`
  - `ConnectionStrings__DefaultConnection`, `DataProtection__KeysPath`, `Sentry:Dsn`, `Proxy__KnownProxies__0`
- **Nota de ciclo de vida:** `Glm__ApiKey` no es un nombre válido para este proyecto (no corresponde a ninguna sección de configuración registrada); la convención vigente es `Definiciones__ApiKey` o los alias listados.
- Cero credenciales en código: User Secrets en desarrollo (`dotnet user-secrets`), variables de entorno o gestor de secretos en producción; `appsettings.Development.json` ignorado por Git; `dotnet publish` no incluye la BD de desarrollo; validación al arranque con `DiagnosticoConfiguracion` (registra solo **presencia y longitud** de claves, nunca su contenido) y detección de claves mal formadas.
- Endurecimiento: CSP con `wss:` para SignalR, nosniff, referrer, frame-ancestors, antiforgery en formularios y endpoints sensibles, cookies `Secure`/`HttpOnly`, códigos de 8 caracteres criptográficos (`RandomNumberGenerator`), índice único para pares de amistad, mensajes uniformes anti-enumeración de usuarios, eliminación de cuenta transaccional y exportación de datos completa.

### Rendimiento en la UI (Blazor Server)

- Toggles de estado (contexto de lectura, tema) gestionados con `@bind` sobre `bool` simples: cada cambio produce **un único re-render diferencial**, sin estado global ni JS interop en el camino caliente.
- El aviso de bienvenida solo ejecuta su lógica en `OnAfterRenderAsync(firstRender)` y persiste "no volver a mostrar" en `localStorage`; cero trabajo por conexión de datos en la ruta rápida.
- Lógica pesada en servicios con `async/await` sin bloqueos (`async void` eliminado; sin `.Result`/`.Wait()`), `CancellationToken` en todo I/O nuevo, y consultas EF puntuales (`ExecuteDelete`, proyecciones, índices únicos) para minimizar el tiempo de respuesta del circuito.
- Cachés en memoria para búsquedas de libros (10 min) y definiciones (7 días); reintentos acotados para no degradar la percepción de latencia.

### Manejo de excepciones y logging

- Logging estructurado con plantillas de mensajes (props con nombre: `{Palabra}`, `{Modelo}`, `{Estado}`) en todo el código de servicios; captura jerárquica `OperationCanceledException` (re-lanzada si es del token del caller), `TimeoutException`, `HttpRequestException` y `JsonException`; nunca excepciones silenciadas vacías.
- Sin fugas: los errores HTTP externos registran `StatusCode` y `ResponseBody` truncado (4000 caracteres) **sin** exponer API keys; los mensajes al usuario son genéricos y no incluyen `ex.Message`; el diagnóstico de configuración no revela valores de claves.
- Sentry opcional (DSN por configuración) con `SendDefaultPii = false`.

### Calidad verificable

| Métrica | Valor actual |
|---|---|
| Compilación Release | 0 errores, 0 advertencias |
| Suite automatizada | xUnit + `WebApplicationFactory` sobre SQLite temporal; **65 tests** (registro duplicado, recuperación de contraseña, autorización por propietario, eliminación de cuenta, compartición de estantes, validación, cliente de definiciones, diagnóstico de configuración, rate limiting) |
| Cobertura | Mínimo exigido 70 % de líneas (`coverlet.runsettings`) |
| Paquetes vulnerables | 0 (`dotnet list package --vulnerable --include-transitive`) |
| CI | Ok (build + test + cobertura + auditoría) |

## 4. Límites conocidos (sin adornos)

- **Escalabilidad horizontal:** no soportada; Blazor Server mantiene estado por circuito y la base de datos gestionada de Render no es dedicada. Suficiente para la beta privada.
- **Migraciones EF solo SQLite:** en PostgreSQL el esquema se crea por `EnsureCreated`; cambios futuros de modelo en Postgres requerirán migraciones Npgsql dedicadas.
- **Cola de correo (outbox):** pendiente de evaluación; hoy el envío es en-petición con captura de errores.
- **Dominio anémico parcial:** validaciones de negocio viven en servicios; no hay eventos de dominio ni invariantes en entidades (deuda técnica conocida, documentada en `AGENTS.md`).
- Documentación operativa: `README.md`, `CHANGELOG.md`, `AGENTS.md` (estado de hallazgos P0–P3) y `DOCS/respaldos.md` (RPO/RTO, restauración); pendiente `LICENSE`/`.editorconfig`/`global.json`.

## 5. Conclusión de arquitectura

Otium mantiene una separación de capas funcional y aplica resiliencia temprana (circuit breaker, reintentos acotados, cuotas y validación de configuración al arranque) donde las integraciones externas la exigen, siempre a través de interfaces y opciones tipadas que permiten conmutar proveedores por configuración. Las últimas mejoras se concentran en el camino de la IA del glosario (respuesta JSON estructurada, prompts duales por modo, contexto de lectura conmutable) y en el transporte de correo por la API HTTP de Brevo, decisión crítica para PaaS. La limitación de escala (Blazor Server + BD gestionada) está asumida y documentada: es una decisión deliberada para el alcance personal del producto.
