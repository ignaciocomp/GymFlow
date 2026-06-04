---
tags:
  - plan
requerimiento: RF-22
---

# RF-22: Gestión de Planes y Plan por Unidad — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Spec:** [[spec-rf22-planes-por-unidad]]
**Última actualización:** 2026-04-06
**Historial:**
- 2026-04-06 — Versión inicial

**Goal:** Add CRUD management for Plans and move plan assignment from Socio-level to per-Unidad-level (UsuarioUnidad).

**Architecture:** Move PlanId from Socio entity to UsuarioUnidad join entity. Add Create/Update/Delete commands for Plans. Restructure socio create/edit payloads to carry plan-per-unit assignments. Update frontend forms and add 3 new plan management pages.

**Tech Stack:** C# .NET 8, EF Core, PostgreSQL 16, xUnit + Moq, React 19 + TypeScript + Tailwind CSS 4 + shadcn/ui

---

## File Map

### New Files (Backend)
- `backend/src/GymFlow.Application/DTOs/UnidadAsignacionDto.cs`
- `backend/src/GymFlow.Application/DTOs/CreatePlanRequest.cs`
- `backend/src/GymFlow.Application/DTOs/UpdatePlanRequest.cs`
- `backend/src/GymFlow.Application/UseCases/Planes/CreatePlanCommand.cs`
- `backend/src/GymFlow.Application/UseCases/Planes/UpdatePlanCommand.cs`
- `backend/src/GymFlow.Application/UseCases/Planes/DeletePlanCommand.cs`
- `backend/src/GymFlow.Application/UseCases/Planes/GetPlanByIdQuery.cs`

### Modified Files (Backend)
- `backend/src/GymFlow.Domain/Entities/UsuarioUnidad.cs` — add PlanId + Plan nav property
- `backend/src/GymFlow.Domain/Entities/Socio.cs` — remove PlanId/Plan
- `backend/src/GymFlow.Domain/Entities/Plan.cs` — add Actualizar method
- `backend/src/GymFlow.Application/DTOs/CreateSocioRequest.cs` — use UnidadAsignacionDto
- `backend/src/GymFlow.Application/DTOs/UpdateSocioRequest.cs` — use UnidadAsignacionDto
- `backend/src/GymFlow.Application/DTOs/SocioDto.cs` — update Unidades shape
- `backend/src/GymFlow.Application/DTOs/UnidadDto.cs` — add PlanId/PlanNombre
- `backend/src/GymFlow.Application/DTOs/PlanDto.cs` — add EstaActivo, UnidadNombre
- `backend/src/GymFlow.Application/Interfaces/IPlanRepository.cs` — add Add/Save/HasSocios
- `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs` — adapt to new DTO
- `backend/src/GymFlow.Application/UseCases/Socios/UpdateSocioCommand.cs` — adapt to new DTO
- `backend/src/GymFlow.Application/UseCases/Socios/GetSocioByIdQuery.cs` — update MapToDto
- `backend/src/GymFlow.Application/UseCases/Socios/GetSociosQuery.cs` — update MapToDto
- `backend/src/GymFlow.Application/UseCases/Planes/GetPlanesQuery.cs` — include UnidadNombre
- `backend/src/GymFlow.Infrastructure/Persistence/Configurations/UsuarioUnidadConfiguration.cs` — add PlanId FK
- `backend/src/GymFlow.Infrastructure/Persistence/Configurations/SocioConfiguration.cs` — remove Plan FK
- `backend/src/GymFlow.Infrastructure/Repositories/PlanRepository.cs` — add methods
- `backend/src/GymFlow.Infrastructure/Repositories/SocioRepository.cs` — include Plan in UsuarioUnidad
- `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs` — no code change (ApplyConfigurations handles it)
- `backend/src/GymFlow.Infrastructure/DependencyInjection.cs` — register new services
- `backend/src/GymFlow.API/Controllers/PlanesController.cs` — add POST/PUT/DELETE
- `backend/src/GymFlow.API/Controllers/SociosController.cs` — minor (controller unchanged, commands handle new DTOs)
- `backend/src/GymFlow.API/DependencyInjection.cs` — register new commands
- `backend/src/GymFlow.API/Program.cs` — update seed data
- `backend/tests/GymFlow.Application.Tests/UseCases/CreateSocioCommandTests.cs` — update for new DTOs
- `backend/tests/GymFlow.Application.Tests/UseCases/AuditLoggingTests.cs` — update for new DTOs

### New Files (Frontend)
- `frontend/src/pages/admin/PlanesPage.tsx`
- `frontend/src/pages/admin/NuevoPlanPage.tsx`
- `frontend/src/pages/admin/EditPlanPage.tsx`

### Modified Files (Frontend)
- `frontend/src/types/index.ts` — update types
- `frontend/src/services/api.ts` — add planesApi CRUD
- `frontend/src/pages/admin/NuevoSocioPage.tsx` — plan per unit
- `frontend/src/pages/admin/EditSocioPage.tsx` — plan per unit
- `frontend/src/pages/admin/SociosPage.tsx` — update display
- `frontend/src/App.tsx` — add routes
- `frontend/src/components/layout/Sidebar.tsx` — add Planes group

---

## Task 1: Domain — Add PlanId to UsuarioUnidad + Remove from Socio

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/UsuarioUnidad.cs`
- Modify: `backend/src/GymFlow.Domain/Entities/Socio.cs`
- Modify: `backend/src/GymFlow.Domain/Entities/Plan.cs`

- [ ] **Step 1: Update UsuarioUnidad — add PlanId and Plan nav property**

Replace the entire file `backend/src/GymFlow.Domain/Entities/UsuarioUnidad.cs`:

```csharp
namespace GymFlow.Domain.Entities;

public class UsuarioUnidad
{
    public Guid UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;

    public Guid UnidadId { get; private set; }
    public Unidad Unidad { get; private set; } = null!;

    public Guid? PlanId { get; private set; }
    public Plan? Plan { get; private set; }

    private UsuarioUnidad() { } // EF Core

    public UsuarioUnidad(Guid usuarioId, Guid unidadId, Guid? planId = null)
    {
        UsuarioId = usuarioId;
        UnidadId = unidadId;
        PlanId = planId;
    }

    public void AsignarPlan(Guid? planId)
    {
        PlanId = planId;
    }
}
```

- [ ] **Step 2: Remove PlanId/Plan from Socio**

In `backend/src/GymFlow.Domain/Entities/Socio.cs`:

Remove these two properties:
```csharp
public Guid? PlanId { get; private set; }
public Plan? Plan { get; private set; }
```

Remove `Guid? planId,` from the constructor parameter list and remove `PlanId = planId;` from constructor body.

Remove `Guid? planId,` from `ActualizarDatosSocio` parameter list and remove `PlanId = planId;` from its body.

The constructor should become:
```csharp
public Socio(
    string nombre,
    string apellido,
    string correo,
    string passwordHash,
    DateTime fechaAlta,
    bool consentimientoInformado,
    TipoDocumento tipoDocumento,
    string? telefono = null,
    string? documentoIdentidad = null,
    DateTime? fechaNacimiento = null)
    : base(nombre, apellido, correo, passwordHash, Rol.Socio)
{
    FechaAlta = fechaAlta;
    Telefono = telefono;
    FechaNacimiento = fechaNacimiento;

    if (!consentimientoInformado)
        throw new ArgumentException("Consentimiento informado is required (Ley 18.331).", nameof(consentimientoInformado));

    ConsentimientoInformado = true;
    ConsentimientoTimestamp = DateTime.UtcNow;

    ValidarDocumento(tipoDocumento, documentoIdentidad);
    TipoDocumento = tipoDocumento;
    DocumentoIdentidad = documentoIdentidad;
}
```

And `ActualizarDatosSocio` becomes:
```csharp
public void ActualizarDatosSocio(
    string nombre,
    string apellido,
    string correo,
    TipoDocumento tipoDocumento,
    string? telefono,
    string? documentoIdentidad,
    DateTime? fechaNacimiento)
{
    ActualizarDatosBase(nombre, apellido, correo);
    Telefono = telefono;
    FechaNacimiento = fechaNacimiento;

    ValidarDocumento(tipoDocumento, documentoIdentidad);
    TipoDocumento = tipoDocumento;
    DocumentoIdentidad = documentoIdentidad;
}
```

- [ ] **Step 3: Add Actualizar method to Plan**

In `backend/src/GymFlow.Domain/Entities/Plan.cs`, add after `Desactivar()`:

```csharp
public void Actualizar(string nombre, decimal precio, string descripcion)
{
    Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
    Precio = precio >= 0 ? precio : throw new ArgumentException("Precio must be non-negative.", nameof(precio));
    Descripcion = descripcion ?? string.Empty;
}
```

- [ ] **Step 4: Build domain project**

Run: `dotnet build backend/src/GymFlow.Domain/`
Expected: Build succeeded. (Application/Infrastructure/API will fail — expected)

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Domain/
git commit -m "feat: mover PlanId de Socio a UsuarioUnidad, agregar Plan.Actualizar()"
```

---

## Task 2: Domain Tests — Update for Removed PlanId

**Files:**
- Modify: `backend/tests/GymFlow.Domain.Tests/Entities/SocioTests.cs`

- [ ] **Step 1: Update SocioTests helper and all tests**

The `CrearSocio` helper and `ActualizarDatosSocio` calls need to remove the `planId` parameter. Read the file first, then update:

The helper becomes:
```csharp
private static Socio CrearSocio(
    TipoDocumento tipoDocumento,
    string? documentoIdentidad) =>
    new Socio(
        nombre: "Juan",
        apellido: "García",
        correo: "juan@test.com",
        passwordHash: "hash",
        fechaAlta: DateTime.UtcNow,
        consentimientoInformado: true,
        tipoDocumento: tipoDocumento,
        documentoIdentidad: documentoIdentidad);
```

Update `ActualizarDatosSocio` calls to remove `planId: null,`:
```csharp
socio.ActualizarDatosSocio(
    nombre: "Juan",
    apellido: "García",
    correo: "juan@test.com",
    tipoDocumento: TipoDocumento.CI,
    documentoIdentidad: "12345678",
    telefono: null,
    fechaNacimiento: null);
```

- [ ] **Step 2: Run domain tests**

Run: `dotnet test backend/tests/GymFlow.Domain.Tests/ -v minimal`
Expected: All tests pass.

- [ ] **Step 3: Commit**

```bash
git add backend/tests/GymFlow.Domain.Tests/
git commit -m "fix: actualizar tests de dominio para nueva firma sin PlanId"
```

---

## Task 3: Application — New DTOs + Update Existing DTOs

**Files:**
- Create: `backend/src/GymFlow.Application/DTOs/UnidadAsignacionDto.cs`
- Create: `backend/src/GymFlow.Application/DTOs/CreatePlanRequest.cs`
- Create: `backend/src/GymFlow.Application/DTOs/UpdatePlanRequest.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/CreateSocioRequest.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/UpdateSocioRequest.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/SocioDto.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/UnidadDto.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/PlanDto.cs`

- [ ] **Step 1: Create UnidadAsignacionDto**

```csharp
namespace GymFlow.Application.DTOs;

public record UnidadAsignacionDto(Guid UnidadId, Guid? PlanId);
```

- [ ] **Step 2: Create CreatePlanRequest**

```csharp
namespace GymFlow.Application.DTOs;

public record CreatePlanRequest(string Nombre, Guid UnidadId, decimal Precio, string? Descripcion);
```

- [ ] **Step 3: Create UpdatePlanRequest**

```csharp
namespace GymFlow.Application.DTOs;

public record UpdatePlanRequest(string Nombre, decimal Precio, string? Descripcion);
```

- [ ] **Step 4: Update CreateSocioRequest**

Replace entire file:
```csharp
using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record CreateSocioRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    TipoDocumento TipoDocumento,
    string? DocumentoIdentidad,
    DateTime? FechaNacimiento,
    List<UnidadAsignacionDto> Unidades,
    bool ConsentimientoInformado);
```

- [ ] **Step 5: Update UpdateSocioRequest**

Replace entire file:
```csharp
using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record UpdateSocioRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    TipoDocumento TipoDocumento,
    string? DocumentoIdentidad,
    DateTime? FechaNacimiento,
    List<UnidadAsignacionDto> Unidades);
```

- [ ] **Step 6: Update UnidadDto to include plan info**

Replace entire file:
```csharp
namespace GymFlow.Application.DTOs;

public record UnidadDto(Guid Id, string Nombre, string Direccion, Guid? PlanId = null, string? PlanNombre = null);
```

- [ ] **Step 7: Update SocioDto — remove PlanId/PlanNombre**

Replace entire file:
```csharp
using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record SocioDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    TipoDocumento TipoDocumento,
    string? DocumentoIdentidad,
    DateTime? FechaNacimiento,
    DateTime FechaAlta,
    bool EstaActivo,
    List<UnidadDto> Unidades);
```

- [ ] **Step 8: Update PlanDto — add EstaActivo and UnidadNombre**

Replace entire file:
```csharp
namespace GymFlow.Application.DTOs;

public record PlanDto(Guid Id, string Nombre, decimal Precio, string Descripcion, Guid UnidadId, string UnidadNombre, bool EstaActivo);
```

- [ ] **Step 9: Build Application project**

Run: `dotnet build backend/src/GymFlow.Application/`
Expected: May fail (commands reference old DTOs — that's expected, fixed next task).

- [ ] **Step 10: Commit**

```bash
git add backend/src/GymFlow.Application/DTOs/
git commit -m "feat: actualizar DTOs para plan por unidad (RF-22)"
```

---

## Task 4: Application — Update Socio Commands for New DTOs

**Files:**
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/UpdateSocioCommand.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/GetSocioByIdQuery.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/GetSociosQuery.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/ReactivateSocioCommand.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/DeleteSocioCommand.cs`
- Modify: `backend/src/GymFlow.Application/Interfaces/IPlanRepository.cs`

- [ ] **Step 1: Update IPlanRepository — add write methods**

Replace entire file:
```csharp
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IPlanRepository
{
    Task<IEnumerable<Plan>> GetAllAsync(bool includeInactive = false);
    Task<Plan?> GetByIdAsync(Guid id);
    Task<IEnumerable<Plan>> GetByUnidadIdAsync(Guid unidadId);
    Task<bool> ExisteSocioConPlanAsync(Guid planId);
    Task AddAsync(Plan plan);
    Task SaveChangesAsync();
}
```

- [ ] **Step 2: Update CreateSocioCommand**

Read the file first. The key changes:
- Remove `_planRepository` (no longer validate global PlanId)
- Change how unidades are assigned (use `UnidadAsignacionDto` with `PlanId`)
- Validate plan belongs to the correct unit
- Update constructor to keep `IPlanRepository` (still needed for plan-per-unit validation)

Replace the entire file with:
```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Socios;

public class CreateSocioCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public CreateSocioCommand(
        ISocioRepository socioRepository,
        IUnidadRepository unidadRepository,
        IPlanRepository planRepository,
        IAuditLogger auditLogger)
    {
        _socioRepository = socioRepository;
        _unidadRepository = unidadRepository;
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task<SocioDto> ExecuteAsync(CreateSocioRequest request, Guid usuarioId, string usuarioNombre)
    {
        if (request.DocumentoIdentidad != null && await _socioRepository.ExisteCedulaAsync(request.DocumentoIdentidad))
            throw new InvalidOperationException("El número de cédula ya está registrado");

        if (await _socioRepository.ExisteCorreoAsync(request.Correo))
            throw new InvalidOperationException("El correo ingresado ya está registrado.");

        var unidades = request.Unidades?.Distinct().ToList() ?? [];
        if (unidades.Count == 0)
            throw new ArgumentException("Debe asignar al menos una unidad.");

        // Validate unidades exist and plans belong to correct units
        foreach (var asignacion in unidades)
        {
            var unidad = await _unidadRepository.GetByIdAsync(asignacion.UnidadId);
            if (unidad == null)
                throw new ArgumentException($"La unidad con ID {asignacion.UnidadId} no existe.");

            if (asignacion.PlanId.HasValue)
            {
                var plan = await _planRepository.GetByIdAsync(asignacion.PlanId.Value);
                if (plan == null || !plan.EstaActivo)
                    throw new ArgumentException("El plan seleccionado no existe o no está activo.");
                if (plan.UnidadId != asignacion.UnidadId)
                    throw new ArgumentException($"El plan seleccionado no pertenece a la unidad {unidad.Nombre}.");
            }
        }

        var socio = new Socio(
            nombre: request.Nombre,
            apellido: request.Apellido,
            correo: request.Correo,
            passwordHash: "PENDING_OAUTH",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: request.ConsentimientoInformado,
            tipoDocumento: request.TipoDocumento,
            telefono: request.Telefono,
            documentoIdentidad: request.DocumentoIdentidad,
            fechaNacimiento: request.FechaNacimiento.HasValue
                ? DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc)
                : null);

        foreach (var asignacion in unidades)
        {
            socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, asignacion.UnidadId, asignacion.PlanId));
        }

        await _socioRepository.AddAsync(socio);
        await _socioRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId,
            usuarioNombre,
            TipoAccionAuditoria.Creacion,
            "Socio",
            socio.Id,
            $"Se registró al socio {request.Nombre} {request.Apellido}");

        var saved = await _socioRepository.GetByIdAsync(socio.Id);
        return MapToDto(saved!);
    }

    internal static SocioDto MapToDto(Socio socio)
    {
        return new SocioDto(
            Id: socio.Id,
            Nombre: socio.Nombre,
            Apellido: socio.Apellido,
            Correo: socio.Correo,
            Telefono: socio.Telefono,
            TipoDocumento: socio.TipoDocumento,
            DocumentoIdentidad: socio.DocumentoIdentidad,
            FechaNacimiento: socio.FechaNacimiento,
            FechaAlta: socio.FechaAlta,
            EstaActivo: socio.EstaActivo,
            Unidades: socio.UnidadesAsignadas
                .Select(uu => new UnidadDto(
                    uu.UnidadId,
                    uu.Unidad?.Nombre ?? "",
                    uu.Unidad?.Direccion ?? "",
                    uu.PlanId,
                    uu.Plan?.Nombre))
                .ToList());
    }
}
```

- [ ] **Step 3: Update UpdateSocioCommand**

Replace the entire file with:
```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Socios;

public class UpdateSocioCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public UpdateSocioCommand(
        ISocioRepository socioRepository,
        IUnidadRepository unidadRepository,
        IPlanRepository planRepository,
        IAuditLogger auditLogger)
    {
        _socioRepository = socioRepository;
        _unidadRepository = unidadRepository;
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task<SocioDto> ExecuteAsync(Guid id, UpdateSocioRequest request, Guid usuarioId, string usuarioNombre)
    {
        var socio = await _socioRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el socio con ID {id}.");

        if (!string.Equals(socio.Correo, request.Correo, StringComparison.OrdinalIgnoreCase))
        {
            if (await _socioRepository.ExisteCorreoAsync(request.Correo))
                throw new InvalidOperationException("El correo ingresado ya está registrado.");
        }

        var unidades = request.Unidades?.Distinct().ToList() ?? [];
        foreach (var asignacion in unidades)
        {
            var unidad = await _unidadRepository.GetByIdAsync(asignacion.UnidadId);
            if (unidad == null)
                throw new ArgumentException($"La unidad con ID {asignacion.UnidadId} no existe.");

            if (asignacion.PlanId.HasValue)
            {
                var plan = await _planRepository.GetByIdAsync(asignacion.PlanId.Value);
                if (plan == null || !plan.EstaActivo)
                    throw new ArgumentException("El plan seleccionado no existe o no está activo.");
                if (plan.UnidadId != asignacion.UnidadId)
                    throw new ArgumentException($"El plan seleccionado no pertenece a la unidad {unidad.Nombre}.");
            }
        }

        // Capture old values for audit
        var cambios = new Dictionary<string, object?>();
        if (socio.Nombre != request.Nombre) cambios["Nombre"] = new { anterior = socio.Nombre, nuevo = request.Nombre };
        if (socio.Apellido != request.Apellido) cambios["Apellido"] = new { anterior = socio.Apellido, nuevo = request.Apellido };
        if (socio.Correo != request.Correo) cambios["Correo"] = new { anterior = socio.Correo, nuevo = request.Correo };
        if (socio.Telefono != request.Telefono) cambios["Telefono"] = new { anterior = socio.Telefono, nuevo = request.Telefono };
        if (socio.TipoDocumento != request.TipoDocumento) cambios["TipoDocumento"] = new { anterior = socio.TipoDocumento.ToString(), nuevo = request.TipoDocumento.ToString() };
        if (socio.DocumentoIdentidad != request.DocumentoIdentidad) cambios["DocumentoIdentidad"] = new { anterior = socio.DocumentoIdentidad, nuevo = request.DocumentoIdentidad };

        socio.ActualizarDatosSocio(
            nombre: request.Nombre,
            apellido: request.Apellido,
            correo: request.Correo,
            tipoDocumento: request.TipoDocumento,
            telefono: request.Telefono,
            documentoIdentidad: request.DocumentoIdentidad,
            fechaNacimiento: request.FechaNacimiento.HasValue
                ? DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc)
                : null);

        socio.UnidadesAsignadas.Clear();
        foreach (var asignacion in unidades)
        {
            socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, asignacion.UnidadId, asignacion.PlanId));
        }

        await _socioRepository.SaveChangesAsync();

        string? detallesJson = cambios.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(cambios)
            : null;

        await _auditLogger.LogAsync(
            usuarioId,
            usuarioNombre,
            TipoAccionAuditoria.Modificacion,
            "Socio",
            id,
            $"Se modificaron los datos del socio {request.Nombre} {request.Apellido}",
            detallesJson);

        var updated = await _socioRepository.GetByIdAsync(id);
        return CreateSocioCommand.MapToDto(updated!);
    }
}
```

- [ ] **Step 4: Update GetSocioByIdQuery, GetSociosQuery, ReactivateSocioCommand, DeleteSocioCommand**

For `GetSocioByIdQuery.cs` — update `MapToDto` to use new SocioDto (no PlanId/PlanNombre, Unidades has plan info):
```csharp
private static SocioDto MapToDto(Socio socio) => CreateSocioCommand.MapToDto(socio);
```

For `GetSociosQuery.cs` — same change:
```csharp
private static SocioDto MapToDto(Socio socio) => CreateSocioCommand.MapToDto(socio);
```

For `ReactivateSocioCommand.cs` — same change:
```csharp
private static SocioDto MapToDto(Socio socio) => CreateSocioCommand.MapToDto(socio);
```

For `DeleteSocioCommand.cs` — no DTO changes needed (returns void).

- [ ] **Step 5: Update GetPlanesQuery to include UnidadNombre**

Replace entire file:
```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Planes;

public class GetPlanesQuery
{
    private readonly IPlanRepository _repository;

    public GetPlanesQuery(IPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PlanDto>> ExecuteAsync(Guid? unidadId = null, bool includeInactive = false)
    {
        var planes = unidadId.HasValue
            ? await _repository.GetByUnidadIdAsync(unidadId.Value)
            : await _repository.GetAllAsync(includeInactive);

        return planes.Select(p => new PlanDto(p.Id, p.Nombre, p.Precio, p.Descripcion, p.UnidadId, p.Unidad?.Nombre ?? "", p.EstaActivo));
    }
}
```

- [ ] **Step 6: Build Application project**

Run: `dotnet build backend/src/GymFlow.Application/`
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GymFlow.Application/
git commit -m "feat: actualizar comandos y queries de socio para plan por unidad"
```

---

## Task 5: Application — Plan CRUD Commands

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Planes/CreatePlanCommand.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Planes/UpdatePlanCommand.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Planes/DeletePlanCommand.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Planes/GetPlanByIdQuery.cs`

- [ ] **Step 1: Create GetPlanByIdQuery**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Planes;

public class GetPlanByIdQuery
{
    private readonly IPlanRepository _repository;

    public GetPlanByIdQuery(IPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlanDto> ExecuteAsync(Guid id)
    {
        var plan = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el plan con ID {id}.");

        return new PlanDto(plan.Id, plan.Nombre, plan.Precio, plan.Descripcion, plan.UnidadId, plan.Unidad?.Nombre ?? "", plan.EstaActivo);
    }
}
```

- [ ] **Step 2: Create CreatePlanCommand**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Planes;

public class CreatePlanCommand
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly IAuditLogger _auditLogger;

    public CreatePlanCommand(IPlanRepository planRepository, IUnidadRepository unidadRepository, IAuditLogger auditLogger)
    {
        _planRepository = planRepository;
        _unidadRepository = unidadRepository;
        _auditLogger = auditLogger;
    }

    public async Task<PlanDto> ExecuteAsync(CreatePlanRequest request, Guid usuarioId, string usuarioNombre)
    {
        var unidad = await _unidadRepository.GetByIdAsync(request.UnidadId)
            ?? throw new ArgumentException("La unidad seleccionada no existe.");

        var plan = new Plan(request.Nombre, request.Precio, request.Descripcion ?? "", request.UnidadId);

        await _planRepository.AddAsync(plan);
        await _planRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Plan", plan.Id,
            $"Se creó el plan {request.Nombre} para {unidad.Nombre}");

        return new PlanDto(plan.Id, plan.Nombre, plan.Precio, plan.Descripcion, plan.UnidadId, unidad.Nombre, plan.EstaActivo);
    }
}
```

- [ ] **Step 3: Create UpdatePlanCommand**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Planes;

public class UpdatePlanCommand
{
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public UpdatePlanCommand(IPlanRepository planRepository, IAuditLogger auditLogger)
    {
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task<PlanDto> ExecuteAsync(Guid id, UpdatePlanRequest request, Guid usuarioId, string usuarioNombre)
    {
        var plan = await _planRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el plan con ID {id}.");

        if (!plan.EstaActivo)
            throw new InvalidOperationException("No se puede editar un plan dado de baja.");

        plan.Actualizar(request.Nombre, request.Precio, request.Descripcion ?? "");

        await _planRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Plan", id,
            $"Se modificó el plan {request.Nombre}");

        return new PlanDto(plan.Id, plan.Nombre, plan.Precio, plan.Descripcion, plan.UnidadId, plan.Unidad?.Nombre ?? "", plan.EstaActivo);
    }
}
```

- [ ] **Step 4: Create DeletePlanCommand**

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Planes;

public class DeletePlanCommand
{
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public DeletePlanCommand(IPlanRepository planRepository, IAuditLogger auditLogger)
    {
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var plan = await _planRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el plan con ID {id}.");

        if (!plan.EstaActivo)
            throw new InvalidOperationException("El plan ya está dado de baja.");

        if (await _planRepository.ExisteSocioConPlanAsync(id))
            throw new InvalidOperationException("El plan tiene socios asignados. Reasígnelos antes de darlo de baja.");

        plan.Desactivar();
        await _planRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Plan", id,
            $"Se dio de baja el plan {plan.Nombre}");
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build backend/src/GymFlow.Application/`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GymFlow.Application/UseCases/Planes/ backend/src/GymFlow.Application/Interfaces/IPlanRepository.cs
git commit -m "feat: agregar CRUD commands para planes (RF-22)"
```

---

## Task 6: Infrastructure — Update EF Config, Repositories, DI

**Files:**
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/UsuarioUnidadConfiguration.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/SocioConfiguration.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Repositories/PlanRepository.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Repositories/SocioRepository.cs`

- [ ] **Step 1: Update UsuarioUnidadConfiguration — add PlanId FK**

Replace entire file:
```csharp
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class UsuarioUnidadConfiguration : IEntityTypeConfiguration<UsuarioUnidad>
{
    public void Configure(EntityTypeBuilder<UsuarioUnidad> builder)
    {
        builder.ToTable("UsuarioUnidades");
        builder.HasKey(uu => new { uu.UsuarioId, uu.UnidadId });

        builder.HasOne(uu => uu.Unidad)
            .WithMany()
            .HasForeignKey(uu => uu.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(uu => uu.Plan)
            .WithMany()
            .HasForeignKey(uu => uu.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

- [ ] **Step 2: Update SocioConfiguration — remove Plan FK**

Replace entire file:
```csharp
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class SocioConfiguration : IEntityTypeConfiguration<Socio>
{
    public void Configure(EntityTypeBuilder<Socio> builder)
    {
        builder.Property(s => s.FechaAlta).IsRequired();
        builder.Property(s => s.ConsentimientoInformado).IsRequired();
        builder.Property(s => s.ConsentimientoTimestamp);
        builder.Property(s => s.Telefono).HasMaxLength(50);
        builder.Property(s => s.DocumentoIdentidad).HasMaxLength(50);
        builder.Property(s => s.FechaNacimiento);
        builder.Property(s => s.MotivoBaja).HasMaxLength(500);
    }
}
```

- [ ] **Step 3: Update PlanRepository — add write methods + include Unidad**

Replace entire file:
```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly GymFlowDbContext _context;

    public PlanRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Plan>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Planes
            .Include(p => p.Unidad)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.EstaActivo);

        return await query.OrderBy(p => p.Nombre).ToListAsync();
    }

    public async Task<Plan?> GetByIdAsync(Guid id)
    {
        return await _context.Planes
            .Include(p => p.Unidad)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Plan>> GetByUnidadIdAsync(Guid unidadId)
    {
        return await _context.Planes
            .Include(p => p.Unidad)
            .Where(p => p.UnidadId == unidadId && p.EstaActivo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteSocioConPlanAsync(Guid planId)
    {
        return await _context.UsuarioUnidades.AnyAsync(uu => uu.PlanId == planId);
    }

    public async Task AddAsync(Plan plan)
    {
        await _context.Planes.AddAsync(plan);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 4: Update SocioRepository — include Plan in UsuarioUnidad**

In `SocioRepository.cs`, update all queries that use `.Include(s => s.UnidadesAsignadas).ThenInclude(uu => uu.Unidad)` to also include Plan:

```csharp
.Include(s => s.UnidadesAsignadas)
    .ThenInclude(uu => uu.Unidad)
.Include(s => s.UnidadesAsignadas)
    .ThenInclude(uu => uu.Plan)
```

Apply this to `GetAllAsync`, `GetByIdAsync`, and `SearchAsync`.

- [ ] **Step 5: Build Infrastructure**

Run: `dotnet build backend/src/GymFlow.Infrastructure/`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/
git commit -m "feat: actualizar EF configs y repositorios para plan por unidad"
```

---

## Task 7: API — Update Controllers + DI + Seed Data

**Files:**
- Modify: `backend/src/GymFlow.API/Controllers/PlanesController.cs`
- Modify: `backend/src/GymFlow.API/DependencyInjection.cs`
- Modify: `backend/src/GymFlow.API/Program.cs`

- [ ] **Step 1: Update PlanesController — add CRUD endpoints**

Replace entire file:
```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Planes;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanesController : ControllerBase
{
    private readonly GetPlanesQuery _getPlanesQuery;
    private readonly GetPlanByIdQuery _getPlanByIdQuery;
    private readonly CreatePlanCommand _createPlanCommand;
    private readonly UpdatePlanCommand _updatePlanCommand;
    private readonly DeletePlanCommand _deletePlanCommand;

    public PlanesController(
        GetPlanesQuery getPlanesQuery,
        GetPlanByIdQuery getPlanByIdQuery,
        CreatePlanCommand createPlanCommand,
        UpdatePlanCommand updatePlanCommand,
        DeletePlanCommand deletePlanCommand)
    {
        _getPlanesQuery = getPlanesQuery;
        _getPlanByIdQuery = getPlanByIdQuery;
        _createPlanCommand = createPlanCommand;
        _updatePlanCommand = updatePlanCommand;
        _deletePlanCommand = deletePlanCommand;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanDto>>> GetAll(
        [FromQuery] Guid? unidadId,
        [FromQuery] bool includeInactive = false)
    {
        var planes = await _getPlanesQuery.ExecuteAsync(unidadId, includeInactive);
        return Ok(planes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlanDto>> GetById(Guid id)
    {
        try
        {
            var plan = await _getPlanByIdQuery.ExecuteAsync(id);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<PlanDto>> Create([FromBody] CreatePlanRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var plan = await _createPlanCommand.ExecuteAsync(request, userId, userName);
            return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlanDto>> Update(Guid id, [FromBody] UpdatePlanRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var plan = await _updatePlanCommand.ExecuteAsync(id, request, userId, userName);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _deletePlanCommand.ExecuteAsync(id, userId, userName);
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

- [ ] **Step 2: Register new commands in API DI**

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<CreatePlanCommand>();
services.AddScoped<UpdatePlanCommand>();
services.AddScoped<DeletePlanCommand>();
services.AddScoped<GetPlanByIdQuery>();
```

Add using: `using GymFlow.Application.UseCases.Planes;` (may already exist partially)

- [ ] **Step 3: Build entire solution**

Run: `dotnet build backend/GymFlow.sln`
Expected: Build succeeds. Tests may fail (will fix next).

- [ ] **Step 4: Commit**

```bash
git add backend/src/GymFlow.API/
git commit -m "feat: agregar endpoints CRUD de planes y actualizar DI"
```

---

## Task 8: EF Migration + Fix Tests

**Files:**
- Migration files (auto-generated)
- Modify: `backend/tests/GymFlow.Application.Tests/UseCases/CreateSocioCommandTests.cs`
- Modify: `backend/tests/GymFlow.Application.Tests/UseCases/AuditLoggingTests.cs`

- [ ] **Step 1: Generate migration**

```bash
cd backend && dotnet ef migrations add MovePlanIdToUsuarioUnidad --project src/GymFlow.Infrastructure --startup-project src/GymFlow.API
```

- [ ] **Step 2: Fix CreateSocioCommandTests**

Update all `CreateSocioRequest` constructions to use new format. The old format used `PlanId` and `UnidadIds`. The new format uses `Unidades` (List of UnidadAsignacionDto).

Read the file first. Replace every `CreateSocioRequest(...)` call. Example:

Old:
```csharp
var request = new CreateSocioRequest(
    Nombre: "Juan", Apellido: "García", Correo: "juan@test.com",
    Telefono: null, TipoDocumento: TipoDocumento.CI, DocumentoIdentidad: "54321163",
    FechaNacimiento: null, PlanId: null, UnidadIds: [unidadId], ConsentimientoInformado: true);
```

New:
```csharp
var request = new CreateSocioRequest(
    Nombre: "Juan", Apellido: "García", Correo: "juan@test.com",
    Telefono: null, TipoDocumento: TipoDocumento.CI, DocumentoIdentidad: "54321163",
    FechaNacimiento: null, Unidades: [new UnidadAsignacionDto(unidadId, null)], ConsentimientoInformado: true);
```

Apply this pattern to ALL test methods in the file. Also update the `SocioFake` helper to remove `planId` param:

```csharp
private static Socio SocioFake(TipoDocumento tipo, string? doc) =>
    new("Juan", "García", "juan@test.com", "PENDING_OAUTH",
        DateTime.UtcNow, true, tipo, null, doc, null);
```

Add using: `using GymFlow.Application.DTOs;` (for UnidadAsignacionDto)

- [ ] **Step 3: Fix AuditLoggingTests**

Same pattern — update `CreateSocioRequest` and `SocioFake` calls:

```csharp
private static Socio SocioFake() =>
    new("Juan", "García", "juan@test.com", "PENDING_OAUTH",
        DateTime.UtcNow, true, TipoDocumento.Otro);
```

Update request:
```csharp
var request = new CreateSocioRequest(
    "Juan", "García", "juan@test.com", null,
    TipoDocumento.Otro, null, null, [new UnidadAsignacionDto(unidadId, null)], true);
```

- [ ] **Step 4: Run ALL tests**

Run: `dotnet test backend/GymFlow.sln -v minimal`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/Persistence/Migrations/ backend/tests/
git commit -m "chore: agregar migración MovePlanIdToUsuarioUnidad y actualizar tests"
```

---

## Task 9: Frontend — Update Types + API Service

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/services/api.ts`

- [ ] **Step 1: Update types**

Read the file first. Make these changes:

Update `Unidad` to optionally include plan info when used in socio context:
```typescript
export interface UnidadConPlan {
  id: string
  nombre: string
  direccion: string
  planId: string | null
  planNombre: string | null
}
```

Update `Socio` — replace `planId`/`planNombre` and `unidades: Unidad[]` with:
```typescript
export interface Socio {
  id: string
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  fechaAlta: string
  estaActivo: boolean
  unidades: UnidadConPlan[]
}
```

Update `CreateSocioRequest` — replace `planId` and `unidadIds` with:
```typescript
export interface CreateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  unidades: { unidadId: string; planId: string | null }[]
  consentimientoInformado: boolean
}
```

Update `UpdateSocioRequest` — same change:
```typescript
export interface UpdateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  unidades: { unidadId: string; planId: string | null }[]
}
```

Update `Plan` to include new fields:
```typescript
export interface Plan {
  id: string
  nombre: string
  precio: number
  descripcion: string
  unidadId: string
  unidadNombre: string
  estaActivo: boolean
}
```

Add new types:
```typescript
export interface CreatePlanRequest {
  nombre: string
  unidadId: string
  precio: number
  descripcion: string | null
}

export interface UpdatePlanRequest {
  nombre: string
  precio: number
  descripcion: string | null
}
```

- [ ] **Step 2: Update API service**

Add to the `planesApi` object:
```typescript
export const planesApi = {
  getAll: async (unidadId?: string, includeInactive?: boolean): Promise<Plan[]> => {
    const params: Record<string, string> = {}
    if (unidadId) params.unidadId = unidadId
    if (includeInactive) params.includeInactive = 'true'
    const { data } = await api.get<Plan[]>('/planes', { params })
    return data
  },

  getById: async (id: string): Promise<Plan> => {
    const { data } = await api.get<Plan>(`/planes/${id}`)
    return data
  },

  create: async (request: CreatePlanRequest): Promise<Plan> => {
    const { data } = await api.post<Plan>('/planes', request)
    return data
  },

  update: async (id: string, request: UpdatePlanRequest): Promise<Plan> => {
    const { data } = await api.put<Plan>(`/planes/${id}`, request)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/planes/${id}`)
  },
}
```

Update the import to include new types:
```typescript
import type { Unidad, Socio, CreateSocioRequest, UpdateSocioRequest, DeleteSocioRequest, Plan, AuditoriaEntry, CreatePlanRequest, UpdatePlanRequest } from '@/types'
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/types/index.ts frontend/src/services/api.ts
git commit -m "feat: actualizar tipos y API service para planes CRUD y plan por unidad"
```

---

## Task 10: Frontend — Plan Management Pages

**Files:**
- Create: `frontend/src/pages/admin/PlanesPage.tsx`
- Create: `frontend/src/pages/admin/NuevoPlanPage.tsx`
- Create: `frontend/src/pages/admin/EditPlanPage.tsx`

These are new pages following the same patterns as existing socio pages. Due to size, implement them as subagent tasks following the existing `SociosPage`, `NuevoSocioPage`, `EditSocioPage` patterns exactly.

The key details for each page are described in the spec. The subagent should read the existing pages and follow the same component library (shadcn/ui), styling (Tailwind), and data-fetching (TanStack React Query) patterns.

- [ ] **Step 1: Create PlanesPage.tsx** — table with columns (Nombre, Unidad, Precio, Estado), filter by Unidad, Nuevo Plan button, edit/delete actions
- [ ] **Step 2: Create NuevoPlanPage.tsx** — form with Nombre, Unidad dropdown, Precio, Descripcion
- [ ] **Step 3: Create EditPlanPage.tsx** — same form, Unidad disabled, precargado
- [ ] **Step 4: Commit**

```bash
git add frontend/src/pages/admin/PlanesPage.tsx frontend/src/pages/admin/NuevoPlanPage.tsx frontend/src/pages/admin/EditPlanPage.tsx
git commit -m "feat: agregar páginas de gestión de planes (CRUD)"
```

---

## Task 11: Frontend — Update Socio Forms (Plan per Unit)

**Files:**
- Modify: `frontend/src/pages/admin/NuevoSocioPage.tsx`
- Modify: `frontend/src/pages/admin/EditSocioPage.tsx`
- Modify: `frontend/src/pages/admin/SociosPage.tsx`

The key change: when a unit checkbox is checked, a plan dropdown appears below it showing only plans for that unit. The form state changes from `{ planId, unidadIds }` to `{ unidades: [{ unidadId, planId }] }`.

- [ ] **Step 1: Update NuevoSocioPage** — replace `planId`/`unidadIds` with `unidades` array, show plan dropdown per selected unit
- [ ] **Step 2: Update EditSocioPage** — same pattern, pre-load plan per unit from socio data
- [ ] **Step 3: Update SociosPage** — display unit+plan badges instead of separate Plan column
- [ ] **Step 4: Build frontend**

Run: `cd frontend && npm run build`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/admin/NuevoSocioPage.tsx frontend/src/pages/admin/EditSocioPage.tsx frontend/src/pages/admin/SociosPage.tsx
git commit -m "feat: actualizar formularios de socio para plan por unidad"
```

---

## Task 12: Frontend — Routing + Sidebar

**Files:**
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Add routes in App.tsx**

Add imports:
```typescript
import PlanesPage from '@/pages/admin/PlanesPage'
import NuevoPlanPage from '@/pages/admin/NuevoPlanPage'
import EditPlanPage from '@/pages/admin/EditPlanPage'
```

Add routes inside admin group:
```tsx
<Route path="planes" element={<PlanesPage />} />
<Route path="planes/nuevo" element={<NuevoPlanPage />} />
<Route path="planes/:id/editar" element={<EditPlanPage />} />
```

- [ ] **Step 2: Add Planes group in Sidebar.tsx**

Add `CreditCard` to lucide-react imports. Add new navigation group:
```typescript
{
  label: 'Planes',
  icon: <CreditCard className="h-5 w-5" />,
  items: [
    { label: 'Nuevo Plan', path: '/admin/planes/nuevo', icon: <CreditCard className="h-4 w-4" /> },
    { label: 'Lista de Planes', path: '/admin/planes', icon: <CreditCard className="h-4 w-4" /> },
  ],
},
```

- [ ] **Step 3: Build and verify**

Run: `cd frontend && npm run build`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/App.tsx frontend/src/components/layout/Sidebar.tsx
git commit -m "feat: agregar rutas y sidebar para gestión de planes"
```

---

## Task 13: Full Build + All Tests + Push

- [ ] **Step 1: Build backend**

Run: `dotnet build backend/GymFlow.sln`
Expected: 0 errors, 0 warnings.

- [ ] **Step 2: Run all tests**

Run: `dotnet test backend/GymFlow.sln -v minimal`
Expected: All tests pass.

- [ ] **Step 3: Build frontend**

Run: `cd frontend && npm run build`
Expected: 0 errors.

- [ ] **Step 4: Push**

```bash
git push origin feature/RF_22
```
