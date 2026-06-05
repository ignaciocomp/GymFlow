---
tags:
  - spec
  - iteracion
requerimiento: RF-10, RF-11, RN-09
---

# Inscripción por Horario — Spec de Cambio de Diseño

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] — RF-10, RF-11, RF-09, RN-09, CU-02
**Spec base:** [[spec-it4-inscripciones-empleados-horarios]] (§13)
**Plan de implementación:** [[plan-inscripcion-por-horario]]
**Última actualización:** 2026-06-05
**Historial:**
- 2026-06-05 — v1: spec inicial del cambio de modelo de inscripción

## Resumen

La inscripción a clases pasa de ser **por clase** (`InscripcionClase.ClaseId`) a ser **por horario** (`InscripcionClase.HorarioClaseId`). Un socio se inscribe a "Yoga los lunes de 8 a 10", no a "Yoga" genéricamente.

**Motivación:** El modelo original permitía que un socio se inscribiera a una clase y quedara automáticamente anotado en todos sus horarios. En la práctica, un socio elige horarios específicos según su disponibilidad. La inscripción por horario es más natural y da control granular de cupo por franja horaria.

## Cambios al modelo de datos

### Entidad `InscripcionClase`

| Antes | Después |
|-------|---------|
| `ClaseId` (Guid, FK → Clase) | `HorarioClaseId` (Guid, FK → HorarioClase) |
| `Clase` (navigation) | `HorarioClase` (navigation) |

La clase se accede vía `InscripcionClase.HorarioClase.Clase`.

### Entidad `Clase`

- Se **elimina** la colección `Inscripciones` (ya no hay FK directa).
- `CapacidadMaxima` se **mantiene** en `Clase` — es la capacidad de la actividad, compartida por todos sus horarios.

### Entidad `HorarioClase`

Sin cambios.

### Migración

- Eliminar columna `ClaseId` de tabla `InscripcionesClase`
- Agregar columna `HorarioClaseId` (Guid, NOT NULL, FK → HorariosClase, CASCADE)

## Cambios a la API

| Endpoint | Antes | Después |
|----------|-------|---------|
| `POST /api/inscripciones` | `{ claseId }` | `{ horarioClaseId }` |
| `GET /api/inscripciones/mis-inscripciones` | DTO con `ClaseId` | DTO con `HorarioClaseId` + `ClaseId` + `DiaSemana` + `HoraInicio` + `HoraFin` + `Sala` |
| `DELETE /api/inscripciones/{id}` | Sin cambio de contrato | Sin cambio de contrato |

### `InscripcionClaseDto` actualizado

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `HorarioClaseId` | Guid | **Nuevo** — FK al horario inscripto |
| `ClaseId` | Guid | Derivado de `HorarioClase.ClaseId` |
| `DiaSemana` | DiaSemana | **Nuevo** — día del horario |
| `HoraInicio` | string | **Nuevo** — "HH:mm" |
| `HoraFin` | string | **Nuevo** — "HH:mm" |
| `Sala` | string? | **Nuevo** — sala del horario |
| (resto) | — | Sin cambios |

## Reglas de negocio actualizadas

| Regla | Antes | Después |
|-------|-------|---------|
| RN-09 | "No puede inscribirse dos veces a la misma clase" | "No puede inscribirse dos veces al mismo horario" |
| Cupo | Contado por `ClaseId` | Contado por `HorarioClaseId` |
| Lista de espera | Por clase | Por horario |
| Duplicado check | `GetActivaBySocioYClaseAsync` | `GetActivaBySocioYHorarioAsync` |

Un socio **puede** inscribirse a la misma clase en **distintos horarios** (ej: Yoga lunes y Yoga miércoles son inscripciones independientes).

## Cambios al frontend

### Vista eliminada
- `CatalogoClasesPage` (`portal/clases`) — redundante con la inscripción desde horarios.

### Vista unificada
- `HorariosPortalPage` (`portal/horarios`) — el socio ve la grilla semanal y se inscribe con un botón por horario.

### Vista actualizada
- `MisInscripcionesPage` — ahora muestra día, hora y sala de cada inscripción.

### Navegación
- Se elimina el tab "Clases" del `SocioLayout`.

## Impacto en otros componentes

| Componente | Impacto |
|------------|---------|
| `ClaseDto` | Se elimina `InscripcionesActivas` (ya no tiene sentido por clase) |
| `GetClasesQuery` / `GetClaseByIdQuery` | Se simplifica — no consulta conteo de inscripciones |
| `UpdateClaseCommand` | Validación de capacidad usa el máximo de inscripciones entre todos los horarios |
| `CancelClaseCommand` | Cancela inscripciones de todos los horarios de la clase |
| `GetHorariosQuery` | Conteo de inscripciones por `HorarioClaseId` (en vez de por `ClaseId`) |
| Email templates | Incluyen día y hora del horario en el cuerpo |

## Trazabilidad

| Criterio (CU-02) | Estado |
|-------------------|--------|
| CA-05 (sin cupo) | ✅ Por horario |
| CA-07 (cupo decrementa) | ✅ Por horario |
| CA-08 (aparece en Mis Clases) | ✅ Con día/hora |
| CA-09 (notificación) | ✅ Email con día/hora |
| E1 (lista de espera) | ✅ Por horario |
| E2 (duplicado) | ✅ Por horario |
| RN-09 | ✅ Redefinido a "no duplicar al mismo horario" |
