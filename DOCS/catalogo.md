# Catálogo de dominio público

- **Estado:** objetivo (Fase 2). No implementado.
- **Última revisión:** 2026-11-15
- **Tareas:** R-30 a R-35 en [`../ROADMAP.md`](../ROADMAP.md)

---

## 1. Principio

El catálogo **no** es un lector de Gutenberg en vivo. Es una base de datos propia, poblada por
ingesta, con archivos servidos desde caché propia. Si la fuente externa cae, el catálogo sigue
funcionando.

## 2. Fuentes y sus reglas

| Fuente | Uso | Licencia / condición |
|---|---|---|
| **Gutendex** (API sobre Project Gutenberg) | Metadatos e identificación de obras; ingesta masiva | Datos del catálogo; el **texto** de cada obra se verifica por separado |
| **Standard Ebooks** | Fuente principal de archivos EPUB | Producción propia liberada como **CC0**; se atribuye igualmente |
| **Project Gutenberg** | Respaldo de archivo cuando no hay edición de Standard Ebooks | Dominio público en EE. UU.; la marca "Project Gutenberg" tiene condiciones de uso |

Regla: **una obra no se publica sin revisar su archivo y su licencia**. La ingesta masiva carga
metadatos en estado `Borrador`; solo pasa a `Publicada` tras la revisión (R-32).

## 3. Modelo

```text
ObrasCatalogo (Id, Fuente, IdExterno, Titulo, Autores, Idiomas, Materias,
               PortadaUrl, Licencia, Descargas, Sha256Epub, Estado, RevisadoUtc)
```

- `Estado`: `Borrador` → `Revisada` → `Publicada` → `Retirada`.
- `IdExterno` + `Fuente` es único: la ingesta es idempotente y se puede repetir.
- `Sha256Epub` deduplica archivos: dos ediciones idénticas no ocupan dos veces.
- Índice GIN de texto completo sobre título, autores y materias, con configuración española e
  inglesa (`to_tsvector('spanish', ...)` / `'english'`).

## 4. Ingesta

1. Descarga por lotes de Gutendex (paginada, con `User-Agent` propio y reintentos acotados).
2. Normalización: título, autores, idiomas, materias, portada y popularidad (`Descargas`).
3. Emparejamiento con Standard Ebooks por título/autor/idioma; si hay coincidencia, se registra
   el EPUB CC0 como archivo preferente.
4. `Upsert` idempotente por `(Fuente, IdExterno)`.
5. Informe de ingesta: nuevas, actualizadas, sin archivo, descartadas por licencia.

La ingesta es un proceso de operador (comando o *job*), no un endpoint público. No se ejecuta en
el arranque de la app.

## 5. Búsqueda y lectura

- **Búsqueda local** con `tsvector` + trigramas para tolerancia a tildes y erratas; paginación por
  cursor; sin llamadas a la API externa en la ruta del usuario.
- **Materializar:** "Leer ahora" crea el `Libro` del usuario enlazado a la obra del catálogo,
  reutilizando el formulario existente para no duplicar altas.
- **Descarga bajo demanda** la primera vez: se copia el archivo al almacén compartido; a partir de
  ahí todos los usuarios leen la misma copia (una descarga, N lecturas).
- **Portadas:** proxy y caché propios (`/catalogo/portada/{id}`), sin exponer la URL de origen ni
  permitir *hotlinking*.

## 6. Cumplimiento

- Página `/licencias` con la fuente, la licencia y el enlace a la ficha original de cada obra.
- Atribución visible en la ficha del libro: "Texto: Standard Ebooks (CC0)" o "Project Gutenberg".
- **No** se usa el logotipo ni la marca de Project Gutenberg como marca del producto.
- Proceso de retirada: obra `Retirada` deja de servirse; los libros ya creados conservan el
  archivo solo si la licencia lo permite y, si no, se sustituye por un aviso.
- Revisión por jurisdicción: dominio público en EE. UU. no implica dominio público en la UE; las
  obras se publican con la ley aplicable documentada por obra.

## 7. Costes y límites

- El catálogo se cachea con presupuesto: ~5–20 GB con caché selectiva (obras populares primero).
- La ingesta no consume ancho de banda de usuario; la descarga bajo demanda sí, y cuenta para el
  límite de almacenamiento compartido, no para el del usuario.
- Sin descargas masivas por usuario: límite por plan y por IP.

## 8. Pruebas mínimas

- Ingesta repetida del mismo lote no duplica filas.
- Búsqueda con tildes y erratas devuelve resultados esperados.
- Una obra `Retirada` no aparece en búsqueda ni se sirve su archivo.
- Un libro materializado sigue siendo legible si la fuente externa está caída.
