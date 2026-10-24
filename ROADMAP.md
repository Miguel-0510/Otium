# ROADMAP.md - Otium

Plan de ejecución para llevar Otium de beta privada a un estado apto para lanzamiento.

- **Proyecto:** `C:\Proyectos\Otium`
- **Auditoría base:** `AGENTS.md`
- **Commit de partida:** `0f02b1b`
- **Última actualización:** 2026-10-23

---

## 0. Reglas de ejecución

1. Trabajar siempre en `C:\Proyectos\Otium`, nunca en el build publicado `C:\Otium`.
2. Un PR por tarea o por bloque pequeño. No mezclar P0 con refactorizaciones P2.
3. No commitear secretos, bases de datos ni archivos de `bin/`, `obj/` o `.vs/`.
4. Cada corrección debe incluir una prueba automatizada o, como mínimo, un procedimiento de verificación reproducible.
5. No iniciar P1 hasta que los tres P0 estén corregidos y verificados en un entorno de staging.
6. No iniciar P2 hasta que P1 esté cerrado o exista una decisión explícita de posponerlo.
7. Actualizar `AGENTS.md` cuando cambie arquitectura, configuración o el estado de un hallazgo.
8. Antes de cada cierre: `dotnet build -c Release` sin advertencias nuevas.

Convención sugerida de ramas:

```text
fix/p0-registro-codigo-amigo
fix/p0-recuperacion-contrasena
chore/p0-secretos-gemini
fix/p1-eliminacion-datos
fix/p1-autorizacion-propietario
fix/p1-rate-limiting-codigos
chore/p1-https-despliegue
chore/p1-base-datos-publicacion
fix/p1-social-privacidad
refactor/p2-solid
```

Definición de esfuerzo:

- **S:** menos de 2 horas.
- **M:** entre 2 y 6 horas.
- **L:** entre 1 y 2 días.
- **XL:** más de 2 días o requiere decisión de producto.

---

## 1. Orden general

| Fase | Contenido | Resultado esperado |
|---|---|---|
| Fase 0 | Red de seguridad mínima | Poder verificar P0 con pruebas reproducibles |
| Fase 1 | P0: Bloqueantes | Registro, recuperación y secretos corregidos |
| Fase 2 | P1: Seguridad y riesgos altos | Datos, autorización, abuso y despliegue controlados |
| Fase 3 | P2: Refactorización | Código mantenible, probado y observable |
| Fase 4 | Lanzamiento | Checklist de producción completo |

Secuencia recomendada:

```text
P0.0 Red de seguridad
  -> P0.1 Registro / CodigoAmigo
  -> P0.2 Recuperación de contraseña
  -> P0.3 Secretos y Gemini
  -> Validación en staging
  -> P1.1 Eliminación y exportación de datos
  -> P1.2 Autorización por propietario
  -> P1.3 Base de datos fuera de publicación
  -> P1.4 HTTPS/TLS
  -> P1.5 Rate limiting y códigos de amigo
  -> P1.6 Función social y privacidad
  -> Validación en staging
  -> P2 Refactorizaciones
  -> Checklist final de lanzamiento
```

---

## 2. P0 - Bloqueantes

Objetivo: que un usuario nuevo pueda registrarse, recuperar su contraseña y operar sin exponer credenciales.

### P0.0 - Red de seguridad mínima

**Estado:** [x] Completado (2026-10-23)  
**Evidencia:** `tests/Otium.Tests`, `Otium.slnx`; `dotnet test Otium.slnx -c Release` ejecuta 7 pruebas verdes sin tocar `Data/app.db`.
**Prioridad:** P0 habilitador  
**Esfuerzo:** M  
**Depende de:** nada

**Objetivo:** poder comprobar los tres bloqueantes sin depender de pruebas manuales.

**Pasos:**

1. Crear un proyecto de pruebas de integración, por ejemplo `Otium.Tests`.
2. Usar una base SQLite temporal o `WebApplicationFactory` con base en memoria/archivo temporal.
3. Implementar un `IEmailSender<ApplicationUser>` falso que registre los enlaces enviados.
4. Añadir pruebas iniciales:
   - Registro de un usuario.
   - Registro de dos usuarios consecutivos.
   - Solicitud de recuperación de contraseña.
   - Eliminación de cuenta con datos relacionados.
5. Añadir un comando único de verificación:

```powershell
dotnet test
```

**Criterio de aceptación:**

- `dotnet test` ejecuta al menos una prueba de registro y una de recuperación.
- Las pruebas no tocan `Data/app.db` ni `C:\OtiumDatos\app.db`.
- El test de dos registros reproduce el fallo actual antes de corregirlo.

---

### P0.1 - Corregir el registro y `CodigoAmigo`

**Estado:** [x] Completado (2026-10-23)  
**Evidencia:** `ICodigoAmigoGenerator`/`CodigoAmigoGenerator` (criptográfico, 8 caracteres), `ICodigoAmigoService` (unicidad), `IRegistroService` (asigna código antes de `CreateAsync` con reintentos). `AmistadService` reutiliza el mismo generador y valida `UpdateAsync`. Pruebas: `RegistroTests` (dos registros consecutivos y correo duplicado).
**Prioridad:** P0  
**Esfuerzo:** M  
**Depende de:** P0.0

**Problema:** el segundo usuario nuevo falla con `UNIQUE constraint failed: AspNetUsers.CodigoAmigo` porque todos se crean con `CodigoAmigo = ""` y existe un índice único.

**Evidencia:**

- `Data/ApplicationUser.cs:7`
- `Data/ApplicationDbContext.cs:37-39`
- `Data/Migrations/20260923221056_AgregarIndiceCodigoAmigo.cs:13-17`
- `Components/Account/Pages/Register.razor:71-90`
- `Services/AmistadService.cs:21-44`

**Estrategia recomendada:** generar el código durante el registro, antes de `UserManager.CreateAsync`, y mantener el índice único como protección contra duplicados.

**Pasos:**

1. Crear un generador de códigos reutilizable, por ejemplo `ICodigoAmigoGenerator` / `CodigoAmigoGenerator`.
2. Usar `RandomNumberGenerator` en lugar de `new Random()`.
3. Generar códigos alfanuméricos de 6 a 8 caracteres, no solo 4 dígitos.
4. Implementar generación con reintentos y comprobación de colisiones contra la base.
5. Asignar `user.CodigoAmigo` en `Register.razor` antes de `CreateAsync`.
6. Reemplazar la generación de `AmistadService.ObtenerOCrearCodigoAsync` por el mismo generador.
7. Validar el resultado de `UpdateAsync`; no ignorar errores.
8. Manejar `DbUpdateException` por colisión con un reintento acotado.
9. Evaluar una migración posterior para hacer el código nullable y usar un índice único filtrado, si se quieren admitir usuarios legados sin código.
10. Añadir prueba de dos registros consecutivos y de colisión forzada.

**Criterio de aceptación:**

- Dos registros consecutivos en una base limpia terminan sin HTTP 500.
- Todos los usuarios nuevos tienen `CodigoAmigo` no vacío y único.
- El primer usuario no necesita visitar `/amigos` para que el segundo pueda registrarse.
- Los usuarios existentes conservan sus códigos.
- Existe una prueba automatizada que cubre el caso.

**Verificación manual:**

```text
1. Base limpia.
2. Registrar usuario A.
3. Registrar usuario B en otra sesión/incógnito.
4. Ambos deben entrar correctamente.
5. Consultar AspNetUsers: CodigoAmigo no vacío y distinto.
```

---

### P0.2 - Reparar confirmación de correo y recuperación de contraseña

**Estado:** [x] Completado (2026-10-23) — Opción B elegida por producto.  
**Evidencia:** `IRecuperacionContrasenaService` elimina la comprobación `IsEmailConfirmedAsync`; el correo solo se envía al solicitar recuperación. Páginas `ForgotPassword`, `ResetPassword` y confirmaciones traducidas. Pruebas: `RecuperacionContrasenaTests` (usuario sin confirmar recibe enlace, el enlace cambia la contraseña y un correo inexistente no envía nada).
**Prioridad:** P0  
**Esfuerzo:** L  
**Depende de:** P0.0

**Problema:** la recuperación exige `EmailConfirmed`, pero el registro nunca confirma el correo. Todas las cuentas revisadas tienen `EmailConfirmed = 0`.

**Evidencia:**

- `Program.cs:41-47`
- `Components/Account/Pages/Register.razor:71-90`
- `Components/Account/Pages/ForgotPassword.razor:45-66`
- `Services/EmailSender.cs`
- `Components/Account/Pages/Manage/Email.razor`

**Decisión de producto requerida:** elegir uno de estos dos caminos.

**Opción A - Confirmación obligatoria (recomendada):**

- `RequireConfirmedAccount = true`.
- Al registrar, generar token de confirmación y enviar correo.
- Mostrar pantalla de "revisa tu correo".
- Bloquear login hasta confirmar.
- Permitir reenvío de confirmación.
- La recuperación solo funciona para correos confirmados.

**Opción B - Confirmación opcional:**

- Mantener `RequireConfirmedAccount = false`.
- Quitar la comprobación `IsEmailConfirmedAsync` de `ForgotPassword`.
- Documentar que la recuperación funciona sin confirmar correo.
- Mantener el envío de confirmación como función secundaria.

**Pasos para la opción elegida:**

1. Implementar el envío de confirmación en `Register.razor`.
2. Generar el enlace con `GenerateEmailConfirmationTokenAsync` y `ConfirmEmail`.
3. Añadir validación de configuración SMTP al arrancar.
4. Traducir `ForgotPassword`, `ResetPassword`, confirmaciones y páginas relacionadas.
5. Evitar enumeración de usuarios: misma respuesta exista o no la cuenta.
6. Añadir pruebas con un `IEmailSender` falso:
   - Registro envía confirmación.
   - Confirmación marca `EmailConfirmed`.
   - Recuperación envía enlace.
   - Reset cambia la contraseña.
   - Usuario no confirmado se comporta según la opción elegida.
7. Probar con un buzón real o un SMTP de pruebas antes de cerrar.

**Criterio de aceptación:**

- Un usuario nuevo puede confirmar su correo y luego recuperar su contraseña.
- O, si se elige la opción B, puede recuperarla sin confirmar y el texto de la UI es coherente.
- No se revela si un correo existe.
- El flujo funciona con la configuración SMTP real de staging.

---

### P0.3 - Sacar secretos del código y proteger la API key de Gemini

**Estado:** [x] Completado (2026-10-23); rotación de credenciales pendiente por el propietario.  
**Evidencia:** `Gemini:ApiKey` viaja en la cabecera `x-goog-api-key`; errores de Gemini ya no exponen `ex.Message` ni el detalle HTTP; `CorreoOptions`/`GeminiOptions` con `ValidateOnStart`; credenciales locales movidas a User Secrets y `appsettings.Development.json` saneado. `git grep` no encuentra credenciales en archivos versionados.
**Prioridad:** P0  
**Esfuerzo:** M  
**Depende de:** nada (puede hacerse en paralelo a P0.1/P0.2, pero antes de desplegar)

**Problema:** hay credenciales reales en configuración local y la key de Gemini viaja en la URL, además de poder aparecer en mensajes de error.

**Evidencia:**

- `appsettings.Development.json:11-20` (no reproducir valores)
- `Services/GlosarioService.cs:24-30`
- `Services/GlosarioService.cs:57`
- `Services/GlosarioService.cs:92-95`
- `appsettings.json`
- `.gitignore:489`

**Pasos:**

1. Rotar las credenciales de Gemini y SMTP si el archivo se compartió, se subió o estuvo accesible fuera del equipo.
2. Mover las credenciales a User Secrets en desarrollo:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "<nueva-key>"
dotnet user-secrets set "Correo:Host" "<host>"
dotnet user-secrets set "Correo:Puerto" "587"
dotnet user-secrets set "Correo:Usuario" "<usuario>"
dotnet user-secrets set "Correo:Clave" "<nueva-clave>"
dotnet user-secrets set "Correo:Remitente" "<remitente>"
```

3. Dejar `appsettings.Development.json` sin secretos o eliminarlo del disco si no se necesita.
4. Confirmar que `.gitignore` excluye `appsettings.Development.json` y cualquier `.env`.
5. Revisar el historial:

```powershell
git log --all -p -- appsettings.Development.json
git grep -n -I -E "ApiKey|Correo:Clave|Secret|PrivateKey" $(git rev-list --all)
```

Si alguna credencial aparece en el historial, rotarla y evaluar limpieza del historial.
6. Cambiar Gemini para enviar la key en cabecera:

```text
x-goog-api-key: <api-key>
```

No incluir la key en la URL.
7. Devolver mensajes genéricos al usuario; registrar el detalle técnico solo en logs internos sin secretos.
8. Añadir validación de configuración al inicio con `IOptions<T>` y `ValidateOnStart`.
9. Añadir una prueba que verifique que un error de Gemini no expone la key.

**Criterio de aceptación:**

- No hay secretos en archivos versionados.
- `git grep` no encuentra credenciales.
- La app arranca sin secretos locales solo si las variables de entorno están configuradas, y falla con un mensaje claro si faltan.
- La key no aparece en URLs, logs de aplicación ni mensajes de error.
- Las credenciales antiguas fueron rotadas.

---

## 3. P1 - Seguridad y riesgos altos

Objetivo: proteger datos de usuarios, evitar abusos y preparar un despliegue seguro.

### P1.1 - Unificar eliminación y exportación de datos

**Estado:** [x] Completado (2026-10-23). `IEliminacionCuentaService` transaccional, `IExportacionDatosService` completo, ruta antigua redirigida y pruebas en `EliminacionCuentaTests`/`ExportacionDatosTests`.

**Prioridad:** P1  
**Esfuerzo:** L  
**Depende de:** P0 completado

**Problema:** la eliminación de cuenta no borra el glosario; la ruta antigua `DeletePersonalData` sigue accesible y puede fallar con amistades; la exportación no incluye libros, travesías, glosario ni amistades.

**Evidencia:**

- `Components/Account/Pages/Manage/EliminarCuenta.razor:73-83`
- `Components/Account/Pages/Manage/DeletePersonalData.razor:78-89`
- `Components/Account/Pages/Manage/PersonalData.razor:20-25`
- `Data/ApplicationDbContext.cs:25-45`
- `Models/PalabraGlosario.cs`
- `Components/Pages/Privacidad.razor:22-23`

**Pasos:**

1. Crear un único caso de uso `EliminarCuentaAsync(usuarioId, password)`.
2. Ejecutar el borrado en una transacción.
3. Borrar o cascada explícita para:
   - `Libros`
   - `EntradasLectura` (cascada por libro)
   - `Amistades`
   - `PalabrasGlosario`
   - datos de Identity
4. Eliminar la ruta `DeletePersonalData` o redirigirla al flujo nuevo.
5. Ampliar la exportación de datos para incluir libros, reseñas, puntuaciones, travesías, glosario y amistades.
6. Actualizar la política de privacidad para que coincida exactamente con lo que se borra y exporta.
7. Añadir prueba con un usuario que tenga libros, travesías, glosario y amistades.

**Criterio de aceptación:**

- Después de eliminar una cuenta no quedan filas huérfanas en ninguna tabla.
- El borrado funciona aunque el usuario tenga amistades.
- La exportación contiene todos los datos del usuario.
- La política de privacidad describe el comportamiento real.

---

### P1.2 - Autorización por propietario en servicios

**Estado:** [x] Completado (2026-10-23). Servicios reciben `usuarioId`, filtran por propietario y devuelven `bool`/resultado explícito; pruebas en `AutorizacionPropietarioTests`.

**Prioridad:** P1  
**Esfuerzo:** M  
**Depende de:** P1.1

**Problema:** los servicios de libros y travesías no verifican que el recurso pertenezca al usuario actual. La UI lo comprueba, pero la regla no está en la capa de servicio.

**Evidencia:**

- `Services/LibroService.cs:16-50`
- `Services/EntradaLecturaService.cs:16-52`
- `Services/GlosarioService.cs:32-109`
- `Components/Pages/Estantes.razor:413-432` y `:525-534`

**Pasos:**

1. Cambiar las firmas para recibir siempre `usuarioId`.
2. Filtrar por propietario en consultas, actualizaciones y borrados.
3. Añadir comprobación de propiedad para travesías mediante join con `Libros`.
4. Devolver resultados explícitos (`bool`, `Result`) en lugar de asumir éxito.
5. Evitar aceptar entidades completas desde la UI; usar DTOs de entrada.
6. Añadir pruebas con dos usuarios:
   - Usuario A no puede leer, editar ni borrar datos de B.
   - Usuario B no puede añadir travesías a un libro de A.
7. Revisar endpoints de Identity que reciben IDs.

**Criterio de aceptación:**

- Todas las operaciones de datos de usuario verifican propiedad en la capa de servicio.
- Las pruebas de autorización pasan.
- La UI ya no es la única defensa.

---

### P1.3 - Evitar publicar la base de datos de desarrollo

**Estado:** [x] Completado (2026-10-23). Copia eliminada de `Otium.csproj`; `dotnet publish` verificado sin `Data/app.db`.

**Prioridad:** P1  
**Esfuerzo:** S  
**Depende de:** nada; hacerlo antes de cualquier publicación

**Problema:** `Data/app.db` se copia a la salida de publicación y contiene datos reales de desarrollo.

**Evidencia:**

- `Otium.csproj:11-13`
- `Data/app.db`
- `C:\Otium\OtiumServidor\Data\app.db`

**Pasos:**

1. Quitar del `.csproj` la copia de `Data/app.db`.
2. Mantener `Data/app.db` solo como base local ignorada.
3. Crear la base de producción en `C:\OtiumDatos\app.db` o ruta equivalente.
4. Añadir una comprobación posterior a `dotnet publish`:

```powershell
Test-Path .\publish\Data\app.db
# Debe ser False
```

5. Añadir la comprobación a CI cuando exista.
6. Revisar que el Dockerfile siga excluyendo `Data/app.db*`.

**Criterio de aceptación:**

- `dotnet publish` no incluye `Data/app.db`.
- El despliegue usa una ruta de datos externa.
- La base de desarrollo no se distribuye.

---

### P1.4 - HTTPS/TLS real en producción

**Estado:** [x] Implementado en código (2026-10-23); pendiente verificar login, SignalR, PWA y recuperación en el VPS Oracle con Caddy/Nginx. Soporte dual documentado en `AGENTS.md` sección 6.6.

**Prioridad:** P1  
**Esfuerzo:** M  
**Depende de:** P1.3

**Problema:** la publicación y Docker escuchan HTTP; HSTS y redirección no protegen sin TLS.

**Evidencia:**

- `Program.cs:85-106`
- `Properties/launchSettings.json`
- `Dockerfile:12-18`
- Script de arranque publicado

**Pasos:**

1. Elegir terminación TLS: Caddy, Nginx, IIS, Cloudflare Tunnel u otro proxy.
2. Exponer Kestrel solo en red interna o localhost.
3. Configurar `ForwardedHeaders` si hay proxy.
4. Forzar HTTPS y `Secure` en cookies de autenticación.
5. Verificar HSTS solo en producción.
6. Documentar el despliegue y los puertos.
7. Probar login, SignalR, PWA y recuperación por HTTPS.

**Criterio de aceptación:**

- `http://` redirige a `https://` en producción.
- Las cookies de sesión llevan `Secure`.
- Blazor SignalR funciona por `wss://`.
- No hay advertencias de redirección HTTPS sin puerto.

---

### P1.5 - Rate limiting, códigos de amigo y antiabuso

**Estado:** [x] Completado (2026-10-23). Rate limiting HTTP, throttling por usuario, cuota Gemini, códigos de 8 caracteres, mensajes uniformes e índice único `ClavePar`; pruebas en `RateLimitingTests` y `AmistadAntiabusoTests`.

**Prioridad:** P1  
**Esfuerzo:** L  
**Depende de:** P0.1 y P1.2

**Problema:** no hay límites en login, registro, solicitudes de amistad ni Gemini. Los códigos de amigo son 4 dígitos generados con `Random`.

**Evidencia:**

- `Program.cs` (sin `AddRateLimiter`)
- `Services/AmistadService.cs:30-39`, `:46-76`
- `Services/GlosarioService.cs:41-96`
- `Components/Pages/Amigos.razor:30`

**Pasos:**

1. Añadir rate limiting HTTP para login, registro, recuperación y endpoints de Identity.
2. Añadir throttling a nivel de servicio para acciones Blazor Server, porque las llamadas interactivas viajan por SignalR y no pasan por el middleware HTTP.
3. Políticas sugeridas:
   - Login: por IP + cuenta.
   - Registro: por IP.
   - Solicitudes de amistad: por usuario + IP.
   - Glosario/Gemini: por usuario, con cuota diaria.
4. Reemplazar códigos de 4 dígitos por códigos alfanuméricos de 6-8 caracteres con `RandomNumberGenerator`.
5. Unificar los mensajes de solicitud de amistad para no revelar si el código existe.
6. Añadir índice único para pares de amistad y manejar carreras.
7. Añadir métricas/logs de límites alcanzados.

**Criterio de aceptación:**

- Un ataque de fuerza bruta queda limitado.
- No se puede abusar de Gemini sin cuota.
- Los códigos de amigo son suficientemente aleatorios.
- No se pueden crear amistades duplicadas.

---

### P1.6 - Alinear la función social con la política de privacidad

**Estado:** [x] Completado (2026-10-23, Opción B). `CompartirEstantes` desactivado por defecto, vista `/amigos/{id}` autorizada, eliminar amigo, bloquear/desbloquear y textos de privacidad/registro alineados. Travesías y glosario no se comparten. Pruebas en `CompartirEstantesTests`.

**Prioridad:** P1  
**Esfuerzo:** XL (decisión de producto)  
**Depende de:** P1.2 y P1.5

**Problema:** la app promete que los amigos pueden ver lo que el usuario comparte, pero solo muestra una lista de correos.

**Evidencia:**

- `Components/Pages/Amigos.razor`
- `Components/Pages/Privacidad.razor:14-17`
- `Components/Account/Pages/Register.razor:18`

**Decisión requerida:** elegir una de estas opciones.

**Opción A - Ajustar el texto a la funcionalidad actual:**

- Cambiar privacidad y registro para decir que los amigos solo aparecen en una lista y no ven libros.
- Esfuerzo: S.
- Recomendada si el lanzamiento es inminente.

**Opción B - Implementar compartir de verdad:**

- Añadir preferencia `CompartirEstantes` por usuario.
- Añadir vista de estantes de un amigo.
- Definir qué se comparte: estantes, reseñas, puntuaciones, travesías o actividad.
- Respetar la preferencia en cada consulta y probar autorización.
- Añadir eliminar amigo y bloquear.
- Esfuerzo: XL.

**Criterio de aceptación:**

- Lo que dice la política de privacidad coincide con lo que hace la app.
- Si se implementa compartir, existen pruebas de que un amigo no ve datos no compartidos.

---

## 4. P2 - Refactorización

Objetivo: mejorar mantenibilidad, testabilidad y claridad sin cambiar el comportamiento visible.

No iniciar P2 antes de cerrar P0/P1 o de aceptar formalmente el riesgo.

### P2.1 - Infraestructura de pruebas y CI

**Esfuerzo:** L

- Proyecto `Otium.Tests` con pruebas unitarias e integración.
- Pruebas de flujos críticos: registro, login, recuperación, eliminación, autorización, códigos de amigo, glosario y travesías.
- GitHub Actions con:
  - `dotnet restore`
  - `dotnet build -c Release`
  - `dotnet test`
  - `dotnet list package --vulnerable`
- Cobertura mínima acordada para servicios críticos.

### P2.2 - Options pattern y validación de configuración

**Esfuerzo:** M

- Crear `CorreoOptions`, `GeminiOptions` y `DataProtectionOptions`.
- Usar `IOptions<T>` en lugar de `IConfiguration` dispersa.
- Validar con `ValidateDataAnnotations` y `ValidateOnStart`.
- Evitar `int.Parse` y valores nulos en `EmailSender`.

### P2.3 - Abstraer clientes externos

**Esfuerzo:** M

- Crear `IBuscadorLibros` e inyectarlo en la UI.
- Crear `IGeminiClient` y separar:
  - Construcción del prompt.
  - Llamada HTTP.
  - Parseo de respuesta.
  - Persistencia y mensajes de error.
- Añadir cancelación, timeout, reintentos acotados y caché.
- Usar un `User-Agent` real para Open Library.

### P2.4 - Extraer generación de códigos

**Esfuerzo:** S

- `ICodigoAmigoGenerator` con implementación criptográfica.
- Eliminar la duplicación entre registro y `AmistadService`.
- Probar colisiones y reintentos.

### P2.5 - DTOs, validación y modelos de vista

**Esfuerzo:** L

- Dejar de enlazar entidades EF directamente a formularios.
- Crear DTOs de entrada/salida por caso de uso.
- Añadir DataAnnotations y validación de longitud/rango:
  - `Titulo`, `Autor`, `Resena`.
  - `Puntuacion` 0-5.
  - `PaginaInicio`, `PaginaFin`, orden y no negativos.
  - `Palabra` y `LibroTitulo`.
- Validar también en servicios, no solo en la UI.

### P2.6 - Separar responsabilidades en la UI

**Esfuerzo:** L

- Dividir `Estantes.razor` en componentes:
  - `BuscadorLibros`
  - `FormularioLibro`
  - `RepisaLibros`
  - `FichaDetalleLibro`
  - `ListaTravesias`
  - `EditorResena`
- Mover lógica de negocio a servicios o view models.
- Reducir el tamaño de los componentes y facilitar pruebas.

### P2.7 - Unificar flujos de cuenta y eliminar redundancias

**Esfuerzo:** M

- Eliminar `IdentityNoOpEmailSender` si no se usa.
- Eliminar o redirigir `DeletePersonalData`, `PersonalData` y rutas heredadas no enlazadas.
- Eliminar `Invitacion:Codigo` si ya no se usa.
- Unificar `AddDbContext` y `AddDbContextFactory` en una configuración clara.
- Revisar y deduplicar CSS en `Estantes.razor.css`.
- Traducir o eliminar pantallas de Identity en inglés que no se usan.

### P2.8 - Mejorar manejo de errores y logging

**Esfuerzo:** M

- No mostrar `ex.Message` al usuario.
- Crear excepciones/resultados de dominio.
- Añadir logging estructurado con IDs de correlación.
- Añadir telemetría de errores (Sentry, Application Insights u otro).
- Añadir health checks (`/health`).

### P2.9 - Respaldos y operación

**Esfuerzo:** M

- Backup diario de SQLite con retención.
- Verificación de integridad y prueba de restauración.
- Documentar RPO/RTO.
- Añadir health check de base de datos.
- Evaluar migraciones controladas en lugar de `Migrate()` al arrancar.

### P2.10 - Endurecer Docker

**Esfuerzo:** M

- Usuario no root.
- `VOLUME /data`.
- `HEALTHCHECK`.
- Imágenes fijadas por digest.
- Variables de entorno documentadas.
- Límites de recursos y logs persistentes.
- Pipeline de build y escaneo de imagen.

### P2.11 - Accesibilidad e internacionalización

**Esfuerzo:** M

- Auditar contraste, foco, navegación por teclado y lectores de pantalla.
- Revisar `alt`, `aria-*` y botones sin texto.
- Unificar todo el texto en español o introducir recursos i18n.
- Traducir `NotFound`, `Error` y pantallas de Identity.
- Añadir pruebas básicas de accesibilidad.

### P2.12 - Rendimiento y resiliencia

**Esfuerzo:** M

- Caché de búsquedas y portadas de Open Library.
- Paginación o virtualización si crece el número de libros.
- Reintentos y circuit breaker para APIs externas.
- Evitar consultas repetidas en render.
- Medir tiempos de carga y tamaño de la base.

---

## 5. Secuencia recomendada de PRs

| Orden | PR | Prioridad | Depende de |
|---|---|---|---|
| 1 | `test/p0-red-seguridad` | P0.0 | - |
| 2 | `fix/p0-registro-codigo-amigo` | P0.1 | 1 |
| 3 | `fix/p0-recuperacion-contrasena` | P0.2 | 1 |
| 4 | `chore/p0-secretos-gemini` | P0.3 | - |
| 5 | `fix/p1-eliminacion-datos` | P1.1 | 2, 3 |
| 6 | `fix/p1-autorizacion-propietario` | P1.2 | 5 |
| 7 | `chore/p1-base-datos-publicacion` | P1.3 | - |
| 8 | `chore/p1-https-despliegue` | P1.4 | 7 |
| 9 | `fix/p1-rate-limiting-codigos` | P1.5 | 2, 6 |
| 10 | `fix/p1-social-privacidad` | P1.6 | 6, 9 |
| 11 | `refactor/p2-solid` | P2 | 1-10 |

Los PRs 2, 3 y 4 pueden ejecutarse en paralelo si no tocan los mismos archivos. Los PRs 5 y 6 deben ir después de P0 para evitar conflictos en identidad y datos.

---

## 6. Criterios de salida por fase

### Salida de P0

- Dos registros consecutivos funcionan.
- Recuperación de contraseña funciona de punta a punta.
- No hay secretos en el repositorio ni en URLs/mensajes.
- `dotnet build -c Release` limpio.
- `dotnet test` verde.
- Staging validado manualmente.

### Salida de P1

- Eliminación y exportación de datos completas.
- Autorización por propietario probada con dos usuarios.
- Rate limiting activo en flujos sensibles.
- Códigos de amigo robustos.
- Producción solo por HTTPS.
- `dotnet publish` no incluye la base de desarrollo.
- Privacidad y funcionalidad social coherentes.

### Salida de P2

- Pruebas y CI obligatorios.
- Configuración validada al arrancar.
- Servicios desacoplados de detalles de infraestructura.
- UI dividida en componentes manejables.
- Errores y logs tratados de forma segura.
- Respaldos, health checks y Docker endurecidos.
- Documentación actualizada.

---

## 7. Decisiones pendientes

Antes de ejecutar P0.2 y P1.6 hay que decidir:

1. ¿La cuenta requiere confirmación de correo obligatoria o no?
   - **Resuelto (2026-10-23):** Opción B, confirmación opcional. El correo solo se envía cuando el usuario olvida su contraseña.
2. ¿Otium será público o solo por invitación?
3. ¿Los amigos podrán ver estantes, reseñas o actividad, o solo existirá una lista de contactos?
4. ¿Se aceptará registro abierto o se recuperará el código de invitación?
5. ¿El despliegue será local, VPS, Docker o un servicio gestionado?
6. ¿Se usará un proveedor de email transaccional distinto de SMTP directo?

Estas decisiones cambian el alcance de P0.2 y P1.6. No deben resolverse implícitamente durante la implementación.

---

## 8. Seguimiento

Actualizar este archivo al cerrar cada tarea:

- Cambiar el estado de la tarea a completado.
- Añadir el PR/commit de referencia.
- Añadir la evidencia de verificación.
- Actualizar `AGENTS.md` si cambió la arquitectura o el estado de un hallazgo.

Estados sugeridos:

```text
[ ] Pendiente
[~] En curso
[x] Completado
[!] Bloqueado
[-] Descartado / aceptado como riesgo
```

---

## 9. Resumen ejecutivo del plan

El orden no es negociable: primero se corrige el registro, luego la recuperación de contraseña y la gestión de secretos. Sin esos tres puntos, cualquier lanzamiento produce errores de alta visibilidad o expone credenciales.

Después se protegen los datos y el despliegue: eliminación/exportación, autorización por propietario, rate limiting, HTTPS y separación de la base de desarrollo. Finalmente se refactoriza para que el proyecto sea mantenible y testeable.

La meta de P0 es **poder registrar usuarios y recuperar cuentas sin errores**. La meta de P1 es **poder exponer la app sin filtrar datos ni permitir abusos**. La meta de P2 es **poder mantenerla y evolucionarla con seguridad**.
