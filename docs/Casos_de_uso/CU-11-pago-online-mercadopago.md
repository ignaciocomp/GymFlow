# CU-11: Pago de Cuotas Online con Mercado Pago



| *Campo* | |
|-|-|
| *Nombre* | Pago online de cuota del socio vía Mercado Pago (Checkout Pro) |
| *Actor principal* | Socio (paga su propia cuota). Actor secundario: Mercado Pago (pasarela) — Sistema (procesa el webhook). |
| *Precondición* | Socio autenticado en el portal. Tiene al menos una cuota en estado "Próxima a vencer" o "Vencida". Existe configuración válida de credenciales de Mercado Pago. |
| *Postcondición* | Si el pago se aprueba y el webhook es auténtico: la cuota pasa a "Pagada", se registra `FechaPago`, se audita la transacción y se envía email de confirmación al socio. Si el pago es rechazado o queda pendiente: la cuota **no** cambia de estado y el socio conserva el acceso al portal. |
| *RF cubiertos* | RF-21 (Pago de cuotas online con Mercado Pago) |
| *Iteración de entrega* | IT-6 (planificada) — [GymFlow_Requerimientos_Completos.md § Iteración 6](../GymFlow_Requerimientos_Completos.md) |
| *Referencia original* | [GymFlow_Requerimientos_Completos.md — RF-21](../GymFlow_Requerimientos_Completos.md) |
| *Specs / planes* | [spec-email-confirmacion-pago](../specs/spec-email-confirmacion-pago.md) · [spec-rf21-pago-online](../specs/spec-rf21-pago-online.md) (stub con enfoque de tooling) · *(a generar)* [[plan-rf21-pago-online]] |
| *CU relacionado* | [CU-03 — Gestión de Cuotas](CU-03-cuotas-recordatorios.md) (habilita el botón "Pagar" hoy deshabilitado) |

**Flujo principal — Pago de cuota desde el portal:**

1. El socio accede a "Mis Cuotas" (ver [CU-03](CU-03-cuotas-recordatorios.md)) en el portal.
2. Sobre una cuota en estado "Próxima a vencer" o "Vencida", hace clic en **"Pagar"** (botón habilitado por este CU).
3. El sistema determina el **monto a cobrar a partir del plan activo del socio** (RN-32); el socio no puede modificarlo.
4. El backend crea una preferencia de pago en Mercado Pago (Checkout Pro) asociada a esa cuota (referencia externa = id de cuota) y redirige al socio al checkout de Mercado Pago.
5. El socio completa el pago en la pasarela de Mercado Pago.
6. Mercado Pago notifica el resultado al **endpoint de webhook** del sistema.
7. El sistema **valida la autenticidad del webhook (firma HMAC)** antes de procesarlo (RN-31). Si la firma no es válida, descarta la notificación.
8. Si el pago está **aprobado**, el sistema marca la cuota como **"Pagada"**, setea `FechaPago` y registra en auditoría la transacción (usuario, timestamp, monto, número de transacción MP y estado resultante — RN-33).
9. El sistema envía al socio un **email de confirmación de pago** (ver [spec-email-confirmacion-pago](../specs/spec-email-confirmacion-pago.md)).
10. El socio es redirigido a una página de resultado en el portal y ve su cuota actualizada a "Pagada".

**Flujo alternativo — Pago rechazado:**

1. Mercado Pago informa el pago como rechazado.
2. El sistema valida el webhook y registra el intento, pero **no modifica el estado de la cuota** ni bloquea el acceso del socio (RN-34).
3. El socio ve un mensaje de "pago rechazado" y puede reintentar.

**Flujo alternativo — Pago pendiente:**

1. Mercado Pago informa el pago como pendiente (ej. medio de pago en efectivo/transferencia).
2. El sistema valida el webhook y deja la cuota en su estado actual (RN-34); no la marca como pagada hasta recibir la aprobación.
3. Cuando Mercado Pago confirma la aprobación posteriormente, se ejecuta el flujo principal desde el paso 8.

**Flujos de excepción:**

- **E1 — Cuota no pagable:** Si la cuota no está "Próxima a vencer" ni "Vencida" (p. ej. ya pagada, anulada, o futura), el sistema no inicia el pago (RN-30 — no se generan pagos anticipados).
- **E2 — Webhook con firma inválida:** Se descarta sin alterar el estado de la cuota (RN-31).
- **E3 — Webhook duplicado / reintento de Mercado Pago:** El procesamiento es idempotente (referencia = id de cuota + id de pago); no genera doble pago ni doble email.
- **E4 — Falla en el envío del email de confirmación:** No revierte el pago; la cuota queda "Pagada" y el fallo del email queda registrado (envío best-effort).
- **E5 — Socio abandona el checkout:** No se crea pago; la cuota permanece sin cambios.

**Reglas de negocio aplicables:**

- **RN-30:** Solo se puede pagar una cuota cuyo estado sea "Próxima a vencer" o "Vencida". No se generan pagos anticipados de cuotas futuras.
- **RN-31:** El sistema nunca actualiza el estado de cuota sin haber validado la autenticidad del webhook de Mercado Pago (firma HMAC).
- **RN-32:** El monto del pago lo determina el sistema según el plan activo del socio. El socio no puede modificarlo.
- **RN-33:** Cada pago queda registrado en auditoría con: usuario, timestamp, monto, número de transacción MP y estado resultante de la cuota.
- **RN-34:** Ante pago rechazado o pendiente, no se modifica el estado de la cuota ni se bloquea el acceso del socio al portal.

**Criterios de aceptación:**

- **CA-01:** El socio puede iniciar el pago de una cuota "Próxima a vencer" o "Vencida" desde el portal; las demás no ofrecen pago.
- **CA-02:** El monto cobrado coincide con el precio del plan activo del socio y no es editable por el socio.
- **CA-03:** La cuota pasa a "Pagada" únicamente tras un webhook aprobado y con firma HMAC válida.
- **CA-04:** Tras un pago aprobado, el socio recibe email de confirmación y ve la cuota actualizada.
- **CA-05:** Un webhook con firma inválida o un pago rechazado/pendiente no cambian el estado de la cuota.
- **CA-06:** Cada pago aprobado queda en el log de auditoría con los datos de RN-33.

**Desviaciones respecto del diseño original:**

- En [CU-03](CU-03-cuotas-recordatorios.md) el botón "Pagar" quedó deshabilitado ("Próximamente"); este CU lo habilita.
- **Producto Mercado Pago: Checkout Pro** (decisión cerrada). Se descartan Checkout API/Bricks y Suscripciones para RF-21. Ver [spec-rf21-pago-online](../specs/spec-rf21-pago-online.md).
