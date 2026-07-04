# RF-18 Dashboard en Tiempo Real — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development. TDD estricto por tarea (test rojo → implementación → verde → commit). Checkboxes para tracking.

**Goal:** Dashboard operativo de solo lectura con 4 métricas en vivo (SSE, ≤30s), filtro por unidad, gráfica con selector de vista, acceso por permiso del módulo Dashboard, y como pantalla de inicio del admin.

**Spec:** `docs/specs/spec-rf18-dashboard.md` · **CU:** `docs/Casos_de_uso/CU-10-dashboard-tiempo-real.md`
**Branch:** `feature/rf18-dashboard` (off main) → **PR con base `develop`** (NO main — el usuario lleva develop→main él mismo). NO mergear el PR.

**Convenciones verificadas:** commands/queries en `API/DependencyInjection.cs` (`AddScoped`); repos en `Infrastructure/DependencyInjection.cs`; permisos con `[RequierePermiso(Modulo.X, Operacion.Y)]`; filtrado multi-sede con `IUnidadesVisiblesResolver` (ver `EventosController`); seeds de RolPermisos en `GymFlowDbContext` con `DeterministicGuid` (ver el patrón del módulo Eventos y el commit de PR #42); migraciones: `dotnet ef migrations add "X" --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`. Tests: `dotnet test` desde backend/; `npm test` + `npm run build` desde frontend/.

---

## Task 1: `Modulo.Dashboard` + seed de permisos + migración
- **Files:** `Domain/Enums/Modulo.cs`; `Infrastructure/Persistence/GymFlowDbContext.cs` (seed RolPermisos: permiso **Lectura** de Dashboard para los roles **Admin** y **Dueño**, con DeterministicGuid como Eventos); migración `AddDashboardPermisos`; **actualizar tests de seed existentes** (`SeedRolDuenoTests` cuenta permisos exactos — pasa de 25 a 26 — y cualquier test análogo de Admin).
- TDD: primero ajustar los tests de seed al nuevo conteo/módulo (rojo), luego seed + migración (verde). `dotnet ef migrations has-pending-model-changes` limpio al final.
- Commit: `feat(dashboard): modulo Dashboard con permiso de lectura (seed + migracion) (#RF-18)`

## Task 2: Agregados en repos (lo que falte)
- Revisar `ISocioRepository` (ya tiene `CountActivosByUnidadAsync`), `ICuotaRepository`, `IHorarioRepository`/clases, `IInscripcionRepository` y agregar SOLO los métodos de agregado que falten para: counts de cuotas por estado/vencimiento (próximas a vencer = Pendiente con vencimiento futuro cercano según la lógica ya usada por recordatorios; vencidas = Pendiente con fechaVencimiento < hoy; pagadas del mes), horarios del día de la semana actual con inscriptos, últimas 10 inscripciones, e inscripciones por día (últimos 7 días). Con `Guid? unidadId` o lista de unidades donde aplique.
- TDD con EF InMemory (patrón de los repo-tests existentes). Commit: `feat(dashboard): agregados de metricas en repositorios (#RF-18)`

## Task 3: `GetDashboardQuery` + `DashboardDto`
- `Application/UseCases/Dashboard/`: DTO como en el spec §2; `ExecuteAsync(Guid? unidadId, IReadOnlyCollection<Guid>? unidadesPermitidas)`; si `unidadId` no está en las permitidas → `UnauthorizedAccessException`; `unidadesPermitidas == null` = todas (admin). Registrar DI.
- TDD (mocks de repos): consolidado suma todas las unidades; filtro por unidad; dueño restringido; unidad no permitida lanza; series de la gráfica correctas. Commit: `feat(dashboard): GetDashboardQuery con metricas y series (#RF-18)`

## Task 4: `DashboardController` (snapshot + SSE)
- `GET /api/dashboard?unidadId=` → snapshot vía query + `IUnidadesVisiblesResolver` (patrón EventosController). `[RequierePermiso(Modulo.Dashboard, Operacion.Lectura)]`.
- `GET /api/dashboard/stream?unidadId=` → SSE: `Response.ContentType = "text/event-stream"`, `Response.Headers` no-cache, loop `while (!ct.IsCancellationRequested)`: snapshot → serializar (camelCase como el resto de la API) → si difiere del último enviado escribir `data: {json}\n\n` sino `: ping\n\n` → `FlushAsync` → `Task.Delay(10s, ct)`. La comparación "cambió o no" en un helper puro testeable. Primer snapshot SIEMPRE se envía.
- TDD: attrs por reflexión + snapshot endpoint con mocks; helper de diff. Commit: `feat(dashboard): endpoints snapshot y stream SSE (#RF-18)`

## Task 5: Frontend — `dashboardApi` + hook `useDashboardStream`
- `services/api.ts`: `dashboardApi.get(unidadId?)`. Hook en `src/hooks/useDashboardStream.ts`: estado `{ data, live, actualizadoEn }`; carga inicial GET; stream por `fetch('/api/dashboard/stream…', { headers: { Authorization } })` leyendo el body como stream y parseando líneas `data:`; `AbortController` al desmontar/cambiar unidad; en error → reintento con backoff (2 intentos) y luego **polling cada 15s** con `live=false`.
- El token: leer del mismo lugar que usa el interceptor de axios (localStorage `gymflow_token`).
- TDD del hook (mock fetch): datos iniciales, transición a live, degradación a polling. Commit: `feat(dashboard): api y hook de stream con fallback a polling (#RF-18)`

## Task 6: Frontend — `DashboardPage` (diseñar con plugin ui-ux-pro-max)
- `pages/admin/DashboardPage.tsx`: 4 metric cards (socios activos + desglose, próximas a vencer, vencidas, clases de hoy) · Select de unidad ("Todas" default) · **gráfica recharts con selector de vista** (sociosPorSede barras / cuotasPorEstado barras / inscripciones7dias línea; elección en localStorage `gymflow_dashboard_grafica`) · tabla/lista clases del día (clase, sede, hora, inscriptos/cupo) · lista inscripciones recientes · badge 🟢 En vivo / ⏸ Actualización en pausa + hora `actualizadoEn`. Estilo del panel admin actual (shadcn), responsive. `npm i recharts`.
- TDD: renderiza métricas desde mock; cambia vista de gráfica y persiste; badge pausado con `live=false`; ceros sin error. Commit: `feat(dashboard): pagina del dashboard con metricas, grafica y vivo (#RF-18)`

## Task 7: Routing + nav + landing por permiso
- `App.tsx`: `/admin/dashboard` → `DashboardPage` (borrar el redirect viejo). Index de `/admin`: componente chico que mira los permisos del AuthContext (mismo shape que usa el nav para gatear módulos — leer AdminLayout) → `Navigate` a `dashboard` si tiene Dashboard-Lectura, sino `socios`.
- `AdminLayout`: ítem "Dashboard" (ícono LayoutDashboard) gateado por el permiso, primero en el menú.
- TDD: con permiso aterriza en dashboard; sin permiso en socios y sin ítem de menú. Commit: `feat(dashboard): dashboard como inicio del admin gateado por permiso (#RF-18)`

## Task 8: Verificación final + PR a develop
- `dotnet test` (backend) + `npm test` + `npm run build` (frontend) → todo verde; `has-pending-model-changes` limpio.
- Smoke opcional con dev server (preview): cargar /admin/dashboard, ver métricas y el indicador.
- Push + `gh pr create --base develop --title "feat(rf18): dashboard en tiempo real multi-espacio (SSE + permisos)"` — body: qué incluye, la migración aditiva de permisos, decisión SSE-por-fetch y fallback, gráfica con selector, landing por permiso, conteos de tests, y nota de que la base es develop a pedido del usuario. **NO mergear.**
