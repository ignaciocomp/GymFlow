# RF-07 — Gestión de Cuotas v2 (Mejoras)

## Resumen

Mejoras al sistema de cuotas existente: vencimiento mensual correcto, generación retroactiva, reversibilidad de acciones admin, y visibilidad de cuotas anuladas.

## Cambios respecto a v1

### 1. Fecha de vencimiento = mismo día del mes siguiente

**Antes:** `FechaEmision.AddDays(30)` — genera desfase acumulativo.
**Ahora:** `FechaEmision.AddMonths(1)` — la cuota del 10/05 vence el 10/06.

Caso borde: día 31 en meses con 30 días → .NET `AddMonths(1)` ajusta al último día del mes (31/01 → 28/02). Esto es el comportamiento correcto.

**Archivos afectados:**
- `backend/src/GymFlow.Domain/Entities/Cuota.cs` — constructor
- `backend/tests/GymFlow.Domain.Tests/Entities/CuotaTests.cs` — actualizar assertion

### 2. Generación retroactiva de cuotas al alta con fecha pasada

Cuando se crea un socio con `FechaAlta` anterior a hoy, se generan todas las cuotas intermedias. Ejemplo: alta el 20/01/2026, hoy es 10/05/2026 → genera 4 cuotas:
- Emisión 20/01 → Vencimiento 20/02
- Emisión 20/02 → Vencimiento 20/03
- Emisión 20/03 → Vencimiento 20/04
- Emisión 20/04 → Vencimiento 20/05

La lógica se implementa en `ICuotaGeneradorService` con un nuevo método `GenerarCuotasRetroactivasAsync` que reemplaza la llamada unitaria desde `CreateSocioCommand`.

**Reglas:**
- Se generan cuotas desde `FechaAlta` hasta que la fecha de vencimiento supere `DateTime.UtcNow`.
- Todas las cuotas generadas retroactivamente quedan como `Pendiente`.
- El `BackgroundService` no se ve afectado — sigue usando `GetUltimaCuotaAsync` y genera la siguiente cuando la última venció.

**Archivos afectados:**
- `backend/src/GymFlow.Application/Interfaces/ICuotaGeneradorService.cs` — agregar método
- `backend/src/GymFlow.Infrastructure/Services/CuotaGeneradorService.cs` — implementar
- `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs` — usar nuevo método

### 3. Reversibilidad de acciones admin

El admin puede deshacer "marcar como pagada" y "revertir anulación":

**`RevertirPago`** — Cuota Pagada → Pendiente:
- Limpia `FechaPago`, cambia `Estado` a `Pendiente`.
- Solo se puede revertir si la cuota está Pagada y no anulada.

**`RevertirAnulacion`** — Cuota anulada → Pendiente:
- Limpia `FechaBaja`.
- Solo se puede revertir si la cuota tiene `FechaBaja`.

**Archivos afectados:**
- `backend/src/GymFlow.Domain/Entities/Cuota.cs` — métodos `RevertirPago()`, `RevertirAnulacion()`
- `backend/tests/GymFlow.Domain.Tests/Entities/CuotaTests.cs` — tests
- Nuevos commands: `RevertirPagoCuotaCommand`, `RevertirAnulacionCuotaCommand`
- `backend/src/GymFlow.API/Controllers/CuotasController.cs` — endpoints
- `backend/src/GymFlow.API/DependencyInjection.cs` — registrar commands
- Frontend: botones de revertir en la tabla admin, API calls

**Endpoints nuevos:**
| Método | Ruta | Permiso | Descripción |
|--------|------|---------|-------------|
| `PUT` | `/api/cuotas/{id}/revertir-pago` | Cuotas, Modificacion | Revertir pago |
| `PUT` | `/api/cuotas/{id}/revertir-anulacion` | Cuotas, Modificacion | Revertir anulación |

### 4. Filtro de cuotas anuladas en vista admin

**Antes:** `SearchAsync` excluye cuotas con `FechaBaja.HasValue`. El admin nunca las ve.
**Ahora:** Se agrega parámetro `bool? incluirAnuladas` al repositorio y query. Si el admin filtra por "Anulada", se muestran las cuotas con `FechaBaja`.

**Cambios en el DTO:**
- `CuotaDto` agrega campo `DateTime? fechaBaja` para que el frontend pueda mostrar el badge "Anulada".

**Cambios en el frontend:**
- Filtro "Anulada" en el select de estados.
- Badge amarillo/gris para cuotas anuladas.
- Botón "Revertir anulación" visible en cuotas anuladas.

**Archivos afectados:**
- `backend/src/GymFlow.Application/Interfaces/ICuotaRepository.cs` — agregar param
- `backend/src/GymFlow.Infrastructure/Repositories/CuotaRepository.cs` — implementar
- `backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasAdminQuery.cs` — pasar param
- `backend/src/GymFlow.Application/DTOs/CuotaDto.cs` — agregar `FechaBaja`
- `backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasBySocioQuery.cs` — actualizar MapToDto
- `backend/src/GymFlow.API/Controllers/CuotasController.cs` — agregar query param
- `frontend/src/types/index.ts` — agregar campo
- `frontend/src/services/api.ts` — agregar param
- `frontend/src/pages/admin/CuotasPage.tsx` — filtro y badges
