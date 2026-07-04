# Spec — RF-18 / CU-10: Dashboard en tiempo real multi-espacio

- **RF:** RF-18 (Dashboard en tiempo real) + RNF-02 (actualización sin recarga vía SSE).
- **CU fuente:** [CU-10](../Casos_de_uso/CU-10-dashboard-tiempo-real.md) — flujos, reglas RN-14/15/16/17 y criterios CA-01..CA-05 aplican tal cual.
- **Fecha:** 2026-07-04 · **Iteración:** 6 · **Rama:** `feature/rf18-dashboard` (off main) → **PR con base `develop`** (decisión del usuario; él lleva develop→main después).

## 1. Decisiones acordadas con el usuario

| Decisión | Elección |
|---|---|
| Tiempo real | **SSE con snapshot periódico (~10s)**: el backend recalcula y empuja solo si cambió (+ heartbeat). Cumple RNF-02 y el ≤30s (RN-15). **Fallback a polling** con indicador "actualización en pausa" (E2). |
| Auth del stream | `EventSource` no permite header Authorization y poner el JWT en la URL está prohibido → **lector SSE por `fetch`** con header. |
| Acceso | Nuevo **`Modulo.Dashboard`** en el sistema de permisos (solo **Lectura**; seed para Admin y Dueño + migración). Sin vistas por rol (CA-04). |
| Landing | **El dashboard pasa a ser el inicio de `/admin`** si el rol tiene el permiso; si no, Socios como hoy. |
| Visual | 4 cards de métricas + clases del día + inscripciones recientes (lo del CU) **+ una gráfica con selector de vista** (elegible por el usuario, persistida en localStorage): *Socios por sede* · *Cuotas por estado* · *Inscripciones últimos 7 días*. Librería `recharts`. |
| Filtro | "Todas" (consolidado, RN-14) por defecto + por unidad. Server-side con `IUnidadesVisiblesResolver` (Dueño solo sus sedes). |

## 2. Backend (.NET 8, Clean Arch — patrón del módulo Eventos)

- **`Modulo.Dashboard`** al enum + seed en `GymFlowDbContext` de permiso Lectura para los roles Admin y Dueño + **migración** (`AddDashboardPermisos`). ⚠️ Actualizar los tests de seed existentes (ej. `SeedRolDuenoTests` cuenta permisos exactos).
- **`GetDashboardQuery`** (Application/UseCases/Dashboard): `ExecuteAsync(Guid? unidadId, IReadOnlyCollection<Guid>? unidadesPermitidas)` → `DashboardDto`:
  - `generadoEn` (UTC), `unidades` visibles (para el filtro)
  - `sociosActivos { total, porUnidad[] }`
  - `cuotas { proximasAVencer, vencidas, pagadasMes }` — **calculado en vivo** (RN-17)
  - `clasesDelDia[] { clase, unidad, horaInicio, horaFin, cupo, inscriptos }` (horarios del día de la semana actual)
  - `inscripcionesRecientes[]` (últimas 10: socio, clase, unidad, fecha)
  - `grafica { sociosPorSede[], cuotasPorEstado[], inscripcionesUltimos7Dias[] }`
  - Si `unidadId` no está entre las permitidas → excepción/vacío (validación server-side).
  - Métodos de agregado nuevos en repos existentes donde falten (counts, recientes) — reusar `CountActivosByUnidadAsync` etc.
- **`DashboardController`**:
  - `GET /api/dashboard?unidadId=` — snapshot (carga inicial + polling de fallback). `[RequierePermiso(Modulo.Dashboard, Operacion.Lectura)]`.
  - `GET /api/dashboard/stream?unidadId=` — **SSE** (`text/event-stream`): loop con `CancellationToken`, cada ~10s calcula el snapshot; si el JSON difiere del último enviado → `data: {json}\n\n`; si no → comentario heartbeat `: ping\n\n`. Sin buffering. Mismo permiso.
  - El stream con scale-to-zero es correcto: la conexión mantiene la réplica viva mientras el dashboard esté abierto.

## 3. Frontend (panel admin)

- **`DashboardPage`** (`/admin/dashboard`, reemplaza el redirect actual):
  - 4 cards (socios activos con desglose por sede, cuotas próximas a vencer, cuotas vencidas, clases de hoy) · filtro de unidad · gráfica con **selector de vista** (recharts; elección persistida en localStorage) · lista clases del día (cupo/inscriptos) · lista inscripciones recientes · indicador **🟢 En vivo / ⏸ Pausado** + "actualizado hh:mm:ss".
  - Unidad sin datos → ceros (E3), no error.
- **`useDashboardStream(unidadId)`** (hook): carga inicial por `GET /api/dashboard`; abre el stream por fetch-SSE (header Authorization del interceptor/token); parsea eventos `data:`; en error/cierre → reintento con backoff y **degradación a polling cada 15s** con `live=false`.
- **Landing:** el index de `/admin` navega a `dashboard` si el usuario tiene el permiso Dashboard-Lectura (leer de AuthContext/permisos como hace el nav actual), sino a `socios`. Ítem "Dashboard" en el nav del AdminLayout gateado por permiso (patrón Eventos).
- Dep nueva: `recharts` (solo frontend).

## 4. Testing (TDD)

- **Backend:** query (agregados correctos con datos seed InMemory; filtro por unidad; `unidadesPermitidas` restringe al Dueño; unidad no permitida rechazada), controller (attrs de permiso por reflexión + comportamiento del snapshot), seeds actualizados. La lógica "enviar solo si cambió" del stream se aisla en un helper testeable; el loop SSE en sí se prueba a mano/E2E.
- **Frontend:** page renderiza cards/listas desde datos mock; filtro dispara re-fetch; indicador pausado cuando el hook reporta `live=false`; selector de gráfica cambia la vista y persiste.

## 5. Criterios de aceptación
Los **CA-01..CA-05 del CU-10** tal cual (consolidado al cargar; ≤30s sin recarga; filtro respeta al Dueño; acceso solo con permiso del módulo; solo lectura).

## 6. Fuera de alcance (YAGNI)
Push por eventos de dominio; métricas históricas/rangos de fecha arbitrarios; export; personalización de layout de cards; WebSockets.
