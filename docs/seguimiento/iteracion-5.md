---
title: DOCUMENTACIÓN ITERACIÓN 5
tags:
  - seguimiento
related:
  - "[[seguimiento_index]]"
  - "[[spec-it5-login-google]]"
  - "[[plan-it5-login-google]]"
  - "[[spec-it5-mfa-empleados]]"
  - "[[plan-it5-mfa-empleados]]"
  - "[[spec-it5-rol-dueno]]"
  - "[[plan-it5-rol-dueno]]"
  - "[[spec-rf15-eventos]]"
  - "[[plan-rf15-eventos]]"
  - "[[spec-rf16-notificaciones]]"
  - "[[plan-rf16-notificaciones]]"
---

# DOCUMENTACIÓN ITERACIÓN 5

**Iteración 5 --- Fase de Construcción (14/06/2026 -- 28/06/2026)**
**Prioridad:** DESEABLE/OPCIONAL

## Descripción general

La quinta iteración cerró el bloque de **autenticación avanzada** (RNF-10 + RNF-01 parte socios y Dueño) y entregó **gestión de eventos** (RF-15). Adicionalmente se **adelantó RF-16** desde la Iteración 6: el centro de notificaciones in-system del socio se construyó sobre la base de eventos ya hecha, aprovechando que el envío por email y el repositorio de socios por unidad ya estaban resueltos.

Los cinco entregables de esta iteración son:

1. **Login de socios con Google (OAuth 2.0)** — el socio inicia sesión con su cuenta de Google sin gestionar contraseña propia (RN-19). Reusa el JWT y el sistema de roles/permisos existente.
2. **MFA TOTP para empleados** — segundo factor obligatorio con app autenticadora (Google/Microsoft Authenticator), enrolment en el primer login, códigos de recuperación y reset de emergencia por admin.
3. **Rol "Dueño"** — rol de sistema entre Admin y los roles dinámicos, con **filtrado automático server-side por unidades asignadas** sobre socios, clases, horarios, cuotas, empleados y eventos.
4. **RF-15 Eventos** — ABM de eventos por sede para el admin, notificación por email a los socios de la unidad, vista de "Próximos eventos" en el portal.
5. **RF-16 Notificaciones in-system** *(adelantado desde IT6)* — entidad `Notificacion` persistida por socio, campana con badge en el portal e inbox; se enganchó en cuotas, inscripciones, cambios/cancelaciones de clase y eventos.

## Tareas planificadas

Funcionalidades a implementar:

- **RNF-10 (OAuth)** — Login de socios con Google OAuth 2.0, sin contraseña propia.
- **RNF-10 (MFA)** — MFA TOTP (Google/Microsoft Authenticator) para empleados.
- **RNF-01 (rol Dueño)** — Rol de sistema "Dueño" con filtro por unidades asignadas.
- **RF-15** — Gestión de eventos (ABM por sede + notificación a socios).
- **RF-16** — Notificaciones al socio dentro del sistema *(adelantado desde IT6).*

Tareas técnicas de base:

- Vincular cuentas de Google al socio: identificador de Google asociado al usuario y migración de base de datos correspondiente.
- Soporte de MFA en el empleado: clave TOTP cifrada en reposo, indicador de MFA activo, contador de intentos fallidos para bloqueo y tabla de códigos de recuperación. La firma del "ticket temporal" del segundo factor usa una clave separada de la del token de sesión.
- Carga inicial del rol "Dueño" como rol de sistema con sus permisos operativos (sin acceso a Auditoría).
- Servicio interno que resuelve, por cada request, el conjunto de sedes visibles para el usuario; los listados del admin filtran automáticamente por ese conjunto.
- Módulo "Eventos" en el sistema de permisos, entidad `Evento` y plantilla de email para la notificación al socio.
- Centro de notificaciones in-system: entidad `Notificacion`, servicio interno de creación y guardado de la notificación en una transacción propia (si falla, no rompe la operación de negocio).

## Configuración de Google Cloud

Para que el botón "Iniciar sesión con Google" funcione, GymFlow está registrado como aplicación en la consola de Google Cloud. El proyecto OAuth está hoy en estado **"Prueba"**: solo los correos cargados como "usuarios de prueba" pueden autenticarse. Cuando la app pase al estado **"En producción"** (proceso de verificación de Google), cualquier cuenta de Google podrá usar el botón sin pre-autorización.

Aspectos a destacar:

- Los orígenes autorizados incluyen tanto el entorno de desarrollo local como la URL de producción en Azure.
- El identificador público de la aplicación (Client ID) se versiona junto al código; **no** se usa una clave secreta del lado del servidor, porque el único objetivo de esta integración es autenticar al socio (no acceder a datos de su cuenta de Google).

## ¿Qué se implementó?

Funcionalidades implementadas:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RNF-10 (OAuth) | CU-05 — flujo alternativo | Completado | Login de socios con cuenta de Google. El backend valida el token de Google (firma, expiración y origen) usando la biblioteca oficial de Google. Solo entran socios pre-registrados activos; mensaje genérico para no revelar qué correos existen. |
| RNF-10 (MFA) | CU-05 — flujo principal | Completado | Segundo factor obligatorio para todos los empleados, basado en código de 6 dígitos generado por una app autenticadora (Google/Microsoft Authenticator). Alta del MFA en el primer login (con QR + clave manual + 10 códigos de recuperación de un solo uso). Login en dos pasos. Reset de emergencia por admin. Bloqueo tras 5 códigos fallidos. |
| RNF-01 (Dueño) | CU-07 — variante | Completado | Nuevo rol de sistema "Dueño" entre Admin y los roles dinámicos. Tiene permisos operativos sobre socios, planes, clases, cuotas, empleados, eventos y vista de sedes (no accede a Auditoría). Solo el Admin puede crear Dueños. El sistema filtra automáticamente todo lo que el Dueño ve por las sedes que tiene asignadas. |
| RF-15 | CU-08 — Gestión de Eventos | Completado | ABM de eventos por sede para el admin (con baja lógica), notificación por email a los socios de la sede al crear el evento, acción manual de re-notificar, y vista de "Próximos eventos" en el portal del socio. Se agrega un nuevo módulo "Eventos" al sistema de permisos. |
| RF-16 | CU-09 — Notificaciones in-system | Completado *(adelantado de IT6)* | Centro de notificaciones dentro de la app para el socio. Tipos cubiertos: recordatorio de cuota, cambio de horario, cancelación de clase, confirmación de inscripción y evento nuevo. Campana con contador de no leídas e inbox en el portal. El guardado de cada notificación se hace en una transacción propia: si falla, la operación de negocio que la disparó (p. ej. la inscripción) queda confirmada igual. |

Requerimientos no funcionales implementados:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RNF-05 | Seguridad — MFA | Completado | La clave TOTP del empleado se guarda cifrada en la base con un algoritmo simétrico estándar (AES-GCM), con una clave de cifrado dedicada. Los códigos de recuperación se almacenan hasheados y son de un solo uso. El "ticket temporal" del segundo factor se firma con una clave distinta de la del token de sesión; si alguien lo intenta usar contra otro endpoint de la API, el sistema lo rechaza automáticamente. |
| RNF-05 | Seguridad — Dueño | Completado | El filtro por sedes se resuelve en el servidor a partir de la identidad del usuario (no de lo que mande el cliente). Si el Dueño intenta consultar una sede que no tiene asignada, el sistema devuelve resultado vacío sin error. Durante la revisión se detectó y cerró una fuga entre sedes en el listado de cuotas. |
| RNF-11 | Auditoría | Completado | MFA: se auditan activación, verificación de código, uso de código de recuperación y reset de emergencia. Eventos: se audita la creación (con conteo de emails enviados/fallidos), modificación, baja y re-notificación. El login con Google también deja registro de auditoría. |

## Tareas pendientes

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RF-16 (canal email) | — | Diferido a IT6 | Unificación del canal email con el centro de notificaciones — IT6. Los emails ya se envían; falta que pasen por la misma infraestructura que hoy maneja la campanita del portal. |
| Publicación OAuth | — | Operativo | El proyecto OAuth de Google sigue en modo **Prueba**: solo los correos cargados como usuarios de prueba pueden autenticarse. La publicación se hará junto con el deploy productivo. |

## Pantallas implementadas

**Pantalla Login con Google (botón en el Login)**

*(captura de pantalla)*

**Ruta:** /login

**Descripción:** Botón oficial "Iniciar sesión con Google" provisto por el servicio oficial de Google, con divisor "o" debajo del formulario de email + contraseña. El login tradicional convive con el botón de Google. Los errores de autenticación con Google se muestran en el mismo lugar que los del login normal.

**Pantalla MFA — Enrolment (empleado, primer login)**

*(captura de pantalla)*

**Ruta:** /auth/mfa/setup

**Descripción:** Tras el primer login del empleado, el sistema muestra un código QR y una clave manual equivalente, junto con un input para confirmar con el primer código de la app autenticadora. Al confirmar, se muestran los 10 códigos de recuperación de un solo uso (visibles una única vez) y se completa el login.

**Pantalla MFA — Verificación en login (empleado)**

*(captura de pantalla)*

**Ruta:** /auth/mfa/verify

**Descripción:** Pantalla intermedia que aparece después de validar el correo y la contraseña: input de 6 dígitos para el código de la app autenticadora y enlace "Usar código de recuperación". Bloqueo tras 5 intentos fallidos consecutivos.

**Pantalla Reset MFA (admin)**

*(captura de pantalla)*

**Ruta:** /admin/empleados (acción dentro del detalle del empleado)

**Descripción:** Acción del admin para desactivar MFA de un empleado (caso pérdida de dispositivo / códigos). El empleado debe volver a enrolarse en el siguiente login. La acción se audita.

**Pantalla Selector de Sede + Rol Dueño (alta de empleado)**

*(captura de pantalla)*

**Ruta:** /admin/empleados/nuevo

**Descripción:** El formulario de alta/edición de empleado ahora permite asignar **unidades** (multi-select) y el rol "Dueño" entre los roles seleccionables (solo si el creador es Admin). Un empleado con rol Dueño requiere ≥1 unidad asignada.

**Pantalla Gestión de Eventos (admin)**

*(captura de pantalla)*

**Ruta:** /admin/eventos

**Descripción:** Listado de eventos por sede (filtro de sede obligatorio para Dueño, opcional para Admin), con acciones crear/editar/cancelar/notificar. Toggle "incluir cancelados" para ver bajas lógicas. El formulario pide título, descripción, fecha (no pasada) y unidad.

**Pantalla Próximos Eventos (portal del socio)**

*(captura de pantalla)*

**Ruta:** /portal/eventos

**Descripción:** Vista de solo lectura de los próximos eventos de la/s sede/s del socio (las sedes se resuelven en el servidor desde la sesión del socio), ordenados por fecha ascendente. Solo se muestran eventos activos cuya fecha aún no pasó.

**Pantalla Campana de Notificaciones (portal del socio)**

*(captura de pantalla)*

**Ruta:** componente global en `/portal/*`

**Descripción:** Icono de campana en la cabecera del portal con contador de no-leídas (se actualiza periódicamente). Al hacer clic, abre un panel con las últimas notificaciones (recordatorios de cuota, cambios de horario, cancelaciones de clase, confirmaciones de inscripción, eventos nuevos). Marcar como leída actualiza el contador en el momento.

## Estructura de API --- endpoints implementados

| **Método** | **Endpoint** | **Descripción** | **Auth** |
|-|-|-|-|
| POST | `/api/auth/google` | Login de socio con cuenta de Google; devuelve el mismo paquete de respuesta que el login con email y contraseña | Anónimo |
| POST | `/api/auth/login` | Login email + contraseña. Si el usuario es empleado, en vez del token de sesión devuelve un ticket temporal para pasar por el segundo factor | Anónimo |
| POST | `/api/auth/mfa/setup` | Genera el QR y la clave manual para que el empleado configure la app autenticadora (sin activar MFA todavía) | Ticket temporal (alta de MFA) |
| POST | `/api/auth/mfa/activate` | Valida el primer código de la app, genera 10 códigos de recuperación, activa MFA y emite el token de sesión | Ticket temporal (alta de MFA) |
| POST | `/api/auth/mfa/verify` | Valida el código de 6 dígitos del login y emite el token de sesión | Ticket temporal (login) |
| POST | `/api/auth/mfa/recovery` | Valida un código de recuperación (de un solo uso) y emite el token de sesión | Ticket temporal (login) |
| POST | `/api/empleados/{id}/mfa/reset` | El admin resetea el MFA de un empleado y lo fuerza a configurarlo de nuevo | Empleado con permiso de escritura en Empleados |
| GET | `/api/eventos?unidadId=&incluirInactivos=` | Listar eventos del admin, con filtro por sede | Permiso de lectura en Eventos |
| GET | `/api/eventos/{id}` | Detalle de evento | Permiso de lectura en Eventos |
| POST | `/api/eventos` | Crear evento (notifica por email a los socios de la sede) | Permiso de escritura en Eventos |
| PUT | `/api/eventos/{id}` | Actualizar evento | Permiso de escritura en Eventos |
| DELETE | `/api/eventos/{id}` | Cancelar evento (baja lógica) | Permiso de escritura en Eventos |
| POST | `/api/eventos/{id}/notificar` | Re-enviar la notificación por email a los socios de la sede | Permiso de escritura en Eventos |
| GET | `/api/portal/eventos` | Próximos eventos de la/s sede/s del socio | Socio (portal) |
| GET | `/api/portal/notificaciones?soloNoLeidas=&take=` | Buzón del socio | Socio (portal) |
| GET | `/api/portal/notificaciones/no-leidas/count` | Contador para la campanita del portal | Socio (portal) |
| POST | `/api/portal/notificaciones/{id}/leer` | Marcar una notificación como leída (verifica que sea del socio) | Socio (portal) |
| GET | `/api/auth/me` | Datos del usuario autenticado. Para empleados con rol Dueño, incluye el conjunto de sedes asignadas | Usuario autenticado |

## Casos de uso extendidos --- Iteración 5

### CU-05: Autenticación con Google (flujo alternativo --- Socio)

| *Campo* | |
|-|-|
| *Nombre* | Login de socio con Google OAuth 2.0 |
| *Actor principal* | Socio |
| *Precondición* | Socio activo pre-registrado con correo coincidente al de Google. Correo cargado como usuario de prueba en Google Cloud mientras el proyecto OAuth esté en modo Prueba. |
| *Postcondición* | Socio autenticado con el token de sesión de GymFlow. En el primer login, el identificador de Google queda vinculado al socio. Se audita el inicio de sesión. |

**Flujo principal:**

1. El socio hace clic en "Iniciar sesión con Google" en el Login.
2. El servicio oficial de Google autentica al socio y devuelve un token de identidad al navegador.
3. El frontend envía ese token al backend de GymFlow.
4. El backend valida el token (firma, expiración y que el token sea para esta aplicación) y verifica que el correo corresponda a un **socio activo con rol**.
5. El backend emite su propio token de sesión con rol Socio y el frontend redirige al portal.

**Flujos de excepción:**

- **E3 --- Correo no registrado / socio inactivo:** *"No encontramos una cuenta asociada a este correo."* (mismo mensaje para inexistente, inactivo o sin rol — no se revela en qué estado está la cuenta).
- **Token de Google inválido, expirado, emitido para otra aplicación o correo no verificado:** rechazo con mensaje genérico.

### CU-05: Autenticación con MFA TOTP (flujo principal --- Empleado)

| *Campo* | |
|-|-|
| *Nombre* | Login de empleado con segundo factor TOTP |
| *Actor principal* | Empleado (admin, profesor, Dueño u otro rol interno) |
| *Precondición* | Empleado activo. Para enrolment: primer login tras el release o tras un reset. Para verificación: MFA ya activado. |
| *Postcondición* | Empleado autenticado con el token de sesión de GymFlow. El paso MFA realizado queda auditado. |

**Flujo principal (MFA ya activado):**

1. El empleado ingresa correo y contraseña en el Login.
2. El backend valida la contraseña. Al ser empleado, en vez de emitir el token de sesión, devuelve un **ticket temporal** de corta duración (aprox. 5 minutos) que solo sirve para pasar por el segundo factor.
3. El frontend redirige a la pantalla del código de 6 dígitos.
4. El empleado abre la app autenticadora, lee el código y lo ingresa.
5. El backend valida el código (con una pequeña tolerancia para compensar el desfasaje del reloj del celular) y, si es correcto, emite el token de sesión real.

**Flujo alternativo --- Alta de MFA (primer login):**

1. Tras email + contraseña, el backend responde con un ticket temporal indicando que el empleado todavía no configuró MFA.
2. El frontend muestra el QR y la clave manual generados por el backend. El secreto se guarda cifrado en la base, marcado como "no activado".
3. El empleado escanea el QR con la app, ingresa el primer código generado y lo envía.
4. El backend valida el código, genera **10 códigos de recuperación** (los devuelve en pantalla una sola vez y los guarda hasheados), activa el MFA y emite el token de sesión.

**Flujo alternativo --- Recuperación (sin acceso a la app):**

1. En la pantalla de verificación, el empleado elige "Usar código de recuperación" y envía uno de los 10 códigos que se le entregaron al activar MFA.
2. El backend valida el código, lo marca como usado (no se puede reutilizar) y emite el token de sesión.

**Flujo alternativo --- Reset de emergencia (admin):**

1. El admin abre el detalle del empleado y ejecuta "Resetear MFA".
2. El backend borra la clave TOTP y los códigos de recuperación, y desactiva el MFA del empleado. La acción queda auditada.
3. En el próximo login el empleado pasa de nuevo por el alta de MFA.

**Flujos de excepción:**

- **E1 --- Código de la app inválido:** rechazo; no se emite token de sesión. Tras 5 intentos fallidos consecutivos, el paso MFA del empleado queda bloqueado en la base.
- **E2 --- Ticket temporal expirado, con propósito incorrecto o firma inválida:** rechazo. El ticket temporal se firma con una clave separada de la del token de sesión, así que si alguien intenta usarlo contra otra parte de la API es rechazado automáticamente.
- **E3 --- Código de recuperación ya usado:** rechazo.

### CU-07: Gestión de Empleados --- Rol Dueño y unidades asignadas

| *Campo* | |
|-|-|
| *Nombre* | Alta/edición de empleado con rol Dueño y unidades |
| *Actor principal* | Administrador |
| *Precondición* | Admin autenticado. Existen unidades creadas. Solo el Admin puede crear/editar Dueños. |
| *Postcondición* | Empleado con rol Dueño asociado a ≥1 sede. Su sesión incluye el conjunto de sedes asignadas. Cualquier listado de socios, clases, horarios, cuotas, empleados o eventos queda filtrado en el servidor a esas sedes. |

**Flujo principal:**

1. El admin accede a "Empleados" → "Nuevo" (o edita uno existente).
2. El sistema presenta el selector de rol (incluye "Dueño") y el selector multi-sede.
3. El admin selecciona el rol Dueño y al menos una sede.
4. El sistema valida las reglas (Dueño requiere ≥1 sede; solo Admin puede asignar Dueño), guarda la asignación de sedes y deja registro de auditoría.

**Comportamiento posterior (filtrado automático por sede):**

- En cada request del Dueño, el backend resuelve a partir de su sesión cuáles son las sedes que tiene asignadas. Para el Admin no hay filtro: ve todo.
- Todos los listados del panel del admin (socios, clases, horarios, cuotas, empleados, eventos) aplican ese filtro automáticamente en la consulta a la base, sin depender de lo que envíe el cliente.
- Si el Dueño intenta consultar explícitamente una sede que no tiene asignada, el sistema devuelve resultado vacío (no error), evitando filtrar información de manera lateral.

### CU-08: Gestión de Eventos

| *Campo* | |
|-|-|
| *Nombre* | ABM de eventos por sede con notificación a socios |
| *Actor principal* | Administrador / Dueño (acotado a sus unidades) |
| *Precondición* | Actor con permiso de escritura en el módulo Eventos. La sede destino existe. |
| *Postcondición* | Evento creado, actualizado o cancelado. Email enviado a los socios activos de la sede (envío "best-effort": si alguno falla no rompe la operación). Auditoría con conteo de emails enviados y fallidos. Notificación in-system creada por cada socio. |

**Flujo principal --- Crear evento:**

1. El admin (o Dueño) accede a "Eventos" → "Nuevo".
2. Completa título, descripción, fecha (no pasada) y sede (el Dueño solo ve sus sedes asignadas).
3. El sistema valida que la sede exista y que la fecha sea futura, y **guarda y audita el evento ANTES de enviar emails** (así, aunque el envío de emails falle, el evento queda creado).
4. El sistema obtiene del repositorio el listado de socios activos de la sede.
5. El sistema envía los emails en paralelo usando la plantilla del evento, y cuenta cuántos se enviaron correctamente y cuántos fallaron para registrarlo en la auditoría.
6. El sistema crea, para cada socio, una notificación in-system del tipo "Evento nuevo" para que aparezca en su campanita del portal.

**Flujos alternativos:**

- **Actualizar:** mismo flujo, sin re-notificación automática (si se quiere re-avisar, se usa la acción manual "Notificar").
- **Cancelar:** baja lógica; el evento deja de aparecer en el portal del socio pero queda en la base para consulta.
- **Re-notificar manualmente:** reenvía email a los socios activos de la sede; se audita.

**Portal del socio:** el endpoint correspondiente devuelve los próximos eventos activos de las sedes del socio, ordenados por fecha. Las sedes se resuelven en el servidor desde la sesión del socio, no se confía en lo que mande el frontend.

### CU-09: Notificaciones in-system del socio

| *Campo* | |
|-|-|
| *Nombre* | Inbox de notificaciones del socio dentro del sistema |
| *Actor principal* | Socio |
| *Precondición* | Socio autenticado en el portal. |
| *Postcondición* | El socio ve sus notificaciones con tipo, título, mensaje y fecha. Puede marcar como leídas. El badge de la campana refleja el conteo de no-leídas. |

**Comportamiento:**

- Cada vez que ocurre uno de los eventos de negocio listados abajo, **después** de que la operación principal se haya guardado, el sistema crea la notificación in-system correspondiente. El guardado de la notificación se hace en su propia transacción: si falla, la operación de negocio que la disparó queda confirmada igual.

| Disparador | Tipo de notificación | Destinatario |
|---|---|---|
| Recordatorio diario de cuota (job automático) y notificación manual de cuota | Recordatorio de cuota | el socio de la cuota |
| Cambio de horario de una clase | Cambio de horario | socios inscriptos en ese horario |
| Cancelación de una clase | Cancelación de clase | socios inscriptos en los horarios de la clase |
| Inscripción a una clase | Confirmación de inscripción | el socio inscripto |
| Creación de un evento (RF-15) | Evento nuevo | socios activos de la sede |

- El portal consulta periódicamente el contador de no-leídas para mantener el indicador de la campanita actualizado.
- "Marcar como leída" verifica que la notificación pertenezca al socio autenticado y es idempotente (marcarla dos veces no cambia la fecha original de lectura).

## Reuniones con el cliente

No se realizaron reuniones formales con el cliente durante esta iteración. El alcance de autenticación avanzada y los entregables de eventos/notificaciones se trabajaron a partir de las definiciones del documento de requerimientos y los specs versionados en el repositorio.

Funcionalidades a presentar en la próxima reunión:

- Login con Google del socio (flujo demo con un usuario de prueba).
- MFA en login de empleado (enrolment + verificación + recuperación).
- Rol Dueño operando sobre un subset de sedes.
- Gestión de eventos con notificación por email + inbox in-system del socio.

## Pruebas automatizadas

Suite backend en verde (0 fallos). Cobertura agregada en esta iteración:

**Login con Google:**

- *Dominio:* el identificador de Google del socio arranca vacío; la vinculación es idempotente y no pisa un vínculo existente; argumento vacío o nulo lanza error.
- *Application:* login exitoso; rechazo cuando el socio no existe, está inactivo o sin rol; rechazo si el token es inválido o el correo no está verificado; el primer login vincula y persiste, el segundo no re-vincula.
- *Infrastructure:* tokens malformados o con firma inválida son rechazados de manera controlada, sin propagar excepción.

**MFA TOTP:**

- *Dominio:* activación y desactivación del MFA; contador de intentos fallidos y bloqueo; consumo de código de recuperación de un solo uso.
- *Application:* el alta genera el secreto y lo persiste como "no activado"; la activación genera los 10 códigos hasheados y emite el token de sesión; la verificación acepta códigos válidos dentro de la ventana de tolerancia y los rechaza fuera de ella; los intentos fallidos suman y al quinto bloquean; el código de recuperación se acepta sólo si está vigente; el reset del admin invalida el secreto y los códigos.
- *Infrastructure:* el componente de cifrado/descifrado del secreto recupera el valor original; el generador y validador de códigos TOTP acepta los códigos correctos en su ventana y rechaza los demás; el ticket temporal de MFA se valida con su clave dedicada y rechaza cualquier token firmado con la clave del token de sesión.

**Rol Dueño:**

- *Application:* el resolver de sedes devuelve "sin restricción" para el Admin y el conjunto de sedes para el Dueño; los listados de socios, clases, cuotas y empleados filtran correctamente cuando se les pasa el conjunto; si se pide una sede fuera del conjunto permitido, el resultado es vacío.
- Los tests existentes se actualizaron para reflejar la nueva firma de los repositorios (que ahora aceptan el conjunto de sedes habilitadas).

**Eventos (RF-15):**

- *Dominio:* el constructor valida el título; las operaciones de actualizar, cancelar y reactivar funcionan como se espera.
- *Application:* el evento se guarda y audita antes del envío de emails (test simulando que el servicio de email falla — el evento queda creado igual); el conteo de enviados/fallidos llega a la auditoría; la cancelación es baja lógica; la re-notificación reenvía correctamente; la consulta del portal filtra por las sedes del socio y devuelve sólo eventos futuros activos.

**Notificaciones (RF-16):**

- *Dominio:* el constructor valida título y mensaje; marcar como leída es idempotente.
- *Application:* creación, consulta del inbox, validación de pertenencia al marcar como leída; el parámetro de cantidad del listado queda acotado a un rango razonable (1 a 100).
- *Infrastructure:* el servicio interno de notificaciones, al usar una transacción propia, no rompe la operación de negocio cuando su guardado falla.

## Pruebas de API realizadas con Postman

Se ampliaron los tests automatizados de la colección Postman (`GymFlow API Tests.postman_collection.json`) para cubrir los endpoints nuevos de la iteración: login con Google, reset de MFA, eventos (RF-15) y notificaciones in-system del socio (RF-16). La suite completa quedó en **244 aserciones, 0 fallos**.

### Tests agregados

**Auth con Google (RNF-10) --- 2 tests**

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| 400 - Token de Google vacío | POST | `/api/auth/google` | Rechazo cuando falta el `idToken` |
| 401 - Token de Google malformado/inválido | POST | `/api/auth/google` | Rechazo con mensaje genérico ante token no válido (sin revelar detalle) |

> El flujo feliz del login con Google requiere un `id_token` real firmado por Google, por lo que se valida de forma manual (ver Prueba 5.1) y la colección cubre únicamente los caminos de error.

**MFA --- Reset por admin (RNF-10) --- 3 tests**

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| 400 - Admin no puede resetear su propio MFA | POST | `/api/empleados/{id}/mfa/reset` | El admin no puede auto-resetearse el segundo factor |
| 404 - Reset MFA empleado inexistente | POST | `/api/empleados/{id}/mfa/reset` | Error 404 con empleado inexistente |
| 401 - Reset MFA sin token | POST | `/api/empleados/{id}/mfa/reset` | Rechazo sin autenticación |

**Eventos (RF-15) --- 11 tests**

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| Setup - Capturar unidad para eventos | GET | `/api/unidades` | Toma la sede del socio seed para encadenar los tests |
| 201 - Crear evento | POST | `/api/eventos` | Alta con estructura `EventoDto` y `estaActivo=true` |
| 400 - Crear evento con fecha pasada | POST | `/api/eventos` | Rechazo por fecha no futura |
| 200 - Listar eventos (por unidad) | GET | `/api/eventos?unidadId=` | Array que incluye el evento recién creado |
| 200 - Detalle de evento | GET | `/api/eventos/{id}` | Devuelve el evento creado |
| 404 - Detalle evento inexistente | GET | `/api/eventos/{id}` | Error 404 con ID inexistente |
| 200 - Actualizar evento | PUT | `/api/eventos/{id}` | Edición de título reflejada en la respuesta |
| 200 - Re-notificar evento | POST | `/api/eventos/{id}/notificar` | Reenvío con mensaje resumen de envío |
| 204 - Cancelar evento (baja lógica) | DELETE | `/api/eventos/{id}` | Baja lógica del evento |
| 200 - Listar incluyendo cancelados | GET | `/api/eventos?incluirInactivos=true` | El evento cancelado aparece con `estaActivo=false` |
| 401 - Listar eventos sin token | GET | `/api/eventos` | Rechazo sin autenticación |

**Notificaciones in-system del socio (RF-16) --- E2E --- 11 tests**

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| Setup - Login socio (sin MFA) | POST | `/api/auth/login` | El socio recibe sesión directa (sin segundo factor) |
| Setup - Capturar count inicial de no-leídas | GET | `/api/portal/notificaciones/no-leidas/count` | Conteo inicial del badge |
| Trigger - Admin crea evento en la sede del socio | POST | `/api/eventos` | Gatilla la notificación in-system |
| 200 - Count de no-leídas incrementó tras el evento | GET | `/api/portal/notificaciones/no-leidas/count` | El conteo sube al menos 1 respecto al inicial |
| 200 - Listar notificaciones (soloNoLeidas) y capturar id | GET | `/api/portal/notificaciones?soloNoLeidas=true` | Hay ≥1 no leída; se captura su `id` |
| 200 - Marcar notificación como leída | POST | `/api/portal/notificaciones/{id}/leer` | Marca como leída con mensaje de confirmación |
| 200 - Marcar como leída es idempotente | POST | `/api/portal/notificaciones/{id}/leer` | Segundo POST también responde 200 |
| 404 - Marcar notificación inexistente | POST | `/api/portal/notificaciones/{id}/leer` | Error 404 con ID inexistente |
| 401 - Listar notificaciones sin token | GET | `/api/portal/notificaciones` | Rechazo sin autenticación |
| 200 - Portal eventos del socio | GET | `/api/portal/eventos` | Solo eventos futuros y activos de la sede del socio |
| Cleanup - Cancelar evento del trigger | DELETE | `/api/eventos/{id}` | Limpieza del evento creado para el E2E |

### Tests modificados

La obligatoriedad del segundo factor (MFA) para empleados cambió el contrato del login: `/api/auth/login` ya no devuelve el token de sesión directamente para empleados, sino un **ticket temporal** que debe completar el segundo factor. Esto obligó a actualizar todos los logins de empleado de la colección.

| **Test / área** | **Método** | **Endpoint** | **Cambio aplicado** |
|-|-|-|-|
| 200 - Login exitoso (Admin) - paso 1 credenciales | POST | `/api/auth/login` | Valida el nuevo contrato `LoginResultado` (`requiereMfa`, `mfaToken`, `sesion`) |
| 200 - Login exitoso (Admin) - paso 2 MFA | POST | `/api/auth/mfa/*` | Completa el segundo factor por código TOTP (setup+activate la primera vez, verify después) |
| 200 - Obtener perfil autenticado | GET | `/api/auth/me` | Sigue validando la presencia de `unidadIds` (no-null para rol Dueño) |
| Logins de empleado en setup/cleanup (RNF-01, Inscripciones, Empleados) | POST | `/api/auth/login` | Migrados a un helper de login MFA-aware compartido |

### Resumen de resultados

| **Módulo** | **Tests** | **Resultado** |
|-|-|-|
| Auth Google (RNF-10) | 2 | Pasaron |
| MFA - Reset por admin (RNF-10) | 3 | Pasaron |
| Eventos (RF-15) | 11 | Pasaron |
| Notificaciones Portal (RF-16) - E2E | 11 | Pasaron |
| Login MFA + helper compartido (modificados) | resto de la colección | Pasaron |
| **Total de la colección** | **244 aserciones** | **0 fallos** |


## Pruebas funcionales de frontend

### Prueba 5.1 --- Login de socio con Google (correo registrado)

*(captura de pantalla)*

**Pasos:**

1. En el Login, hacer clic en "Iniciar sesión con Google".
2. Elegir una cuenta de Google cuyo correo esté registrado como socio activo (y cargado como usuario de prueba en Google Cloud).

**Resultado esperado:** El socio inicia sesión y es redirigido al portal, sin haber ingresado contraseña.

**Descripción:** Verifica el flujo feliz del login con Google y la emisión del token de sesión propio, reusando el sistema de roles existente.

### Prueba 5.2 --- Login con Google de correo no registrado

*(captura de pantalla)*

**Pasos:**

1. En el Login, hacer clic en "Iniciar sesión con Google".
2. Elegir una cuenta de Google cuyo correo **no** corresponda a un socio activo.

**Resultado esperado:** Se muestra el mensaje *"No encontramos una cuenta asociada a este correo."* y el login no se completa.

**Descripción:** Verifica el rechazo controlado y que no se crean cuentas automáticamente.

### Prueba 5.3 --- Enrolment MFA del empleado (primer login)

*(captura de pantalla)*

**Pasos:**

1. Loguear como un empleado que aún no tiene MFA activado.
2. Tras email + contraseña, el sistema muestra QR + clave manual.
3. Escanear el QR con Google Authenticator e ingresar el primer código de 6 dígitos.

**Resultado esperado:** Sistema muestra los 10 códigos de recuperación (una sola vez) y completa el login. En la próxima sesión, el login pide directamente el código TOTP.

**Descripción:** Verifica el enrolment, la persistencia del secreto cifrado y la generación de códigos de recuperación.

### Prueba 5.4 --- Verificación MFA y bloqueo tras 5 intentos

*(captura de pantalla)*

**Pasos:**

1. Loguear como empleado con MFA activado.
2. Ingresar 5 códigos incorrectos consecutivos en la pantalla de verificación.

**Resultado esperado:** El sistema bloquea el paso MFA del empleado. Los siguientes códigos correctos no autorizan hasta que un admin haga reset o se levante el bloqueo según la política.

**Descripción:** Verifica el bloqueo persistido contra fuerza bruta.

### Prueba 5.5 --- Código de recuperación (uso único)

*(captura de pantalla)*

**Pasos:**

1. En la pantalla de verificación, elegir "Usar código de recuperación".
2. Ingresar uno de los 10 códigos.
3. Intentar reutilizar el mismo código en un nuevo login.

**Resultado esperado:** El primer intento entra; el segundo es rechazado.

**Descripción:** Verifica uso único de los códigos de recuperación.

### Prueba 5.6 --- Reset de MFA por admin

*(captura de pantalla)*

**Pasos:**

1. Loguear como admin, ir al detalle de un empleado con MFA activado.
2. Ejecutar "Resetear MFA".
3. Loguear como ese empleado.

**Resultado esperado:** Tras el reset, el empleado vuelve a pasar por el enrolment (QR + códigos nuevos). Auditoría registra el reset.

**Descripción:** Verifica el flujo de emergencia para pérdida de dispositivo.

### Prueba 5.7 --- Rol Dueño ve solo sus unidades (socios)

*(captura de pantalla)*

**Pasos:**

1. Loguear como un Dueño con dos unidades asignadas (de tres existentes).
2. Navegar a "Socios" (listado).

**Resultado esperado:** Solo se muestran socios de las dos unidades del Dueño. Pedir `?unidadId=` de una tercera devuelve lista vacía, sin error.

**Descripción:** Verifica que el filtrado por sedes se aplique siempre en el servidor y que no haya fugas entre sedes.

### Prueba 5.8 --- Admin no se ve afectado por el filtro de unidades

*(captura de pantalla)*

**Pasos:**

1. Loguear como Admin.
2. Navegar a "Socios" sin filtro de sede.

**Resultado esperado:** Se muestran socios de todas las unidades (Admin = sin restricción).

**Descripción:** Verifica que para el Admin no se impone restricción por sedes.

### Prueba 5.9 --- Crear evento y notificar a los socios

*(captura de pantalla)*

**Pasos:**

1. Loguear como admin, ir a "Eventos" → "Nuevo".
2. Completar título, descripción, fecha futura, unidad.
3. Guardar.

**Resultado esperado:** El evento queda creado. Los socios activos de la unidad reciben un email y una notificación in-system (badge de la campana se incrementa). La auditoría registra el conteo enviados/fallidos.

**Descripción:** Verifica el flujo de creación con notificación dual (email + in-system) y que el evento se persista antes del envío de emails (si fallan, el evento queda creado igual).

### Prueba 5.10 --- Portal del socio: próximos eventos

*(captura de pantalla)*

**Pasos:**

1. Loguear como socio asignado a una unidad con eventos futuros.
2. Navegar a "Eventos" en el portal.

**Resultado esperado:** Listado de próximos eventos ordenado por fecha ascendente. No se ven eventos cancelados ni pasados.

**Descripción:** Verifica el endpoint de eventos del portal y el filtrado por las sedes del socio.

### Prueba 5.11 --- Campana de notificaciones e inbox

*(captura de pantalla)*

**Pasos:**

1. Loguear como socio con notificaciones nuevas (p.ej. tras crear un evento en su sede).
2. Observar el badge de la campana en el header.
3. Abrir el inbox y marcar una notificación como leída.

**Resultado esperado:** El badge muestra el conteo de no-leídas. Al marcar leída, el badge decrementa y la notificación queda con marca de leída.

**Descripción:** Verifica el centro de notificaciones in-system y los endpoints del portal asociados.

### Prueba 5.12 --- El login email + contraseña sigue intacto

*(captura de pantalla)*

**Pasos:**

1. Iniciar sesión como empleado con correo y contraseña (más MFA si aplica).

**Resultado esperado:** El login tradicional funciona sin cambios. El botón de Google convive con el formulario.

**Descripción:** Verifica la no regresión del login email + password tras agregar las vías de Google y MFA.

## Registro de tiempos

**Desarrollo -- tiempo por commit**

| **Hash** | **Fecha** | **Descripción** | **Tiempo (hs)** |
|:--:|----|----|:--:|
| 5a90c3d | 2026-06-15 | Merge PR #30: login Google + docs IT5 | 2.00 |
| 1e26202 | 2026-06-16 | Docs: spec y plan MFA TOTP para empleados | 1.00 |
| 09e4ab7 | 2026-06-16 | MFA: setup, activar, verificar y recuperacion | 0.75 |
| e6a868e | 2026-06-16 | MFA: AES-256-GCM + token firmado dedicado | 0.75 |
| b9c6579 | 2026-06-16 | MFA: UI enrolment, verificacion y reset | 0.75 |
| 9595898 | 2026-06-16 | MFA: login dos pasos + endpoints + reset API | 0.75 |
| 5b2589f | 2026-06-16 | MFA: persistencia (repo, EF config, migracion) | 0.75 |
| 36a233d | 2026-06-16 | Fix CI: build imagen en runner local | 0.75 |
| c0020d1 | 2026-06-16 | Docs: spec email confirmacion de pago + SMTP | 0.50 |
| a900a45 | 2026-06-16 | Docs deploy: activacion MFA y reset de emergencia | 0.25 |
| 472471a | 2026-06-17 | Docs: spec y plan rol Dueno | 1.00 |
| 674c214 | 2026-06-17 | Dueno: seed rol + resolver unidades visibles | 0.75 |
| 6d921bb | 2026-06-17 | Dueno: login devuelve unidadIds | 0.75 |
| fb8a95d | 2026-06-17 | Docs: spec y plan gestion eventos (RF-15) | 1.00 |
| cfdb247 | 2026-06-17 | Eventos: CRUD + notificacion email + portal | 0.75 |
| 64026d5 | 2026-06-17 | Docs: spec y plan centro notificaciones (RF-16) | 1.00 |
| a9458f5 | 2026-06-17 | Cambios en la documentacion | 1.00 |
| 443d13f | 2026-06-18 | Dueno: UI unidades, selector sede, filtro repos | 0.75 |
| 36bb3e1 | 2026-06-18 | Dueno: controllers y queries filtran por unidad | 0.75 |
| 2d6c38f | 2026-06-18 | Notificaciones: entidad, repo, endpoints, UI | 0.75 |
| a1d39fe | 2026-06-18 | Notificaciones: enganche en cuotas/clases/eventos | 0.75 |
| 56dcf7e | 2026-06-18 | Fix: cerrar fuga cross-unidad en cuotas | 0.50 |
| 4b0c8e1 | 2026-06-18 | CI: workflow manual configure-mfa (claves MFA) | 0.25 |
| 8cb4dd4 | 2026-06-18 | Dueno: permisos de Eventos + filtro unidad en eventos | 0.50 |
| eeef7d6 | 2026-06-18 | Cuotas: endpoints manuales para disparar jobs | 0.50 |
| 85b1689 | 2026-06-18 | RF-19: sitio web publico (5 paginas + SEO) | 0.75 |
| 8b75f95 | 2026-06-19 | Actualizacion de la documentacion iteracion 5 | 1.00 |
| c7661df | 2026-06-20 | Fix horarios: filtrar planes por sede/espacio | 0.50 |
| ad41a09 | 2026-06-20 | Eventos: mostrar destinatarios del correo | 0.50 |
| 84d3526 | 2026-06-20 | UI: formato prolijo de fechas y horas | 0.50 |
| 64397a2 | 2026-06-20 | Docs: inventario pruebas automatizadas it1-4 | 0.75 |
| 44621f7 | 2026-06-22 | Casos de uso documentados | 1.00 |
| 66fb4b3 | 2026-06-23 | Mejoras documentacion casos de uso | 1.00 |
| f92366e | 2026-06-23 | Avances de documentacion | 0.50 |
| dd49bfb | 2026-06-24 | Test API finalizado y documentado | 1.00 |
|  |  | **Subtotal Desarrollo** | **26.8** |

**Otras actividades**

| **Actividad** | **Tiempo (hs)** |
|----|:--:|
| Plan de testing - Frontend | 2 |
| Ejecución plan de testing - Frontend | 4 |
| Planificación Plan de testing - Endpoints en Postman | 2 |
| Ejecución Plan de testing - Endpoints en Postman | 2 |
| Documentación | 8 |
| **Subtotal Otras Actividades** | **18** |

**TOTAL HORAS - Iteración 5: 44.8**
