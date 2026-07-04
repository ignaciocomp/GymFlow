# CU-09: Notificaciones in-system del Socio

| *Campo* | |
|-|-|
| *Nombre* | Inbox de notificaciones del socio dentro del sistema |
| *Actor principal* | Socio |
| *Precondición* | Socio autenticado en el portal. |
| *Postcondición* | El socio ve sus notificaciones con tipo, título, mensaje y fecha. Puede marcar como leídas. El badge de la campanita refleja el conteo de no-leídas. |
| *RF cubiertos* | RF-16 |
| *Iteración(es) de entrega* | IT-5 — Centro de notificaciones dentro de la app para el socio (adelantado de IT-6). Campanita con contador de no leídas + inbox en el portal. El guardado de cada notificación se hace en transacción propia: si falla, la operación de negocio que la disparó queda confirmada igual. |
| *Referencia original* | Funcionalidad documentada en [iteracion-5.md § CU-09](../seguimiento/iteracion-5.md). No figura como CU detallado en el documento original de requerimientos. |
| *Referencia spec* | [spec-rf16-notificaciones](../specs/spec-rf16-notificaciones.md) |
| *Referencia plan* | [plan-rf16-notificaciones](../plans/plan-rf16-notificaciones.md) |
| *Documento de iteración* | [iteracion-5.md](../seguimiento/iteracion-5.md) |

**Comportamiento general:**

- Cada vez que ocurre uno de los eventos de negocio listados abajo, **después** de que la operación principal se haya guardado, el sistema crea la notificación in-system correspondiente.
- El guardado de la notificación se hace en su propia transacción: si falla, la operación de negocio que la disparó (por ejemplo, una inscripción) **queda confirmada igual**.

**Tipos de notificaciones y disparadores:**

| Disparador | Tipo de notificación | Destinatario |
|---|---|---|
| Recordatorio diario de cuota (job automático) y notificación manual de cuota | Recordatorio de cuota | Socio de la cuota |
| Cambio de horario de una clase | Cambio de horario | Socios inscriptos en ese horario |
| Cancelación de una clase | Cancelación de clase | Socios inscriptos en los horarios de la clase |
| Inscripción a una clase | Confirmación de inscripción | Socio inscripto |
| Creación de un evento (RF-15) | Evento nuevo | Socios activos de la sede |

**Flujo principal — Consulta del inbox:**

1. El socio accede al portal.
2. La campanita en el header muestra el contador de no-leídas (consulta periódica a `GET /api/portal/notificaciones/no-leidas/count`).
3. El socio hace clic en la campanita o accede al inbox.
4. El sistema devuelve las notificaciones del socio (`GET /api/portal/notificaciones?soloNoLeidas=&take=`) con tipo, título, mensaje y fecha.

**Flujo alternativo — Marcar como leída:**

1. El socio hace clic en una notificación no leída.
2. El sistema invoca `POST /api/portal/notificaciones/{id}/leer`.
3. El backend **verifica que la notificación pertenezca al socio autenticado** y la marca como leída.
4. La operación es **idempotente**: marcar dos veces no cambia la fecha original de lectura.

**Flujos de excepción:**

- **E1 — Notificación de otro socio:** Bloqueado por verificación de pertenencia; respuesta de error.
- **E2 — Falla al guardar la notificación:** Como el guardado ocurre en su propia transacción, **no rompe la operación de negocio** que la disparó. La notificación simplemente no se crea (queda registro en logs).

**Reglas de negocio aplicables:**

- **Transacción independiente:** la notificación se guarda en una transacción propia, separada de la operación de negocio. Esto evita que un fallo en la creación de la notificación afecte el flujo principal (p. ej. una inscripción).
- **Verificación de pertenencia obligatoria** al marcar como leída — previene IDOR.
- **Idempotencia** de "marcar como leída": la fecha de lectura no se sobrescribe.
- **Listado acotado:** el parámetro `take` de paginación queda limitado a un rango razonable (1 a 100).
- **Polling periódico del contador** desde el portal para mantener el badge actualizado.

**Desviaciones respecto del diseño original:**

- **Adelantado desde IT-6 a IT-5.** No figura en el documento original como CU detallado; se construyó como soporte transversal a los demás CUs (cuotas, clases, eventos).
