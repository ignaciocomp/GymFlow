# RF-15 Gestión de Eventos — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: usar superpowers:subagent-driven-development. Cada tarea con TDD estricto (test rojo → mínimo verde → suite completa → commit). Pasos con checkbox `- [ ]`.

**Goal:** ABM de eventos especiales por sede, con notificación por email a los socios y vista de eventos en el portal.

**Architecture:** Clean Architecture estándar siguiendo el patrón de Clases: entidad `Evento` (soft-delete), Command/Query en Application, `[RequierePermiso(Modulo.Eventos, ...)]` en API, email best-effort tipo `CancelClaseCommand` (persistir+auditar antes de enviar, secuencial por socio). Portal self-service por correo del JWT (patrón `PortalController`).

**Tech Stack:** .NET 8, EF Core (PostgreSQL), xUnit+Moq. Frontend React 18 + TS + Tailwind + TanStack Query.

**Spec:** [[spec-rf15-eventos]]. **Rama:** `feature/rf15-eventos` (base develop).

**Regla de oro:** mensajes al usuario en español rioplatense. Tras CADA tarea: `dotnet test backend/GymFlow.sln` 100% verde antes del commit. Seguí los patrones del repo (Command/Query concretos con AddScoped, repos con interfaz, plantillas de email con HtmlEncode).

---

## File Structure
- `backend/src/GymFlow.Domain/Enums/Modulo.cs` — (modif) agregar `Eventos`
- `backend/src/GymFlow.Domain/Entities/Evento.cs` — (nuevo)
- `backend/src/GymFlow.Application/Interfaces/IEventoRepository.cs` — (nuevo)
- `backend/src/GymFlow.Application/Interfaces/ISocioRepository.cs` — (modif) `GetActivosByUnidadAsync`
- `backend/src/GymFlow.Application/DTOs/EventoDtos.cs` — (nuevo) EventoDto + requests
- `backend/src/GymFlow.Application/UseCases/Eventos/*` — (nuevo) queries/commands/mapper/templates
- `backend/src/GymFlow.Infrastructure/Repositories/EventoRepository.cs` + `SocioRepository.cs` (modif) — (nuevo/modif)
- `backend/src/GymFlow.Infrastructure/Persistence/Configurations/EventoConfiguration.cs` + `GymFlowDbContext.cs` (DbSet) + Migration — (nuevo/modif)
- `backend/src/GymFlow.API/Controllers/EventosController.cs` (nuevo) + `PortalController.cs` (modif) + DI — (nuevo/modif)
- `frontend/src/types/permisos.ts` (modif), `types/index.ts` (modif), `services/api.ts` (modif), `pages/admin/EventosPage.tsx` + Nuevo/Editar (nuevo), `pages/portal/EventosPortalPage.tsx` (nuevo), `Sidebar.tsx` (modif), router/nav (modif)

---

### Task 1: Enum `Modulo.Eventos` + entidad `Evento` + métodos de dominio

**Files:** Modify `Modulo.cs`; Create `Evento.cs`; Test `backend/tests/GymFlow.Domain.Tests/Entities/EventoTests.cs`

- [ ] **Step 1:** Agregar `Eventos` al enum `Modulo` (append al final).
- [ ] **Step 2 (RED):** Tests de `Evento`:
  - `Ctor_ConTituloVacio_Lanza` (ArgumentException).
  - `Ctor_SeteaCamposYActivo`: título/desc/fecha/unidadId seteados, `EstaActivo==true`, `FechaCreacion` seteada.
  - `Actualizar_CambiaCampos` (incluye permitir fecha pasada — el dominio NO valida fecha pasada).
  - `Cancelar_DesactivaYReactivar_Activa`.
- [ ] **Step 3:** Correr → FAIL (clase inexistente). Verificar.
- [ ] **Step 4 (GREEN):** Implementar `Evento` (setters privados; ctor `(titulo, descripcion, fecha, unidadId)` validando título; `Actualizar(titulo, descripcion, fecha)`, `Cancelar()`, `Reactivar()`; nav `Unidad`). NO validar fecha pasada en el dominio.
- [ ] **Step 5:** `dotnet test --filter EventoTests` → PASS; suite completa → PASS.
- [ ] **Step 6:** Commit `feat(eventos): enum Modulo.Eventos + entidad Evento`.

---

### Task 2: Persistencia — repos + EF config + migración (tabla + permisos)

**Files:** Create `IEventoRepository.cs`, `EventoRepository.cs`, `EventoConfiguration.cs`; Modify `ISocioRepository.cs`, `SocioRepository.cs`, `GymFlowDbContext.cs`; Migration.

- [ ] **Step 1:** `IEventoRepository`: `GetAllAsync(Guid? unidadId, bool incluirInactivos)`, `GetByIdAsync(Guid)`, `GetProximosByUnidadesAsync(IEnumerable<Guid> unidadIds, DateTime ahora)`, `AddAsync(Evento)`, `void Update(Evento)`, `SaveChangesAsync()`. Todas las lecturas con `.Include(e => e.Unidad)`. `GetAll` ordena por `Fecha`; `GetProximos` filtra `EstaActivo && Fecha >= ahora && unidadIds.Contains(UnidadId)` orden por `Fecha`.
- [ ] **Step 2:** Agregar `Task<IEnumerable<Socio>> GetActivosByUnidadAsync(Guid unidadId)` a `ISocioRepository` + impl en `SocioRepository` (`s.EstaActivo && s.UnidadesAsignadas.Any(uu => uu.UnidadId == unidadId)`).
- [ ] **Step 3:** `EventoConfiguration` (FK a `Unidad` por `UnidadId`, índices `UnidadId` y `Fecha`, `Titulo` requerido). `DbSet<Evento> Eventos` en `GymFlowDbContext`. Implementar `EventoRepository`.
- [ ] **Step 4:** Migración: `dotnet ef migrations add AgregarEventos --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`. Editar la migración generada para que, además de crear la tabla `Eventos`, **inserte las 4 filas de `Permisos` para `Modulo.Eventos`** (Lectura/Escritura/Modificacion/Eliminacion) y los `RolPermisos` que las asignan al **rol Admin** — copiar el patrón EXACTO de `20260502204656_AddEmpleadosYPermisosEmpleados.cs` (mismos Guids fijos generados, mismas tablas/columnas). Verificar el `Down`.
- [ ] **Step 5:** `dotnet build` + suite completa → PASS.
- [ ] **Step 6:** Commit `feat(eventos): persistencia (repos + EF config + migracion con permisos)`.

---

### Task 3: Queries de admin + DTO + mapper

**Files:** Create `EventoDtos.cs`, `UseCases/Eventos/GetEventosQuery.cs`, `GetEventoByIdQuery.cs`, `EventoMapper.cs`; Tests en `backend/tests/GymFlow.Application.Tests/UseCases/Eventos/`.

- [ ] **Step 1 (RED):** Tests:
  - `GetEventos_FiltraPorUnidad_MapeaDto`: con mock de `IEventoRepository`, devuelve `EventoDto` con `UnidadNombre` poblado.
  - `GetEventoById_NoExiste_DevuelveNull` (o lanza KeyNotFound según patrón de `GetClaseByIdQuery` — seguilo).
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** `EventoDto(Id, Titulo, Descripcion, Fecha, UnidadId, UnidadNombre, EstaActivo)`; `EventoMapper.ToDto`; las dos queries.
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(eventos): queries de admin + EventoDto + mapper`.

---

### Task 4: `CrearEventoCommand` + email a socios

**Files:** Create `UseCases/Eventos/CrearEventoCommand.cs`, `EventoEmailTemplates.cs`; Test `CrearEventoCommandTests.cs`.

- [ ] **Step 1 (RED):** Tests:
  - `Crear_PersisteYNotificaSocios`: valida unidad existente, persiste el evento (AddAsync+SaveChanges), obtiene socios activos de la unidad (`GetActivosByUnidadAsync`) y manda email a cada uno (`EnviarAsync` Times = nº socios), audita `Creacion`.
  - `Crear_ConFechaPasada_Lanza`: fecha < `DateTime.UtcNow` → ArgumentException, no persiste.
  - `Crear_SiEmailFalla_ElEventoIgualSeCrea`: `EnviarAsync` lanza/Exitoso=false → el evento queda persistido (SaveChanges llamado antes), no se propaga excepción.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Implementar: validar título (vía ctor de Evento) + fecha no pasada (en el command, contra `DateTime.UtcNow`) + unidad existe; normalizar Fecha a UTC; `AddAsync`+`SaveChangesAsync`; auditar `Creacion`; LUEGO enviar emails best-effort secuencial (try/catch por socio) con `EventoEmailTemplates.Notificacion` (HtmlEncode en todo). Auditar conteo enviados/fallidos en el detalle.
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(eventos): CrearEventoCommand con notificacion por email`.

---

### Task 5: Actualizar + Cancelar + Notificar commands

**Files:** Create `ActualizarEventoCommand.cs`, `CancelarEventoCommand.cs`, `NotificarEventoCommand.cs` + tests.

- [ ] **Step 1 (RED):** Tests: `Actualizar_CambiaCamposYAudita` (permite fecha pasada); `Cancelar_BajaLogicaYAudita`; `Notificar_EnviaEmailASociosDeLaUnidad` (reusa el helper de envío; best-effort).
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Implementar los 3 (reusar la lógica de envío de Task 4, extraída a un helper o repetida según el repo). Auditoría `Modificacion`/`Baja`.
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(eventos): Actualizar + Cancelar + Notificar commands`.

---

### Task 6: Query del portal

**Files:** Create `UseCases/Eventos/GetEventosPortalQuery.cs` + test.

- [ ] **Step 1 (RED):** Test `GetEventosPortal_DevuelveProximosDeMisUnidades`: dado un socio (por correo) con N unidades, devuelve solo eventos activos con `Fecha >= ahora` de esas unidades, ordenados; no incluye pasados/cancelados/de otras unidades.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** `GetEventosPortalQuery.ExecuteAsync(correoSocio)`: `GetByCorreoAsync` → unidades del socio → `IEventoRepository.GetProximosByUnidadesAsync(unidadIds, DateTime.UtcNow)` → `EventoDto`.
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(eventos): query de eventos del portal`.

---

### Task 7: API — `EventosController` + endpoint del portal + DI

**Files:** Create `Controllers/EventosController.cs`; Modify `Controllers/PortalController.cs`, `GymFlow.API/DependencyInjection.cs`.

- [ ] **Step 1:** Registrar en DI los commands/queries (AddScoped), `IEventoRepository`→`EventoRepository`.
- [ ] **Step 2:** `EventosController` (patrón `ClasesController`): GET `?unidadId=&incluirInactivos=` `[RequierePermiso(Eventos,Lectura)]`, GET `/{id}` `[Lectura]`, POST `[Escritura]`, PUT `/{id}` `[Modificacion]`, DELETE `/{id}` `[Eliminacion]`, POST `/{id}/notificar` `[Escritura]`. Mapear `ArgumentException`→`BadRequest`, `KeyNotFoundException`→`NotFound`.
- [ ] **Step 3:** `PortalController`: GET `/api/portal/eventos` (resuelve socio por correo del JWT como el resto del controller) → `GetEventosPortalQuery`.
- [ ] **Step 4:** `dotnet build` + suite completa → PASS.
- [ ] **Step 5:** Commit `feat(eventos): EventosController + endpoint portal/eventos (API)`.

---

### Task 8: Frontend admin — EventosPage + sidebar

**Files:** Modify `types/permisos.ts`, `types/index.ts`, `services/api.ts`, `Sidebar.tsx`, router; Create `pages/admin/EventosPage.tsx` (+ Nuevo/Editar inline o páginas).

- [ ] **Step 1:** `permisos.ts`: agregar `'Eventos'` al union `Modulo`. `types/index.ts`: tipo `Evento`. `api.ts`: `eventosApi` (getAll(unidadId), getById, create, update, cancel, notificar).
- [ ] **Step 2:** `EventosPage` (patrón `ClasesPage`/`HorariosPage`): filtro de sede obligatorio, listado (tabla `sm+` / cards mobile), crear/editar/cancelar, botón "Notificar". Para frontend usar el plugin UI/UX Pro Max si aplica.
- [ ] **Step 3:** `Sidebar.tsx`: ítem "Eventos" con `puedeLeer(Modulo.Eventos)`. Ruta en el router.
- [ ] **Step 4:** `npm run build` + `npx vitest run` → PASS.
- [ ] **Step 5:** Commit `feat(eventos): UI admin de eventos (ABM + notificar)`.

---

### Task 9: Frontend portal — EventosPortalPage

**Files:** Modify `services/api.ts` (portalApi.getEventos), portal nav/router; Create `pages/portal/EventosPortalPage.tsx`.

- [ ] **Step 1:** `portalApi.getEventos()` → GET `/portal/eventos`.
- [ ] **Step 2:** `EventosPortalPage` (patrón `HorariosPortalPage`): lista de próximos eventos de las sedes del socio (título, descripción, fecha, sede), solo lectura, responsive. Ítem en el nav del portal + ruta.
- [ ] **Step 3:** `npm run build` + `npx vitest run` → PASS.
- [ ] **Step 4:** Commit `feat(eventos): vista de eventos en el portal del socio`.

---

### Task 10: Review final + PR

- [ ] Reviewer adversarial sobre el diff completo vs develop: permisos (módulo Eventos seedeado al Admin; 403 sin permiso), email best-effort (evento persiste si falla, orden persist→email, HtmlEncode), portal filtra por sedes propias + próximos + activos, validación de fecha contra UtcNow, migración consistente (tabla + permisos), build backend+frontend y suites completas verdes. Crear el PR a develop.
