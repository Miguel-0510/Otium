# Lector — diseño técnico

- **Estado:** objetivo (Fase 1). No implementado.
- **Última revisión:** 2026-11-15
- **Tareas:** R-10 a R-20 en [`../ROADMAP.md`](../ROADMAP.md)

---

## 1. Decisiones que no se negocian

| Decisión | Motivo |
|---|---|
| **Parseo en servidor** (`VersOne.Epub`) | El cliente recibe HTML ya limpio; una sola implementación y control total del saneado |
| **Sin CSS ni `class` del EPUB** | Elimina superficie de ataque y garantiza tipografía consistente |
| **Sin `iframe` ni HTML activo** | Un EPUB es contenido de terceros: se trata como no confiable |
| **Binarios fuera de SignalR** | Imágenes y descargas van por HTTP con `Range`; el circuito nunca transporta MB |
| **Progreso por capítulo + offset de caracteres** | Sobrevive a cambios de fuente, tamaño y dispositivo |
| **Capítulos inmutables una vez importados** | El progreso y las citas apuntan a un `CapituloId` estable |

## 2. Modelo de datos

```text
ArchivoLibro      (Id, LibroId, Formato, TamanioBytes, Sha256, RutaAlmacen, Estado, MensajeError, FechaSubida)
CapituloLibro     (Id, LibroId, Indice, Titulo, Ancla, Html, TextoPlano, Chars)   único (LibroId, Indice)
ProgresoLectura   (Id, UsuarioId, LibroId, CapituloId, Offset, Porcentaje,
                   UltimaLecturaUtc, MinutosAcumulados)                          único (UsuarioId, LibroId)
PreferenciasLector(UsuarioId, TamanoFuente, Interlineado, Margen, Tema, Justificado)
```

- `ArchivoLibro` es único por `Libro`; `(UsuarioId, Sha256)` deduplica subidas del mismo archivo.
- `Porcentaje` se calcula sobre `Chars`, no sobre páginas: es la única medida común entre EPUB,
  PDF y maquetaciones distintas.
- `EntradaLectura` gana `ProgresoInicio`/`ProgresoFin` (`decimal(5,4)`, nullable) para las
  travesías; las páginas se conservan para PDF.
- Borrado en cascada desde `Libro` y desde la cuenta (P1-4).

## 3. Importación

```text
Subida → validar contenedor → detectar DRM → extraer metadatos → leer índice
       → sanear capítulo → extraer imágenes → calcular TextoPlano → guardar → encolar mapa (Fase 5)
```

1. **Validación:** `mimetype` correcto, tamaño dentro del plan, ZIP válido, sin rutas `../`.
2. **DRM:** si existe `META-INF/encryption.xml` con cifrado, se rechaza y no se guarda nada.
3. **Metadatos:** título, autor, idioma, editorial y portada; nunca se sobrescribe lo que el
   usuario ya editó.
4. **Índice:** `nav`/`ncx`; si falta, orden del `spine`. Cada capítulo recibe `Indice` y `Ancla`.
5. **Saneado:** allowlist explícita. Todo lo demás se descarta.
6. **Imágenes:** se extraen al almacén y el `src` se reescribe a
   `/libros/{id}/recurso/{ruta}`.
7. **Texto plano:** se guarda por capítulo para búsqueda, TTS y mapas de personajes.
8. **Atomicidad:** el archivo se escribe con `.tmp` + `File.Move`; la fila se marca `Listo` al
   final. Un fallo deja `Error` con mensaje para el usuario y no bloquea el libro existente.

### Allowlist de saneado

```text
p div span em strong i b u h1 h2 h3 h4 h5 h6 blockquote ul ol li br hr img a
table thead tbody tr th td sup sub figure figcaption
```

- Atributos permitidos: `href` (solo `http/https/mailto`), `src` (reescrito), `alt`, `title`,
  `colspan`, `rowspan`.
- Prohibido: `style`, `class`, `id` del archivo, `script`, `iframe`, `object`, `embed`, `form`,
  `input`, todo `on*` y `javascript:`.
- Vectores de prueba obligatorios: `<script>`, `<img onerror>`, `<a href="javascript:">`,
  `<svg><script>`, entidades dobles y `data:` URIs.

## 4. Endpoints

| Método | Ruta | Autorización | Notas |
|---|---|---|---|
| `GET` | `/libros/{id}/recurso/{**ruta}` | Cookie + propietario | `Range`, `Cache-Control: private`, `Content-Disposition: inline` |
| `GET` | `/libros/{id}/epub` | Cookie + propietario | Descarga del original |
| `GET` | `/libros/{id}/capitulos` | Cookie + propietario | Índice en JSON (contrato reutilizable por un cliente futuro) |
| `GET` | `/libros/{id}/capitulos/{indice}` | Cookie + propietario | HTML saneado + metadatos |
| `POST` | `/libros/{id}/progreso` | Cookie + propietario | *Upsert* con validación de rango |

Reglas: autorización **en el servicio**, no en el componente; `404` para libro ajeno (no `403`,
para no filtrar existencia); sin exponer rutas del sistema de archivos.

## 5. Progreso

- El cliente reporta `(capituloId, offset, porcentaje)` cada **5 s** o al cambiar de capítulo,
  con *debounce*; el servidor valida que el capítulo pertenezca al libro y que el offset esté
  dentro de `Chars`.
- Reanudar: se abre el capítulo guardado y se desplaza al offset. Si la ventana es más corta, se
  ancla al párrafo contenedor.
- `MinutosAcumulados` se incrementa con tiempo de sesión activo, acotado por capítulo, para
  alimentar rachas y recap sin depender del cliente.
- **Nunca** se sobrescribe un progreso más avanzado con uno anterior por un cliente desincronizado:
  se compara `UltimaLecturaUtc` y se acepta solo lo más reciente.

## 6. Preferencias y tipografía

- Tamaño de fuente, interlineado, margen, justificado y tema (`Claro`, `Sepia`, `Oscuro`, `Oled`).
- Se guardan en servidor (viajan entre dispositivos); `localStorage` solo como caché inicial para
  evitar parpadeo.
- La hoja de estilos del lector es propia; el EPUB nunca aporta estilos.

## 7. Travesías y glosario

- **Travesía automática:** al cerrar sesión de lectura con progreso nuevo se propone una entrada
  con `ProgresoInicio`/`ProgresoFin`; el usuario confirma o edita. No se crean entradas vacías.
- **Glosario por selección:** la selección viaja como texto + `CapituloId` + `Offset`, de modo que
  la definición queda ligada a la obra y no a una palabra suelta. Respeta cuotas del plan y
  devuelve la cuota si la llamada falla (F-01).

## 8. Límites y fallos esperados

| Caso | Comportamiento |
|---|---|
| EPUB con DRM | Rechazo explícito, sin guardar archivo |
| EPUB corrupto | Error de importación con mensaje en español; el libro previo sigue intacto |
| Capítulo gigante (+2 MB de HTML) | Se divide en fragmentos por bloque, nunca se sirve entero |
| Imagen no encontrada | Se sirve un marcador; el capítulo no falla |
| Circuito caído | El progreso local se reintenta al reconectar; no se pierde la última posición |
| Libro borrado durante la lectura | 404 y vuelta al estante con aviso |

## 9. Accesibilidad y rendimiento

- Navegación por teclado en índice, capítulo y controles; foco visible.
- Estructura semántica conservada (`h1..h6`, listas, tablas) y `lang` del libro.
- Presupuesto: capítulo de 50 000 caracteres servido y pintado en **< 300 ms** en el VPS.
- Memoria: el HTML del capítulo no se retiene en el circuito después de renderizar.

## 10. Pruebas mínimas

- Importación de EPUB de Gutenberg, Standard Ebooks y uno real de usuario.
- XSS: los cinco vectores de la §3 quedan inertes.
- Autorización: usuario B no puede leer, descargar ni escribir progreso del libro de A.
- Progreso: ida y vuelta exacta de capítulo + offset; cliente desincronizado no retrocede.
- Borrado: libro y cuenta eliminan archivo, capítulos y progreso.
