# RF-07 — Gestión de Cuotas

## Resumen

El sistema genera cuotas automáticamente para cada socio activo con plan asignado y permite su visualización tanto para el socio como para el admin. El admin puede marcar cuotas como pagadas y anular cuotas generadas por error.

## Decisiones de diseño

- **Enfoque A (reactivo + batch):** la primera cuota se genera al dar de alta al socio (`CreateSocioCommand`). Las cuotas recurrentes las genera un `BackgroundService` diario.
- Cambiar unidad/plan a un socio **no** genera cuota nueva inmediata — la próxima cuota del batch usará el plan actualizado.
- Un socio puede acumular múltiples cuotas pendientes.
- Solo se generan cuotas para socios con `EstaActivo == true`.
- Búsqueda admin por `DocumentoIdentidad` exacto.

## 1. Entidad de dominio

### Entidad `Cuota`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | `Guid` | PK |
| `SocioId` | `Guid` (FK → Socio) | Socio al que pertenece |
| `UnidadId` | `Guid` (FK → Unidad) | Unidad asociada |
| `PlanId` | `Guid` (FK → Plan) | Plan vigente al momento de generación |
| `NombrePlan` | `string` | Snapshot del nombre del plan |
| `Monto` | `decimal` | Snapshot del precio del plan |
| `FechaEmision` | `DateTime` | Fecha en que se generó |
| `FechaVencimiento` | `DateTime` | FechaEmision + 30 días |
| `Estado` | `EstadoCuota` | Pendiente / Pagada |
| `FechaPago` | `DateTime?` | Se llena al marcar como pagada |
| `FechaBaja` | `DateTime?` | Soft delete para anulaciones |

### Nuevo enum `EstadoCuota`

```
Pendiente = 0
Pagada = 1
```

### Cambio en enum `Modulo`

Se agrega `Cuotas` al enum `Modulo` existente para el sistema RBAC. Se seedean las 4 operaciones (Lectura, Escritura, Modificación, Eliminación). El rol Admin recibe todos los permisos automáticamente.

## 2. Capa de aplicación

### Commands

| Command | Descripción | Consumidor |
|---------|-------------|------------|
| `GenerarCuotaCommand` | Genera una cuota para un socio+unidad con snapshot de plan/monto | Interno (CreateSocioCommand, BackgroundService) |
| `MarcarCuotaPagadaCommand` | Cambia estado a Pagada, registra FechaPago + log de auditoría | Admin |
| `AnularCuotaCommand` | Soft delete (FechaBaja = UtcNow) + log de auditoría | Admin |

### Queries

| Query | Descripción | Consumidor |
|-------|-------------|------------|
| `GetCuotasBySocioQuery` | Cuotas del socio autenticado (portal "Mis cuotas") | Socio |
| `GetCuotasAdminQuery` | Busca por DocumentoIdentidad exacto con filtros opcionales | Admin |

### DTOs

- **`CuotaDto`** (respuesta): Id, NombrePlan, NombreUnidad, Monto, FechaEmision, FechaVencimiento, Estado, FechaPago.
- **`CuotasAdminRequestDto`** (request admin): DocumentoIdentidad (requerido), Estado?, Mes?, Año?, UnidadId?.

### Servicio de dominio

`ICuotaGeneradorService` con método `GenerarCuota(Socio, UsuarioUnidad)` que encapsula la lógica de snapshot (toma el plan actual, captura nombre y precio, calcula fechas). Lo usan tanto `CreateSocioCommand` como el BackgroundService.

## 3. API (Controller y permisos)

### Endpoints Admin (módulo Cuotas)

| Método | Ruta | Permiso | Descripción |
|--------|------|---------|-------------|
| `GET` | `/api/cuotas/admin?documentoIdentidad=...&estado=...&mes=...&anio=...&unidadId=...` | Cuotas, Lectura | Busca cuotas por cédula con filtros |
| `PUT` | `/api/cuotas/{id}/pagar` | Cuotas, Modificacion | Marcar como pagada |
| `DELETE` | `/api/cuotas/{id}` | Cuotas, Eliminacion | Anular cuota (soft delete) |

### Endpoint Socio

| Método | Ruta | Permiso | Descripción |
|--------|------|---------|-------------|
| `GET` | `/api/cuotas/mis-cuotas` | Solo JWT (rol Socio) | Cuotas del socio autenticado (extrae socioId del JWT) |

## 4. BackgroundService

### Configuración en `appsettings.json`

```json
{
  "CuotaGeneracion": {
    "HoraEjecucion": "03:00",
    "Habilitado": true
  }
}
```

### Lógica de ejecución

1. Calcula el próximo horario basado en `HoraEjecucion` (UTC).
2. Espera con `Task.Delay` hasta ese momento.
3. Obtiene todos los socios activos (`EstaActivo == true`) con al menos un `UsuarioUnidad`.
4. Por cada par socio+unidad, verifica si la última cuota (no anulada) ya venció (`FechaVencimiento <= UtcNow`).
5. Si venció o no tiene cuotas, genera una nueva con `ICuotaGeneradorService` usando el plan actual.
6. Repite el ciclo al día siguiente.

### Consideraciones

- Usa `IServiceScopeFactory` para crear scopes por ejecución (patrón estándar para BackgroundServices con DbContext scoped).
- Logging en cada ejecución: cuántas cuotas generó, errores si los hubo.
- Flag `Habilitado` permite desactivar en dev/testing.

## 5. Frontend

### Vista Socio — "Mis Cuotas" (`/portal/mis-cuotas`)

- Nueva página en `pages/portal/`.
- Tabla: Plan, Monto, Fecha de Vencimiento, Estado (badge verde Pagada / rojo Pendiente).
- Botón "Pagar" visible en cuotas pendientes, sin funcionalidad (no hace nada al clickearlo).
- Sin filtros ni paginación.
- Accesible desde el menú del portal del socio.

### Vista Admin — "Gestión de Cuotas" (`/admin/cuotas`)

- Nueva página en `pages/admin/`.
- **Buscador:** input de cédula con botón "Buscar". Carga cuotas del socio encontrado. Si no existe, mensaje de error.
- **Filtros** (aparecen después de buscar): Estado (Pendiente/Pagada/Todas), Mes/Año (selectores), Unidad (selector).
- **Tabla:** Socio (nombre completo), Unidad, Plan, Monto, Fecha de Vencimiento, Estado (badge), Acciones.
- **Acciones por cuota pendiente:**
  - "Marcar como pagada" — dialog de confirmación → `PUT /api/cuotas/{id}/pagar`.
  - "Anular" — dialog de confirmación → `DELETE /api/cuotas/{id}`.
- **Ordenamiento:** fecha de vencimiento descendente por defecto.
- Usa componentes existentes de shadcn/ui (Table, Badge, Dialog, Select, Button).

### Navegación

- "Gestión de Cuotas" en menú admin (condicionado por permiso `Cuotas.Lectura`).
- "Mis Cuotas" en menú del portal del socio.

## 6. Generación de primera cuota

Al crear un socio con plan asignado (`CreateSocioCommand`), se genera automáticamente su primera cuota pendiente por cada `UsuarioUnidad` con:
- `FechaEmision = FechaAlta`
- `FechaVencimiento = FechaAlta + 30 días`
- Snapshot del plan actual (nombre y precio)
