---
tags:
  - plan
requerimiento: RF-07
---

# RF-07 — Gestión de Cuotas: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Spec:** [[spec-rf07-gestion-cuotas]]
**Última actualización:** 2026-05-10
**Historial:**
- 2026-05-10 — v2: plan de mejoras (vencimiento mensual, retroactiva, reversibilidad, filtro anuladas)
- 2026-05-10 — v1: plan inicial (entidad, CRUD, BackgroundService, frontend socio/admin)

**Goal:** Implementar gestión completa de cuotas: generación automática, vistas socio/admin, pago/anulación con reversibilidad, vencimiento mensual y generación retroactiva.

**Architecture:** Cambios en entidad `Cuota` (AddMonths, métodos de reversión), nuevo método en `CuotaGeneradorService` para generación retroactiva, nuevos commands/endpoints de reversión, filtro de anuladas en repositorio/frontend.

**Tech Stack:** .NET 8, PostgreSQL, EF Core, React 18, TypeScript, TanStack Query, shadcn/ui.

**Nota:** No commitear nada — el usuario se encarga del git. Este plan asume que RF-02 (FechaAlta seleccionable) ya fue implementado.

---

### Task 1: Cambiar vencimiento de +30 días a +1 mes

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/Cuota.cs`
- Modify: `backend/tests/GymFlow.Domain.Tests/Entities/CuotaTests.cs`

- [ ] **Step 1: Cambiar cálculo de FechaVencimiento en constructor de `Cuota`**

En `backend/src/GymFlow.Domain/Entities/Cuota.cs`, cambiar la línea:

```csharp
FechaVencimiento = fechaEmision.AddDays(30);
```

Por:

```csharp
FechaVencimiento = fechaEmision.AddMonths(1);
```

- [ ] **Step 2: Actualizar test del constructor**

En `backend/tests/GymFlow.Domain.Tests/Entities/CuotaTests.cs`, en el test `Constructor_WithValidData_CreatesCuota`, cambiar:

```csharp
Assert.Equal(fechaEmision.AddDays(30), cuota.FechaVencimiento);
```

Por:

```csharp
Assert.Equal(fechaEmision.AddMonths(1), cuota.FechaVencimiento);
```

- [ ] **Step 3: Agregar test de caso borde día 31**

En el mismo archivo de tests, agregar:

```csharp
[Fact]
public void Constructor_WithDay31_AdjustsVencimientoToLastDayOfMonth()
{
    var fechaEmision = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
    var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, fechaEmision);

    Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), cuota.FechaVencimiento);
}
```

- [ ] **Step 4: Correr tests de dominio**

Run: `dotnet test backend/tests/GymFlow.Domain.Tests/ --filter "CuotaTests" -v minimal`
Expected: Todos los tests pasan.

---

### Task 2: Métodos de reversión en entidad `Cuota`

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/Cuota.cs`
- Modify: `backend/tests/GymFlow.Domain.Tests/Entities/CuotaTests.cs`

- [ ] **Step 1: Agregar método `RevertirPago` a la entidad**

En `backend/src/GymFlow.Domain/Entities/Cuota.cs`, agregar después de `MarcarComoPagada()`:

```csharp
public void RevertirPago()
{
    if (Estado != EstadoCuota.Pagada)
        throw new InvalidOperationException("Solo se puede revertir el pago de una cuota pagada.");
    if (FechaBaja.HasValue)
        throw new InvalidOperationException("No se puede revertir el pago de una cuota anulada.");

    Estado = EstadoCuota.Pendiente;
    FechaPago = null;
}
```

- [ ] **Step 2: Agregar método `RevertirAnulacion` a la entidad**

En el mismo archivo, agregar después de `Anular()`:

```csharp
public void RevertirAnulacion()
{
    if (!FechaBaja.HasValue)
        throw new InvalidOperationException("La cuota no está anulada.");

    FechaBaja = null;
}
```

- [ ] **Step 3: Agregar tests de reversión**

En `backend/tests/GymFlow.Domain.Tests/Entities/CuotaTests.cs`, agregar:

```csharp
[Fact]
public void RevertirPago_WhenPagada_ChangesEstadoToPendienteAndClearsFechaPago()
{
    var cuota = CrearCuotaValida();
    cuota.MarcarComoPagada();

    cuota.RevertirPago();

    Assert.Equal(EstadoCuota.Pendiente, cuota.Estado);
    Assert.Null(cuota.FechaPago);
}

[Fact]
public void RevertirPago_WhenPendiente_ThrowsInvalidOperationException()
{
    var cuota = CrearCuotaValida();

    Assert.Throws<InvalidOperationException>(() => cuota.RevertirPago());
}

[Fact]
public void RevertirAnulacion_WhenAnulada_ClearsFechaBaja()
{
    var cuota = CrearCuotaValida();
    cuota.Anular();

    cuota.RevertirAnulacion();

    Assert.Null(cuota.FechaBaja);
}

[Fact]
public void RevertirAnulacion_WhenNotAnulada_ThrowsInvalidOperationException()
{
    var cuota = CrearCuotaValida();

    Assert.Throws<InvalidOperationException>(() => cuota.RevertirAnulacion());
}
```

- [ ] **Step 4: Correr tests de dominio**

Run: `dotnet test backend/tests/GymFlow.Domain.Tests/ --filter "CuotaTests" -v minimal`
Expected: Todos los tests pasan (10 tests).

---

### Task 3: Generación retroactiva de cuotas

**Files:**
- Modify: `backend/src/GymFlow.Application/Interfaces/ICuotaGeneradorService.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Services/CuotaGeneradorService.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`

- [ ] **Step 1: Agregar método a la interfaz `ICuotaGeneradorService`**

En `backend/src/GymFlow.Application/Interfaces/ICuotaGeneradorService.cs`, agregar:

```csharp
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface ICuotaGeneradorService
{
    Task<Cuota> GenerarCuotaAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaEmision);
    Task<IReadOnlyList<Cuota>> GenerarCuotasRetroactivasAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaAlta);
}
```

- [ ] **Step 2: Implementar generación retroactiva**

En `backend/src/GymFlow.Infrastructure/Services/CuotaGeneradorService.cs`, agregar el método:

```csharp
public async Task<IReadOnlyList<Cuota>> GenerarCuotasRetroactivasAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaAlta)
{
    if (!usuarioUnidad.PlanId.HasValue)
        throw new InvalidOperationException("El socio no tiene plan asignado en esta unidad.");

    var plan = await _planRepository.GetByIdAsync(usuarioUnidad.PlanId.Value)
        ?? throw new InvalidOperationException("El plan asignado no existe.");

    var cuotas = new List<Cuota>();
    var fechaEmision = fechaAlta;

    while (fechaEmision.AddMonths(1) <= DateTime.UtcNow)
    {
        var cuota = new Cuota(
            socioId: socioId,
            unidadId: usuarioUnidad.UnidadId,
            planId: plan.Id,
            nombrePlan: plan.Nombre,
            monto: plan.Precio,
            fechaEmision: fechaEmision);

        await _cuotaRepository.AddAsync(cuota);
        cuotas.Add(cuota);
        fechaEmision = fechaEmision.AddMonths(1);
    }

    return cuotas;
}
```

La lógica: mientras la fecha de vencimiento (`fechaEmision + 1 mes`) sea <= hoy, se genera una cuota. Esto produce el comportamiento esperado:
- Alta 20/01, hoy 10/05 → genera cuotas con emisión 20/01, 20/02, 20/03, 20/04 (vencimientos 20/02, 20/03, 20/04, 20/05).

- [ ] **Step 3: Actualizar `CreateSocioCommand` para usar generación retroactiva**

En `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`, reemplazar el bloque de generación de cuotas:

```csharp
// ANTES:
foreach (var asignacion in unidades)
{
    if (asignacion.PlanId.HasValue)
    {
        var uu = socio.UnidadesAsignadas.First(u => u.UnidadId == asignacion.UnidadId);
        await _cuotaGenerador.GenerarCuotaAsync(socio.Id, uu, socio.FechaAlta);
    }
}
```

```csharp
// DESPUÉS:
foreach (var asignacion in unidades)
{
    if (asignacion.PlanId.HasValue)
    {
        var uu = socio.UnidadesAsignadas.First(u => u.UnidadId == asignacion.UnidadId);
        await _cuotaGenerador.GenerarCuotasRetroactivasAsync(socio.Id, uu, socio.FechaAlta);
    }
}
```

- [ ] **Step 4: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

- [ ] **Step 5: Correr tests existentes**

Run: `dotnet test backend/tests/GymFlow.Application.Tests/ --filter "CreateSocioCommand" -v minimal`
Expected: Tests pasan (el mock de `ICuotaGeneradorService` no necesita setup para `GenerarCuotasRetroactivasAsync` porque por defecto retorna null/default).

---

### Task 4: Commands de reversión + endpoints

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Cuotas/RevertirPagoCuotaCommand.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Cuotas/RevertirAnulacionCuotaCommand.cs`
- Modify: `backend/src/GymFlow.API/Controllers/CuotasController.cs`
- Modify: `backend/src/GymFlow.API/DependencyInjection.cs`

- [ ] **Step 1: Crear `RevertirPagoCuotaCommand`**

```csharp
// backend/src/GymFlow.Application/UseCases/Cuotas/RevertirPagoCuotaCommand.cs
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class RevertirPagoCuotaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IAuditLogger _auditLogger;

    public RevertirPagoCuotaCommand(ICuotaRepository cuotaRepository, IAuditLogger auditLogger)
    {
        _cuotaRepository = cuotaRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid cuotaId, Guid usuarioId, string usuarioNombre)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        cuota.RevertirPago();
        await _cuotaRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id,
            $"Se revirtió el pago de la cuota de {cuota.NombrePlan} del socio {cuota.Socio?.Nombre} {cuota.Socio?.Apellido}");
    }
}
```

- [ ] **Step 2: Crear `RevertirAnulacionCuotaCommand`**

```csharp
// backend/src/GymFlow.Application/UseCases/Cuotas/RevertirAnulacionCuotaCommand.cs
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class RevertirAnulacionCuotaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IAuditLogger _auditLogger;

    public RevertirAnulacionCuotaCommand(ICuotaRepository cuotaRepository, IAuditLogger auditLogger)
    {
        _cuotaRepository = cuotaRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid cuotaId, Guid usuarioId, string usuarioNombre)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        cuota.RevertirAnulacion();
        await _cuotaRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id,
            $"Se revirtió la anulación de la cuota de {cuota.NombrePlan} del socio {cuota.Socio?.Nombre} {cuota.Socio?.Apellido}");
    }
}
```

- [ ] **Step 3: Registrar commands en DI**

En `backend/src/GymFlow.API/DependencyInjection.cs`, agregar después de las líneas de `AnularCuotaCommand`:

```csharp
services.AddScoped<RevertirPagoCuotaCommand>();
services.AddScoped<RevertirAnulacionCuotaCommand>();
```

- [ ] **Step 4: Agregar endpoints al controller**

En `backend/src/GymFlow.API/Controllers/CuotasController.cs`:

Agregar los campos al constructor:

```csharp
private readonly RevertirPagoCuotaCommand _revertirPagoCommand;
private readonly RevertirAnulacionCuotaCommand _revertirAnulacionCommand;
```

Actualizar el constructor para inyectar los dos nuevos:

```csharp
public CuotasController(
    GetCuotasBySocioQuery getCuotasBySocioQuery,
    GetCuotasAdminQuery getCuotasAdminQuery,
    MarcarCuotaPagadaCommand marcarPagadaCommand,
    AnularCuotaCommand anularCommand,
    RevertirPagoCuotaCommand revertirPagoCommand,
    RevertirAnulacionCuotaCommand revertirAnulacionCommand)
{
    _getCuotasBySocioQuery = getCuotasBySocioQuery;
    _getCuotasAdminQuery = getCuotasAdminQuery;
    _marcarPagadaCommand = marcarPagadaCommand;
    _anularCommand = anularCommand;
    _revertirPagoCommand = revertirPagoCommand;
    _revertirAnulacionCommand = revertirAnulacionCommand;
}
```

Agregar los endpoints después del método `Anular`:

```csharp
[HttpPut("{id:guid}/revertir-pago")]
[RequierePermiso(Modulo.Cuotas, Operacion.Modificacion)]
public async Task<IActionResult> RevertirPago(Guid id)
{
    try
    {
        var (userId, userName) = GetCurrentUser();
        await _revertirPagoCommand.ExecuteAsync(id, userId, userName);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
}

[HttpPut("{id:guid}/revertir-anulacion")]
[RequierePermiso(Modulo.Cuotas, Operacion.Modificacion)]
public async Task<IActionResult> RevertirAnulacion(Guid id)
{
    try
    {
        var (userId, userName) = GetCurrentUser();
        await _revertirAnulacionCommand.ExecuteAsync(id, userId, userName);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
}
```

- [ ] **Step 5: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

---

### Task 5: Filtro de cuotas anuladas — backend

**Files:**
- Modify: `backend/src/GymFlow.Application/Interfaces/ICuotaRepository.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Repositories/CuotaRepository.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/CuotaDto.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasBySocioQuery.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasAdminQuery.cs`
- Modify: `backend/src/GymFlow.API/Controllers/CuotasController.cs`

- [ ] **Step 1: Agregar `fechaBaja` al DTO**

En `backend/src/GymFlow.Application/DTOs/CuotaDto.cs`, agregar campo:

```csharp
public record CuotaDto(
    Guid Id,
    string NombrePlan,
    string NombreUnidad,
    string? NombreSocio,
    decimal Monto,
    DateTime FechaEmision,
    DateTime FechaVencimiento,
    EstadoCuota Estado,
    DateTime? FechaPago,
    DateTime? FechaBaja);
```

- [ ] **Step 2: Actualizar `MapToDto` en `GetCuotasBySocioQuery`**

En `backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasBySocioQuery.cs`, agregar `FechaBaja` al mapeo:

```csharp
internal static CuotaDto MapToDto(Cuota c) => new(
    Id: c.Id,
    NombrePlan: c.NombrePlan,
    NombreUnidad: c.Unidad?.Nombre ?? "",
    NombreSocio: c.Socio != null ? $"{c.Socio.Nombre} {c.Socio.Apellido}" : null,
    Monto: c.Monto,
    FechaEmision: c.FechaEmision,
    FechaVencimiento: c.FechaVencimiento,
    Estado: c.Estado,
    FechaPago: c.FechaPago,
    FechaBaja: c.FechaBaja);
```

- [ ] **Step 3: Agregar parámetro `incluirAnuladas` a la interfaz del repositorio**

En `backend/src/GymFlow.Application/Interfaces/ICuotaRepository.cs`, cambiar la firma de `SearchAsync`:

```csharp
Task<IEnumerable<Cuota>> SearchAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false);
```

- [ ] **Step 4: Implementar filtro en `CuotaRepository`**

En `backend/src/GymFlow.Infrastructure/Repositories/CuotaRepository.cs`, actualizar `SearchAsync`:

```csharp
public async Task<IEnumerable<Cuota>> SearchAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false)
{
    var query = _context.Cuotas
        .Include(c => c.Socio)
        .Include(c => c.Unidad)
        .Where(c => c.SocioId == socioId)
        .AsQueryable();

    if (!incluirAnuladas)
        query = query.Where(c => !c.FechaBaja.HasValue);

    if (estado.HasValue)
        query = query.Where(c => c.Estado == estado.Value);

    if (mes.HasValue)
        query = query.Where(c => c.FechaVencimiento.Month == mes.Value);

    if (anio.HasValue)
        query = query.Where(c => c.FechaVencimiento.Year == anio.Value);

    if (unidadId.HasValue)
        query = query.Where(c => c.UnidadId == unidadId.Value);

    return await query
        .OrderByDescending(c => c.FechaVencimiento)
        .ToListAsync();
}
```

- [ ] **Step 5: Pasar parámetro desde `GetCuotasAdminQuery`**

En `backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasAdminQuery.cs`, actualizar la firma y llamada:

```csharp
public async Task<IEnumerable<CuotaDto>> ExecuteAsync(string documentoIdentidad, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false)
{
    var socio = (await _socioRepository.GetAllAsync(includeInactive: true))
        .FirstOrDefault(s => s.DocumentoIdentidad == documentoIdentidad)
        ?? throw new KeyNotFoundException("No se encontró un socio con ese documento de identidad.");

    var cuotas = await _cuotaRepository.SearchAsync(socio.Id, estado, mes, anio, unidadId, incluirAnuladas);
    return cuotas.Select(GetCuotasBySocioQuery.MapToDto);
}
```

- [ ] **Step 6: Agregar query param al controller**

En `backend/src/GymFlow.API/Controllers/CuotasController.cs`, actualizar el método `GetAdmin`:

```csharp
[HttpGet("admin")]
[RequierePermiso(Modulo.Cuotas, Operacion.Lectura)]
public async Task<ActionResult<IEnumerable<CuotaDto>>> GetAdmin(
    [FromQuery] string documentoIdentidad,
    [FromQuery] EstadoCuota? estado,
    [FromQuery] int? mes,
    [FromQuery] int? anio,
    [FromQuery] Guid? unidadId,
    [FromQuery] bool incluirAnuladas = false)
{
    try
    {
        var cuotas = await _getCuotasAdminQuery.ExecuteAsync(documentoIdentidad, estado, mes, anio, unidadId, incluirAnuladas);
        return Ok(cuotas);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { error = ex.Message });
    }
}
```

- [ ] **Step 7: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

---

### Task 6: Tests de commands de reversión

**Files:**
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Cuotas/RevertirPagoCuotaCommandTests.cs`
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Cuotas/RevertirAnulacionCuotaCommandTests.cs`

- [ ] **Step 1: Tests de `RevertirPagoCuotaCommand`**

```csharp
// backend/tests/GymFlow.Application.Tests/UseCases/Cuotas/RevertirPagoCuotaCommandTests.cs
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class RevertirPagoCuotaCommandTests
{
    [Fact]
    public async Task ExecuteAsync_CuotaPagada_RevierteAPendiente()
    {
        var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, DateTime.UtcNow);
        cuota.MarcarComoPagada();

        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        var audit = new Mock<IAuditLogger>();

        var sut = new RevertirPagoCuotaCommand(repo.Object, audit.Object);
        await sut.ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin Test");

        Assert.Equal(EstadoCuota.Pendiente, cuota.Estado);
        Assert.Null(cuota.FechaPago);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id, It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaNoExiste_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cuota?)null);
        var audit = new Mock<IAuditLogger>();

        var sut = new RevertirPagoCuotaCommand(repo.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));
    }
}
```

- [ ] **Step 2: Tests de `RevertirAnulacionCuotaCommand`**

```csharp
// backend/tests/GymFlow.Application.Tests/UseCases/Cuotas/RevertirAnulacionCuotaCommandTests.cs
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class RevertirAnulacionCuotaCommandTests
{
    [Fact]
    public async Task ExecuteAsync_CuotaAnulada_RevierteAnulacion()
    {
        var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, DateTime.UtcNow);
        cuota.Anular();

        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        var audit = new Mock<IAuditLogger>();

        var sut = new RevertirAnulacionCuotaCommand(repo.Object, audit.Object);
        await sut.ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin Test");

        Assert.Null(cuota.FechaBaja);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id, It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaNoExiste_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cuota?)null);
        var audit = new Mock<IAuditLogger>();

        var sut = new RevertirAnulacionCuotaCommand(repo.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));
    }
}
```

- [ ] **Step 3: Correr todos los tests de cuotas**

Run: `dotnet test backend/tests/ --filter "Cuota" -v minimal`
Expected: Todos los tests pasan.

---

### Task 7: Frontend — API, tipos, y filtro de anuladas

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/services/api.ts`

- [ ] **Step 1: Actualizar tipo `CuotaDto` con campo `fechaBaja`**

En `frontend/src/types/index.ts`, actualizar:

```typescript
export interface CuotaDto {
  id: string
  nombrePlan: string
  nombreUnidad: string
  nombreSocio: string | null
  monto: number
  fechaEmision: string
  fechaVencimiento: string
  estado: EstadoCuota
  fechaPago: string | null
  fechaBaja: string | null
}
```

- [ ] **Step 2: Agregar endpoints de reversión y parámetro de anuladas al API**

En `frontend/src/services/api.ts`, actualizar `cuotasApi`:

```typescript
export const cuotasApi = {
  getMisCuotas: async (): Promise<CuotaDto[]> => {
    const { data } = await api.get<CuotaDto[]>('/cuotas/mis-cuotas')
    return data
  },

  getAdmin: async (params: {
    documentoIdentidad: string
    estado?: string
    mes?: number
    anio?: number
    unidadId?: string
    incluirAnuladas?: boolean
  }): Promise<CuotaDto[]> => {
    const { data } = await api.get<CuotaDto[]>('/cuotas/admin', { params })
    return data
  },

  marcarPagada: async (id: string): Promise<void> => {
    await api.put(`/cuotas/${id}/pagar`)
  },

  anular: async (id: string): Promise<void> => {
    await api.delete(`/cuotas/${id}`)
  },

  revertirPago: async (id: string): Promise<void> => {
    await api.put(`/cuotas/${id}/revertir-pago`)
  },

  revertirAnulacion: async (id: string): Promise<void> => {
    await api.put(`/cuotas/${id}/revertir-anulacion`)
  },
}
```

---

### Task 8: Frontend — Página admin con reversión y filtro de anuladas

**Files:**
- Modify: `frontend/src/pages/admin/CuotasPage.tsx`

- [ ] **Step 1: Reescribir `CuotasPage.tsx` con las nuevas funcionalidades**

Reemplazar el contenido completo de `frontend/src/pages/admin/CuotasPage.tsx`:

```tsx
import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { cuotasApi, unidadesApi } from '@/services/api'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { CreditCard, Search, CheckCircle, XCircle, Undo2 } from 'lucide-react'

type ConfirmAction = 'pagar' | 'anular' | 'revertir-pago' | 'revertir-anulacion'

export default function CuotasPage() {
  const queryClient = useQueryClient()
  const [cedula, setCedula] = useState('')
  const [searchedCedula, setSearchedCedula] = useState<string | null>(null)
  const [estadoFilter, setEstadoFilter] = useState<string>('all')
  const [mesFilter, setMesFilter] = useState<string>('all')
  const [anioFilter, setAnioFilter] = useState<string>('all')
  const [unidadFilter, setUnidadFilter] = useState<string>('all')
  const [confirmDialog, setConfirmDialog] = useState<{
    type: ConfirmAction
    id: string
    plan: string
  } | null>(null)

  const incluirAnuladas = estadoFilter === 'Anulada' || estadoFilter === 'all'

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const { data: cuotas, isLoading, error } = useQuery({
    queryKey: ['cuotas-admin', searchedCedula, estadoFilter, mesFilter, anioFilter, unidadFilter],
    queryFn: () =>
      cuotasApi.getAdmin({
        documentoIdentidad: searchedCedula!,
        estado: estadoFilter !== 'all' && estadoFilter !== 'Anulada' ? estadoFilter : undefined,
        mes: mesFilter !== 'all' ? parseInt(mesFilter) : undefined,
        anio: anioFilter !== 'all' ? parseInt(anioFilter) : undefined,
        unidadId: unidadFilter !== 'all' ? unidadFilter : undefined,
        incluirAnuladas,
      }),
    enabled: !!searchedCedula,
  })

  const filteredCuotas = estadoFilter === 'Anulada'
    ? cuotas?.filter(c => c.fechaBaja)
    : cuotas

  const pagarMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.marcarPagada(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] }); setConfirmDialog(null) },
  })
  const anularMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.anular(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] }); setConfirmDialog(null) },
  })
  const revertirPagoMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.revertirPago(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] }); setConfirmDialog(null) },
  })
  const revertirAnulacionMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.revertirAnulacion(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] }); setConfirmDialog(null) },
  })

  const handleSearch = () => {
    if (cedula.trim()) setSearchedCedula(cedula.trim())
  }

  const handleConfirm = () => {
    if (!confirmDialog) return
    switch (confirmDialog.type) {
      case 'pagar': pagarMutation.mutate(confirmDialog.id); break
      case 'anular': anularMutation.mutate(confirmDialog.id); break
      case 'revertir-pago': revertirPagoMutation.mutate(confirmDialog.id); break
      case 'revertir-anulacion': revertirAnulacionMutation.mutate(confirmDialog.id); break
    }
  }

  const isProcessing = pagarMutation.isPending || anularMutation.isPending ||
    revertirPagoMutation.isPending || revertirAnulacionMutation.isPending

  const getDialogTitle = (type?: ConfirmAction) => {
    switch (type) {
      case 'pagar': return 'Confirmar pago'
      case 'anular': return 'Confirmar anulacion'
      case 'revertir-pago': return 'Revertir pago'
      case 'revertir-anulacion': return 'Revertir anulacion'
      default: return ''
    }
  }

  const getDialogDescription = (type?: ConfirmAction, plan?: string) => {
    switch (type) {
      case 'pagar': return `Marcar como pagada la cuota de "${plan}"?`
      case 'anular': return `Anular la cuota de "${plan}"?`
      case 'revertir-pago': return `Revertir el pago de la cuota de "${plan}"? Volvera a estado Pendiente.`
      case 'revertir-anulacion': return `Revertir la anulacion de la cuota de "${plan}"? Volvera a estado Pendiente.`
      default: return ''
    }
  }

  const getBadge = (cuota: { estado: string; fechaBaja: string | null }) => {
    if (cuota.fechaBaja) return <Badge variant="outline">Anulada</Badge>
    if (cuota.estado === 'Pagada') return <Badge variant="default">Pagada</Badge>
    return <Badge variant="destructive">Pendiente</Badge>
  }

  const currentYear = new Date().getFullYear()
  const years = Array.from({ length: 5 }, (_, i) => currentYear - i)
  const months = [
    { value: '1', label: 'Enero' }, { value: '2', label: 'Febrero' },
    { value: '3', label: 'Marzo' }, { value: '4', label: 'Abril' },
    { value: '5', label: 'Mayo' }, { value: '6', label: 'Junio' },
    { value: '7', label: 'Julio' }, { value: '8', label: 'Agosto' },
    { value: '9', label: 'Septiembre' }, { value: '10', label: 'Octubre' },
    { value: '11', label: 'Noviembre' }, { value: '12', label: 'Diciembre' },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <CreditCard className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Gestion de Cuotas</h1>
          <p className="text-sm text-muted-foreground">Buscar por cedula del socio</p>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Cedula del socio..."
            value={cedula}
            onChange={(event) => setCedula(event.target.value)}
            onKeyDown={(event) => event.key === 'Enter' && handleSearch()}
            className="pl-9"
          />
        </div>
        <Button onClick={handleSearch} disabled={!cedula.trim()}>
          Buscar
        </Button>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
          {(error as any)?.response?.data?.error || 'Error al buscar cuotas.'}
        </div>
      )}

      {searchedCedula && (
        <div className="flex flex-wrap items-center gap-3">
          <Select value={estadoFilter} onValueChange={(value) => setEstadoFilter(value ?? 'all')}>
            <SelectTrigger className="w-[160px]">
              <SelectValue placeholder="Todos los estados" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los estados</SelectItem>
              <SelectItem value="Pendiente">Pendiente</SelectItem>
              <SelectItem value="Pagada">Pagada</SelectItem>
              <SelectItem value="Anulada">Anulada</SelectItem>
            </SelectContent>
          </Select>

          <Select value={mesFilter} onValueChange={(value) => setMesFilter(value ?? 'all')}>
            <SelectTrigger className="w-[160px]">
              <SelectValue placeholder="Todos los meses" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los meses</SelectItem>
              {months.map((month) => (
                <SelectItem key={month.value} value={month.value}>{month.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={anioFilter} onValueChange={(value) => setAnioFilter(value ?? 'all')}>
            <SelectTrigger className="w-[120px]">
              <SelectValue placeholder="Todos los anos" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los anos</SelectItem>
              {years.map((year) => (
                <SelectItem key={year} value={year.toString()}>{year}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={unidadFilter} onValueChange={(value) => setUnidadFilter(value ?? 'all')}>
            <SelectTrigger className="w-[200px]">
              <SelectValue placeholder="Todas las unidades" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todas las unidades</SelectItem>
              {unidades?.map((unidad) => (
                <SelectItem key={unidad.id} value={unidad.id}>{unidad.nombre}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      {searchedCedula && (
        <div className="rounded-xl border bg-card overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Socio</TableHead>
                <TableHead>Unidad</TableHead>
                <TableHead>Plan</TableHead>
                <TableHead>Monto</TableHead>
                <TableHead>Vencimiento</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground">
                    Cargando...
                  </TableCell>
                </TableRow>
              )}
              {filteredCuotas?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground">
                    No se encontraron cuotas.
                  </TableCell>
                </TableRow>
              )}
              {filteredCuotas?.map((cuota) => (
                <TableRow key={cuota.id} className={cuota.fechaBaja ? 'opacity-60' : ''}>
                  <TableCell className="font-medium">{cuota.nombreSocio}</TableCell>
                  <TableCell>{cuota.nombreUnidad}</TableCell>
                  <TableCell>{cuota.nombrePlan}</TableCell>
                  <TableCell>${cuota.monto.toLocaleString()}</TableCell>
                  <TableCell>{new Date(cuota.fechaVencimiento).toLocaleDateString()}</TableCell>
                  <TableCell>{getBadge(cuota)}</TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      {/* Cuota pendiente: marcar pagada o anular */}
                      {cuota.estado === 'Pendiente' && !cuota.fechaBaja && (
                        <>
                          <Button
                            size="sm"
                            variant="outline"
                            className="gap-1"
                            onClick={() => setConfirmDialog({ type: 'pagar', id: cuota.id, plan: cuota.nombrePlan })}
                          >
                            <CheckCircle className="h-3.5 w-3.5" />
                            Pagada
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            className="gap-1 text-destructive hover:text-destructive"
                            onClick={() => setConfirmDialog({ type: 'anular', id: cuota.id, plan: cuota.nombrePlan })}
                          >
                            <XCircle className="h-3.5 w-3.5" />
                            Anular
                          </Button>
                        </>
                      )}
                      {/* Cuota pagada: revertir pago */}
                      {cuota.estado === 'Pagada' && !cuota.fechaBaja && (
                        <Button
                          size="sm"
                          variant="outline"
                          className="gap-1"
                          onClick={() => setConfirmDialog({ type: 'revertir-pago', id: cuota.id, plan: cuota.nombrePlan })}
                        >
                          <Undo2 className="h-3.5 w-3.5" />
                          Revertir pago
                        </Button>
                      )}
                      {/* Cuota anulada: revertir anulacion */}
                      {cuota.fechaBaja && (
                        <Button
                          size="sm"
                          variant="outline"
                          className="gap-1"
                          onClick={() => setConfirmDialog({ type: 'revertir-anulacion', id: cuota.id, plan: cuota.nombrePlan })}
                        >
                          <Undo2 className="h-3.5 w-3.5" />
                          Revertir
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <Dialog open={!!confirmDialog} onOpenChange={() => setConfirmDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{getDialogTitle(confirmDialog?.type)}</DialogTitle>
            <DialogDescription>
              {getDialogDescription(confirmDialog?.type, confirmDialog?.plan)}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmDialog(null)}>
              Cancelar
            </Button>
            <Button
              variant={confirmDialog?.type === 'anular' ? 'destructive' : 'default'}
              onClick={handleConfirm}
              disabled={isProcessing}
            >
              {isProcessing ? 'Procesando...' : 'Confirmar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
```

- [ ] **Step 2: Verificar que el frontend compila**

Run: `cd frontend && npm run build`
Expected: Build succeeded.

---

### Task 9: Actualizar seed y BackgroundService

**Files:**
- Modify: `backend/src/GymFlow.API/Program.cs` (seed de cuota)
- Verify: `backend/src/GymFlow.API/BackgroundServices/CuotaGeneracionBackgroundService.cs`

- [ ] **Step 1: Actualizar seed del socio de prueba**

El seed actual genera una sola cuota con `AddDays(30)` implícito. Como cambiamos a `AddMonths(1)`, el seed ya usará el nuevo cálculo automáticamente a través del constructor de `Cuota`. No se necesitan cambios al seed.

Verificar que el `BackgroundService` sigue funcionando correctamente: la lógica de `ultimaCuota.FechaVencimiento <= DateTime.UtcNow` sigue siendo válida porque el vencimiento ahora es `AddMonths(1)` en vez de `AddDays(30)`, lo que no cambia la semántica del check.

- [ ] **Step 2: Verificar que compila todo el backend**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

---

### Task 10: Verificación integral

- [ ] **Step 1: Correr todos los tests del backend**

Run: `dotnet test backend/tests/ -v minimal`
Expected: Todos los tests pasan.

- [ ] **Step 2: Compilar frontend**

Run: `cd frontend && npm run build`
Expected: Build succeeded.

- [ ] **Step 3: Verificación funcional**

1. Levantar backend y frontend.
2. Login como admin.
3. Crear un socio con fecha de ingreso pasada (ej: 20/01/2026) → verificar que se generan múltiples cuotas retroactivas.
4. En Gestión de Cuotas, buscar por cédula del socio → verificar cuotas con vencimientos mensuales correctos.
5. Marcar una cuota como pagada → verificar botón "Revertir pago" aparece.
6. Revertir el pago → verificar vuelve a Pendiente.
7. Anular una cuota → verificar que desaparece de la vista por defecto.
8. Filtrar por "Anulada" → verificar que aparece con badge "Anulada" y botón "Revertir".
9. Revertir anulación → verificar vuelve a Pendiente.
