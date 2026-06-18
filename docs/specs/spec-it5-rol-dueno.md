---
tags:
  - spec
  - iteracion
requerimiento: RNF-01
---

# Rol "Dueño" — Spec (cierre IT5)

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] — RNF-01 (sección "Rol Dueño — Planificado para It.5"), decisiones y cambios técnicos 1-6.
**Plan de implementación:** [[plan-it5-rol-dueno]]
**Specs relacionadas:** [[spec-rnf01-roles-y-permisos]], [[spec-rnf01-gestion-usuarios]]
**Última actualización:** 2026-06-17 (v2: incorpora revisión)

## Resumen

Rol de sistema **`Dueño`** entre el Admin y los roles dinámicos. El Dueño (p.ej. Maurice) **opera solo sus unidades asignadas**: ve y gestiona socios, clases, horarios, cuotas y empleados **filtrados automáticamente a sus sedes**, gestiona roles dinámicos, pero **no ve Auditoría, no toca config global y no crea otros Dueños**. Solo el Admin crea Dueños. Cierra IT5 (junto al login Google y el MFA ya hechos).

## Decisiones de diseño

### Rol y permisos (seed por HasData, NO InsertData a mano)
- En `RolesSeed` (Domain): nuevo `public static readonly Guid DuenoRolId = Guid.Parse("44444444-4444-4444-4444-444444444444")`. **Ojo:** `3333...` ya es el Id del Empleado admin de bootstrap — por eso `4444...`.
- El seed se hace en `GymFlowDbContext.OnModelCreating` (mismo lugar que Admin/Socio):
  - Agregar el `Rol` Dueño al `HasData` de `Rol` (`EsSistema=true`, `FechaCreacion = RolSeed.SeedTimestamp`).
  - Agregar `RolPermiso` para el Dueño seleccionando del diccionario `permisoIds` ya existente (que mapea `(Modulo,Operacion) → DeterministicGuid`): **todas las operaciones de** `Socios`, `Planes`, `Clases`, `Cuotas`, `Empleados`, **+ `Unidades` Lectura**. **NO** `Auditoria` (módulo exclusivo del Admin).
  - Luego `dotnet ef migrations add` genera la migración (es la salida de `OnModelCreating`, no se escribe `InsertData` a mano).
- **`Eventos` queda fuera de esta entrega:** el módulo `Eventos` y sus queries (RF-15) **no están en develop** (viven en `feature/rf15-eventos`, sin mergear). Cuando RF-15 mergee, se agregan los permisos `Eventos` al Dueño y el filtro a `GetEventosQuery` en un follow-up de 1 línea cada uno.

### Unidades de empleados (Dueños)
- `UsuarioUnidad` ya está en la base `Usuario`; hoy solo lo usan socios. Se habilita asignarlo a **empleados**:
  - `CrearEmpleadoRequest`/`ActualizarEmpleadoRequest` aceptan `UnidadIds: Guid[]`. `CrearEmpleadoCommand`/`ActualizarEmpleadoCommand` agregan/sincronizan `UsuarioUnidad` (sin `PlanId`, que es solo de socios).
  - Un empleado con rol Dueño debe tener ≥1 unidad (validación).

### Sesión y JWT (login de empleado con unidadIds)
- `EmpleadoRepository.GetByCorreoAsync` (y `GetAllAsync`) deben hacer **`.Include(e => e.UnidadesAsignadas)`** (hoy NO lo hacen → vendría vacío).
- El **login de empleado** suma `unidadIds` al `LoginResponse` (hoy solo el socio; el parámetro `UnidadIds` ya existe en el DTO). `/auth/me` se extiende para resolver las unidades **también para empleados** (hoy solo consulta el repo de socios).
- No se agregan claims nuevos al JWT. El backend resuelve las unidades visibles por request server-side (siguiente punto), no confía en el cliente.

### Filtrado automático por unidad (server-side, no spoofeable)
- Servicio `IUnidadesVisiblesResolver` (Application; impl en Infrastructure): dado el `userId` + `rolId` del JWT, devuelve el **conjunto de unidades visibles**:
  - **Admin** (`rolId == AdminRolId`) → `null` (sin restricción).
  - **Dueño** (`rolId == DuenoRolId`) → las `UnidadId` de sus `UsuarioUnidad` (lookup por `userId` vía `EmpleadoRepository.GetByIdAsync` con include de unidades; agregar el include si falta).
  - Otros empleados → `null` (su acceso lo siguen gobernando los permisos de módulo; scoping de Profesor por unidad = fuera de alcance).
- **Extensión de repos (trabajo explícito):** cada repo de listado con datos de sede recibe un parámetro nuevo `IReadOnlyCollection<Guid>? unidadesPermitidas` (null = sin restricción) que filtra con `.Where(... unidadesPermitidas.Contains(unidadId))`. Afecta:
  - `ISocioRepository.SearchAsync` (hoy escalar `Guid? unidadId`).
  - `IClaseRepository` (hoy `GetByUnidadIdAsync(Guid)`/`GetAllAsync()`).
  - `IHorarioClaseRepository.GetAllAsync(Guid? unidadId)`.
  - `ICuotaRepository.SearchAsync(...)` — ver nota especial abajo.
  - `IEmpleadoRepository.GetAllAsync` — hoy **no** filtra por unidad y **no** incluye `UnidadesAsignadas`; agregar include + filtro por set.
- Las queries (`GetSociosQuery`, `GetClasesQuery`, `GetHorariosQuery`, `GetEmpleadosQuery`, `GetCuotasAdminQuery`) reciben `unidadesPermitidas` y se lo pasan al repo. El controller lo obtiene del resolver. Un `unidadId` explícito fuera del conjunto permitido → resultado vacío.
- **`GetEmpleadosQuery`:** con `unidadesPermitidas` seteado, un Dueño solo ve empleados con alguna `UsuarioUnidad` en su conjunto. Admin y otros Dueños no comparten sus unidades → quedan naturalmente excluidos (se documenta explícito).
- **`GetCuotasAdminQuery` (caso especial):** no es un listado abierto — exige `documentoIdentidad`/`socioId` y devuelve las cuotas de **un** socio. Acá la regla es: **el socio consultado debe pertenecer a una unidad permitida**; si no, se rechaza/devuelve vacío (no un `Contains` sobre la cuota).

### Reglas de creación (validadas con el rol del actuante)
- Los comandos `CrearEmpleadoCommand`/`ActualizarEmpleadoCommand`/**`ReactivarEmpleadoCommand`** (los tres asignan rol) reciben un parámetro nuevo `Guid actuanteRolId` (y, donde aplique, las unidades del actuante), que el controller extrae del claim `rolId`. Hoy solo reciben `usuarioId`/`usuarioNombre` para auditoría, no para autorizar.
- **Asignar el rol `Dueño` requiere que `actuanteRolId == AdminRolId`.** Un Dueño que intenta asignar/reactivar al rol Dueño → excepción (403). Esto cubre el bypass por `ReactivarEmpleadoCommand`.
- Un Dueño puede crear empleados con roles **no-sistema**, con `UnidadIds` **subconjunto de las suyas** (validado en el command con las unidades del actuante).

### Gestión de roles por el Dueño (desacople de Auditoría)
- `RolesController` hoy exige `[RequierePermiso(Modulo.Auditoria, ...)]`. Se **desacopla a `[RequierePermiso(Modulo.Empleados, ...)]`** (la gestión de roles es parte de la gestión del equipo). El Admin tiene ambos módulos sembrados → no se afecta. Así el Dueño (con Empleados) gestiona roles sin ver Auditoría. (Asume que ningún rol dinámico actual dependía de Auditoría→Roles; en el seed solo el Admin tiene esos permisos.)

### Frontend
- Login de empleado ahora trae `unidadIds`; el `Sidebar` ya filtra por permiso (el Dueño no verá Auditoría al no tener el permiso).
- Selector de sede del admin: para un Dueño se constriñe a sus `unidadIds` (como el portal del socio). Admin sin cambios.
- Form de empleado: permitir asignar unidades; mostrar el rol Dueño como opción **solo si el actuante es Admin**.

## Criterios de aceptación
- Existe el rol `Dueño` (EsSistema, Guid `4444...`) con permisos operativos y **sin** Auditoría; aparece como opción de rol solo para el Admin.
- Un empleado Dueño con unidades, al loguearse, recibe `unidadIds` **no vacíos**; al listar socios/clases/horarios/cuotas/empleados **solo ve los de sus unidades**, aunque pida otra unidad por query param.
- El Admin sigue viendo todas las unidades.
- Un Dueño **no** puede crear, editar **ni reactivar** un empleado al rol Dueño (403/validación); solo el Admin.
- Un Dueño puede crear un empleado (rol no-sistema) con unidades restringidas a las suyas.
- El Dueño **no** ve Auditoría (sin permiso) pero **sí** gestiona roles (CRUD de roles dinámicos).

## Fuera de alcance
- **`Eventos`** para el Dueño (depende de RF-15 sin mergear; follow-up trivial al mergear).
- "Roles limitados a los que el Dueño creó" (ownership con `CreadoPorUsuarioId`) — mejora futura.
- Scoping por unidad del rol Profesor u otros dinámicos.
- "Ver varias unidades a la vez" en un único listado (la query soporta el set; el selector elige una; un Dueño de 1 sede ve todo lo suyo).
- Multi-compañía con aislamiento total más allá del filtro por unidad.
