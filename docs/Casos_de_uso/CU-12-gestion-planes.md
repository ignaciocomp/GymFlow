# CU-12: Gestión de Planes por Unidad de Negocio

| *Campo* | |
|-|-|
| *Nombre* | Gestión de planes por unidad de negocio (ABM con baja lógica y reactivación) |
| *Actor principal* | Administrador (o empleado con permisos del módulo Planes) |
| *Precondición* | Usuario autenticado con permisos del módulo Planes (Lectura / Escritura / Modificación / Eliminación según la operación). Existen unidades de negocio. |
| *Postcondición* | El catálogo de planes queda actualizado y auditado. Los planes activos de cada unidad son los que se ofrecen al asignar plan en el alta y edición de socios (RF-22), y su precio vigente determina el monto de las cuotas que se emitan a partir de ese momento (RF-07). |
| *RF cubiertos* | Sin RF propio — funcionalidad de soporte de RF-22 (plan por unidad de negocio) y RF-07 (el precio del plan es el origen del monto de cada cuota emitida). |
| *Iteración de entrega* | IT-1 — [iteracion-1.md](../seguimiento/iteracion-1.md) (CRUD completo con baja lógica y reactivación, pantallas y filtro por unidad). Protección por permisos del módulo Planes incorporada con el sistema de permisos por módulo (RNF-01, IT-2). |
| *Referencia original* | No tiene CU propio en [GymFlow_Requerimientos_Completos.md](../GymFlow_Requerimientos_Completos.md); nace como soporte de RF-22 dentro del alcance de la Iteración 1. |
| *Specs / planes* | Sin spec propio. Pruebas funcionales del módulo Planes documentadas en [iteracion-1.md](../seguimiento/iteracion-1.md). |
| *CU relacionados* | [CU-01 — Gestión de socios](CU-01-gestion-socios.md) (asignación de plan por unidad en el alta/edición) · [CU-03 — Gestión de cuotas](CU-03-cuotas-recordatorios.md) (snapshot del precio del plan en las cuotas emitidas) |

**Flujo principal — Alta de plan:**

1. El admin accede a `/admin/planes` y hace clic en "Nuevo plan".
2. Completa nombre, precio, descripción (opcional) y selecciona la unidad de negocio a la que pertenece el plan.
3. El sistema valida los datos (nombre obligatorio, precio no negativo, unidad existente) y crea el plan en estado Activo (`POST /api/planes`).
4. La auditoría registra la creación ("Se creó el plan X para {unidad}").
5. El plan queda disponible de inmediato para asignarse a socios de esa unidad en el alta y la edición de socio.

**Flujo alternativo — Listado y filtro:**

1. El admin accede a `/admin/planes`.
2. El sistema muestra la tabla de planes con nombre, unidad, precio y estado (Activo / Inactivo), incluyendo los dados de baja.
3. El admin puede filtrar por unidad de negocio.

**Flujo alternativo — Edición de plan:**

1. Sobre un plan activo, el admin elige "Editar" (`/admin/planes/:id/editar`).
2. Puede modificar nombre, precio y descripción. **La unidad no se puede cambiar** una vez creado el plan (campo deshabilitado; la edición no la acepta).
3. Al guardar (`PUT /api/planes/{id}`), los cambios se reflejan en el listado y la auditoría registra la modificación.
4. El nuevo precio aplica **solo a las cuotas que se emitan de ahí en adelante**: las cuotas ya emitidas conservan su monto (snapshot de precio — ver [CU-03](CU-03-cuotas-recordatorios.md)).

**Flujo alternativo — Baja lógica:**

1. Sobre un plan activo **sin socios asignados**, el admin elige "Dar de baja" y confirma en el diálogo.
2. El sistema desactiva el plan (`DELETE /api/planes/{id}`, soft delete: `EstaActivo = false`); no se borra físicamente.
3. El plan deja de ofrecerse en el alta y la edición de socios, pero sigue visible como Inactivo en el listado del admin.
4. La auditoría registra la baja.

**Flujo alternativo — Reactivación:**

1. Sobre un plan Inactivo, el admin elige "Reactivar" y confirma.
2. El sistema lo vuelve a activar (`PATCH /api/planes/{id}/reactivar`) y el plan vuelve a ofrecerse para asignación.
3. La auditoría registra la reactivación.

**Flujos de excepción:**

- **E1 — Baja con socios asignados:** si el plan tiene socios asignados, la baja es rechazada con "El plan tiene socios asignados. Reasígnelos antes de darlo de baja." El plan no cambia de estado.
- **E2 — Editar un plan dado de baja:** rechazado ("No se puede editar un plan dado de baja.").
- **E3 — Operación redundante:** dar de baja un plan ya inactivo o reactivar uno ya activo es rechazado (409).
- **E4 — Datos inválidos:** nombre vacío o precio negativo son bloqueados por validación, tanto en el formulario como en el dominio.
- **E5 — Unidad inexistente en el alta:** rechazado con error de validación (400).

**Reglas de negocio aplicables:**

- **Plan por unidad (RF-22):** cada plan pertenece a una única unidad de negocio y no puede moverse a otra.
- **Solo planes activos se ofrecen:** el alta y la edición de socio listan únicamente los planes activos de la unidad seleccionada.
- **Baja lógica:** los planes nunca se eliminan físicamente; la baja exige que no haya socios con el plan asignado.
- **Snapshot de precio (RF-07):** el monto de una cuota se congela al emitirse (`Monto = Plan.Precio` en ese instante); los cambios de precio posteriores no alteran cuotas ya emitidas.
- **Auditoría:** creación, modificación, baja y reactivación quedan registradas con usuario, timestamp y detalle.

**Criterios de aceptación:**

- **CA-01:** Un plan creado queda Activo y disponible de inmediato en el alta de socio de su unidad (y solo de su unidad).
- **CA-02:** La edición de precio no modifica el monto de cuotas ya emitidas; las cuotas nuevas toman el precio vigente.
- **CA-03:** Un plan con socios asignados no puede darse de baja; el sistema lo informa sin cambiar el estado.
- **CA-04:** Un plan dado de baja deja de ofrecerse en el alta de socio; al reactivarlo vuelve a ofrecerse.
- **CA-05:** Las cuatro operaciones del ABM quedan en el log de auditoría.

**Pantallas implementadas:**

| Pantalla | Ruta | Descripción |
|-|-|-|
| Planes | `/admin/planes` | Listado con filtro por unidad, estado (Activo/Inactivo) y acciones: editar, dar de baja, reactivar. |
| Nuevo plan | `/admin/planes/nuevo` | Formulario de alta: nombre, precio, descripción y unidad. |
| Editar plan | `/admin/planes/:id/editar` | Edición de nombre, precio y descripción; la unidad aparece deshabilitada. |

**Deuda técnica pendiente (detectada al documentar este CU):**

- El endpoint `PATCH /api/planes/{id}/reactivar` no tiene `[Authorize]` ni `[RequierePermiso]`, y `PlanesController` no declara `[Authorize]` a nivel de clase, por lo que la reactivación queda accesible sin autenticación. Los demás endpoints del módulo sí están protegidos por `[RequierePermiso(Modulo.Planes, ...)]`. Fix: agregar `[Authorize]` al controller y `[RequierePermiso(Modulo.Planes, Operacion.Modificacion)]` al endpoint.
