# AGENTS.md - Otium

Guía de trabajo y auditoría técnica del proyecto.

- **Fuente de verdad:** `C:\Proyectos\Otium`
- **Build publicado:** `C:\Otium\OtiumServidor` (no editar a mano)
- **Datos de producción:** `C:\OtiumDatos\app.db`
- **Commit auditado:** `0f02b1b` (`main`)
- **Fecha de auditoría:** 2026-10-23
- **Estado:** beta privada funcional; **no listo para lanzamiento público** hasta resolver los hallazgos P0/P1.

---

## 1. Qué es Otium

Otium es una aplicación web personal de diario de lectura y gestión de biblioteca.

Funciones principales:

- Registro e inicio de sesión con ASP.NET Core Identity.
- Estantes personales: `Leyendo`, `Leido`, `QuieroLeer`.
- Búsqueda de libros por título o autor en Open Library.
- Portadas, puntuación de 1 a 5 y reseñas.
- "Travesías": entradas de lectura con páginas y resumen.
- Glosario personal con definiciones generadas por GLM-5.3-Flash (Z.ai): definición global breve y, solo si aporta un matiz relevante, un apartado "En el contexto de la lectura:".
- Amigos mediante código de 4 dígitos.
- Tema claro/oscuro.
- PWA instalable con página de aviso sin conexión.
- Eliminación de cuenta y política de privacidad.

La interfaz y el dominio están en español. Quedan pantallas heredadas de la plantilla de Identity en inglés.

---

## 2. Stack y datos rápidos

| Elemento | Valor |
|---|---|
| Framework | ASP.NET Core `net10.0` |
| UI | Blazor Web App, render `InteractiveServer` |
| Autenticación | ASP.NET Core Identity + cookies |
| Base de datos | SQLite + EF Core |
| Paquetes EF/Identity | `10.0.12` |
| APIs externas | Open Library (HTTPS), Z.ai GLM (HTTPS), Brevo HTTP API (HTTPS) |
| PWA | `manifest.json`, `sw.js`, `instalar.js` |
| Puerto local publicado | `http://localhost:5000` |
| Puerto de desarrollo | `http://localhost:5235`, `https://localhost:7111` |
| SDK instalado | .NET SDK `10.0.102` y `10.0.401` |
| Pruebas automatizadas | `tests/Otium.Tests` (xUnit + WebApplicationFactory); `dotnet test` |
| CI/CD | No existe |

La compilación actual pasa sin errores ni advertencias:

```powershell
dotnet build Otium.csproj -c Release --nologo
# Compilación correcta. 0 advertencias, 0 errores.
```

No se encontraron paquetes vulnerables con `dotnet list package --vulnerable --include-transitive`.

---

## 3. Comandos de trabajo

Ejecutar desde `C:\Proyectos\Otium`:

```powershell
dotnet restore
dotnet build -c Release
dotnet run
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

La aplicación aplica migraciones automáticamente al iniciar (`Program.cs:73-77`). Para producción conviene respaldar antes y evitar múltiples instancias ejecutando migraciones a la vez.

Base de datos local de desarrollo:

- `Data/app.db` está ignorada por Git.
- **No usar datos reales en desarrollo.**
- `Otium.csproj` ya no copia `Data/app.db` a la salida; `dotnet publish` no incluye la base de desarrollo. Ver hallazgo P1-8.

---

## 4. Estructura del repositorio

```text
Program.cs                         Arranque, DI, pipeline, cabeceras, migraciones
Otium.csproj                       Proyecto web net10.0
appsettings.json                   Configuración base
appsettings.Development.json       Configuración local ignorada por Git (contiene secretos reales)
Dockerfile                         Imagen de publicación
Components/
  App.razor                        HTML raíz, CSS, PWA, scripts
  Routes.razor                     Router y AuthorizeRouteView
  _Imports.razor                   Usings globales de Razor
  Layout/                          MainLayout, menú, tema, instalación, reconexión
  Pages/                           Estantes, Amigos, Glosario, Privacidad, Error, NotFound
  Account/                         Identity: login, registro, gestión, 2FA, passkeys
Data/
  ApplicationDbContext.cs          EF Core + relaciones
  ApplicationUser.cs               Usuario + CodigoAmigo
  DescriptorErroresEs.cs           Traducción de errores de Identity
  Migrations/                      Migraciones EF Core
Models/
  Libro.cs                         Libro + enum Estante
  Amistad.cs                       Amistad + enum EstadoAmistad
  EntradaLectura.cs               Travesías
  PalabraGlosario.cs              Glosario
Services/
  LibroService.cs                 CRUD de libros
  AmistadService.cs               Código, solicitudes, amigos
  EntradaLecturaService.cs        CRUD de travesías
  GlosarioService.cs              Persistencia y cuotas del glosario
  DefinicionesClient.cs           Cliente GLM (Z.ai): prompt JSON, HTTP, caché y parseo
  BuscadorLibros.cs               Cliente Open Library con caché y reintentos
  EmailSender.cs                  Plantillas y envío (delega en IEnvioCorreo)
  BrevoEnvioCorreo.cs             Transporte HTTP de Brevo
wwwroot/
  app.css, tema.js                Estilos y tema
  manifest.json, sw.js            PWA
  icons/                          Iconos PWA
```

---

## 5. Configuración y secretos

> **Correo (2026-10-26):** el transporte SMTP/MailKit fue reemplazado por la API HTTP de Brevo.
> Configura `Brevo:ApiKey` (API key v3 `xkeysib-`), `Brevo:SenderEmail`, `Brevo:SenderName` y
> `Brevo:Endpoint`. Las claves `Smtp:*` solo se aceptan como respaldo temporal; `Smtp:Password` debe contener una API key v3, no una SMTP key.

> **Glosario (2026-10-28):** el cliente de Google Gemini fue reemplazado por la API OpenAI-compatible
> de Z.ai (modelo por defecto `glm-5.3-flash`) para reducir la latencia. Configura `Definiciones:ApiKey`
> (alias `GLM_API_KEY`/`ZAI_API_KEY`); la antigua clave `Gemini:ApiKey` ya no se usa.

Claves usadas:

| Clave | Uso | Dónde |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | SQLite | `appsettings.json` |
| `DataProtection:KeysPath` | Persistencia de claves en filesystem (opcional; sin ella se usa la base de datos) | Entorno/Docker |
| `Brevo:ApiKey` | API key v3 de Brevo (`xkeysib-`) | User Secrets/entorno (`Brevo__ApiKey`) |
| `Brevo:SenderEmail` | Remitente verificado en Brevo | `appsettings.json` |
| `Brevo:SenderName` | Nombre visible del remitente | `appsettings.json` |
| `Brevo:Endpoint` | Endpoint HTTP de Brevo | `appsettings.json` |
| `Definiciones:ApiKey` | API key de Z.ai (GLM) para el glosario | Configuración local/entorno |
| `Definiciones:Endpoint` | Endpoint OpenAI-compatible de Z.ai | `appsettings.json` |
| `Definiciones:Modelo` | Modelo GLM (`glm-5.3-flash`) | `appsettings.json` |
| `Sitio:UrlPublica` | URL externa para enlaces de correo | Configuración local/entorno |
| `Invitacion:Codigo` | Legado; ya no se usa en el fuente actual | `appsettings.Development.json` |

El correo se envía con la API HTTP de Brevo (`https://api.brevo.com/v3/smtp/email`) mediante `BrevoEnvioCorreo`. `Brevo:SenderEmail` debe ser un remitente verificado en Brevo y `Brevo:ApiKey` una API key v3 (`xkeysib-`). Las claves `Smtp:*` son legado; `Smtp:Password` solo se usa como respaldo y debe contener una API key v3. `GLM_API_KEY` o `ZAI_API_KEY` se aceptan como alias de entorno para el servicio de definiciones (Z.ai).

Reglas:

- **Nunca** commitear `appsettings.Development.json`, claves, tokens, cadenas de conexión reales ni archivos `.db`.
- El archivo local `appsettings.Development.json` contiene credenciales reales y está ignorado por Git (`.gitignore:489`). Tratarlo como secreto: si se compartió, rotar las claves.
- Mover secretos a User Secrets en desarrollo:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Definiciones:ApiKey" "<valor>"
dotnet user-secrets set "Brevo:ApiKey" "<valor>"
```

- En producción usar variables de entorno o un gestor de secretos, nunca el archivo del repositorio.
- Si se define `DataProtection:KeysPath`, debe existir y tener permisos restringidos. Si no se define, las claves se guardan en la tabla `DataProtectionKeys` de la base de datos, de modo que sobreviven a reinicios y despliegues.
- `Definiciones:ApiKey` se envía en la cabecera `Authorization: Bearer` y no se devuelve en los mensajes de error.

---

## 6. Arquitectura y flujos

### 6.1 Pipeline de arranque

`Program.cs`:

1. Registra Razor Components interactivos por servidor.
2. Configura autenticación, Identity, cookies y revalidación de seguridad.
3. Registra `ApplicationDbContext` y `IDbContextFactory<ApplicationDbContext>` con SQLite.
4. Configura Data Protection en filesystem si existe `DataProtection:KeysPath`; si no, persiste el llavero en la base de datos (`DataProtectionKeys`).
5. Registra servicios de libros, amigos, travesías, glosario, Open Library y correo.
6. Aplica migraciones al iniciar.
7. Configura HSTS, manejo de errores, cabeceras de seguridad, redirección HTTPS y antiforgery.
8. Mapea componentes Blazor y endpoints adicionales de Identity.

### 6.2 Capas de aplicación

| Capa | Implementación | Estado |
|---|---|---|
| Presentación | Componentes Razor, CSS aislado, JS mínimo | Existe, pero `Estantes.razor` concentra demasiada lógica |
| Aplicación | `Services/*` | Parcial; mezcla casos de uso, persistencia y configuración |
| Dominio | `Models/*` | Entidades anémicas, sin reglas de negocio |
| Infraestructura | EF Core, SQLite, SMTP (Brevo), HttpClient | Existe |
| Contratos | Interfaces de servicios | Parcial; `IBuscadorLibros` e `IDefinicionClient` existen; faltan DTOs de entrada/salida |

Flujo típico de una página Blazor:

```text
Componente Razor
  -> AuthenticationStateProvider (obtiene UsuarioId)
  -> servicio de aplicación (ILibroService, IAmistadService, ...)
  -> IDbContextFactory<ApplicationDbContext>
  -> SQLite
```

### 6.3 Modelo de datos

- `ApplicationUser` extiende `IdentityUser` y añade `CodigoAmigo` y `CompartirEstantes`.
- `Libro` pertenece a un usuario. `Estante` se guarda como entero.
- `EntradaLectura` pertenece a `Libro` y se elimina en cascada.
- `Amistad` relaciona solicitante y receptor; ambas FK son `Restrict`; `ClavePar` es único.
- `Bloqueo` relaciona `UsuarioId` con `BloqueadoId`; índice único por par y cascada al borrar usuarios.
- `PalabraGlosario` guarda `UsuarioId` como texto, **sin FK ni cascada**.

Relaciones relevantes en `Data/ApplicationDbContext.cs`:

- `Libros -> AspNetUsers`: cascada.
- `Amistades -> AspNetUsers`: restrict.
- `EntradasLectura -> Libros`: cascada.
- `Bloqueos -> AspNetUsers`: cascada por ambos lados.
- `PalabrasGlosario`: sin relación configurada.

### 6.4 Autenticación

- Login por correo (el correo también es `UserName`).
- Contraseña mínima: 8 caracteres; mayúscula, minúscula y número; sin requerir símbolo (`Program.cs:41-47`).
- Confirmación de cuenta desactivada (`SignIn.RequireConfirmedAccount = false`).
- Lockout por intentos fallidos activado en el login.
- Sesión persistente opcional.
- Recuperación de contraseña funcional sin exigir confirmación de correo (Opción B). Ver P0-2.
- Compartir estantes desactivado por defecto; el usuario lo activa desde Amigos. Los amigos solo ven libros, puntuaciones y reseñas; travesías y glosario quedan privados.
- 2FA, passkeys, correo y datos personales heredados de la plantilla de Identity.

### 6.5 Integraciones externas

- **Open Library:** búsqueda de 8 resultados por título/autor; portadas desde `covers.openlibrary.org`.
- **GLM (Z.ai):** definiciones breves del glosario; endpoint OpenAI-compatible `https://api.z.ai/api/paas/v4/chat/completions`, modelo por defecto `glm-5.3-flash`; la API key viaja en `Authorization: Bearer`. La respuesta es JSON (`definicion` + `contexto_lectura` opcional) y se guarda como definición global seguida de "En el contexto de la lectura: ..." solo si aporta un matiz relevante.
- **Brevo HTTP API:** envío de enlaces de recuperación con `POST https://api.brevo.com/v3/smtp/email`, autenticación `api-key` y cuerpo JSON (`sender`, `to`, `subject`, `htmlContent`).
- **PWA:** el service worker solo guarda la página de aviso sin conexión. La app requiere conexión, por diseño.

### 6.6 Despliegue dual: Tailscale local y VPS Oracle

La app soporta dos modos sin cambiar el código:

| Modo | Entorno | Kestrel | TLS | Configuración clave |
|---|---|---|---|---|
| Local/Tailscale | `Development` o `Production` | HTTP en la interfaz Tailscale | Tailscale cifra el transporte | `Seguridad__PermitirHttp=true` si se usa `Production` |
| VPS Oracle (Ubuntu ARM64) | `Production` | `http://127.0.0.1:5000` | Caddy/Nginx con certificado automático | `Seguridad__PermitirHttp=false`, `Proxy__KnownProxies__0=127.0.0.1` |

Reglas de compatibilidad:

- `ForwardedHeaders` acepta `X-Forwarded-For`, `X-Forwarded-Proto` y `X-Forwarded-Host`. Por defecto solo confía en proxies loopback; si el proxy está en Docker u otra IP, añadirla en `Proxy:KnownProxies`.
- `Sitio:UrlPublica` debe ser la URL externa real (por ejemplo, `https://pc.tail29325a.ts.net` con Tailscale Funnel o el dominio del VPS). Se usa para construir los enlaces de recuperación de contraseña; si está vacío, se usa la URL de la petición.
- `UseHttpsRedirection`, `UseHsts` y cookies `Secure` solo se activan cuando `exigirHttps` es verdadero: `Production` y `Seguridad:PermitirHttp=false`.
- En local sobre Tailscale se puede ejecutar en `Development` (recomendado) o en `Production` con `Seguridad__PermitirHttp=true`; en este último caso las cookies usan `SameAsRequest`.
- En el VPS, Kestrel no debe escuchar en `0.0.0.0`; el proxy inverso termina TLS y reenvía a loopback.
- Las claves de Data Protection (filesystem o tabla `DataProtectionKeys`) y la base de datos deben vivir fuera de la carpeta de publicación (`DataProtection__KeysPath` si se usa y `ConnectionStrings__DefaultConnection`).
- El CSP ya declara `wss:`; el proxy debe permitir WebSocket para Blazor Server.

Ejemplo de variables en el VPS:

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000
ConnectionStrings__DefaultConnection="Data Source=/var/lib/otium/app.db"
DataProtection__KeysPath=/var/lib/otium/keys
Seguridad__PermitirHttp=false
Proxy__KnownProxies__0=127.0.0.1
Sitio__UrlPublica=https://otium.example.com
```

### 6.7 Render + PostgreSQL

- `ConnectionStrings__DefaultConnection` acepta tanto el formato ADO.NET como la URL de Render (`postgresql://usuario:clave@host:puerto/base`). `ConfiguracionBaseDatos` la normaliza a ADO.NET con `SslMode=Require`; si la URL trae `?sslmode=...`, ese valor prevalece.
- El proveedor se detecta por la forma de la cadena (URL de Postgres, `Host=`/`Server=`/`Username=`) o por `Database__Provider=PostgreSql|Sqlite`.
- Las migraciones de `Data/Migrations` son de SQLite y **no son portables** a PostgreSQL. En PostgreSQL el esquema se crea desde el modelo con `EnsureCreatedAsync` (`Data/BaseDatosInicializador.cs`). Si en el futuro cambia el modelo en producción, hay que crear migraciones específicas de Npgsql en un ensamblado aparte y cambiar la estrategia.
- El arranque reintenta 5 veces con espera exponencial y, si no puede preparar el esquema, **detiene la aplicación** en lugar de servir errores 500.
- Data Protection persiste las claves en la tabla `DataProtectionKeys` de la base de datos cuando no se define `DataProtection__KeysPath`; esto funciona en Render free y evita invalidar sesiones y enlaces de recuperación en cada reinicio. Si se usa un Disk, configurar `DataProtection__KeysPath=/var/data/keys`. La tabla se crea de forma idempotente al arrancar (`Data/BaseDatosInicializador.cs`).
- Si el proxy de Render no está en `Proxy__KnownProxies`, `ForwardedHeaders` no confía en `X-Forwarded-Proto`. Configurar el proxy o `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` para que `exigirHttps`, HSTS y las cookies `Secure` funcionen con TLS terminado en el borde.

---

## 7. Convenciones para agentes

- Trabajar en `C:\Proyectos\Otium`, no en `C:\Otium`.
- Mantener la UI y los mensajes de dominio en español. Si se toca una pantalla heredada en inglés, traducirla de forma consistente.
- Mantener `Nullable` habilitado y evitar introducir `!` innecesarios.
- Usar `async`/`await`; no usar `.Result`, `.Wait()` ni `async void`.
- Añadir `CancellationToken` en operaciones nuevas de I/O.
- No editar migraciones ya aplicadas. Crear una migración nueva.
- No commitear secretos, bases de datos, `bin/`, `obj/` ni `.vs/`.
- No cambiar el orden numérico de los enums persistidos (`Estante`, `EstadoAmistad`) sin migración de datos.
- Preferir interfaces para nuevos servicios y mantener la inyección de dependencias.
- Seguir el patrón existente de CSS aislado por componente (`.razor.css`).
- No añadir librerías nuevas sin revisar si ya existe una solución en el proyecto.
- Antes de terminar: compilar y verificar que no haya advertencias nuevas.
- Si se corrige un hallazgo de este archivo, actualizar su estado y la evidencia.

Comandos de verificación mínimos:

```powershell
dotnet build Otium.csproj -c Release --nologo
dotnet test Otium.slnx -c Release --nologo
dotnet list Otium.csproj package --vulnerable --include-transitive
```

La suite vive en `tests/Otium.Tests` y usa `WebApplicationFactory` con SQLite temporal. No toca `Data/app.db` ni `C:\OtiumDatos\app.db`.

---

## 8. Evaluación SOLID

| Principio | Estado | Evidencia | Recomendación |
|---|---|---|---|
| **S** - Responsabilidad única | Parcial | `Estantes.razor` tiene búsqueda, CRUD, reseñas, travesías y presentación. `GlosarioService` mezcla HTTP, prompt, parsing, persistencia y traducción de errores. `AmistadService` mezcla Identity, generación de códigos y acceso a datos. | Extraer componentes/vistas y separar casos de uso, generación de códigos y clientes externos. |
| **O** - Abierto/cerrado | Parcial | Añadir un estante obliga a tocar el enum, `Nombre()` y varias vistas. Añadir un proveedor de búsqueda obliga a tocar la página. | Centralizar nombres/estantes y abstraer proveedores con interfaces. |
| **L** - Sustitución de Liskov | Cumple | Las implementaciones respetan sus interfaces; no hay jerarquías problemáticas. | Mantener contratos pequeños y explícitos. |
| **I** - Segregación de interfaces | Cumple | `ILibroService`, `IAmistadService`, `IEntradaLecturaService` son acotadas. | No engordar interfaces existentes; crear contratos específicos. |
| **D** - Inversión de dependencias | Parcial | Servicios dependen de `IDbContextFactory`, interfaces y `IOptions<T>`; las páginas usan `IBuscadorLibros`, pero las entidades EF se filtran a la UI. | Introducir DTOs/view models. |

Conclusión SOLID: **parcial**. La base es razonable para una app pequeña, pero la UI y algunos servicios concentran demasiadas responsabilidades y dependen de detalles de infraestructura.

---

## 9. Capas y modelo OSI

OSI es un modelo de comunicaciones de red, no una arquitectura de software. No existe un "cumplimiento OSI" del código fuente. Lo verificable es el uso correcto de protocolos y la separación en capas de la aplicación.

### 9.1 Stack de red

| Capa OSI | Uso en Otium | Estado |
|---|---|---|
| 7 Aplicación | HTTP/HTTPS, Blazor SignalR, Open Library, Z.ai GLM, Brevo | Correcto en salidas; entrada publicada sin TLS |
| 6 Presentación | HTML, CSS, JS, JSON, UTF-8 | Correcto |
| 5 Sesión | Cookies de Identity, circuito SignalR, Data Protection | Correcto: llavero persistido en base de datos o filesystem |
| 4 Transporte | TCP | Correcto |
| 3 Red | IP, proxy inverso | No documentado; depende del despliegue |
| 2/1 Enlace/Físico | Infraestructura | Fuera del alcance del código |

Hallazgos de red:

- Las APIs externas usan HTTPS.
- El correo usa la API HTTP de Brevo sobre HTTPS; no hay puertos SMTP salientes.
- La publicación actual escucha HTTP en `http://localhost:5000` y el Dockerfile expone HTTP en `8080`.
- `UseHttpsRedirection()` y `UseHsts()` existen, pero sin un puerto HTTPS o proxy TLS delante no protegen nada.
- Las cookies de autenticación deben ir con `Secure` en producción; eso exige HTTPS real.
- El CSP declara `wss:` pero no `ws:`. En desarrollo por HTTP, el WebSocket de Blazor puede quedar bloqueado y caer a otro transporte. Añadir `ws:` solo en desarrollo o generar CSP por entorno.

### 9.2 Capas de software

La separación actual es **aceptable para beta privada**, no para un diseño por capas estricto:

- Presentación: componentes Razor, con lógica de negocio incrustada.
- Aplicación: servicios, sin una capa de casos de uso formal.
- Dominio: modelos anémicos, sin validaciones ni reglas.
- Infraestructura: EF Core, SQLite, SMTP, HttpClient.
- No hay DTOs: las entidades se enlazan directamente a formularios y vistas.

---

## 10. Checklist de lanzamiento

| Área | Estado | Nota |
|---|---|---|
| Compilación | Sí | 0 errores, 0 advertencias |
| Pruebas automatizadas | Sí | `tests/Otium.Tests`; 36 pruebas y cobertura mínima del 70% de líneas |
| CI/CD | Sí | `.github/workflows/ci.yml`: restore, build, test con cobertura y auditoría de paquetes |
| README / LICENSE / CHANGELOG | No | No existen |
| `.editorconfig` / `global.json` | No | Sin convenciones ni SDK fijado |
| Secretos fuera del repo | Sí | Credenciales locales movidas a User Secrets; `appsettings.Development.json` saneado. Rotación pendiente por el propietario |
| HTTPS/TLS de entrada | Sí (código) | Proxy TLS + `ForwardedHeaders`; pendiente verificar en VPS Oracle |
| Rate limiting | Sí | HTTP en login/registro/recuperación y cuotas por usuario en amistades/definiciones |
| Confirmación de correo | Parcial | Opción B: confirmación opcional; el correo solo se envía al recuperar contraseña |
| Recuperación de contraseña | Sí | Funciona sin exigir `EmailConfirmed`; enlaces construidos con `Sitio:UrlPublica`; probado con `IEmailSender` falso |
| Eliminación de cuenta | Sí | Flujo único transaccional con libros, travesías, glosario y amistades |
| Exportación de datos | Sí | JSON con cuenta, libros, travesías, glosario y amistades |
| Respaldos | Sí | `BackupService` con retención de 5 archivos; `DOCS/respaldos.md` con RPO/RTO y procedimiento de restauración |
| Health checks | Sí | Endpoint `/health` con `SaludDbCheck` |
| Observabilidad | Parcial | Sentry.AspNetCore registrado (solo si `Sentry:Dsn` está configurado); logging básico de framework/Identity |
| Validación de entrada | Parcial | UI valida poco; servicios no validan |
| Manejo de errores | Parcial | Algunos errores internos llegan al usuario |
| Cabeceras de seguridad | Sí | CSP, nosniff, referrer, frame-options; CSP mejorable |
| CSRF | Sí | Antiforgery en formularios y endpoints sensibles |
| Autorización de páginas | Sí | `[Authorize]` en estantes, amigos y glosario |
| Autorización a nivel de dato | Sí | Servicios filtran por `usuarioId`; pruebas con dos usuarios |
| Función social | Sí | Compartir estantes opcional, eliminar amigo y bloquear; travesías/glosario privados |
| Accesibilidad | Parcial | Buenas bases; falta auditoría formal |
| Internacionalización | Parcial | Español mezclado con scaffold en inglés |
| PWA | Sí | Manifiesto, iconos, service worker, instalación |
| Rendimiento | Aceptable | Suficiente para uso personal/pequeño |
| Escalabilidad horizontal | No | SQLite + Blazor Server con estado por circuito |
| Docker | Parcial | Sin usuario no root, healthcheck ni volumen declarado |
| Dependencias vulnerables | Sí | Sin vulnerabilidades conocidas al momento de la auditoría |

**Veredicto:** P0 y P1 cerrados en código; pendiente verificar el despliegue TLS en el VPS Oracle. Antes de abrir al público, completar P2 (CI, respaldos, validación y endurecimiento operativo).

---

## 11. Hallazgos priorizados

### P0 - Bloqueantes

#### P0-1. El registro falla a partir del segundo usuario nuevo

**Estado: corregido (2026-10-23).** El registro usa `IRegistroService`, que asigna un `CodigoAmigo` criptográfico de 8 caracteres (`ICodigoAmigoGenerator`) antes de `CreateAsync` y reintenta ante colisiones. `AmistadService` reutiliza el mismo generador y valida `UpdateAsync`. Pruebas en `RegistroTests`.

Evidencia histórica:

- `Data/ApplicationUser.cs:7`: `CodigoAmigo` por defecto `""`.
- `Data/ApplicationDbContext.cs:37-39`: índice único sobre `CodigoAmigo`.
- `Data/Migrations/20260923221056_AgregarIndiceCodigoAmigo.cs:13-17`: índice `unique: true`.
- `Components/Account/Pages/Register.razor:71-90`: el registro no genera `CodigoAmigo`.

Reproducido en una base limpia:

```text
Registro 1: HTTP 200
Registro 2: HTTP 500
SQLite Error 19: UNIQUE constraint failed: AspNetUsers.CodigoAmigo
```

El primer usuario queda con `CodigoAmigo = ""` hasta que visita Amigos; el segundo ya no puede registrarse.

Corrección recomendada:

1. Generar el código en el registro, antes de `CreateAsync`.
2. O hacer `CodigoAmigo` nullable y crear un índice único filtrado para valores no nulos.
3. Validar el resultado de `UpdateAsync` y cubrir la carrera con reintentos.
4. Añadir una prueba de integración que registre dos usuarios seguidos.

#### P0-2. La recuperación de contraseña no funciona para cuentas nuevas

**Estado: corregido (2026-10-23) con la Opción B.** Se eliminó la comprobación `IsEmailConfirmedAsync`; `IRecuperacionContrasenaService` envía el enlace a cualquier cuenta existente y mantiene la misma respuesta para no enumerar usuarios. El correo solo se envía al solicitar recuperación. Pruebas en `RecuperacionContrasenaTests`.

Evidencia histórica:

- `Program.cs:43`: `RequireConfirmedAccount = false`.
- `Register.razor:71-90`: registra e inicia sesión sin confirmar correo ni enviar confirmación.
- `ForgotPassword.razor:47-53`: solo envía el enlace si `IsEmailConfirmedAsync(user)` es verdadero.
- La base de datos revisada tiene todas las cuentas con `EmailConfirmed = 0`.

Resultado: el usuario nunca recibe el correo de recuperación, aunque la pantalla diga que lo haga.

Corrección recomendada:

- Decidir el flujo: confirmación obligatoria o recuperación sin confirmación.
- Si se exige confirmación: enviar confirmación al registrar, bloquear login hasta confirmar y ofrecer reenvío.
- Si no se exige: quitar la comprobación `IsEmailConfirmedAsync` del flujo de recuperación.
- Probar el flujo completo con un buzón real o un servidor SMTP de pruebas.

#### P0-3. Gestión de secretos y exposición de la API key de Gemini

**Estado: corregido (2026-10-23); rotación de credenciales pendiente por el propietario.** La key viaja en la cabecera `x-goog-api-key`, los errores ya no exponen `ex.Message` ni el detalle HTTP, y las credenciales locales se movieron a User Secrets dejando `appsettings.Development.json` sin secretos. `CorreoOptions` valida la configuración al arrancar.

Evidencia histórica:

- `appsettings.Development.json:11-20` contiene credenciales reales de Gemini y SMTP.
- El archivo está ignorado por Git (`.gitignore:489`), pero sigue siendo un secreto local.
- `Services/GlosarioService.cs:57`: la API key viaja en la URL.
- `Services/GlosarioService.cs:92-95`: un error devuelve `ex.Message` al usuario; puede incluir la URL con la key.

Acciones:

- Rotar las credenciales si el archivo se compartió o se subió a algún entorno.
- Moverlas a User Secrets/entorno.
- Enviar la key en la cabecera `x-goog-api-key`.
- No devolver mensajes de excepción crudos al usuario.
- Revisar que `.gitignore` siga excluyendo el archivo y que no exista en el historial.

### P1 - Alto

#### P1-4. Eliminación de cuenta y exportación de datos inconsistentes

**Estado: corregido (2026-10-23).** `IEliminacionCuentaService` borra en una transacción libros, travesías, glosario, amistades y usuario; `IExportacionDatosService` exporta todas las entidades; `DeletePersonalData` redirige al flujo nuevo. Pruebas en `EliminacionCuentaTests` y `ExportacionDatosTests`.

Evidencia:

- `Components/Account/Pages/Manage/EliminarCuenta.razor:73-83` elimina amistades y libros, pero no el glosario.
- `Models/PalabraGlosario.cs:6` guarda `UsuarioId` sin relación ni cascada en `ApplicationDbContext.cs:41-45`.
- `Components/Account/Pages/Manage/DeletePersonalData.razor:78-89` sigue accesible y elimina el usuario sin limpiar libros/amistades/glosario.
- Con amistades, las FK `Restrict` (`ApplicationDbContext.cs:25-35`) pueden hacer fallar `UserManager.DeleteAsync`.
- `Components/Account/Pages/Manage/PersonalData.razor:20-25` descarga un JSON que no incluye libros, reseñas, travesías, glosario ni amistades.
- `Privacidad.razor:22-23` promete borrar "todos tus libros, reseñas y puntuaciones".

Corrección recomendada:

- Un solo flujo de eliminación, transaccional y completo.
- Añadir FK/cascada o borrado explícito para `PalabrasGlosario` y `EntradasLectura`.
- Eliminar o proteger la ruta antigua `DeletePersonalData`.
- Ampliar la exportación de datos a todas las entidades del usuario.
- Añadir prueba de eliminación con libros, travesías, glosario y amistades.

#### P1-5. Falta de verificación de propiedad en servicios

**Estado: corregido (2026-10-23).** `ILibroService`, `IEntradaLecturaService` y `IGlosarioService.EliminarAsync` reciben `usuarioId` y filtran por propietario; las travesías validan el libro mediante join. Pruebas en `AutorizacionPropietarioTests`.

Evidencia:

- `Services/EntradaLecturaService.cs:16-52`: recibe `libroId` o `EntradaLectura` sin comprobar que pertenezcan al usuario actual.
- `Services/LibroService.cs:32-43`: `ActualizarAsync` y `EliminarAsync` confían en la entidad recibida.
- La UI comprueba `UsuarioId` en `Estantes.razor:413-432` y `:525-534`, pero la regla no está en la capa de servicio.

Riesgo: si en el futuro se expone un endpoint o se reutiliza el servicio, un usuario podría modificar recursos ajenos (IDOR).

Corrección recomendada:

- Pasar siempre `usuarioId` a los servicios y filtrar por propietario.
- Usar DTOs de entrada, no entidades completas.
- Añadir pruebas de autorización con dos usuarios.

#### P1-6. Sin rate limiting y códigos de amigo débiles/enumerables

**Estado: corregido (2026-10-23).** Rate limiting HTTP en login/registro/recuperación, cuota por usuario en amistades y glosario, códigos de 8 caracteres con `RandomNumberGenerator`, mensajes uniformes e índice único `ClavePar`. Pruebas en `RateLimitingTests`, `AmistadAntiabusoTests` y `CodigoAmigoGeneratorTests`.

Evidencia:

- No hay `AddRateLimiter` en `Program.cs`.
- `Services/AmistadService.cs:30-39`: `Random` no criptográfico, solo 10.000 combinaciones de 4 dígitos, 20 intentos y sin control del resultado de `UpdateAsync`.
- `Amigos.razor:30` sugiere un código alfanumérico de 6 caracteres, pero el servicio genera 4 dígitos.
- `AmistadService.cs:54-66` distingue entre "no existe" y "ya existe", lo que permite enumerar usuarios.
- `GlosarioService` puede consumir cuota de Gemini sin límite.
- Login y registro no tienen límite por IP.

Corrección recomendada:

- `RandomNumberGenerator` y códigos de 6-8 caracteres alfanuméricos.
- Rate limiting en login, registro, solicitudes de amistad y glosario.
- Mensaje uniforme para solicitudes de amistad.
- Índice único para pares de amistad y control de carreras.

#### P1-7. Producción sin HTTPS real

**Estado: corregido en código (2026-10-23); pendiente verificar en VPS Oracle.** `ForwardedHeaders`, HTTPS/HSTS y cookies `Secure` se activan solo con `Production` y `Seguridad:PermitirHttp=false`; el modo Tailscale puede usar HTTP con `Seguridad__PermitirHttp=true`. Ver sección 6.6.

Evidencia:

- `Properties/launchSettings.json` usa HTTP/HTTPS en desarrollo.
- El script publicado usa `http://localhost:5000`.
- `Dockerfile:12-18` expone HTTP en `8080`.
- `Program.cs:85-106` configura HSTS y redirección, pero sin TLS delante no hay protección efectiva.

Riesgo: cookies de sesión, credenciales y datos personales viajan sin cifrar si el servicio se expone fuera de localhost.

Corrección recomendada:

- Publicar detrás de un proxy inverso con TLS (Caddy, Nginx, IIS, Cloudflare Tunnel, etc.).
- Configurar `ForwardedHeaders` si hay proxy.
- Forzar HTTPS y cookies `Secure`.
- No exponer Kestrel directamente a Internet.

#### P1-8. La base de datos de desarrollo puede publicarse

**Estado: corregido (2026-10-23).** Se eliminó la copia de `Data/app.db` de `Otium.csproj`; `dotnet publish` verificado sin `Data/app.db`.

Evidencia:

- `Otium.csproj:11-13` copia `Data/app.db` a la salida.
- `Data/app.db` contiene usuarios, hashes de contraseña, libros y glosario de desarrollo.
- El `.dockerignore` la excluye para Docker, pero no para `dotnet publish` local.

Acciones:

- Quitar esa copia del `.csproj` o usar una base de datos de desarrollo separada y vacía.
- Nunca publicar `Data/app.db` con datos reales.
- Documentar la ruta de datos de producción (`C:\OtiumDatos\app.db`).

#### P1-9. La función social está incompleta respecto a lo que promete la privacidad

**Estado: corregido (2026-10-23).** `ApplicationUser.CompartirEstantes` (desactivado por defecto), vista `/amigos/{id}` con autorización por amistad y preferencia, eliminar amigo, bloquear/desbloquear y textos de privacidad/registro alineados. Travesías y glosario no se comparten. Pruebas en `CompartirEstantesTests`.

Evidencia histórica:

- `Amigos.razor` solo lista amigos por correo; no muestra estantes, libros ni actividad.
- `Privacidad.razor:14-17` dice que los amigos pueden ver "lo que decidas compartir".
- No hay opción de compartir, dejar de ser amigo, bloquear ni notificaciones.
- El registro dice "solo para ti y tus amigos" (`Register.razor:18`).

Acciones:

- Implementar el compartir o corregir el texto para que refleje el estado real.
- Añadir eliminar amigo/bloquear si se mantiene la función social.
- Definir qué datos ve un amigo y aplicar autorización explícita.

### P2 - Medio

#### P2-10. Sin pruebas, CI, documentación ni licencia

**Estado: parcialmente corregido (2026-10-23).** Existe `tests/Otium.Tests` con 36 pruebas, cobertura mínima del 70% de líneas (`coverlet.runsettings`) y CI en `.github/workflows/ci.yml`. Siguen faltando README, LICENSE, CHANGELOG, `.editorconfig` y `global.json`.

- ~~No hay proyecto de tests, workflows...~~ Corregido el proyecto y el workflow.
- Añadir como mínimo pruebas de:
  - Registro de dos usuarios.
  - Recuperación de contraseña.
  - Eliminación de cuenta con datos relacionados.
  - Autorización entre usuarios.
  - Generación/colisión de códigos de amigo.
  - Travesías y glosario.

#### P2-11. Validación de entrada insuficiente

**Estado: corregido (2026-10-23).** `Libro`, `EntradaLectura` y `PalabraGlosario` tienen DataAnnotations; los servicios validan títulos, autores, reseñas, puntuación 0-5, páginas no negativas y ordenadas, y longitud de palabra/título. Pruebas en `ValidacionEntradaTests`.

- ~~`Libro`, `EntradaLectura` y `PalabraGlosario` no tienen DataAnnotations ni validación de longitud.~~
- ~~`Puntuacion` no se valida en el servicio (solo la UI limita a 1-5).~~
- ~~`PaginaInicio`/`PaginaFin` no validan negativos ni orden.~~
- ~~`Resena` y `Resumen` no tienen longitud máxima.~~
- ~~`GlosarioService` no limita el tamaño de la palabra ni del título.~~
- ~~`EntradaLecturaService.AgregarAsync` no valida que `libroId` exista o pertenezca al usuario.~~

#### P2-12. Numeración de travesías con carreras y reutilización

- `EntradaLecturaService.cs` usa `Max(Numero) + 1`.
- Dos altas simultáneas pueden generar el mismo `Numero`.
- Al borrar la última entrada, el siguiente `Max + 1` puede reutilizar un número ya usado.
- No hay índice único `(LibroId, Numero)`.
- Usar un contador persistido o una transacción con bloqueo, y añadir restricción.

#### P2-13. Fugas de información en mensajes de error

- `Estantes.razor` concatena `ex.Message` al mostrar errores de búsqueda.
- `IdentityComponentsEndpointRouteBuilderExtensions.cs:64` y `:122` devuelven IDs internos en errores.
- `EmailSender.cs` registra la excepción con destinatario (correcto), pero el flujo de UI debe mostrar mensajes genéricos.

#### P2-14. Cabeceras y CSP mejorables

- `Program.cs:94-101` permite `'unsafe-inline'` en scripts y estilos.
- `connect-src` no incluye `ws:` para desarrollo HTTP.
- Se observaron dos cabeceras CSP en la respuesta (la propia y `frame-ancestors 'self'`), conviene unificar.
- `X-Frame-Options: DENY` y `frame-ancestors 'self'` no son equivalentes; alinear la política.
- `AllowedHosts: "*"` en `appsettings.json:11`; restringir en producción.

#### P2-15. `async void` y excepciones silenciadas

- `Components/Layout/BotonTema.razor:44-47` usa `async void`.
- `BotonTema.razor:57-60` y `:69-71` capturan y silencian todas las excepciones.
- `BotonInstalar.razor:77` también silencia.
- Usar un manejador seguro, registrar el error y no tragar excepciones.

#### P2-16. Código y pantallas redundantes

- `Components/Account/IdentityNoOpEmailSender.cs` no se usa (se registra `EmailSender`).
- `DeletePersonalData.razor` y `PersonalData.razor` son rutas heredadas no enlazadas en el menú.
- `Email.razor`, `ChangePassword` y otras pantallas de Identity conviven en inglés/español.
- `Invitacion:Codigo` ya no se usa en el fuente actual.
- Hay reglas CSS duplicadas en `Estantes.razor.css` (`.entradas-lectura`, `.entrada-cabecera`, `.quitar-entrada`, `.form-entrada`, `.campo-resumen`, `.bloc-notas`).
- `AddDbContext` y `AddDbContextFactory` duplican la configuración de SQLite (`Program.cs:27-30`).

#### P2-17. `EmailSender` frágil

**Estado: corregido (2026-10-26).** `EmailSender` delega en `IEnvioCorreo`; el transporte es la API HTTP de Brevo (`BrevoEnvioCorreo`) con opciones tipadas y validadas. Usa `POST` con cabecera `api-key`, timeout de 15 s, cancelación y captura de código/mensaje HTTP sin romper la petición. El arranque registra si el correo está configurado. Sigue pendiente valorar una cola/outbox.

- ~~`Services/EmailSender.cs:33-37` usa `int.Parse` y claves opcionales sin validación.~~
- ~~Si falta `Smtp:Host`, `Smtp:Username` o `Smtp:Password`, falla en tiempo de ejecución.~~
- ~~`SmtpClient` es una API heredada; valorar MailKit.~~ Superado: MailKit/SMTP se sustituyó por la API HTTP de Brevo.
- Enviar correo dentro de la petición puede aumentar la latencia; valorar cola/outbox.
- ~~No hay reintentos, cancelación ni verificación de configuración al arrancar.~~
- ~~Render free bloquea los puertos SMTP `25`, `465` y `587`; usar `Smtp__Port=2525` (Brevo) o la API HTTP de Brevo.~~ Resuelto al migrar a la API HTTP de Brevo.

> **Actualizado (2026-10-26):** el transporte de correo es ahora la API HTTP de Brevo
> (`BrevoEnvioCorreo`); MailKit/SMTP quedó eliminado. Ver `README.md`.

#### P2-18. Sin respaldos ni recuperación documentada

**Estado: corregido (2026-10-25).** `BackupService` implementado con `CrearRespaldoAsync()`,
`VerificarIntegridadAsync()` y retención de 5 archivos; registrado en `Program.cs`.
Documentación de RPO/RTO y procedimiento de restauración en `DOCS/respaldos.md`.

#### P2-19. Docker y operación

- `Dockerfile` corre como root, no declara `VOLUME /data`, no tiene `HEALTHCHECK` y no fija imágenes por digest.
- `DataProtection:KeysPath=/data/keys` requiere que el directorio exista y tenga permisos.
- No hay límites de recursos, logs persistentes ni estrategia de actualización.
- `db.Database.Migrate()` al arrancar puede fallar y tumbar la app; en producción conviene un paso de migración controlado.

#### P2-20. Dependencias externas sin resiliencia

**Estado: parcialmente corregido (2026-10-23).** Open Library y Gemini tienen timeout, reintentos acotados y cancelación; las búsquedas se cachean 10 minutos y los errores de Open Library son mensajes amigables. Falta circuit breaker y caché de definiciones.

- ~~Open Library y Gemini no tienen reintentos, circuit breaker, caché ni cancelación.~~ Quedan circuit breaker y caché de definiciones.
- ~~El `User-Agent` de Open Library usa un correo de ejemplo.~~ Ahora usa `Otium/1.0`.
- ~~Si Open Library falla, la UI muestra el error crudo.~~ El cliente lanza un mensaje amigable.
- Añadir timeout, reintentos acotados, caché y mensajes amigables.

> **Actualizado (2026-10-26):** Render free bloquea los puertos SMTP salientes. El correo usa
> ahora la API HTTP de Brevo; ya no se requiere `Smtp__Port` ni MailKit.

### P3 - Bajo

- `NotFound.razor` y `Error.razor` siguen en inglés. [corregido]
- `MenuUsuario` no muestra "Salir"; el logout está dentro de "Mi cuenta".
- `CopiarCodigo` no captura errores de `navigator.clipboard` (puede fallar fuera de contexto seguro). [corregido]
- `BotonInstalar` no protege la llamada JS de la primera renderización. [corregido]
- Las imágenes de portada usan `alt=""`; correcto si son decorativas, pero la tarjeta debería describir el libro.
- No hay `robots.txt`, `security.txt`, términos de uso ni página de soporte. [corregido: wwwroot/robots.txt, wwwroot/.well-known/security.txt, /terminos, /soporte]
- El glosario no evita duplicados y no permite editar definiciones. [corregido]
- La búsqueda de libros no guarda caché ni permite paginación.
- No hay auditoría de accesibilidad ni pruebas en móviles reales.
- No hay telemetría de errores (Sentry, Application Insights, etc.). [corregido: Sentry.AspNetCore registrado, solo envía si Sentry:Dsn está configurado]

---

## 12. Orden recomendado de corrección

1. **P0-1:** ~~arreglar `CodigoAmigo` y probar dos registros consecutivos.~~ Completado.
2. **P0-2:** ~~decidir y arreglar el flujo de confirmación/recuperación.~~ Completado (Opción B).
3. **P0-3:** ~~rotar y mover secretos; key de Gemini fuera de la URL.~~ Completado; rotación manual pendiente.
4. **P1-4:** ~~unificar eliminación y exportación de datos.~~ Completado.
5. **P1-5:** ~~autorización por propietario en servicios.~~ Completado.
6. **P1-6:** ~~rate limiting y códigos de amigo robustos.~~ Completado.
7. **P1-7/P1-8:** ~~TLS de entrada y dejar de publicar la base de desarrollo.~~ Completado en código; verificar VPS.
8. **P1-9:** ~~completar o corregir la función social y la privacidad.~~ Completado (Opción B).
9. **P2:** pruebas, CI, validación, errores, CSP, respaldos, Docker.
10. **P3:** pulido de UX, i18n y accesibilidad.

---

## 13. Definition of Done para cambios

Un cambio se considera terminado cuando:

- Compila en Release sin errores ni advertencias nuevas.
- No introduce secretos ni datos personales en el repositorio.
- Respeta la separación UI/servicio/datos existente o mejora la existente.
- Incluye validación de entrada y autorización si toca datos de usuario.
- Actualiza o crea la migración correspondiente si cambia el modelo.
- Tiene una prueba automatizada si corrige un flujo crítico.
- Actualiza este `AGENTS.md` si cambia arquitectura, configuración o estado de un hallazgo.

---

## 14. Diferencias entre fuente y publicación

- `C:\Otium\OtiumServidor` corresponde a un build anterior (`1bcddde`) y todavía usa código de invitación.
- `C:\Proyectos\Otium` está en `0f02b1b` y ya no usa invitación.
- El script publicado configura `Invitacion__Codigo`, pero el fuente actual no lo lee.
- La base publicada en `C:\Otium\OtiumServidor\Data\app.db` no es la base activa; la activa es `C:\OtiumDatos\app.db`.
- Cualquier cambio debe hacerse en el fuente y volver a publicar; no editar los archivos de `C:\Otium`.

---

## 15. Resumen ejecutivo

Otium tiene una base funcional y ordenada para un proyecto personal: Blazor Server, Identity, EF Core/SQLite, servicios con interfaces, PWA y una UI cuidada. La compilación está limpia y no hay vulnerabilidades conocidas en paquetes.

Sin embargo, **todavía no está listo para lanzamiento público**: P0 y P1 están corregidos y cubiertos por 20 pruebas, pero falta verificar TLS/SignalR/PWA en el VPS Oracle y completar P2 (CI, respaldos, validación de entrada, manejo de errores, CSP y endurecimiento de Docker).

Con P0/P1 cerrados, la app es viable como beta privada. Antes de abrirla al público, completar P2 y añadir CI/respaldos.
