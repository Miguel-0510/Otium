# Licencias y atribuciones

- **Estado:** vigente (política); las atribuciones por obra se generan en Fase 2.
- **Última revisión:** 2026-11-15
- **Tareas:** R-32, R-35 en [`../ROADMAP.md`](../ROADMAP.md)

---

## 1. Regla

Todo lo que se sirve tiene una licencia identificada y una atribución visible. Si no se sabe la
licencia de un archivo, **no se publica**.

## 2. Contenido

| Contenido | Origen | Licencia | Obligación |
|---|---|---|---|
| Obras del catálogo (EPUB) | Standard Ebooks | **CC0** (producción propia) | Atribución recomendada, no exigida; se muestra igual |
| Obras del catálogo (texto) | Project Gutenberg | Dominio público en EE. UU. | Verificar por jurisdicción; no usar su marca como propia |
| Metadatos | Gutendex | Datos del catálogo | Enlace a la ficha de origen |
| Portadas del catálogo | Fuente de la obra | Según obra | Servir desde caché propia, sin *hotlinking* |
| Archivos del usuario | Usuario | Responsabilidad del usuario | No se comparten ni se redistribuyen; se declara en los ToS |
| Reseñas, travesías, citas | Usuario | Licencia de uso interna | Exportables y borrables por el usuario |

## 3. Dependencias de software

| Paquete | Licencia | Uso |
|---|---|---|
| ASP.NET Core / EF Core / Identity | MIT | Base de la aplicación |
| Npgsql.EntityFrameworkCore.PostgreSQL | PostgreSQL License | Proveedor PostgreSQL |
| VersOne.Epub | MIT | Parseo de EPUB (Fase 1) |
| Ganss.Xss (HtmlSanitizer) | MIT | Saneado de HTML (Fase 1) |
| Sentry.AspNetCore | MIT | Telemetría de errores |
| xUnit / coverlet | Apache-2.0 / MIT | Pruebas y cobertura |

Regla: antes de añadir un paquete se comprueba licencia y vulnerabilidades
(`dotnet list package --vulnerable --include-transitive`). Nada de licencias copyleft fuerte en
el camino crítico del servidor.

## 4. Servicios externos

| Servicio | Qué se le envía | Condición |
|---|---|---|
| Z.ai (GLM) | Palabra o pasaje seleccionado, prompt | Datos de uso según su política; no se envían datos de cuenta |
| Brevo | Correo y enlace transaccional | Encargado de tratamiento; DPA |
| Paddle | Datos de facturación | *Merchant of Record*; asume IVA |
| Hetzner / Backblaze | Datos alojados y copias | Encargados de tratamiento; DPA |
| Open Library | Consultas de búsqueda | Sin datos personales |

## 5. Tipografía e imágenes

- Fuentes de lectura con licencia **OFL** o equivalente, incluidas en el repositorio con su
  archivo de licencia.
- Iconos propios o de licencia permisiva; nada de *scraping* de iconos de terceros.
- Ilustraciones generadas: se documenta la herramienta y se revisan antes de publicar.

## 6. Atribución en el producto

- Página `/licencias` con la lista de fuentes, licencias y enlaces.
- En cada ficha de obra: fuente, licencia y enlace al original.
- En el pie de la app: aviso de que las obras del catálogo pertenecen a su dominio público y que
  los archivos subidos son responsabilidad del usuario.

## 7. Retirada de contenido

1. Notificación a `soporte@` con la obra y la base del reclamo.
2. Obra a `Retirada` en menos de 72 h si el reclamo es verosímil.
3. Se deja de servir el archivo; los libros ya creados se sustituyen por un aviso si la licencia
   no permite conservarlo.
4. Registro de la retirada (fecha, obra, motivo) sin datos del reclamante más allá de lo necesario.

## 8. Licencia del propio código

Pendiente de decidir: repositorio privado propietario o apertura parcial. Mientras no exista
`LICENSE`, **todos los derechos reservados** por defecto. Decisión antes de M5 (R-55).
