---
title: DOCUMENTACION ITERACIÓN 1 FASE DE CONSTRUCCIÓN
tags:
  - seguimiento
related:
  - "[[seguimiento_index]]"
---

# DOCUMENTACION ITERACIÓN 1 FASE DE CONSTRUCCIÓN

**Iteración 1 --- Fase de Construcción (15/04/2026 -- 29/04/2026)**

> Nota: este documento se retro-convirtió desde `Documentacion_It.1.docx` (la versión entregada) para incorporarlo al flujo de trabajo en Obsidian. Las capturas de pantalla están solo en el .docx original. Se agregó la sección de pruebas automatizadas (xUnit).

## Descripción general

El objetivo de esta iteración es establecer la base técnica del sistema GymFlow e implementar el núcleo funcional de gestión de socios, con cumplimiento de la Ley 18.331 desde el inicio del desarrollo.

## Tareas planificadas

Funcionalidades a implementar:

- **RF-01** — Registro de socios: alta de socios con validación de datos y cláusula de consentimiento informado.
- **RF-02** — Listado de socios: visualización con búsqueda y filtros por estado de cuota, espacio y plan.
- **RF-03** — Edición de socios: modificación de datos con generación de log de auditoría.
- **RF-04** — Baja lógica de socios: marcado como inactivo sin eliminación del registro.
- **RF-20** — Gestión multi-espacio: base arquitectónica para operar Gimnasio Nuevo Malvín y Espacio Mora desde una única plataforma.
- **RF-22** — Planes por unidad de negocio: carga y diferenciación de planes según el espacio.

Requerimientos no funcionales:

- **RNF-01** — Autenticación y autorización por roles: cada usuario accede únicamente a lo que le corresponde según su perfil.
- **RNF-05** — Seguridad de datos: almacenamiento seguro con contraseñas cifradas.
- **RNF-08** — Arquitectura multi-espacio: estructura preparada para gestionar múltiples unidades sin degradación de rendimiento.
- **RNF-09** — Cumplimiento Ley 18.331: consentimiento informado, posibilidad de baja/modificación de datos y evidencia de aceptación con timestamp.
- **RNF-11** — Registro de auditoría: log de operaciones críticas con usuario, timestamp y detalle de la acción.

## ¿Qué se implementó?

Funcionalidades implementadas:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|:---|:---|:---|:---|
| RF-01 — Registro de socios | CU-01 | Completo | Alta con validación de datos, cláusula Ley 18.331 obligatoria (checkbox + timestamp), validación de documento, asignación a unidades y planes. |
| RF-02 — Listado de socios | CU-01 | Completo | Página de socios con búsqueda por nombre, filtros por estado, unidad y plan; tabs activos/inactivos. |
| RF-03 — Edición de socios | CU-01 | Completo | Formulario de edición con PUT al backend; log de auditoría generado automáticamente con detalle de campos modificados. |
| RF-04 — Baja lógica | CU-01 | Completo | Soft-delete con campo motivoBaja y opción de reactivación (PATCH /reactivar); el registro nunca se elimina de la base de datos. |
| RF-20 — Gestión multi-espacio | CU-01 (prerrequisito) | Completo | Arquitectura UsuarioUnidad N:M; 2 unidades seedeadas (Nuevo Malvín y Espacio Mora); filtros por unidad en socios y planes. |
| RF-22 — Planes por unidad | CU-01 | Completo | PlanId asignado por espacio (no global); página de planes con filtro por unidad y CRUD completo. |

Requerimientos no funcionales:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|:---|:---|:---|:---|
| RNF-01 — Autenticación y roles | CU-05 | Parcial | JWT implementado (8h, claims con Rol/Nombre), usuarios hardcodeados para desarrollo. Los roles existen pero sin guards en endpoints; cualquier usuario autenticado accede a todo. Se completa en iteraciones siguientes. |
| RNF-05 — Seguridad de datos | CU-05 | Parcial | Token en localStorage, CORS configurado. Contraseñas de usuarios de desarrollo sin hash real. Se corrige en la próxima iteración. |
| RNF-08 — Arquitectura multi-espacio | — | Completo | Entidad Unidad + UsuarioUnidad con filtros en queries; preparado para escalar sin degradación de rendimiento. |
| RNF-09 — Cumplimiento Ley 18.331 | CU-01 | Mayormente completo | Consentimiento obligatorio con timestamp guardado. La posibilidad de modificación de datos personales por parte del socio se implementará en Iteración 2 junto con RF-05 (perfil del socio). |
| RNF-11 — Registro de auditoría | CU-01 | Completo | RegistroAuditoria con usuario, timestamp UTC, tipo de acción y detallesCambios en JSON. Visor en frontend con filtros. |

## Pantallas implementadas

| **Pantalla** | **Descripción** |
|:---|:---|
| Login | Autenticación de usuario con email y contraseña; genera y almacena el token JWT |
| Socios | Listado de socios con tabs activo/inactivo, búsqueda por nombre y filtros por unidad y plan |
| Nuevo Socio | Formulario de alta de socio con validación de datos, aceptación obligatoria de consentimiento informado (Ley 18.331) y asignación de unidad y plan |
| Editar Socio | Formulario de edición de datos del socio; registra automáticamente un log de auditoría con los campos modificados |
| Planes | Listado de planes filtrado por unidad, con opciones de creación, edición y baja |
| Nuevo Plan | Formulario de alta de plan con nombre, precio, descripción y asignación a una unidad |
| Editar Plan | Formulario de edición de los datos de un plan existente |
| Auditoría | Registro de operaciones con filtros por fecha, tipo de acción e ID de entidad; muestra el detalle de cambios por operación |

*(capturas de pantalla en el .docx)*

## Estructura de API --- endpoints implementados

| **Método** | **Endpoint** | **Descripción** |
|:---|:---|:---|
| POST | /api/auth/login | Autenticación y generación de JWT |
| GET | /api/auth/me | Validación del token y obtención del usuario actual |
| GET | /api/socios | Listado con filtros por nombre, unidad, plan y estado |
| GET | /api/socios/{id} | Obtención de un socio por ID |
| POST | /api/socios | Alta de socio |
| PUT | /api/socios/{id} | Edición de socio |
| DELETE | /api/socios/{id} | Baja lógica de socio con motivo opcional |
| PATCH | /api/socios/{id}/reactivar | Reactivación de socio |
| GET | /api/planes | Listado de planes por unidad |
| GET | /api/planes/{id} | Obtención de un plan por ID |
| POST | /api/planes | Alta de plan |
| PUT | /api/planes/{id} | Edición de plan |
| DELETE | /api/planes/{id} | Baja de plan |
| GET | /api/unidades | Listado de unidades de negocio |
| GET | /api/auditoria | Listado de registros de auditoría con filtros |

## Tarea para próxima iteración

El rol de profesor, incluido en RNF-01 (Autenticación y autorización por roles), no pudo completarse en esta iteración dado que en la reunión de revisión con el cliente no quedaron definidas las responsabilidades concretas de este perfil. Sin esa especificación, no fue posible determinar a qué funcionalidades debe tener acceso ni cómo diferenciarlo del rol administrador. Se definió entre el equipo integrar al RNF-01 lo siguiente:

*RNF-01 — Gestión de Roles y Grupos de Seguridad por Menú*

La idea es reemplazar los roles fijos del sistema (Administrador / Profesor / Socio) por un sistema de permisos flexible donde el administrador pueda crear roles personalizados combinando grupos de seguridad.

Cada menú del sistema (Socios, Clases, Cuotas, Empleados, Eventos, Rutinas, Dashboard) tendría asociados 4 grupos de seguridad basados en el modelo CRUD:

- **Lectura** — ver y consultar registros
- **Escritura** — crear nuevos registros
- **Modificación** — editar registros existentes
- **Eliminación** — dar de baja registros

El administrador podría entonces crear un rol como "Recepcionista" con lectura y escritura en Socios, pero sin modificación ni eliminación, o un rol "Supervisor" con lectura en todos los módulos, pero sin escritura en ninguno.

*En la iteración 3 se incluye el menú para la creación de usuarios.*

## Caso de uso extendido --- Iteración 1

### CU-01: Gestión de socios

| *Campo* | *Detalle* |
|:---|:---|
| *Nombre* | *Gestión de socios* |
| *Actor principal* | *Administrador* |
| *Precondición* | *El usuario está autenticado con rol Administrador* |
| *Postcondición* | *El sistema refleja el estado actualizado del socio y registra la operación en el log de auditoría* |

**Flujo principal:**

1. El administrador accede al listado de socios
2. El sistema muestra los socios con filtros por nombre, unidad, plan y estado (activo/inactivo)
3. El administrador selecciona una acción: registrar, editar o dar de baja

**Subflujo A — Registro de socio:**

- A1. El administrador completa el formulario con datos personales (nombre, apellido, documento, teléfono, fecha de nacimiento)
- A2. El administrador asigna el socio a una o más unidades y selecciona un plan por unidad
- A3. El administrador acepta la cláusula de consentimiento informado (Ley 18.331) — campo obligatorio
- A4. El sistema valida los datos, registra el timestamp de consentimiento y persiste el socio
- A5. El sistema registra la operación en el log de auditoría con usuario, timestamp y detalle

**Subflujo B — Edición de socio:**

- B1. El administrador selecciona un socio del listado y accede al formulario de edición
- B2. Modifica los campos deseados
- B3. El sistema persiste los cambios y genera un registro de auditoría con el detalle de los campos modificados

**Subflujo C — Baja lógica:**

- C1. El administrador selecciona la opción de baja e ingresa un motivo opcional
- C2. El sistema marca al socio como inactivo sin eliminar el registro
- C3. El registro puede ser reactivado posteriormente por el administrador

**Flujos alternativos:**

- Si el consentimiento informado no es aceptado, el sistema impide el alta del socio
- Si los datos obligatorios están incompletos, el sistema muestra errores de validación sin persistir

## Modelo de datos --- Gestión multi-espacio y planes

Se implementó la diferenciación de planes por unidad de negocio. El plan se asigna a nivel de la relación socio-unidad, lo que permite que un mismo socio tenga, por ejemplo, el plan "Musculación" en Gimnasio Nuevo Malvín y el plan "Completo" en Espacio Mora de forma simultánea e independiente.

*(diagrama en el .docx)*

## Reuniones con el cliente

El lunes 13/04/2026, previo al inicio formal de la iteración, se realizó una reunión con el cliente en la que se presentó un avance temprano de las funcionalidades planificadas para esta primera iteración. Esta instancia sirvió como validación anticipada de los requerimientos relevados en la fase de Elaboración y permitió obtener feedback del cliente antes del inicio oficial de la Construcción.

Funcionalidades presentadas (Iteración 1):

- Gestión de Socios: Se mostró la capacidad de dar de alta, modificar, dar de baja y borrar socios. El sistema incluye un validador de cédulas para evitar registros erróneos.
- Estados de Socios: Los socios pueden quedar como inactivos en lugar de ser borrados definitivamente, lo que permite al cliente mantener un registro para futuras promociones o reingresos.
- Gestión de Planes y Sedes: Es posible inscribir a los socios en el Gimnasio Nuevo Malvín, en Espacio Mora, o en ambos, permitiendo seleccionar planes específicos para cada unidad de negocio.
- Sistema de Auditoría: El software incluye un registro de todas las acciones realizadas (quién borró a un usuario, motivos de baja, etc.) para mantener el control administrativo.

En general, el cliente calificó el avance como prolijo y sencillo, destacando que las funcionalidades básicas cumplen con lo esperado para esta etapa inicial.

## Pruebas de API realizadas con Postman

Se realizaron pruebas funcionales y de integración a nivel API sobre los endpoints de GymFlow, utilizando Postman como herramienta. Las pruebas verifican el correcto funcionamiento de los módulos de Auth, Socios, Planes, Unidades y Auditoría, validando para cada endpoint: el código de estado HTTP devuelto según la operación (200, 201, 204, 400, 401, 404, 409), la estructura JSON de las respuestas conforme a los DTOs definidos en el backend, los tipos de datos de cada campo, el tiempo de respuesta (menor a 500ms) y las reglas de negocio. Los tests se ejecutan de forma encadenada mediante variables de colección, permitiendo simular flujos completos.

*(capturas de pantalla en el .docx)*

## Pruebas automatizadas (xUnit)

Además de las pruebas de API con Postman, los módulos de esta iteración cuentan con pruebas automatizadas hechas en código (xUnit + Moq) en `backend/tests/**`, ejecutadas con `dotnet test` desde `backend/`. Cobertura correspondiente a esta iteración:

**Gestión de socios (RF-01 a RF-04):**

- *Dominio:* la validación de la cédula uruguaya cubre cédulas de 7 y 8 dígitos, formatos con puntos y guion, dígito verificador incorrecto y texto no numérico; el documento es obligatorio para el tipo cédula de identidad, el pasaporte admite valores arbitrarios y el tipo "otro" permite omitirlo; la actualización de datos del socio revalida la cédula con las mismas reglas.
- *Application:* el alta de socio acepta cada tipo de documento y devuelve el DTO con el tipo correcto; una cédula inválida rechaza el alta; el listado de socios propaga los filtros al repositorio.

**Multi-espacio y unidades (RF-20 / RNF-08):**

- *Dominio:* la unidad valida nombre y dirección tanto al crearse como al actualizarse.
- *Application:* el listado de unidades devuelve todas las unidades registradas y maneja correctamente el caso sin unidades.

**Empleados (base para RNF-01):**

- *Dominio:* el empleado se crea activo y exige el hash de contraseña (nunca texto plano); el cambio de rol valida que el identificador recibido sea válido.

El inventario completo de las pruebas automatizadas de las iteraciones 1 a 4, clase por clase, está en [[pruebas-automatizadas-it1-4]].

## Pruebas funcionales de frontend módulo de Socios

- Dar de alta un nuevo socio *(captura de pantalla)*
- Socio se agrega correctamente a la lista de socios activos *(captura de pantalla)*
- Buscar socio por Nombre *(captura de pantalla)*
- Buscar socio por Plan *(captura de pantalla)*
- Editar un socio *(captura de pantalla)*
- Socio editado correctamente *(captura de pantalla)*
- Dar de baja un socio *(captura de pantalla)*
- Verificar que el socio haya quedado inactivo correctamente *(captura de pantalla)*

## Pruebas funcionales de frontend módulo de Planes

Se completa el formulario de "Nuevo Plan" con los datos requeridos (nombre, unidad, precio y descripción). Al guardar, el plan aparece en la lista con estado "Activo", validando el alta correcta. *(captura de pantalla)*

Se accede a la edición de un plan existente. Se modifican el precio y la descripción. El campo "Unidad" aparece deshabilitado ya que no se permite cambiar una vez creado. Al guardar, los cambios se reflejan en la lista. *(captura de pantalla)*

Se utiliza el filtro de unidad para verificar que la lista muestra únicamente los planes correspondientes a la unidad seleccionada, confirmando el correcto funcionamiento del filtrado. *(capturas de pantalla)*

Se intenta dar de baja un plan que tiene socios asignados, mostrando un mensaje de error que impide la operación. Luego se da de baja un plan sin socios, confirmando la baja lógica exitosa y el cambio de estado a "Inactivo". *(capturas de pantalla)*

## Pruebas funcionales de frontend módulo de auditoría

Se valida que al generar cualquier acción dentro del sistema se genera un registro del módulo. *(captura de pantalla)*

## Registro de tiempos

**Desarrollo -- tiempo por commit**

| **Hash** | **Fecha** | **Descripción** | **Tiempo (hs)** |
|:--:|----|----|:--:|
| b6daf11 | 2026-04-26 | Hotfix: correcciones menores en vista Planes | 1.50 |
| e9b022d | 2026-04-26 | Correcciones endpoints + middleware errores | 1.50 |
| ae760f6 | 2026-04-26 | Sistema de permisos y roles por módulo | 1.50 |
| 84117bf | 2026-04-29 | Docs: reclasificar RF-23 a RNF-01 | 1.00 |
| 5965f4b | 2026-04-29 | Feat: DTOs y repo para gestión empleados | 2.50 |
| 5c790a2 | 2026-04-29 | Feat: IPasswordHasher con BCrypt | 2.50 |
| a59fb94 | 2026-04-29 | Feat: entidad Empleado + módulo permisos | 2.50 |
| b3cf9cd | 2026-04-29 | Refactor: PasswordHash nullable, limpiar OAuth | 1.50 |
| 7570f16 | 2026-04-29 | Test: tests de Empleado.CambiarRol | 1.50 |
|  |  | **Subtotal Desarrollo** | **16.0** |

**Otras actividades**

| **Actividad** | **Tiempo (hs)** |
|----|:--:|
| Planificación Plan de testing - Frontend | 2 |
| Ejecución plan de testing - Frontend | 3 |
| Planificación Plan de testing - Endpoints en Postman | 4 |
| Ejecución Plan de testing - Endpoints en Postman | 6 |
| Reuniones con el Cliente | 1 |
| Documentación | 4 |
| **Subtotal Otras Actividades** | **19** |

**TOTAL HORAS - Iteración 1: 36**
