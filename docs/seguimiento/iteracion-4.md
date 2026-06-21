---
title: DOCUMENTACION ITERACIÓN 4 FASE DE CONSTRUCCIÓN
tags:
  - seguimiento
related:
  - "[[seguimiento_index]]"
---

# DOCUMENTACION ITERACIÓN 4 FASE DE CONSTRUCCIÓN

**Iteración 4 --- Fase de Construcción (30/05/2026 -- 13/06/2026)**

## Descripción general

La cuarta iteración estabilizó los módulos de inscripción a clases, gestión de empleados/profesores y horarios del portal. El cambio más significativo fue rediseñar la inscripción para que opere por horario individual (`HorarioClaseId`) en lugar de por clase genérica, permitiendo que un socio se inscriba a "Yoga, lunes 08:00" y "Yoga, miércoles 18:00" como inscripciones independientes. Se eliminó la lista de espera del alcance (si no hay cupo, se rechaza la inscripción). Se implementaron credenciales temporales autogeneradas con envío por email al crear empleados. RF-13 y RF-14 quedaron cubiertos por el sistema de roles y permisos configurables desde interfaz, sin requerir una relación fija profesor-clase.

## Tareas planificadas

Funcionalidades a implementar:

- **RF-10** — Inscripción a clase por horario individual con validación de cupo, duplicados y cuota al día.
- **RF-11** — Ver mis clases: vista "Mis Inscripciones" con día, hora y sala.
- **RF-12** — Gestionar empleados y profesores: credenciales temporales autogeneradas + email de bienvenida.
- **RF-13** — Profesor registra socios: cubierto por roles y permisos configurables desde interfaz.
- **RF-14** — Profesor gestiona sus clases: cubierto por roles y permisos configurables desde interfaz.

Requerimientos no funcionales:

- RNF-03 — Responsive: vistas del portal y admin optimizadas para móvil.
- RNF-11 — Log de auditoría en inscripciones, cancelaciones y creación de empleados.

Tareas técnicas de base:

- Migración de modelo: `InscripcionClase` referencia `HorarioClaseId` en lugar de `ClaseId`.
- Crear migración EF Core para el cambio de FK en `InscripcionesClase`.
- Implementar `GeneradorPassword` para credenciales temporales seguras.
- Optimizar `GetMisInscripcionesQuery` para evitar N+1 (conteo batch por horarios).
- Agregar `unidadIds` al `LoginResponse` y `/api/auth/me` para filtro de sede en frontend.
- Eliminar `CatalogoClasesPage` y ruta `/portal/clases` por redundancia.
- Mejorar `HorariosPortalPage` con filtro de sede obligatorio.
- Mejorar `HorariosPage` (admin) con split de clases solapadas y filtro de sede.
- Escribir tests unitarios de dominio (entidad `InscripcionClase`), de aplicación (commands y queries de inscripciones, empleados) y build de frontend.

## ¿Qué se implementó?

Funcionalidades implementadas:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RF-10 | CU-02 — Inscripción a Clase | Completado | Inscripción por `HorarioClaseId` con validación de cupo, duplicados (RN-09), cuota al día y clase activa. Email de confirmación al socio. Auditoría de inscripción y cancelación. Lista de espera descartada: si no hay cupo se bloquea. |
| RF-11 | CU-02 — Ver Mis Clases | Completado | Página "Mis Inscripciones" muestra clase, sede, día, hora, sala, capacidad y ocupación. Query optimizada con conteo batch (2 queries en vez de N+1). |
| RF-12 | CU-07 — Gestión de Empleados | Completado | Al crear empleado ya no se solicita password manual. El sistema autogenera una contraseña temporal segura y la envía por email con plantilla de bienvenida. Auditoría registra si el email fue enviado o falló. |
| RF-13 | CU-07 — Profesor registra socios | Completado | Cubierto por roles y permisos configurables desde interfaz. Un admin puede crear un rol "Profesor" con permisos de escritura en el módulo Socios. |
| RF-14 | CU-07 — Profesor gestiona sus clases | Completado | Cubierto por roles y permisos configurables desde interfaz. El admin asigna permisos por módulo (Clases.Lectura, Clases.Escritura, etc.) al rol del profesor. |
| RF-09 (mejora) | CU-06 — Horarios admin | Completado | Filtro de sede obligatorio en calendario de horarios del admin. Split visual de clases solapadas en la misma franja horaria. |

Requerimientos no funcionales implementados:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RNF-11 | Auditoría | Completado | Inscripciones y cancelaciones registran audit log con usuario, timestamp y detalle. Creación de empleados audita envío de email. |
| RNF-03 | Responsive | Completado | Vistas del portal de socios (horarios, mis inscripciones) y vistas de admin (horarios, clases) optimizadas para móvil. |
| RNF-05 | Seguridad | Completado | Controller de inscripciones usa `[Authorize]` con validación de ownership. Contraseñas temporales generadas con criterios de seguridad (mayúsculas, minúsculas, números, especiales). |

## Tareas pendientes

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| — | Forzar cambio de password en primer login | Deuda técnica | Existe flujo manual de cambio de password, pero no se fuerza al primer ingreso del empleado. |

## Pantallas implementadas

**Pantalla Horarios Portal del Socio (rediseñada)**

*(captura de pantalla)*

**Ruta:** /portal/horarios

**Descripción:** Vista principal de inscripción a clases del socio. Muestra horarios agrupados por día con nombre de clase, instructor, horario, sala y cupo disponible. El socio se inscribe a un horario específico (no a la clase genérica). Filtro de sede obligatorio. Botón "Inscribirme" deshabilitado con texto "Cupo lleno" cuando no hay cupos disponibles.

**Pantalla Mis Inscripciones (mejorada)**

*(captura de pantalla)*

**Ruta:** /portal/mis-inscripciones

**Descripción:** Listado de inscripciones activas del socio con día de semana, hora de inicio/fin, sala, nombre de clase, instructor y sede. Opción de cancelar inscripción con confirmación.

**Pantalla Calendario de Horarios Admin (mejorada)**

*(captura de pantalla)*

**Ruta:** /admin/horarios

**Descripción:** Grilla semanal de horarios con filtro de sede obligatorio. Mejora visual con split de clases solapadas en la misma franja horaria para evitar superposición de bloques.

**Pantalla Nuevo Empleado (modificada)**

*(captura de pantalla)*

**Ruta:** /admin/usuarios/nuevo

**Descripción:** Formulario de alta de empleado sin campo de contraseña. El sistema autogenera credenciales temporales y las envía por email al correo del nuevo empleado.

## Estructura de API --- endpoints implementados

| **Método** | **Endpoint** | **Descripción** |
|-|-|-|
| POST | `/api/inscripciones` | Inscribir socio a un horario (`{ horarioClaseId }`). Valida cupo, duplicados, cuota y clase activa. |
| GET | `/api/inscripciones/mis-inscripciones` | Obtener inscripciones del socio autenticado con día, hora, sala, capacidad y ocupación. |
| DELETE | `/api/inscripciones/{id}` | Cancelar inscripción propia del socio (auditoría + liberación de cupo). |
| POST | `/api/empleados` | Crear empleado sin password manual. Autogenera contraseña temporal y envía email. |
| GET | `/api/empleados` | Listar empleados (sin cambios funcionales en esta iteración). |
| GET | `/api/horarios` | Obtener horarios con conteo de inscripciones activas por horario. |
| GET | `/api/auth/me` | Retorna datos del usuario autenticado incluyendo `unidadIds` para filtro de sede. |
| POST | `/api/auth/login` | Login con `unidadIds` en la respuesta. |

## Caso de uso extendido --- Iteración 4

### CU-02: Inscripción a Clase (por horario)

| *Campo* | |
|-|-|
| *Nombre* | Inscripción a Clase por Horario |
| *Actor principal* | Socio |
| *Precondición* | Socio autenticado, estado Activo, cuota al día. Existen clases con horarios y cupo disponible. |
| *Postcondición* | Socio inscripto al horario. Cupo ocupado incrementado. Inscripción visible en "Mis Inscripciones". Email de confirmación enviado. Auditoría registrada. |

**Flujo principal:**

1. Socio accede a "Horarios" en el portal.
2. Sistema muestra horarios agrupados por día con nombre de clase, instructor, hora, sala y cupos disponibles.
3. Socio selecciona filtro de sede (obligatorio).
4. Socio selecciona un horario con cupo disponible → "Inscribirme".
5. Sistema verifica: (a) no inscripto previamente a ese horario (RN-09), (b) cupo disponible, (c) cuota al día en la unidad de la clase, (d) clase activa.
6. Sistema registra inscripción con `HorarioClaseId`, actualiza conteo de cupo, genera log de auditoría.
7. Sistema envía email de confirmación al socio con día, hora y sala.
8. Horario actualiza cupo visible. La inscripción aparece en "Mis Inscripciones".

**Flujos alternativos:**

- **Desinscripción:** Socio accede a "Mis Inscripciones" → "Cancelar inscripción" → confirma → sistema cancela inscripción, libera cupo, registra auditoría.
- **Mismo curso en otro horario:** Socio puede inscribirse a la misma clase en un horario distinto (ej. Yoga lunes y Yoga miércoles). RN-09 solo impide duplicar el mismo horario.

**Flujos de excepción:**

- **E1 — Sin cupo:** "Este horario no tiene cupos disponibles." Botón deshabilitado con texto "Cupo lleno".
- **E2 — Inscripción duplicada:** "Ya estás inscripto en este horario."
- **E3 — Clase cancelada:** No permite nuevas inscripciones a horarios de clases canceladas.
- **E4 — Cuota vencida:** "No podés inscribirte con cuota vencida en esta sede."

### CU-07: Gestión de Empleados y Profesores (credenciales temporales)

| *Campo* | |
|-|-|
| *Nombre* | Alta de Empleado con Credenciales Temporales |
| *Actor principal* | Administrador |
| *Precondición* | Admin autenticado con permisos en módulo Empleados. |
| *Postcondición* | Empleado creado con estado Activo. Contraseña temporal generada. Email de bienvenida enviado con credenciales. Auditoría registrada. |

**Flujo principal:**

1. Admin accede a "Empleados y Profesores" → "Nuevo".
2. Sistema presenta formulario sin campo de contraseña.
3. Admin completa nombre, correo, teléfono, rol y espacio asignado.
4. Sistema genera contraseña temporal segura con `GeneradorPassword`.
5. Sistema crea el empleado, envía email de bienvenida con las credenciales temporales.
6. Auditoría registra la creación y el resultado del envío de email (exitoso/fallido).

**Flujos de excepción:**

- **E1 — Correo duplicado:** "Ya existe un usuario registrado con ese correo."
- **E2 — Error envío email:** Se registra el fallo en auditoría. Admin puede reenviar manualmente.

## Reuniones con el cliente

No se realizaron reuniones formales con el cliente durante esta iteración. Se trabajó sobre los ajustes y sugerencias recopilados en la reunión de la iteración 3.

Funcionalidades presentadas:

- Pendiente de presentación en próxima reunión.

Sugerencias y ajustes solicitados:

- Los ajustes de la reunión anterior (inscripción desde cronograma, notificaciones de bienvenida) fueron incorporados en esta iteración.

## Pruebas de API realizadas con Postman

Se implementaron tests automatizados con Postman para los módulos modificados en esta iteración, validando los cambios en inscripciones (por horario) y creación de empleados (sin password manual).

### Inscripciones por Horario (RF-10/RF-11) — 7 tests

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| 200 - Login como Socio | POST | `/api/auth/login` | Autenticación de socio con `unidadIds` en respuesta |
| 200 - Inscribir socio a horario | POST | `/api/inscripciones` | Inscripción exitosa con `{ horarioClaseId }`, estructura `InscripcionClaseDto` |
| 409 - Inscripción duplicada al mismo horario | POST | `/api/inscripciones` | Rechazo por RN-09: ya inscripto en ese horario |
| 404 - Inscribir a horario inexistente | POST | `/api/inscripciones` | Error 404 con horario inexistente |
| 200 - Obtener mis inscripciones con día/hora/sala | GET | `/api/inscripciones/mis-inscripciones` | Array con inscripciones del socio, incluye `diaSemana`, `horaInicio`, `horaFin`, `sala` |
| 204 - Cancelar inscripción | DELETE | `/api/inscripciones/{id}` | Cancelación exitosa, cupo liberado |
| 404 - Cancelar inscripción inexistente | DELETE | `/api/inscripciones/{id}` | Error 404 con ID inexistente |

### Empleados (RF-12) — 4 tests

| **Test** | **Método** | **Endpoint** | **Validación** |
|-|-|-|-|
| 201 - Crear empleado sin password | POST | `/api/empleados` | Empleado creado exitosamente sin campo password en request |
| 409 - Crear empleado con correo duplicado | POST | `/api/empleados` | Rechazo por correo ya registrado |
| 200 - Listar empleados | GET | `/api/empleados` | Array con empleados registrados |
| 401 - Crear empleado sin autenticación | POST | `/api/empleados` | Rechazo sin token |

### Resumen de resultados

| **Módulo** | **Tests** | **Resultado** |
|-|-|-|
| Auth (login + me) | 2 | Pasaron |
| Inscripciones (RF-10/RF-11) | 7 | Pasaron |
| Empleados (RF-12) | 4 | Pasaron |

### Correcciones detectadas durante testing

Durante la ejecución de los tests de Postman se detectaron y corrigieron los siguientes defectos:

| **Defecto** | **Causa raíz** | **Corrección aplicada** |
|-|-|-|
| `UnidadesController` devolvía 200 sin autenticación (debía ser 401/403) | El controller no tenía `[Authorize]` ni `[RequierePermiso]`. Era el único módulo del enum `Modulo` sin protección de acceso. | Se agregó `[Authorize]` a nivel de controller y `[RequierePermiso(Modulo.Unidades, Operacion.Lectura)]` al endpoint `GET /api/unidades`. |
| `ClaseDto` no incluía `inscripcionesActivas` | El DTO de clases fue diseñado antes de la migración a inscripciones por horario. El campo nunca se agregó al DTO, aunque el conteo existía a nivel de `HorarioClaseDto`. | Se agregó el campo `InscripcionesActivas` al `ClaseDto` y se actualizaron `GetClasesQuery` y `GetClaseByIdQuery` para calcular la suma de inscripciones activas de todos los horarios de la clase. En `CreateClaseCommand` y `ReactivarClaseCommand` se retorna 0 (clase nueva/sin inscripciones). En `UpdateClaseCommand` se calcula el total real. |
| Test RNF-01 fallaba al loguear empleado restringido (401) | `CrearEmpleadoRequest` ya no acepta password (it4: credenciales autogeneradas). El test intentaba loguear con un password hardcodeado que no coincidía con el generado por el sistema. | Se agregó un paso intermedio `PATCH /api/empleados/{id}/password` en el setup de RNF-01 para asignar un password conocido al empleado antes del login. |
| Test `unidadIds` fallaba para admin (expected array, got null) | `unidadIds` es `null` para usuarios que no son socios (admin, empleados), ya que solo los socios tienen unidades asignadas. | Se actualizó la aserción en los tests de login y `/auth/me` para aceptar `null` (admin/empleados) o `array` (socios). |

## Pruebas automatizadas (xUnit)

Además de las pruebas de API con Postman, los módulos de esta iteración cuentan con pruebas automatizadas hechas en código (xUnit + Moq) en `backend/tests/**`. Se ejecutan con `dotnet test` desde `backend/`. El inventario completo de las pruebas automatizadas de las iteraciones 1 a 4 está en [[pruebas-automatizadas-it1-4]].

**Pruebas de aplicación (`GymFlow.Application.Tests`):**

| Clase de test | Caso de uso / área | Casos (aprox.) |
|-|-|-|
| `UseCases/Inscripciones/InscribirSocioCommandTests.cs` | Inscribir a un horario (RF-10): cupo, duplicados RN-09, cuota al día, clase activa | ~4 |
| `UseCases/Inscripciones/CancelarInscripcionCommandTests.cs` | Cancelar inscripción, liberar cupo | ~2 |
| `UseCases/Inscripciones/GetMisInscripcionesQueryTests.cs` | "Mis Inscripciones" (RF-11): conteo batch sin N+1 | ~1 |
| `UseCases/Empleados/CrearEmpleadoCommandTests.cs` | Alta de empleado con credenciales temporales autogeneradas + email (RF-12) | ~9 |
| `UseCases/Empleados/ActualizarEmpleadoCommandTests.cs` | Editar empleado | ~6 |
| `UseCases/Empleados/CambiarPasswordCommandTests.cs` | Cambio de password de empleado | ~3 |
| `UseCases/Empleados/DarDeBajaEmpleadoCommandTests.cs` | Baja lógica de empleado | ~3 |
| `UseCases/Empleados/ReactivarEmpleadoCommandTests.cs` | Reactivar empleado | ~2 |
| `UseCases/Empleados/GetEmpleadosQueryTests.cs` | Listado de empleados | ~2 |
| `Common/GeneradorPasswordTests.cs` | `GeneradorPassword`: contraseñas temporales seguras | ~2 |

**Total aproximado de la iteración:** ~34 casos `[Fact]`/`[Theory]` (todos en Application). RF-13 y RF-14 quedan cubiertos por las pruebas de roles y permisos de la iteración 2 (`RequierePermisoAttributeTests`, commands de `Roles/`), ya que el profesor se modela como un rol configurable.

## Pruebas funcionales de frontend

### Prueba 4.1 --- Inscripción a horario específico desde portal del socio

*(captura de pantalla)*

**Pasos:**

1. Iniciar sesión como socio.
2. Navegar a "Horarios" en el portal.
3. Seleccionar sede en el filtro obligatorio.
4. Seleccionar un horario con cupo disponible (ej. "Yoga, Lunes 08:00-09:00") y hacer clic en "Inscribirme".

**Resultado esperado:** La inscripción se registra, el cupo disponible se decrementa, y la inscripción aparece en "Mis Inscripciones" con día, hora y sala.

**Descripción:** Se verifica que la inscripción opera por horario individual. El socio se inscribe a "Yoga, lunes 08:00" y puede luego inscribirse a "Yoga, miércoles 18:00" como inscripción separada.

### Prueba 4.2 --- Inscripción bloqueada por cupo lleno

*(captura de pantalla)*

**Pasos:**

1. Navegar a "Horarios" como socio.
2. Localizar un horario que no tiene cupos disponibles.

**Resultado esperado:** El botón "Inscribirme" aparece deshabilitado con texto "Cupo lleno". No se permite la inscripción.

**Descripción:** Se verifica que cuando un horario alcanza su capacidad máxima, el sistema bloquea la inscripción sin ofrecer lista de espera.

### Prueba 4.3 --- Cancelación de inscripción desde Mis Inscripciones

*(captura de pantalla)*

**Pasos:**

1. Navegar a "Mis Inscripciones" como socio.
2. Seleccionar una inscripción activa que muestra día, hora y sala.
3. Hacer clic en "Cancelar inscripción".

**Resultado esperado:** La inscripción se elimina, el cupo se libera y el horario vuelve a mostrar cupo disponible en "Horarios".

**Descripción:** Se verifica el flujo de cancelación con la nueva estructura por horario, incluyendo la actualización correcta del conteo de cupo.

### Prueba 4.4 --- Crear empleado sin password manual

*(captura de pantalla)*

**Pasos:**

1. Iniciar sesión como administrador.
2. Navegar a Empleados → Nuevo.
3. Completar formulario con nombre, correo, teléfono, rol y espacio. El campo de contraseña no aparece.
4. Hacer clic en "Guardar".

**Resultado esperado:** El empleado se crea exitosamente. El sistema genera una contraseña temporal y envía un email de bienvenida con las credenciales.

**Descripción:** Se verifica que el formulario ya no solicita password manual y que el sistema autogenera credenciales temporales seguras enviadas por email.

### Prueba 4.5 --- Filtro de sede obligatorio en horarios del portal

*(captura de pantalla)*

**Pasos:**

1. Iniciar sesión como socio asignado a ambas sedes.
2. Navegar a "Horarios" en el portal.
3. Observar que se requiere seleccionar una sede antes de ver los horarios.

**Resultado esperado:** Los horarios se filtran por la sede seleccionada. Solo se muestran clases de la sede elegida.

**Descripción:** Se verifica que el filtro de sede es obligatorio y funcional, mostrando solo los horarios correspondientes a la unidad seleccionada.

### Prueba 4.6 --- Split visual de clases solapadas en horarios admin

*(captura de pantalla)*

**Pasos:**

1. Iniciar sesión como administrador.
2. Navegar a "Horarios" en el panel de admin.
3. Seleccionar una sede que tiene clases con horarios solapados en el mismo día.

**Resultado esperado:** Las clases solapadas se muestran con split visual (bloques divididos) para evitar superposición ilegible.

**Descripción:** Se verifica la mejora de UX en la grilla de horarios del admin cuando hay múltiples clases programadas en la misma franja horaria.
