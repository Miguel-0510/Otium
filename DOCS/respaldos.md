# Respaldos de SQLite — Otium

Documentación operativa del servicio de respaldo de la base de datos SQLite
(`Services/BackupService.cs`).

## Objetivos de recuperación

| Métrica | Valor | Justificación |
|---------|-------|---------------|
| **RPO** (Recovery Point Objective) | 24 horas | Se ejecuta un respaldo diario al iniciar la aplicación. En el peor caso se pierden hasta 24 horas de datos (libros, travesías, glosario, amistades). |
| **RTO** (Recovery Time Objective) | < 1 hora | La restauración consiste en copiar el archivo `.db` de un respaldo sobre la base activa y reiniciar la aplicación. No requiere migraciones ni reconfiguración. |

## Alcance

Se respalda únicamente la base de datos SQLite (`app.db`). No se respaldan:
- Claves de Data Protection en filesystem (`DataProtection__KeysPath`), si se configuran. Sin esa ruta, las claves viven en la tabla `DataProtectionKeys` y quedan cubiertas por el respaldo de la base de datos.
- Archivos de configuración (`appsettings.json`, variables de entorno).
- Secretos (User Secrets, variables de entorno, claves de API).

## Procedimiento de respaldo

El servicio `BackupService` se registra como singleton en `Program.cs` (`Services/BackupService.cs`).

El respaldo se ejecuta bajo demanda mediante `CrearRespaldoAsync()`:

1. Ejecuta `PRAGMA wal_checkpoint(TRUNCATE)` para vaciar el WAL al archivo principal.
2. Copia el archivo `.db` a la carpeta `Backups/` (al lado de la base de datos).
3. Aplica retención: conserva los 5 respaldos más recientes y elimina los antiguos.

Los archivos se nombran como `otium_YYYYMMDD_HHmmss.db`.

## Procedimiento de restauración

1. Detener la aplicación de Otium.
2. Copiar el archivo `.db` del respaldo deseado sobre la ruta de la base activa (`app.db`).
3. (Opcional) Verificar integridad con `BackupService.VerificarIntegridadAsync()`.
4. Reiniciar la aplicación.

## Retención

- Máximo 5 archivos de respaldo.
- Los archivos más antiguos se eliminan automáticamente al crear uno nuevo.
- La carpeta de respaldos se crea automáticamente si no existe.

## Verificación

- `VerificarIntegridadAsync()` ejecuta `PRAGMA integrity_check` y `PRAGMA foreign_key_check`.
- Devuelve `true` solo si ambos comandos devuelven `ok` y no hay problemas de claves foráneas.

## Notas operativas

- Los respaldos se almacenan en la misma carpeta que la base de datos, bajo la subcarpeta `Backups/`.
- En producción (`C:\OtiumDatos\app.db`), los respaldos quedarían en `C:\OtiumDatos\Backups/`.
- Asegurar que la carpeta de respaldos tenga permisos de escritura para el usuario que ejecuta la aplicación.
- No confiar exclusivamente en los respaldos locales; considerar copiarlos a almacenamiento externo (blob storage, NAS, etc.) según la criticidad de los datos.