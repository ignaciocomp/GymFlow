# CU-08: Gestión de Eventos

| *Campo* | |
|-|-|
| *Nombre* | ABM de Eventos por sede con notificación a socios |
| *Actor principal* | Administrador / Dueño (acotado a sus unidades) — gestión. Socio — consulta. |
| *Precondición* | Actor con permiso de escritura en el módulo Eventos. La sede destino existe. |
| *Postcondición* | Evento creado, actualizado o cancelado. Email enviado a los socios activos de la sede (envío "best-effort": si alguno falla no rompe la operación). Auditoría con conteo de emails enviados y fallidos. Notificación in-system creada por cada socio. |
| *RF cubiertos* | RF-15 |
| *Iteración(es) de entrega* | IT-5 — ABM de eventos por sede + baja lógica + notificación por email a socios al crear + acción manual de re-notificar + vista "Próximos eventos" en el portal. Se agrega el módulo "Eventos" al sistema de permisos. |
| *Referencia original* | Funcionalidad documentada en [iteracion-5.md § CU-08](../seguimiento/iteracion-5.md). No figura como CU detallado en el documento original de requerimientos. |
| *Documento de iteración* | [iteracion-5.md](../seguimiento/iteracion-5.md) |

**Flujo principal — Crear evento:**

1. El admin (o Dueño) accede a "Eventos" → "Nuevo".
2. Completa título, descripción, fecha (no pasada) y sede (el Dueño solo ve sus sedes asignadas).
3. El sistema valida que la sede exista y que la fecha sea futura, y **guarda y audita el evento ANTES de enviar emails** (así, aunque el envío de emails falle, el evento queda creado).
4. El sistema obtiene del repositorio el listado de socios activos de la sede.
5. El sistema envía los emails en paralelo usando la plantilla del evento y cuenta cuántos se enviaron correctamente y cuántos fallaron para registrarlo en la auditoría.
6. El sistema crea, para cada socio, una notificación in-system del tipo "Evento nuevo" para que aparezca en su campanita del portal (ver [CU-09](CU-09-notificaciones-insystem.md)).

**Flujo alternativo — Actualizar evento:**

- Mismo flujo que crear, sin re-notificación automática. Si se quiere re-avisar, se usa la acción manual "Notificar".

**Flujo alternativo — Cancelar evento:**

- Baja lógica: el evento deja de aparecer en el portal del socio pero queda en la base para consulta histórica.

**Flujo alternativo — Re-notificar manualmente:**

- El admin/Dueño elige "Notificar" sobre un evento existente.
- Reenvía email a los socios activos de la sede; se audita.

**Flujo alternativo — Vista del Socio (Portal):**

1. El socio accede a "Próximos eventos" en el portal.
2. El endpoint devuelve los próximos eventos activos de las sedes del socio, ordenados por fecha.
3. Las sedes se resuelven en el servidor desde la sesión del socio — **no se confía en lo que mande el frontend**.

**Flujos de excepción:**

- **E1 — Fecha pasada:** Bloqueado por validación.
- **E2 — Sede inexistente o no permitida (Dueño):** Bloqueado.
- **E3 — Falla envío de email a un socio:** No rompe la operación; el conteo de fallidos queda en auditoría.

**Reglas de negocio aplicables:**

- **Eventos asociados a una sede:** cada evento pertenece a una unidad. Los socios solo ven eventos de las sedes en que están inscriptos.
- **Filtrado por sede para el Dueño:** solo puede crear/ver eventos de las sedes que tiene asignadas.
- **Envío de emails best-effort:** si fallan algunos, el evento queda creado igualmente; el conteo de fallidos queda en auditoría.
- **Guardado antes del envío:** el evento se persiste y audita antes de iniciar los envíos para garantizar consistencia.
- **Baja lógica:** los eventos no se borran físicamente.
- **Re-notificación manual:** la actualización del evento no dispara emails automáticamente — el admin decide cuándo re-avisar.
- **Notificación in-system por socio:** además del email, cada socio recibe una notificación en la campanita del portal.

**Desviaciones respecto del diseño original:**

- **No figura en el documento de requerimientos original.** Se agregó como nuevo CU en IT-5 a partir de RF-15.
