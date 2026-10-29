# ROADMAP.md — Otium — Backlog de hallazgos pendientes

Plan de trabajo vigente: solo contiene **hallazgos a corregir**. Los P0 ya cerrados
(registro/códigos de amigo, recuperación de contraseña Opción B, secretos) y los P1
(eliminación/exportación, autorización por propietario, BD fuera de publicación,
rate limiting, función social) están completados y documentados en `AGENTS.md` §11;
no se repiten aquí.

- **Proyecto:** `C:\Proyectos\Otium`
- **Auditoría base:** auditoría integral de código del 2026-10-28 (commit `33ce448`)
- **Última actualización:** 2026-10-28

---

## 0. Reglas de ejecución

1. Trabajar siempre en `C:\Proyectos\Otium`, nunca en el build publicado `C:\Otium`.
2. Un PR por tarea o por bloque pequeño; no mezclar prioridades en un mismo PR.
3. Cada corrección incluye prueba automatizada o procedimiento de verificación reproducible.
4. Antes de cerrar: `dotnet build Otium.csproj -c Release --nologo` sin advertencias nuevas
   y `dotnet test Otium.slnx -c Release --nologo` en verde.
5. Al cerrar un hallazgo: marcar `[x]`, añadir commit de evidencia y actualizar `AGENTS.md`.
6. No commitear secretos, bases de datos ni `bin/`, `obj/`, `.vs/`.

Esfuerzo: **S** < 2 h · **M** 2–6 h · **L** 1–2 días · **XL** +2 días o decisión de producto.

---

## 1. Resumen del backlog

| ID | Hallazgo | Prioridad | Esfuerzo | Estado |
|---|---|---|---|---|
| F-01 | La cuota diaria de glosario se consume aunque el modelo falle | P1 | S | [ ] |
| F-02 | `BackupService` asume SQLite (`File.Copy`); rompe con PostgreSQL y silencia errores de retención | P1 | M | [ ] |
| F-03 | `/terminos` aún cita "Gemini de Google" como proveedor de IA | P1 | S | [ ] |
| F-04 | `robots.txt` declara `sitemap.xml` inexistente; `security.txt` con dominio placeholder desalineado de `/soporte` | P1 | S | [ ] |
| F-05 | Errores de alta de libro/travesía invisibles para el usuario (estados muertos en `Estantes.razor`) | P1 | S | [ ] |
| F-06 | Docker incoherente: root sin `USER`, `/var/data` vs `/data` del compose, healthcheck con `curl`, imágenes sin digest | P1 | M | [ ] |
| F-07 | `CircuitBreaker` no thread-safe e instanciado por cliente (no pasa por DI) | P2 | S | [ ] |
| F-08 | CSP laxa (`'unsafe-inline'` en script/style, `ws:` siempre) y `AllowedHosts: "*"`; 429 sin página propia | P2 | M | [ ] |
| F-09 | Workflows duplicados: `build.yml` redundante con el mismo `name: CI`; trigger de release poco coherente en `deploy.yml` | P2 | S | [ ] |
| F-10 | Fugas de información interna en endpoints de Identity (IDs en errores) — legacy P2-13 | P2 | S | [ ] |
| F-11 | Texto en inglés en el shell: `ReconnectModal` y pantallas de 2FA/passkeys — legacy P2-11 | P2 | M | [ ] |
| F-12 | Duplicaciones de código: mapeo `LibroActualizarDTO` ×3 y `Nombre(Estante)` ×3 | P2 | S | [ ] |
| F-13 | Último `async void` del fuente (`BotonTema.AlCambiarDePagina`) y `catch { }` en `BotonInstalar.DisposeAsync` | P2 | S | [ ] |
| F-14 | `AddDbContext` + `AddDbContextFactory` registran la misma configuración SQLite dos veces — reabre P2-16 | P2 | S | [ ] |
| F-15 | Re-render redundantes de `StateHasChanged` en `BuscadorLibros` (2 llamadas manuales) | P3 | S | [ ] |
| F-16 | "Salir" no existe en el menú (`MenuUsuario`); el logout solo está dentro de Mi cuenta | P3 | S | [ ] |
| F-17 | Auditoría formal de accesibilidad (contraste, foco, lectores de pantalla) y prueba en móvil real | P3 | L | [ ] |
| F-18 | Documentación desactualizada: `AGENTS.md` dice códigos de 8 caracteres (son 6) y "36 pruebas" (hay 64) | P3 | S | [ ] |
| F-19 | Paginación `ObtenerDeUsuarioPaginadoAsync` implementada pero sin usar | P3 | S | [ ] |
| F-20 | Operativo: rotar credenciales si se compartieron (P0-3) y verificar TLS/SignalR/PWA en VPS Oracle (P1-7) | P1 | M | [ ] |

---

## 2. Hallazgos P1 — antes de lanzar al público

### F-01 — La cuota diaria del glosario se consume aunque el modelo falle

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `Services/GlosarioService.cs:96-107`

**Problema:** `_limites.IntentarCuotaDiaria("glosario:{usuarioId}", 50)` se ejecuta **antes**
de llamar al modelo. Si `DefinirAsync` devuelve `null` (timeout, JSON inválido, 429 del
proveedor…), el intento cuenta igual y el usuario pierde una de sus 50 definiciones/día
sin resultado.

**Pasos:**
1. Mover la verificación de cuota después de la llamada, o devolver el intento cuando el
   resultado es `null` (añadir `DevolverCuota(clave)` a `ILimiteIntentosService`).
2. Cuidar la carrera obvia: no permitir reembolsar la misma llamada dos veces.
3. Añadir prueba en `GlosarioServiceTests`: fallo del modelo no consume cuota.

**Criterio de aceptación:** un fallo del modelo no reduce la cuota del usuario; el límite
sí se mantiene ante palabras duplicadas válidas y abuso real.

---

### F-02 — Respaldos: asumen SQLite y silencian errores de retención

**Estado:** [ ] Pendiente · **Esfuerzo:** M · **Lugar:** `Services/BackupService.cs:33-74`

**Problema:**
1. `CrearRespaldoAsync` hace `PRAGMA wal_checkpoint(TRUNCATE)` + `File.Copy` de la BD
   local. Con PostgreSQL (Render) el `SqliteConnectionStringBuilder` no resuelve la ruta y
   el servicio no produce respaldo.
2. `AplicarRetencion` captura excepciones silenciosamente en la eliminación de respaldos
   viejos (aceptado solo por comentario).

**Pasos:**
1. Detectar proveedor (`ConfiguracionBaseDatos`): en PostgreSQL usar `pg_dump`/API u
   omitir el respaldo de archivos y documentarlo en `DOCS/respaldos.md`.
2. Registrar Warning visible cuando la retención no puede borrar; nunca `catch {}` sin log.
3. Prueba de integración: respaldo creado y verificado con retención en SQLite temporal.

**Criterio de aceptación:** en SQLite respaldo íntegro con retención y log de errores; en
PostgreSQL comportamiento explícito documentado (respaldo gestionado o fallback claro).

---

### F-03 — Los términos de uso citan "Gemini de Google" (proveedor incorrecto)

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `Components/Pages/Terminos.razor:23`

**Problema:** el servicio real es una API OpenAI-compatible (Z.ai por defecto,
conmutable a OpenRouter) con el modelo `glm-5.3-flash` vía `IDefinicionClient`. El
texto legal menciona un proveedor que ya no se usa desde 2026-10-28.

**Pasos:**
1. Redactar el párrafo de IA sin fijar el proveedor en duro: "servicio de inteligencia
   artificial externo accesible mediante API REST (proveedores Z.ai u OpenRouter, modelo
   GLM-5.3-Flash)". Así no caduca otra vez.
2. Revisar que `/privacidad` y `AvisoBienvenida` usen la misma formulación.

**Criterio de aceptación:** la mención legal coincide con la implementación y no nombra
un proveedor fijo salvo que sea consciente.

---

### F-04 — SEO/ops: `sitemap.xml` inexistente y `security.txt` con placeholders

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `wwwroot/robots.txt`,
`wwwroot/.well-known/security.txt`

**Problema:**
1. `robots.txt` declara `Sitemap: /sitemap.xml` que no existe (enlace roto declarado).
2. `security.txt` usa `otium.example.com` (dominios placeholder) y contactos que no
   coinciden con `/soporte` (`otium.lectura@gmail.com`).

**Pasos:**
1. Quitar la línea `Sitemap` o generar un `sitemap.xml` mínimo (solo rutas públicas:
   `/privacidad`, `/terminos`, `/soporte`).
2. Fijar dominio real y contacto de `/soporte`; actualizar `Expires` anualmente.
3. Si la app se despliega bajo dominio personalizado, parametrizar por entorno/despliegue.

**Criterio de aceptación:** ningún enlace declarado apunta a un recurso inexistente;
contactos reales y verificables.

---

### F-05 — Errores de alta de libro/travesía invisibles (estados muertos)

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `Components/Pages/Estantes.razor:145,
148`; `Components/Pages/EditorResena.razor:127-128`

**Problema:** `errorFormulario` y `errorEntrada` se asignan en los handlers del padre
(`OnGuardarLibro`, `OnAñadirTravesía`, `OnQuitarTravesía`) pero **nunca se renderizan**
(la UI muestra los errores de los hijos). Si fallan en nivel clase-padre, el usuario no
ve nada. Además, el parámetro `OnToggleTravesía` de `EditorResena` no lo usa nadie.

**Pasos:**
1. Unificar el contrato de errores: o el hijo muestra los errores del servicio, o el
   padre los renderiza; elegir y quitar el estado duplicado.
2. Eliminar `OnToggleTravesía` (código muerto).
3. Añadir prueba de UI mínima (harness de pruebas) o verificación manual reproducible.

**Criterio de aceptación:** cualquier fallo de alta/edición/eliminación se ve en la UI
con mensaje en español.

---

### F-06 — Docker incoherente y sin endurecer

**Estado:** [ ] Pendiente · **Esfuerzo:** M · **Lugar:** `Dockerfile`, `docker-compose.yml`

**Problema:**
1. El contenedor corre como **root** (no existe `USER`) — ⚠ el roadmap anterior lo marcaba
   como completado; la auditoría del 2026-10-28 confirma que **no** lo está.
2. El Dockerfile prepara `/var/data/keys` pero el compose monta el volumen en `/data` y
   define `DataProtection__KeysPath=/data/keys` (funciona por el ENV, pero desalineado).
3. `healthcheck` del compose usa `curl`, que puede no estar en la imagen runtime de aspnet.
4. Imágenes base sin digest (etiqueta flotante `10.0`).
5. `ASPNETCORE_URLS` del compose es ignorado (el código fija `http://0.0.0.0:{PORT}`).

**Pasos:**
1. Añadir `USER` no root con permisos sobre `/data`; unificar ruta de datos (`/data`).
2. Healthcheck en C# compacto (`dotnet` + HttpClient) o instalar `curl` explícitamente.
3. Fijar imágenes por digest; revisar la coherencia de `GC`/`InvariantGlobalization` con el csproj.
4. Alinear `ASPNETCORE_URLS` (o dejar que el código gobierna y quitar la variable del compose).
5. Verificar el arranque local: `docker compose up` + `/health` respondiendo OK.

**Criterio de aceptación:** contenedor no-root, un único volumen de datos, healthcheck
funcional en la imagen, una sola ruta de datos y documentación alineada.

---

### F-20 — Operativo: rotación de credenciales y verificación del VPS Oracle

**Estado:** [ ] Pendiente por el propietario · **Esfuerzo:** M · **Lugar:** despliegue

**Problema:**
1. **Rotación de credenciales** (P0-3) sigue pendiente desde el 2026-10-23: rotar
   `Definiciones` API key, `Brevo:ApiKey` y evaluar historial de Git si se compartieron.
2. **Verificación de producción en el VPS Oracle** (P1-7): comprobar TLS real, cookies
   `Secure`, login, SignalR (`wss:`), PWA y recuperación de contraseña detrás de Caddy/Nginx.

**Pasos:**
1. Rotar claves y actualizar User Secrets/entorno.
2. Ejecutar el checklist manual de `AGENTS.md` §6.6 sobre dominio real.
3. Registrar evidencia (capturas / comandos) en `AGENTS.md`.

**Criterio de aceptación:** credenciales viejas invalidadas; checklist del VPS completo.

---

## 3. Hallazgos P2 — endurecimiento

### F-07 — `CircuitBreaker` no thread-safe y fuera de DI

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `Services/CircuitBreaker.cs`,
`DefinicionesClient.cs:65`, `BuscadorLibros.cs:27`

**Problema:** el estado (`_fallosConsecutivos`, `_estado`, `_horaSiguienteIntento`) se
muta sin sincronización; con la concurrencia de los circuitos Blazor Server, pueden
golpearse más de 3 fallos sin que el circuito se abra. Se instancia
con `new()` por cada servicio, sin pasar por DI.

**Pasos:**
1. Proteger transición de estado con `lock` o `Interlocked` (estado volatile).
2. Registrar como singleton y limitar su tasa mediante `IMemoryCache` (opcional).
3. Añadir prueba unitaria de concurrencia (20 llamadas paralelas fallidas → abre una vez).

**Criterio de aceptación:** estados consistentes bajo concurrencia; pruebas en verde.

---

### F-08 — CSP y hosts permitidos laxos; 429 sin página propia

**Estado:** [ ] Pendiente · **Esfuerzo:** M · **Lugar:** `Program.cs:420-426`,
`Program.cs:272-284`, `appsettings.json:30`

**Problema:**
1. CSP mantiene `'unsafe-inline'` en `script-src` **y** `style-src` (requerido por
   Blazor inline styles, conservador pero necesario). `connect-src` incluye `ws:`
   incondicional (en producción debería ser solo `wss:`).
2. `AllowedHosts: "*"` sin restringir (P2-14 legacy).
3. `OnRejected` del rate limiter solo loguea: el cliente recibe `429` vacío sin mensaje.
4. Se solapan dos cabeceras de protección: `X-Frame-Options: DENY` y sin
   `frame-ancestors` en el CSP.

**Pasos:**
1. Generar CSP por entorno: incluir `ws:` solo en Development; en Production solo `wss:`.
2. Reducir `unsafe-inline` a hashes/nonces donde Blazor lo permita (o documentar por qué
   se mantiene y cerrar).
3. Delimitar `AllowedHosts` al host real (variables de entorno).
4. Añadir `frame-ancestors 'self'` idéntico al `X-Frame-Options` o quitar esta última
   (el estándar moderno es el CSP).
5. Devolver cuerpo JSON/HTML mínimos en `OnRejected` con mensaje en español.

**Criterio de aceptación:** un solo mecanismo de frame; `AllowedHosts` cerrado; 429
explicado; sin `ws:` en producción.

---

### F-09 — Workflows de GitHub duplicados o incoherentes

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `.github/workflows/{ci,build,deploy}.yml`

**Problema:**
1. `ci.yml` y `build.yml` tienen el mismo `name: CI`; `build.yml` es un subconjunto
   (solo restore+build) y compila todo dos veces en cada push.
2. `deploy.yml`: el trigger es `push` a `main` + `workflow_dispatch`, pero el paso de
   release solo se dispara para `refs/tags/`, que no ocurre en el trigger declarado.
3. `build.yml` no limita `permissions`.

**Pasos:**
1. Eliminar `build.yml` (todo su contenido está en `ci.yml`).
2. En `deploy.yml`, añadir el trigger `push: tags: ['v*']` o convertirlo en
   `workflow_dispatch` manual con parámetro de tag.
3. Añadir `permissions: contents: read` faltante.

**Criterio de aceptación:** un solo workflow de CI; release solo en tags reales;
permisos mínimos.

---

### F-10 — Fugas de información interna en endpoints de Identity (legacy P2-13)

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** endpoints de Identity de
`Components/Account/*` servidos por `MapAdditionalIdentityEndpoints` (extensión de la
plantilla), en particular errores que devuelven IDs internos al usuario.

**Problema:** mensajes de error de endpoints de Identity (registro/login/2FA) pueden
exponer `statuscode`/IDs internos que no aportan al usuario final.

**Pasos:**
1. Revisar los errores devueltos por los endpoints (`Dictionary<string, string>` de
   Identity) y mapear a mensajes genéricos (usar `DescriptorErroresEs` ya existente).
2. Verificar con pruebas de integración que ningún mensaje contiene identificadores.

**Criterio de aceptación:** ningún texto visible contenga IDs técnicos.

---

### F-11 — Texto en inglés residual (i18n)

**Estado:** [ ] Pendiente · **Esfuerzo:** M · **Lugar:** `Components/Layout/ReconnectModal.razor`,
`Components/Account/Pages/` (LoginWith2fa, LoginWithRecoveryCode, RegisterConfirmation,
ResendEmailConfirmation, ExternalLogin, Manage/Disable2fa, EnableAuthenticator,
ExternalLogins, GenerateRecoveryCodes, Passkeys, RenamePasskey, ResetAuthenticator,
SetPassword, TwoFactorAuthentication — títulos/leyendas en inglés)

**Problema:** el único idioma soportado es el español; resta traducción.
`ReconnectModal` visible durante cortes de sesión Blazor Server (de la plantilla).

**Pasos:**
1. Traducir `ReconnectModal` (los textos más visibles: "Rejoining the server…" etc.).
2. Traducir cabeceras y etiquetas de 2FA/passkeys si se hacen visibles en el flujo.

**Criterio de aceptación:** cero strings de usuario en inglés en las rutas navegables.

---

### F-12 — Duplicaciones de mapeo y etiquetas (mantenibilidad)

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `Components/Pages/Estantes.razor`
(`CambiarEstante`/`Puntuar`/`OnGuardarReseña` reconstruyen el `LibroActualizarDTO` campo a
campo en 3 lugares), `Nombre(Estante)` triplicado (Estantes/FormularioLibro/EstantesAmigo)

**Pasos:**
1. Centralizar `Nombre(Estante)` en `Models/Estante` (o extensión) y quitar copias.
2. Construir `LibroActualizarDTO` desde un único punto común (método helper o método de
   extensión del DTO).
3. Opcional: extraer CSS recurrente `.ficha` del scoped (ya parcialmente unificado).

**Criterio de aceptación:** una sola fuente de verdad para mapeos y nombres.

---

### F-13 — `async void` residual y captura silenciosa

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `Layout/BotonTema.razor:45` 
(`AlCambiarDePagina`, el único `async void` del fuente), `Layout/BotonInstalar.razor:85`
(`DisposeAsync` con `catch { }` vacío)

**Pasos:**
1. Convertir `AlCambiarDePagina` a `async Task` con captura interna o migrar a
   fire-and-forget explícito (`_ = SafeSyncAsync()` con try/catch + log).
2. Añadir log en el `catch` de `DisposeAsync` (o documentar por qué está vacío).

**Criterio de aceptación:** ningún `async void` en el fuente; ningún `catch {}` sin
justificación en comentario.

---

### F-14 — Doble registro de DbContext (reabre P2-16)

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:**
`Services/Extensions/ServiceCollectionExtensions.cs` (`AddBaseDeDatos`)

**Problema:** pese a que el roadmap anterior (commit `b097289`) lo marcaba como
unificado, el método registra **el mismo `Action<DbContextOptionsBuilder>` en
`AddDbContext` y `AddDbContextFactory`** (el factory con `Scoped`), duplicando la
configuración del proveedor SQLite/Npgsql.

**Pasos:**
1. Explorar `AddDbContextFactory` puro (Blazor Server ya resuelve el ámbito scoped a
   nivel de factory) o mantener ambos con un builder compartido explícito y comentario.
2. Verificar que los servicios que usan `IDbContextFactory` y `ApplicationDbContext`
   (Identity) siguen resolviendo bien.

**Criterio de aceptación:** una sola declaración de configuración; `dotnet test` verde.

---

## 4. Hallazgos P3 — pulido y calidad

### F-15 — Re-renders redundantes en `BuscadorLibros`

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `Components/Pages/BuscadorLibros.razor:87,102`

**Pasos:** mover los cambios de estado previos al `await` para confiar en el re-render
del circuito, o reducir a una sola `StateHasChanged`. Verificar el comportamiento del
spinner y errores visibles.

**Criterio de aceptación:** los estados siguen visibles y no hay llamadas manuales redundantes.

---

### F-16 — Falta "Salir" en el menú

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `Components/Layout/MenuUsuario.razor`

**Pasos:** añadir opción "Salir" que encapsule el formulario del POST `/Account/Logout`
con confirmación visual; mantener la opción dentro de "Mi cuenta" funcionando igual para
no cambiar el comportamiento actual.

**Criterio de aceptación:** el usuario cierra sesión desde el menú superior visible.

---

### F-17 — Auditoría formal de accesibilidad

**Estado:** [ ] Pendiente · **Esfuerzo:** L · **Lugar:** global (App, CSS, ARIA)

**Pasos:** pasar herramientas (axe/Lighthouse), revisar contraste AA en ambos temas,
foco en modales (`ReconnectModal`, `AvisoBienvenida`), navegación por teclado,
y probar en un móvil real (iOS Safari + Android Chrome). Registrar hallazgos aquí.

**Criterio de aceptación:** informe con errores AA corregidos y demostración en móvil.

---

### F-18 — Documentación desactualizada en AGENTS.md

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `AGENTS.md`

**Problema:** códigos de amigo son de **6** caracteres (`CodigoAmigoGenerator.cs:7`)
y la suite tiene **64 métodos / 66 casos** (`tests/Otium.Tests`); el doc dice 8 y 36.
También menciona Gemini ya no vigente y describe `CircuitBreaker`/caché como pendientes
cuando ya existen.

**Pasos:** actualizar estado real en `AGENTS.md` (fecha/auditoría 2026-10-28) y
corregir cualquier sección afín.

**Criterio de aceptación:** ningún dato del documento contradiga el código.

---

### F-19 — Paginación de libros implementada pero sin usar

**Estado:** [ ] Pendiente · **Esfuerzo:** S · **Lugar:** `ILibroService.ObtenerDeUsuarioPaginadoAsync`

**Pasos:** decidir (a) usar paginación en `Estantes.razor` si la biblioteca crece, o
(b) eliminar el método no usado (código muerto sin valor de prueba).

**Criterio de aceptación:** no queda código sin uso y los listados grandes quedan paginados.

---

## 5. Seguimiento

Al cerrar cada hallazgo:

1. Cambiar estado `[ ]` → `[x]` y añadir commit/PR de referencia.
2. Correr `dotnet build Otium.csproj -c Release --nologo` y `dotnet test Otium.slnx -c Release --nologo`.
3. Actualizar `AGENTS.md` si tocara arquitectura o configuración.
4. Los hallazgos operativos (F-20) solo cierran con evidencia del propietario.

Estados: `[ ]` Pendiente · `[~]` En curso · `[x]` Completado · `[!]` Bloqueado ·
`[-]` Descartado/aceptado como riesgo.
