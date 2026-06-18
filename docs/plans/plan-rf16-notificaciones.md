# RF-16 Notificaciones in-system — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: usar superpowers:subagent-driven-development. TDD estricto por tarea (rojo → verde → suite completa → commit). Pasos con checkbox `- [ ]`.

**Goal:** Centro de notificaciones in-system para el socio: entidad `Notificacion`, inbox en el portal, y enganche en los puntos donde hoy se manda email.

**Architecture:** Entidad `Notificacion` (FK a Socio). `INotificadorInApp` con **commit propio** (best-effort, llamado después del save de negocio, en try/catch). Cada punto de notificación crea la notificación en su rama de éxito. Portal resuelve el `socioId` por el claim `NameIdentifier`.

**Tech Stack:** .NET 8, EF Core (PostgreSQL), xUnit+Moq. Frontend React 18 + TS + TanStack Query.

**Spec:** [[spec-rf16-notificaciones]] — leerla, tiene el detalle. **Rama:** `feature/rf16-notificaciones` (apilada sobre `feature/rf15-eventos`, que tiene `Evento`).

**Regla de oro:** español rioplatense. Tras CADA tarea `dotnet test backend/GymFlow.sln` 100% verde. Best-effort: crear la notificación nunca rompe la operación de negocio.

---

### Task 1: Entidad `Notificacion` + enum `TipoNotificacion`

**Files:** Create `Domain/Enums/TipoNotificacion.cs`, `Domain/Entities/Notificacion.cs`; Test `backend/tests/GymFlow.Domain.Tests/Entities/NotificacionTests.cs`.

- [ ] **Step 1:** Enum `TipoNotificacion { RecordatorioCuota, CambioHorario, CancelacionClase, ConfirmacionInscripcion, EventoNuevo }`.
- [ ] **Step 2 (RED):** Tests: ctor con título/mensaje vacío → ArgumentException; ctor setea campos, `Leida==false`, `FechaCreacion` seteada; `MarcarLeida(ahora)` → `Leida==true` + `FechaLectura`; idempotente (segunda llamada no cambia la fecha).
- [ ] **Step 3:** Correr → FAIL. Verificar.
- [ ] **Step 4 (GREEN):** `Notificacion` (Id, SocioId, Tipo, Titulo, Mensaje, Leida, FechaCreacion, FechaLectura?; setters privados; ctor `(socioId, tipo, titulo, mensaje)` validando; `MarcarLeida(DateTime)`).
- [ ] **Step 5:** Tests + suite → PASS. Commit `feat(notificaciones): entidad Notificacion + enum TipoNotificacion`.

---

### Task 2: Persistencia — repo + EF config + migración

**Files:** Create `INotificacionRepository.cs`, `NotificacionRepository.cs`, `NotificacionConfiguration.cs`; Modify `GymFlowDbContext.cs` (DbSet); Migration.

- [ ] **Step 1:** `INotificacionRepository`: `AddRangeAsync(IEnumerable<Notificacion>)`, `GetBySocioAsync(Guid socioId, bool soloNoLeidas, int take)`, `ContarNoLeidasAsync(Guid socioId)`, `GetByIdAsync(Guid)`, `MarcarTodasLeidasAsync(Guid socioId, DateTime ahora)`, `SaveChangesAsync()`.
- [ ] **Step 2:** `NotificacionConfiguration`: `HasOne<Socio>().WithMany().HasForeignKey(n => n.SocioId)`, `OnDelete(Cascade)`, índices `(SocioId, Leida)` y `FechaCreacion`. `DbSet<Notificacion>` en el context. Implementar el repo.
- [ ] **Step 3:** Migración `AgregarNotificaciones` (tabla `Notificaciones`). Verificar solo tabla + índices + FK.
- [ ] **Step 4:** `dotnet build` + suite → PASS. Commit `feat(notificaciones): persistencia (repo + EF config + migracion)`.

---

### Task 3: `INotificadorInApp` (commit propio, best-effort)

**Files:** Create `INotificadorInApp.cs` (Application/Interfaces), `NotificadorInApp.cs` (Infrastructure); Test del impl.

- [ ] **Step 1 (RED):** Tests: `CrearAsync` persiste 1 notificación (mock repo, verifica AddRange + SaveChanges una vez); `CrearParaVariosAsync` con N socioIds → AddRange de N + UN solo SaveChanges (no loop).
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** `CrearAsync(socioId, tipo, titulo, mensaje)` y `CrearParaVariosAsync(socioIds, tipo, titulo, mensaje)`. **CLAVE — aislamiento real:** el impl NO usa el `GymFlowDbContext` scoped compartido (su `SaveChanges` flushearía cambios de negocio pendientes). Usa un **`IDbContextFactory<GymFlowDbContext>`**: crea un contexto efímero (`await using var ctx = await _factory.CreateDbContextAsync()`), `ctx.Notificaciones.AddRange(...)`, **un** `await ctx.SaveChangesAsync()`. Registrar `AddDbContextFactory<GymFlowDbContext>(...)` en DI (además del scoped existente). NO maneja try/catch (lo hace el caller best-effort).
- [ ] **Step 4:** Tests + suite → PASS. Commit `feat(notificaciones): INotificadorInApp con commit propio`.

---

### Task 4: Enganche en Cuotas e Inscripción

**Files:** Modify `NotificarCuotaCommand`, `ProcesarRecordatoriosCommand`, `InscribirSocioCommand`; ajustar sus tests.

- [ ] **Step 1 (RED):** Tests (mockeando `INotificadorInApp`):
  - `NotificarCuota_EmailOk_CreaNotificacion`: solo si `resultado.Exitoso`. `EmailFalla_NoCrea` (la notif no se crea cuando el mail falla / el flujo lanza).
  - `ProcesarRecordatorios_CreaNotificacionesEnBatch`: junta los socioId notificados (rama `Exitoso`) y llama `CrearParaVariosAsync` UNA vez **después** del `SaveChangesAsync` de los recordatorios (l.70).
  - `InscribirSocio_CreaNotificacionConfirmacion` (best-effort: si el notificador lanza, la inscripción igual se confirma).
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Inyectar `INotificadorInApp` en los 3 comandos. **Actualizar el helper `CrearCommand()`/constructor de `NotificarCuotaCommandTests`, `ProcesarRecordatoriosCommandTests`, `InscribirSocioCommandTests` agregando `Mock<INotificadorInApp>`** (si no, la suite no compila). Crear la notificación **después** del save de negocio, en la rama de éxito, envuelta en try/catch (best-effort). Detalles por comando:
  - `NotificarCuotaCommand`: crear la notif **entre** el save (l.64) y el throw por email fallido (l.71), **solo si `resultado.Exitoso`**.
  - `ProcesarRecordatoriosCommand`: agregar `var sociosNotificados = new List<Guid>()` (no existe hoy), `Add(cuota.SocioId)` solo en la rama `Exitoso` (l.66); tras el `SaveChangesAsync` final, `CrearParaVariosAsync(sociosNotificados, RecordatorioCuota, ...)`. (Por-cuota está bien: puede haber >1 por socio/día, igual que el email.)
  - `InscribirSocioCommand`: notif `ConfirmacionInscripcion` tras el save (l.49).
- [ ] **Step 4:** Tests + suite → PASS. Commit `feat(notificaciones): enganche en cuotas e inscripcion`.

---

### Task 5: Enganche en Horarios, Clases y Eventos

**Files:** Modify `UpdateHorarioCommand`, `CancelClaseCommand`, `CrearEventoCommand`; ajustar tests.

- [ ] **Step 1 (RED):** Tests: cada uno crea notificación(es) para los socios afectados (cambio de horario → inscriptos; cancelación → inscriptos; evento → socios activos de la unidad), best-effort tras el save. Tipos: `CambioHorario`, `CancelacionClase`, `EventoNuevo`.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Inyectar `INotificadorInApp`; **actualizar los helpers/constructores de `UpdateHorarioCommandTests`, `CancelClaseCommandTests`, `CrearEventoCommandTests` con `Mock<INotificadorInApp>`**. Usar `CrearParaVariosAsync` con los socioIds afectados (ya disponibles: inscripciones con Socio incluido; eventos: `GetActivosByUnidadAsync`). Tras el save de negocio (que en estos 3 ocurre antes de los emails), try/catch best-effort.
- [ ] **Step 4:** Tests + suite → PASS. Commit `feat(notificaciones): enganche en horarios, clases y eventos`.

---

### Task 6: Queries/commands del portal + DTO

**Files:** Create `UseCases/Notificaciones/*` (GetNotificacionesQuery, ContarNoLeidasQuery, MarcarNotificacionLeidaCommand, MarcarTodasLeidasCommand), `NotificacionDto`; tests.

- [ ] **Step 1 (RED):** Tests:
  - `GetNotificaciones_DevuelveDelSocioOrdenadas` (desc por fecha; filtro soloNoLeidas; take).
  - `ContarNoLeidas`.
  - `MarcarLeida_DeOtroSocio_Lanza` (ownership: la notif debe ser del socioId dado, si no → KeyNotFound/Unauthorized).
  - `MarcarLeida_Propia_Ok` (idempotente).
  - `MarcarTodasLeidas`.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Implementar las 2 queries y 2 commands. `NotificacionDto(Id, Tipo, Titulo, Mensaje, Leida, FechaCreacion)`. Ownership validada por `socioId`.
- [ ] **Step 4:** Tests + suite → PASS. Commit `feat(notificaciones): queries y commands del portal`.

---

### Task 7: API — endpoints del portal

**Files:** Modify `PortalController.cs`; DI.

- [ ] **Step 1:** Registrar en DI. **OJO con el patrón de auth de `PortalController`:** NO tiene `[Authorize]` ni `GetSocioId()`; valida el JWT a mano con `ExtractClaims()` y tiene `GetCurrentUser(claims)` que devuelve el `userId` del `NameIdentifier`. En cada endpoint nuevo: `var claims = ExtractClaims(); if (claims is null) return Unauthorized(...); var socioId = GetCurrentUser(claims).UserId;` (ese `userId` ES el socioId para un socio logueado). Ownership en marcar-leída: `notif.SocioId == socioId`. Endpoints:
  - `GET /api/portal/notificaciones?soloNoLeidas=&take=`
  - `GET /api/portal/notificaciones/no-leidas/count`
  - `POST /api/portal/notificaciones/{id}/leer`
  - `POST /api/portal/notificaciones/leer-todas`
- [ ] **Step 2:** `dotnet build` + suite → PASS. Commit `feat(notificaciones): endpoints del portal`.

---

### Task 8: Frontend — campana, badge e inbox

**Files:** Modify `services/api.ts` (portalApi), `components/layout/SocioLayout.tsx` (campana+badge); Create `pages/portal/NotificacionesPortalPage.tsx` (o panel desplegable); ruta en `App.tsx`.

- [ ] **Step 1:** `portalApi`: `getNotificaciones`, `contarNoLeidas`, `marcarLeida`, `marcarTodasLeidas`. Tipo `Notificacion`.
- [ ] **Step 2:** Campana en `SocioLayout` con badge de no leídas: `useQuery(['notif-count'])` con `refetchInterval` ~45s + `refetchOnWindowFocus`. Inbox (página o desplegable) con la lista (ícono por tipo, leído/no leído), "marcar leída" por ítem y "marcar todas"; invalidar el contador tras las mutaciones. Para frontend usar el plugin UI/UX Pro Max si aplica.
- [ ] **Step 3:** Validación: **`npm run build` + `npm run lint`** → PASS. (El repo NO tiene infra de tests de frontend —ni script `test` ni vitest config—; no se promete `vitest`.) Commit `feat(notificaciones): campana, badge e inbox en el portal`.

---

### Task 9: Review final + PR

- [ ] Reviewer adversarial: best-effort real (commit propio del notificador; operación se confirma si la notif falla), batch en el job (un solo save), ownership en marcar-leída, los 6 puntos enganchados en su rama de éxito, FK/migración consistente, build+suite verdes. Crear PR (nota: apilado sobre RF-15 #38 — mergear #38 primero).
