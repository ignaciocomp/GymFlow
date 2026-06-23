# CU-03: Gestión de Cuotas y Recordatorios Automáticos

| *Campo* | |
|-|-|
| *Nombre* | Gestión de Cuotas y Recordatorios Automáticos |
| *Actor principal* | Sistema (proceso automático) / Administrador / Socio |
| *Precondición* | Para generación automática: existe `BackgroundService` configurado y socios activos con plan asignado. Para gestión: admin autenticado con permisos del módulo Cuotas. Para vista del socio: socio autenticado. |
| *Postcondición* | Cuotas generadas automáticamente cada 30 días. Recordatorios enviados según calendario. El admin puede registrar pagos manualmente, anular cuotas, revertir pagos y reactivar cuotas anuladas. Todas las acciones quedan en auditoría. |
| *RF cubiertos* | RF-06 (recordatorios automáticos), RF-07 (control de estado de cuota) |
| *Iteración(es) de entrega* | IT-2 — Generación automática de cuotas + vista admin/socio + recordatorios por email |
| *Referencia original* | [GymFlow_Requerimientos_Completos.md § CU-03](../GymFlow_Requerimientos_Completos.md#cu-03--gestión-de-cuotas-y-recordatorios-automáticos) |
| *Referencia spec* | [spec-rf06-recordatorios-cuota](../specs/spec-rf06-recordatorios-cuota.md), [spec-rf07-gestion-cuotas](../specs/spec-rf07-gestion-cuotas.md) |
| *Referencia plan* | [plan-rf07-gestion-cuotas](../plans/plan-rf07-gestion-cuotas.md) |
| *Documento de iteración* | [Documentacion_It.2.docx](../seguimiento/Documentacion_It.2.docx) |

**Flujo principal — Generación automática de cuotas (RF-07):**

1. Un `BackgroundService` se ejecuta una vez al día (hora configurable en `appsettings.json`, ej. `03:00` UTC).
2. Por cada socio activo con plan asignado, el sistema verifica si la última cuota ya venció (o si no tiene cuotas).
3. Si corresponde, genera una nueva cuota pendiente con: socio, unidad, plan vigente (snapshot), monto (precio actual del plan), fecha de emisión, fecha de vencimiento (`FechaEmision + 30 días`), estado `Pendiente`.
4. Si el socio pertenece a 2 unidades, se generan 2 cuotas separadas (una por unidad).
5. La primera cuota de un socio se genera al momento del alta del socio con plan asignado (vencimiento = `FechaAlta + 30 días`).

**Flujo principal — Recordatorios automáticos (RF-06):**

1. Diariamente, un job evalúa las fechas de vencimiento de todas las cuotas pendientes de socios activos.
2. Envía recordatorios por email según calendario:
   - **5 días antes:** email informativo ("Tu cuota vence pronto").
   - **1 día antes:** email urgente ("Tu cuota vence mañana").
   - **Día del vencimiento:** email de aviso ("Tu cuota venció hoy").
3. Cada envío queda registrado en la entidad `RecordatorioCuota` con timestamp, tipo (`CincoDias` / `UnDia` / `DiaVencimiento` / `Manual`), resultado (exitoso / con error) y mensaje de error si falló.
4. **No se envía más de un recordatorio del mismo tipo por socio por día** (validación contra `RecordatorioCuota`).
5. (IT-5) Por cada recordatorio enviado se crea también una notificación in-system "Recordatorio de cuota".

**Flujo alternativo — Notificación manual del admin:**

1. El admin accede a la vista de gestión de cuotas y busca por cédula del socio.
2. Sobre una cuota pendiente, hace clic en "Notificar".
3. El sistema envía un email al socio con nombre, plan, unidad, monto y fecha de vencimiento.
4. Si el socio no tiene correo registrado, se muestra error al admin.
5. No se puede reenviar la misma notificación manual al mismo socio por la misma cuota más de una vez por día.

**Flujo alternativo — Marcar cuota como pagada (admin):**

1. El admin busca al socio por documento (`GET /api/cuotas/admin?documentoIdentidad=...` — documento obligatorio para forzar contexto).
2. Sobre una cuota `Pendiente`, hace clic en "Marcar como pagada" → confirma.
3. El sistema valida que no esté ya pagada ni anulada (`Cuota.MarcarComoPagada()`).
4. Cambia el estado a `Pagada` y setea `FechaPago = UtcNow`.
5. Registra en auditoría.

**Flujo alternativo — Revertir pago:**

1. Sobre una cuota `Pagada`, el admin hace clic en "Revertir pago" → confirma.
2. La cuota vuelve a `Pendiente` y se limpia `FechaPago`.
3. Queda registrado en auditoría.

**Flujo alternativo — Anular cuota:**

1. Sobre una cuota `Pendiente`, el admin hace clic en "Anular" → confirma.
2. Soft delete: la cuota queda con `FechaBaja = UtcNow` y estado `Anulada`.
3. No se puede anular una cuota ya pagada.

**Flujo alternativo — Reactivar cuota anulada:**

1. El admin filtra por estado `Anulada` y elige "Reactivar".
2. La cuota vuelve a `Pendiente` y se limpia `FechaBaja`. Ambas acciones (anulación + reversión) quedan en historial.

**Flujo alternativo — Vista del socio "Mis Cuotas":**

1. El socio accede a `/portal/mis-cuotas`.
2. Ve sus cuotas con plan, unidad, monto, vencimiento, estado (badge de color) y botón "Pagar" deshabilitado (con tooltip "Próximamente").
3. El endpoint `GET /api/cuotas/mis-cuotas` resuelve el `socioId` desde el JWT (el backend nunca expone IDs de otros socios — previene IDOR).

**Estados de la cuota:**

- **Pendiente al día:** badge gris/celeste, vencimiento futuro.
- **Pendiente vencida:** badge rojo, `FechaVencimiento < hoy`.
- **Pagada:** badge verde.
- **Anulada:** badge gris claro (soft-deleted).

**Flujos de excepción:**

- **E1 — Cuota ya pagada al marcar como pagada:** Error de validación; la entidad bloquea el cambio de estado.
- **E2 — Anular cuota ya pagada:** No permitido.
- **E3 — Socio sin correo al notificar:** Mensaje de error al admin; no se envía email.
- **E4 — Falla SMTP en recordatorio automático:** El `RecordatorioCuota` queda registrado con `Exitoso = false` y el mensaje de error. No bloquea la ejecución del resto.

**Reglas de negocio aplicables:**

- **Cuota por unidad:** un socio con 2 unidades genera 2 cuotas separadas por período, cada una con el monto del plan correspondiente.
- **Snapshot de precio:** el monto se congela al momento de generar la cuota (`Monto = Plan.Precio` en ese instante); cambios futuros del plan no afectan cuotas ya emitidas.
- **No duplicar recordatorios del mismo tipo por día:** `RecordatorioCuota` actúa como llave de idempotencia.
- **Soft delete de cuotas:** la entidad `Cuota` nunca se borra físicamente.
- **Recordatorios solo para cuotas pendientes:** las pagadas o anuladas se omiten.
- **El servicio de email se puede deshabilitar en configuración** para desarrollo/testing.

**Deuda técnica pendiente (registrada en IT-2):**

- Migrar `SmtpClient` (deprecated, bugs de TLS) a `MailKit`.
- Si el server arranca después de la hora configurada, los recordatorios del día se saltean. Fix: comparar última ejecución contra hora actual al arrancar.
- `TimeSpan.Parse` en `HoraEjecucion` sin validación crashea la app. Fix: usar `TryParse` con fallback.
- `/cuotas/socios-estado` devuelve todos los socios sin paginar.
- Falta endpoint manual para disparar el cron (`POST /api/cuotas/procesar-recordatorios` solo admin) — útil para demos.

**Desviaciones respecto del diseño original:**

- **Pago online (botón "Pagar"):** el botón existe en la vista del socio pero está deshabilitado. El pago online (CU-08 del documento original / RF-21 con Mercado Pago) no se implementó.
- **Notificación masiva:** el diseño original mencionaba "notificar a todos". Solo se implementó notificación individual por cuota.
