# Monetización

- **Estado:** objetivo (Fase 3). No implementado.
- **Última revisión:** 2026-11-15
- **Tareas:** R-40 a R-47 en [`../ROADMAP.md`](../ROADMAP.md)

---

## 1. Principio

**Se cobra por capacidad, no por funciones.** Las funciones de lectura son idénticas en todos los
planes; cambian los libros, los megas y la cuota de IA. Un usuario que no paga puede usar el
producto de verdad, y eso es lo que convierte.

## 2. Planes

| Plan | Libros | Almacenamiento | IA/día | Catálogo |
|---|---|---|---|---|
| Gratis | 5 | 100 MB | 10 definiciones | Básico |
| Lector | 200 | 5 GB | 100 definiciones | Completo + descargas |

- Los límites viven en **una sola fuente de verdad** (`IPlanService`) y se aplican en servicios,
  nunca en la UI.
- Un usuario sin suscripción cae siempre en Gratis, también si el cobro falla.
- Cambiar de plan surte efecto sin reiniciar sesión.

## 3. Modelo

```text
Plan        (Codigo, Nombre, PrecioMensual, PrecioAnual, LimiteLibros, LimiteBytes, LimiteIA)
Suscripcion (UsuarioId, PlanCodigo, Estado, PeriodoInicio, PeriodoFin, Proveedor, IdExterno)
UsoMensual  (UsuarioId, Mes, DefinicionesIA, Importaciones, MinutosLeidos)
EventosWebhook (Id, Proveedor, IdEventoExterno, Tipo, RecibidoUtc, ProcesadoUtc)
```

- `UsoMensual` se reinicia por mes natural en la zona del usuario, no en UTC.
- `EventosWebhook` garantiza idempotencia: un evento repetido no duplica la suscripción.

## 4. Cobro

- **Paddle** como *Merchant of Record*: gestiona IVA de la UE, facturación y *chargebacks*.
  Alternativa evaluada: Stripe + gestión fiscal propia (más trabajo, más riesgo).
- Checkout alojado y portal del cliente; el producto nunca toca datos de tarjeta.
- Webhooks con verificación de firma; estados `trial`, `active`, `past_due`, `canceled`.
- Periodo de gracia de 3 días antes de degradar; **nunca** se borran datos al impagar.

## 5. Prueba y degradación

| Situación | Qué puede hacer el usuario |
|---|---|
| Prueba activa | Todo |
| Prueba expirada | Leer y exportar; no subir ni importar |
| Impago (`past_due`) | Leer; aviso visible y enlace a facturación |
| Cancelado | Leer; subir solo hasta el límite de Gratis |

Aviso 3 días antes del primer cobro y antes de cada renovación anual.

## 6. Reglas de cuota

- Nunca consumir cuota **antes** de una operación que puede fallar; si falla la llamada de IA, se
  devuelve la cuota (F-01).
- La cuota de IA del glosario y la de mapas de personajes son **cubetas separadas**.
- Caché por palabra y por `Sha256` del libro: dos usuarios con el mismo texto no pagan dos veces.
- Mensaje de límite tipado (`LimiteAlcanzado`) con enlace a `/planes`, en español y sin culpar al
  usuario.

## 7. Precio

- Precio mensual y anual (dos meses gratis en anual), sin plan de por vida en v1.
- El precio se decide con datos de M1.5 y M4, no antes: se prueba en la página de planes.
- Referencia de mercado: los *trackers* de lectura cobran 4–5 €/mes; este producto incluye lector,
  por lo que puede sostener un precio similar o superior si la retención acompaña.

## 8. Legal y fiscal

- ToS, privacidad con encargados (Z.ai, Paddle, Brevo, Hetzner, Backblaze) y DPA firmados.
- Derecho de desistimiento de 14 días para contenido digital, con renuncia expresa al acceso
  inmediato si se quiere cobrar desde el minuto uno.
- Cookies: solo esenciales; sin banner si no hay analítica de terceros.
- Alta fiscal para facturar: **bloqueante administrativo**, no técnico.
- Exportación y borrado ya existen (P1-4) y deben incluir las tablas nuevas.

## 9. Antifraude

- Verificación de correo antes de pagar (hoy el registro no la exige).
- Límite de cuentas por IP/dispositivo en el registro y en la prueba gratuita.
- Cuota de IA por cuenta **y** por IP.
- Revisión manual de los primeros casos; no se automatiza la expulsión sin evidencia.

## 10. Play Store

La app **no vende**: solo autentica y lee. La compra se hace en la web. Revisar el programa
*reader app* de Google si se quiere enlazar el checkout dentro de la app; documentar la decisión y
la fecha de revisión de la política.

## 11. Métricas de cobro

| Métrica | Uso |
|---|---|
| Conversión registro → pago | Validar límites y precio |
| *Churn* mensual | Detectar degradación de valor |
| Ingreso medio por usuario | Cubrir coste de IA y almacenamiento |
| Coste de IA por usuario activo | Ajustar cuotas antes de que descuadre |
