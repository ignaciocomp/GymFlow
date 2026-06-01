---
tags:
  - spec
requerimiento: RF-22
---

# RF_22 — Gestión de Planes y Plan por Unidad en Socio

**Branch:** feature/RF_22
**Estado:** Aprobado
**Plan:** [[plan-rf22-planes-por-unidad]]
**Última actualización:** 2026-04-06
**Historial:**
- 2026-04-06 — Versión inicial

---

## Resumen

Actualmente los Planes se crean únicamente via seed data en `Program.cs` y no existe UI para gestionarlos. Además, un Socio solo puede tener un Plan global independientemente de cuántas Unidades de Negocio tenga asignadas. Este RF agrega:

1. Una sección visual para gestión CRUD de Planes.
2. La capacidad de asignar un Plan por cada Unidad de Negocio al dar de alta o modificar un Socio.

---

## Sección 1: Modelo de Datos

### Cambio en `UsuarioUnidad`

Se agrega la columna `PlanId` (nullable, FK a `Planes`) a la tabla `UsuarioUnidades`:

```
UsuarioUnidad
─────────────────────────────────────────
UsuarioId  (FK → Usuarios)         [PK]
UnidadId   (FK → Unidades)         [PK]
PlanId     (FK → Planes, nullable) ← NUEVO
```

**Restricción:** El `PlanId` asignado en un registro `UsuarioUnidad` debe pertenecer a la misma `UnidadId` del registro. Esta validación se aplica en la capa de aplicación (dominio), no solo a nivel de BD.

**Configuración EF:** `DeleteBehavior.SetNull` para la FK `PlanId` en `UsuarioUnidad` (si se da de baja un Plan, los registros relacionados quedan con `PlanId = null`).

### Cambio en `Socio`

Se elimina la propiedad `PlanId` y su FK de la entidad `Socio`. El plan pasa a vivir exclusivamente en `UsuarioUnidad`.

### Migración de datos

Los Socios existentes que tengan `PlanId != null`: se mueve ese valor al registro `UsuarioUnidad` correspondiente a su primera unidad asignada. Los demás registros `UsuarioUnidad` del mismo socio quedan con `PlanId = null`.

### Cambio en lógica de eliminación de `Plan`

La baja lógica ya existe (`EstaActivo = false`). Se agrega validación en `DeletePlanCommand`: si existen registros en `UsuarioUnidad` con ese `PlanId`, se retorna un error de negocio (HTTP 409) con mensaje descriptivo. El usuario debe reasignar los socios antes de dar de baja el plan.

---

## Sección 2: Backend

### Nuevos endpoints en `PlanesController`

| Método   | Ruta                  | Descripción                                                     |
|----------|-----------------------|-----------------------------------------------------------------|
| `POST`   | `/api/planes`         | Crear plan                                                      |
| `PUT`    | `/api/planes/{id}`    | Editar plan                                                     |
| `DELETE` | `/api/planes/{id}`    | Baja lógica (`EstaActivo = false`), bloqueada si hay socios    |

El endpoint `GET /api/planes?unidadId=` existente se mantiene sin cambios.

### Nuevos casos de uso (Application layer)

**`CreatePlanCommand`**
- Campos: `Nombre`, `UnidadId`, `Precio`, `Descripcion?`
- Valida que la Unidad exista y esté activa
- Crea el Plan con `EstaActivo = true`

**`UpdatePlanCommand`**
- Campos: `Id`, `Nombre`, `Precio`, `Descripcion?`
- La `UnidadId` no es modificable una vez creado el plan
- Valida que el Plan exista y esté activo

**`DeletePlanCommand`**
- Verifica que no existan registros en `UsuarioUnidad` con ese `PlanId`
- Si los hay: retorna error de negocio con mensaje "El plan tiene socios asignados. Reasignelos antes de darlo de baja."
- Si no los hay: setea `EstaActivo = false`

### Cambios en comandos de Socio

**`CreateSocioCommand` / `UpdateSocioCommand`**

El payload reemplaza el campo plano `PlanId` por una lista de asignaciones con plan incluido:

```csharp
// Antes
List<Guid> UnidadIds
Guid? PlanId

// Después
List<UnidadAsignacionDto> Unidades
// donde UnidadAsignacionDto = { UnidadId: Guid, PlanId: Guid? }
```

Validación por cada par: si `PlanId` no es null, verificar que el plan pertenezca a la `UnidadId` del mismo par.

### DTOs afectados

**`CreateSocioRequest` / `UpdateSocioRequest`**
- Reemplazar `PlanId` (Guid?) y `UnidadIds` (List<Guid>) por `Unidades` (List<`{ UnidadId, PlanId? }`>)

**`SocioDto`**
- La propiedad `PlanNombre` (string) se elimina
- La propiedad `Unidades` pasa de `List<{ UnidadId, Nombre }>` a `List<{ UnidadId, Nombre, PlanId?, PlanNombre? }>`

**`PlanDto`** — sin cambios

---

## Sección 3: Frontend

### Sidebar (`Sidebar.tsx`)

Se agrega un nuevo grupo colapsable **"Planes"** al mismo nivel que "Socios":

```
▼ Socios
    + Nuevo Socio
    ✓ Socios Activos
    ✗ Socios Inactivos
▼ Planes
    + Nuevo Plan       → /admin/planes/nuevo
    ☰ Lista de Planes  → /admin/planes
```

### Nuevas páginas

**`PlanesPage`** (`/admin/planes`)
- Tabla plana con columnas: Nombre, Unidad, Precio, Estado (badge Activo/Inactivo), Acciones (editar, dar de baja)
- Dropdown de filtro por Unidad arriba a la izquierda
- Botón "Nuevo Plan" arriba a la derecha
- Baja lógica: confirma con dialog simple antes de ejecutar
- Si el plan tiene socios asociados, muestra el error del backend en el dialog

**`NuevoPlanPage`** (`/admin/planes/nuevo`)
- Formulario: Nombre (texto requerido), Unidad (dropdown de unidades activas, requerido), Precio (número requerido, no negativo), Descripción (textarea opcional)
- Al guardar exitosamente redirige a `/admin/planes`

**`EditPlanPage`** (`/admin/planes/:id/editar`)
- Mismo formulario precargado con los datos del plan
- El campo Unidad se muestra deshabilitado (un plan no cambia de unidad)
- Al guardar exitosamente redirige a `/admin/planes`

### Cambios en `NuevoSocioPage` y `EditSocioPage`

La sección **"Plan y Acceso"** se reestructura:

- Se elimina el dropdown global de Plan
- Al marcar una Unidad, aparece inmediatamente un dropdown de Plan debajo de ella, cargando solo los planes activos de esa unidad (`planesApi.getAll(unidadId)`)
- Al desmarcar una Unidad, su dropdown de Plan desaparece (y el valor se descarta)
- Cada dropdown incluye la opción "Sin plan" como primera opción (valor null)
- El payload enviado al backend incluye `unidades: [{ unidadId, planId? }]`

### Cambios en `SociosPage`

- La columna **"Plan"** (badge único) se reemplaza por badges de "Unidad — Plan" dentro de la columna Unidades, o se muestra como columna separada mostrando los planes de cada unidad
- El filtro por plan existente se mantiene (filtra socios que tengan ese plan en cualquiera de sus unidades)

### Nuevas rutas en `App.tsx`

```
/admin/planes              → PlanesPage
/admin/planes/nuevo        → NuevoPlanPage
/admin/planes/:id/editar   → EditPlanPage
```

---

## Decisiones descartadas

- **Nueva tabla `SocioPlan`**: descartada por redundante. `UsuarioUnidad` ya representa la relación Socio↔Unidad; agregar `PlanId` ahí es suficiente y más simple.
- **Mantener `Socio.PlanId` con campo adicional por unidad**: descartado por inconsistencia del modelo.
- **Eliminación física de Planes**: descartada en favor de baja lógica consistente con el patrón del resto del sistema.
