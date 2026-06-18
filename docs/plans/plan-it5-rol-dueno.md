# Rol "Dueño" — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: usar superpowers:subagent-driven-development. TDD estricto por tarea (rojo → verde → suite completa → commit). Pasos con checkbox `- [ ]`.

**Goal:** Rol de sistema `Dueño` que opera solo sus unidades asignadas, con filtrado automático server-side por unidad.

**Architecture:** Seed del rol por `HasData` en `OnModelCreating`. Empleados con `UsuarioUnidad`. Resolver server-side de "unidades visibles" desde el JWT (Admin→null, Dueño→sus unidades). Las queries de listado reciben `unidadesPermitidas` y los repos filtran por ese set. Reglas de asignación de rol validadas con el `rolId` del actuante.

**Tech Stack:** .NET 8, EF Core (PostgreSQL, HasData seed), xUnit+Moq. Frontend React 18 + TS.

**Spec:** [[spec-it5-rol-dueno]] — leerla, tiene el detalle. **Rama:** `feature/it5-rol-dueno` (base develop).

**Regla de oro:** mensajes en español rioplatense. Tras CADA tarea `dotnet test backend/GymFlow.sln` 100% verde. NO incluir `Eventos` (no está en develop). Seguir patrones del repo.

---

### Task 1: Seed del rol Dueño + permisos

**Files:** Modify `RolesSeed.cs`, `GymFlowDbContext.cs`; Migration. Test: `backend/tests/GymFlow.Infrastructure.Tests/` (o Domain) si aplica.

- [ ] **Step 1:** `RolesSeed.DuenoRolId = Guid.Parse("44444444-4444-4444-4444-444444444444")`.
- [ ] **Step 2:** En `GymFlowDbContext.OnModelCreating`: agregar el `Rol` Dueño al `HasData` de `Rol` (EsSistema=true, FechaCreacion=RolSeed.SeedTimestamp). Agregar `RolPermiso` para el Dueño seleccionando de `permisoIds` las operaciones de `Socios`, `Planes`, `Clases`, `Cuotas`, `Empleados` (todas) + `Unidades` Lectura. NO Auditoria. NO Eventos.
- [ ] **Step 3:** Migración `dotnet ef migrations add AgregarRolDueno --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`. Verificar que solo agregue el rol + sus RolPermisos (sin cambios espurios). Down consistente.
- [ ] **Step 4:** `dotnet build` + suite completa → PASS.
- [ ] **Step 5:** Commit `feat(dueno): seed del rol Dueno con permisos operativos`.

---

### Task 2: Empleados con unidades + reglas de asignación de rol (actuante)

**Files:** Modify `CrearEmpleadoRequest`/`ActualizarEmpleadoRequest` (DTOs), `CrearEmpleadoCommand`, `ActualizarEmpleadoCommand`, `ReactivarEmpleadoCommand`; tests respectivos.

- [ ] **Step 1 (RED):** Tests:
  - `CrearEmpleado_AsignaUnidades`: con `UnidadIds`, el empleado queda con esas `UsuarioUnidad`.
  - `CrearEmpleado_RolDueno_PorNoAdmin_Lanza`: `actuanteRolId != AdminRolId` y rol asignado == DuenoRolId → excepción (no persiste).
  - `CrearEmpleado_RolDueno_PorAdmin_OK`.
  - `CrearEmpleado_RolDueno_SinUnidades_Lanza` (Dueño necesita ≥1 unidad).
  - `Reactivar_AlRolDueno_PorNoAdmin_Lanza` (cubre el bypass).
  - `Actualizar_AlRolDueno_PorNoAdmin_Lanza`.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Agregar `UnidadIds: Guid[]` a los requests. Los 3 comandos reciben `Guid actuanteRolId` (y, para Crear/Actualizar por Dueño, las unidades del actuante para validar subconjunto). Lógica: sincronizar `UsuarioUnidad` (sin PlanId); validar "rol Dueño solo lo asigna Admin"; validar Dueño con ≥1 unidad; validar subconjunto cuando el actuante es Dueño. Mantener la validación existente "no rol Socio".
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(dueno): asignacion de unidades a empleados + regla solo-Admin-crea-Dueno`.

---

### Task 3: Login de empleado con unidadIds + /auth/me

**Files:** Modify `EmpleadoRepository.cs` (includes), `AuthController.cs`; Test: ajustar/añadir donde haya cobertura de Auth (si no hay test de controller, verificar por compilación + un test de repo si aplica).

- [ ] **Step 1:** `EmpleadoRepository.GetByCorreoAsync`/`GetByIdAsync`/`GetAllAsync` → agregar `.Include(e => e.UnidadesAsignadas)`.
- [ ] **Step 2:** `AuthController.Login` (rama empleado): poblar `unidadIds` en `LoginResponse` desde `empleado.UnidadesAsignadas`. `/auth/me`: si no es socio, resolver empleado por correo y devolver sus `unidadIds`.
- [ ] **Step 3:** `dotnet build` + suite completa → PASS.
- [ ] **Step 4:** Commit `feat(dueno): login de empleado y /auth/me devuelven unidadIds`.

---

### Task 4: `IUnidadesVisiblesResolver`

**Files:** Create `IUnidadesVisiblesResolver.cs` (Application/Interfaces), impl en Infrastructure; Test del impl.

- [ ] **Step 1 (RED):** Tests del resolver: Admin (`rolId==AdminRolId`) → null; Dueño (`rolId==DuenoRolId`) → las UnidadId del empleado (mock repo); otro empleado → null.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** `Task<IReadOnlyCollection<Guid>?> ResolverAsync(Guid userId, Guid rolId)`. Impl: Admin→null; Dueño→`GetByIdAsync(userId)` con include de unidades → sus UnidadId; otro→null.
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(dueno): IUnidadesVisiblesResolver`.

---

### Task 5: Repos — filtro por set de unidades

**Files:** Modify `ISocioRepository`/`SocioRepository`, `IClaseRepository`/`ClaseRepository`, `IHorarioClaseRepository`/`HorarioClaseRepository`, `IEmpleadoRepository`/`EmpleadoRepository`, `ICuotaRepository`/`CuotaRepository`. Tests de repo si existen; si no, se ejercita vía las queries (Task 6).

- [ ] **Step 1:** Agregar a cada repo de listado un parámetro `IReadOnlyCollection<Guid>? unidadesPermitidas` (null = sin restricción) que filtra `.Where(... unidadesPermitidas.Contains(unidadId))`:
  - `SocioRepository.SearchAsync`: agregar el filtro `s.UnidadesAsignadas.Any(uu => unidadesPermitidas.Contains(uu.UnidadId))`.
  - `ClaseRepository`: variante que acepte el set (o reemplazar GetByUnidadId por filtro por set).
  - `HorarioClaseRepository.GetAllAsync`: filtrar por set de UnidadId (vía Clase.UnidadId).
  - `EmpleadoRepository.GetAllAsync`: `.Include(UnidadesAsignadas)` + filtro `e.UnidadesAsignadas.Any(uu => unidadesPermitidas.Contains(uu.UnidadId))`.
  - `CuotaRepository`: para el caso por-socio, exponer un método para validar que el socio pertenece a una unidad permitida (o que la query lo valide).
- [ ] **Step 2:** `dotnet build` → compila (las firmas nuevas con default null no rompen llamadas existentes).
- [ ] **Step 3:** Suite completa → PASS.
- [ ] **Step 4:** Commit `feat(dueno): filtro por set de unidades en los repos de listado`.

---

### Task 6: Queries — propagar `unidadesPermitidas`

**Files:** Modify `GetSociosQuery`, `GetClasesQuery`, `GetHorariosQuery`, `GetEmpleadosQuery`, `GetCuotasAdminQuery`; tests de cada query.

- [ ] **Step 1 (RED):** Por query, test: con `unidadesPermitidas` seteado, solo devuelve los de esas unidades; con null, devuelve todo (comportamiento actual). Para `GetCuotasAdminQuery`: si el socio consultado no pertenece a una unidad permitida → vacío/rechazo.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Agregar el parámetro `unidadesPermitidas` y pasarlo al repo. Mantener compat (default null).
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(dueno): queries de listado respetan unidades visibles`.

---

### Task 7: API — resolver + threading + desacople de RolesController

**Files:** Modify `SociosController`, `ClasesController`, `HorariosController`, `EmpleadosController`, `CuotasController`, `RolesController`; DI.

- [ ] **Step 1:** Registrar `IUnidadesVisiblesResolver` en DI. En cada controller de listado: obtener `userId`+`rolId` del JWT, `unidadesPermitidas = await resolver.ResolverAsync(...)`, pasarlo a la query.
- [ ] **Step 2:** `EmpleadosController`: pasar `actuanteRolId` (del claim `rolId`) a Crear/Actualizar/Reactivar; y las unidades del actuante si es Dueño.
- [ ] **Step 3:** `RolesController`: cambiar `[RequierePermiso(Modulo.Auditoria, ...)]` → `[RequierePermiso(Modulo.Empleados, ...)]` en todos sus endpoints.
- [ ] **Step 4:** `dotnet build` + suite completa → PASS.
- [ ] **Step 5:** Commit `feat(dueno): controllers aplican filtro por unidad + roles via permiso Empleados`.

---

### Task 8: Frontend — unidades de empleado + selector + opción de rol

**Files:** Modify `services/api.ts`, `context/AuthContext.tsx`, form de empleado (`NuevoUsuarioPage`/`EditUsuarioPage`), selector de sede compartido.

- [ ] **Step 1:** AuthContext ya recibe `unidadIds` del login (ahora también para empleados). Form de empleado: permitir asignar unidades (multi-select de sedes); mostrar el rol "Dueño" como opción **solo si** el usuario actuante es Admin.
- [ ] **Step 2:** Selector de sede del admin: para un Dueño, constreñir las opciones a `user.unidadIds` (como el portal del socio). Admin sin cambios.
- [ ] **Step 3:** `npm run build` + `npx vitest run` → PASS.
- [ ] **Step 4:** Commit `feat(dueno): UI de unidades de empleado, selector de sede y opcion de rol Dueno`.

---

### Task 9: Review final + PR

- [ ] Reviewer adversarial: seguridad (filtrado server-side no spoofeable; un Dueño no ve otra unidad por query param; solo Admin crea/reactiva Dueños — los 3 comandos), migración del seed consistente, login empleado con unidadIds, RolesController desacoplado sin romper al Admin, build+suite verdes. Crear PR a develop (nota: Eventos para Dueño queda como follow-up al mergear RF-15).
