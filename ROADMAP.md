# ROADMAP.md — Otium (nombre comercial en evaluación): lector, catálogo y monetización

Plan de ejecución para convertir la beta privada en un producto de lectura con cobro y
distribución en Play Store.

- **Proyecto:** `C:\Proyectos\Otium` (carpeta y ensamblados sin renombrar; ver R-93)
- **Nombre comercial:** **en evaluación**. La candidata principal es **Elicona**, pero la decisión
  no está cerrada: hay cinco rondas de criba y una lista corta en `DOCS/marca.md`. Hasta que se
  cierre, **no se compran dominios, no se reservan handles y no se renombra el código**.
- **Base:** `a39b5e8` (`main`), árbol limpio
- **Fecha de decisión del plan:** 2026-11-15
- **Histórico:** los hallazgos F-xx de la auditoría viven en `DOCS/hallazgos.md`; siguen válidos
  como deuda técnica y se referencian donde toca, pero no se duplican aquí.
- **Índice documental:** `DOCS/README.md`.
- **Estado:** planificación cerrada. No se ha escrito código de ninguna fase.

---

## 0. Decisiones tomadas

| # | Decisión | Valor | Motivo |
|---|---|---|---|
| D1 | Contenido | **Biblioteca propia (BYOF, sin DRM) + catálogo de dominio público** | Valor inmediato sin licencias; el archivo privado del usuario no genera riesgo de distribución |
| D2 | Motor de lectura | **Parseo en servidor** (`VersOne.Epub`) y capítulos HTML sanitizados en componentes Blazor | Integra glosario IA, búsqueda, TTS y travesías sin puente JS frágil; control total de tipografía |
| D3 | Cliente v1 | **Web + PWA + TWA (Bubblewrap) en Play Store** | Presencia de app sin reescribir Blazor Server; cobro por web para evitar Play Billing |
| D4 | Infraestructura | **Hetzner CX22 (2 vCPU / 4 GB) + Docker Compose + Caddy + PostgreSQL + restic → Backblaze B2** | ~5–7 €/mes, tres contenedores, portable a cualquier proveedor |
| D5 | Catálogo público | **Gutendex como fuente de catálogo (ingesta masiva) y Standard Ebooks como fuente principal de archivos** | Búsqueda local sin depender de la API; licencia CC0 compatible con producto de pago |
| D6 | Alcance de escala | **≤ 100 usuarios registrados, 10–25 circuitos concurrentes** en v1 | Dimensiona toda la capacidad; sin multi-instancia ni backplane |
| D7 | Lectura sin conexión | **Fuera de alcance de la v1** | Incompatible con Blazor Server; se reevalúa con cliente propio en v3 |
| D8 | DRM | **No se soporta**; se rechaza el archivo con mensaje explícito | Soporte DRM exige licencia y SDK de terceros; no aporta al modelo BYOF |
| D9 | Nombre comercial | **En evaluación**; candidata principal **Elicona**, con lista corta y evidencia en `DOCS/marca.md` | `Otium` colisiona en el mismo nicho (`otium-library.com`, `otiumbookshelf.com`, `otiumfeed.it`). La decisión se cierra al final de la Fase −1, con clearance favorable; antes no se invierte en marca |
| D10 | Alcance funcional | Se incorporan las prácticas de **hábito y retención** (Fase 1.5) y la **diferenciación social** (Fase 5: mapas de personajes y lectura conjunta) | Sin retención no hay suscripción sostenible; sin diferenciación no hay motivo para pagar frente a un tracker gratuito |

### Decisiones de diseño derivadas (a aplicar desde la Fase 1)

- **API-first:** el lector expone endpoints minimal API reutilizables por un cliente futuro
  (MAUI/Kotlin). El componente Blazor consume esos contratos, no lógica encerrada.
- **Sin CSS del EPUB en v1:** se descarta el CSS embebido del archivo y se aplica la hoja de
  estilos propia. Elimina superficie de ataque y garantiza tipografía consistente.
- **Progreso agnóstico de formato:** `EntradaLectura` gana `ProgresoInicio`/`ProgresoFin`
  decimales; las páginas siguen existiendo para PDF.
- **Archivos servidos fuera del circuito:** endpoints HTTP con autorización y `Range`.
  Nunca binarios por SignalR.

---

## 1. Principios y restricciones

1. Un PR por tarea; no mezclar fases en el mismo PR.
2. Cada tarea incluye prueba automatizada o procedimiento reproducible de verificación.
3. Antes de cerrar: `dotnet build Otium.csproj -c Release --nologo` sin advertencias nuevas
   y `dotnet test Otium.slnx -c Release --nologo` en verde. Tras R-93, los mismos comandos con
   el nombre definitivo del proyecto.
4. Nada de secretos, bases de datos ni binarios en el repositorio.
5. Se mantienen las convenciones existentes: servicios con interfaz, DTOs en la frontera,
   `IDbContextFactory`, autorización por propietario en la capa de servicio, UI en español.
6. No se rompe ninguna migración aplicada; el esquema nuevo entra por migración.
7. Esfuerzo: **S** < 2 h · **M** 2–6 h · **L** 1–2 días · **XL** +2 días.

### Fuera de alcance de este roadmap

DRM y EPUB de maquetación fija avanzada · lectura offline · app iOS nativa · audiolibros ·
escalado horizontal y backplane Redis · editor de libros · compartir archivos entre amigos
(solo se comparten metadatos, reseñas y estantes, como hoy) · reconocimiento de lomos por foto.

Los **mapas de personajes** y la **lectura conjunta** sí están en el plan, pero en la Fase 5:
no son requisito de la v1 cobrable ni del lanzamiento en Play Store.

---

## 2. Arquitectura objetivo

```text
TWA (Play Store)  ─┐
Navegador / PWA   ─┼─► ASP.NET Core (Blazor Server + minimal APIs)
                   │       │
                   │       ├─► ApplicationDbContext ─► PostgreSQL 16 (contenedor)
                   │       ├─► IAlmacenLibros ──────► volumen /data/libros
                   │       ├─► IImportadorEpub ─────► VersOne.Epub + HtmlSanitizer
                   │       └─► IGlosarioService ────► Z.ai GLM
                   │
Caddy (TLS, HTTP/2, Range) ─► Kestrel 127.0.0.1:5000
restic (cron) ─► Backblaze B2 (BD + libros + claves)
```

### 2.1 Modelo de datos nuevo

| Tabla | Campos clave | Notas |
|---|---|---|
| `ArchivosLibro` | `Id`, `LibroId`, `Formato`, `TamanioBytes`, `Sha256`, `RutaAlmacen`, `Estado`, `MensajeError`, `FechaSubida` | Único `(LibroId)`; índice `(UsuarioId, Sha256)` vía join o columna desnormalizada para deduplicar |
| `CapitulosLibro` | `Id`, `LibroId`, `Indice`, `Titulo`, `Ancla`, `Html`, `TextoPlano`, `Chars` | Índice único `(LibroId, Indice)`; `TextoPlano` para búsqueda y TTS |
| `ProgresoLectura` | `Id`, `UsuarioId`, `LibroId`, `CapituloId`, `Offset`, `Porcentaje`, `UltimaLecturaUtc`, `MinutosAcumulados` | Único `(UsuarioId, LibroId)`; sincroniza entre dispositivos gratis |
| `PreferenciasLector` | `UsuarioId`, `TamanoFuente`, `Interlineado`, `Margen`, `Tema`, `Justificado` | Único por usuario |
| `ObrasCatalogo` | `Id`, `Fuente`, `IdExterno`, `Titulo`, `Autores`, `Idiomas`, `Materias`, `PortadaUrl`, `Licencia`, `Descargas`, `Sha256Epub` | Catálogo público; índice GIN de texto completo |
| `Planes`, `Suscripciones`, `UsoMensual` | ver R-40 | Fase 3 |
| `EventosWebhook` | `Id`, `Proveedor`, `IdEventoExterno`, `Tipo`, `RecibidoUtc`, `ProcesadoUtc` | Idempotencia de pagos |

### 2.2 Contratos nuevos

```csharp
IAlmacenLibros   // GuardarAsync, AbrirAsync(range), EliminarAsync, ExisteAsync
IImportadorEpub  // ImportarAsync(stream, libroId, ct) -> ResultadoImportacion
ILectorService   // ObtenerIndiceAsync, ObtenerCapituloAsync, RegistrarProgresoAsync
ICatalogoService // BuscarAsync, ObtenerObraAsync, MaterializarEnEstanteAsync
IPlanService     // ObtenerPlanAsync, PuedeAgregarLibroAsync, ConsumirCuotaIAAsync
```

### 2.3 Endpoints HTTP nuevos

| Método | Ruta | Autorización | Uso |
|---|---|---|---|
| `GET` | `/libros/{id}/recurso/{**ruta}` | Cookie + propietario | Imágenes y recursos internos del EPUB, con `Range` y `Cache-Control: private` |
| `GET` | `/libros/{id}/epub` | Cookie + propietario | Descarga del archivo original |
| `GET` | `/catalogo/portada/{id}` | Anónimo | Portada cacheada del catálogo público |
| `POST` | `/webhooks/pagos` | Firma del proveedor | Eventos de suscripción |
| `GET` | `/.well-known/assetlinks.json` | Anónimo | Verificación de TWA |

---

## 2.5 Fase −1 — Identidad de marca

Objetivo: dejar de llamarse `Otium` antes de tocar la base de datos, el manifiesto de la PWA y
las migraciones. El renombrado es mecánico pero transversal; hacerlo después de la Fase 0
obligaría a repetirlo sobre `Otium.Data` y sobre la migración inicial de PostgreSQL.

**El nombre no está decidido.** Hay cinco rondas de criba y una lista corta en `DOCS/marca.md`
(§3.1, §3.2, §3.4). La candidata principal es **Elicona**, pero la elección se cierra al final de esta
fase, con la clearance de R-90 delante. Mientras no se cierre: nada de dominios, handles ni
renombrado de código.

**Evidencia acumulada (2026-11-15).** `Otium` está ocupado en el mismo nicho:
`otium-library.com` (*book tracker and reading journal*, suscripción de pago),
`otiumbookshelf.com` y `otiumfeed.it`; además hay una solicitud USPTO `OTIUM` (serial
50156419, clase 041). En la lista corta, **Elicona** no tiene app en App Store ES/US ni en Play
Store; `elicona.com` pertenece a una sala de arte y música japonesa (clase ajena); `elicona.es`,
`elicona.app`, `elicona.com.es`, `getelicona.com`, `eliconareader.com`, `elicona.io`,
`elicona.page`, `elicona.club` y `elicona.studio` no tienen delegación DNS, por lo que están
libres o en parking. Lo mismo se comprobó, con distinto resultado, para `Alciona`, `Filomela`,
`Aganipe`, `Calirroe`, `Luscinda`, `Melibea`, `Ismene`, `Kelaino`, `Krissa`, `Ianthe` y
`Laelia`. Es un indicio, **no** un dictamen legal: la clearance formal es R-90.

| ID | Tarea | Prioridad | Esfuerzo | Depende de |
|---|---|---|---|---|
| R-90 | Clearance formal y elección del nombre | P0 | S (trámite externo) | — |
| R-91 | Dominios, correo y DNS | P0 | S | R-90 |
| R-92 | Handles y reserva en tiendas | P1 | S | R-91 |
| R-93 | Renombrado en código, datos y configuración | P0 | L | R-90 |
| R-94 | Identidad visual y PWA | P1 | M | R-91 |
| R-95 | Solicitud de marca (OEPM) | P1 | M (trámite externo) | R-90 |

### R-90 — Clearance formal y elección del nombre

Búsqueda en **OEPM** (España), **EUIPO** (UE) y **USPTO/TESS**, con foco en las clases
**9** (software), **41** (educación, ocio, publicación) y **42** (servicios tecnológicos, SaaS),
para las **finalistas** de `DOCS/marca.md` (no solo para Elicona). Revisar también
denominaciones sociales en el registro mercantil y una búsqueda web de
`"<nombre>" + app/lectura/libros/ebook` para descartar uso no registrado en el nicho.

**Aceptación.** Informe de una página por finalista con las marcas encontradas, clases, titular,
estado y una recomendación explícita *adelante / adelante con reserva / descartar*. El resultado
cierra la elección del nombre y desbloquea R-91 y R-93. Si todas las finalistas se descartan, se
vuelve a la lista larga antes de gastar en dominio o logo. Alternativas ya descartadas:
`Breviario` (apps religiosas), `Incunable` y `Florilegio` (app iOS), `Scholia` (app de lectura),
`Lacuna` (varias apps), `Estrofa` (lector antiguo).

### R-91 — Dominios, correo y DNS

Comprar el dominio principal en `.es` y el `.app` para la TWA (obligatorio: Play exige HTTPS en
el dominio de la app), más registro defensivo del `.com` si está libre o es asequible. **Se
compra en cuanto R-90 cierre el nombre**, sin esperar al informe completo de marca: el dominio
cuesta ~12 €/año y perderlo por esperar una búsqueda es peor que el coste hundido.
Correo `hola@<dominio>` (o el buzón actual) con **SPF, DKIM y DMARC** alineados con Brevo: sin
DKIM los enlaces de recuperación acaban en spam y eso rompe un flujo P0 ya cerrado (P0-2).

**Aceptación.** Los tres dominios resuelven con TLS válido, el correo transaccional de prueba
pasa DMARC y la bandeja de entrada lo recibe (no spam).

### R-92 — Handles y reserva en tiendas

Reservar `@<nombre>` en Instagram, TikTok, X y YouTube y el nombre del repositorio. Registrar
la ficha en Play Console con el nombre definitivo (`applicationId` se decide en R-51, pero el
título visible no debe cambiar después). No hace falta reservar App Store en v1: no hay app iOS.

**Aceptación.** Handles activos o en lista de espera documentada; nombre visible de Play
Console fijado por escrito.

### R-93 — Renombrado en código, datos y configuración

Renombrar solución y ensamblados a `<Nombre>.*` (`<Nombre>.slnx`, `<Nombre>.csproj`,
`tests/<Nombre>.Tests`, `<Nombre>.Data` cuando exista) y el espacio de nombres raíz, de forma
mecánica y en un único PR. Renombrar los textos visibles (título, `manifest.json`, `sw.js`,
política de privacidad, términos, correos de Brevo, `Sitio:UrlPublica`), la cookie de
autenticación y las claves de configuración prefijadas si las hubiera. El nombre concreto lo
fija R-90.

**Cuidado.** Cambiar el nombre de la cookie invalida sesiones activas: aceptable ahora (beta),
no lo sería después. La clave de Data Protection no cambia, así que los enlaces de
recuperación emitidos antes siguen siendo válidos. La base de datos de desarrollo se renombra a
`<nombre>.db`; en producción el nombre lo fija `ConnectionStrings__DefaultConnection` y no se
toca.

**Aceptación.** `dotnet build -c Release` y `dotnet test` en verde; ningún `Otium` visible en
la UI, el manifiesto, los correos ni los metadatos de ensamblado; sesión nueva iniciada y
recuperación de contraseña probada con el nombre nuevo.

### R-94 — Identidad visual y PWA

Logotipo y símbolo derivados del nombre que cierre R-90 (evitar el libro genérico), paleta con
tema claro, sepia y oscuro, tipografía de lectura con licencia libre, iconos PWA 192/512 y
*maskable*, splash de la TWA y una guía mínima de tono de voz. Los iconos actuales se
regeneran; el manifest y el tema se ajustan en R-50.

**Aceptación.** Set de iconos validado en el *Lighthouse* de la PWA y contraste AA en los tres
temas (el sepia y el OLED entran en R-22).

### R-95 — Solicitud de marca (OEPM)

Solicitud de marca denominativa y figurativa en **OEPM**, clases 9 y 42 (valorar 41). Coste
aproximado 150 € la primera clase y ~50 € cada adicional. EUIPO (marca de la UE, ~850 €) solo
después de validar tracción, no antes. La solicitud se presenta con la marca ya en uso o con
intención de uso, nunca antes de la clearance.

**Aceptación.** Justificante de solicitud archivado y fecha de publicación en el BOPI anotada
para vigilar oposiciones.

---

## 3. Fase 0 — Cimientos para cobrar

Objetivo: base de datos evolucionable, respaldos reales y despliegue reproducible. Sin esto,
ni el lector ni el cobro son sostenibles.

| ID | Tarea | Prioridad | Esfuerzo | Depende de |
|---|---|---|---|---|
| R-01 | Migraciones PostgreSQL reales | P0 | L | — |
| R-02 | Respaldos y restauración verificada | P0 | M | R-01 |
| R-03 | Docker endurecido | P0 | M | — |
| R-04 | VPS y red | P0 | M | R-03 |
| R-05 | CI/CD con staging y rollback | P1 | M | R-04 |
| R-06 | Observabilidad mínima | P1 | S | R-04 |
| R-07 | CSP por entorno, `AllowedHosts`, 429 con cuerpo | P1 | M | R-04 |
| R-08 | Rotación de credenciales | P0 | S | — |

### R-01 — Migraciones PostgreSQL reales

**Problema.** `Data/BaseDatosInicializador.cs:55-73` usa `EnsureCreatedAsync` en PostgreSQL
porque las migraciones de `Data/Migrations` son SQLite. Con clientes de pago no se puede
evolucionar el esquema: cualquier columna nueva exige recrear la base.

**Solución.** Extraer la capa de datos a un proyecto `Otium.Data` conservando el espacio de
nombres `Otium.Data` (cero cambios en los `using`), y añadir
`Otium.Data.Migrations.PostgreSql` que referencie a `Otium.Data`. La web referencia ambos;
las migraciones SQLite permanecen en el ensamblado web y PostgreSQL usa
`MigrationsAssembly("Otium.Data.Migrations.PostgreSql")`.

**Pasos.**
1. Mover `ApplicationDbContext`, `ApplicationUser`, `BaseDatosInicializador` y
   `DescriptorErroresEs` a `Otium.Data` sin cambiar namespaces.
2. Crear `Otium.Data.Migrations.PostgreSql` con `IDesignTimeDbContextFactory` y cadena de
   diseño ficticia.
3. Generar migración inicial; revisar tipos (`text`, `timestamp with time zone`, `numeric`).
4. Configurar `MigrationsAssembly` solo para el proveedor PostgreSQL en
   `ServiceCollectionExtensions.AddBaseDeDatos`.
5. Sustituir `EnsureCreatedAsync` por `MigrateAsync` en ambos proveedores.
6. Baseline de la base existente: si hay datos en Render, aplicar la migración inicial a una
   base nueva e importar con `pg_dump --data-only`; alternativa documentada: insertar la fila
   en `__EFMigrationsHistory` tras verificar el esquema.
7. Resolver F-14 (doble registro de `DbContext`) al tocar este archivo.

**Aceptación.** `dotnet ef migrations add Prueba` en el proyecto PostgreSQL genera SQL
válido; `MigrateAsync` crea el esquema desde cero; `EnsureCreated` desaparece; los tests de
integración siguen verdes con SQLite y con PostgreSQL.

### R-02 — Respaldos y restauración verificada

**Problema.** `BackupService` asume SQLite (`PRAGMA wal_checkpoint` + `File.Copy`) y silencia
errores de retención (F-02). En el VPS hay tres cosas que respaldar: base de datos, archivos
de libros y llavero de Data Protection.

**Pasos.**
1. `pg_dump` en formato custom desde el contenedor de Postgres, cifrado con `age`.
2. `restic` para el volumen `/data` (libros + claves si no están en base de datos).
3. Destino Backblaze B2, retención 7 diarios / 4 semanales / 6 mensuales.
4. Cron en el host (no en el contenedor) con alerta a Sentry si falla.
5. Prueba de restauración mensual documentada: base limpia, `pg_restore`, arranque, login y
   apertura de un libro.
6. `BackupService` pasa a ser un *wrapper* que verifica antigüedad del último respaldo y lo
   expone en `/health`; se elimina el `File.Copy`.

**Aceptación.** Un respaldo completo restaura un entorno funcional en < 30 min; `/health`
refleja el estado del último respaldo; ningún `catch` silencioso.

### R-03 — Docker endurecido

**Pasos.**
1. `USER app` no root con propietario de `/data`.
2. Unificar en `/data` (elimina la incoherencia `/var/data` del Dockerfile y `/data` del
   compose; F-06).
3. Healthcheck funcional: instalar `curl` en la imagen runtime o usar una sonda propia; el
   compose no debe depender de un binario ausente.
4. Fijar imágenes base por digest.
5. `restart: unless-stopped`, límites de CPU/memoria, rotación de logs.
6. Alinear `ASPNETCORE_URLS` con el `PORT` que fija `Program.cs:22-23`.
7. Multi-stage con caché de restore; imagen final < 300 MB.

**Aceptación.** `docker compose up` levanta app + Postgres + Caddy; contenedor no root;
`/health` responde `Healthy`; reinicio del host recupera el servicio solo.

### R-04 — VPS y red

**Pasos.**
1. Hetzner CX22 (Falkenstein/Helsinki), Debian 13, SSH por clave, `ufw` (22/80/443).
2. Caddy como proxy TLS: `Kestrel` en `127.0.0.1:5000`, WebSocket y `Range` habilitados.
3. `Proxy__KnownProxies__0=127.0.0.1`, `Seguridad__PermitirHttp=false`,
   `Sitio__UrlPublica=https://<dominio>`.
4. Cloudflare solo DNS (naranja apagado para WebSocket estable) o proxy con WebSocket
   verificado; decidir con prueba real.
5. `unattended-upgrades`, fail2ban, zona horaria UTC.
6. Staging en la instancia Oracle free existente, misma imagen y compose.

**Aceptación.** HTTPS válido, cookies `Secure`, login y recuperación funcionando, circuito
SignalR estable 30 min, `/health` accesible solo por HTTPS.

### R-05 — CI/CD con staging y rollback

**Pasos.**
1. Un único workflow `ci.yml` (eliminar `build.yml`; F-09) con `permissions: contents: read`.
2. Build + test + auditoría de paquetes en cada PR.
3. Deploy a staging en `main`; deploy a producción por tag `v*`.
4. Imagen publicada con tag semántico + digest; rollback cambiando el tag en `.env`.
5. Migraciones como paso explícito previo al arranque, con verificación de compatibilidad.

**Aceptación.** Un push a `main` actualiza staging; un tag despliega producción; volver a la
versión anterior es un cambio de tag y un `compose up -d`.

### R-06 — Observabilidad mínima

**Pasos.** Logs JSON a stdout con retención en el host · Sentry ya registrado (`Program.cs:332-343`) ·
Uptime Kuma para disponibilidad · alerta de fallo de respaldo y de errores 5xx · panel simple
de circuitos activos y memoria del proceso.

**Aceptación.** Cualquier caída o fallo de respaldo genera aviso; hay evidencia de 7 días de
métricas.

### R-07 — CSP, hosts y 429

**Pasos.** CSP por entorno (`ws:` solo en Development; `frame-src 'self' blob:` para el visor
de PDF si se añade; `img-src` con el dominio de portadas) · `AllowedHosts` cerrado al dominio ·
`OnRejected` devuelve HTML/JSON en español · unificar `X-Frame-Options` con `frame-ancestors`.

**Aceptación.** Sin `unsafe-inline` en `script-src` cuando Blazor lo permita; 429 explicado;
un solo mecanismo de frame; `AllowedHosts` no es `*`.

### R-08 — Rotación de credenciales

Rotar `Definiciones:ApiKey`, `Brevo:ApiKey` y revisar el historial de Git (F-20). Mover todo a
variables de entorno del compose. **Sin esto no se abre al público.**

---

## 4. Fase 1 — Lector EPUB

Objetivo: subir un EPUB propio, leerlo con comodidad y que el glosario y las travesías
funcionen sobre el texto real.

| ID | Tarea | Prioridad | Esfuerzo | Depende de |
|---|---|---|---|---|
| R-10 | Modelo de datos y migraciones | P0 | M | R-01 |
| R-11 | Almacenamiento de archivos | P0 | M | R-10 |
| R-12 | Importador EPUB con sanitización | P0 | L | R-10 |
| R-13 | Servicio de lectura y progreso | P0 | M | R-12 |
| R-14 | Endpoints de recursos | P0 | S | R-11 |
| R-15 | UI del lector | P0 | L | R-13 |
| R-16 | Travesías automáticas | P1 | M | R-13 |
| R-17 | Glosario desde selección | P1 | M | R-15 |
| R-18 | Cola de importación y límites | P1 | M | R-12 |
| R-19 | Pruebas del lector | P0 | M | R-12..R-17 |
| R-20 | Accesibilidad del lector | P2 | M | R-15 |

### R-10 — Modelo de datos y migraciones

**Pasos.** Crear `ArchivoLibro`, `CapituloLibro`, `ProgresoLectura`, `PreferenciasLector` ·
índices únicos descritos en §2.1 · FK a `Libros` en cascada · `EntradaLectura` gana
`ProgresoInicio`/`ProgresoFin` (`decimal(5,4)`, nullable) · migración SQLite y migración
PostgreSQL en paralelo.

**Aceptación.** Borrar un libro elimina archivo, capítulos y progreso; las migraciones aplican
en limpio en ambos proveedores; el borrado de cuenta sigue completo (P1-4).

### R-11 — Almacenamiento de archivos

**Pasos.** `IAlmacenLibros` con `AlmacenLocal` (rutas `libros/{usuarioId}/{libroId}/`, escritura
atómica con `.tmp` + `File.Move`) · límite por archivo y por usuario · nombre canónico por
`Sha256` · validación de ruta para impedir *path traversal* · `AlmacenS3` (R2) queda como
adaptador previsto, no se implementa en v1 (YAGNI a 100 usuarios).

**Aceptación.** Ninguna ruta construida con datos del usuario puede salir de `/data/libros`;
test con nombres maliciosos (`../`, rutas absolutas, UTF-16).

### R-12 — Importador EPUB con sanitización

**Pasos.**
1. Validar contenedor (`mimetype`), tamaño y presencia de `META-INF/encryption.xml` → si hay
   cifrado, rechazar con mensaje "El archivo tiene DRM y no se puede abrir".
2. Extraer metadatos: título, autor, idioma, editorial, portada. Rellenar `Libro` sin
   sobrescribir datos que el usuario ya editó.
3. Leer `nav`/`ncx` para el índice; si falta, orden de `spine`.
4. Sanitizar cada capítulo con `Ganss.Xss` allowlist: `p, div, span, em, strong, i, b, u, h1-h6,
   blockquote, ul, ol, li, br, hr, img, a, table, thead, tbody, tr, th, td, sup, sub, figure,
   figcaption`. Sin `style`, sin `class`, sin `script`, sin `iframe`, sin `on*`.
5. Reescribir `src` de imágenes a `/libros/{id}/recurso/{ruta}` y extraerlas al almacén.
6. Guardar `TextoPlano` por capítulo (para búsqueda y TTS) y `Ancla` estable.
7. Calcular `Chars` y estimar páginas equivalentes (1 página ≈ 1.800 caracteres).
8. Registrar duración y tamaño para métricas.

**Aceptación.** EPUB de Gutenberg, Standard Ebooks y un EPUB real de usuario importan sin
errores; un EPUB con XSS (`<script>`, `onerror`, `javascript:`) queda inerte; un EPUB con DRM
se rechaza; importación de 5 MB < 10 s.

### R-13 — Servicio de lectura y progreso

**Pasos.** `ILectorService.ObtenerIndiceAsync`, `ObtenerCapituloAsync(usuarioId, libroId, indice)`
con verificación de propiedad · `RegistrarProgresoAsync` con *upsert* y *debounce* (cada 5 s
o al cambiar de capítulo) · `ObtenerProgresoAsync` para reanudar · cálculo de porcentaje
global y tiempo restante estimado · `PreferenciasLector` por usuario.

**Aceptación.** Dos usuarios no pueden leer el libro del otro (IDOR cubierto con test);
cerrar y reabrir el navegador reanuda en el mismo punto; el progreso se comparte entre
dispositivos con la misma cuenta.

### R-14 — Endpoints de recursos

**Pasos.** Minimal API en `Program.cs` o extensión propia · `RequireAuthorization()` +
comprobación de propietario en el handler · `Results.File` con `enableRangeProcessing: true` ·
MIME por extensión con allowlist (imágenes y fuentes) · `Cache-Control: private, max-age=604800`
· 404 uniforme para libro ajeno (no 403, para no filtrar existencia).

**Aceptación.** `curl` sin cookie → 401/302; libro ajeno → 404; `Range` devuelve 206; no se
sirve ningún archivo fuera del directorio del libro.

### R-15 — UI del lector

**Pasos.**
1. `Components/Pages/Lector.razor` (`@page "/leer/{LibroId:int}"`), protegido con `[Authorize]`.
2. Índice lateral, cabecera con título y porcentaje, navegación por capítulo y por teclado.
3. Paginación por columnas CSS (`column-width` + `column-gap`) con desplazamiento horizontal;
   alternativa de scroll continuo en móvil.
4. Controles de tipografía: tamaño, interlineado, margen, justificado, tema claro/oscuro/
   sepia; persistidos en `PreferenciasLector`.
5. Barra de progreso, tiempo restante y "continuar donde lo dejaste".
6. Búsqueda dentro del libro sobre `TextoPlano` (`ILIKE` es suficiente por libro).
7. Modo pantalla completa y ocultación de cromo al hacer scroll.
8. `wwwroot/lector.js`: módulo ES con `seleccionarTexto()` y `aplicarTema()`; sin `async void`.

**Aceptación.** Lectura fluida en móvil y escritorio; sin saltos de layout al cambiar de
capítulo; el 90 % de la pantalla es texto; funciona con teclado (flechas, PageUp/PageDown).

### R-16 — Travesías automáticas

**Pasos.** Al cerrar el lector o cada 15 min, calcular progreso recorrido y proponer una
`EntradaLectura` con `ProgresoInicio`/`ProgresoFin`, páginas equivalentes y duración ·
resumen opcional generado por GLM a partir de una ventana de texto (con cuota y confirmación
del usuario) · nunca crear entradas sin acción del usuario.

**Aceptación.** Una sesión de 20 min genera una propuesta con datos correctos; descartarla no
deja residuos; las travesías antiguas siguen mostrándose igual.

### R-17 — Glosario desde selección

**Pasos.** `seleccionarTexto()` devuelve `{ palabra, frase }` con límites (2–60 caracteres,
máximo 400 de contexto) · popover con "Definir" y "Añadir al glosario" · llamada a
`IGlosarioService` con el contexto de la frase; ampliar el contrato para aceptar
`contextoLectura` · cuota de IA aplicada · caché por palabra+contexto para no repetir coste.

**Aceptación.** Seleccionar una palabra en el lector devuelve definición global y, si aporta,
el matiz "En el contexto de la lectura"; el fallo del modelo no consume cuota (F-01).

### R-18 — Cola de importación y límites

**Pasos.** `Channel<ImportacionPendiente>` + `BackgroundService` · estado `Importando` visible
en la UI con sondeo · cancelación y timeout (60 s) · límites: 50 MB por archivo, 5.000
capítulos, 100 libros por usuario en plan gratuito · importación idempotente por `Sha256`.

**Aceptación.** Subir dos veces el mismo archivo no duplica; un archivo corrupto deja estado
`Error` con mensaje claro y no bloquea la cola.

### R-19 — Pruebas del lector

Fixtures de dominio público (un EPUB de Gutenberg y uno de Standard Ebooks) versionados como
recurso de test · pruebas de sanitización, DRM, rutas, autorización, progreso, deduplicación
y travesía automática. Objetivo: cubrir los caminos de error, no solo el feliz.

### R-20 — Accesibilidad del lector

Contraste AA en los tres temas, foco visible, navegación por teclado, `aria-label` en
controles, respeto de `prefers-reduced-motion`, tamaño mínimo de toque 44 px, y prueba real en
Android Chrome. El lector es la pantalla que más se usa: aquí la accesibilidad no es opcional.

---

## 4.5 Fase 1.5 — Diario, hábito y crecimiento

Objetivo: convertir el lector en un producto que se abre todos los días. El lector solo
resuelve "puedo leer"; estas tareas resuelven "vuelvo mañana" y "lo recomiendo". Ninguna es
requisito para leer, así que entran después de la Fase 1 y antes del cobro: la página de
precios (R-45) convierte mucho mejor con rachas, recap y demo que con una lista de funciones
del lector.

Regla de alcance: **nada de esto añade tablas nuevas de usuario sin pasar por exportación y
borrado de cuenta** (P1-4) en el mismo PR.

| ID | Tarea | Prioridad | Esfuerzo | Depende de |
|---|---|---|---|---|
| R-21 | Lomos reales en la repisa | P1 | M | R-15 |
| R-22 | Temas sepia y OLED | P1 | S | R-15 |
| R-23 | Rachas con congelación y calendario | P1 | M | R-13 |
| R-24 | Importación CSV de Goodreads | P2 | M | R-10 |
| R-25 | Wishlist compartible por enlace | P1 | S | P1-9 |
| R-26 | Recap anual en imagen | P2 | M | R-13 |
| R-27 | Demo sin registro | P1 | M | R-15 |
| R-28 | Filtros combinables en estantes | P2 | S | — |
| R-29 | Citas con ubicación | P1 | M | R-13 |

### R-21 — Lomos reales en la repisa

Sustituir la rejilla plana de `RepisaLibros.razor` por un estante con **lomos verticales**:
color derivado de forma determinista del título (hash → paleta de 12 tonos apagados), grosor
proporcional al número de páginas, título y autor en vertical, y portada al hacer *hover* o
*focus* en móvil. Sin imágenes nuevas ni generación de assets: todo CSS y SVG.

**Aceptación.** 200 libros se pintan en < 300 ms; el lomo es navegable con teclado y anuncia
título y autor a lectores de pantalla (no es decorativo); en 360 px de ancho no hay scroll
horizontal de página.

### R-22 — Temas sepia y OLED

Ampliar `PreferenciasLector.Tema` a `Claro | Sepia | Oscuro | Oled`. El sepia usa fondo
`#f4ecd8` y texto `#5b4636`; OLED usa negro puro y evita halos en AMOLED. Se aplica al lector
y, si el usuario lo permite, a la app entera.

**Aceptación.** Contraste AA en los cuatro temas; el tema persiste en servidor (no solo en
`localStorage`) y viaja entre dispositivos; `prefers-color-scheme` solo decide el tema inicial.

### R-23 — Rachas con congelación y calendario

Tabla `RegistroLectura` (`UsuarioId`, `Fecha` local del usuario, `Minutos`, `CharsLeidos`),
con índice único `(UsuarioId, Fecha)`. Racha = días consecutivos con al menos 1 minuto o 1.000
caracteres. **Dos días de congelación automática al mes**: si el usuario falta un día, se
consume una congelación y la racha no se rompe; el calendario muestra el día congelado en otro
color. Zona horaria del usuario en `ApplicationUser.ZonaHoraria` (IANA), por defecto
`Europe/Madrid`.

**Aceptación.** Un cambio de hora o de zona no rompe ni regala días; la racha se calcula en
servidor y no se puede inflar desde el cliente; el calendario marca correctamente un día
congelado; exportación y borrado incluyen `RegistroLectura`.

### R-24 — Importación CSV de Goodreads

Importador tolerante: detecta el CSV por cabecera, mapea `Title, Author, ISBN, My Rating,
Exclusive Shelf, Date Read, My Review, Number of Pages`, ignora filas sin título, no duplica
por ISBN+título y mapea `read/currently-reading/to-read` a los estantes existentes. Informe
final: importados, omitidos y errores por fila. Límite por plan (R-41).

**Aceptación.** Un export real de 500 libros importa en < 20 s; una fila corrupta no aborta el
resto; el informe es descargable; segunda importación del mismo archivo no duplica.

### R-25 — Wishlist compartible por enlace

Lista "Quiero leer" con enlace público opcional (`/l/{token}`), sin exponer correo ni el resto
de estantes, revocable y con `noindex`. Reutiliza el patrón de autorización de la vista de
amigo (P1-9) en lugar de inventar otro.

**Aceptación.** Revocar el token devuelve 404 en < 1 s; el enlace no filtra el `UsuarioId`, el
correo ni libros de otros estantes; sin token válido no hay listado.

### R-26 — Recap anual en imagen

"Tu año en <Nombre>": libros leídos, páginas, racha más larga, mejor puntuación, género
dominante y una cita. Render en el cliente con `<canvas>` (sin dependencia nueva de imágenes
en servidor) y descarga/share vía Web Share API con imagen.

**Aceptación.** La imagen se genera en < 1 s con 100 libros; funciona sin red tras cargar (los
datos ya están en la página); el texto alternativo describe el contenido; no incluye datos
privados si el usuario no lo pide.

### R-27 — Demo sin registro

Sesión anónima con un libro de dominio público **empaquetado con la app** (no depende del
catálogo de la Fase 2: un solo EPUB, licencia CC0, versionado en el repositorio de contenido y
no en el de código), tres estantes de ejemplo y el lector completo; caducidad de 24 h, sin
correo y sin subida de archivos. Al registrarse, opción de adoptar el progreso de la demo.

**Aceptación.** Un visitante nuevo lee un capítulo en ≤ 2 clics desde la portada; la demo no
consume cuota de IA del plan gratuito global (límite propio por IP); no se pueden subir
archivos ni pagar desde la demo.

### R-28 — Filtros combinables en estantes

Filtro por estante, autor, puntuación, etiqueta, idioma y estado de lectura, combinables y
reflejados en la URL (`?estante=leido&min=4`). Sin recargar la página; consulta resuelta en
servidor con `IQueryable` y paginación.

**Aceptación.** Combinar tres filtros devuelve el mismo resultado que la consulta directa;
la URL es compartible y restaurable; 200 libros filtran en < 200 ms.

### R-29 — Citas con ubicación

Guardar una cita desde el lector con `LibroId`, `CapituloId`, `Offset`, texto y nota opcional.
Vista de citas por libro y global, exportación a Markdown e imagen compartible reutilizando
R-26. Las citas respetan `CompartirEstantes`: nunca se publican sin acción explícita.

**Aceptación.** La cita reabre el capítulo en el offset exacto; recortar el texto a 500
caracteres; una cita de un libro privado no aparece en vistas de amigos.

---

## 5. Fase 2 — Catálogo de dominio público

| ID | Tarea | Prioridad | Esfuerzo | Depende de |
|---|---|---|---|---|
| R-30 | Ingesta del catálogo Gutendex | P1 | M | R-10 |
| R-31 | Búsqueda local con texto completo | P1 | M | R-30 |
| R-32 | Licencias y fuentes de archivo | P0 | S | — |
| R-33 | Descarga bajo demanda y caché compartida | P1 | M | R-11, R-30 |
| R-34 | UI del catálogo | P1 | M | R-31 |
| R-35 | Atribución y cumplimiento | P1 | S | R-32 |

### R-30 — Ingesta del catálogo Gutendex

**Pasos.** Comando `dotnet run -- importar-catalogo` · paginación de Gutendex (~2.400
peticiones, 1 req/s, reanudable por `next`) · normalización de autores e idiomas · filtro
prioritario `languages=es` · idempotencia por `(Fuente, IdExterno)` · ejecución en el host,
no en el arranque de la app · log de progreso. **La búsqueda del usuario nunca llama a
Gutendex.**

**Aceptación.** Catálogo completo importado sin duplicados; repetir el comando no duplica
filas; el proceso se puede cortar y reanudar.

### R-31 — Búsqueda local

**Pasos.** Columna `tsvector` generada (`to_tsvector('spanish', titulo || ' ' || autores ||
' ' || materias)`) con índice GIN · `pg_trgm` para tolerancia a erratas · ranking por
coincidencia, idioma y descargas · paginación · fallback `ILIKE` para SQLite en desarrollo.

**Aceptación.** "quijote" y "quijtoe" encuentran la obra; respuesta < 150 ms con el catálogo
completo; funciona sin conexión a Gutendex.

### R-32 — Licencias y fuentes de archivo

**Problema.** Los textos de Gutenberg son dominio público, pero sus archivos llevan cabecera
de licencia y marca registrada incompatibles con un producto de pago.

**Pasos.** Campo `Licencia` en `ObrasCatalogo` (`CC0`, `PublicDomain`, `PG`) · Standard Ebooks
como fuente primaria de archivos · para títulos solo disponibles en Gutenberg, decisión
documentada por obra antes de servirlos · nunca usar la marca "Project Gutenberg" en la UI ·
`DOCS/licencias.md` con criterio y checklist de alta.

**Aceptación.** Ninguna obra se sirve sin licencia registrada; el checklist se aplica en la
ingesta; la UI muestra autor, fuente y licencia.

### R-33 — Descarga bajo demanda y caché compartida

**Pasos.** Al abrir una obra del catálogo: descargar EPUB de la fuente, verificar `Sha256`,
importar con el mismo pipeline (R-12) y guardar el resultado **una sola vez** por obra, no por
usuario · `Libro` del usuario referencia `ObraCatalogoId` · progreso, reseñas y estante siguen
siendo privados por usuario · si la descarga falla, mensaje y reintento manual.

**Aceptación.** 50 usuarios leyendo la misma obra ocupan el espacio de una; borrar el libro de
un usuario no elimina la obra cacheada; el progreso no se comparte.

### R-34 — UI del catálogo

Buscador separado del de Open Library · filtros por idioma y materia · "Añadir a mi estante" y
"Leer ahora" · ficha con portada, autor, licencia y fuente · integración con el formulario de
libro existente para no duplicar altas.

### R-35 — Atribución y cumplimiento

Página `/licencias`, enlaces a la fuente, aviso de dominio público y texto de atribución en la
ficha. Actualizar `/terminos` y `/privacidad` (F-03).

---

## 6. Fase 3 — Monetización

| ID | Tarea | Prioridad | Esfuerzo | Depende de |
|---|---|---|---|---|
| R-40 | Planes, suscripciones y entitlements | P0 | L | R-01 |
| R-41 | Cuotas efectivas y mensajes de límite | P0 | M | R-40 |
| R-42 | Cobro con webhooks idempotentes | P0 | L | R-40 |
| R-43 | Prueba gratuita y estados de cuenta | P1 | M | R-42 |
| R-44 | Legal y RGPD | P0 | M | R-42 |
| R-45 | Página de precios y portal del cliente | P1 | M | R-42 |
| R-46 | Antifraude y abuso de cuotas | P1 | M | R-41 |
| R-47 | Soporte y estado del servicio | P2 | S | R-04 |

### R-40 — Planes y entitlements

**Modelo.** `Plan` (código, nombre, precio, límites) · `Suscripcion` (usuario, plan, estado,
periodo, proveedor, id externo) · `UsoMensual` (usuario, mes, definiciones IA, importaciones,
minutos leídos) · `IPlanService` como única fuente de verdad de límites.

**Propuesta inicial** (ajustable): Gratis = 5 libros, 100 MB, 10 definiciones IA/día.
Lector = 200 libros, 5 GB, 100 definiciones IA/día, catálogo completo, sin marca. Precio a
validar, no fijar en código.

**Aceptación.** Los límites se aplican en servicios, no en la UI; un usuario sin suscripción
cae siempre en Gratis; cambiar de plan surte efecto sin reiniciar sesión.

### R-41 — Cuotas efectivas

Aplicar en `LibroService.AgregarAsync`, importador, `GlosarioService` y catálogo · respuesta
tipada (`LimiteAlcanzado`) con mensaje en español y enlace a precios · nunca consumir cuota
antes de una operación que puede fallar (F-01).

### R-42 — Cobro

**Recomendación:** Paddle como *Merchant of Record* (gestiona IVA UE y facturación) frente a
Stripe + gestión propia. Checkout alojado, portal del cliente, webhooks con firma,
`EventosWebhook` para idempotencia, estados `trial/active/past_due/canceled`, periodo de
gracia de 3 días y degradación a Gratis (nunca borrado de datos).

**Requisito no técnico:** alta fiscal como autónomo/empresa para poder facturar. Bloqueante
administrativo, no de código.

**Aceptación.** Un pago de prueba activa el plan en < 10 s; un evento duplicado no duplica la
suscripción; un impago degrada sin perder libros.

### R-43 — Prueba y estados

14 días de prueba sin tarjeta (o con tarjeta, según conversión que se mida) · aviso 3 días
antes del cobro · bloqueo suave al expirar (lectura sí, subida no) · historial de facturas.

### R-44 — Legal y RGPD

ToS · privacidad con encargados (Z.ai, Paddle, Brevo, Hetzner, Cloudflare) · proceso de
retirada de contenido (DSA/DMCA) y correo dedicado · derecho de desistimiento de 14 días para
contenido digital con renuncia expresa al acceso inmediato · cookies: solo esenciales, sin
banner si no hay analítica de terceros · exportación y borrado ya existen (P1-4) y deben
incluir las tablas nuevas · DPA firmados con proveedores.

### R-45 — Precios y portal

Página `/planes` con comparativa, FAQ y CTA · portal para cambiar plan, método de pago y
facturas · enlace desde los límites alcanzados.

### R-46 — Antifraude

Verificación de correo antes de pagar (hoy el registro no la exige) · límite de cuentas por
IP/dispositivo en el registro · cuota de IA por cuenta y por IP · detección de abuso de prueba
gratuita · revisión manual de los primeros casos.

### R-47 — Soporte

Correo de soporte real (hoy `otium.lectura@gmail.com`), tiempos de respuesta publicados, FAQ y
página de estado. Play Store exige contacto verificado.

---

## 7. Fase 4 — TWA y Play Store

| ID | Tarea | Prioridad | Esfuerzo | Depende de |
|---|---|---|---|---|
| R-50 | Preparar la web para TWA | P0 | S | R-04 |
| R-51 | Proyecto Bubblewrap y firma | P0 | M | R-50 |
| R-52 | Play Console y prueba cerrada | P0 | M | R-51 |
| R-53 | Reglas de facturación en la app | P0 | S | R-42 |
| R-54 | Integración de UX (back, splash, enlaces) | P1 | S | R-51 |
| R-55 | Publicación y seguimiento | P1 | M | R-52 |

### R-50 — Web lista para TWA

`manifest.json` con `start_url` dentro del *scope*, `display: standalone`, iconos 192/512 y
maskable, `theme_color` · `/.well-known/assetlinks.json` con el SHA-256 de la clave de firma
(servido como `application/json`) · service worker que no intercepte la navegación de la TWA ·
`/privacidad` y eliminación de cuenta accesibles (Play las exige: ya existen).

### R-51 — Bubblewrap y firma

Proyecto TWA generado con Bubblewrap, `applicationId` definitivo, clave de firma en secrets de
CI (nunca en el repo), `versionCode` incremental automático, build de AAB en cada tag. Keystore
respaldado fuera del repositorio; perderla impide actualizar la app.

### R-52 — Play Console

Cuenta de desarrollador (25 USD único), verificación de identidad, ficha (textos, capturas,
icono, gráfico), formulario de seguridad de datos, clasificación de contenido y política de
privacidad. **Cuenta personal:** prueba cerrada con **12 testers durante 14 días continuos**
antes de producción → arrancar en Fase 1, no en Fase 4.

### R-53 — Facturación en la app

La app solo autentica y lee; la compra se hace en la web. Revisar el programa *reader app* de
Google si se quiere enlazar el checkout dentro de la app. Documentar la decisión y la fecha de
revisión de la política.

### R-54 — UX de la TWA

Botón atrás gestionado por Blazor (historial) · splash y tema coherentes · enlaces externos
abren navegador · descarga de EPUB desde la app con permiso de almacenamiento · aviso de
requiere conexión.

### R-55 — Publicación

Rollout gradual (10 % → 50 % → 100 %), crash reporting, y revisión de reseñas durante las dos
primeras semanas. Plan de respuesta a rechazos: la causa más común es la verificación de
*asset links* o una ficha que aparenta ser solo una web.

---

## 8. Transversal y continuo

| ID | Tarea | Fase |
|---|---|---|
| R-60 | Pruebas por fase; cobertura mínima 70 % de líneas en servicios nuevos | Todas |
| R-61 | Documentación: `DOCS/lector.md`, `DOCS/despliegue.md`, `DOCS/licencias.md`, `DOCS/monetizacion.md`, `DOCS/marca.md`; actualizar `AGENTS.md` | Todas |
| R-62 | Rendimiento: medir memoria por circuito, tiempo de importación y latencia de capítulo; presupuesto por operación | 1–3 |
| R-63 | i18n residual (F-11) y textos legales coherentes (F-03) | 3–4 |
| R-64 | Endurecer manejo de errores visibles (F-05, F-10, P2-13) | 1–3 |
| R-65 | Accesibilidad global (F-17) | 1–4 |
| R-66 | Revisión de dependencias y auditoría de paquetes en CI | Todas |

---

## 8.5 Fase 5 — Lectura compartida y mapas de personajes

Objetivo: lo que ningún tracker gratuito hace bien y justifica renovar la suscripción. Ambas
funciones se construyen sobre lo que ya existe en la Fase 1: `CapituloLibro.TextoPlano` y
`ProgresoLectura.Porcentaje`. **Nada de esto es requisito para lanzar**; entra después de M4
y se financian con los ingresos del plan Lector.

Regla de oro: cualquier función social se diseña **spoiler-safe desde el modelo de datos**, no
con un aviso en la UI. Si el filtro no está en la consulta, no existe.

| ID | Tarea | Prioridad | Esfuerzo | Depende de |
|---|---|---|---|---|
| R-70 | Modelo de personajes y menciones | P1 | L | R-10 |
| R-71 | Extracción asistida por IA con revisión | P1 | L | R-70 |
| R-72 | Mapa de personajes spoiler-safe | P1 | L | R-71 |
| R-73 | Clubes de lectura conjunta | P1 | L | R-13, R-40 |
| R-74 | Hilos por capítulo con control de spoiler | P1 | M | R-73 |
| R-75 | Moderación, privacidad y coste | P1 | M | R-73 |

### R-70 — Modelo de personajes y menciones

`PersonajesLibro` (`Id`, `LibroId`, `Nombre`, `Alias`, `Descripcion`, `PrimeraAparicion`,
`Relevancia`, `Estado`) · `MencionesPersonaje` (`PersonajeId`, `CapituloId`, `Offset`,
`Contexto`) · `RelacionesPersonaje` (`OrigenId`, `DestinoId`, `Tipo`, `PrimeraAparicion`).
`PrimeraAparicion` en **índice de capítulo**, nunca en número de página: es lo único que
sobrevive a un cambio de tipografía o de tamaño de fuente.

**Aceptación.** Borrar el libro borra personajes, menciones y relaciones en cascada; el índice
único `(LibroId, Nombre)` evita duplicados; exportación y borrado de cuenta lo incluyen.

### R-71 — Extracción asistida por IA con revisión

Pipeline en cola: por cada libro importado, extraer candidatos de `TextoPlano` (con ventana de
capítulo para tener offsets reales) y pedir al modelo JSON con nombre, alias, descripción
neutra y primera aparición. Todo candidato entra como `Estado = Pendiente`; el mapa público
solo muestra `Confirmado`. Cuota separada y caché por `Sha256` del archivo: dos usuarios con
el mismo EPUB de dominio público no pagan dos veces.

**Aceptación.** Coste medido por libro y techo por plan; un fallo del modelo deja el libro
legible (la función es opcional); reintento idempotente; ninguna descripción revela hechos
posteriores a `PrimeraAparicion`.

### R-72 — Mapa de personajes spoiler-safe

Vista de grafo (SVG, sin librería pesada) con control "hasta el capítulo N", inicializado en el
progreso real del usuario. Personajes y relaciones se filtran **en la consulta** por
`PrimeraAparicion <= N`. Leyenda de tipos de relación, búsqueda y ficha lateral. En libros sin
progreso, el control arranca en el capítulo 1 y avisa de que hay más.

**Aceptación.** Con N=3 no se puede obtener por API ningún personaje que aparezca en el 4;
inspeccionar el HTML o el JSON no revela nombres ocultos; el grafo funciona con 40 personajes
en móvil sin bloquear el circuito.

### R-73 — Clubes de lectura conjunta

`Club` (`Id`, `Nombre`, `LibroId`, `CreadorId`, `CodigoInvitacion`, `FechaInicio`,
`CalendarioJson`, `Privacidad`) · `MiembroClub` (`ClubId`, `UsuarioId`, `Rol`, `FechaAlta`) ·
`ComentarioClub` (`ClubId`, `UsuarioId`, `CapituloIndice`, `Texto`, `CreadoUtc`). Un club
pertenece a **un** libro y a un ritmo (capítulos o páginas por semana). Invitación por código
de 8 caracteres, con límite de intentos y sin revelar si el club existe (mismo patrón que
P1-6).

**Aceptación.** Un club no puede leer archivos de sus miembros: solo comparte progreso y
comentarios; el creador puede expulsar y cerrar; salir del club no borra el progreso personal;
un usuario bloqueado no puede unirse.

### R-74 — Hilos por capítulo con control de spoiler

Cada capítulo tiene su hilo. Un usuario solo ve comentarios de capítulos que ya alcanzó
(`ProgresoLectura`), y el servidor **no devuelve** los ocultos. Vista "mi siguiente capítulo"
con el comentario destacado del grupo y resumen de quién va por dónde (sin porcentajes exactos
si el usuario los oculta).

**Aceptación.** Manipular el cliente no da acceso a comentarios de capítulos no leídos; el
recuento agregado no permite inferir contenido; editar o borrar un comentario propio funciona
y queda registrado.

### R-75 — Moderación, privacidad y coste

Reporte de comentarios, cola de revisión manual, límite de clubes por usuario según plan y
cuota de IA del mapa de personajes separada de la del glosario. Actualizar `/privacidad`:
qué ve un miembro del club, qué se conserva al salir y durante cuánto tiempo. El mapa de
personajes es privado por defecto; compartirlo es una acción explícita.

**Aceptación.** Un comentario reportado se oculta en < 24 h o queda en cola visible; el coste
de IA de la fase está acotado por plan y medido en el panel; la política de privacidad
describe clubes y mapas sin ambigüedad.

---

## 9. Hitos

| Hito | Contenido | Criterio de salida |
|---|---|---|
| **M0 — Identidad** | Fase −1 (R-90..R-95) | Clearance favorable, dominio y correo con la marca nueva, build renombrado sin `Otium` visible y solicitud OEPM presentada |
| **M1 — Lector local** | Fase 0 (R-01, R-03) + Fase 1 | Un EPUB real se sube, se lee, guarda progreso y genera glosario y travesía en local |
| **M1.5 — Hábito** | Fase 1.5 | Rachas, recap y demo funcionando; la página de precios se puede enseñar sin pedir registro |
| **M2 — Producción** | R-02, R-04..R-08 | App accesible por HTTPS en el dominio, con backups restaurados y alertas |
| **M3 — Catálogo** | Fase 2 | Búsqueda local del catálogo y lectura de una obra pública sin intervención manual |
| **M4 — Cobro** | Fase 3 | Un pago de prueba activa el plan; los límites se aplican en servicios |
| **M5 — Play Store** | Fase 4 | TWA en producción con prueba cerrada superada |
| **M6 — Diferenciación** | Fase 5 | Un club de lectura completo y un mapa de personajes que no revela nada del capítulo siguiente |

Dependencias: **M0 → M1 → M1.5 → M2 → M3 → M4 → M5 → M6**. La prueba cerrada de Play arranca
en M2 para que los 14 días no bloqueen M5. La rotación de credenciales (R-08) es requisito de
M2, no de M5. M0 no se puede saltar: renombrar después de R-01 obliga a repetir el trabajo
sobre `Otium.Data` y sobre la migración inicial de PostgreSQL.

---

## 10. Riesgos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| XSS a través del HTML del EPUB | Crítico | Allowlist estricta, sin CSS ni `class` del archivo, pruebas con vectores conocidos, CSP |
| Copyright por archivos subidos | Alto | ToS, almacenamiento privado, sin compartir archivos, proceso de retirada, logs de subida |
| Marca y licencia de Gutenberg | Alto | Standard Ebooks como fuente primaria (R-32); revisión por obra |
| Coste de IA fuera de control | Alto | Cuotas por plan, caché por palabra, alerta de gasto diario, devolución de cuota en fallo (F-01) |
| Rechazo en Play por "ser una web" | Medio | TWA con *asset links* verificados, ficha con capturas reales, no ocultar que hay web |
| Pérdida de datos en la migración a PostgreSQL | Alto | Ensayo en staging, `pg_dump` previo, baseline documentado, restauración probada |
| Blazor Server se queda sin memoria con sesiones largas | Medio | Medir en M2; límite de circuitos; reinicio programado; plan de migración a cliente propio si crece |
| Crecimiento del almacenamiento | Medio | Límite por plan, deduplicación por `Sha256`, compartición de obras de catálogo |
| Fuga de datos personales a Sentry | Medio | `SendDefaultPii = false` (ya configurado), revisión de eventos |
| Colisión de marca u oposición de un tercero | Alto | Clearance formal (R-90) antes de comprar dominio o logo; registro OEPM (R-95); lista corta de nombres alternativos ya cribada |
| Renombrar tarde, con usuarios reales | Medio | Un único renombrado en M0, antes de R-01; `301` desde el dominio antiguo y el dominio viejo apuntando durante 12 meses |
| El mapa de personajes revela un spoiler | Crítico para la propuesta de valor | Filtro por `PrimeraAparicion` **en la consulta**, estado `Pendiente`/`Confirmado`, pruebas de API con N=3 y N=10 |
| Mala conducta o spoilers deliberados en clubes | Medio | Reporte y moderación (R-75), hilos por capítulo, expulsión y bloqueo ya existentes |
| Coste de IA de los mapas fuera de control | Medio | Caché por `Sha256` del EPUB, cuota separada del glosario, extracción solo de libros abiertos alguna vez |
| Dependencia de una sola persona | Alto | Documentación operativa (R-61), runbooks de restauración y despliegue |

---

## 11. Capacidad y costes estimados (v1)

| Concepto | Estimación |
|---|---|
| Usuarios | ≤ 100 registrados, 10–25 concurrentes |
| Circuitos Blazor | 25 × ~200–400 KB ≈ 10 MB + runtime; 4 GB sobran |
| Almacenamiento | 100 usuarios × 10 libros × 2 MB ≈ 2 GB; catálogo compartido ≈ 5–20 GB con caché selectiva |
| Hetzner CX22 | ~5–6 €/mes |
| Backblaze B2 | ~1 €/mes con 50 GB |
| Dominio | ~12 €/año (`<nombre>.es` + `<nombre>.app`) |
| Marca OEPM | ~150 € (clase 9) + ~50 € por clase adicional; EUIPO opcional ~850 € |
| Play Console | 25 USD único |
| Paddle | ~5 % + 0,50 € por transacción |
| Z.ai GLM | Variable; presupuesto inicial 10–20 €/mes con cuotas activas |
| Sentry / Uptime Kuma | 0 € en plan gratuito |

La demo (R-27) no añade coste fijo: un EPUB empaquetado y sesiones anónimas de 24 h con cuota
propia por IP. Su coste real es memoria de circuitos; se mide en M1.5 y se limita a 5 circuitos
anónimos simultáneos.

Coste fijo de operación: **~8–10 €/mes** antes de IA y comisiones.

---

## 12. Definition of Done por tarea

- Compila en Release sin errores ni advertencias nuevas.
- Prueba automatizada del flujo o procedimiento reproducible documentado.
- Autorización por propietario verificada si toca datos de usuario.
- Validación de entrada en la frontera del servicio, no solo en la UI.
- Migración nueva si cambia el modelo; nunca editar una aplicada.
- Sin secretos, binarios ni bases de datos en el repositorio.
- Documentación actualizada (`AGENTS.md` o `DOCS/`) si cambia arquitectura o configuración.
- Sin `async void`, sin `catch {}` vacío y sin `.Result`/`.Wait()`.

---

## 13. Seguimiento

1. Estado por tarea: `[ ]` pendiente · `[~]` en curso · `[x]` completada · `[!]` bloqueada.
2. Al cerrar: enlace al commit/PR y una línea de evidencia.
3. Las tareas de la Fase 0 que ya existen en `DOCS/hallazgos.md` (F-02, F-06, F-09, F-14, F-20) se
   marcan allí como absorbidas por este plan para no duplicar trabajo.
4. Revisión del roadmap al cerrar cada hito: lo no validado por usuarios reales se recorta,
   no se acumula.
5. La Fase −1 (R-90..R-95) se ejecuta antes de R-01. Si la clearance de R-90 no es favorable,
   se elige la siguiente opción de la lista corta antes de escribir una línea de código:
   cambiar de nombre después de crear `Otium.Data` y la migración inicial duplica el trabajo.
