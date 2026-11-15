# Documentación del proyecto

Índice, estado y política documental del proyecto. **Empieza aquí.**

- **Producto:** nombre comercial **en evaluación**; el código y la carpeta dicen `Otium` hasta R-93
  (ver [`marca.md`](marca.md))
- **Repositorio:** `C:\Proyectos\Otium`
- **Última revisión del índice:** 2026-11-15

---

## 1. Regla de oro

**Una decisión, un documento.** Si un dato aparece en dos sitios, uno es la fuente y el otro
enlaza. Cuando cambie una decisión, se actualiza la fuente y se anota en `CHANGELOG.md`.

Estados posibles de un documento:

- **Vigente** — describe el sistema o el plan actual. Se mantiene al día en el mismo PR que
  cambia lo que describe.
- **Objetivo** — describe lo que se va a construir (fases futuras). No es una promesa de que
  exista hoy.
- **Histórico** — foto de un momento pasado. No se corrige salvo error grave; no se usa para
  tomar decisiones.

---

## 2. Mapa documental

| Documento | Estado | Qué contiene | Fuente de verdad para |
|---|---|---|---|
| [`../README.md`](../README.md) | Vigente | Puerta de entrada: qué es, estado, arranque rápido | Presentación del producto |
| [`../AGENTS.md`](../AGENTS.md) | Vigente | Arquitectura actual, configuración, convenciones, auditoría P0–P3 | Cómo trabajar en el código |
| [`../ROADMAP.md`](../ROADMAP.md) | Vigente | Plan por fases (Fase −1 a 5), tareas R-xx, hitos, riesgos | **Qué se construye y en qué orden** |
| [`producto.md`](producto.md) | Vigente | Alcance v1, no-objetivos, modelo de negocio, escala | **Qué es y qué no es el producto** |
| [`marca.md`](marca.md) | Vigente | Naming, evidencia de disponibilidad, acciones de marca | Nombre y registro |
| [`lector.md`](lector.md) | Objetivo | Motor de lectura EPUB, sanitización, progreso, endpoints | Diseño del lector (Fase 1) |
| [`catalogo.md`](catalogo.md) | Objetivo | Catálogo de dominio público, fuentes y licencias de archivo | Catálogo (Fase 2) |
| [`monetizacion.md`](monetizacion.md) | Objetivo | Planes, cuotas, cobro, estados de cuenta | Cobro (Fase 3) |
| [`despliegue.md`](despliegue.md) | Objetivo | Hetzner, Docker Compose, Caddy, PostgreSQL, CI/CD | Infraestructura (Fases 0 y 4) |
| [`respaldos.md`](respaldos.md) | Mixto | Respaldo actual SQLite y objetivo PostgreSQL + restic | RPO/RTO y restauración |
| [`licencias.md`](licencias.md) | Vigente | Licencias de contenido, dependencias y atribuciones | Cumplimiento de licencias |
| [`hallazgos.md`](hallazgos.md) | **Histórico** | Auditoría F-xx del 2026-10-23/28 | Trazabilidad de la deuda técnica |
| [`ficha-tecnica.md`](ficha-tecnica.md) | **Histórico** | Ficha técnica del 2026-10-28 (pre-plan) | Foto del estado anterior |
| [`requerimientos_otium.md`](requerimientos_otium.md) | **Histórico** | SRS pre-auditoría con diagramas UML | Foto del estado anterior |
| [`../CHANGELOG.md`](../CHANGELOG.md) | Vigente | Cambios notables por fecha | Historial de cambios |

---

## 3. Qué está obsoleto y por qué

| Documento | Problema | Tratamiento |
|---|---|---|
| `ficha-tecnica.md` | Describe un producto sin lector ni catálogo, con Render como único despliegue y 65 pruebas (hoy 68) | Marcado histórico. Lo vigente está en `AGENTS.md` §2 y §6 y en `despliegue.md` |
| `requerimientos_otium.md` | SRS de la beta: alcance sin lectura integrada, sin monetización y sin TWA | Marcado histórico. El alcance vigente está en `producto.md` y el plan en `ROADMAP.md` |
| `hallazgos.md` | Era el `ROADMAP.md` antiguo; se llamaba "plan de trabajo vigente" | Renombrado y marcado histórico; el plan vigente es `../ROADMAP.md` |
| `respaldos.md` | Solo cubre SQLite; el objetivo es PostgreSQL + restic + B2 | Ampliado con la sección objetivo |

Nada se borra: un documento histórico con banner claro cuesta menos que perder la evidencia.

---

## 4. Documentos que faltan (y cuándo se crean)

| Documento | Cuándo | Tarea |
|---|---|---|
| `DOCS/habitos.md` (rachas, recap, importación CSV) | Al implementar la Fase 1.5 | R-23..R-29 |
| `DOCS/lectura-compartida.md` (mapas, clubes) | Al abrir la Fase 5 | R-70..R-75 |
| `DOCS/runbook.md` (incidentes, rollback, rotación) | Antes de M2 | R-05, R-08 |
| `DOCS/moderacion.md` (reportes y retirada de contenido) | Antes de M4 | R-44, R-75 |

---

## 5. Convenciones de escritura

- Español para el dominio y la documentación; identificadores de código en el idioma del
  proyecto existente.
- Cada documento abre con **estado**, **fecha** y **a qué tarea del roadmap responde**.
- Las decisiones se escriben como decisiones, no como opciones: "se usa X porque Y".
- Lo no verificado se marca explícitamente como **pendiente de verificar**.
- Los enlaces son relativos; nunca rutas absolutas del equipo.
- Sin secretos, sin datos personales reales ni capturas con correos.
