---
tags:
  - spec
  - iteracion
requerimiento: RF-16, N-03, N-05, N-11
---

# RF-16 — Notificaciones in-system para el Socio — Spec

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] — RF-16 (el socio recibe avisos de eventos, recordatorios de cuota y cambios de horario por email **y dentro del sistema**).
**Plan de implementación:** [[plan-rf16-notificaciones]]
**Spec relacionada:** [[spec-rf15-eventos]] (esta rama se apoya en RF-15; el evento ya notifica por email).
**Última actualización:** 2026-06-17
**Nota de branching:** rama `feature/rf16-notificaciones` apilada sobre `feature/rf15-eventos` (necesita la entidad `Evento`). Mergear RF-15 (#38) primero; luego rebasar/mergear esta.

## Resumen

Hoy las notificaciones al socio son **solo por email** (recordatorios de cuota, cambios/cancelaciones de clase, confirmación de inscripción, eventos). RF-16 agrega el canal **"dentro del sistema"**: una entidad `Notificacion` persistida por socio y un **buzón (inbox) en el portal** con contador de no leídas y marcado de leídas. Cada punto donde hoy se manda un email pasa a **también** crear una notificación in-app (best-effort, sin romper la operación).

## Decisiones de diseño

### Modelo de datos
- **Entidad `Notificacion`** (Domain): `Id` (Guid), `SocioId` (Guid, FK), `Tipo` (`TipoNotificacion`), `Titulo` (string), `Mensaje` (string), `Leida` (bool, default false), `FechaCreacion` (DateTime UTC), `FechaLectura` (DateTime? UTC). Métodos de dominio: ctor con validación (título/mensaje no vacíos), `MarcarLeida(DateTime ahora)` (idempotente: si ya está leída, no cambia la fecha).
- **Enum `TipoNotificacion`** (Domain): `RecordatorioCuota`, `CambioHorario`, `CancelacionClase`, `ConfirmacionInscripcion`, `EventoNuevo`. (Se usa para iconito/categoría en el front; ampliable por append.)
- **EF:** `NotificacionConfiguration`: relación **a `Socio`** (`HasOne<Socio>().WithMany().HasForeignKey(n => n.SocioId)` — `Socio : Usuario` TPH, la FK resuelve contra `Usuarios`), `OnDelete(DeleteBehavior.Cascade)`, índice por `(SocioId, Leida)` y `FechaCreacion`. `DbSet<Notificacion>`. Migración que crea la tabla `Notificaciones` (se auto-aplica al arrancar).

### Servicio de creación (un solo lugar)
- Interfaz `INotificadorInApp` (Application) con `Task CrearAsync(Guid socioId, TipoNotificacion tipo, string titulo, string mensaje)` y `Task CrearParaVariosAsync(IEnumerable<Guid> socioIds, TipoNotificacion tipo, string titulo, string mensaje)`. Impl en Infrastructure que persiste vía `INotificacionRepository`.
- **Commit independiente (clave para que el best-effort sea real):** `INotificadorInApp` hace **su propio `SaveChangesAsync`** (un commit aparte del de la operación de negocio). `CrearParaVariosAsync` hace **`AddRange` + un solo `SaveChanges`** (no un loop de saves — evita N+1 y rompe el batching de los jobs). La llamada al notificador va **siempre después** del `SaveChangesAsync` de la operación de negocio y **envuelta en try/catch**: si falla, se traga (best-effort) y la operación igual queda confirmada.
- **No se mezcla con el ChangeTracker de la operación:** como hace su propio save, nunca "ensucia" un `SaveChanges` posterior del comando de negocio.

### Puntos a enganchar (cada uno: además del email de hoy, crea la notificación)
| Comando | Tipo | Destinatario(s) |
|---|---|---|
| `ProcesarRecordatoriosCommand` (job diario) y `NotificarCuotaCommand` (manual) | `RecordatorioCuota` | el socio de la cuota |
| `UpdateHorarioCommand` (cambio de horario) | `CambioHorario` | socios inscriptos en ese horario |
| `CancelClaseCommand` (cancelación de clase) | `CancelacionClase` | socios inscriptos en los horarios de la clase |
| `InscribirSocioCommand` (confirmación) | `ConfirmacionInscripcion` | el socio inscripto |
| `CrearEventoCommand` (RF-15) | `EventoNuevo` | socios activos de la unidad |
- En cada uno, **después** del `SaveChanges` de la operación de negocio (y junto al envío de email), se invoca `INotificadorInApp`. El mensaje reusa la info que ya arma el email (sin HTML).
- **La notificación in-app se crea solo en la rama de éxito**, no incondicionalmente:
  - `ProcesarRecordatoriosCommand`: hace **un solo `SaveChanges` al final** (batch). Por eso se **junta el conjunto de `socioId` efectivamente notificados** (rama `enviados`, no los omitidos por N-11/sin-correo) y se llama **`CrearParaVariosAsync` una sola vez al final** (fuera del loop), con su propio save y try/catch.
  - `NotificarCuotaCommand`: hoy lanza excepción si el email falla. La notificación in-app se crea **solo si `resultado.Exitoso`** (no cuando el mail falla y el flujo termina en 500).
  - Los demás (`UpdateHorario`, `CancelClase`, `InscribirSocio`, `CrearEvento`) crean la notificación tras su `SaveChanges` de negocio, best-effort.
- **N-11** (máx. un recordatorio del mismo tipo por socio por día) ya se respeta en `ProcesarRecordatoriosCommand` (dedupe por `RecordatorioCuota`); como la notificación in-app se crea solo para los efectivamente notificados, no duplica.

### API (portal, self-service)
- En `PortalController`. **El `socioId` se toma del claim `NameIdentifier` del JWT** (como hacen `InscripcionesController.GetSocioId()` y `CuotasController` — 1 query menos por request, importante porque el badge se pollea seguido), **no** por correo. Ownership: la notificación a marcar debe tener `SocioId == socioIdDelJwt` (si no → 404/403).
  - `GET /api/portal/notificaciones?soloNoLeidas=&take=` → lista de notificaciones del socio (orden `FechaCreacion` desc; `take` por defecto 20).
  - `GET /api/portal/notificaciones/no-leidas/count` → `{ count }` para el badge.
  - `POST /api/portal/notificaciones/{id}/leer` → marca una como leída (valida ownership: la notificación debe ser del socio del JWT).
  - `POST /api/portal/notificaciones/leer-todas` → marca todas las del socio como leídas.
- Queries/commands: `GetNotificacionesQuery`, `ContarNoLeidasQuery`, `MarcarNotificacionLeidaCommand` (valida ownership), `MarcarTodasLeidasCommand`.
- DTO `NotificacionDto` (Id, Tipo, Titulo, Mensaje, Leida, FechaCreacion).

### Frontend (portal)
- **Campana/badge** en el header del portal (`SocioLayout`) con el contador de no leídas: `useQuery(['notif-count'])` con `refetchInterval` (~45s) + `refetchOnWindowFocus`, e invalidación tras marcar leída(s). (Sin tiempo real; el polling cubre el MVP.)
- **Inbox**: panel/desplegable o página `NotificacionesPortalPage` con la lista (título, mensaje, fecha, ícono por tipo, estado leído/no leído), botón "marcar como leída" por ítem y "marcar todas". Al abrir el inbox o clickear una, se marca leída y el badge se actualiza.
- `portalApi`: `getNotificaciones`, `contarNoLeidas`, `marcarLeida`, `marcarTodasLeidas`. Reusa TanStack Query con invalidación del contador.

## Criterios de aceptación
- Al ocurrir cada evento (recordatorio de cuota, cambio de horario, cancelación de clase, inscripción, evento nuevo), además del email se crea una `Notificacion` para el/los socio/s correspondiente/s con el tipo correcto.
- Si la creación de la notificación in-app falla, la operación de negocio (pago/inscripción/cambio) igual se confirma (best-effort).
- El socio ve en su portal sus notificaciones, ordenadas por fecha desc, con el contador de no leídas; **solo las suyas** (ownership por correo del JWT).
- Marcar una notificación como leída la pasa a leída (idempotente) y baja el contador; "marcar todas" deja todas leídas.
- Un socio no puede marcar como leída una notificación de otro socio (ownership validado → 403/404).

## Fuera de alcance
- Notificaciones in-app para **empleados/admin** (RF-16 es del socio).
- Tiempo real / push (WebSocket/SignalR/SSE) — el badge se actualiza por polling/refetch (RNF-02/Observer queda para más adelante).
- Preferencias de notificación (opt-out por tipo).
- Borrado de notificaciones por el socio (solo marcar leídas).
