---
tags:
  - spec
  - iteracion
requerimiento: RNF-01
---

# Rol "Dueño" — Spec (cierre IT5)

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] — RNF-01 (rol Dueño, sección "Rol Dueño — Planificado para It.5"), decisiones de diseño y cambios técnicos 1-6.
**Plan de implementación:** [[plan-it5-rol-dueno]]
**Specs relacionadas:** [[spec-rnf01-roles-y-permisos]], [[spec-rnf01-gestion-usuarios]]
**Última actualización:** 2026-06-17

## Resumen

Se agrega un rol de sistema **`Dueño`** entre el Admin y los roles dinámicos. El Dueño (p.ej. Maurice) **opera solo sus unidades asignadas**: ve y gestiona socios, clases, horarios, cuotas, eventos y empleados **filtrados automáticamente a sus sedes**, puede gestionar roles y empleados, pero **no ve el módulo de Auditoría, no toca configuraciones globales y no puede crear otros Dueños**. Solo el Admin crea Dueños. Es la pieza que cierra IT5 (junto al login Google y el MFA ya hechos).

## Decisiones de diseño

### Rol y permisos
- **Seed del rol `Dueño`** (`EsSistema=true`, Guid fijo `33333333-3333-3333-3333-333333333333` en `RolesSeed`). Permisos asignados vía migración: **todas las operaciones de** `Socios`, `Planes`, `Clases`, `Cuotas`, `Eventos`, `Empleados`, **+ `Unidades` Lectura**. **NO** recibe permisos de `Auditoria` (el módulo de auditoría queda exclusivo del Admin).
- El Admin ya tiene todos los permisos (incluido `Eventos` por la migración de RF-15 y `Auditoria`). El Dueño es un subconjunto operativo sin Auditoría.

### Unidades del Dueño (y de empleados en general)
- La relación `Usuario ↔ Unidad` (`UsuarioUnidad`) ya existe en la base `Usuario`; hoy solo la usan los socios. Se habilita asignar **unidades a empleados** (en particular Dueños):
  - `CrearEmpleadoRequest`/`ActualizarEmpleadoRequest` aceptan `UnidadIds: Guid[]`. `CrearEmpleadoCommand`/`ActualizarEmpleadoCommand` agregan/sincronizan `UsuarioUnidad` (sin `PlanId`, que es solo para socios).
  - Un Dueño debe tener ≥1 unidad asignada (validación al crear/editar un empleado con rol Dueño).

### Sesión y JWT
- El **login de empleado** pasa a incluir `unidadIds` en el `LoginResponse` (hoy solo el socio lo hace) y `/auth/me` los reconstruye para empleados también. Esto permite al frontend constreñir el selector de sede y al backend conocer las sedes del usuario.
- No se agregan claims nuevos al JWT (se evita inflarlo); el backend resuelve las unidades visibles por request a partir del `userId` + rol (ver siguiente punto), de forma **server-side y no spoofeable**.

### Filtrado automático por unidad (el corazón del Dueño)
- Servicio `IUnidadesVisiblesResolver` (Application; impl en Infrastructure o API): dado el usuario actual (userId + rolNombre del JWT), devuelve el **conjunto de unidades visibles**:
  - **Admin** → `null` (sin restricción: ve todas).
  - **Dueño** → las `UnidadId` de sus `UsuarioUnidad`.
  - Otros empleados → por ahora `null` (sin restricción; su acceso lo siguen gobernando los permisos de módulo). *(Scoping de Profesor por unidad: fuera de alcance, ver abajo.)*
- Las **queries de listado con datos de sede** (`GetSociosQuery`, `GetClasesQuery`, `GetHorariosQuery`, `GetEventosQuery`, `GetCuotasAdminQuery`, `GetEmpleadosQuery`) reciben un parámetro `unidadesPermitidas: IReadOnlyCollection<Guid>?` (null = sin restricción). Si viene seteado, **restringen los resultados a esas unidades**, combinado con cualquier `unidadId` explícito del request (un `unidadId` fuera del conjunto permitido devuelve vacío / 403). El controller obtiene `unidadesPermitidas` del resolver y se lo pasa a la query.
- `GetEmpleadosQuery` (que hoy no filtra por unidad) pasa a filtrar: un Dueño solo ve empleados asignados a sus unidades (y no ve otros Dueños/Admins). Implementación: empleados con alguna `UsuarioUnidad` en el conjunto permitido.

### Reglas de creación
- **Solo el Admin crea Dueños:** al crear/editar un empleado con rol `Dueño`, el `CrearEmpleadoCommand`/`ActualizarEmpleadoCommand` exige que el usuario actuante sea Admin (rolId == AdminRolId). Un Dueño que intenta asignar el rol Dueño → error 403/validación.
- **Un Dueño no puede crear otros Dueños** (caso particular de la regla anterior).
- El Dueño sí puede crear empleados con roles no-sistema (Profesor, etc.) **dentro de sus unidades** (las `UnidadIds` del empleado creado deben ser subconjunto de las del Dueño).

### Gestión de roles por el Dueño
- Hoy `RolesController` exige `[RequierePermiso(Modulo.Auditoria, ...)]`. Como el Dueño **no** tiene Auditoría pero **sí** debe gestionar roles, se **desacopla**: `RolesController` pasa a exigir `[RequierePermiso(Modulo.Empleados, ...)]` (la gestión de roles es parte de la gestión del equipo). El Admin no se ve afectado (tiene ambos). Así el Dueño (con Empleados) gestiona roles sin ver Auditoría.
- *(La regla "un Dueño solo ve/edita los roles que él creó" requiere un `CreadoPorUsuarioId` en `Rol` + filtrado; se documenta como mejora pero queda **fuera de alcance** de esta entrega — ver abajo. En esta entrega el Dueño gestiona los roles dinámicos no-sistema.)*

### Frontend
- `AuthContext`/login: los empleados ahora reciben `unidadIds`; el `Sidebar` ya filtra módulos por permiso (el Dueño no verá Auditoría automáticamente, al no tener el permiso).
- Selector de sede del admin: para un Dueño se constriñe a sus `unidadIds` (como el portal del socio constriñe a sus sedes). Para el Admin, sin cambios (ve todas / "todas").
- Form de empleado: permitir asignar unidades al empleado; mostrar el rol Dueño como opción **solo si el usuario actuante es Admin**.

## Criterios de aceptación
- Existe el rol `Dueño` (EsSistema) con permisos operativos y **sin** Auditoría; aparece como opción de rol solo para el Admin.
- Un empleado con rol Dueño y unidades asignadas, al loguearse, recibe `unidadIds`; al listar socios/clases/horarios/eventos/cuotas/empleados **solo ve los de sus unidades**, aunque pida otra unidad por query param.
- El Admin sigue viendo todas las unidades.
- Un Dueño **no** puede crear/editar un empleado con rol Dueño (403/validación); solo el Admin puede.
- Un Dueño puede crear un empleado (rol no-sistema) y asignarle unidades, restringidas a las suyas.
- El Dueño **no** ve el módulo de Auditoría (no tiene el permiso) pero **sí** puede gestionar roles (CRUD de roles dinámicos).
- El módulo de auditoría sigue siendo accesible solo con permiso de Auditoría (que el Dueño no tiene).

## Fuera de alcance
- "Roles limitados a los que el Dueño creó" (ownership de roles con `CreadoPorUsuarioId`) — mejora futura; en esta entrega el Dueño gestiona los roles dinámicos no-sistema.
- Scoping por unidad del rol **Profesor** u otros roles dinámicos (solo el Dueño se filtra por unidad en esta entrega; los demás empleados siguen gobernados por permisos de módulo).
- "Ver varias unidades a la vez" en un único listado para Dueños con N unidades: el filtrado restringe a sus unidades; el selector de sede del frontend elige una. (La query soporta el conjunto, así que un Dueño con 1 sede —caso de Maurice— ve todo lo suyo sin elegir.)
- Multi-compañía / múltiples Dueños totalmente aislados entre sí más allá del filtro por unidad.
