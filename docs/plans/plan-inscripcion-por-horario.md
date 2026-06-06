# Inscripcion por Horario - Plan vigente

#plan

**Spec:** [[spec-inscripcion-por-horario]]
**Spec base:** [[spec-it4-inscripciones-empleados-horarios]]

## Objetivo

Mantener la inscripcion a clases basada en `HorarioClaseId`, sin lista de espera y con cupo controlado por horario.

## Cambios de dominio e infraestructura

- `InscripcionClase` contiene `HorarioClaseId`, `SocioId`, `FechaInscripcion` y `EstaActiva`.
- `InscripcionClaseConfiguration` mapea FK a `HorarioClase`.
- La migracion vigente cambia la FK desde `ClaseId` a `HorarioClaseId`.
- No hay migracion ni columna vigente para lista de espera.

## Cambios de aplicacion

- `IInscripcionClaseRepository` expone metodos por horario:
  - `GetActivaBySocioYHorarioAsync`
  - `GetInscripcionesActivasCountAsync`
  - `GetConteoActivasPorHorariosAsync`
  - `GetActivasByHorarioClaseIdAsync`
- `InscribirSocioCommand`:
  - valida existencia de horario;
  - bloquea clases canceladas;
  - bloquea duplicado por socio y horario;
  - bloquea cupo lleno;
  - crea inscripcion solo si hay cupo;
  - envia email de confirmacion;
  - audita creacion.
- `CancelarInscripcionCommand`:
  - valida ownership;
  - cancela la inscripcion;
  - audita baja.
- `GetMisInscripcionesQuery`:
  - agrupa conteos por `HorarioClaseId` para evitar N+1.

## Cambios de API

- `InscribirSocioRequest` contiene `HorarioClaseId`.
- `InscripcionClaseDto` contiene datos de clase, sede, horario y cupo.
- El DTO no contiene campos de lista de espera.

## Cambios de frontend

- `inscripcionesApi.inscribirse(horarioClaseId)` envia `{ horarioClaseId }`.
- `HorariosPortalPage` llama a la inscripcion con `h.id`.
- `MisInscripcionesPage` muestra dia, hora y sala.
- `/portal/clases` no existe.

## Verificacion esperada

```powershell
cd backend
dotnet test
```

```powershell
cd frontend
npm.cmd run build
```
