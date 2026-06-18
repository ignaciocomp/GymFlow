---
tags:
  - spec
  - iteracion
requerimiento: RF-15, N-11
---

# RF-15 — Gestión de Eventos — Spec

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] — RF-15 (gestionar eventos y notificar socios), parcialmente RF-16 (notificación por email). Regla N-11.
**Plan de implementación:** [[plan-rf15-eventos]]
**Última actualización:** 2026-06-17

## Resumen

El administrador crea **eventos especiales** (torneos, charlas, promociones) por sede y los socios de esa sede son notificados por **email** y pueden verlos en su portal. Cubre el ABM de eventos con baja lógica, el envío de notificación (best-effort, reusando el servicio de email existente) y una vista de eventos próximos en el portal del socio.

## Decisiones de diseño

### Alcance
- **En alcance:** entidad `Evento`, ABM de eventos para el admin (scopeado por sede), notificación por **email** a los socios de la unidad al crear un evento (+ acción manual de re-notificar), y una **vista de "Eventos" de solo lectura** en el portal del socio (próximos eventos de su/s sede/s).
- **Fuera de alcance (queda para RF-16 / IT6):** el **centro de notificaciones in-system** persistente (entidad `Notificacion` con inbox leído/no leído, que también agrupa recordatorios de cuota y cambios de horario), y el patrón Observer del dashboard. La "notificación dentro del sistema" de RF-15 se satisface con la vista de eventos del portal; el inbox unificado es RF-16.

### Modelo de datos
- **Entidad `Evento`** (Domain): `Id` (Guid), `Titulo` (string, requerido), `Descripcion` (string), `Fecha` (DateTime UTC — fecha/hora del evento), `UnidadId` (Guid, FK a `Unidad`), `EstaActivo` (bool, default true — baja lógica como `Clase`/`Plan`), `FechaCreacion` (DateTime UTC). Navegación a `Unidad`.
- **Mutación por métodos de dominio** (setters privados, como el resto): ctor con validación (título no vacío), `Actualizar(titulo, descripcion, fecha)`, `Cancelar()` (setea `EstaActivo=false`), `Reactivar()`.
- **EF:** `EventoConfiguration` (FK a Unidad, índices por `UnidadId` y `Fecha`); `DbSet<Evento>`. Migración que crea la tabla `Eventos` (se auto-aplica al arrancar). Las queries que devuelven `EventoDto` con `UnidadNombre` deben `.Include(e => e.Unidad)` (como `GetClasesQuery`).
- **Repositorios:** `IEventoRepository` (GetAllAsync(unidadId?, incluirInactivos), GetByIdAsync, GetProximosByUnidadesAsync(IEnumerable<Guid> unidadIds, DateTime ahora) para el portal, AddAsync, Update, SaveChangesAsync). Para los socios a notificar: **agregar `Task<IEnumerable<Socio>> GetActivosByUnidadAsync(Guid unidadId)` a `ISocioRepository`** (impl en `SocioRepository` filtrando `s.EstaActivo && s.UnidadesAsignadas.Any(uu => uu.UnidadId == unidadId)`); es más limpio que reusar el `SearchAsync` genérico (que trae includes de Plan innecesarios).

### Permisos
- Nuevo valor en el enum `Modulo` (Domain): **`Eventos`**. Migración que inserta las **4 filas de permiso** (una por operación) y los `RolPermisos` que **asignan las 4 operaciones al rol Admin** — plantilla exacta: la migración `20260502204656_AddEmpleadosYPermisosEmpleados.cs`. Los controllers usan `[RequierePermiso(Modulo.Eventos, Operacion.X)]`.
- **Frontend:** agregar `'Eventos'` al union `Modulo` en `frontend/src/types/permisos.ts` (definición duplicada del enum), si no, `puedeLeer(Modulo.Eventos)` en el Sidebar no typechequea.
- El portal del socio (lectura de eventos) es **self-service**. El endpoint vive en `PortalController` (ruta `/api/portal/*`) y **resuelve el socio por el correo del JWT** (patrón de `PortalController.ExtractClaims` + `GetByCorreoAsync`, que ya incluye `UnidadesAsignadas`), NO por socioId. De ahí deriva las `UnidadId` del socio.

### Casos de uso (Application — patrón Command/Query)
- **Queries:** `GetEventosQuery` (admin: filtra por `unidadId`, opción de incluir inactivos), `GetEventoByIdQuery`, `GetEventosPortalQuery` (socio: resuelve sus `UnidadId` desde `socio.UnidadesAsignadas` y trae próximos eventos `Fecha >= DateTime.UtcNow` y activos, ordenados por fecha).
- **Commands:** `CrearEventoCommand` (valida unidad existente y **fecha no pasada contra `DateTime.UtcNow`** —en el command, no en el dominio, para que `Actualizar` pueda ajustar fechas—; normaliza la `Fecha` a UTC; **persiste y audita ANTES de enviar emails**; luego **notifica por email** a los socios de la unidad; audita `Creacion` con el conteo enviados/fallidos en el detalle), `ActualizarEventoCommand` (audita `Modificacion`), `CancelarEventoCommand` (baja lógica; audita `Baja`), `NotificarEventoCommand` (re-envía el email a los socios de la unidad; audita).
- DTO `EventoDto` (Id, Titulo, Descripcion, Fecha, UnidadId, UnidadNombre, EstaActivo).

### Notificación por email
- Plantilla `EventoEmailTemplates.Notificacion(socio, evento)` siguiendo el estilo de `ClaseEmailTemplates`/`InscripcionEmailTemplates`: método estático que devuelve `(Asunto, Cuerpo)` con `WebUtility.HtmlEncode` en **todo** valor dinámico (título, descripción, nombre del socio, sede). Asunto tipo "Nuevo evento en {sede}: {título}".
- Envío **best-effort**, replicando EXACTAMENTE el patrón de `CancelClaseCommand`: **primero `SaveChangesAsync()` del evento + auditoría, después** se obtiene la lista de socios activos (`GetActivosByUnidadAsync`) y se envía con **`Task.WhenAll`** (paralelo), contando `EmailResultado.Exitoso` para el detalle de auditoría (enviados/fallidos). `IEmailService.EnviarAsync` **no lanza** (devuelve `EmailResultado` con `Exitoso=false` ante fallo), así que el best-effort se expresa por ese flag, no por try/catch. El evento queda creado aunque los emails fallen (porque se persiste ANTES de enviar; hay test de eso con `Exitoso=false`).
- Sin cola/async nuevo (YAGNI). El detalle de auditoría incluye el conteo enviados/fallidos.
- **N-11** (no más de un recordatorio del mismo tipo por socio por día) aplica a los recordatorios automáticos de cuota; para eventos, la notificación se dispara una vez al crear y manualmente vía "Notificar" (no es un job diario), así que N-11 no agrega lógica acá.

### API (endpoints)
| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| GET | `/api/eventos?unidadId=&incluirInactivos=` | `[RequierePermiso(Eventos, Lectura)]` | Lista eventos (admin), filtrado por sede |
| GET | `/api/eventos/{id}` | `[RequierePermiso(Eventos, Lectura)]` | Detalle |
| POST | `/api/eventos` | `[RequierePermiso(Eventos, Escritura)]` | Crear (+ notifica socios) |
| PUT | `/api/eventos/{id}` | `[RequierePermiso(Eventos, Modificacion)]` | Editar |
| DELETE | `/api/eventos/{id}` | `[RequierePermiso(Eventos, Eliminacion)]` | Cancelar (baja lógica) |
| POST | `/api/eventos/{id}/notificar` | `[RequierePermiso(Eventos, Escritura)]` | Re-enviar email a socios de la sede |
| GET | `/api/portal/eventos` | `[Authorize]` | (socio) Próximos eventos de sus sedes |

### Frontend
- **Admin — `EventosPage`** (patrón `ClasesPage`/`HorariosPage`): filtro de sede obligatorio, listado de eventos (tabla en `sm+`, cards en mobile), crear/editar/cancelar vía diálogo o páginas Nuevo/Editar, botón "Notificar". Ítem nuevo en el `Sidebar` filtrado por `puedeLeer(Modulo.Eventos)`.
- **Portal — `EventosPortalPage`** (patrón `HorariosPortalPage`): lista de próximos eventos de la/s sede/s del socio (título, descripción, fecha, sede), solo lectura. Ítem en el nav del portal.
- `services/api.ts`: `eventosApi` (getAll, getById, create, update, cancel, notificar) y `portalApi.getEventos`. Tipos en `types/`.

### Validaciones
- Título obligatorio (no vacío) — `ArgumentException` → el controller mapea a `BadRequest` (patrón `ClasesController.Create`).
- Fecha del evento no puede ser pasada al **crear** (comparada contra `DateTime.UtcNow`, validación en `CrearEventoCommand`); al **editar** se permite ajustar (la validación no está en el dominio ni en `Actualizar`). La `Fecha` recibida se normaliza a UTC antes de persistir (Npgsql `timestamptz` exige `Kind=Utc`).
- Unidad obligatoria y existente.

## Criterios de aceptación

- El admin con permiso puede crear un evento en una sede; queda listado y auditado (`Creacion`).
- Al crear un evento, los socios **activos** de esa sede reciben un email con los datos del evento; si el envío a alguno falla, el evento igual queda creado.
- El admin puede editar y cancelar (baja lógica) un evento; un evento cancelado no aparece en el portal ni en la lista activa.
- El botón "Notificar" reenvía el email a los socios de la sede.
- Un socio ve en su portal solo los eventos **próximos y activos** de **sus** sedes, ordenados por fecha; no ve los de otras sedes ni los pasados/cancelados.
- Crear un evento con título vacío o fecha pasada es rechazado con un mensaje claro.
- Un usuario sin permiso `Eventos` no puede acceder a los endpoints de gestión (403); el módulo aparece en el sidebar solo si tiene lectura.
- Todo valor dinámico del email se escapa con `HtmlEncode`.

## Fuera de alcance

- Centro de notificaciones in-system unificado / entidad `Notificacion` con inbox (RF-16).
- Notificaciones push / dentro del sistema en tiempo real (Observer/SSE — RF-16/RNF-02).
- Inscripción/cupo a eventos (los eventos son informativos, no tienen cupo).
- Recordatorios automáticos de eventos por job diario (la notificación es al crear + manual).
