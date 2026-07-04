# CU-07: Gestión de Empleados, Roles y Permisos

| *Campo* | |
|-|-|
| *Nombre* | Gestión de Empleados, Roles y Permisos (incluye rol Dueño) |
| *Actor principal* | Administrador |
| *Precondición* | Admin autenticado con permisos sobre el módulo Empleados/Roles. Para asignar rol Dueño: solo el Admin de sistema puede hacerlo. |
| *Postcondición* | Empleado creado/modificado/dado de baja con credenciales temporales enviadas por email (desde IT-4). Roles personalizados creados con combinaciones de permisos por módulo. Filtrado automático por sedes para el rol Dueño (desde IT-5). |
| *RF cubiertos* | RNF-01 (autenticación + autorización por roles), RF-12 (credenciales temporales), RF-13 (profesor registra socios), RF-14 (profesor gestiona clases) |
| *Iteración(es) de entrega* | IT-2 — Sistema de roles dinámicos con permisos por módulo + ABM de empleados + entidad `Empleado` con BCrypt. IT-4 — Alta de empleado sin password manual: el sistema autogenera credenciales temporales y las envía por email. Roles "Profesor" cubiertos por permisos configurables. IT-5 — Rol "Dueño" entre Admin y los roles dinámicos, con filtrado automático por sedes asignadas. |
| *Referencia original* | [GymFlow_Requerimientos_Completos.md § CU-07](../GymFlow_Requerimientos_Completos.md#cu-07--gestión-de-empleados-y-profesores) |
| *Referencia spec* | [spec-it4-inscripciones-empleados-horarios](../specs/spec-it4-inscripciones-empleados-horarios.md), [spec-it5-rol-dueno](../specs/spec-it5-rol-dueno.md) |
| *Referencia plan* | [plan-it4-inscripciones-empleados-horarios](../plans/plan-it4-inscripciones-empleados-horarios.md), [plan-it5-rol-dueno](../plans/plan-it5-rol-dueno.md) |
| *Documentos de iteración* | [Documentacion_It.2.docx](../seguimiento/Documentacion_It.2.docx), [iteracion-4.md](../seguimiento/iteracion-4.md), [iteracion-5.md](../seguimiento/iteracion-5.md) |

**Flujo principal — Alta de Empleado con credenciales temporales (IT-4):**

1. Admin accede a "Empleados y Profesores" → "Nuevo".
2. Sistema presenta formulario **sin campo de contraseña**.
3. Admin completa nombre, correo, teléfono, rol y espacio asignado.
4. Sistema genera contraseña temporal segura con `GeneradorPassword` (mayúsculas, minúsculas, números, especiales).
5. Sistema crea al empleado y envía email de bienvenida con las credenciales temporales (plantilla específica).
6. Auditoría registra la creación y el resultado del envío del email (exitoso/fallido).
7. (IT-5) En el primer login el empleado pasa por el alta de MFA (ver [CU-05](CU-05-autenticacion.md)).

**Flujo alternativo — Edición de Empleado:**

1. Admin selecciona empleado → "Editar".
2. Modifica datos (no incluye password — se cambia desde flujo separado `PATCH /api/empleados/{id}/password`).
3. Sistema persiste cambios y registra auditoría.

**Flujo alternativo — Baja Lógica y Reactivación:**

1. Admin selecciona empleado → "Dar de baja" (soft delete).
2. Empleado pasa a estado `Inactivo`; queda en historial.
3. Desde la tab de inactivos, el admin puede "Reactivar" (`PATCH /api/empleados/{id}/reactivar`).

**Flujo alternativo — Crear rol personalizado (IT-2):**

1. Admin accede a `/admin/roles` → "Nuevo Rol".
2. Ingresa nombre del rol (ej. "Recepcionista", "Profesor", "Supervisor").
3. Marca los permisos por módulo: `Modulo.{Socios, Clases, Cuotas, Empleados, Eventos, Unidades, ...}` × `Operacion.{Lectura, Escritura, Modificacion, Eliminacion}`.
4. Sistema guarda el rol y sus permisos. Los permisos quedan cacheados para no consultar la base en cada request.

**Flujo alternativo — Rol Profesor (RF-13/RF-14, cubierto por roles dinámicos en IT-4):**

- El admin crea un rol "Profesor" con permisos:
  - **RF-13 — registrar socios:** `Modulo.Socios × Operacion.Escritura`.
  - **RF-14 — gestionar clases:** `Modulo.Clases × {Lectura, Escritura, Modificacion}`.
- El profesor accede automáticamente solo a las funciones permitidas por su rol.

**Flujo alternativo — Alta/edición de empleado con rol Dueño (IT-5):**

1. Admin accede a "Empleados" → "Nuevo" (o edita uno existente).
2. Sistema presenta selector de rol (incluye "Dueño") y selector multi-sede.
3. Admin selecciona el rol Dueño y al menos una sede.
4. Sistema valida las reglas:
   - Dueño requiere ≥1 sede asignada.
   - Solo el Admin puede asignar el rol Dueño.
5. Guarda la asignación de sedes y registra auditoría.

**Comportamiento del rol Dueño (filtrado automático por sede):**

- En cada request del Dueño, el backend resuelve a partir de su sesión cuáles son las sedes asignadas. Para el Admin no hay filtro: ve todo.
- Todos los listados del panel admin (socios, clases, horarios, cuotas, empleados, eventos) aplican el filtro **automáticamente en la consulta a la base**, sin depender de lo que envíe el cliente.
- Si el Dueño intenta consultar explícitamente una sede que no tiene asignada, el sistema devuelve **resultado vacío (no error)** para evitar filtración lateral de información.
- El Dueño **no accede al módulo Auditoría**.

**Flujos de excepción:**

- **E1 — Correo duplicado:** "Ya existe un usuario registrado con ese correo."
- **E2 — Error envío email de credenciales:** Se registra el fallo en auditoría. Admin puede reenviar manualmente.
- **E3 — Dueño sin sedes asignadas:** Bloqueado por validación.
- **E4 — Empleado distinto del Admin intentando asignar rol Dueño:** Bloqueado.

**Reglas de negocio aplicables:**

- **Roles dinámicos por permisos:** los roles no son fijos; el admin puede crear cualquier combinación de permisos por módulo × operación.
- **Sin password manual al crear empleado (desde IT-4):** el sistema autogenera una contraseña temporal segura y la envía por email.
- **Empleado con BCrypt:** contraseñas hasheadas en la base (IT-2).
- **MFA obligatorio para todos los empleados (IT-5):** ver [CU-05](CU-05-autenticacion.md).
- **Rol Dueño jerárquicamente entre Admin y los roles dinámicos:** tiene permisos operativos amplios pero acotado a las sedes asignadas. No accede a Auditoría.
- **Solo el Admin puede crear Dueños.**
- **Filtrado por sede del lado del servidor (Dueño):** no se confía en el cliente; los repos aceptan el conjunto de sedes habilitadas como parámetro.

**Deuda técnica pendiente (registrada en IT-4):**

- No se fuerza cambio de password en el primer login del empleado. Existe el flujo manual pero no obligatorio.

**Desviaciones respecto del diseño original:**

- **Roles "Profesor" no como rol fijo:** el diseño original definía Profesor como rol del sistema. Se reemplazó por el sistema de permisos dinámicos (un admin crea un rol llamado "Profesor" con los permisos que considere). RF-13 y RF-14 quedan cubiertos por configuración, no por código específico.
- **Rol Dueño agregado en IT-5:** no estaba en el diseño original. Surgió por necesidad operativa de delegar la administración por sede sin dar acceso total.
