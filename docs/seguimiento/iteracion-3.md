---
title: DOCUMENTACION ITERACIÓN 3 FASE DE CONSTRUCCIÓN
tags:
  - seguimiento
related:
  - "[[seguimiento_index]]"
---

# DOCUMENTACION ITERACIÓN 3 FASE DE CONSTRUCCIÓN

**Iteración 3 --- Fase de Construcción (15/05/2026 -- 29/05/2026)**

## Descripción general

La tercera iteración se enfocó en la gestión de clases y horarios, permitiendo al administrador crear, editar, cancelar y reactivar clases, así como definir horarios semanales con validación de conflictos de sala. Se desarrolló la vista de calendario semanal para el administrador y la vista de horarios para el portal de socios. Adicionalmente, se implementó el flujo base de inscripción y cancelación de socios a clases (RF-10/RF-11), que será formalizado en la iteración 4.

## Tareas planificadas

Funcionalidades a implementar:

- **RF-08** — Gestionar clases: crear, editar y eliminar/cancelar clases con nombre, descripción, instructor, duración, cupo máximo y unidad.
- **RF-09** — Gestionar horarios semanales: definir horarios de clases con día, hora de inicio/fin y sala, contemplando validación de conflictos.

Requerimientos no funcionales:

- Arquitectura CQRS con Clean Architecture mantenida desde iteración 1.
- Log de auditoría (RNF-11) en todas las operaciones CRUD de clases y horarios.
- Notificaciones por email a socios inscriptos al cancelar una clase o modificar un horario.

Tareas técnicas de base:

- Crear entidades de dominio `Clase` y `HorarioClase` con validaciones.
- Crear migraciones EF Core para las tablas `Clases`, `HorariosClase` e `InscripcionesClase`.
- Implementar repositorios, commands, queries y DTOs.
- Desarrollar vistas de administración (CRUD de clases + calendario semanal de horarios).
- Desarrollar vista de portal de socios (horarios disponibles y mis inscripciones).
- Escribir tests unitarios de dominio y de casos de uso.

## ¿Qué se implementó?

Funcionalidades implementadas:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RF-08 | CU-06 — Gestión de Clases | Completado | CRUD completo de clases con cancelación lógica, reactivación, validación de capacidad vs inscripciones activas, notificación por email a inscriptos al cancelar y log de auditoría. |
| RF-09 | CU-06 — Gestión de Horarios | Completado | CRUD de horarios semanales con validación de conflicto de sala (mismo día, misma sala, solapamiento de hora), selección múltiple de días, notificación por email al modificar horarios y log de auditoría. |
| RF-10 (base) | CU-02 — Inscripción a Clase | Completado (parcial) | Flujo base de inscripción y cancelación de inscripción a clases implementado en backend y frontend. Formalización completa en iteración 4. |
| RF-11 (base) | CU-02 — Ver Mis Clases | Completado (parcial) | Página "Mis Inscripciones" en el portal del socio. Formalización completa en iteración 4. |

Requerimientos no funcionales implementados:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RNF-11 | Auditoría | Completado | Todas las operaciones de clases y horarios registran audit log con usuario, timestamp y detalle de la acción. Se registra también el resultado de envío de emails. |
| RNF-05 | Seguridad | Completado | Controladores protegidos con `[Authorize]` a nivel de clase. Permisos granulares por módulo (Clases.Lectura, Clases.Escritura, Clases.Modificacion, Clases.Eliminacion). |

## Tareas pendientes

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RF-10 | CU-02 — Inscripción a Clase | Para iteración 4 | Pulido de UX, validación de cuota al día como prerequisito, y testing completo. |
| RF-11 | CU-02 — Ver Mis Clases | Para iteración 4 | Mejoras de UX y testing. |

## Pantallas implementadas

**Pantalla Listado de Clases (Admin)**

*(captura de pantalla)*

**Ruta:** /admin/clases

**Descripción:** Vista principal del módulo de clases en el panel de administración. Muestra todas las clases en formato tabla con nombre, instructor, capacidad máxima, inscripciones activas, unidad, duración y estado (activa/cancelada). Permite filtrar por unidad e incluir clases inactivas. Acciones disponibles: editar, cancelar y reactivar.

**Pantalla Nueva Clase (Admin)**

*(captura de pantalla)*

**Ruta:** /admin/clases/nueva

**Descripción:** Formulario de creación de clase con campos: nombre, descripción, instructor, capacidad máxima, duración en minutos y unidad (seleccionable entre las unidades existentes).

**Pantalla Editar Clase (Admin)**

*(captura de pantalla)*

**Ruta:** /admin/clases/:id/editar

**Descripción:** Formulario de edición con los mismos campos que la creación. Valida que la capacidad máxima no se reduzca por debajo de las inscripciones activas.

**Pantalla Calendario de Horarios (Admin)**

*(captura de pantalla)*

**Ruta:** /admin/horarios

**Descripción:** Grilla semanal de horarios (7:00 a 23:00). Los bloques de horario se muestran con código de colores por clase. Permite crear, editar y eliminar horarios. En la creación se pueden seleccionar múltiples días a la vez (checkboxes). Filtro por unidad.

**Pantalla Horarios Portal (Socio)**

*(captura de pantalla)*

**Ruta:** /portal/horarios

**Descripción:** Vista de horarios disponibles para el socio, agrupados por día de la semana. Muestra nombre de clase, instructor, horario, sala y capacidad disponible (con badges de ocupación). Botones de "Inscribirme" y "Cancelar inscripción" por clase. Filtro por sede.

**Pantalla Mis Inscripciones (Socio)**

*(captura de pantalla)*

**Ruta:** /portal/mis-inscripciones

**Descripción:** Listado de las clases a las que el socio está inscripto, con opción de cancelar inscripción.

## Estructura de API --- endpoints implementados

| **Método** | **Endpoint** | **Descripción** |
|-|-|-|
| GET | `/api/clases` | Obtener todas las clases (filtro opcional por unidad e inclusión de inactivas) |
| GET | `/api/clases/{id}` | Obtener clase por ID |
| POST | `/api/clases` | Crear nueva clase |
| PUT | `/api/clases/{id}` | Actualizar datos de una clase |
| DELETE | `/api/clases/{id}` | Cancelar/desactivar una clase |
| PATCH | `/api/clases/{id}/reactivar` | Reactivar una clase cancelada |
| GET | `/api/horarios` | Obtener todos los horarios (filtro opcional por unidad) |
| GET | `/api/horarios/{id}` | Obtener horario por ID |
| POST | `/api/horarios` | Crear nuevo horario (con validación de conflicto de sala) |
| PUT | `/api/horarios/{id}` | Actualizar horario (notifica socios inscriptos por email) |
| DELETE | `/api/horarios/{id}` | Eliminar horario |
| POST | `/api/inscripciones` | Inscribir socio autenticado a una clase |
| GET | `/api/inscripciones/mis-inscripciones` | Obtener inscripciones del socio autenticado |
| DELETE | `/api/inscripciones/{id}` | Cancelar inscripción del socio autenticado |

## Caso de uso extendido --- Iteración 3

### CU-06: Gestión de Clases y Horarios

| *Campo* | |
|-|-|
| *Nombre* | Gestión de Clases y Horarios |
| *Actor principal* | Administrador |
| *Precondición* | Admin autenticado con rol Administrador. Existe al menos una unidad registrada. |
| *Postcondición* | Clase creada/modificada/cancelada con log de auditoría. Horarios asignados con validación de conflictos. Socios notificados ante cambios. |

**Flujo principal — Creación de Clase:**

1. Admin accede al módulo "Clases" → "Nueva Clase".
2. Sistema presenta formulario con campos: nombre, descripción, instructor, capacidad máxima, duración (minutos), unidad.
3. Admin completa los campos y confirma.
4. Sistema valida que la capacidad esté entre 1 y 500 y que los campos obligatorios estén completos.
5. Sistema registra la clase con estado "Activo", genera log de auditoría y muestra confirmación.
6. La clase queda visible en el calendario y en el portal de socios.

**Flujo alternativo — Asignación de Horarios Semanales:**

1. Admin accede al calendario semanal en "Horarios" → "Nuevo Horario".
2. Selecciona la clase, uno o más días de la semana (checkboxes), hora de inicio, hora de fin y sala (opcional).
3. Sistema verifica que no haya solapamiento de sala (misma sala, mismo día, horarios superpuestos).
4. Guarda los horarios y los muestra en la grilla semanal con código de colores.

**Flujo alternativo — Edición de Clase:**

1. Admin selecciona clase → "Editar".
2. Sistema carga formulario con datos actuales.
3. Admin modifica campos deseados.
4. Si se reduce la capacidad máxima, sistema valida que no quede por debajo de las inscripciones activas.
5. Sistema guarda cambios y registra auditoría.

**Flujo alternativo — Cancelación de Clase:**

1. Admin selecciona clase → "Cancelar".
2. Sistema solicita confirmación.
3. Sistema cancela la clase (baja lógica), cancela todas las inscripciones activas.
4. Envía emails de notificación a los socios inscriptos (en paralelo con `Task.WhenAll`).
5. Registra en auditoría el resultado del envío de cada email (exitoso/fallido).

**Flujo alternativo — Reactivación de Clase:**

1. Admin selecciona clase cancelada → "Reactivar".
2. Sistema reactiva la clase (estado "Activo").
3. Las inscripciones previas no se restauran — los socios deben reinscribirse.

**Flujo alternativo — Modificación de Horario:**

1. Admin selecciona horario existente → "Editar".
2. Modifica día, horario o sala.
3. Sistema re-valida conflictos de sala.
4. Si hay socios inscriptos a esa clase, envía notificación por email informando el cambio.
5. Registra auditoría con resultado de envío de emails.

**Flujos alternativos — Portal de Socios:**

1. Socio accede a "Horarios" → ve los horarios disponibles agrupados por día.
2. Selecciona "Inscribirme" en una clase con cupo disponible.
3. Sistema registra la inscripción y decrementa el cupo disponible.
4. La clase aparece en "Mis Inscripciones".
5. El socio puede "Cancelar inscripción" desde "Mis Inscripciones", liberando el cupo.

**Flujos de excepción:**

- **E1 — Conflicto de sala:** "La sala ya está ocupada en ese horario." Bloquea guardado.
- **E2 — Capacidad menor a inscripciones:** "No se puede reducir la capacidad por debajo de las inscripciones activas (X)."
- **E3 — Clase ya cancelada:** "La clase ya está cancelada."
- **E4 — Clase ya activa:** "La clase ya está activa."
- **E5 — Horario inválido:** Hora de fin debe ser posterior a hora de inicio.

## Reuniones con el cliente

Se realizó una reunión con el cliente (Maurice, propietario de Espacio Mora) donde se revisaron los avances de las iteraciones 2 y 3 de la plataforma GymFlow, destacando nuevas funcionalidades para la gestión de clases, pagos y roles de usuario.

Funcionalidades presentadas:

- **Gestión de Clases y Horarios:** Se presentó la creación de clases (funcional, aeróbica, etc.) asignadas a espacios específicos, con capacidad y duración definidas. Estas se visualizan en un cronograma donde se pueden asociar a horarios específicos, similar a la interfaz de Teams.
- **Módulo de Cuotas y Pagos:** El administrador puede filtrar socios por estado (al día, pendiente, vencido) y ver el historial individual de pagos. El sistema permite enviar notificaciones manuales y automáticas de vencimiento vía email.
- **Roles Personalizados:** Se mostró la capacidad de crear roles (como "profesor") con permisos específicos de lectura o escritura (crear, modificar, eliminar) para diferentes secciones del sistema.
- **Vista del Socio:** Los socios pueden ver sus cuotas, el cronograma de clases y solicitar modificaciones de sus datos personales, las cuales deben ser aprobadas manualmente por el administrador para mantener el control.
- **Optimización Móvil:** Se demostró que la aplicación ya está totalmente optimizada para su uso en dispositivos móviles.

Sugerencias y ajustes solicitados:

- **Inscripciones:** Se sugirió que los socios puedan inscribirse a las clases directamente pinchando en el cronograma y que se limite el tiempo de antelación para la inscripción.
- **Notificaciones:** El cliente solicitó ajustar el recordatorio de vencimiento a dos días antes (en lugar de seis o siete) y agregar saludos automáticos por cumpleaños. También se pidió notificar al socio cuando se ingrese su pago y darle una bienvenida al inscribirse.
- **Datos Médicos:** Se acordó incluir campos para la sociedad médica y el carné de salud, con alertas de vencimiento para este último.
- **Métodos de Pago:** Ante la preferencia del gimnasio por las transferencias bancarias para evitar comisiones, se propuso una función para que los socios adjunten el comprobante (PDF o imagen) directamente en la app para su validación manual.

## Pruebas de API realizadas con Postman

Se implementó una colección automatizada en Postman con **52 tests** organizados por módulo funcional. Cada test valida código de respuesta, estructura del DTO, reglas de negocio y tiempo de respuesta (< 500ms). La colección utiliza variables de entorno y scripts de pre/post-request para encadenar flujos completos (login → creación → verificación → cleanup).

### Clases (RF-08) — 11 tests

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| 201 - Crear Clase | POST | `/api/clases` | Estructura ClaseDto, estado activo, campos obligatorios |
| 400 - Crear Clase sin nombre | POST | `/api/clases` | Rechazo por campo requerido faltante |
| 200 - Lista Clases | GET | `/api/clases` | Array de ClaseDto con estructura completa |
| 200 - Lista Clases filtradas por unidad | GET | `/api/clases?unidadId=...` | Filtrado correcto por unidad |
| 200 - Lista Clases incluyendo inactivas | GET | `/api/clases?incluirInactivas=true` | Inclusión de clases canceladas |
| 200 - Clase por ID | GET | `/api/clases/{id}` | Estructura ClaseDto individual |
| 404 - Clase no encontrada | GET | `/api/clases/{id}` | Error 404 con ID inexistente |
| 200 - Actualizar Clase | PUT | `/api/clases/{id}` | Datos actualizados correctamente |
| 404 - Actualizar Clase inexistente | PUT | `/api/clases/{id}` | Error 404 con ID inexistente |
| 204 - Cancelar Clase | DELETE | `/api/clases/{id}` | Baja lógica exitosa |
| 409 - Cancelar Clase ya cancelada | DELETE | `/api/clases/{id}` | Conflicto al cancelar clase inactiva |
| 200 - Reactivar Clase | PATCH | `/api/clases/{id}/reactivar` | Reactivación exitosa |
| 409 - Reactivar Clase ya activa | PATCH | `/api/clases/{id}/reactivar` | Conflicto al reactivar clase ya activa |

### Horarios (RF-09) — 10 tests

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| 201 - Crear Horario (Lunes 18:00-19:00, Sala 1) | POST | `/api/horarios` | Estructura HorarioClaseDto, día, hora inicio/fin, sala |
| 409 - Crear Horario con conflicto de sala | POST | `/api/horarios` | Rechazo por solapamiento de sala en mismo día |
| 400 - Crear Horario con hora fin antes de inicio | POST | `/api/horarios` | Rechazo por rango horario inválido |
| 201 - Crear Horario sin sala (válido) | POST | `/api/horarios` | Horario creado con sala null |
| 200 - Lista Horarios | GET | `/api/horarios` | Array de HorarioClaseDto con estructura completa |
| 200 - Lista Horarios filtrados por unidad | GET | `/api/horarios?unidadId=...` | Filtrado correcto por unidad |
| 200 - Horario por ID | GET | `/api/horarios/{id}` | Estructura HorarioClaseDto individual |
| 404 - Horario no encontrado | GET | `/api/horarios/{id}` | Error 404 con ID inexistente |
| 200 - Actualizar Horario | PUT | `/api/horarios/{id}` | Día, hora y sala actualizados correctamente |
| 404 - Actualizar Horario inexistente | PUT | `/api/horarios/{id}` | Error 404 con ID inexistente |
| 204 - Eliminar Horario | DELETE | `/api/horarios/{id}` | Eliminación exitosa |
| 404 - Eliminar Horario inexistente | DELETE | `/api/horarios/{id}` | Error 404 con ID inexistente |

### Inscripciones (RF-10/RF-11) — 7 tests

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| 200 - Login como Socio | POST | `/api/auth/login` | Autenticación de socio para obtener token |
| 200 - Inscribir socio a clase | POST | `/api/inscripciones` | Inscripción exitosa con estructura de respuesta |
| 409 - Inscripción duplicada | POST | `/api/inscripciones` | Rechazo por inscripción ya existente |
| 404 - Inscribir a clase inexistente | POST | `/api/inscripciones` | Error 404 con clase inexistente |
| 200 - Obtener mis inscripciones | GET | `/api/inscripciones/mis-inscripciones` | Array con inscripciones del socio autenticado |
| 401 - Mis inscripciones sin autenticación | GET | `/api/inscripciones/mis-inscripciones` | Rechazo sin token |
| 204 - Cancelar inscripción | DELETE | `/api/inscripciones/{id}` | Cancelación exitosa |
| 404 - Cancelar inscripción inexistente | DELETE | `/api/inscripciones/{id}` | Error 404 con ID inexistente |

### Resumen de resultados

| **Módulo**                  | **Tests** | **Resultado**     |
| --------------------------- | --------- | ----------------- |
| Auth                        | 4         | Pasaron           |
| Clases (RF-08)              | 13        | Pasaron           |
| Horarios (RF-09)            | 12        | Pasaron           |
| Inscripciones (RF-10/RF-11) | 8         | Pasaron           |


## Pruebas funcionales de frontend

### Prueba 3.1 --- Crear clase desde panel de administración

*(captura de pantalla)*

**Pasos:**

1. Iniciar sesión como administrador.
2. Navegar a Clases → Nueva Clase.
3. Completar formulario con nombre, descripción, instructor, capacidad, duración y unidad.
4. Hacer clic en "Guardar".

**Resultado esperado:** La clase aparece en el listado de clases con estado "Activa".

**Descripción:** Se verifica la creación exitosa de una clase con todos los campos obligatorios completos. La clase queda visible en el listado y disponible para asignar horarios.

### Prueba 3.2 --- Cancelar clase con socios inscriptos

*(captura de pantalla)*

**Pasos:**

1. Seleccionar una clase activa con inscripciones.
2. Hacer clic en "Cancelar".
3. Confirmar la cancelación.

**Resultado esperado:** La clase pasa a estado "Cancelada", las inscripciones se cancelan y los socios reciben email de notificación.

**Descripción:** Se verifica que la cancelación lógica funciona correctamente: la clase no se elimina de la base de datos, las inscripciones activas se cancelan automáticamente y los socios inscriptos reciben notificación por email.

### Prueba 3.3 --- Crear horario con validación de conflicto de sala

*(captura de pantalla)*

**Pasos:**

1. Navegar a Horarios → Nuevo Horario.
2. Seleccionar una clase, día "Lunes", hora 18:00-19:00, sala "Sala 1".
3. Guardar exitosamente.
4. Intentar crear otro horario para otra clase en "Lunes", 18:30-19:30, sala "Sala 1".

**Resultado esperado:** El segundo intento falla con mensaje "La sala ya está ocupada en ese horario."

**Descripción:** Se verifica que el sistema detecta y previene solapamientos de horarios en la misma sala y mismo día.

### Prueba 3.4 --- Crear horario en múltiples días simultáneamente

*(captura de pantalla)*

**Pasos:**

1. Navegar a Horarios → Nuevo Horario.
2. Seleccionar una clase, marcar checkboxes de "Lunes", "Miércoles" y "Viernes".
3. Definir hora 10:00-11:00.
4. Guardar.

**Resultado esperado:** Se crean tres horarios (uno por cada día seleccionado) visibles en la grilla semanal.

**Descripción:** Se verifica la funcionalidad de creación múltiple de horarios, que permite definir un mismo bloque horario para varios días de la semana en una sola operación.

### Prueba 3.5 --- Inscripción a clase desde portal del socio

*(captura de pantalla)*

**Pasos:**

1. Iniciar sesión como socio.
2. Navegar a "Horarios" en el portal.
3. Seleccionar una clase con cupo disponible y hacer clic en "Inscribirme".

**Resultado esperado:** La inscripción se registra, el cupo disponible se decrementa, y la clase aparece en "Mis Inscripciones".

**Descripción:** Se verifica el flujo completo de inscripción desde la perspectiva del socio, incluyendo la actualización del cupo en tiempo real.

### Prueba 3.6 --- Cancelar inscripción desde Mis Inscripciones

*(captura de pantalla)*

**Pasos:**

1. Navegar a "Mis Inscripciones" como socio.
2. Seleccionar una inscripción activa.
3. Hacer clic en "Cancelar inscripción".

**Resultado esperado:** La inscripción se elimina, el cupo se libera y la clase desaparece de "Mis Inscripciones".

**Descripción:** Se verifica que el socio puede cancelar sus inscripciones y que el cupo se restaura correctamente.

### Prueba 3.7 --- Reducir capacidad por debajo de inscripciones activas

*(captura de pantalla)*

**Pasos:**

1. Editar una clase que tiene 5 inscripciones activas.
2. Intentar reducir la capacidad máxima a 3.
3. Hacer clic en "Guardar".

**Resultado esperado:** El sistema rechaza el cambio con mensaje de error indicando que no se puede reducir por debajo de las inscripciones activas.

**Descripción:** Se verifica la regla de negocio RN-24 que impide reducir el cupo por debajo de las inscripciones activas existentes.
