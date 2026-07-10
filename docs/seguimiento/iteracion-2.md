---
title: DOCUMENTACIÓN ITERACIÓN 2
tags:
  - seguimiento
related:
  - "[[seguimiento_index]]"
---

# DOCUMENTACIÓN ITERACIÓN 2

**Iteración 2 --- Fase de Construcción (30/04/2026 -- 14/05/2026)**

> Nota: este documento se retro-convirtió desde `Documentacion_It.2.docx` (la versión entregada) para incorporarlo al flujo de trabajo en Obsidian. Las capturas de pantalla están solo en el .docx original. Se agregó la sección de pruebas automatizadas (xUnit). El .docx original indica "14/04/2026" como fecha de fin; se corrigió a 14/05/2026.

## Descripción general

El objetivo de esta iteración es comenzar a construir un portal básico para socios con gestión de cuotas y mecanismos de administración de roles, permisos y usuarios.

## Tareas planificadas

Funcionalidades a implementar:

- **RF-05 — Ver perfil del socio:** El socio puede consultar sus datos personales, estado de cuota y plan activo desde el portal. Además, como parte de completar el punto (b) de RNF-09 que quedó pendiente en la iteración 1, desde este perfil el socio también puede solicitar la modificación o baja de sus datos personales.
- **RF-06 — Recordatorios automáticos de cuota:** El sistema ejecuta diariamente un job en background que evalúa las fechas de vencimiento de todos los socios activos y envía recordatorios por correo: preventivo a los 5 días antes del vencimiento, urgente a 1 día antes, y notificación de vencimiento el día que se cumple si no se registró pago. Cada envío queda registrado con timestamp y tipo de recordatorio. No se envía más de un recordatorio del mismo tipo por socio por día.
- **RF-07 — Control de estado de cuota:** El administrador puede visualizar qué socios están al día, próximos a vencer (dentro de 7 días) o vencidos, desde el módulo de Cuotas. El estado se calcula dinámicamente según la fecha del sistema. El administrador puede filtrar por estado y registrar manualmente un pago, actualizando la fecha de vencimiento con generación de log de auditoría.

Requerimientos no funcionales:

- **RNF-01 — Autenticación y autorización basada en Roles y Grupos de Seguridad por Menú:** En esta iteración se implementa el sistema de roles y permisos para los perfiles de Administrador, Profesor y otros roles personalizados que el administrador pueda crear. La parte correspondiente a socios (OAuth 2.0) se completa en la iteración 4.
- **RNF-03 — Plataforma responsive:** Las vistas del portal del socio y del módulo de cuotas deben adaptarse correctamente a móviles, tablets y escritorio.
- **RNF-07 — Compatibilidad de navegadores:** Las nuevas vistas implementadas en esta iteración deben funcionar correctamente en las últimas versiones de Chrome, Firefox, Safari y Edge.

## ¿Qué se implementó?

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|----|----|----|----|
| RF-05 — Ver perfil del socio | CU-01 | Implementado | Portal del socio con consulta de datos personales, estado de cuota, plan activo por unidad y solicitudes ARCO de modificación/baja de datos (Ley 18.331, RNF-09b) |
| RF-06 — Recordatorio de cuota | CU-03 | Implementado | Envío automático de emails por SMTP a socios con cuota próxima a vencer (5 días, 1 día) y vencida. Envío no bloqueante, sin duplicados por tipo/día |
| RF-07 — Control de estado de cuota | CU-03 | Implementado | Vista de gestión de cuotas con estados al día / próxima a vencer / vencida. Registro manual de pagos por admin con actualización de fecha de vencimiento. Nueva UX completa |
| RNF-01 — Autenticación y autorización por roles | CU-05, CU-07 | Implementado | Sistema de permisos dinámicos por módulo, roles con cache, middleware de autorización, gestión CRUD de empleados, entidad Empleado con BCrypt, login productivo para empleados |
| RNF-03 — Responsive | — | Implementado | Adaptación móvil, tablet y escritorio aplicada junto con la UX de gestión de cuotas y recordatorios |
| RNF-06 — Disponibilidad ≥ 95% | — | No verificable | Requerimiento de infraestructura/operación, no tiene implementación de código específica en esta iteración |

## Tareas pendientes

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|----|----|----|----|
| Deuda técnica — SmtpClient deprecated | — | Pendiente | SmtpClient tiene bugs conocidos de TLS. Migrar a MailKit en próxima iteración |
| Deuda técnica — BG Service no re-ejecuta tras reinicio | — | Pendiente | Si el server arranca después del horario configurado, los recordatorios del día se saltean. Fix: comparar última ejecución contra hora actual al arrancar |
| Deuda técnica — TimeSpan.Parse sin validación | — | Pendiente | Config inválida en HoraEjecucion crashea la app. Fix: usar TryParse con fallback |
| Deuda técnica — Endpoint sin paginación | — | Pendiente | /cuotas/socios-estado devuelve todos los socios sin paginar. OK para escala TFG, no escala |
| Deuda técnica — Falta endpoint manual de recordatorios | — | Pendiente | No hay forma de disparar el cron manualmente. Útil para demos: POST /api/cuotas/procesar-recordatorios (solo admin) |

## Pantallas implementadas

**Pantalla PerfilSocioPage**

*(captura de pantalla)*

**Ruta:** /portal/perfil

**Descripción:** Portal del socio: visualización de datos personales, plan, unidades y solicitudes de modificación/baja (RNF-09b)

**Pantalla MisCuotasPage**

*(captura de pantalla)*

**Ruta:** /portal/mis-cuotas

**Descripción:** Vista del socio con historial de sus cuotas, estado y paginación

**Pantalla SociosCuotasPage**

*(captura de pantalla)*

**Ruta:** /admin/cuotas

**Descripción:** Listado de socios con resumen de estado de cuota, filtro por unidad y búsqueda

**Pantalla CuotasPage**

*(captura de pantalla)*

**Ruta:** /admin/cuotas/:socioId

**Descripción:** Detalle de cuotas de un socio: marcar pagada, revertir pago, anular, notificar.

**Pantalla RolesPage**

*(captura de pantalla)*

**Ruta:** /admin/roles

**Descripción:** Listado de roles con permisos asociados

**Pantalla NuevoRolPage**

*(captura de pantalla)*

**Ruta:** /admin/roles/nuevo

**Descripción:** Formulario de creación de rol con selección de permisos por módulo

**Pantalla EditRolPage**

*(captura de pantalla)*

**Ruta:** /admin/roles/:id

**Descripción:** Edición de rol existente y sus permisos

**Pantalla UsuariosPage**

*(captura de pantalla)*

**Ruta:** /admin/usuarios

**Descripción:** Listado de empleados activos/inactivos con baja y reactivación

**Pantalla NuevoUsuarioPage**

*(captura de pantalla)*

**Ruta:** /admin/usuarios/nuevo

**Descripción:** Formulario de alta de empleado con rol y contraseña

**Pantalla EditUsuarioPage**

*(captura de pantalla)*

**Ruta:** /admin/usuarios/:id

**Descripción:** Edición de datos de empleado

**Pantalla CambiarPasswordPage**

*(captura de pantalla)*

**Ruta:** /admin/usuarios/:id/password

**Descripción:** Cambio de contraseña de empleado

## Estructura de API --- endpoints implementados

| **Método** | **Endpoint** | **Descripción** |
|:--:|----|----|
| GET | /api/portal/perfil | Obtener perfil del socio autenticado |
| POST | /api/portal/solicitar-modificacion | Solicitar modificación de datos personales (RNF-09b) |
| POST | /api/portal/solicitar-baja | Solicitar baja de cuenta (RNF-09b) |
| GET | /api/cuotas/mis-cuotas | Obtener cuotas del socio autenticado |
| GET | /api/cuotas/admin | Buscar cuotas por documento con filtros (estado, mes, año, unidad) |
| GET | /api/cuotas/admin/socio/{socioId} | Obtener cuotas de un socio por ID |
| GET | /api/cuotas/socios-estado | Listar socios con resumen de estado de cuota |
| PUT | /api/cuotas/{id}/pagar | Marcar cuota como pagada |
| PUT | /api/cuotas/{id}/revertir-pago | Revertir pago de cuota |
| PUT | /api/cuotas/{id}/revertir-anulacion | Revertir anulación de cuota |
| POST | /api/cuotas/{id}/notificar | Enviar notificación manual de cuota |
| DELETE | /api/cuotas/{id} | Anular cuota |
| GET | /api/empleados | Listar empleados (filtro por estado activo) |
| GET | /api/empleados/{id} | Obtener empleado por ID |
| POST | /api/empleados | Crear nuevo empleado |
| PUT | /api/empleados/{id} | Actualizar datos de empleado |
| PATCH | /api/empleados/{id}/password | Cambiar contraseña de empleado |
| PATCH | /api/empleados/{id}/reactivar | Reactivar empleado dado de baja |
| DELETE | /api/empleados/{id} | Baja lógica de empleado |
| GET | /api/roles | Listar todos los roles |
| GET | /api/roles/{id} | Obtener rol por ID |
| POST | /api/roles | Crear nuevo rol con permisos |
| PUT | /api/roles/{id} | Actualizar rol y sus permisos |
| DELETE | /api/roles/{id} | Eliminar rol |
| GET | /api/permisos | Listar catálogo de permisos disponibles |

## Requerimiento RF-07

El sistema debe generar cuotas automáticamente para cada socio activo y permitir su visualización tanto para el socio como para el admin. El admin debe poder marcar cuotas como pagadas y anular cuotas generadas por error.

**Incluido**

- Generación automática de cuotas cada 30 días por socio activo con plan asignado.
- Vista del socio con sus cuotas (estado, monto, vencimiento, botón "Pagar" sin funcionalidad por ahora).
- Vista del admin con búsqueda por cédula de socios y te muestra sus cuotas. Incluye filtros por estado/mes/año/unidad, y ordenamiento por vencimiento.
- El admin puede marcar una cuota como pagada manualmente (para pagos en efectivo o transferencia).
- El admin puede anular una cuota generada por error (soft delete).
- Cuotas separadas por unidad (si el socio pertenece a 2 unidades, tiene 2 cuotas por período).
- Nuevo módulo de permisos Cuotas (lectura, escritura, modificación, eliminación).
- BackgroundService diario para generación automática de cuotas, configurable desde appsettings.json.

**Fuera de alcance (queda para RF-06 u otras iteraciones)**

- Botón "Notificar" individual por cuota pendiente - RF-06.
- Recordatorios automáticos por email (5 días, 1 día, día del vencimiento) - RF-06.
- Infraestructura de email (servicio, configuración, proveedor) - RF-06.
- Entidad RecordatorioCuota (registro de notificaciones enviadas) - RF-06.
- Pago online: el botón "Pagar" del socio existe pero no procesa pagos. Se implementa en una iteración futura.
- Notificaciones in-app (campanita/push): solo email por ahora. Queda para RF-16 (Iteración 6).
- Notificación masiva: solo se notifica individualmente por cuota. Un botón "notificar a todos" podría agregarse después.
- Configuración de días de anticipación desde la UI: se configura en el servidor (appsettings.json).
- Cuota combinada para socios en 2 unidades: por ahora son 2 cuotas separadas.
- Recordatorios post-vencimiento (ej. a 7 días de mora): se puede agregar después.

**3. Generación automática de cuotas**

- Cuando un socio se registra (o se le asigna un plan a una unidad), el sistema genera su primera cuota con estado pendiente.
- La fecha de vencimiento de la primera cuota es 30 días después de la FechaAlta del socio.
- Cada 30 días se genera automáticamente una nueva cuota pendiente para cada socio activo que tenga plan asignado.
- Si el socio pertenece a 2 unidades, se generan 2 cuotas separadas (una por unidad, cada una con el monto de su plan correspondiente).

Las cuotas se generan con los siguientes datos:

- Socio al que pertenece
- Unidad (Gimnasio Nuevo Malvín o Espacio Mora)
- Plan vigente del socio en esa unidad (snapshot al momento de generación)
- Monto a pagar (precio del plan al momento de generación)
- Fecha de emisión (fecha en que se generó)
- Fecha de vencimiento (fecha de emisión + 30 días)
- Estado: pendiente

**4. Vista del socio — "Mis cuotas"**

El socio accede desde su portal y ve una lista de sus cuotas con los siguientes datos por cada una:

| **Dato** | **Descripción** |
|:---|:---|
| Plan activo | Nombre del plan con el que se generó la cuota |
| Monto a pagar | Precio del plan |
| Fecha de vencimiento | Hasta cuándo tiene para pagar |
| Estado | Pagada o Pendiente (badge con color: verde/rojo) |
| Botón "Pagar" | Visible solo si la cuota está pendiente. Por ahora no hace nada — se muestra un mensaje "Próximamente" o se deja deshabilitado. |

**5. Vista del admin — "Gestión de cuotas"**

El admin (o cualquier usuario con permisos sobre el módulo Cuotas) accede desde el panel de administración.

**5.1. Filtros disponibles**

| **Filtro** | **Opciones** |
|------------|-------------------------------------------------------------|
| Estado | Pendiente / Pagada / Todas |
| Mes / Año | Selector de mes y año para filtrar por fecha de vencimiento |
| Unidad | Gimnasio Nuevo Malvín / Espacio Mora / Todas |

**5.2. Datos visibles por cuota**

| **Dato** | **Descripción** |
|----|----|
| Socio | Nombre completo del socio |
| Unidad | A qué unidad corresponde la cuota |
| Plan | Nombre del plan |
| Monto | Monto a pagar |
| Fecha de vencimiento | Fecha límite de pago |
| Estado | Pagada o Pendiente (badge con color) |
| Acciones | Botón "Marcar como pagada" (si pendiente) + Botón "Anular" (si pendiente) |

**5.3. Ordenamiento**

Por defecto: fecha de vencimiento descendente (los más próximos a vencer arriba).

**5.4. Botón "Marcar como pagada"**

- Visible solo si la cuota está pendiente.
- Al hacer clic, el admin confirma y el sistema cambia el estado a pagada y registra la fecha de pago.
- Se genera log de auditoría (quién marcó el pago, cuándo).

**5.5. Botón "Anular cuota"**

- Visible solo si la cuota está pendiente.
- Al hacer clic, el admin confirma y el sistema realiza un soft delete (establece FechaBaja).
- Se genera log de auditoría.

**6. Estados de la cuota**

| **Estado** | **Significado** |
|----|----|
| Pendiente | La cuota fue generada pero no se registró el pago. Badge rojo. |
| Pagada | El pago fue registrado (manual por admin o futuro pago online). Badge verde. |

**7. Entidad de dominio**

Entidad Cuota:

- Id (Guid)
- SocioId (FK - Socio)
- UnidadId (FK - Unidad)
- PlanId (FK - Plan)
- Monto (snapshot del precio del plan al generar)
- FechaEmision (cuándo se generó)
- FechaVencimiento (FechaEmision + 30 días)
- Estado (Pendiente / Pagada)
- FechaPago (nullable, se llena cuando se marca como pagada)
- FechaBaja (nullable, soft delete si se anula por error)

**8. BackgroundService para generación automática**

Se implementa un BackgroundService dentro de la aplicación ASP.NET que se ejecuta una vez al día para generar cuotas pendientes a socios activos con plan asignado.

- La hora de ejecución se configura en appsettings.json (ej: "HoraEjecucion": "03:00").
- Usa la hora del servidor (UTC).
- Por cada socio activo con plan asignado, verifica si la última cuota generada ya venció (o no tiene cuotas) y genera una nueva.
- Al crear un socio con plan asignado, también se genera la primera cuota automáticamente (FechaVencimiento = FechaAlta + 30 días).

**9. Cambio necesario en el alta de socio**

El socio ya tiene FechaAlta. Este campo se usa como referencia para generar la primera cuota (fecha de vencimiento = FechaAlta + 30 días). Al crear un socio con plan asignado, se genera automáticamente su primera cuota pendiente.

## Requerimiento RF-06

Implementar un sistema de notificaciones por email que permita al admin enviar recordatorios manuales individuales por cuota pendiente, y que el sistema envíe recordatorios automáticos antes del vencimiento de cada cuota.

**2. Alcance**

**Incluido**

- Infraestructura de email (servicio abstracto, configuración SMTP en appsettings.json, implementación con MailKit o similar).
- Servicio de email deshabilitable en configuración para desarrollo/testing.
- Botón "Notificar" individual por cuota pendiente en la vista admin (envío de email al socio).
- Recordatorios automáticos por email: 5 días antes, 1 día antes, y el día del vencimiento.
- Entidad RecordatorioCuota para registro de notificaciones enviadas (evitar duplicados).

**Fuera de alcance**

- Notificaciones in-app (campanita/push) - RF-16.
- Notificación masiva ("notificar a todos").
- Recordatorios post-vencimiento.

**3. Botón "Notificar" (manual del admin)**

- Es por cuota individual, se agrega a la vista admin de Gestión de cuotas.
- Al hacer clic, el sistema envía un email al socio recordándole que tiene una cuota pendiente.
- El email incluye: nombre del socio, plan, unidad, monto y fecha de vencimiento.
- Si el socio no tiene correo registrado, se muestra un mensaje de error al admin.
- No se puede reenviar la misma notificación más de una vez por día al mismo socio por la misma cuota.

**4. Recordatorios automáticos**

Además del botón manual del admin, el sistema envía recordatorios automáticos por email:

- 5 días antes del vencimiento: email informativo ("Tu cuota vence pronto").
- 1 día antes del vencimiento: email urgente ("Tu cuota vence mañana").
- El día del vencimiento: email de aviso ("Tu cuota venció hoy").

**Reglas**

- No se envía más de un recordatorio del mismo tipo por socio por día.
- Si el socio no tiene correo, se omite y queda registrado en el sistema.
- Los recordatorios solo se envían para cuotas en estado pendiente.
- El servicio de email se puede deshabilitar en configuración para desarrollo/testing.

**5. Entidad RecordatorioCuota**

Tabla auxiliar para registro de notificaciones enviadas (evitar duplicados):

- Id (Guid)
- CuotaId (FK - Cuota)
- SocioId (FK - Socio)
- TipoRecordatorio (CincoDias / UnDia / DiaVencimiento / Manual)
- FechaEnvio
- Exitoso (bool)
- Error (string nullable, mensaje si falló)

## Diagrama de actividades Recordatorios Automáticos de Cuota

*(diagrama en el .docx)*

## Reuniones con el cliente

No se realizaron reuniones formales con el cliente durante esta iteración. Se trabajó sobre los ajustes y sugerencias recopilados en la reunión de la iteración 3.

Funcionalidades presentadas:

- Pendiente de presentación en próxima reunión.

Sugerencias y ajustes solicitados:

- Los ajustes de la reunión anterior (inscripción desde cronograma, notificaciones de bienvenida) fueron incorporados en esta iteración.

## Pruebas automatizadas (xUnit)

Además de las pruebas de API con Postman, los módulos de esta iteración cuentan con pruebas automatizadas hechas en código (xUnit + Moq) en `backend/tests/**`, ejecutadas con `dotnet test` desde `backend/`. Cobertura correspondiente a esta iteración:

**Cuotas (RF-07):**

- *Dominio:* la cuota valida que el monto no sea negativo; marcar como pagada solo aplica desde estado pendiente y registra la fecha de pago; la anulación es una baja lógica que no aplica a cuotas ya pagadas; revertir el pago y revertir la anulación validan el estado previo; un vencimiento con día 31 se ajusta al último día del mes.
- *Application:* marcar la cuota como pagada registra auditoría y envía el email de confirmación — si el email falla o lanza excepción, el pago igual se confirma; revertir pago, anular y revertir anulación validan existencia y estado, y auditan; la generación de cuotas crea cuota al socio sin cuota previa o con la última vencida, no duplica si la última sigue vigente y no genera si la unidad no tiene plan; el listado admin busca las cuotas por socio con sus filtros; la vista de estado de cuota clasifica a cada socio como al día, pendiente o vencido, ordena los vencidos primero y obtiene los datos sin consultas N+1.

**Roles y permisos (RNF-01):**

- *Dominio:* la creación del rol valida el nombre; los roles de sistema no pueden renombrarse ni cambiar sus permisos; reemplazar permisos quita los anteriores, agrega los nuevos y deduplica silenciosamente.
- *Application:* el filtro `[RequierePermiso]` devuelve 401 sin autenticación, 403 sin claim de rol o sin el permiso requerido, y deja pasar cuando el permiso existe; crear, actualizar y eliminar roles validan nombre vacío o duplicado y protegen los roles de sistema; un rol con usuarios activos no puede eliminarse; las operaciones invalidan la cache de permisos y registran auditoría.

**Portal del socio (RF-05 / RNF-09b):**

- *Application:* el perfil devuelve los datos del socio autenticado y falla de forma controlada si no existe; las solicitudes de modificación y de baja registran auditoría (con o sin motivo) y validan el detalle vacío y el socio inexistente.

**Auditoría (RNF-11):**

- *Dominio:* el registro de auditoría valida usuario y descripción, almacena el detalle de cambios como JSON y admite eventos sin entidad asociada (como el inicio de sesión).
- *Application:* las operaciones sobre socios registran alta, baja y reactivación en la auditoría, y no generan registro si la operación falla.

> Nota: los recordatorios automáticos de cuota (RF-06) también tienen pruebas automatizadas (`ProcesarRecordatoriosCommandTests`, `NotificarCuotaCommandTests`), pero su forma actual (jobs disparables + notificaciones in-app) se consolidó en la Iteración 5 y se documenta allí.

El inventario completo de las pruebas automatizadas de las iteraciones 1 a 4, clase por clase, está en [[pruebas-automatizadas-it1-4]].

## Pruebas de API realizadas con Postman

Para la iteración 2 se incorporaron pruebas automatizadas en Postman para los nuevos endpoints de los módulos Cuotas, Portal del Socio, Empleados, y Roles y Permisos.

- **Carpetas nuevas:** 4
- **Requests nuevos:** 109
- **Resultado de ejecución:** 231 tests pasados y 0 fallidos en la colección ejecutada.

**Alcance de las pruebas**

Las pruebas cubren los principales escenarios funcionales y de error:

- **Happy path:** validación de operaciones exitosas como crear, consultar, actualizar, pagar cuotas, reactivar registros y obtener datos autenticados.
- **Errores de validación:** datos obligatorios faltantes, correos duplicados, roles inexistentes o solicitudes incompletas.
- **Datos inexistentes:** validación de respuestas 404 Not Found al consultar o modificar recursos que no existen.
- **Permisos:** validación de respuestas 403 Forbidden cuando un usuario autenticado no tiene permisos suficientes.
- **Autenticación:** validación de respuestas 401 Unauthorized cuando no se envía token o el acceso no corresponde.
- **RNF-01:** control de tiempos de respuesta, principalmente con límites menores a 500 ms, salvo casos particulares como login inicial o envío de notificaciones.

**Detalle por carpeta**

**Cuotas**

- Consulta de cuotas por socio.
- Filtros por estado.
- Consulta de socios con estado de cuota.
- Pago, reversión de pago, anulación y reversión de anulación.
- Notificación de cuotas.
- Errores por cuota o socio inexistente.
- Errores por operaciones inválidas, como pagar una cuota ya pagada.
- Validación de permisos 403 para usuarios sin permisos de cuotas.

**Portal del Socio**

- Consulta del perfil del socio autenticado.
- Solicitud de modificación de datos.
- Solicitud de baja.
- Validación de acceso sin autenticación.
- Validación de solicitudes incompletas.

**Empleados**

- Listado, creación, consulta y actualización de empleados.
- Cambio de contraseña.
- Baja lógica y reactivación.
- Validaciones por datos faltantes, correo duplicado y rol inexistente.
- Validación de empleados inexistentes con respuesta 404.

**Roles y Permisos**

- Listado de permisos y roles.
- Creación, consulta, actualización y eliminación de roles.
- Validación de roles duplicados o inexistentes.
- Creación de empleados con roles específicos.
- Pruebas de autorización según permisos asignados.
- Validación de respuestas 403 Forbidden para acciones no autorizadas.

**Ajustes realizados**

Durante la validación se detectaron inconsistencias en algunos controladores: ciertos endpoints devolvían 204 No Content aunque modificaban recursos. Estos casos fueron corregidos para devolver 200 OK junto con el recurso actualizado, manteniendo el mismo patrón REST utilizado en la iteración 1.

## Pruebas funcionales de frontend

### MÓDULO RF-05 — Portal del Socio (6 pruebas)

**Prueba 5.1 — Login y redirección automática al portal**

*(capturas de pantalla)*

Pasos:

1. Ir a /login
2. Ingresar socio@gymflow.com / socio123
3. Click "Iniciar sesión"

Resultado esperado: El sistema detecta el rol "Socio" y redirige automáticamente a /portal (no al panel admin). Se ve el header con "Portal del Socio" y la solapa "Mi Perfil".

Descripción: Verifica que el sistema redirija al socio al portal correcto según su rol, sin permitirle ingresar al panel administrativo. La separación de layouts se aplica en AdminLayout y SocioLayout.

**Prueba 5.2 — Visualización de datos personales**

*(captura de pantalla)*

Pasos:

1. Logueado como socio, estar en /portal
2. Observar la tarjeta "Datos personales" a la izquierda

Resultado esperado: Se muestran todos los datos:

- Nombre completo: María López
- Correo: socio@gymflow.com
- Teléfono: 099 123 456
- Documento: Cédula de identidad — 12345672
- Fecha de nacimiento: 20/06/1992
- Miembro desde: 15/01/2025

Descripción: Confirma que el endpoint GET /api/portal/perfil devuelve correctamente los datos del socio autenticado vía el correo del JWT, sin necesidad de pasar un ID (previene IDOR).

**Prueba 5.3 — Visualización de plan activo y unidad asignada**

*(captura de pantalla)*

Pasos:

1. En /portal, observar la tarjeta "Plan y acceso" a la derecha

Resultado esperado: Muestra la unidad "Espacio Mora" con el badge del plan "Plan Musculación" debajo.

Descripción: Valida la relación N:M UsuarioUnidad con PlanId, mostrando un plan por unidad asignada al socio.

**Prueba 5.4 — Solicitar modificación de datos personales**

*(capturas de pantalla)*

Pasos:

1. Click en botón "Solicitar modificación de datos"
2. En el diálogo, escribir: Quiero actualizar mi número de teléfono a 099 888 777
3. Click "Enviar solicitud"
4. (Logueado como admin) Ir a Sistema - Auditoría
5. Filtrar por tipo "Solicitud de modificación"

Resultado esperado:

- Aparece banner verde: *"Tu solicitud de modificación fue registrada..."*
- En auditoría aparece el registro con badge celeste, descripción detallada y timestamp.

Descripción: Cumplimiento del RNF-09b (Ley 18.331): el socio tiene derecho a solicitar modificación de sus datos. La solicitud no modifica datos directamente, solo registra la intención en auditoría para que el equipo administrativo la procese manualmente.

**Prueba 5.5 — Solicitar baja de cuenta con auto-logout**

*(capturas de pantalla)*

Pasos:

1. Click en botón rojo "Solicitar baja de cuenta"
2. Escribir motivo (opcional): Me mudo a otra ciudad
3. Click "Confirmar solicitud de baja"
4. Esperar 3 segundos

Resultado esperado:

- Banner verde: *"Tu solicitud de baja fue registrada. Cerrando sesión..."*
- A los 3 segundos, redirige automáticamente a /login
- En auditoría queda el registro con badge naranja

Descripción: Misma lógica que RF-05 pero con efecto colateral de auto-logout para evitar confusión (el socio entiende que su solicitud está siendo procesada y no debe seguir usando la cuenta).

### MÓDULO RF-07 — Gestión de Cuotas (7 pruebas)

**Prueba 7.1 — Buscar cuotas de un socio por cédula**

*(captura de pantalla)*

Pasos:

1. Logueado como admin, ir a Admin - Cuotas
2. Ingresar la cédula 12345672 en el buscador
3. Click "Buscar"

Resultado esperado: Aparece la lista de cuotas de María López (al menos 1 cuota seeded de Plan Musculación). Cada fila muestra: socio, unidad, plan, monto, vencimiento, estado.

Descripción: El endpoint GET /api/cuotas/admin?documentoIdentidad=... requiere documento obligatorio (decisión de diseño para forzar contexto de búsqueda). Internamente usa SocioRepository.GetByDocumentoIdentidadAsync() para eficiencia.

**Prueba 7.2 — Filtrar cuotas por estado**

*(capturas de pantalla)*

Pasos:

1. Con un socio cargado, usar el dropdown de Estado
2. Probar las 3 opciones: Pendiente / Pagada / Anulada

Resultado esperado: La tabla se actualiza dinámicamente. Cada estado tiene su badge de color:

- Pendiente (al día)
- Pendiente (vencida)
- Pagada
- Anulada

Descripción: El enum EstadoCuota modela los 3 estados explícitamente. La diferenciación visual entre "Pendiente al día" y "Pendiente vencida" se hace comparando fechaVencimiento con la fecha actual.

**Prueba 7.3 — Marcar cuota como pagada**

*(capturas de pantalla)*

Pasos:

1. Buscar cuotas del socio
2. Sobre una cuota Pendiente, click en botón "Marcar como pagada"
3. Confirmar en el diálogo

Resultado esperado:

- El estado pasa a "Pagada" (badge verde)
- En auditoría queda el registro con descripción "Se marcó como pagada la cuota de Plan Musculación del socio María López"

Descripción: Llamada a PUT /api/cuotas/{id}/pagar. La entidad Cuota.MarcarComoPagada() valida que no esté ya pagada ni anulada antes de cambiar el estado y setear FechaPago = UtcNow.

**Prueba 7.4 — Anular cuota**

*(capturas de pantalla)*

Pasos:

1. Sobre una cuota Pendiente, click en botón "Anular"
2. Confirmar en el diálogo

Resultado esperado: Cuota cambia a estado "Anulada" (badge gris). Aparece en la tabla solo si el filtro incluye anuladas.

Descripción: Soft delete: la cuota no se borra de la base, queda con FechaBaja = UtcNow y Estado = Anulada. Mantiene trazabilidad histórica. Una cuota ya pagada no se puede anular.

**Prueba 7.5 — Revertir pago de una cuota**

*(capturas de pantalla)*

Pasos:

1. Sobre una cuota Pagada, click en botón "Revertir pago"
2. Confirmar

Resultado esperado: Cuota vuelve a "Pendiente" y FechaPago se borra. Se registra en auditoría.

Descripción: Función de "deshacer" para corregir errores del admin (ej: marcar pagada por error). Solo aplica a cuotas en estado Pagada.

**Prueba 7.6 — Reactivar cuota anulada**

*(capturas de pantalla)*

Pasos:

1. Filtrar por estado "Anulada"
2. Sobre una cuota anulada, click en botón "Reactivar"
3. Confirmar

Resultado esperado: La cuota vuelve a "Pendiente" y FechaBaja se limpia.

Descripción: Permite recuperar cuotas anuladas por error. La auditoría registra ambas acciones (anulación + reversión) preservando el historial completo.

**Prueba 7.7 — Socio visualiza sus cuotas con estados diferenciados**

*(captura de pantalla)*

Pasos:

1. Logueado como socio (socio@gymflow.com)
2. Ir a /portal/mis-cuotas (o navegar desde el menú)

Resultado esperado: Tabla con:

- Plan, unidad, monto, vencimiento, estado, acción
- Badges con colores según estado (Pagada verde, Pendiente al día gris, Pendiente vencida rojo, Anulada gris claro)
- Botón "Pagar" deshabilitado con tooltip explicando que el pago online es futuro RF
- Paginación si hay más de 12 cuotas

Descripción: Endpoint GET /api/cuotas/mis-cuotas usa el socioId del JWT (igual que /perfil). El socio solo ve sus propias cuotas — el backend nunca expone IDs de otros socios.

## Registro de tiempos

**Desarrollo -- tiempo por commit**

| **Hash** | **Fecha** | **Descripción** | **Tiempo (hs)** |
|:--:|----|----|:--:|
| ce433d7 | 2026-04-30 | Correcciones varias | 1.00 |
| 3383779 | 2026-05-02 | Update nombre tutor en README | 1.00 |
| 5512577 | 2026-05-03 | RF-05: portal del socio con perfil | 1.50 |
| 18f6f5b | 2026-05-03 | Gestión de usuarios desarrollo completo | 1.50 |
| a8b5018 | 2026-05-03 | Tests automatizados creados y aprobados | 1.50 |
| b687e09 | 2026-05-04 | Fix: error CS0854 en tests + login socios | 1.25 |
| a9a9cd2 | 2026-05-04 | Merge PR #3: feature/RF_05 | 0.25 |
| b6daf11 | 2026-05-05 | Merge PR #4: fix RNF02/RF05 | 0.25 |
| 509cbdb | 2026-05-05 | Ajustes RF-05 y RNF-01 | 1.00 |
| bb996ca | 2026-05-10 | Desarrollo completo RF-07 (cuotas) | 1.50 |
| b751229 | 2026-05-11 | RF-06: recordatorios + UX cuotas + responsive | 1.50 |
| a295819 | 2026-05-11 | Correcciones según review de RF-07 | 1.50 |
| 545b483 | 2026-05-12 | Fix: 3 bloqueantes review (N+1, XSS, SMTP) | 1.25 |
| 4c0651c | 2026-05-14 | Mejoras en controllers iteración 2 | 1.50 |
|  |  | **Subtotal Desarrollo** | **16.5** |

**Otras actividades**

| **Actividad** | **Tiempo (hs)** |
|----|:--:|
| Planificación Plan de testing - Frontend | 2 |
| Ejecución plan de testing - Frontend | 2 |
| Planificación Plan de testing - Endpoints en Postman | 3 |
| Ejecución Plan de testing - Endpoints en Postman | 5 |
| Documentación | 5 |
| **Subtotal Otras Actividades** | **17** |

**TOTAL HORAS - Iteración 2: 33.5**
