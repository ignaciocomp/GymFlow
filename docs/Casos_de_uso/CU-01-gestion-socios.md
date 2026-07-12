# CU-01: Gestión de Socios (ABM + Perfil del Socio + ARCO)

| *Campo* | |
|-|-|
| *Nombre* | Gestión de Socios |
| *Actor principal* | Administrador (ABM) / Socio (perfil y solicitudes ARCO) |
| *Precondición* | El usuario está autenticado. Para ABM: rol con permisos sobre el módulo Socios. Para perfil: rol Socio. |
| *Postcondición* | El sistema refleja el estado actualizado del socio y registra la operación en el log de auditoría. Las solicitudes de modificación/baja del socio quedan registradas en auditoría para procesamiento manual. |
| *RF cubiertos* | RF-01, RF-02, RF-03, RF-04, RF-05, RF-20 (prerrequisito), RF-22, RNF-09 (Ley 18.331) |
| *Iteración(es) de entrega* | IT-1 — ABM completo (RF-01/02/03/04/20/22) + consentimiento Ley 18.331. IT-2 — Perfil del socio (RF-05) + solicitudes ARCO de modificación/baja (RNF-09b). |
| *Referencia original* | [GymFlow_Requerimientos_Completos.md § CU-01](../GymFlow_Requerimientos_Completos.md#cu-01--gestión-de-socios-alta-baja-modificación) |
| *Referencia spec* | [spec-rf01-tipo-documento](../specs/spec-rf01-tipo-documento.md), [spec-rf01-frontend-tipo-documento](../specs/spec-rf01-frontend-tipo-documento.md), [spec-rf02-fecha-alta-seleccionable](../specs/spec-rf02-fecha-alta-seleccionable.md), [spec-rf22-planes-por-unidad](../specs/spec-rf22-planes-por-unidad.md) |
| *Referencia plan* | [plan-rf01-tipo-documento](../plans/plan-rf01-tipo-documento.md), [plan-rf01-frontend-tipo-documento](../plans/plan-rf01-frontend-tipo-documento.md), [plan-rf02-fecha-alta-seleccionable](../plans/plan-rf02-fecha-alta-seleccionable.md), [plan-rf22-planes-por-unidad](../plans/plan-rf22-planes-por-unidad.md) |
| *Documentos de iteración* | [Documentacion_It.1.docx](../seguimiento/Documentacion_It.1.docx), [Documentacion_It.2.docx](../seguimiento/Documentacion_It.2.docx) |

**Flujo principal:**

1. El administrador accede al listado de socios.
2. El sistema muestra los socios con búsqueda por nombre y filtros por unidad, plan y estado (activo/inactivo) — con tabs activos/inactivos.
3. El administrador selecciona una acción: registrar, editar, dar de baja o reactivar.

**Subflujo A — Registro de socio (RF-01):**

1. El administrador completa el formulario con datos personales: nombre, apellido, tipo y número de documento (con validación de cédula uruguaya cuando aplica), teléfono, fecha de nacimiento, correo.
2. El administrador asigna el socio a una o más unidades (Gimnasio Nuevo Malvín / Espacio Mora / ambas) y selecciona un plan por cada unidad asignada (RF-22).
3. El administrador acepta la cláusula de consentimiento informado de la Ley 18.331 — checkbox obligatorio.
4. El sistema valida los datos, persiste al socio con estado `Activo` y guarda el timestamp de aceptación del consentimiento.
5. Si el socio tiene plan asignado, se genera automáticamente su primera cuota pendiente con vencimiento a `FechaAlta + 1 mes` (RF-07).
6. El sistema registra la operación en el log de auditoría con usuario, timestamp y detalle.

**Subflujo B — Edición de socio (RF-03):**

1. El administrador selecciona un socio y accede al formulario de edición.
2. Modifica los campos deseados.
3. El sistema persiste los cambios (PUT al backend) y genera un registro de auditoría con el detalle de los campos modificados.

**Subflujo C — Baja lógica y reactivación (RF-04):**

1. El administrador selecciona la opción de baja e ingresa un motivo opcional (`motivoBaja`).
2. El sistema marca al socio como `Inactivo` (soft-delete) sin eliminar el registro.
3. El registro puede ser reactivado posteriormente desde la tab de inactivos.

**Subflujo D — Perfil del socio (RF-05, IT-2):**

1. El socio se autentica y el sistema lo redirige automáticamente a `/portal` según su rol (separación de layouts Admin/Socio).
2. El socio ve su tarjeta "Datos personales" (nombre, correo, teléfono, documento, fecha de nacimiento, fecha de alta).
3. El socio ve la tarjeta "Plan y acceso" con sus unidades asignadas y el plan vigente por unidad (relación N:M `UsuarioUnidad` con `PlanId`).
4. El backend identifica al socio a partir del correo del JWT (no se pasa ID por URL — previene IDOR).

**Subflujo E — Solicitud de modificación de datos (RNF-09b, IT-2):**

1. El socio hace clic en "Solicitar modificación de datos" desde su perfil.
2. Completa un diálogo describiendo el cambio deseado.
3. El sistema **no modifica los datos directamente**: registra la solicitud en el log de auditoría (badge celeste) para que el equipo administrativo la procese manualmente.
4. El socio recibe confirmación en pantalla.

**Subflujo F — Solicitud de baja con auto-logout (RNF-09b, IT-2):**

1. El socio hace clic en "Solicitar baja de cuenta" desde su perfil.
2. Ingresa motivo opcional y confirma.
3. El sistema registra la solicitud en auditoría (badge naranja) y muestra confirmación.
4. A los 3 segundos, el sistema cierra la sesión automáticamente y redirige a `/login` (evita que el socio siga operando una cuenta marcada para baja).

**Flujos de excepción:**

- **E1 — Consentimiento no aceptado:** El sistema impide el alta del socio.
- **E2 — Campos obligatorios incompletos:** Errores de validación en el formulario; no persiste.
- **E3 — CI uruguaya inválida:** Si `TipoDocumento == CI`, valida el dígito verificador y rechaza CIs malformadas.
- **E4 — Acceso sin autenticación al portal:** Bloqueado por middleware; 401.

**Reglas de negocio aplicables:**

- **Consentimiento Ley 18.331** obligatorio durante el alta, con timestamp persistido (RNF-09a).
- **Plan por unidad (RF-22):** el plan no es un campo global del socio; se almacena en `UsuarioUnidad.PlanId` y se filtra a los planes activos de cada unidad. Un mismo socio puede tener planes distintos en unidades distintas (ej. "Musculación" en Nuevo Malvín y "Completo" en Espacio Mora).
- **Baja lógica:** los socios nunca se borran físicamente; se marcan como `Inactivo` y pueden reactivarse.
- **Trazabilidad total:** toda alta, modificación, baja y reactivación queda en `RegistroAuditoria` con usuario, timestamp UTC, tipo de acción y `detallesCambios` en JSON.
- **ARCO Ley 18.331 (RNF-09b):** el socio puede solicitar modificación/baja de sus datos personales; la solicitud queda en auditoría para procesamiento manual.
- **Generación automática de cuota inicial:** al crear un socio con plan asignado, se genera su primera cuota pendiente con vencimiento a `FechaAlta + 1 mes`.

**Pantallas implementadas:**

- **Socios** (`/admin/socios`) — listado con tabs activo/inactivo, búsqueda y filtros.
- **Nuevo Socio** (`/admin/socios/nuevo`) — alta con consentimiento obligatorio.
- **Editar Socio** (`/admin/socios/:id`) — edición con auditoría automática.
- **PerfilSocioPage** (`/portal/perfil`) — datos personales, plan, unidades y botones de solicitud ARCO.

**Desviaciones respecto del diseño original:**

- **Plan por unidad (RF-22):** el diseño original tenía `PlanId` como campo global del socio. Se reubicó a `UsuarioUnidad.PlanId` en IT-1 para permitir planes distintos por sede.
- **Tipo de documento:** se agregó el enum `TipoDocumento` (CI | Pasaporte | Otro) con validación condicional, no contemplado en la versión original.
- **Solicitudes ARCO en lugar de auto-modificación:** el RNF-09b se implementa como **solicitudes registradas en auditoría** (procesamiento manual por admin) en vez de auto-modificación directa por parte del socio. Decisión de IT-2 para mantener control administrativo.
