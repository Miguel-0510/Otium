# Respaldos y restauración

- **Estado:** mixto. §2–§5 describen el respaldo actual (SQLite); §6 es el objetivo de la Fase 0.
- **Última revisión:** 2026-11-15
- **Tareas:** R-02 en [`../ROADMAP.md`](../ROADMAP.md)

---

## 1. Objetivos de recuperación

| Métrica | Hoy (SQLite) | Objetivo (PostgreSQL + restic) |
|---|---|---|
| **RPO** | 24 h (un respaldo diario) | 24 h para la base; 1 h para archivos nuevos |
| **RTO** | < 1 h | < 30 min, probado y documentado |

Un respaldo que nunca se ha restaurado no es un respaldo. La prueba mensual es parte del
procedimiento, no una recomendación.

## 2. Alcance actual

Se respalda **solo** la base SQLite (`app.db`).

No se respaldan:

- Claves de Data Protection en filesystem (`DataProtection__KeysPath`), si se configuran. Sin esa
  ruta, las claves viven en la tabla `DataProtectionKeys` y quedan cubiertas por la base.
- Archivos de configuración y secretos.
- Archivos de libros (aún no existen).

## 3. Procedimiento actual

`BackupService` se registra como singleton en `Program.cs`.

1. `PRAGMA wal_checkpoint(TRUNCATE)` para vaciar el WAL al archivo principal.
2. Copia del `.db` a `Backups/`, junto a la base.
3. Retención: se conservan los 5 respaldos más recientes.

Nombres: `<nombre>_YYYYMMDD_HHmmss.db` (el prefijo lo fija el nombre comercial de R-90).

## 4. Restauración actual

1. Detener la aplicación.
2. Copiar el `.db` del respaldo sobre la ruta de la base activa (`app.db`).
3. Verificar con `BackupService.VerificarIntegridadAsync()`.
4. Reiniciar y comprobar login y apertura de un libro.

## 5. Límites conocidos

- Los respaldos viven en la misma carpeta que la base: un fallo de disco los pierde juntos.
- No hay copia fuera del servidor.
- La retención silencia errores de borrado (F-02).
- No cubre archivos de libros ni el llavero de claves.
- En producción actual (`C:\OtiumDatos\app.db`) los respaldos quedarían en
  `C:\OtiumDatos\Backups/`.

Todo esto lo resuelve la §6.

## 6. Objetivo (Fase 0, R-02)

### Qué se respalda

| Elemento | Método |
|---|---|
| Base PostgreSQL | `pg_dump --format=custom`, cifrado con `age` |
| Archivos de libros (`/data/libros`) | `restic` con cifrado en cliente |
| Llavero de Data Protection (`/data/keys`) | `restic` (si no está en la base) |
| Configuración | No se respalda; vive en el gestor de secretos |

### Destino y retención

- **Backblaze B2** como destino único, con `restic` para deduplicación y cifrado.
- Retención: **7 diarios · 4 semanales · 6 mensuales**.
- Las copias de Postgres se ejecutan desde el **host** (no desde el contenedor de la app) por
  cron, con alerta a Sentry si fallan.

### Verificación

- `restic check --read-data-subset=5%` semanal.
- `VerificarIntegridadAsync()` se mantiene para SQLite en desarrollo.
- `/health` expone la antigüedad del último respaldo; si supera 48 h, la comprobación falla.
- **Prueba mensual de restauración:** base limpia → `pg_restore` → arranque → login → abrir un
  libro → confirmar progreso. Se anota fecha y resultado.

### Procedimiento de restauración objetivo

```bash
# 1. Parar la app
docker compose stop app

# 2. Restaurar la base
age -d -i /ruta/clave.txt backup.dump.age | \
  docker compose exec -T postgres pg_restore -U <usuario> -d <bd> --clean --if-exists

# 3. Restaurar archivos y claves
restic restore latest --target /data

# 4. Arrancar y verificar
docker compose up -d app
curl -fsS https://<dominio>/health
```

Criterio de éxito: un entorno funcional en **menos de 30 minutos** y `/health` en verde.

## 7. Notas operativas

- La carpeta de respaldos necesita permisos de escritura para el usuario que ejecuta la app.
- No confiar solo en copias locales: B2 es el destino real.
- Rotar la contraseña de restic si se comparte el equipo; sin ella las copias son ilegibles.
- Las copias contienen datos personales: cifradas en origen y con acceso restringido.
