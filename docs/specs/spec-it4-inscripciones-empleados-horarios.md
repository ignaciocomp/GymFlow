---
tags:
  - spec
  - iteracion
requerimiento: IT4
---

# Iteracion 4 - Estabilizacion de Inscripciones, Empleados y Horarios

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] (seccion Iteracion 4)
**Ultima actualizacion:** 2026-06-05

## Resumen

La Iteracion 4 estabiliza la inscripcion a clases, la vista de horarios y la gestion de empleados/profesores.

La inscripcion se implementa **por horario individual**: un socio se inscribe a un `HorarioClaseId`, por ejemplo "Yoga, lunes 08:00", no a la clase generica. Ver detalle en [[spec-inscripcion-por-horario]] y [[plan-inscripcion-por-horario]].

La lista de espera queda **fuera de alcance y no se implementa**. Si un horario no tiene cupos disponibles, el sistema rechaza la inscripcion con conflicto y no crea ningun registro alternativo.

RF-13 y RF-14 se consideran cubiertos por el sistema de roles y permisos configurables desde interfaz. Un administrador puede crear roles como "Profesor" y asignar permisos especificos por modulo sin introducir una relacion fija profesor-clase.

## Alcance

### Incluido

| #   | Componente                                                                    | Mapea a             |
| --- | ----------------------------------------------------------------------------- | ------------------- |
| 1   | Inscripcion por `HorarioClaseId` con validacion de cupo y duplicados          | RF-10, RN-09, CU-02 |
| 2   | Notificacion por email y auditoria de inscripcion/cancelacion                 | CA-09, RNF-11       |
| 3   | Vista "Mis Inscripciones" con dia, hora y sala                                | RF-11               |
| 4   | Horarios del portal como vista unica de inscripcion                           | CU-02               |
| 5   | Horarios admin con filtro de sede obligatorio y split simple de solapamientos | RF-09, RNF-03       |
| 6   | Credenciales temporales por email al crear empleado                           | RF-12, CA-29        |
| 7   | Tests de dominio, aplicacion y build frontend                                 | Calidad             |

### Fuera de alcance

- Lista de espera: no se crea entidad, flag, DTO ni promocion automatica.
- Asignacion fija profesor-clase: se resuelve por roles/permisos configurables desde interfaz.
- MFA TOTP y OAuth para socios: quedan para IT5.
- Forzar cambio de password en primer login: deuda tecnica futura; existe flujo manual de cambio de password.

## Decisiones de diseno

- **Inscripcion por horario:** `InscripcionClase` referencia `HorarioClaseId`. La clase se obtiene mediante `InscripcionClase.HorarioClase.Clase`.
- **Cupo por horario:** los conteos de ocupacion se calculan por `HorarioClaseId`; `CapacidadMaxima` sigue viviendo en `Clase`.
- **Sin lista de espera:** cuando `inscripcionesActivas >= capacidadMaxima`, `InscribirSocioCommand` lanza error y no persiste inscripcion.
- **Emails best-effort:** `IEmailService` devuelve `EmailResultado`; si SMTP falla, el servicio lo registra y la operacion principal ya confirmada no se revierte.
- **RF-14 por permisos:** el rol "Profesor" y sus capacidades se modelan con roles dinamicos y permisos por modulo, no con una FK profesor-clase.

## Cambios de modelo

| Entidad | Cambio |
|---------|--------|
| `InscripcionClase` | Usa `HorarioClaseId` como FK obligatoria. |
| `Clase` | Mantiene `CapacidadMaxima`; no tiene inscripciones directas. |
| `HorarioClase` | Expone cupos ocupados mediante queries de inscripciones. |

No existe campo `EsListaEspera` ni migracion vigente de lista de espera.

## Cambios de API

| Endpoint | Contrato vigente |
|----------|------------------|
| `POST /api/inscripciones` | Recibe `{ horarioClaseId }`; rechaza si no hay cupo, si el horario no existe, si la clase esta cancelada o si hay duplicado. |
| `DELETE /api/inscripciones/{id}` | Cancela la inscripcion propia del socio y audita la baja. |
| `GET /api/inscripciones/mis-inscripciones` | Devuelve `HorarioClaseId`, `ClaseId`, dia, hora, sala, capacidad y ocupacion. |
| `POST /api/empleados` | No recibe password; autogenera una contrasena temporal y la envia por email. |

## DTO de inscripcion

`InscripcionClaseDto` expone:

- `Id`
- `HorarioClaseId`
- `ClaseId`
- `ClaseNombre`
- `Instructor`
- `UnidadId`
- `UnidadNombre`
- `DiaSemana`
- `HoraInicio`
- `HoraFin`
- `Sala`
- `CapacidadMaxima`
- `InscripcionesActivas`
- `FechaInscripcion`

No expone campos de lista de espera.

## Repositorios y queries

`IInscripcionClaseRepository` usa metodos por horario:

```csharp
Task<InscripcionClase?> GetActivaBySocioYHorarioAsync(Guid socioId, Guid horarioClaseId);
Task<int> GetInscripcionesActivasCountAsync(Guid horarioClaseId);
Task<Dictionary<Guid, int>> GetConteoActivasPorHorariosAsync(IEnumerable<Guid> horarioClaseIds);
Task<IEnumerable<InscripcionClase>> GetActivasByHorarioClaseIdAsync(Guid horarioClaseId);
```

`IClaseRepository` no contiene metodos de conteo de inscripciones por clase.

## Frontend

- `HorariosPortalPage` es la vista donde el socio se inscribe a un horario especifico.
- `MisInscripcionesPage` muestra dia, hora y sala.
- `CatalogoClasesPage` y la ruta `/portal/clases` no existen.
- Cuando un horario esta lleno, el boton queda deshabilitado y se muestra "Cupo lleno".

## Trazabilidad

| Criterio | Estado |
|----------|--------|
| CA-05 / sin cupo | Cubierto: se bloquea la inscripcion. |
| CA-07 / cupo decrementa | Cubierto por conteo de activas por horario. |
| CA-08 / aparece en Mis Clases | Cubierto en `MisInscripcionesPage`. |
| CA-09 / notificacion | Cubierto con `InscripcionEmailTemplates.Confirmacion`. |
| RN-09 / no duplicado | Cubierto por `GetActivaBySocioYHorarioAsync`. |
| RF-12 / empleados | Cubierto con credenciales temporales por email. |
| RF-13/RF-14 | Cubierto por roles y permisos configurables desde interfaz. |
