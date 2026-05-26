# RF-07 � Gesti�n de Cuotas: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar generaci�n autom�tica de cuotas, vistas de socio y admin, y acciones de pago/anulaci�n.

**Architecture:** Nueva entidad `Cuota` con generaci�n reactiva (primera cuota al alta) + BackgroundService diario para recurrentes. Servicio de dominio `ICuotaGeneradorService` centraliza la l�gica de creaci�n. RBAC con nuevo m�dulo `Cuotas`.

**Tech Stack:** .NET 8, PostgreSQL, EF Core, React 18, TypeScript, TanStack Query, shadcn/ui, TailwindCSS.

**Nota:** No commitear nada � el usuario se encarga del git.

---

### Task 1: Entidad de dominio `Cuota` + enum `EstadoCuota`

**Files:**
- Create: `backend/src/GymFlow.Domain/Enums/EstadoCuota.cs`
- Create: `backend/src/GymFlow.Domain/Entities/Cuota.cs`
- Modify: `backend/src/GymFlow.Domain/Enums/Modulo.cs`

- [ ] **Step 1: Crear enum `EstadoCuota`**

```csharp
// backend/src/GymFlow.Domain/Enums/EstadoCuota.cs
namespace GymFlow.Domain.Enums;

public enum EstadoCuota
{
    Pendiente,
    Pagada
}
```

- [ ] **Step 2: Agregar `Cuotas` al enum `Modulo`**

En `backend/src/GymFlow.Domain/Enums/Modulo.cs`, agregar `Cuotas` al final:

```csharp
namespace GymFlow.Domain.Enums;

public enum Modulo
{
    Socios,
    Planes,
    Unidades,
    Auditoria,
    Empleados,
    Cuotas
}
```

- [ ] **Step 3: Crear entidad `Cuota`**

```csharp
// backend/src/GymFlow.Domain/Entities/Cuota.cs
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Cuota
{
    public Guid Id { get; private set; }
    public Guid SocioId { get; private set; }
    public Socio Socio { get; private set; } = null!;
    public Guid UnidadId { get; private set; }
    public Unidad Unidad { get; private set; } = null!;
    public Guid PlanId { get; private set; }
    public Plan Plan { get; private set; } = null!;
    public string NombrePlan { get; private set; } = string.Empty;
    public decimal Monto { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public DateTime FechaVencimiento { get; private set; }
    public EstadoCuota Estado { get; private set; }
    public DateTime? FechaPago { get; private set; }
    public DateTime? FechaBaja { get; private set; }

    private Cuota() { } // EF Core

    public Cuota(Guid socioId, Guid unidadId, Guid planId, string nombrePlan, decimal monto, DateTime fechaEmision)
    {
        if (string.IsNullOrWhiteSpace(nombrePlan))
            throw new ArgumentException("El nombre del plan es requerido.", nameof(nombrePlan));
        if (monto < 0)
            throw new ArgumentException("El monto no puede ser negativo.", nameof(monto));

        Id = Guid.NewGuid();
        SocioId = socioId;
        UnidadId = unidadId;
        PlanId = planId;
        NombrePlan = nombrePlan;
        Monto = monto;
        FechaEmision = fechaEmision;
        FechaVencimiento = fechaEmision.AddDays(30);
        Estado = EstadoCuota.Pendiente;
    }

    public void MarcarComoPagada()
    {
        if (Estado == EstadoCuota.Pagada)
            throw new InvalidOperationException("La cuota ya est� pagada.");
        if (FechaBaja.HasValue)
            throw new InvalidOperationException("No se puede pagar una cuota anulada.");

        Estado = EstadoCuota.Pagada;
        FechaPago = DateTime.UtcNow;
    }

    public void Anular()
    {
        if (Estado == EstadoCuota.Pagada)
            throw new InvalidOperationException("No se puede anular una cuota ya pagada.");
        if (FechaBaja.HasValue)
            throw new InvalidOperationException("La cuota ya fue anulada.");

        FechaBaja = DateTime.UtcNow;
    }
}
```

- [ ] **Step 4: Escribir test m�nimo de la entidad `Cuota`**

Create: `backend/tests/GymFlow.Domain.Tests/Entities/CuotaTests.cs`

```csharp
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class CuotaTests
{
    private static Cuota CrearCuotaValida() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Musculaci�n", 2500m, DateTime.UtcNow);

    [Fact]
    public void Constructor_WithValidData_CreatesCuota()
    {
        var fechaEmision = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Completo", 3500m, fechaEmision);

        Assert.NotEqual(Guid.Empty, cuota.Id);
        Assert.Equal("Plan Completo", cuota.NombrePlan);
        Assert.Equal(3500m, cuota.Monto);
        Assert.Equal(fechaEmision, cuota.FechaEmision);
        Assert.Equal(fechaEmision.AddDays(30), cuota.FechaVencimiento);
        Assert.Equal(EstadoCuota.Pendiente, cuota.Estado);
        Assert.Null(cuota.FechaPago);
        Assert.Null(cuota.FechaBaja);
    }

    [Fact]
    public void Constructor_WithNegativeMonto_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan", -100m, DateTime.UtcNow));
    }

    [Fact]
    public void MarcarComoPagada_WhenPendiente_ChangesEstadoAndSetsFechaPago()
    {
        var cuota = CrearCuotaValida();
        cuota.MarcarComoPagada();

        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
        Assert.NotNull(cuota.FechaPago);
    }

    [Fact]
    public void MarcarComoPagada_WhenAlreadyPagada_ThrowsInvalidOperationException()
    {
        var cuota = CrearCuotaValida();
        cuota.MarcarComoPagada();

        Assert.Throws<InvalidOperationException>(() => cuota.MarcarComoPagada());
    }

    [Fact]
    public void Anular_WhenPendiente_SetsFechaBaja()
    {
        var cuota = CrearCuotaValida();
        cuota.Anular();

        Assert.NotNull(cuota.FechaBaja);
    }

    [Fact]
    public void Anular_WhenPagada_ThrowsInvalidOperationException()
    {
        var cuota = CrearCuotaValida();
        cuota.MarcarComoPagada();

        Assert.Throws<InvalidOperationException>(() => cuota.Anular());
    }
}
```

- [ ] **Step 5: Correr tests de dominio**

Run: `dotnet test backend/tests/GymFlow.Domain.Tests/ --filter "CuotaTests" -v minimal`
Expected: 6 tests PASS.

---

### Task 2: EF Core � Configuraci�n, DbSet, Migraci�n

**Files:**
- Create: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/CuotaConfiguration.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`

- [ ] **Step 1: Crear `CuotaConfiguration`**

```csharp
// backend/src/GymFlow.Infrastructure/Persistence/Configurations/CuotaConfiguration.cs
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class CuotaConfiguration : IEntityTypeConfiguration<Cuota>
{
    public void Configure(EntityTypeBuilder<Cuota> builder)
    {
        builder.ToTable("Cuotas");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NombrePlan).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Monto).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(c => c.FechaEmision).IsRequired();
        builder.Property(c => c.FechaVencimiento).IsRequired();
        builder.Property(c => c.Estado).IsRequired();
        builder.Property(c => c.FechaPago);
        builder.Property(c => c.FechaBaja);

        builder.HasOne(c => c.Socio)
            .WithMany()
            .HasForeignKey(c => c.SocioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Unidad)
            .WithMany()
            .HasForeignKey(c => c.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Plan)
            .WithMany()
            .HasForeignKey(c => c.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 2: Agregar `DbSet<Cuota>` y actualizar seed**

En `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`:

Agregar el DbSet junto a los otros:
```csharp
public DbSet<Cuota> Cuotas => Set<Cuota>();
```

No se requiere cambio en el seed de permisos: el seed ya usa `Enum.GetValues<Modulo>()` y `Enum.GetValues<Operacion>()`, as� que al agregar `Cuotas` al enum `Modulo` en Task 1, el seed genera autom�ticamente los 4 permisos de Cuotas y los asigna al rol Admin.

- [ ] **Step 3: Generar migraci�n**

Run: `dotnet ef migrations add AddCuotas --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`
Expected: Migraci�n creada sin errores.

- [ ] **Step 4: Aplicar migraci�n**

Run: `dotnet ef database update --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`
Expected: Migraci�n aplicada. Tabla `Cuotas` creada + 4 nuevos permisos del m�dulo Cuotas seeded.

---

### Task 3: Repositorio `ICuotaRepository`

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/ICuotaRepository.cs`
- Create: `backend/src/GymFlow.Infrastructure/Repositories/CuotaRepository.cs`
- Modify: `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Crear interfaz `ICuotaRepository`**

```csharp
// backend/src/GymFlow.Application/Interfaces/ICuotaRepository.cs
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface ICuotaRepository
{
    Task<Cuota?> GetByIdAsync(Guid id);
    Task<IEnumerable<Cuota>> GetBySocioIdAsync(Guid socioId);
    Task<IEnumerable<Cuota>> SearchAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId);
    Task<Cuota?> GetUltimaCuotaAsync(Guid socioId, Guid unidadId);
    Task AddAsync(Cuota cuota);
    Task SaveChangesAsync();
}
```

- [ ] **Step 2: Crear `CuotaRepository`**

```csharp
// backend/src/GymFlow.Infrastructure/Repositories/CuotaRepository.cs
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class CuotaRepository : ICuotaRepository
{
    private readonly GymFlowDbContext _context;

    public CuotaRepository(GymFlowDbContext context) => _context = context;

    public async Task<Cuota?> GetByIdAsync(Guid id)
    {
        return await _context.Cuotas
            .Include(c => c.Socio)
            .Include(c => c.Unidad)
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Cuota>> GetBySocioIdAsync(Guid socioId)
    {
        return await _context.Cuotas
            .Include(c => c.Unidad)
            .Where(c => c.SocioId == socioId && !c.FechaBaja.HasValue)
            .OrderByDescending(c => c.FechaVencimiento)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cuota>> SearchAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId)
    {
        var query = _context.Cuotas
            .Include(c => c.Socio)
            .Include(c => c.Unidad)
            .Where(c => c.SocioId == socioId && !c.FechaBaja.HasValue)
            .AsQueryable();

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

    public async Task<Cuota?> GetUltimaCuotaAsync(Guid socioId, Guid unidadId)
    {
        return await _context.Cuotas
            .Where(c => c.SocioId == socioId && c.UnidadId == unidadId && !c.FechaBaja.HasValue)
            .OrderByDescending(c => c.FechaVencimiento)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Cuota cuota) => await _context.Cuotas.AddAsync(cuota);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
```

- [ ] **Step 3: Registrar en DI**

En `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`, agregar despu�s de la l�nea `services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();`:

```csharp
services.AddScoped<ICuotaRepository, CuotaRepository>();
```

No olvidar el `using GymFlow.Infrastructure.Repositories;` (ya deber�a estar).

- [ ] **Step 4: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.Infrastructure/`
Expected: Build succeeded.

---

### Task 4: Servicio de dominio `ICuotaGeneradorService`

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/ICuotaGeneradorService.cs`
- Create: `backend/src/GymFlow.Infrastructure/Services/CuotaGeneradorService.cs`
- Modify: `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Crear interfaz**

```csharp
// backend/src/GymFlow.Application/Interfaces/ICuotaGeneradorService.cs
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface ICuotaGeneradorService
{
    Task<Cuota> GenerarCuotaAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaEmision);
}
```

- [ ] **Step 2: Crear implementaci�n**

```csharp
// backend/src/GymFlow.Infrastructure/Services/CuotaGeneradorService.cs
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Infrastructure.Services;

public class CuotaGeneradorService : ICuotaGeneradorService
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IPlanRepository _planRepository;

    public CuotaGeneradorService(ICuotaRepository cuotaRepository, IPlanRepository planRepository)
    {
        _cuotaRepository = cuotaRepository;
        _planRepository = planRepository;
    }

    public async Task<Cuota> GenerarCuotaAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaEmision)
    {
        if (!usuarioUnidad.PlanId.HasValue)
            throw new InvalidOperationException("El socio no tiene plan asignado en esta unidad.");

        var plan = await _planRepository.GetByIdAsync(usuarioUnidad.PlanId.Value)
            ?? throw new InvalidOperationException("El plan asignado no existe.");

        var cuota = new Cuota(
            socioId: socioId,
            unidadId: usuarioUnidad.UnidadId,
            planId: plan.Id,
            nombrePlan: plan.Nombre,
            monto: plan.Precio,
            fechaEmision: fechaEmision);

        await _cuotaRepository.AddAsync(cuota);
        return cuota;
    }
}
```

- [ ] **Step 3: Registrar en DI**

En `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`, agregar:

```csharp
services.AddScoped<ICuotaGeneradorService, CuotaGeneradorService>();
```

- [ ] **Step 4: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.Infrastructure/`
Expected: Build succeeded.

---

### Task 5: Modificar `CreateSocioCommand` � generar primera cuota al alta

**Files:**
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`

- [ ] **Step 1: Inyectar `ICuotaGeneradorService` en `CreateSocioCommand`**

Agregar campo y par�metro de constructor:

```csharp
private readonly ICuotaGeneradorService _cuotaGenerador;
```

Agregar al constructor:
```csharp
public CreateSocioCommand(
    ISocioRepository socioRepository,
    IUnidadRepository unidadRepository,
    IPlanRepository planRepository,
    IRolRepository rolRepository,
    IAuditLogger auditLogger,
    ICuotaGeneradorService cuotaGenerador)
{
    _socioRepository = socioRepository;
    _unidadRepository = unidadRepository;
    _planRepository = planRepository;
    _rolRepository = rolRepository;
    _auditLogger = auditLogger;
    _cuotaGenerador = cuotaGenerador;
}
```

- [ ] **Step 2: Generar cuotas despu�s del `SaveChangesAsync`**

En el m�todo `ExecuteAsync`, despu�s de `await _socioRepository.SaveChangesAsync();` y antes del `await _auditLogger.LogAsync(...)`, agregar:

```csharp
foreach (var asignacion in unidades)
{
    if (asignacion.PlanId.HasValue)
    {
        var uu = socio.UnidadesAsignadas.First(u => u.UnidadId == asignacion.UnidadId);
        await _cuotaGenerador.GenerarCuotaAsync(socio.Id, uu, socio.FechaAlta);
    }
}
await _socioRepository.SaveChangesAsync();
```

- [ ] **Step 3: Actualizar test existente de `CreateSocioCommand`**

En `backend/tests/GymFlow.Application.Tests/UseCases/CreateSocioCommandTests.cs`, agregar el mock de `ICuotaGeneradorService` al setup de cada test que construye el command:

```csharp
var cuotaGenerador = new Mock<ICuotaGeneradorService>();
```

Y pasarlo al constructor:
```csharp
var sut = new CreateSocioCommand(socioRepo.Object, unidadRepo.Object, planRepo.Object, rolRepo.Object, audit.Object, cuotaGenerador.Object);
```

- [ ] **Step 4: Correr tests existentes**

Run: `dotnet test backend/tests/GymFlow.Application.Tests/ --filter "CreateSocioCommand" -v minimal`
Expected: Todos los tests existentes pasan (el mock no necesita setup porque los tests existentes pueden no tener plan asignado, o el mock por defecto retorna null/default que no afecta).

---

### Task 6: Commands � MarcarCuotaPagada y AnularCuota

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Cuotas/MarcarCuotaPagadaCommand.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Cuotas/AnularCuotaCommand.cs`
- Create: `backend/src/GymFlow.Application/DTOs/CuotaDto.cs`
- Modify: `backend/src/GymFlow.API/DependencyInjection.cs`

- [ ] **Step 1: Crear `CuotaDto`**

```csharp
// backend/src/GymFlow.Application/DTOs/CuotaDto.cs
using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record CuotaDto(
    Guid Id,
    string NombrePlan,
    string NombreUnidad,
    string? NombreSocio,
    decimal Monto,
    DateTime FechaEmision,
    DateTime FechaVencimiento,
    EstadoCuota Estado,
    DateTime? FechaPago);
```

- [ ] **Step 2: Crear `MarcarCuotaPagadaCommand`**

```csharp
// backend/src/GymFlow.Application/UseCases/Cuotas/MarcarCuotaPagadaCommand.cs
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class MarcarCuotaPagadaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IAuditLogger _auditLogger;

    public MarcarCuotaPagadaCommand(ICuotaRepository cuotaRepository, IAuditLogger auditLogger)
    {
        _cuotaRepository = cuotaRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid cuotaId, Guid usuarioId, string usuarioNombre)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        cuota.MarcarComoPagada();
        await _cuotaRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id,
            $"Se marc� como pagada la cuota de {cuota.NombrePlan} del socio {cuota.Socio?.Nombre} {cuota.Socio?.Apellido}");
    }
}
```

- [ ] **Step 3: Crear `AnularCuotaCommand`**

```csharp
// backend/src/GymFlow.Application/UseCases/Cuotas/AnularCuotaCommand.cs
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class AnularCuotaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IAuditLogger _auditLogger;

    public AnularCuotaCommand(ICuotaRepository cuotaRepository, IAuditLogger auditLogger)
    {
        _cuotaRepository = cuotaRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid cuotaId, Guid usuarioId, string usuarioNombre)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        cuota.Anular();
        await _cuotaRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Cuota", cuota.Id,
            $"Se anul� la cuota de {cuota.NombrePlan} del socio {cuota.Socio?.Nombre} {cuota.Socio?.Apellido}");
    }
}
```

- [ ] **Step 4: Registrar commands en DI**

En `backend/src/GymFlow.API/DependencyInjection.cs`, agregar el `using` y los registros:

```csharp
using GymFlow.Application.UseCases.Cuotas;
```

Al final del m�todo `AddApplication`:
```csharp
services.AddScoped<MarcarCuotaPagadaCommand>();
services.AddScoped<AnularCuotaCommand>();
```

- [ ] **Step 5: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

---

### Task 7: Queries � GetCuotasBySocio y GetCuotasAdmin

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasBySocioQuery.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasAdminQuery.cs`
- Modify: `backend/src/GymFlow.API/DependencyInjection.cs`

- [ ] **Step 1: Crear `GetCuotasBySocioQuery`**

```csharp
// backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasBySocioQuery.cs
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Cuotas;

public class GetCuotasBySocioQuery
{
    private readonly ICuotaRepository _cuotaRepository;

    public GetCuotasBySocioQuery(ICuotaRepository cuotaRepository) => _cuotaRepository = cuotaRepository;

    public async Task<IEnumerable<CuotaDto>> ExecuteAsync(Guid socioId)
    {
        var cuotas = await _cuotaRepository.GetBySocioIdAsync(socioId);
        return cuotas.Select(MapToDto);
    }

    internal static CuotaDto MapToDto(Cuota c) => new(
        Id: c.Id,
        NombrePlan: c.NombrePlan,
        NombreUnidad: c.Unidad?.Nombre ?? "",
        NombreSocio: c.Socio != null ? $"{c.Socio.Nombre} {c.Socio.Apellido}" : null,
        Monto: c.Monto,
        FechaEmision: c.FechaEmision,
        FechaVencimiento: c.FechaVencimiento,
        Estado: c.Estado,
        FechaPago: c.FechaPago);
}
```

- [ ] **Step 2: Crear `GetCuotasAdminQuery`**

```csharp
// backend/src/GymFlow.Application/UseCases/Cuotas/GetCuotasAdminQuery.cs
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class GetCuotasAdminQuery
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly ISocioRepository _socioRepository;

    public GetCuotasAdminQuery(ICuotaRepository cuotaRepository, ISocioRepository socioRepository)
    {
        _cuotaRepository = cuotaRepository;
        _socioRepository = socioRepository;
    }

    public async Task<IEnumerable<CuotaDto>> ExecuteAsync(string documentoIdentidad, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId)
    {
        var socio = (await _socioRepository.GetAllAsync(includeInactive: true))
            .FirstOrDefault(s => s.DocumentoIdentidad == documentoIdentidad)
            ?? throw new KeyNotFoundException("No se encontr� un socio con ese documento de identidad.");

        var cuotas = await _cuotaRepository.SearchAsync(socio.Id, estado, mes, anio, unidadId);
        return cuotas.Select(GetCuotasBySocioQuery.MapToDto);
    }
}
```

- [ ] **Step 3: Registrar queries en DI**

En `backend/src/GymFlow.API/DependencyInjection.cs`, agregar:

```csharp
services.AddScoped<GetCuotasBySocioQuery>();
services.AddScoped<GetCuotasAdminQuery>();
```

- [ ] **Step 4: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

---

### Task 8: Controller `CuotasController`

**Files:**
- Create: `backend/src/GymFlow.API/Controllers/CuotasController.cs`

- [ ] **Step 1: Crear `CuotasController`**

```csharp
// backend/src/GymFlow.API/Controllers/CuotasController.cs
using System.Security.Claims;
using GymFlow.API.Authorization;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CuotasController : ControllerBase
{
    private readonly GetCuotasBySocioQuery _getCuotasBySocioQuery;
    private readonly GetCuotasAdminQuery _getCuotasAdminQuery;
    private readonly MarcarCuotaPagadaCommand _marcarPagadaCommand;
    private readonly AnularCuotaCommand _anularCommand;

    public CuotasController(
        GetCuotasBySocioQuery getCuotasBySocioQuery,
        GetCuotasAdminQuery getCuotasAdminQuery,
        MarcarCuotaPagadaCommand marcarPagadaCommand,
        AnularCuotaCommand anularCommand)
    {
        _getCuotasBySocioQuery = getCuotasBySocioQuery;
        _getCuotasAdminQuery = getCuotasAdminQuery;
        _marcarPagadaCommand = marcarPagadaCommand;
        _anularCommand = anularCommand;
    }

    [HttpGet("mis-cuotas")]
    public async Task<ActionResult<IEnumerable<CuotaDto>>> GetMisCuotas()
    {
        var socioId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var cuotas = await _getCuotasBySocioQuery.ExecuteAsync(socioId);
        return Ok(cuotas);
    }

    [HttpGet("admin")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Lectura)]
    public async Task<ActionResult<IEnumerable<CuotaDto>>> GetAdmin(
        [FromQuery] string documentoIdentidad,
        [FromQuery] EstadoCuota? estado,
        [FromQuery] int? mes,
        [FromQuery] int? anio,
        [FromQuery] Guid? unidadId)
    {
        try
        {
            var cuotas = await _getCuotasAdminQuery.ExecuteAsync(documentoIdentidad, estado, mes, anio, unidadId);
            return Ok(cuotas);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/pagar")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Modificacion)]
    public async Task<IActionResult> MarcarComoPagada(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _marcarPagadaCommand.ExecuteAsync(id, userId, userName);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Eliminacion)]
    public async Task<IActionResult> Anular(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _anularCommand.ExecuteAsync(id, userId, userName);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    private (Guid Id, string Nombre) GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var nombre = User.FindFirst("nombre")?.Value ?? "";
        var apellido = User.FindFirst("apellido")?.Value ?? "";
        var fullName = $"{nombre} {apellido}".Trim();
        return (userId, string.IsNullOrWhiteSpace(fullName) ? "Sistema" : fullName);
    }
}
```

- [ ] **Step 2: Verificar que compila todo el backend**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

---

### Task 9: BackgroundService � `CuotaGeneracionBackgroundService`

**Files:**
- Create: `backend/src/GymFlow.API/BackgroundServices/CuotaGeneracionBackgroundService.cs`
- Modify: `backend/src/GymFlow.API/Program.cs`
- Modify: `backend/src/GymFlow.API/appsettings.json`

- [ ] **Step 1: Agregar configuraci�n a `appsettings.json`**

En `backend/src/GymFlow.API/appsettings.json`, agregar al nivel ra�z:

```json
"CuotaGeneracion": {
  "HoraEjecucion": "03:00",
  "Habilitado": true
}
```

- [ ] **Step 2: Crear el BackgroundService**

```csharp
// backend/src/GymFlow.API/BackgroundServices/CuotaGeneracionBackgroundService.cs
using GymFlow.Application.Interfaces;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.API.BackgroundServices;

public class CuotaGeneracionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CuotaGeneracionBackgroundService> _logger;

    public CuotaGeneracionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CuotaGeneracionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var habilitado = _configuration.GetValue<bool>("CuotaGeneracion:Habilitado");
        if (!habilitado)
        {
            _logger.LogInformation("CuotaGeneracionBackgroundService est� deshabilitado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalcularDelay();
            _logger.LogInformation("Pr�xima generaci�n de cuotas en {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await GenerarCuotasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la generaci�n autom�tica de cuotas.");
            }
        }
    }

    private TimeSpan CalcularDelay()
    {
        var horaConfig = _configuration["CuotaGeneracion:HoraEjecucion"] ?? "03:00";
        var hora = TimeSpan.Parse(horaConfig);
        var ahora = DateTime.UtcNow;
        var proximaEjecucion = ahora.Date.Add(hora);

        if (proximaEjecucion <= ahora)
            proximaEjecucion = proximaEjecucion.AddDays(1);

        return proximaEjecucion - ahora;
    }

    private async Task GenerarCuotasAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymFlowDbContext>();
        var cuotaGenerador = scope.ServiceProvider.GetRequiredService<ICuotaGeneradorService>();
        var cuotaRepo = scope.ServiceProvider.GetRequiredService<ICuotaRepository>();

        var sociosActivos = await db.Socios
            .Include(s => s.UnidadesAsignadas)
            .Where(s => s.EstaActivo)
            .ToListAsync();

        var cuotasGeneradas = 0;

        foreach (var socio in sociosActivos)
        {
            foreach (var uu in socio.UnidadesAsignadas.Where(u => u.PlanId.HasValue))
            {
                var ultimaCuota = await cuotaRepo.GetUltimaCuotaAsync(socio.Id, uu.UnidadId);

                if (ultimaCuota == null || ultimaCuota.FechaVencimiento <= DateTime.UtcNow)
                {
                    var fechaEmision = ultimaCuota?.FechaVencimiento ?? DateTime.UtcNow;
                    await cuotaGenerador.GenerarCuotaAsync(socio.Id, uu, fechaEmision);
                    cuotasGeneradas++;
                }
            }
        }

        await cuotaRepo.SaveChangesAsync();
        _logger.LogInformation("Generaci�n autom�tica completada: {Count} cuotas generadas.", cuotasGeneradas);
    }
}
```

- [ ] **Step 3: Registrar el BackgroundService en `Program.cs`**

En `backend/src/GymFlow.API/Program.cs`, despu�s de `builder.Services.AddApplication();`, agregar:

```csharp
builder.Services.AddHostedService<GymFlow.API.BackgroundServices.CuotaGeneracionBackgroundService>();
```

- [ ] **Step 4: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

---

### Task 10: Test m�nimo de `MarcarCuotaPagadaCommand`

**Files:**
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Cuotas/MarcarCuotaPagadaCommandTests.cs`

- [ ] **Step 1: Escribir tests**

```csharp
// backend/tests/GymFlow.Application.Tests/UseCases/Cuotas/MarcarCuotaPagadaCommandTests.cs
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class MarcarCuotaPagadaCommandTests
{
    [Fact]
    public async Task ExecuteAsync_CuotaExiste_MarcaComoPagadaYAudita()
    {
        var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, DateTime.UtcNow);
        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        var audit = new Mock<IAuditLogger>();

        var sut = new MarcarCuotaPagadaCommand(repo.Object, audit.Object);
        await sut.ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin Test");

        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
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

        var sut = new MarcarCuotaPagadaCommand(repo.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));
    }
}
```

- [ ] **Step 2: Correr tests**

Run: `dotnet test backend/tests/GymFlow.Application.Tests/ --filter "MarcarCuotaPagadaCommandTests" -v minimal`
Expected: 2 tests PASS.

---

### Task 11: Frontend � tipos, servicio API, tipo de permisos

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/types/permisos.ts`
- Modify: `frontend/src/services/api.ts`

- [x] **Step 1: Agregar tipos de cuota en `types/index.ts`**

Al final del archivo agregar:

```typescript
export type EstadoCuota = 'Pendiente' | 'Pagada'

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
}
```

- [x] **Step 2: Agregar `Cuotas` al tipo `Modulo` en `types/permisos.ts`**

Cambiar la l�nea:
```typescript
export type Modulo = 'Socios' | 'Planes' | 'Unidades' | 'Auditoria' | 'Empleados'
```
A:
```typescript
export type Modulo = 'Socios' | 'Planes' | 'Unidades' | 'Auditoria' | 'Empleados' | 'Cuotas'
```

- [x] **Step 3: Agregar servicio API de cuotas en `services/api.ts`**

Al final del archivo, antes del `export default api`, agregar:

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
}
```

Agregar `CuotaDto` al import de types en el mismo archivo:
```typescript
import type { ..., CuotaDto } from '@/types'
```

---

### Task 12: Frontend � P�gina "Mis Cuotas" (portal socio)

**Files:**
- Create: `frontend/src/pages/portal/MisCuotasPage.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/components/layout/SocioLayout.tsx`

- [x] **Step 1: Crear `MisCuotasPage.tsx`**

```tsx
// frontend/src/pages/portal/MisCuotasPage.tsx
import { useQuery } from '@tanstack/react-query'
import { cuotasApi } from '@/services/api'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { CreditCard } from 'lucide-react'

export default function MisCuotasPage() {
  const { data: cuotas, isLoading } = useQuery({
    queryKey: ['mis-cuotas'],
    queryFn: cuotasApi.getMisCuotas,
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <CreditCard className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Mis Cuotas</h1>
          <p className="text-sm text-muted-foreground">Historial de cuotas y estado de pagos</p>
        </div>
      </div>

      <div className="rounded-xl border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Plan</TableHead>
              <TableHead>Unidad</TableHead>
              <TableHead>Monto</TableHead>
              <TableHead>Vencimiento</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead className="text-right">Acci�n</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground">
                  Cargando...
                </TableCell>
              </TableRow>
            )}
            {cuotas?.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground">
                  No ten�s cuotas registradas.
                </TableCell>
              </TableRow>
            )}
            {cuotas?.map((cuota) => (
              <TableRow key={cuota.id}>
                <TableCell className="font-medium">{cuota.nombrePlan}</TableCell>
                <TableCell>{cuota.nombreUnidad}</TableCell>
                <TableCell>${cuota.monto.toLocaleString()}</TableCell>
                <TableCell>{new Date(cuota.fechaVencimiento).toLocaleDateString()}</TableCell>
                <TableCell>
                  <Badge variant={cuota.estado === 'Pagada' ? 'default' : 'destructive'}>
                    {cuota.estado}
                  </Badge>
                </TableCell>
                <TableCell className="text-right">
                  {cuota.estado === 'Pendiente' && (
                    <Button size="sm" variant="outline" disabled>
                      Pagar
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
```

- [x] **Step 2: Agregar ruta en `App.tsx`**

Agregar import:
```typescript
import MisCuotasPage from '@/pages/portal/MisCuotasPage'
```

Dentro del bloque `<Route path="/portal" element={<SocioLayout />}>`, agregar:
```tsx
<Route path="mis-cuotas" element={<MisCuotasPage />} />
```

- [x] **Step 3: Agregar link en `SocioLayout.tsx`**

Agregar import de `CreditCard` y `Link`/`useLocation`:
```typescript
import { Link, Outlet, Navigate, useLocation } from 'react-router-dom'
import { Dumbbell, LogOut, User, CreditCard } from 'lucide-react'
```

Reemplazar el bloque `<nav>` hardcodeado con tabs din�micas:

```tsx
{/* Nav */}
<nav className="mx-auto flex max-w-4xl gap-4 px-6 pb-0">
  <Link
    to="/portal/perfil"
    className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
      location.pathname === '/portal' || location.pathname === '/portal/perfil'
        ? 'border-primary text-primary'
        : 'border-transparent text-muted-foreground hover:text-foreground'
    }`}
  >
    <User className="h-4 w-4" />
    Mi Perfil
  </Link>
  <Link
    to="/portal/mis-cuotas"
    className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
      location.pathname === '/portal/mis-cuotas'
        ? 'border-primary text-primary'
        : 'border-transparent text-muted-foreground hover:text-foreground'
    }`}
  >
    <CreditCard className="h-4 w-4" />
    Mis Cuotas
  </Link>
</nav>
```

Agregar `useLocation` al componente:
```typescript
const location = useLocation()
```

---

### Task 13: Frontend � P�gina "Gesti�n de Cuotas" (admin)

**Files:**
- Create: `frontend/src/pages/admin/CuotasPage.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [x] **Step 1: Crear `CuotasPage.tsx`**

```tsx
// frontend/src/pages/admin/CuotasPage.tsx
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
import { CreditCard, Search, CheckCircle, XCircle } from 'lucide-react'

export default function CuotasPage() {
  const queryClient = useQueryClient()
  const [cedula, setCedula] = useState('')
  const [searchedCedula, setSearchedCedula] = useState<string | null>(null)
  const [estadoFilter, setEstadoFilter] = useState<string>('all')
  const [mesFilter, setMesFilter] = useState<string>('all')
  const [anioFilter, setAnioFilter] = useState<string>('all')
  const [unidadFilter, setUnidadFilter] = useState<string>('all')
  const [confirmDialog, setConfirmDialog] = useState<{
    type: 'pagar' | 'anular'
    id: string
    plan: string
  } | null>(null)

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const { data: cuotas, isLoading, error } = useQuery({
    queryKey: ['cuotas-admin', searchedCedula, estadoFilter, mesFilter, anioFilter, unidadFilter],
    queryFn: () =>
      cuotasApi.getAdmin({
        documentoIdentidad: searchedCedula!,
        estado: estadoFilter !== 'all' ? estadoFilter : undefined,
        mes: mesFilter !== 'all' ? parseInt(mesFilter) : undefined,
        anio: anioFilter !== 'all' ? parseInt(anioFilter) : undefined,
        unidadId: unidadFilter !== 'all' ? unidadFilter : undefined,
      }),
    enabled: !!searchedCedula,
  })

  const pagarMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.marcarPagada(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] })
      setConfirmDialog(null)
    },
  })

  const anularMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.anular(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] })
      setConfirmDialog(null)
    },
  })

  const handleSearch = () => {
    if (cedula.trim()) {
      setSearchedCedula(cedula.trim())
    }
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
          <h1 className="text-2xl font-bold">Gesti�n de Cuotas</h1>
          <p className="text-sm text-muted-foreground">Buscar por c�dula del socio</p>
        </div>
      </div>

      {/* Search */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="C�dula del socio..."
            value={cedula}
            onChange={(e) => setCedula(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
            className="pl-9"
          />
        </div>
        <Button onClick={handleSearch} disabled={!cedula.trim()}>
          Buscar
        </Button>
      </div>

      {/* Error */}
      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
          {(error as any)?.response?.data?.error || 'Error al buscar cuotas.'}
        </div>
      )}

      {/* Filters (after search) */}
      {searchedCedula && (
        <div className="flex flex-wrap items-center gap-3">
          <Select value={estadoFilter} onValueChange={setEstadoFilter}>
            <SelectTrigger className="w-[160px]">
              <SelectValue>{estadoFilter === 'all' ? 'Todos los estados' : estadoFilter}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los estados</SelectItem>
              <SelectItem value="Pendiente">Pendiente</SelectItem>
              <SelectItem value="Pagada">Pagada</SelectItem>
            </SelectContent>
          </Select>

          <Select value={mesFilter} onValueChange={setMesFilter}>
            <SelectTrigger className="w-[160px]">
              <SelectValue>{mesFilter === 'all' ? 'Todos los meses' : months.find(m => m.value === mesFilter)?.label}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los meses</SelectItem>
              {months.map((m) => (
                <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={anioFilter} onValueChange={setAnioFilter}>
            <SelectTrigger className="w-[120px]">
              <SelectValue>{anioFilter === 'all' ? 'Todos los a�os' : anioFilter}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los a�os</SelectItem>
              {years.map((y) => (
                <SelectItem key={y} value={y.toString()}>{y}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={unidadFilter} onValueChange={setUnidadFilter}>
            <SelectTrigger className="w-[200px]">
              <SelectValue>{unidadFilter === 'all' ? 'Todas las unidades' : unidades?.find(u => u.id === unidadFilter)?.nombre}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todas las unidades</SelectItem>
              {unidades?.map((u) => (
                <SelectItem key={u.id} value={u.id}>{u.nombre}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      {/* Table */}
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
              {cuotas?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground">
                    No se encontraron cuotas.
                  </TableCell>
                </TableRow>
              )}
              {cuotas?.map((cuota) => (
                <TableRow key={cuota.id}>
                  <TableCell className="font-medium">{cuota.nombreSocio}</TableCell>
                  <TableCell>{cuota.nombreUnidad}</TableCell>
                  <TableCell>{cuota.nombrePlan}</TableCell>
                  <TableCell>${cuota.monto.toLocaleString()}</TableCell>
                  <TableCell>{new Date(cuota.fechaVencimiento).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <Badge variant={cuota.estado === 'Pagada' ? 'default' : 'destructive'}>
                      {cuota.estado}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right">
                    {cuota.estado === 'Pendiente' && (
                      <div className="flex justify-end gap-2">
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
                      </div>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Confirm Dialog */}
      <Dialog open={!!confirmDialog} onOpenChange={() => setConfirmDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {confirmDialog?.type === 'pagar' ? 'Confirmar pago' : 'Confirmar anulaci�n'}
            </DialogTitle>
            <DialogDescription>
              {confirmDialog?.type === 'pagar'
                ? `�Marcar como pagada la cuota de "${confirmDialog?.plan}"?`
                : `�Anular la cuota de "${confirmDialog?.plan}"? Esta acci�n no se puede deshacer.`}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmDialog(null)}>
              Cancelar
            </Button>
            <Button
              variant={confirmDialog?.type === 'anular' ? 'destructive' : 'default'}
              onClick={() => {
                if (confirmDialog?.type === 'pagar') {
                  pagarMutation.mutate(confirmDialog.id)
                } else if (confirmDialog) {
                  anularMutation.mutate(confirmDialog.id)
                }
              }}
              disabled={pagarMutation.isPending || anularMutation.isPending}
            >
              {pagarMutation.isPending || anularMutation.isPending ? 'Procesando...' : 'Confirmar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
```

- [x] **Step 2: Agregar ruta en `App.tsx`**

Agregar import:
```typescript
import CuotasPage from '@/pages/admin/CuotasPage'
```

Dentro del bloque `<Route path="/admin" element={<AdminLayout />}>`, agregar:
```tsx
<Route path="cuotas" element={<CuotasPage />} />
```

- [x] **Step 3: Agregar al Sidebar**

En `frontend/src/components/layout/Sidebar.tsx`:

Agregar `Receipt` al import de lucide-react (o usar `CreditCard` que ya est� importado � pero como ya se usa en Planes, usar `Receipt`):
```typescript
import { ..., Receipt } from 'lucide-react'
```

Agregar un nuevo grupo en el array `navigation`, despu�s del grupo de `Planes`:

```typescript
{
  label: 'Cuotas',
  icon: <Receipt className="h-5 w-5" />,
  modulo: 'Cuotas' as Modulo,
  items: [
    { label: 'Gesti�n de cuotas', path: '/admin/cuotas', icon: <Receipt className="h-4 w-4" /> },
  ],
},
```

- [x] **Step 4: Verificar que el frontend compila**

Run: `cd frontend && npm run build`
Expected: Build succeeded.

---

### Task 14: Verificaci�n integral

- [x] **Step 1: Correr todos los tests del backend**

Run: `dotnet test backend/tests/ -v minimal`
Expected: Todos los tests pasan (existentes + nuevos).

- [x] **Step 2: Levantar el backend y verificar migraci�n**

Run: `dotnet run --project backend/src/GymFlow.API/`
Expected: App arranca sin errores, migraci�n se aplica, BackgroundService loguea su pr�xima ejecuci�n.

- [ ] **Step 3: Verificar endpoints con el frontend**

1. Login como admin ? verificar que "Cuotas" aparece en el sidebar.
2. Ir a Gesti�n de Cuotas ? buscar por c�dula del socio de prueba (`12345672`).
3. Verificar que se muestran las cuotas (deber�a haber al menos 1 generada al crear el socio seed).
4. Marcar una cuota como pagada ? verificar badge cambia a verde.
5. Login como socio ? verificar que "Mis Cuotas" aparece en la nav del portal.
6. Ir a Mis Cuotas ? verificar que se listan las cuotas.

---

### Task 15: Seed del socio de prueba � generar cuota inicial

**Files:**
- Modify: `backend/src/GymFlow.API/Program.cs`

- [x] **Step 1: Generar cuota para el socio seed**

En `Program.cs`, dentro del bloque que seedea el socio de prueba (`if (!db.Socios.Any(s => s.Correo == "socio@gymflow.com"))`), despu�s de `db.SaveChanges();`, agregar:

```csharp
// Generar cuota inicial para el socio seed
if (!db.Set<GymFlow.Domain.Entities.Cuota>().Any(c => c.SocioId == socio.Id))
{
    var cuotaSeed = new GymFlow.Domain.Entities.Cuota(
        socioId: socio.Id,
        unidadId: unidad.Id,
        planId: plan.Id,
        nombrePlan: plan.Nombre,
        monto: plan.Precio,
        fechaEmision: socio.FechaAlta);
    db.Set<GymFlow.Domain.Entities.Cuota>().Add(cuotaSeed);
    db.SaveChanges();
}
```

- [x] **Step 2: Verificar que compila y el seed funciona**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded. Al levantar el backend, se crea la cuota para el socio de prueba.
