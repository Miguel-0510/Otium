# Marca — nombre en evaluación

Registro de la búsqueda de nombre, la evidencia recogida y lo que queda por hacer.
Complementa la **Fase −1** de [`../ROADMAP.md`](../ROADMAP.md) (R-90..R-95).

- **Fecha de la investigación:** 2026-11-15
- **Decisión:** **Olvium seleccionada para clearance formal (R-90)**. `Otium` queda descartado por
  colisión de nicho. Se han completado diez rondas de criba (§3.1 a §3.9) más la selección
  documentada en §3.10. `Elicona` deja de ser candidata principal.
- **Estado:** criba hecha; **elección adoptada, clearance formal pendiente (R-90)**
- **Alcance:** este documento es evidencia técnica, no asesoría legal

---

## 1. Criterios

1. Guardar la mística de la lectura (ocio, silencio, Musas, biblioteca, clásico).
2. Ser **muy fácil de recordar y escribir** en español, sin tildes ni dígrafos problemáticos.
3. No estar registrado **en el mismo nicho** (software de lectura, libros, SaaS cultural).
4. Disponibilidad razonable de dominio y de nombre en tiendas.
5. Pronunciable en español e italiano; legible en inglés.

---

## 2. Por qué no se puede seguir llamando Otium

| Tercero | Qué es | Riesgo |
|---|---|---|
| `otium-library.com` | *book tracker and reading journal*: PWA en React, Supabase, cobro con Paddle, Open Library y Google Books, 10 idiomas, 4,99 $/mes o 99 $ de por vida, Trustpilot 4.3 | **Colisión directa de nicho y de categoría** |
| `otiumbookshelf.com` | Estanterías, notas, progreso, "pasaporte de lectura", clubes de lectura lenta, rachas | Colisión directa |
| `otiumfeed.it` | Producto italiano de lectura | Colisión en el mercado italiano |
| USPTO `OTIUM` (serial 50156419, clase 041, Otium Holdco LLC, estado 2026-10-12) | Solicitud de marca en servicios culturales/educativos | Riesgo de oposición y de confusión de marca |

Conclusión: `Otium` no es defendible ni diferenciable. El coste de renombrar crece con cada
tabla nueva de la base de datos, así que el renombrado se hace **antes** de la Fase 0.

---

## 3. Candidatos descartados

| Nombre | Motivo del descarte |
|---|---|
| **Estrofa** | Lector de ebooks antiguo con ese nombre; el `.com` está tomado |
| **Florilegio** | App iOS `Florilegio` (App Store, id 6789623560) y marca de una librería de Florencia |
| **Scholia** | Existe "Scholia: Reading Companion", misma categoría |
| **Lacuna** | Varias apps (música con IA, grupo, juego, aventura) y `.com` tomado |
| **Breviario** | Dominado por apps litúrgicas; confusión semántica religiosa |
| **Marginalia, Colofón, Vitela, Tintero, Remanso, Sosiego, Glosa** | Todos los `.com` registrados; sin señal de disponibilidad |
| **Flyleaf, Dogear, Endpaper, Colophon** | `.com` registrados y varios productos editoriales activos |
| **Camena, Lucerna, Scrinium, Holganza, Lectio** | `.com` registrados; sin diferenciación en tiendas |
| **Incunable** | Solo un resultado irrelevante en App Store, pero `.com` tomado y evocación "libro antiguo" demasiado literal |

Regla aplicada: un `.com` tomado no es bloqueante por sí solo, pero junto con una app del mismo
nicho sí lo es. Ninguno de los descartados pasaba ambas pruebas.

---

## 3.1 Alternativas cribadas (ronda 2, 2026-11-15)

Comprobación: RDAP de Verisign para `.com`, DNS (`NS`) para `.es` y `.app`, iTunes Search API
para App Store ES y búsqueda pública de Play Store ES. ✅ libre · ✗ registrado · ⚠️ existe pero
en otro sector.

| Nombre | Evocación | `.com` | `.es` | `.app` | Apps en el nicho |
|---|---|---|---|---|---|
| **Alciona** | Alción/Alcyone: los "días de alción", la calma del mar; estrella de las Pléyades | ✗ | ✅ | ✅ | Ninguna (0 iOS, 0 Play) |
| **Filomela** | Filomela se convierte en ruiseñor: el canto como forma de lectura | ✗ | ✅ | ✅ | Ninguna |
| **Aganipe** | Fuente de las Musas en el Helicón, junto a Elicona | ✅ | ✅ | ✅ | Ninguna |
| **Calirroe** | Náyade de la fuente "de bello fluir" | ✅ | ✅ | ✅ | Ninguna |
| **Luscinda** | Pastora del Quijote; *luscinia* = ruiseñor | ✅ | ✅ | ✅ | Ninguna |
| **Melibea** | La amada de Calisto, en *La Celestina* | ✗ | ✅ | ✅ | Ninguna |
| **Holganza** | El ocio con el que se lee: traducción exacta de *otium* | ✗ | ✅ | ✅ | Ninguna |
| **Canturia** | Canto llano, melodía que se repite | ✗ | ✅ | ✅ | Ninguna |
| **Amarilis** | Pastora de la novela bucólica | ✗ | ✅ | ✅ | Solo ruido sin relación |
| **Pieria** | Región de las Musas y su fuente | ✗ | ✅ | ✗ | Una app educativa ajena |
| **Egeria** | Ninfa que inspira; en español, la que sugiere ideas | ✗ | ✗ | ✅ | Ninguna |
| **Eolia** | Isla de los vientos y el arpa eolia | ✗ | ✗ | ✅ | Una app de música |
| **Clorinda** | Guerrera de *Jerusalén liberada* | ✗ | ✅ | ✅ | Radios de una ciudad argentina |
| **Cirra** | Puerto sagrado de Delfos | ✗ | ✅ | ✗ | App social y del tiempo |
| **Celandia** | Inventada: "cel" + "landia", país imaginario | ✅ | ✅ | ✅ | Sin comprobar en iOS |
| **Endecha** | Lamento cantado de la tradición oral | ✗ | ✅ | ✅ | Sin comprobar en iOS |
| **Robleda** | Robledal, bosque de roble | ✗ | ✅ | ✅ | Sin comprobar en iOS |
| **Alborada** | Canción del amanecer | ✗ | ✗ | ✗ | App de finanzas |
| **Luminaria** | Luz que se deja encendida | ✗ | ✗ | ✗ | Varias, ajenas |
| **Atalaya** | Torre para mirar lejos | ✗ | ✗ | ✗ | Varias, ajenas |
| **Mirador** | Balcón sobre el paisaje | ✗ | ✗ | ✗ | Varias, ajenas |
| **Quietud** | Calma absoluta | ✗ | ✗ | ✅ | Apps de meditación |
| **Tregua** | Pausa, descanso | ✗ | ✗ | ✗ | Productividad y finanzas |
| **Zenobia** | Reina de Palmira | ✗ | ✗ | ✅ | Ruido (juegos "Zenonia") |
| **Nereida** | Ninfa del mar | ✗ | ✗ | ✅ | Una app de aventura |

**Descartadas por colisión directa:** Eluvia (app de productividad), Somnia (apps de sueño),
Cantiga (varios resultados en Play), Ondina (app de vídeo), Salmodia y Hontanar (`.es` y
`.app` ocupados y sonido más litúrgico o rural que lector).

**Conclusión de la ronda 2.** Solo seis nombres reúnen las tres condiciones fuertes (`.es` y
`.app` libres, `.com` libre o irrelevante, cero apps): **Alciona, Filomela, Aganipe, Calirroe,
Luscinda y Melibea**. `Aganipe`, `Calirroe` y `Luscinda` conservan además el `.com`.

## 3.2 Ronda 3 — palabras cortas en español, griego y latín (2026-11-15)

Criterio: 2–3 sílabas, sonoridad abierta, sin apps en el nicho. Comprobado igual que la ronda 2.

| Nombre | Origen y significado | Sílabas | `.com` | `.es` | `.app` | Apps | Veredicto |
|---|---|---|---|---|---|---|---|
| **Ismene** | Ἰσμήνη, hija de Edipo; suena a "sabia" | 3 | ✗ | ✅ | ✅ | 0 | **Apto** |
| **Kelaino** | Κελαινώ, "la oscura", una de las Pléyades | 3 | ✗ | ✅ | ✅ | 0 | **Apto** |
| **Krissa** | Crisa, el puerto sagrado de Delfos | 2 | ✗ | ✅ | ✅ | 0 | **Apto** |
| **Ianthe** | Ἴανθε, "violeta"; ninfa del mar | 3 | ✗ | ✅ | ✅ | 0 (Play: app personal) | **Apto** |
| **Laelia** | Nombre latino; laelia es una orquídea | 3 | ✗ | ✅ | ✗ | 0 | Apto con `.es` |
| **Filis** | Pastoral latina; suena a *filis*/*file* | 2 | ✗ | ✅ | ✗ | 0 | Dudoso por fonética inglesa |
| **Estío** | El verano, en español literario | 2 | ✗ | ✅ | ✗ | 0 en el nicho | Apto sin tilde en el dominio |
| **Rivus** | Latín: arroyo, corriente | 2 | ✗ | ✗ | ✗ | 1 (Play) | Descartado |
| **Lilaia** | Ninfa y fuente cerca de Delfos | 3 | ✗ | ✅ | ✅ | 1 exacta (iOS) | Descartado |
| **Aulis** | Puerto donde parte la flota griega | 2 | ✗ | ✅ | ✗ | ruido | Descartado |
| **Naias** | Náyade, ninfa de agua | 2 | ✗ | ✅ | ✅ | hotel y apps | Descartado |
| **Elea** | Colonia griega; escuela de Parménides | 2 | ✗ | ✅ | ✗ | software educativo | Descartado |
| **Linnea** | Nombre botánico nórdico | 3 | ✗ | ✗ | ✅ | apps de salud | Descartado |
| **Thea** | Θεά, "diosa" | 1 | ✗ | ✗ | ✗ | — | Descartado (nombre común) |
| **Ione** | Ἰόνη, ninfa marina | 2 | ✗ | ✗ | ✗ | — | Descartado |
| **Aeda** | Aeda: la mujer que canta la epopeya | 2 | ✗ | ✗ | ✗ | — | Descartado |

**Hallazgo de la ronda 3.** Las palabras cortas y bonitas del diccionario están prácticamente
agotadas en `.es` y `.app`: de 35 comprobadas, solo cinco pasan todas las pruebas. Los nombres de
dos sílabas son los más castigados por el registro masivo; los griegos de tres sílabas con
terminación en vocal son los que sobreviven.

**Finalistas cortos:** `Ismene`, `Kelaino`, `Krissa`, `Ianthe` y `Laelia`.

## 3.3 Ronda 4 — ocho palabras propuestas (2026-11-15)

Método idéntico: RDAP de Verisign (`.com`), DNS `NS` (`.es`, `NXDOMAIN` = sin delegación),
RDAP de CentralNic y DNS (`.app`), iTunes `country=es|us`, Play Store y búsqueda web del nicho.

| Nombre | Significado | `.com` | `.es` | `.app` | Colisión encontrada | Veredicto |
|---|---|---|---|---|---|---|
| **Verbum** | Latín: palabra, verbo | Ocupado (1994) | Ocupado | Ocupado | `verbum.com`: plataforma de estudio bíblico de Faithlife, con apps iOS/Play | **Descartado** |
| **Pensum** | Latín: peso, tarea, lana por hilar; en América, plan de estudios | Ocupado (2003) | **Libre** | Ocupado | `Pensum`, `PensumRD`, `Pensual`, `MiPensum`, `Pensum Calorie Tracker`; todas educativas | **Descartado** (nicho educativo saturado) |
| **Solium** | Latín: trono, asiento; también bañera | Ocupado (CSC, 1999) | Ocupado | Ocupado | `Taenia solium` es la tenia del cerdo; Solium Capital es de Morgan Stanley | **Descartado** (parásito + marca financiera) |
| **Nemus** | Latín: bosque, arboleda sagrada | Ocupado (1998) | Ocupado | Ocupado | `nemus.app`: plataforma forestal de Cesefor; `Némus` (navegación), `Nemus Longevity` | **Descartado** |
| **Ozio** | Italiano: ocio (lat. *otium*) | Ocupado (1998) | Ocupado | Ocupado | `Ozio` (market con cashback, TR), OZIO (botiquines, cosmética japonesa) | **Descartado** |
| **Bruma** | Español: niebla tenue; fig. falta de claridad | Ocupado (2003) | Ocupado | Ocupado | Bruma Cosmetics, Bruma Perfumes, `Bruma Mail App`, `Bruma Emulator`, `Bruma: jardines de luz` | **Descartado** |
| **Albor** | Español: primera luz del amanecer; blancura | Ocupado (2000) | Ocupado | Ocupado | **`alborapp.com`: "Ideas from great books"**, app de libros; Ediciones Albor; `Albor: Devocional diario` | **Descartado** (colisión directa de nicho) |
| **Idilio** | Español: poema bucólico; relación amorosa | Ocupado (2023) | Ocupado | **Libre** | `idilio.tv`: microdramas, +1 M descargas, app iOS/Play | Riesgo alto; no recomendado |

**Conclusión de la ronda 4.** Ninguna de las ocho palabras reales es utilizable. Dos son
directamente del nicho (**Verbum** para estudio de libros, **Albor** para "ideas de grandes
libros"), una es un parásito y una marca financiera (**Solium**), dos están saturadas de apps
(**Pensum**, **Idilio**) y tres tienen el nombre comercial ocupado en sectores visibles
(**Nemus**, **Ozio**, **Bruma**). Las palabras del diccionario, incluso las cultas, ya no son
territorio libre.

## 3.4 Ronda 5 — nombres inventados por composición (2026-11-15)

Regla: se combinan las raíces anteriores (ocio, cielo, albor, nemus, pensum, idilio, sol) con
sufijos naturales en español (`-elo`, `-iel`, `-eo`, `-ía`, `-ina`). Se descartan los puramente
descriptivos (`Ociolibro`) porque una marca descriptiva es difícil de registrar y de defender.

| Nombre | Composición | Sílabas | `.com` | `.es` | `.app` | Apps | Web |
|---|---|---|---|---|---|---|---|
| **Ocioleo** | *ocio* + *leo* (leo por ocio) | 4 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Ozielo** | *ocio* + *cielo* (se lee "o cielo") | 3 | ✅ | ✅ | ✅ | 0 | solo un handle personal |
| **Pensiel** | *pensum*/*pensar* + *-iel*; evoca *pensil* | 2 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Nemoriel** | *nemus* (bosque sagrado) + *-iel* | 3 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Alboriel** | *albor* (amanecer) + *-iel* | 4 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Solielo** | *sol* + *cielo* | 3 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Ocileo** | *ocio* + *leo*, forma corta | 4 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Oziello** | *ozio* con grafía italiana | 3 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Alborelio** | *albor* + *-elio* | 4 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Pensilea** | *pensum* + *-ilea* (como Pentesilea) | 4 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Nemoriela** | *nemus* + *-ela* | 4 | ✅ | ✅ | ✅ | 0 | 0 resultados |
| **Idilina** | *idilio* + *-ina* | 4 | ✅ | ✅ | ✅ | 0 | 0 resultados |

Todas las filas tienen `.com`, `.es` y `.app` sin registro, cero apps en App Store (ES/US) y Play
Store, y ninguna mención previa en la web. Eso significa dos cosas: **están libres** y **no
significan nada para nadie**; la marca la construye el producto, no el diccionario.

**Matices por nombre:**

- **Ocioleo** es el único con significado transparente en español: *ocio* + *leo*. Se escribe como
  se pronuncia. Riesgo: parece dos palabras y son cuatro sílabas.
- **Ozielo** se lee casi como "o cielo"; tres sílabas, dulce y corto. Riesgo: existe un
  `@_ozielo` personal en Instagram (no es marca ni producto).
- **Pensiel** es el más corto y el más limpio; evoca *pensar* y *pensil* ("jardín pensil"). Riesgo:
  puede leerse como errata de *pensil*.
- **Nemoriel**, **Alboriel** y **Solielo** comparten la terminación `-iel`, que suena a nombre
  propio/fantasy. Elegante, pero menos "de leer" y más "de videojuego".
- **Ocileo** y **Oziello** son variantes más cortas o italianas, pero con la misma ambigüedad.

**Comparación con las finalistas anteriores:**

| Criterio | Ocioleo | Ozielo | Pensiel | Nemoriel | Ismene | Alciona |
|---|---|---|---|---|---|---|
| Significado para el usuario | **Inmediato** | Sugerente | Ninguno | Ninguno | Ninguno | Ninguno |
| Longitud | 7 | 6 | 7 | 8 | 6 | 7 |
| Facilidad de escritura | Alta | Alta | Alta | Media | Alta | Alta |
| Vínculo con lectura | **Directo** | Indirecto | Indirecto | Indirecto | Cultural | Mitológico |
| Registro limpio | Sí | Sí | Sí | Sí | Sí | Sí |

**Finalistas de la ronda 5:** `Ocioleo`, `Ozielo` y `Pensiel`, con `Nemoriel` como alternativa si
se prefiere un nombre con aire clásico en vez de uno con significado literal.

## 3.5 Ronda 6 — sílabas recombinadas (2026-11-15)

Petición: dejar el significado para después y buscar solo **sonoridad**, con la misma música que
las primeras finalistas. Se extrajeron las sílabas de `Alciona`, `Filomela`, `Aganipe`,
`Calirroe`, `Luscinda`, `Melibea`, `Ismene`, `Kelaino`, `Krissa`, `Ianthe` y `Laelia`, y se
recombinaron con tres reglas: 3–4 sílabas, vocal abierta, terminación en `-a`, `-e` o `-ia`.

**Resultado de 91 combinaciones:** 29 tienen `.es` y `.app` libres; 24 de ellas conservan también
el `.com`. **Ninguna tiene app en App Store (ES/US) ni en Play Store, y ninguna aparece en la web.**
`Calanthe` quedó fuera porque es un género de orquídeas con apps registradas.

| Nombre | Sílabas | Suena a | `.com` | `.es` | `.app` |
|---|---|---|---|---|---|
| **Agalira** | a-ga-li-ra | Aganipe + *lira* | ✅ | ✅ | ✅ |
| **Laelira** | la-e-li-ra | Laelia + *lira* | ✅ | ✅ | ✅ |
| **Ianthia** | ian-thia | Ianthe, más suave | ✅ | ✅ | ✅ |
| **Alcidora** | al-ci-do-ra | Alciona + Dorada | ✅ | ✅ | ✅ |
| **Iselira** | i-se-li-ra | Ismene + *lira* | ✅ | ✅ | ✅ |
| **Kriselia** | kri-se-lia | Krissa + Celia | ✅ | ✅ | ✅ |
| **Filandria** | fi-lan-dria | Filomela + Alejandría | ✅ | ✅ | ✅ |
| **Melindria** | me-lin-dria | Melibea + Melinda | ✅ | ✅ | ✅ |
| **Liriela** | li-rie-la | *Lira* + aire de hada | ✅ | ✅ | ✅ |
| **Agaltea** | a-gal-te-a | Galatea | ✅ | ✅ | ✅ |
| **Tarinea** | ta-ri-ne-a | Mediterráneo | ✅ | ✅ | ✅ |
| **Olindia** | o-lin-dia | Olimpia + India | ✅ | ✅ | ✅ |
| **Isandria** | i-san-dria | Ismene + Alejandría | ✅ | ✅ | ✅ |
| **Iantia** | ian-tia | Ianthe, corto | ✅ | ✅ | ✅ |
| **Tariane** | ta-ria-ne | Ariadna + Tanit | ✅ | ✅ | ✅ |
| **Filadea** | fi-la-de-a | Filomela + idea | ✅ | ✅ | ✅ |
| **Aganora** | a-ga-no-ra | Aganipe + Nora | ✅ | ✅ | ✅ |
| **Melisaria** | me-li-sa-ria | Melibea + rosal | ✅ | ✅ | ✅ |

**El hallazgo de esta ronda: la familia de la *lira*.** Cuatro de los nombres más bonitos
(`Agalira`, `Laelira`, `Iselira`, `Liriela`) contienen *lira*, el instrumento de la poesía. Da un
motivo coherente para el logotipo y para el tono de voz sin depender de un significado
mitológico que el usuario no conoce.

**Orden de preferencia por sonoridad:**

1. **Agalira** — la más fluida de todas; cuatro sílabas que no tropiezan y una *lira* escondida.
2. **Laelira** — hereda la suavidad de Laelia y la alarga sin volverse pesada.
3. **Ianthia** — la versión redonda de Ianthe; tres sílabas y aire clásico inmediato.
4. **Alcidora** — la más "de biblioteca": recia, cálida y fácil de recordar.
5. **Iselira** — delicada, casi susurrada.
6. **Kriselia** — la más limpia y moderna de las seis.
7. **Filandria** — hermosa, pero en inglés evoca *philandry*; solo si el mercado es hispanohablante.
8. **Melindria** — elegante, algo más larga.

**Descartadas de esta ronda:** `Calanthe` (apps), `Meliantha`, `Krisalia`, `Krisandra`,
`Kelaira` (`.com` ocupado aunque `.es`/`.app` libres) y el resto de combinaciones con el `.es` o
el `.app` ya registrados.

**Finalistas de esta ronda:** `Agalira`, `Laelira`, `Ianthia`, `Alcidora`, `Iselira` y `Kriselia`.

## 3.6 Ronda 7 — la familia de Agalira (2026-11-15)

`Agalira` queda como referencia de sonoridad. Se probaron **94 variantes** con su mismo esqueleto
(a-ga-LI-ra: cuatro sílabas abiertas, consonantes líquidas, final en vocal) y **44 conservan
`.com`, `.es` y `.app` libres**, sin apps ni menciones web.

| Grupo | Nombres con los tres dominios libres | Qué cambia |
|---|---|---|
| **Mismo esqueleto** | `Aganira`, `Agasira`, `Agatira`, `Agadira`, `Agalida`, `Agalita`, `Agalora`, `Agalima`* | Una consonante o la vocal final |
| **Mismo final `-lira`** | `Anelira`, `Aselira`, `Abalira`, `Aralira`, `Azalira`, `Amalira`* | El arranque |
| **`lira` con más cuerpo** | `Aurelira`, `Aurilira`, `Calisira`, `Talisira`, `Malisira`, `Camalira`, `Catalira`, `Caralira`, `Naialira` | Dos sílabas antes de la *lira* |
| **Más rotundos (`-dra`, `-dora`)** | `Agalidra`, `Aganidra`, `Agalindra`, `Talidora`, `Malidora`, `Salidora` | Coda consonántica |
| **Variantes de Agalira** | `Agaliria`, `Agaliris`, `Agaliza`, `Agaluria`, `Agalura` | Cola distinta |

\* `Agalima` y `Amalira` tienen el `.com` ocupado; el `.es` y el `.app` están libres.

**Selección por sonoridad, en orden:**

1. **Aganira** — la más cercana a Agalira sin serlo. Además evoca *Aganipe*, la fuente de las
   Musas al pie del Helicón: la misma genealogía que Elicona, sin usar el nombre exacto del mito.
2. **Agalida** — la `d` le da cuerpo; termina más suave y suena a nombre propio.
3. **Agalora** — la más cálida del grupo, con el eco de *aurora*.
4. **Anelira** — cambia el ritmo (a-ne-LI-ra) y gana elegancia; menos infantil que Agalita.
5. **Aurelira** — la más rica, con *aurora* + *lira*; riesgo de leerse como femenino de Aurelio.
6. **Talidora** — rotunda y mediterránea, con *Talía* y *dorada* dentro.
7. **Agaliria** — si Agalira gusta pero se quiere algo con más presencia.

**Descartadas de esta ronda:** `Agalina`, `Agamira`, `Agamora`, `Agalia`, `Adalira`, `Amaralira`,
`Calidra`, `Evalira` y el resto del grupo con `.com` ocupado; `Calanthe` sigue fuera por apps.

## 3.7 Ronda 8 — misma fonología, otras siluetas (2026-11-15)

Petición: conservar el **tipo** de Agalira (vocales abiertas, líquidas, aire clásico) sin repetir
su esqueleto. Se probaron **98 nombres en siete siluetas distintas**: 63 tienen `.es` y `.app`
libres, y **19 conservan además el `.com`**. Todas sin apps (salvo `Lirania`) y sin menciones web.

`*` = también con `.com` libre.

| Silueta | Nombres disponibles | Cómo suena |
|---|---|---|
| **3 sílabas, final `-e`** (ninfa) | `Alciope`*, `Ianthea`*, `Filine`, `Lirane`, `Valine`, `Naiade` | al-CIO-pe · ian-TE-a · fi-LI-ne |
| **3 sílabas, final `-o`** (dios/río) | `Eumelo`*, `Filino`, `Lirano`, `Aletio` | eu-ME-lo · li-RA-no · a-LE-tio |
| **4 sílabas, `-ea`/`-ia`** (corona) | `Melianea`*, `Meliania`*, `Iridiana`, `Filenia` | me-lia-NE-a · me-lia-NI-a |
| **4 sílabas, `-ndra`** (piedra) | `Nelindra`*, `Iriandra`*, `Naiandra`*, `Calindra`, `Mirandra` | ne-LIN-dra · i-RIAN-dra |
| **4 sílabas, `-ora`/`-ira`** (cálido) | `Meliadora`*, `Naiadira`*, `Kalonira`*, `Talonira`*, `Verenisa`*, `Arindela`*, `Olindara`* | me-lia-DO-ra · nai-a-DI-ra |
| **La lira, flexible** | `Lirinda`*, `Liranta`*, `Liriena`, `Lirida` | li-RIN-da · li-RAN-ta |
| **Mitológico con cuerpo** | `Melanipa`* | me-la-NI-pa |

**Selección por sonoridad y variedad:**

1. **Alciope** — la más clásica de todas: tres sílabas con una `p` que le da hueso.
   Suena a nombre de ninfa y no se parece a nada del mercado.
2. **Melianea** — la más luminosa; la `-ea` final la hace distinta de todo lo anterior.
3. **Nelindra** — la más rotunda: la `-ndra` da peso sin perder la vocal abierta.
4. **Eumelo** — la más griega de las cortas; aire de río y de aeda.
5. **Meliadora** — la más cálida y "latina", con *dorada* dentro.
6. **Ianthea** — si Ianthe gustaba pero se quería otra silueta.
7. **Iriandra** — la más singular del grupo `-ndra`; parece un nombre real y no lo es.
8. **Melanipa** — la más mitológica y con más carácter; no suena a invento.

**Matices a tener en cuenta:**

- `Sireno` se descartó por significado: en español es el manatí.
- `Valine` es un aminoácido en inglés; funciona en español, pero ensucia las búsquedas.
- `Naiade` equivale a *náyade*, sustantivo común: bonito, pero descriptivo.
- `Lirania` está ocupado por un juego de estrategia.

## 3.8 Ronda 9 — castellano de paisaje y latín de biblioteca (2026-11-15)

Petición: salir del griego y buscar **castellano y latín**, siempre por combinación de sílabas.
Se probaron **205 nombres** en tres sabores; **147 tienen `.es` y `.app` libres y 60 conservan
también el `.com`**. Todas las seleccionadas están limpias de apps y de menciones web.

### Sabor castellano de paisaje

Prefijos del campo español (`alcor-`, `prad-`, `sot-`, `riber-`, `font-`, `oter-`, `zarc-`,
`almandr-`) con terminaciones `-al`, `-eta`, `-ina`, `-elba`, `-iega`.

| Nombre | Composición | Lectura |
|---|---|---|
| **Cimalva** | *cima* + *alba* | cim-AL-va; cumbre al amanecer, tres sílabas |
| **Fontelba** | *fuente* + *alba* | fon-TEL-ba; la fuente del amanecer |
| **Almandral** | *almendro* + `-al` | al-man-DRAL; almendral, con `d` sonora |
| **Alcoreta** | *alcor* (colina) + `-eta` | al-co-RE-ta; la colina pequeña |
| **Alcorina** | *alcor* + `-ina` | al-co-RI-na |
| **Ribernal** | *ribera* + `-al` | ri-ber-NAL |
| **Marismal** | *marisma* + `-al` | ma-ris-MAL |
| **Alcorzal** | *alcorza* (dulce) + `-al` | al-cor-ZAL |
| **Oterral** | *otero* + `-al` | o-te-RRAL |
| **Zarcal** | *zarza* + `-al` | zar-CAL; dos sílabas, muy castellano |
| **Fresnedal** | *fresno* + `-eda` + `-al` | fres-ne-DAL |
| **Hontanela** | *hontanar* + `-ela` | hon-ta-NE-la |

### Sabor latín de biblioteca

Raíces de libro y papel: *codex*, *charta*, *pergamena*, *nemus*, *litus*, *boscus*.

| Nombre | Composición | Lectura |
|---|---|---|
| **Codicila** | *codex* / *códice* + `-ila` | co-di-CI-la; lleva "códice" dentro |
| **Chartaria** | *charta* (papel) + `-aria` | char-TA-ria; el obrador de papel |
| **Pergamela** | *pergamena* (pergamino) + `-ela` | per-ga-ME-la |
| **Pergamina** | *pergamena* + `-ina` | per-ga-MI-na |
| **Nemorina** | *nemus* (bosque) + `-ina` | ne-mo-RI-na |
| **Litorana** | *litus* (costa) + `-ana` | li-to-RA-na |
| **Boscaria** | *boscus* (bosque) + `-aria` | bos-CA-ria |
| **Melandria** | *mel* (miel) + `-andria` | me-LAN-dria |

### Sabor inventado castellano

Terminación `-iega` (como *labriega*, *mujeriegas*), muy reconocible como español aunque no
exista la palabra.

**Alcoriega** · **Pradiega** · **Sotiega** · **Lomiega** · **Naviega** · **Fontiega** ·
**Riberiega** · **Oteriza** · **Alboresa** · **Saviania** · **Hontanina** · **Almandrina** ·
**Aurelanea**

### Selección

| # | Nombre | Por qué |
|---|---|---|
| 1 | **Cimalva** | La más bonita de toda la ronda: castellana, corta, con imagen de cumbre al alba |
| 2 | **Fontelba** | Fuente y amanecer en una palabra; suena a lugar, no a invento |
| 3 | **Codicila** | La más "de libros" sin decir libro: *códice* escondido |
| 4 | **Almandral** | Castellana y rotunda; el oído la reconoce como paisaje |
| 5 | **Alcoriega** | La más sonora del grupo `-iega`; parece un topónimo real |
| 6 | **Pergamela** | Evoca pergamino y suena a nombre propio |
| 7 | **Alcoreta** | Suave y breve, con `-eta` de diminutivo |
| 8 | **Boscaria** | Bosque + taller; la más "de oficio" |

**Matices:**

- `Hontanela` se parece demasiado a *fontanela* (la mollera del bebé): mejor `Hontanera`.
- `Litorala` y `Ribernala` se oyen forzadas: las dejé fuera de la selección.
- `Zarcal` es preciosa pero tan corta que el `.es` y el `.app` son también más fáciles de ocupar
  en el futuro.

## 3.9 Consulta puntual — Glotium (2026-11-15)

**No está usada.** Es el nombre más libre de todo el proceso: ningún dominio, ninguna app y
ninguna mención web.

| Comprobación | Resultado |
|---|---|
| `glotium.com` | Sin registrar (RDAP 404) |
| `glotium.es` | Sin delegación DNS (`NXDOMAIN`) |
| `glotium.app` | Sin registrar (RDAP 404 y sin DNS) |
| `glotium.net`, `glotium.org`, `glotium.io` | Libres |
| App Store ES/US | Cero coincidencias |
| Play Store | Cero coincidencias |
| Búsqueda web `"glotium"` | Cero resultados |
| Marcas y empresas | Nada con ese nombre; solo suenan parecido `Gootium` (mochilas), `Glowrium` (lámparas) y `Glorium` (consultoría), de otros sectores |

**Lectura del nombre.** Se apoya en `glot-`, la raíz griega de *lengua* (glotis, glosa, glosario),
y en la terminación `-ium`, que suena a latín culto. Permite una historia honesta: *lengua + ocio*,
y engancha con el glosario que ya existe en el producto.

**Dos reparos, y el segundo es serio:**

1. **Suena a ciencia.** La terminación `-ium` es la de los elementos químicos y los fármacos
   (ilium, gadolinium). Un lector nuevo puede pensar en un suplemento antes que en un libro.
2. **Contiene `Otium`.** Cuatro de sus seis letras son el nombre que se está abandonando. Eso da
   continuidad, pero también arrastra el problema: la solicitud USPTO `OTIUM` (serial 50156419,
   clase 041) y los competidores `otium-library.com`, `otiumbookshelf.com` y `otiumfeed.it` siguen
   ahí. En marcas, añadir un prefijo a un signo anterior para servicios idénticos puede bastar
   para que se aprecie riesgo de confusión. No es descartable, pero **es justo lo que tiene que
   resolver R-90**, y no conviene comprar dominios antes de saberlo.

**Variantes con la misma raíz, si se quiere conservar el sonido y soltar el `otium`:**
`Glotela`, `Glotira`, `Glotane`, `Glotina` y `Glotiuma` tienen los tres dominios libres;
`Glotia` y `Glotio` tienen el `.com` ocupado.

### Límites de esta verificación
- `RDAP 404` en `.com` acredita que **no está registrado**; no confirma el precio (puede ser
  *premium*). Se comprueba al añadirlo al carrito del registrador.
- En `.es` no hay RDAP público: se usa la ausencia de delegación DNS. Un dominio registrado
  siempre tiene `NS`, así que `NXDOMAIN` es un indicio fuerte, no una reserva.
- Handles de redes sociales, denominaciones sociales y marcas (OEPM, EUIPO, USPTO) siguen siendo
  comprobación **manual**: es R-90. Ninguna tabla de este documento sustituye un dictamen.

## 3.10 Decisión adoptada — Olvium (2026-11-15)

**Nombre seleccionado para llevar a clearance formal (R-90).** Se elige por mantener la letra
inicial `O` (sin cambiar el logotipo ya diseñado), por su sonoridad corta y limpia (tres sílabas:
ol-vi-um), y por estar libre de apps y menciones web.

### Evidencia de disponibilidad

| Comprobación | Método | Resultado |
|---|---|---|
| `olvium.com` | RDAP Verisign `.com` | Ocupado (terceros) |
| `olvium.es` | DNS `NS` via dns.google | Libre (`NXDOMAIN`) |
| `olvium.app` | DNS `NS` via dns.google | Libre (`NXDOMAIN`) |
| `olvium.xyz`, `olvium.online`, `olvium.co`, `olvium.io` | DNS `NS` / RDAP | Libres |
| `olviumbooks`, `olviumreads`, `olviumlibro`, `olviumlectura`, `olviumlector`, `olviumapp` | RDAP/DNS en `.com`, `.es`, `.app`, `.xyz`, `.online`, `.co`, `.io` | Libres en todos los TLDs verificados |
| App Store iOS (ES/US) | iTunes Search API `entity=software` | Ninguna coincidencia |
| Google Play | Scraping HTML `play.google.com/store/search` | Ninguna coincidencia |
| Búsqueda web `"olvium"` | DuckDuckGo HTML | Sin resultados |
| Búsqueda web `"olvium" + libros/lectura/app` | DuckDuckGo HTML | Sin resultados |
| Redes sociales `"olvium" + instagram/facebook/twitter/tiktok/linkedin` | DuckDuckGo HTML | Sin resultados |

### Reparos documentados

1. **`.com` ocupado.** Está registrado por terceros. Si se necesita obligatoriamente, hay que
   comprarlo o usar un sufijo (`olviumbooks.com`, `olviumreads.com`, etc., todos verificados
   libres).
2. **Semejanza fonética con `Otium`.** `Olvium` (ol-vi-um) es muy cercano a `Otium` (o-ti-um).
   Como `Otium` se descarta por colisión de nicho (USPTO serial 50156419, clase 041;
   `otium-library.com`, `otiumbookshelf.com`, `otiumfeed.it`), `Olvium` arrastra el mismo
   riesgo: añadir una letra a un signo anterior para servicios idénticos puede apreciarse como
   riesgo de confusión. **Es R-90 quien debe resolverlo.**
3. **Terminación `-ium`.** Suena a elemento químico o fármaco; un lector nuevo puede pensar en
   un suplemento antes que en un libro.

### Límites de esta verificación

- `RDAP 404` en `.com` acredita que **no está registrado**; no confirma el precio.
- En `.es` no hay RDAP público: se usa la ausencia de delegación DNS. `NXDOMAIN` es indicio
  fuerte, no reserva.
- Handles de redes sociales, denominaciones sociales y marcas (OEPM, EUIPO, USPTO) siguen
  siendo comprobación **manual**: es R-90. Ninguna tabla de este documento sustituye un
  dictamen.

## 4. Candidata principal — Elicona

> Se analiza aquí por ser la mejor situada de la lista corta, **no porque esté elegida**. La
> comparación con las demás finalistas está en §3.1, §3.2 y §3.4 a §3.6.

**Qué evoca.** El monte Helicón, en Beocia, morada de las Musas y de la fuente Hipocrene: el
lugar donde nace la inspiración poética. Es palabra patrimonial en español e italiano, suena
clásica y no significa "libro": no encasilla el producto si mañana crece hacia audiolibros,
clubes o librería.

**Por qué funciona.** Tres sílabas, sin tildes, sin ambigüedad fonética en español; se escribe
igual en español, italiano e inglés; no compite con un término genérico del sector.

### 4.1 Evidencia recogida (2026-11-15)

| Comprobación | Método | Resultado |
|---|---|---|
| Apps iOS España | iTunes Search API, `country=es`, `entity=software` | Sin resultados |
| Apps iOS EE. UU. | iTunes Search API, `country=us` | Sin resultados (solo apps con nombres parecidos, de otro sector) |
| Google Play | Búsqueda pública `elicona` | Sin app de lectura con ese nombre |
| `elicona.com` | DNS + contenido | Sala de arte y música japonesa (`ART SPACE ELICONA`); sector distinto |
| `elicona.es` | DNS `NS` | `NXDOMAIN` |
| `elicona.app` | DNS `NS` | `NXDOMAIN` |
| `elicona.com.es`, `getelicona.com`, `eliconareader.com`, `elicona.io`, `elicona.page`, `elicona.club`, `elicona.studio` | DNS `NS` | `NXDOMAIN` |
| Búsqueda web del nicho | `"elicona" + app/lectura/libros/ebook` | Sin resultados en el nicho; solo referencias mitológicas y bibliográficas |

`NXDOMAIN` indica ausencia de delegación DNS: el dominio está libre o aparcado sin resolver.
**No equivale a disponible para registro**: hay que confirmarlo en el registrador y comprarlo.

### 4.2 Lo que la evidencia **no** cubre

- Registro de marca en OEPM, EUIPO o USPTO (clases 9, 41, 42). Es R-90.
- Las bases de marcas consultadas por script (`trademarks.justia.com`, `uspto.report`,
  `tmsearch.uspto.gov`) responden `403` a peticiones automáticas, así que **la búsqueda formal
  es manual**. La ausencia de resultados aquí no demuestra nada sobre el registro.
- Denominaciones sociales y nombres comerciales en el registro mercantil.
- Uso no registrado en otros sectores que pudiera oponerse por notoriedad.
- Disponibilidad real de handles en redes sociales (se comprueba al reservarlos, R-92).

---

## 5. Acciones pendientes

| # | Acción | Responsable | Estado |
|---|---|---|---|
| 1 | Cerrar la elección del nombre entre las finalistas (§3.1 a §3.10) | Propietario | Seleccionado: Olvium (pendiente R-90) |
| 2 | Clearance formal OEPM/EUIPO/USPTO en clases 9, 41 y 42 (R-90) | Propietario / asesor | Pendiente |
| 3 | Comprar `<nombre>.es` y `<nombre>.app` (y el `.com` defensivo si es trivial) | Propietario | Bloqueada por 1 |
| 4 | Reservar handles `@<nombre>` (R-92) | Propietario | Bloqueada por 1 |
| 5 | Logotipo, paleta y set de iconos PWA (R-94) | Diseño | Bloqueada por 1 |
| 6 | Solicitud de marca OEPM, clases 9 y 42 (R-95) | Propietario | Bloqueada por 1 |
| 7 | Renombrar solución, ensamblados, textos y configuración (R-93) | Desarrollo | Bloqueada por 1 |

**Nada de esto se ejecuta hasta que R-90 cierre el nombre.** Si la clearance de la primera
finalista no es favorable, se pasa a la siguiente antes de gastar en dominio, logo o renombrado:
renombrar después de crear el esquema PostgreSQL duplica el trabajo.

---

## 6. Reglas de uso del nombre (para el código y la documentación)

- **Mientras el nombre no esté cerrado:** el producto se llama `Otium` en código y en carpeta. No
  se crean dominios, correos, handles, paquetes ni variables con ninguna de las finalistas, y no
  se renombra nada.
- **Tras R-90:** el nombre visible y los identificadores técnicos usan el nombre elegido, sin
  mezclar candidatas en la UI ni en la configuración.
- En textos legales: el nombre comercial es el elegido; la titularidad y el NIF van en el aviso
  legal, nunca en el repositorio.
- El dominio antiguo, si existe, se mantiene con `301` durante 12 meses tras el lanzamiento.
