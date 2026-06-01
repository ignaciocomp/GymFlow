---
tags:
  - spec
requerimiento: RF-07
---

# RF-07 — Gestión de Cuotas

**Plan:** [[plan-rf07-gestion-cuotas]]
**Requerimientos:** [[GymFlow_Requerimientos_Completos]]
**Última actualización:** 2026-05-30
**Historial:**
- 2026-05-30 — v3: sincronización con doc iteración 2 (endpoints faltantes, vista SociosCuotasPage, estados visuales)
- 2026-05-10 — v2: vencimiento mensual (AddMonths), generación retroactiva, reversibilidad de acciones, filtro de anuladas
- 2026-05-10 — v1: versión inicial con generación automática, vista socio/admin, pago y anulación

## Resumen

El sistema genera cuotas automáticamente para cada socio activo con plan asignado y permite su visualización tanto para el socio como para el admin. El admin puede marcar cuotas como pagadas, anular cuotas generadas por error, y revertir ambas acciones.

## Decisiones de diseño

- **Enfoque A (reactivo + batch):** la primera cuota se genera al dar de alta al socio (`CreateSocioCommand`). Las cuotas recurrentes las genera un `BackgroundService` diario.
- Cambiar unidad/plan a un socio **no** genera cuota nueva inmediata — la próxima cuota del batch usará el plan actualizado.
- Un socio puede acumular múltiples cuotas pendientes.
- Solo se generan cuotas para socios con `EstaActivo == true`.
- Búsqueda admin por `DocumentoIdentidad` exacto.
- **Vencimiento mensual:** `FechaEmision.AddMonths(1)` (no AddDays(30)) — evita desfase acumulativo. Caso borde: día 31 en meses con 30 días → .NET ajusta al último día del mes.
- **Generación retroactiva:** al crear un socio con FechaAlta anterior a hoy, se generan todas las cuotas intermedias.
- **Reversibilidad:** el admin puede revertir pagos y anulaciones.

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
| `FechaVencimiento` | `DateTime` | FechaEmision + 1 mes |
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

### Métodos de dominio

- `MarcarComoPagada()` — Pendiente → Pagada
- `Anular()` — soft delete (FechaBaja = UtcNow)
- `RevertirPago()` — Pagada → Pendiente (limpia FechaPago)
- `RevertirAnulacion()` — limpia FechaBaja

## 2. Capa de aplicación

### Commands

| Command | Descripción | Consumidor |
|---------|-------------|------------|
| `GenerarCuotaCommand` | Genera una cuota para un socio+unidad con snapshot de plan/monto | Interno (BackgroundService) |
| `MarcarCuotaPagadaCommand` | Cambia estado a Pagada, registra FechaPago + log de auditoría | Admin |
| `AnularCuotaCommand` | Soft delete (FechaBaja = UtcNow) + log de auditoría | Admin |
| `RevertirPagoCuotaCommand` | Pagada → Pendiente + log de auditoría | Admin |
| `RevertirAnulacionCuotaCommand` | Limpia FechaBaja + log de auditoría | Admin |

### Queries

| Query | Descripción | Consumidor |
|-------|-------------|------------|
| `GetCuotasBySocioQuery` | Cuotas del socio autenticado (portal "Mis cuotas") | Socio |
| `GetCuotasAdminQuery` | Busca por DocumentoIdentidad exacto con filtros opcionales, incluye param `incluirAnuladas` | Admin |

### Servicio de dominio

`ICuotaGeneradorService`:
- `GenerarCuotaAsync(socioId, usuarioUnidad, fechaEmision)` — genera una cuota individual
- `GenerarCuotasRetroactivasAsync(socioId, usuarioUnidad, fechaAlta)` — genera todas las cuotas desde fechaAlta hasta hoy

## 3. API (Controller y permisos)

### Endpoints Admin (módulo Cuotas)

| Método | Ruta | Permiso | Descripción |
|--------|------|---------|-------------|
| `GET` | `/api/cuotas/admin?documentoIdentidad=...&estado=...&mes=...&anio=...&unidadId=...&incluirAnuladas=...` | Cuotas, Lectura | Busca cuotas por cédula con filtros |
| `GET` | `/api/cuotas/admin/socio/{socioId}` | Cuotas, Lectura | Obtener cuotas de un socio por ID |
| `GET` | `/api/cuotas/socios-estado` | Cuotas, Lectura | Listar socios con resumen de estado de cuota |
| `PUT` | `/api/cuotas/{id}/pagar` | Cuotas, Modificacion | Marcar como pagada |
| `DELETE` | `/api/cuotas/{id}` | Cuotas, Eliminacion | Anular cuota (soft delete) |
| `PUT` | `/api/cuotas/{id}/revertir-pago` | Cuotas, Modificacion | Revertir pago |
| `PUT` | `/api/cuotas/{id}/revertir-anulacion` | Cuotas, Modificacion | Revertir anulación |
| `POST` | `/api/cuotas/{id}/notificar` | Cuotas, Modificacion | Enviar notificación manual de cuota ([[spec-rf06-recordatorios-cuota]]) |

### Endpoint Socio

| Método | Ruta | Permiso | Descripción |
|--------|------|---------|-------------|
| `GET` | `/api/cuotas/mis-cuotas` | Solo JWT (rol Socio) | Cuotas del socio autenticado |

## 4. BackgroundService

Ejecuta diariamente a la hora configurada. Obtiene todos los socios activos, por cada par socio+unidad verifica si la última cuota venció, y genera la siguiente.

## 5. Frontend

### Vista Socio — "Mis Cuotas" (`/portal/mis-cuotas`)

Tabla con Plan, Unidad, Monto, Vencimiento, Estado, Acción. Badges con colores según estado:
- **Pagada:** badge verde
- **Pendiente al día:** badge gris (fechaVencimiento >= hoy)
- **Pendiente vencida:** badge rojo (fechaVencimiento < hoy)
- **Anulada:** badge gris claro

Botón "Pagar" deshabilitado (pago online es futuro RF). Paginación a partir de 12 cuotas.

### Vista Admin — "Socios y Estado de Cuota" (`/admin/cuotas`)

Listado de socios con resumen de estado de cuota (al día / próxima a vencer / vencida), filtro por unidad y búsqueda. Usa endpoint `GET /api/cuotas/socios-estado`.

### Vista Admin — "Detalle de Cuotas" (`/admin/cuotas/:socioId`)

Detalle de cuotas de un socio específico. Buscador por cédula, filtros por Estado (Pendiente/Pagada/Anulada), Mes, Año, Unidad. Acciones por cuota: marcar pagada, anular, revertir pago, revertir anulación, notificar ([[spec-rf06-recordatorios-cuota]]). Cuotas anuladas con badge "Anulada" y opacity reducida.

## 6. Generación de primera cuota / retroactiva

Al crear un socio con plan asignado, se usa `GenerarCuotasRetroactivasAsync` que genera cuotas desde `FechaAlta` hasta hoy. Si FechaAlta es hoy, genera solo una cuota.
