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
- **EF:** `NotificacionConfiguration` (FK a `Usuarios` por SocioId, índice por `(SocioId, Leida)` y `FechaCreacion`); `DbSet<Notificacion>`. Migración que crea la tabla `Notificaciones` (se auto-aplica al arrancar).

### Servicio de creación (un solo lugar)
- Interfaz `INotificadorInApp` (Application) con `Task CrearAsync(Guid socioId, TipoNotificacion tipo, string titulo, string mensaje)` y `Task CrearParaVariosAsync(IEnumerable<Guid> socioIds, TipoNotificacion tipo, string titulo, string mensaje)`. Impl en Infrastructure que persiste vía `INotificacionRepository`.
- **Best-effort:** crear la notificación in-app **nunca rompe la operación de negocio** (la inscripción/el pago/el cambio de horario igual se confirman). Se envuelve en try/catch donde corresponda, igual que el email.

### Puntos a enganchar (cada uno: además del email de hoy, crea la notificación)
| Comando | Tipo | Destinatario(s) |
|---|---|---|
| `ProcesarRecordatoriosCommand` (job diario) y `NotificarCuotaCommand` (manual) | `RecordatorioCuota` | el socio de la cuota |
| `UpdateHorarioCommand` (cambio de horario) | `CambioHorario` | socios inscriptos en ese horario |
| `CancelClaseCommand` (cancelación de clase) | `CancelacionClase` | socios inscriptos en los horarios de la clase |
| `InscribirSocioCommand` (confirmación) | `ConfirmacionInscripcion` | el socio inscripto |
| `CrearEventoCommand` (RF-15) | `EventoNuevo` | socios activos de la unidad |
- En cada uno, **después** de la operación de negocio (y junto al envío de email), se invoca `INotificadorInApp`. Si crear la notificación falla, se traga (best-effort) y la operación igual se confirma. El mensaje reusa la info que ya arma el email (sin HTML).
- **N-11** (máx. un recordatorio del mismo tipo por socio por día) ya se respeta a nivel de `RecordatorioCuota` en `ProcesarRecordatoriosCommand` (dedupe por `RecordatorioCuota` existente); la notificación in-app se crea solo cuando efectivamente se notifica, así que no duplica.

### API (portal, self-service)
- En `PortalController` (resuelve el socio por correo del JWT, patrón existente, sin `[Authorize]`):
  - `GET /api/portal/notificaciones?soloNoLeidas=&take=` → lista de notificaciones del socio (orden `FechaCreacion` desc; `take` por defecto 20).
  - `GET /api/portal/notificaciones/no-leidas/count` → `{ count }` para el badge.
  - `POST /api/portal/notificaciones/{id}/leer` → marca una como leída (valida ownership: la notificación debe ser del socio del JWT).
  - `POST /api/portal/notificaciones/leer-todas` → marca todas las del socio como leídas.
- Queries/commands: `GetNotificacionesQuery`, `ContarNoLeidasQuery`, `MarcarNotificacionLeidaCommand` (valida ownership), `MarcarTodasLeidasCommand`.
- DTO `NotificacionDto` (Id, Tipo, Titulo, Mensaje, Leida, FechaCreacion).

### Frontend (portal)
- **Campana/badge** en el header del portal (`SocioLayout`) con el contador de no leídas (consulta `no-leidas/count`, refresca al abrir).
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
