# Producto — Otium (nombre comercial en evaluación)

- **Estado:** vigente
- **Última revisión:** 2026-11-15
- **Plan asociado:** [`../ROADMAP.md`](../ROADMAP.md) · **Arquitectura y convenciones:** [`../AGENTS.md`](../AGENTS.md)

---

## 1. Qué es

Un diario de lectura con biblioteca propia **y lector integrado**. El usuario trae sus EPUB sin
DRM, añade obras de dominio público desde un catálogo y lee dentro de la app, con progreso
sincronizado, glosario asistido y travesías de lectura.

Se diferencia de un *tracker* por tres cosas: **se lee dentro**, el progreso es **del servidor**
(no del navegador) y el conocimiento del texto permite funciones que un tracker no puede ofrecer
(glosario en contexto, citas con ubicación, mapas de personajes sin spoilers, clubes por capítulo).

## 2. Modelo de contenido

| Fuente | Qué es | Regla |
|---|---|---|
| **BYOF** | El usuario sube su EPUB/PDF sin DRM | El archivo es privado, nunca se comparte ni se distribuye |
| **Catálogo público** | Obras en dominio público, principalmente Standard Ebooks | Se sirve desde caché propia, con licencia y atribución por obra |

**DRM:** no se soporta. Un archivo cifrado se rechaza con un mensaje claro. No hay intención de
negociar licencias de DRM en v1.

## 3. Alcance de la v1

### Entra

- Biblioteca, estantes, puntuaciones, reseñas y travesías (ya existe).
- Lector EPUB con capítulos sanitizados, progreso por posición y preferencias de lectura.
- Catálogo de dominio público con búsqueda local y descarga bajo demanda.
- Glosario por selección y glosario manual, con cuotas.
- Amigos, compartir estantes opcional, bloquear.
- PWA instalable + TWA publicada en Play Store.
- Cobro por web (no en la app), con planes por capacidad.
- Hábito: rachas con congelación, recap anual, demo sin registro, importación de Goodreads.

### No entra

| Fuera | Motivo |
|---|---|
| Lectura sin conexión | Blazor Server necesita conexión; se reevalúa con cliente propio |
| App iOS | Coste de mantenimiento; la web cubre el caso de uso |
| Audiolibros | Otro producto, otra cadena de valor |
| DRM y EPUB de maquetación fija | Licencias de terceros y complejidad de render |
| Compartir archivos entre usuarios | Riesgo de copyright y de responsabilidad |
| Escalado horizontal | Blazor Server con estado por circuito; 100 usuarios no lo exigen |
| Reconocimiento de lomos por foto | Coste alto, valor marginal frente a la importación CSV |

## 4. Escala y operación

- **≤ 100 usuarios registrados**, 10–25 circuitos concurrentes.
- Una sola instancia, un solo VPS, sin backplane.
- Si el producto crece por encima, se migra el cliente antes que escalar el servidor.

## 5. Modelo de negocio

**Se cobra por capacidad, no por funciones.** Las funciones de lectura son las mismas en todos
los planes; lo que cambia es cuánto cabe y cuánta IA se consume. Así el mensaje es simple y el
coste variable (IA, almacenamiento) queda cubierto.

| Plan | Biblioteca | Almacenamiento | IA | Catálogo |
|---|---|---|---|---|
| Gratis | 5 libros | 100 MB | 10 definiciones/día | Básico |
| Lector | 200 libros | 5 GB | 100 definiciones/día | Completo |

- **Prueba:** 14 días; al expirar, degradación suave a Gratis (se lee, no se sube; nunca se borra).
- **Cobro:** Paddle como *Merchant of Record* (IVA UE y facturación resueltos). Checkout en la
  web; la app no vende.
- **Sin precio de por vida en v1:** el coste de IA es variable y una tarifa plana eterna lo
  convierte en pasivo.
- **Requisito administrativo:** alta fiscal para poder facturar. Es bloqueante de negocio, no de
  código.

## 6. Retención y diferenciación

| Capa | Qué aporta | Fase |
|---|---|---|
| Lector | "Puedo leer aquí" | 1 |
| Hábito | Rachas, recap, demo, importación: "vuelvo mañana" | 1.5 |
| Catálogo | "Encuentro qué leer sin salir" | 2 |
| Cobro | "Pago porque me cabe más" | 3 |
| Social | Clubes y mapas de personajes: "no lo hace nadie más" | 5 |

Sin la capa de hábito, la página de precios no convierte. Sin la capa social, el producto compite
solo por precio con trackers gratuitos.

## 7. Métricas que deciden

| Métrica | Umbral para seguir | Si no se cumple |
|---|---|---|
| Activación: primer capítulo leído en 24 h | ≥ 40 % de registros | Revisar onboarding y demo (R-27) |
| Retorno a 7 días | ≥ 25 % | Reforzar rachas y recap (R-23, R-26) |
| Conversión a Lector | ≥ 3 % de registros | Revisar límites y precio (R-40, R-45) |
| Coste de IA por usuario activo | ≤ 0,50 €/mes | Ajustar cuotas y caché (R-41) |

Sin estas cifras, cualquier plan de crecimiento es intuición. Se miden a partir de M1.5.

## 8. Riesgos de producto (no técnicos)

- **Promesa incumplida:** vender "tu biblioteca para siempre" y desaparecer. Mitigación:
  exportación completa ya existente y política de salida publicada.
- **Copyright por archivos subidos.** Mitigación: almacenamiento privado, sin compartición,
  proceso de retirada y registro de subidas.
- **Competencia gratuita.** Mitigación: lectura integrada y funciones que requieren el texto.
- **Dependencia de una persona.** Mitigación: documentación operativa y runbooks antes de M2.
