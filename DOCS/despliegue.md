# Despliegue y operación

- **Estado:** mixto. La sección 2 describe lo que corre hoy; el resto es el objetivo (Fases 0 y 4).
- **Última revisión:** 2026-11-15
- **Tareas:** R-02 a R-08, R-50 en [`../ROADMAP.md`](../ROADMAP.md)

---

## 1. Objetivo

Un VPS único, tres contenedores y un proxy con TLS automático. Portable a cualquier proveedor:
nada depende de una plataforma PaaS concreta.

```text
Internet ─► Caddy :80/:443 (TLS, HTTP/2, Range, WebSocket)
               │
               ├─► app :5000  (ASP.NET Core + Blazor Server)
               │      ├─► postgres :5432 (red interna, sin puerto público)
               │      └─► /data (libros, claves si aplica)
               └─► restic (cron del host) ─► Backblaze B2
```

| Elemento | Elección | Motivo |
|---|---|---|
| Servidor | Hetzner CX22 (2 vCPU / 4 GB / 40 GB) | Suficiente para 100 usuarios y 25 circuitos |
| Proxy | Caddy | TLS automático, HTTP/2, WebSocket, configuración mínima |
| Base de datos | PostgreSQL 16 en contenedor | Migraciones reales; SQLite queda para desarrollo |
| Copias | restic → Backblaze B2 | Cifrado en cliente, versionado, retención barata |
| CI/CD | GitHub Actions con staging y rollback | Sin pipelines manuales |

**Coste fijo estimado:** 5–7 €/mes (VPS + B2 + dominio), antes de IA y comisiones.

## 2. Estado actual (lo que corre hoy)

| Entorno | Cómo funciona |
|---|---|
| Local | `dotnet run`; SQLite en `Data/app.db`; `https://localhost:7111` |
| Tailscale | HTTP en la interfaz privada; `Seguridad__PermitirHttp=true` si corre en `Production` |
| Render | `Production`; PostgreSQL; esquema por `EnsureCreatedAsync`; claves en tabla `DataProtectionKeys` |
| VPS Oracle (legado) | `Production`; Kestrel en loopback; Caddy/Nginx delante; pendiente de verificar |

> El despliegue en Render y el VPS Oracle son **transitorios**. La migración a Hetzner es R-04 y
> sustituye a ambos.

## 3. Contenedores

| Servicio | Imagen | Notas |
|---|---|---|
| `caddy` | `caddy:2-alpine` fijada por digest | Puertos 80/443; configuración en el repositorio |
| `app` | build multi-stage del `Dockerfile` | Usuario **no root**, `/data` con propietario correcto, `HEALTHCHECK` contra `/health` |
| `postgres` | `postgres:16-alpine` fijada por digest | Sin puerto publicado; volumen dedicado; contraseña por secreto |

Reglas:

- Un solo volumen lógico `/data` para libros y claves; nada se escribe fuera.
- Límites de memoria y CPU declarados; `restart: unless-stopped`.
- Logs a `journald` con rotación; sin logs dentro del contenedor.
- Imágenes fijadas por digest para que un `pull` no cambie el sistema sin querer.

## 4. Configuración

Variables de entorno (nunca en el repositorio):

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
ConnectionStrings__DefaultConnection="Host=postgres;Database=<bd>;Username=<usuario>;Password=<secreto>"
DataProtection__KeysPath=/data/keys
Seguridad__PermitirHttp=false
Proxy__KnownProxies__0=<IP del contenedor Caddy>
Sitio__UrlPublica=https://<dominio>
Definiciones__ApiKey=<secreto>
Brevo__ApiKey=<secreto>
Sentry__Dsn=<opcional>
```

- `ForwardedHeaders` debe confiar en el proxy; si no, `exigirHttps`, HSTS y cookies `Secure` no
  funcionan con TLS terminado en Caddy.
- Los secretos viven en un `.env` fuera del repositorio o en el gestor de secretos del CI.
- Cambiar la contraseña de Postgres exige actualizar el `.env` y reiniciar `app`, no recrear el
  volumen.

## 5. Migraciones

- **Decisión:** `MigrateAsync` al arrancar solo si hay una única instancia (es el caso).
- Un fallo de migración **detiene** la app: es preferible a servir con esquema inconsistente.
- Antes de migrar en producción: `pg_dump` + verificación de que el respaldo se puede restaurar.
- Cambios destructivos (renombrar/eliminar columnas) van en dos despliegues: primero compatibles,
  luego limpieza.
- El esquema se versiona con migraciones Npgsql propias (R-01), no con `EnsureCreated`.

## 6. CI/CD

```text
PR        → restore, build (Release, warnings as errors), test, auditoría de paquetes
main      → build de imagen + push a registro + despliegue a staging
tag       → despliegue a producción con aprobación manual
```

- Rollback: imagen anterior etiquetada y `docker compose up -d` con la etiqueta previa; la base de
  datos **no** se revierte sola (por eso las migraciones destructivas van en dos pasos).
- Migración de datos con ventana de mantenimiento y aviso en `/soporte`.
- Ningún despliegue toca `/data` ni el volumen de Postgres.

## 7. Red y seguridad

- Firewall: solo 22 (clave, no contraseña), 80 y 443. Postgres y app sin exposición pública.
- TLS obligatorio; HSTS una vez verificado el dominio; cookies `Secure`, `HttpOnly`, `SameSite=Lax`.
- CSP por entorno: `ws:` solo en desarrollo; `wss:` en producción para Blazor Server.
- `AllowedHosts` restringido al dominio real.
- `/.well-known/assetlinks.json` servido como `application/json` para la TWA.
- Actualizaciones del host: ventana mensual, reinicio avisado.

## 8. Observabilidad

| Señal | Herramienta |
|---|---|
| Errores | Sentry (solo si `Sentry:Dsn` está configurado, `SendDefaultPii=false`) |
| Salud | `/health` con comprobación de base de datos y antigüedad del último respaldo |
| Disponibilidad | Uptime Kuma o equivalente externo |
| Recursos | `docker stats` y alerta de disco > 80 % |
| Coste IA | Contador diario en `UsoMensual` + alerta de gasto |

## 9. Capacidad

| Recurso | Estimación |
|---|---|
| Circuitos concurrentes | 25 × ~200–400 KB ≈ 10 MB + runtime |
| Libros | 100 usuarios × 10 libros × 2 MB ≈ 2 GB |
| Catálogo cacheado | 5–20 GB con caché selectiva |
| Disco del VPS | 40 GB: suficiente con caché acotada y limpieza |

Si el disco supera el 80 %, primero se poda la caché del catálogo; después se amplía el volumen.
