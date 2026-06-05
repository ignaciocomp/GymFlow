---
tags:
  - spec
  - iteracion
requerimiento: RF-10, RF-11, RN-09
---

# Inscripcion por Horario - Spec de Cambio de Diseno

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] - RF-10, RF-11, RF-09, RN-09, CU-02
**Spec base:** [[spec-it4-inscripciones-empleados-horarios]]
**Plan de implementacion:** [[plan-inscripcion-por-horario]]
**Ultima actualizacion:** 2026-06-05

## Resumen

La inscripcion a clases se realiza por `HorarioClaseId`. Un socio se inscribe a un horario concreto de una clase, no a la clase generica.

Ejemplo: "Yoga, lunes 08:00-09:00" y "Yoga, miercoles 18:00-19:00" son inscripciones independientes.

La lista de espera fue desestimada. Si no hay cupo disponible en un horario, el sistema rechaza la inscripcion.

## Modelo de datos

| Entidad | Estado vigente |
|---------|----------------|
| `InscripcionClase` | Tiene `HorarioClaseId`, `SocioId`, `FechaInscripcion`, `EstaActiva`. |
| `HorarioClase` | Mantiene FK a `Clase`; desde aqui se obtiene nombre, instructor, unidad y capacidad. |
| `Clase` | Mantiene `CapacidadMaxima`; no recibe inscripciones directas. |

No existe campo `EsListaEspera`.

## API

| Endpoint | Contrato |
|----------|----------|
| `POST /api/inscripciones` | `{ horarioClaseId }` |
| `GET /api/inscripciones/mis-inscripciones` | DTO con `HorarioClaseId`, `ClaseId`, `DiaSemana`, `HoraInicio`, `HoraFin`, `Sala` |
| `DELETE /api/inscripciones/{id}` | Cancela la inscripcion propia |

## Reglas de negocio

| Regla | Definicion vigente |
|-------|--------------------|
| RN-09 | Un socio no puede inscribirse dos veces al mismo horario. |
| Mismo curso en otro horario | Permitido si el horario es distinto. |
| Cupo | Se cuenta por `HorarioClaseId`. |
| Sin cupo | Se rechaza la inscripcion; no se crea lista de espera. |
| Clase cancelada | No permite nuevas inscripciones y cancela inscripciones activas existentes al cancelar la clase. |

## Frontend

- `HorariosPortalPage` (`/portal/horarios`) es la vista de inscripcion.
- `MisInscripcionesPage` muestra clase, sede, dia, hora y sala.
- `CatalogoClasesPage` y `/portal/clases` se eliminaron por redundancia.

## Impacto en otros componentes

| Componente | Impacto |
|------------|---------|
| `ClaseDto` | No expone `InscripcionesActivas`, porque el cupo no se mide por clase. |
| `GetHorariosQuery` | Calcula inscripciones activas por `HorarioClaseId`. |
| `UpdateClaseCommand` | Valida capacidad contra el maximo de inscripciones entre sus horarios. |
| `CancelClaseCommand` | Cancela inscripciones activas de todos los horarios de la clase. |
| Email templates | Incluyen dia, hora y sala. |

## Trazabilidad

| Criterio (CU-02) | Estado |
|------------------|--------|
| CA-05 (sin cupo) | Cubierto: bloquea la inscripcion. |
| CA-07 (cupo decrementa) | Cubierto por conteo activo por horario. |
| CA-08 (aparece en Mis Clases) | Cubierto con dia/hora/sala. |
| CA-09 (notificacion) | Cubierto con email de confirmacion. |
| RN-09 | Cubierto: no duplica el mismo horario. |
