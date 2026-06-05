# Inscripción por Horario — Plan de Implementación

#plan

---
tags:
  - plan
  - iteracion
requerimiento: RF-10, RF-11, RN-09
---

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cambiar el modelo de inscripción de "por clase" a "por horario", para que un socio pueda inscribirse a una clase en un horario específico (ej: Yoga los lunes de 8 a 10) en vez de inscribirse a la clase completa.

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] — RF-10 (Inscripción a clase), RF-11 (Ver mis clases), RF-09 (Gestionar horarios), RN-09 (no duplicar inscripción al mismo horario), CU-02 (Inscripción a Clase)

**Spec relacionada:** [[spec-it4-inscripciones-empleados-horarios]] — este plan complementa IT4 con un cambio de diseño al modelo de inscripción.

**Plan base:** [[plan-it4-inscripciones-empleados-horarios]] — las tasks de IT4 se implementaron con inscripción por clase; este plan migra a inscripción por horario.

**Spec de diseño:** [[spec-inscripcion-por-horario]]

**Architecture:** La FK de `InscripcionClase` cambia de `ClaseId` a `HorarioClaseId`. El cupo (`CapacidadMaxima`) se mantiene en `Clase` (es la capacidad de la actividad, compartida por todos sus horarios). Los conteos de inscripciones activas pasan a agruparse por `HorarioClaseId` en vez de por `ClaseId`. El frontend elimina `CatalogoClasesPage` y unifica la experiencia en `HorariosPortalPage`.

**Tech Stack:** C# / ASP.NET Core / EF Core / PostgreSQL (backend), React + TypeScript + Tailwind (frontend), xUnit + Moq (tests)

---

### Task 1: Migrar entidad `InscripcionClase` — FK de `ClaseId` a `HorarioClaseId`

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/InscripcionClase.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/InscripcionClaseConfiguration.cs`
- Test: `backend/tests/GymFlow.Domain.Tests/Entities/InscripcionClaseTests.cs`

- [ ] **Step 1: Escribir tests actualizados para el constructor con `horarioClaseId`**

```csharp
// InscripcionClaseTests.cs — reemplazar los tests existentes
using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests.Entities;

public class InscripcionClaseTests
{
    [Fact]
    public void Constructor_PorDefecto_NoEsListaEspera()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(i.EsListaEspera);
        Assert.True(i.EstaActiva);
    }

    [Fact]
    public void Constructor_AsignaHorarioClaseId()
    {
        var horarioId = Guid.NewGuid();
        var i = new InscripcionClase(horarioId, Guid.NewGuid());
        Assert.Equal(horarioId, i.HorarioClaseId);
    }

    [Fact]
    public void Constructor_ConListaEspera_MarcaFlag()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid(), esListaEspera: true);
        Assert.True(i.EsListaEspera);
    }

    [Fact]
    public void PromoverDeListaEspera_EnListaEspera_QuitaFlag()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid(), esListaEspera: true);
        i.PromoverDeListaEspera();
        Assert.False(i.EsListaEspera);
    }

    [Fact]
    public void PromoverDeListaEspera_NoEnListaEspera_LanzaExcepcion()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => i.PromoverDeListaEspera());
    }
}
```

- [ ] **Step 2: Correr tests para verificar que fallan**

Run: `dotnet test backend/tests/GymFlow.Domain.Tests --filter "InscripcionClaseTests" -v n`
Expected: FAIL — `InscripcionClase` no tiene `HorarioClaseId` todavía.

- [ ] **Step 3: Modificar la entidad `InscripcionClase`**

Reemplazar `ClaseId`/`Clase` por `HorarioClaseId`/`HorarioClase`:

```csharp
// InscripcionClase.cs
namespace GymFlow.Domain.Entities;

public class InscripcionClase
{
    public Guid Id { get; private set; }
    public Guid HorarioClaseId { get; private set; }
    public HorarioClase HorarioClase { get; private set; } = null!;
    public Guid SocioId { get; private set; }
    public Socio Socio { get; private set; } = null!;
    public DateTime FechaInscripcion { get; private set; }
    public bool EstaActiva { get; private set; } = true;
    public bool EsListaEspera { get; private set; }

    private InscripcionClase() { } // EF Core

    public InscripcionClase(Guid horarioClaseId, Guid socioId, bool esListaEspera = false)
    {
        Id = Guid.NewGuid();
        HorarioClaseId = horarioClaseId;
        SocioId = socioId;
        FechaInscripcion = DateTime.UtcNow;
        EstaActiva = true;
        EsListaEspera = esListaEspera;
    }

    public void Cancelar()
    {
        EstaActiva = false;
    }

    public void PromoverDeListaEspera()
    {
        if (!EsListaEspera)
            throw new InvalidOperationException("La inscripción no está en lista de espera.");
        EsListaEspera = false;
    }
}
```

- [ ] **Step 4: Actualizar `InscripcionClaseConfiguration`**

```csharp
// InscripcionClaseConfiguration.cs
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class InscripcionClaseConfiguration : IEntityTypeConfiguration<InscripcionClase>
{
    public void Configure(EntityTypeBuilder<InscripcionClase> builder)
    {
        builder.ToTable("InscripcionesClase");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FechaInscripcion).IsRequired();
        builder.Property(i => i.EstaActiva).IsRequired();
        builder.Property(i => i.EsListaEspera).IsRequired().HasDefaultValue(false);

        builder.HasOne(i => i.HorarioClase)
            .WithMany()
            .HasForeignKey(i => i.HorarioClaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Socio)
            .WithMany()
            .HasForeignKey(i => i.SocioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 5: Quitar la colección `Inscripciones` de `Clase`**

En `backend/src/GymFlow.Domain/Entities/Clase.cs`, eliminar:

```csharp
// ELIMINAR estas líneas:
private readonly List<InscripcionClase> _inscripciones = new();
public IReadOnlyCollection<InscripcionClase> Inscripciones => _inscripciones.AsReadOnly();
```

En `backend/src/GymFlow.Infrastructure/Persistence/Configurations/ClaseConfiguration.cs`, eliminar la relación con InscripcionClase:

```csharp
// ELIMINAR esta línea (la FK ahora es de InscripcionClase → HorarioClase, no → Clase):
builder.HasMany(c => c.Inscripciones)
    .WithOne(i => i.Clase)
    .HasForeignKey(i => i.ClaseId)
    .OnDelete(DeleteBehavior.Cascade);
```

- [ ] **Step 6: Correr los tests de dominio**

Run: `dotnet test backend/tests/GymFlow.Domain.Tests --filter "InscripcionClaseTests" -v n`
Expected: PASS

- [ ] **Step 7: Commit**

```
git add backend/src/GymFlow.Domain/Entities/InscripcionClase.cs backend/src/GymFlow.Domain/Entities/Clase.cs backend/src/GymFlow.Infrastructure/Persistence/Configurations/InscripcionClaseConfiguration.cs backend/src/GymFlow.Infrastructure/Persistence/Configurations/ClaseConfiguration.cs backend/tests/GymFlow.Domain.Tests/Entities/InscripcionClaseTests.cs
git commit -m "refactor(domain): cambiar InscripcionClase FK de ClaseId a HorarioClaseId"
```

---

### Task 2: Generar migración de EF Core

**Files:**
- Create: nueva migración en `backend/src/GymFlow.Infrastructure/Persistence/Migrations/`

> **Nota:** El proyecto probablemente no compile completamente hasta que se ajusten repositorios y commands (Tasks 3-5). Si la migración falla por errores de compilación, se puede posponer hasta después de Task 5. En ese caso, hacer la migración como primer paso de Task 6.

- [ ] **Step 1: Generar la migración**

Run (desde `backend/`):
```
dotnet ef migrations add CambiarInscripcionDeClaseAHorario --project src/GymFlow.Infrastructure --startup-project src/GymFlow.API
```

La migración debe:
- Eliminar la columna `ClaseId` de la tabla `InscripcionesClase`
- Agregar columna `HorarioClaseId` (Guid, NOT NULL) con FK a `HorariosClase.Id` (CASCADE)
- Eliminar el index/FK viejo sobre `ClaseId`
- Crear index nuevo sobre `HorarioClaseId`

⚠️ **Dato existente:** Si hay inscripciones en la BD de dev, la migración necesita un paso de datos. Si la BD está vacía o se puede recrear, no hace falta. Si hay datos, agregar un SQL raw en la migración:

```csharp
// En el Up() de la migración, antes del AddColumn:
// Si existen inscripciones, asociarlas al primer horario de su clase
migrationBuilder.Sql(@"
    UPDATE ""InscripcionesClase"" i
    SET ""HorarioClaseId"" = (
        SELECT h.""Id"" FROM ""HorariosClase"" h
        WHERE h.""ClaseId"" = i.""ClaseId""
        ORDER BY h.""DiaSemana"", h.""HoraInicio""
        LIMIT 1
    )
");
```

- [ ] **Step 2: Revisar la migración generada**

Verificar que el `Up()` y `Down()` son correctos.

- [ ] **Step 3: Commit**

```
git add backend/src/GymFlow.Infrastructure/Persistence/Migrations/
git commit -m "migration: cambiar InscripcionClase FK de ClaseId a HorarioClaseId"
```

---

### Task 3: Actualizar `IInscripcionClaseRepository` y su implementación

**Files:**
- Modify: `backend/src/GymFlow.Application/Interfaces/IInscripcionClaseRepository.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Repositories/InscripcionClaseRepository.cs`

- [ ] **Step 1: Actualizar la interfaz**

Todos los métodos que recibían `claseId` ahora reciben `horarioClaseId`. Agregar `GetActivaBySocioYHorarioAsync` para verificar duplicados por horario.

```csharp
// IInscripcionClaseRepository.cs
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IInscripcionClaseRepository
{
    Task<InscripcionClase?> GetByIdAsync(Guid id);
    Task<IEnumerable<InscripcionClase>> GetBySocioIdAsync(Guid socioId);
    Task<InscripcionClase?> GetActivaBySocioYHorarioAsync(Guid socioId, Guid horarioClaseId);
    Task<int> GetInscripcionesActivasCountAsync(Guid horarioClaseId);
    Task<InscripcionClase?> GetPrimeroEnListaEsperaAsync(Guid horarioClaseId);
    Task<int> GetPosicionEnListaEsperaAsync(Guid inscripcionId);
    Task<Dictionary<Guid, int>> GetConteoActivasPorHorariosAsync(IEnumerable<Guid> horarioClaseIds);
    Task<IEnumerable<InscripcionClase>> GetActivasByHorarioClaseIdAsync(Guid horarioClaseId);
    Task AddAsync(InscripcionClase inscripcion);
    Task SaveChangesAsync();
}
```

- [ ] **Step 2: Actualizar la implementación**

```csharp
// InscripcionClaseRepository.cs
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class InscripcionClaseRepository : IInscripcionClaseRepository
{
    private readonly GymFlowDbContext _context;

    public InscripcionClaseRepository(GymFlowDbContext context) => _context = context;

    public async Task<InscripcionClase?> GetByIdAsync(Guid id)
    {
        return await _context.InscripcionesClase
            .Include(i => i.HorarioClase)
                .ThenInclude(h => h.Clase)
                    .ThenInclude(c => c.Unidad)
            .Include(i => i.Socio)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<InscripcionClase>> GetBySocioIdAsync(Guid socioId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.HorarioClase)
                .ThenInclude(h => h.Clase)
                    .ThenInclude(c => c.Unidad)
            .Where(i => i.SocioId == socioId && i.EstaActiva)
            .OrderByDescending(i => i.FechaInscripcion)
            .ToListAsync();
    }

    public async Task<InscripcionClase?> GetActivaBySocioYHorarioAsync(Guid socioId, Guid horarioClaseId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.HorarioClase)
                .ThenInclude(h => h.Clase)
                    .ThenInclude(c => c.Unidad)
            .FirstOrDefaultAsync(i => i.SocioId == socioId && i.HorarioClaseId == horarioClaseId && i.EstaActiva);
    }

    public async Task<int> GetInscripcionesActivasCountAsync(Guid horarioClaseId)
    {
        return await _context.InscripcionesClase
            .CountAsync(i => i.HorarioClaseId == horarioClaseId && i.EstaActiva && !i.EsListaEspera);
    }

    public async Task<InscripcionClase?> GetPrimeroEnListaEsperaAsync(Guid horarioClaseId) =>
        await _context.InscripcionesClase
            .Include(i => i.Socio)
            .Where(i => i.HorarioClaseId == horarioClaseId && i.EstaActiva && i.EsListaEspera)
            .OrderBy(i => i.FechaInscripcion)
            .FirstOrDefaultAsync();

    public async Task<int> GetPosicionEnListaEsperaAsync(Guid inscripcionId)
    {
        var insc = await _context.InscripcionesClase.FindAsync(inscripcionId);
        if (insc is null || !insc.EsListaEspera) return 0;
        return await _context.InscripcionesClase.CountAsync(i =>
            i.HorarioClaseId == insc.HorarioClaseId && i.EstaActiva && i.EsListaEspera &&
            i.FechaInscripcion <= insc.FechaInscripcion);
    }

    public async Task<Dictionary<Guid, int>> GetConteoActivasPorHorariosAsync(IEnumerable<Guid> horarioClaseIds)
    {
        var ids = horarioClaseIds.Distinct().ToList();
        return await _context.InscripcionesClase
            .Where(i => ids.Contains(i.HorarioClaseId) && i.EstaActiva && !i.EsListaEspera)
            .GroupBy(i => i.HorarioClaseId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    public async Task<IEnumerable<InscripcionClase>> GetActivasByHorarioClaseIdAsync(Guid horarioClaseId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.Socio)
            .Where(i => i.HorarioClaseId == horarioClaseId && i.EstaActiva)
            .ToListAsync();
    }

    public async Task AddAsync(InscripcionClase inscripcion) => await _context.InscripcionesClase.AddAsync(inscripcion);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
```

- [ ] **Step 3: Commit**

```
git add backend/src/GymFlow.Application/Interfaces/IInscripcionClaseRepository.cs backend/src/GymFlow.Infrastructure/Repositories/InscripcionClaseRepository.cs
git commit -m "refactor(repo): actualizar IInscripcionClaseRepository para usar HorarioClaseId"
```

---

### Task 4: Actualizar `IClaseRepository` — quitar métodos de inscripción por clase

**Files:**
- Modify: `backend/src/GymFlow.Application/Interfaces/IClaseRepository.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Repositories/ClaseRepository.cs`

Los métodos `GetInscripcionesActivasCountAsync(claseId)` y `GetInscripcionesActivasAsync(claseId)` ya no tienen sentido porque la inscripción no apunta a `Clase`. Los call-sites se ajustarán en tasks posteriores.

- [ ] **Step 1: Eliminar métodos de inscripción de `IClaseRepository`**

```csharp
// IClaseRepository.cs
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IClaseRepository
{
    Task<IEnumerable<Clase>> GetAllAsync(bool includeInactive = false);
    Task<Clase?> GetByIdAsync(Guid id);
    Task<IEnumerable<Clase>> GetByUnidadIdAsync(Guid unidadId, bool includeInactive = false);
    Task AddAsync(Clase clase);
    Task SaveChangesAsync();
}
```

- [ ] **Step 2: Eliminar métodos de inscripción de `ClaseRepository`**

Eliminar los métodos `GetInscripcionesActivasCountAsync` y `GetInscripcionesActivasAsync` de `ClaseRepository.cs`.

```csharp
// ClaseRepository.cs — contenido completo
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class ClaseRepository : IClaseRepository
{
    private readonly GymFlowDbContext _context;

    public ClaseRepository(GymFlowDbContext context) => _context = context;

    public async Task<IEnumerable<Clase>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Clases
            .Include(c => c.Unidad)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.EstaActivo);

        return await query.OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Clase?> GetByIdAsync(Guid id)
    {
        return await _context.Clases
            .Include(c => c.Unidad)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Clase>> GetByUnidadIdAsync(Guid unidadId, bool includeInactive = false)
    {
        var query = _context.Clases
            .Include(c => c.Unidad)
            .Where(c => c.UnidadId == unidadId);

        if (!includeInactive)
            query = query.Where(c => c.EstaActivo);

        return await query.OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task AddAsync(Clase clase) => await _context.Clases.AddAsync(clase);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
```

- [ ] **Step 3: Commit**

```
git add backend/src/GymFlow.Application/Interfaces/IClaseRepository.cs backend/src/GymFlow.Infrastructure/Repositories/ClaseRepository.cs
git commit -m "refactor(repo): quitar métodos de inscripción de IClaseRepository"
```

---

### Task 5: Actualizar DTOs, mapper e email templates

**Files:**
- Modify: `backend/src/GymFlow.Application/DTOs/InscripcionClaseDtos.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/InscripcionMapper.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/InscripcionEmailTemplates.cs`

- [ ] **Step 1: Actualizar `InscripcionClaseDtos`**

El DTO ahora expone `HorarioClaseId` en vez de `ClaseId`, y agrega `DiaSemana`, `HoraInicio`, `HoraFin` para que el frontend sepa a qué horario está inscripto. El request cambia de `ClaseId` a `HorarioClaseId`.

```csharp
// InscripcionClaseDtos.cs
using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record InscripcionClaseDto(
    Guid Id,
    Guid HorarioClaseId,
    Guid ClaseId,
    string ClaseNombre,
    string Instructor,
    Guid UnidadId,
    string UnidadNombre,
    DiaSemana DiaSemana,
    string HoraInicio,
    string HoraFin,
    string? Sala,
    int CapacidadMaxima,
    int InscripcionesActivas,
    DateTime FechaInscripcion,
    bool EnListaEspera,
    int? PosicionListaEspera);

public record InscribirSocioRequest(Guid HorarioClaseId);
```

- [ ] **Step 2: Actualizar `InscripcionMapper`**

```csharp
// InscripcionMapper.cs
using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

internal static class InscripcionMapper
{
    public static InscripcionClaseDto ToDto(InscripcionClase i, int cuposOcupados, int? posicionListaEspera = null)
    {
        var horario = i.HorarioClase;
        var clase = horario.Clase;
        return new(
            i.Id,
            i.HorarioClaseId,
            horario.ClaseId,
            clase?.Nombre ?? "",
            clase?.Instructor ?? "",
            clase?.UnidadId ?? Guid.Empty,
            clase?.Unidad?.Nombre ?? "",
            horario.DiaSemana,
            horario.HoraInicio.ToString("HH:mm"),
            horario.HoraFin.ToString("HH:mm"),
            horario.Sala,
            clase?.CapacidadMaxima ?? 0,
            cuposOcupados,
            i.FechaInscripcion,
            i.EsListaEspera,
            posicionListaEspera);
    }
}
```

- [ ] **Step 3: Actualizar `InscripcionEmailTemplates`**

Las plantillas ahora reciben `HorarioClase` para incluir día/hora en el email:

```csharp
// InscripcionEmailTemplates.cs
using System.Net;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

internal static class InscripcionEmailTemplates
{
    public static (string Asunto, string Cuerpo) Confirmacion(Socio socio, Clase clase, HorarioClase horario)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var dia = WebUtility.HtmlEncode(horario.DiaSemana.ToString());
        var hora = $"{horario.HoraInicio:HH:mm} - {horario.HoraFin:HH:mm}";
        var asunto = $"Inscripción confirmada: {claseNombre}";
        var cuerpo = $@"<html><body style='font-family:Arial,sans-serif;'>
<h2>Hola {nombre},</h2>
<p>Tu inscripción a <strong>{claseNombre}</strong> ({dia} {hora}) fue confirmada.</p>
<p>Saludos,<br/>Equipo GymFlow</p></body></html>";
        return (asunto, cuerpo);
    }

    public static (string Asunto, string Cuerpo) CupoLiberado(Socio socio, Clase clase, HorarioClase horario)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var dia = WebUtility.HtmlEncode(horario.DiaSemana.ToString());
        var hora = $"{horario.HoraInicio:HH:mm} - {horario.HoraFin:HH:mm}";
        var asunto = $"Se liberó un cupo: {claseNombre}";
        var cuerpo = $@"<html><body style='font-family:Arial,sans-serif;'>
<h2>Hola {nombre},</h2>
<p>Se liberó un cupo en <strong>{claseNombre}</strong> ({dia} {hora}) y ya quedaste inscripto.</p>
<p>Saludos,<br/>Equipo GymFlow</p></body></html>";
        return (asunto, cuerpo);
    }
}
```

- [ ] **Step 4: Commit**

```
git add backend/src/GymFlow.Application/DTOs/InscripcionClaseDtos.cs backend/src/GymFlow.Application/UseCases/Inscripciones/InscripcionMapper.cs backend/src/GymFlow.Application/UseCases/Inscripciones/InscripcionEmailTemplates.cs
git commit -m "refactor(dto): actualizar DTOs, mapper y templates para inscripción por horario"
```

---

### Task 6: Actualizar `InscribirSocioCommand` y tests

**Files:**
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/InscribirSocioCommand.cs`
- Modify: `backend/tests/GymFlow.Application.Tests/UseCases/Inscripciones/InscribirSocioCommandTests.cs`

- [ ] **Step 1: Escribir los tests actualizados**

```csharp
// InscribirSocioCommandTests.cs
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Inscripciones;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Inscripciones;

public class InscribirSocioCommandTests
{
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private InscribirSocioCommand CrearCommand() =>
        new(_inscripcionRepo.Object, _horarioRepo.Object,
            _socioRepo.Object, _emailService.Object, _auditLogger.Object);

    private static Socio CrearSocio() =>
        new(Guid.NewGuid(), "María", "López", "m@test.com", "h", DateTime.UtcNow,
            true, TipoDocumento.CI, null, "12345672", null);

    private static Clase CrearClase(int capacidad = 10) =>
        new("Spinning", "Clase de spinning", capacidad, 60, "Juan", Guid.NewGuid());

    private static HorarioClase CrearHorario(Clase clase) =>
        new(clase.Id, DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0), null);

    [Fact]
    public async Task ConCupo_InscribeNormalEnviaEmailYAudita()
    {
        var clase = CrearClase(capacidad: 10);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socio.Id, horario.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(horario.Id)).ReturnsAsync(3);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        var dto = await CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin");

        Assert.False(dto.EnListaEspera);
        Assert.Equal(horario.Id, dto.HorarioClaseId);
        _inscripcionRepo.Verify(r => r.AddAsync(It.Is<InscripcionClase>(i => i.EsListaEspera == false)), Times.Once);
        _emailService.Verify(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin",
            TipoAccionAuditoria.Creacion, "Inscripcion", It.IsAny<Guid?>(), It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task SinCupo_CreaEnListaEspera()
    {
        var clase = CrearClase(capacidad: 5);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socio.Id, horario.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(horario.Id)).ReturnsAsync(5);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _inscripcionRepo.Setup(r => r.GetPosicionEnListaEsperaAsync(It.IsAny<Guid>())).ReturnsAsync(1);

        var dto = await CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin");

        Assert.True(dto.EnListaEspera);
        _inscripcionRepo.Verify(r => r.AddAsync(It.Is<InscripcionClase>(i => i.EsListaEspera == true)), Times.Once);
    }

    [Fact]
    public async Task HorarioNoExiste_LanzaKeyNotFound()
    {
        _horarioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((HorarioClase?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task YaInscripto_LanzaInvalidOperation()
    {
        var clase = CrearClase();
        var horario = CrearHorario(clase);
        var socioId = Guid.NewGuid();
        var inscripcionExistente = new InscripcionClase(horario.Id, socioId);

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socioId, horario.Id)).ReturnsAsync(inscripcionExistente);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(socioId, horario.Id, Guid.NewGuid(), "Admin"));
    }
}
```

- [ ] **Step 2: Correr tests para verificar que fallan**

Run: `dotnet test backend/tests/GymFlow.Application.Tests --filter "InscribirSocioCommandTests" -v n`
Expected: FAIL — el command todavía usa `claseId`.

- [ ] **Step 3: Actualizar `InscribirSocioCommand`**

```csharp
// InscribirSocioCommand.cs
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Inscripciones;

public class InscribirSocioCommand
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly ISocioRepository _socioRepo;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;

    public InscribirSocioCommand(
        IInscripcionClaseRepository inscripcionRepo,
        IHorarioClaseRepository horarioRepo,
        ISocioRepository socioRepo,
        IEmailService emailService,
        IAuditLogger auditLogger)
    {
        _inscripcionRepo = inscripcionRepo;
        _horarioRepo = horarioRepo;
        _socioRepo = socioRepo;
        _emailService = emailService;
        _auditLogger = auditLogger;
    }

    public async Task<InscripcionClaseDto> ExecuteAsync(Guid socioId, Guid horarioClaseId, Guid usuarioId, string usuarioNombre)
    {
        var horario = await _horarioRepo.GetByIdAsync(horarioClaseId)
            ?? throw new KeyNotFoundException("El horario no existe.");

        var clase = horario.Clase;
        if (!clase.EstaActivo)
            throw new InvalidOperationException("No se puede inscribir a una clase cancelada.");

        var existente = await _inscripcionRepo.GetActivaBySocioYHorarioAsync(socioId, horarioClaseId);
        if (existente != null)
            throw new InvalidOperationException("Ya estás inscripto en este horario.");

        var ocupados = await _inscripcionRepo.GetInscripcionesActivasCountAsync(horarioClaseId);
        var esListaEspera = ocupados >= clase.CapacidadMaxima;

        var inscripcion = new InscripcionClase(horarioClaseId, socioId, esListaEspera);
        await _inscripcionRepo.AddAsync(inscripcion);
        await _inscripcionRepo.SaveChangesAsync();

        var socio = await _socioRepo.GetByIdAsync(socioId);

        if (!esListaEspera && socio != null)
        {
            var (asunto, cuerpo) = InscripcionEmailTemplates.Confirmacion(socio, clase, horario);
            await _emailService.EnviarAsync(socio.Correo, asunto, cuerpo);
        }

        var dia = horario.DiaSemana.ToString();
        var descripcion = esListaEspera
            ? $"Socio agregado a la lista de espera de '{clase.Nombre}' ({dia} {horario.HoraInicio:HH:mm})."
            : $"Socio inscripto en '{clase.Nombre}' ({dia} {horario.HoraInicio:HH:mm}).";

        await _auditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria.Creacion,
            "Inscripcion", inscripcion.Id, descripcion);

        var posicion = esListaEspera
            ? await _inscripcionRepo.GetPosicionEnListaEsperaAsync(inscripcion.Id)
            : (int?)null;

        return InscripcionMapper.ToDto(inscripcion, ocupados + (esListaEspera ? 0 : 1), posicion);
    }
}
```

> **Nota:** El mapper ahora necesita acceder a `inscripcion.HorarioClase` y `inscripcion.HorarioClase.Clase`, que no están cargados porque la inscripción se acaba de crear en memoria. Hay que asignar la navegación manualmente o re-fetch. La solución más simple es setear la navegación antes de mapear. Agregar después del `SaveChangesAsync`:

El `HorarioClase` ya lo tenemos en `horario`, pero la inscripción recién creada no lo tiene en su nav property. Solución: re-fetch la inscripción con includes, o construir el DTO manualmente. El mapper ya tiene toda la info necesaria del `horario` cargado. Hay que ajustar el mapper para recibir el horario explícitamente en vez de leerlo de la navegación cuando la inscripción es nueva:

Alternativa más limpia — el mapper ya funciona si la navegación está seteada. Después del `SaveChangesAsync`, hacemos un re-fetch:

```csharp
// Después del SaveChangesAsync, re-fetch para tener navigations:
var inscripcionConNavs = await _inscripcionRepo.GetByIdAsync(inscripcion.Id);
return InscripcionMapper.ToDto(inscripcionConNavs!, ocupados + (esListaEspera ? 0 : 1), posicion);
```

Actualizar el return del command para usar el re-fetch.

- [ ] **Step 4: Correr tests**

Run: `dotnet test backend/tests/GymFlow.Application.Tests --filter "InscribirSocioCommandTests" -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```
git add backend/src/GymFlow.Application/UseCases/Inscripciones/InscribirSocioCommand.cs backend/tests/GymFlow.Application.Tests/UseCases/Inscripciones/InscribirSocioCommandTests.cs
git commit -m "refactor(command): InscribirSocioCommand usa HorarioClaseId"
```

---

### Task 7: Actualizar `CancelarInscripcionCommand`

**Files:**
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/CancelarInscripcionCommand.cs`

- [ ] **Step 1: Actualizar el command**

La lógica cambia de `ClaseId` a `HorarioClaseId` para buscar la lista de espera:

```csharp
// CancelarInscripcionCommand.cs
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Inscripciones;

public class CancelarInscripcionCommand
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;

    public CancelarInscripcionCommand(
        IInscripcionClaseRepository inscripcionRepo,
        IEmailService emailService,
        IAuditLogger auditLogger)
    {
        _inscripcionRepo = inscripcionRepo;
        _emailService = emailService;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid inscripcionId, Guid socioId, Guid usuarioId, string usuarioNombre)
    {
        var inscripcion = await _inscripcionRepo.GetByIdAsync(inscripcionId)
            ?? throw new KeyNotFoundException("La inscripción no existe.");

        if (inscripcion.SocioId != socioId)
            throw new InvalidOperationException("No tenés permiso para cancelar esta inscripción.");

        if (!inscripcion.EstaActiva)
            throw new InvalidOperationException("La inscripción ya fue cancelada.");

        var eraListaEspera = inscripcion.EsListaEspera;

        inscripcion.Cancelar();
        await _inscripcionRepo.SaveChangesAsync();

        if (!eraListaEspera)
        {
            var primero = await _inscripcionRepo.GetPrimeroEnListaEsperaAsync(inscripcion.HorarioClaseId);
            if (primero != null)
            {
                primero.PromoverDeListaEspera();
                await _inscripcionRepo.SaveChangesAsync();

                var horario = inscripcion.HorarioClase;
                var clase = horario?.Clase;
                if (clase != null && horario != null && primero.Socio != null)
                {
                    var (asunto, cuerpo) = InscripcionEmailTemplates.CupoLiberado(primero.Socio, clase, horario);
                    await _emailService.EnviarAsync(primero.Socio.Correo, asunto, cuerpo);
                }

                await _auditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria.Modificacion,
                    "Inscripcion", primero.Id, "Promovido de lista de espera por cupo liberado");
            }
        }

        await _auditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria.Baja,
            "Inscripcion", inscripcion.Id, "Inscripción cancelada");
    }
}
```

- [ ] **Step 2: Commit**

```
git add backend/src/GymFlow.Application/UseCases/Inscripciones/CancelarInscripcionCommand.cs
git commit -m "refactor(command): CancelarInscripcionCommand usa HorarioClaseId"
```

---

### Task 8: Actualizar `GetMisInscripcionesQuery`

**Files:**
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/GetMisInscripcionesQuery.cs`

- [ ] **Step 1: Actualizar el query**

Cambia de agrupar conteos por `ClaseId` a agrupar por `HorarioClaseId`:

```csharp
// GetMisInscripcionesQuery.cs
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Inscripciones;

public class GetMisInscripcionesQuery
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public GetMisInscripcionesQuery(IInscripcionClaseRepository inscripcionRepo)
    {
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task<IEnumerable<InscripcionClaseDto>> ExecuteAsync(Guid socioId)
    {
        var inscripciones = (await _inscripcionRepo.GetBySocioIdAsync(socioId)).ToList();
        var horarioIds = inscripciones.Select(i => i.HorarioClaseId).Distinct();
        var conteos = await _inscripcionRepo.GetConteoActivasPorHorariosAsync(horarioIds);

        return inscripciones.Select(i =>
        {
            var ocupados = conteos.GetValueOrDefault(i.HorarioClaseId, 0);
            return InscripcionMapper.ToDto(i, ocupados, posicionListaEspera: null);
        });
    }
}
```

- [ ] **Step 2: Commit**

```
git add backend/src/GymFlow.Application/UseCases/Inscripciones/GetMisInscripcionesQuery.cs
git commit -m "refactor(query): GetMisInscripcionesQuery agrupa por HorarioClaseId"
```

---

### Task 9: Actualizar `InscripcionesController`

**Files:**
- Modify: `backend/src/GymFlow.API/Controllers/InscripcionesController.cs`

- [ ] **Step 1: Actualizar el controller**

Cambia el request body de `ClaseId` a `HorarioClaseId`:

```csharp
// InscripcionesController.cs
using System.Security.Claims;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Inscripciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InscripcionesController : ControllerBase
{
    private readonly InscribirSocioCommand _inscribirCommand;
    private readonly CancelarInscripcionCommand _cancelarCommand;
    private readonly GetMisInscripcionesQuery _misInscripcionesQuery;

    public InscripcionesController(
        InscribirSocioCommand inscribirCommand,
        CancelarInscripcionCommand cancelarCommand,
        GetMisInscripcionesQuery misInscripcionesQuery)
    {
        _inscribirCommand = inscribirCommand;
        _cancelarCommand = cancelarCommand;
        _misInscripcionesQuery = misInscripcionesQuery;
    }

    [HttpPost]
    public async Task<ActionResult<InscripcionClaseDto>> Inscribirse([FromBody] InscribirSocioRequest request)
    {
        try
        {
            var socioId = GetSocioId();
            var (usuarioId, usuarioNombre) = GetCurrentUser();
            var inscripcion = await _inscribirCommand.ExecuteAsync(socioId, request.HorarioClaseId, usuarioId, usuarioNombre);
            return Ok(inscripcion);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("mis-inscripciones")]
    public async Task<ActionResult<IEnumerable<InscripcionClaseDto>>> GetMisInscripciones()
    {
        var socioId = GetSocioId();
        var inscripciones = await _misInscripcionesQuery.ExecuteAsync(socioId);
        return Ok(inscripciones);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelarInscripcion(Guid id)
    {
        try
        {
            var socioId = GetSocioId();
            var (usuarioId, usuarioNombre) = GetCurrentUser();
            await _cancelarCommand.ExecuteAsync(id, socioId, usuarioId, usuarioNombre);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private Guid GetSocioId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    }

    private (Guid Id, string Nombre) GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var nombre = User.FindFirst("nombre")?.Value ?? "";
        var apellido = User.FindFirst("apellido")?.Value ?? "";
        var fullName = $"{nombre} {apellido}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = User.FindFirst(ClaimTypes.Email)?.Value ?? "Socio";
        }
        return (userId, fullName);
    }
}
```

- [ ] **Step 2: Commit**

```
git add backend/src/GymFlow.API/Controllers/InscripcionesController.cs
git commit -m "refactor(api): InscripcionesController acepta HorarioClaseId"
```

---

### Task 10: Actualizar queries de horarios y clases (admin) que usaban conteo por clase

**Files:**
- Modify: `backend/src/GymFlow.Application/UseCases/Horarios/GetHorariosQuery.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Horarios/GetHorarioByIdQuery.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Clases/GetClasesQuery.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Clases/GetClaseByIdQuery.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Clases/UpdateClaseCommand.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Clases/CancelClaseCommand.cs`

- [ ] **Step 1: Actualizar `GetHorariosQuery` — contar inscripciones por horario**

```csharp
// GetHorariosQuery.cs
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Horarios;

public class GetHorariosQuery
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public GetHorariosQuery(IHorarioClaseRepository horarioRepo, IInscripcionClaseRepository inscripcionRepo)
    {
        _horarioRepo = horarioRepo;
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task<IEnumerable<HorarioClaseDto>> ExecuteAsync(Guid? unidadId = null)
    {
        var horarios = (await _horarioRepo.GetAllAsync(unidadId)).ToList();
        var horarioIds = horarios.Select(h => h.Id);
        var conteos = await _inscripcionRepo.GetConteoActivasPorHorariosAsync(horarioIds);
        return horarios.Select(h => HorarioMapper.ToDto(h, conteos.GetValueOrDefault(h.Id, 0)));
    }
}
```

- [ ] **Step 2: Actualizar `GetHorarioByIdQuery`**

Buscar el archivo y cambiar la línea que llama `_claseRepo.GetInscripcionesActivasCountAsync(h.ClaseId)` por `_inscripcionRepo.GetInscripcionesActivasCountAsync(h.Id)`. Inyectar `IInscripcionClaseRepository` en vez de `IClaseRepository`.

- [ ] **Step 3: Actualizar `GetClasesQuery` y `GetClaseByIdQuery`**

Estos queries muestran `inscripcionesActivas` en el `ClaseDto`. Ahora que la inscripción es por horario, la clase ya no tiene un conteo directo de inscripciones. Opciones:
- **Opción elegida**: `ClaseDto.InscripcionesActivas` no tiene sentido directo. Lo dejamos en 0 o lo removemos. Como el admin ve inscripciones en la grilla de horarios (cada horario muestra su ocupación), no necesita este dato en la lista de clases.

Eliminar `InscripcionesActivas` de `ClaseDto`:

En `backend/src/GymFlow.Application/DTOs/ClaseDtos.cs`, quitar el campo `InscripcionesActivas` del record `ClaseDto`. Actualizar `GetClasesQuery`, `GetClaseByIdQuery` y `UpdateClaseCommand` para no pasar ese valor.

- [ ] **Step 4: Actualizar `UpdateClaseCommand`**

La validación `capacidadMaxima < inscripcionesActivas` del método `Clase.Actualizar()` debe seguir funcionando. Ahora hay que obtener el **máximo** de inscripciones activas entre todos los horarios de esa clase:

```csharp
// En UpdateClaseCommand.ExecuteAsync:
var horarios = await _horarioRepo.GetByClaseIdAsync(id);
var horarioIds = horarios.Select(h => h.Id);
var conteos = await _inscripcionRepo.GetConteoActivasPorHorariosAsync(horarioIds);
var maxInscripciones = conteos.Values.DefaultIfEmpty(0).Max();

clase.Actualizar(request.Nombre, request.Descripcion ?? "", request.CapacidadMaxima,
    request.DuracionMinutos, request.Instructor, maxInscripciones);
```

Inyectar `IHorarioClaseRepository` e `IInscripcionClaseRepository` en el constructor.

- [ ] **Step 5: Actualizar `CancelClaseCommand`**

Al cancelar una clase, se deben cancelar las inscripciones de **todos sus horarios**:

```csharp
// En CancelClaseCommand.ExecuteAsync:
var horarios = await _horarioRepo.GetByClaseIdAsync(id);
var todasInscripciones = new List<InscripcionClase>();
foreach (var h in horarios)
{
    var inscs = await _inscripcionRepo.GetActivasByHorarioClaseIdAsync(h.Id);
    todasInscripciones.AddRange(inscs);
}

foreach (var inscripcion in todasInscripciones)
{
    inscripcion.Cancelar();
}
```

Inyectar `IHorarioClaseRepository` e `IInscripcionClaseRepository` en el constructor (reemplazando `IClaseRepository` para los métodos de inscripción).

- [ ] **Step 6: Actualizar DI registration**

Verificar que `Program.cs` (o el archivo de DI) registra los nuevos constructores. Los commands que ahora inyectan `IHorarioClaseRepository` e `IInscripcionClaseRepository` deben tenerlos registrados (probablemente ya lo están).

- [ ] **Step 7: Compilar y correr todos los tests**

Run: `dotnet build backend/` luego `dotnet test backend/ -v n`
Expected: todo compila y tests pasan.

- [ ] **Step 8: Commit**

```
git add backend/src/GymFlow.Application/
git commit -m "refactor(queries): actualizar queries y commands admin para inscripción por horario"
```

---

### Task 11: Actualizar frontend — types, API, y eliminar `CatalogoClasesPage`

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/services/api.ts`
- Delete: `frontend/src/pages/portal/CatalogoClasesPage.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/components/layout/SocioLayout.tsx`

- [ ] **Step 1: Actualizar tipo `InscripcionClase`**

En `frontend/src/types/index.ts`, reemplazar la interface `InscripcionClase`:

```typescript
export interface InscripcionClase {
  id: string
  horarioClaseId: string
  claseId: string
  claseNombre: string
  instructor: string
  unidadId: string
  unidadNombre: string
  diaSemana: DiaSemana
  horaInicio: string
  horaFin: string
  sala: string | null
  capacidadMaxima: number
  inscripcionesActivas: number
  fechaInscripcion: string
  enListaEspera: boolean
  posicionListaEspera: number | null
}
```

- [ ] **Step 2: Actualizar `inscripcionesApi`**

En `frontend/src/services/api.ts`, cambiar el método `inscribirse` para enviar `horarioClaseId`:

```typescript
export const inscripcionesApi = {
  inscribirse: async (horarioClaseId: string): Promise<InscripcionClase> => {
    const { data } = await api.post<InscripcionClase>('/inscripciones', { horarioClaseId })
    return data
  },

  getMisInscripciones: async (): Promise<InscripcionClase[]> => {
    const { data } = await api.get<InscripcionClase[]>('/inscripciones/mis-inscripciones')
    return data
  },

  cancelar: async (id: string): Promise<void> => {
    await api.delete(`/inscripciones/${id}`)
  },
}
```

- [ ] **Step 3: Eliminar `CatalogoClasesPage` y quitar ruta/nav**

Eliminar el archivo `frontend/src/pages/portal/CatalogoClasesPage.tsx`.

En `frontend/src/App.tsx`:
- Eliminar import: `import CatalogoClasesPage from '@/pages/portal/CatalogoClasesPage'`
- Eliminar ruta: `<Route path="clases" element={<CatalogoClasesPage />} />`

En `frontend/src/components/layout/SocioLayout.tsx`:
- Eliminar import de `Calendar` de lucide-react (si solo se usa para esa nav)
- Eliminar el bloque `<Link to="/portal/clases">...</Link>` (líneas 83-93)

- [ ] **Step 4: Commit**

```
git add frontend/src/types/index.ts frontend/src/services/api.ts frontend/src/App.tsx frontend/src/components/layout/SocioLayout.tsx
git rm frontend/src/pages/portal/CatalogoClasesPage.tsx
git commit -m "refactor(frontend): eliminar CatalogoClasesPage, actualizar types y API para inscripción por horario"
```

---

### Task 12: Actualizar `HorariosPortalPage` — inscripción por horario

**Files:**
- Modify: `frontend/src/pages/portal/HorariosPortalPage.tsx`

- [ ] **Step 1: Actualizar la lógica de inscripción**

El cambio clave: el mapeo de inscripciones pasa de `inscripcionPorClase` (Map por `claseId`) a `inscripcionPorHorario` (Map por `horarioClaseId`). Y el botón "Inscribirme" envía `h.id` (el ID del horario) en vez de `h.claseId`.

```typescript
// HorariosPortalPage.tsx — cambios clave:

// Cambiar el mapeo de inscripciones (líneas 73-76):
const inscripcionPorHorario = new Map<string, InscripcionClase>()
misInscripciones?.forEach(i => {
  inscripcionPorHorario.set(i.horarioClaseId, i)
})

// En el render de cada horario (línea 169):
const inscripcion = inscripcionPorHorario.get(h.id)

// En el onClick de "Inscribirme" (línea 219):
onClick={() => inscribirseMutation.mutate(h.id)}
```

Cambiar también `inscribirseMutation` para usar el `id` del horario:

```typescript
const inscribirseMutation = useMutation({
  mutationFn: (horarioClaseId: string) => inscripcionesApi.inscribirse(horarioClaseId),
  // ... resto igual
})
```

- [ ] **Step 2: Commit**

```
git add frontend/src/pages/portal/HorariosPortalPage.tsx
git commit -m "feat(portal): inscripción por horario individual en HorariosPortalPage"
```

---

### Task 13: Actualizar `MisInscripcionesPage` — mostrar día y hora

**Files:**
- Modify: `frontend/src/pages/portal/MisInscripcionesPage.tsx`

- [ ] **Step 1: Agregar día y hora a la vista de mis inscripciones**

El DTO ahora trae `diaSemana`, `horaInicio`, `horaFin`. Mostrarlos:

```typescript
// En el bloque de cada inscripción, después de instructor · unidad:
<p className="text-xs text-muted-foreground">
  {i.instructor} &middot; {i.unidadNombre}
</p>
<p className="text-xs text-muted-foreground">
  {i.diaSemana} {i.horaInicio} - {i.horaFin}
  {i.sala && <> &middot; {i.sala}</>}
</p>
<p className="text-xs text-muted-foreground mt-1">
  Inscripto el {new Date(i.fechaInscripcion).toLocaleDateString('es-UY')}
</p>
```

- [ ] **Step 2: Commit**

```
git add frontend/src/pages/portal/MisInscripcionesPage.tsx
git commit -m "feat(portal): mostrar día y hora en MisInscripcionesPage"
```

---

### Task 14: Quitar `InscripcionesActivas` de `ClaseDto` y actualizar admin

**Files:**
- Modify: `backend/src/GymFlow.Application/DTOs/ClaseDtos.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Clases/GetClasesQuery.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Clases/GetClaseByIdQuery.cs`
- Modify: `frontend/src/types/index.ts` (tipo `Clase`)
- Modify: `frontend/src/pages/admin/ClasesPage.tsx` (si muestra inscripcionesActivas)

- [ ] **Step 1: Quitar `InscripcionesActivas` de `ClaseDto`**

```csharp
// ClaseDtos.cs — quitar el último campo
public record ClaseDto(
    Guid Id,
    string Nombre,
    string Descripcion,
    int CapacidadMaxima,
    int DuracionMinutos,
    string Instructor,
    Guid UnidadId,
    string UnidadNombre,
    bool EstaActivo);
```

- [ ] **Step 2: Actualizar `GetClasesQuery` — quitar conteo N+1**

Simplificar el query para no contar inscripciones (ya no tiene sentido por clase):

```csharp
public async Task<IEnumerable<ClaseDto>> ExecuteAsync(Guid? unidadId = null, bool includeInactive = false)
{
    var clases = unidadId.HasValue
        ? await _claseRepo.GetByUnidadIdAsync(unidadId.Value, includeInactive)
        : await _claseRepo.GetAllAsync(includeInactive);

    return clases.Select(c => new ClaseDto(
        c.Id, c.Nombre, c.Descripcion, c.CapacidadMaxima, c.DuracionMinutos,
        c.Instructor, c.UnidadId, c.Unidad?.Nombre ?? "", c.EstaActivo));
}
```

- [ ] **Step 3: Actualizar `GetClaseByIdQuery` de la misma forma**

Quitar la llamada a `GetInscripcionesActivasCountAsync` y ajustar la construcción del DTO.

- [ ] **Step 4: Actualizar tipo TypeScript `Clase`**

En `frontend/src/types/index.ts`, quitar `inscripcionesActivas` de la interfaz `Clase`:

```typescript
export interface Clase {
  id: string
  nombre: string
  descripcion: string
  capacidadMaxima: number
  duracionMinutos: number
  instructor: string
  unidadId: string
  unidadNombre: string
  estaActivo: boolean
}
```

- [ ] **Step 5: Actualizar `ClasesPage.tsx` si muestra inscripcionesActivas**

Quitar cualquier referencia a `inscripcionesActivas` o `cuposDisponibles` de la tabla de clases del admin.

- [ ] **Step 6: Compilar frontend y backend**

Run: `dotnet build backend/` y `cd frontend && npm run build`
Expected: ambos compilan sin errores.

- [ ] **Step 7: Commit**

```
git add backend/src/GymFlow.Application/DTOs/ClaseDtos.cs backend/src/GymFlow.Application/UseCases/Clases/ frontend/src/types/index.ts frontend/src/pages/admin/ClasesPage.tsx
git commit -m "refactor: quitar InscripcionesActivas de ClaseDto (ahora es por horario)"
```

---

### Task 15: Actualizar DI y verificar compilación completa

**Files:**
- Modify: `backend/src/GymFlow.API/Program.cs` (o archivo de DI)

- [ ] **Step 1: Verificar registros de DI**

Los commands `InscribirSocioCommand`, `CancelarInscripcionCommand`, `GetHorariosQuery`, `UpdateClaseCommand`, `CancelClaseCommand` cambiaron sus constructores. Verificar que `Program.cs` los registra correctamente. Si usan `AddScoped`/`AddTransient` directo, los cambios de constructor se resuelven automáticamente por DI. Solo verificar que no haya registros explícitos que rompan.

- [ ] **Step 2: Compilar todo**

Run: `dotnet build backend/`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Correr todos los tests**

Run: `dotnet test backend/ -v n`
Expected: todos pasan.

- [ ] **Step 4: Commit si hubo cambios en DI**

```
git add backend/src/GymFlow.API/Program.cs
git commit -m "fix(di): ajustar registros de DI para nuevos constructores"
```

---

### Task 16: Actualizar documentación — spec y plan de IT4

**Files:**
- Modify: `docs/specs/spec-it4-inscripciones-empleados-horarios.md`
- Modify: `docs/plans/plan-it4-inscripciones-empleados-horarios.md`
- Modify: `docs/GymFlow_Requerimientos_Completos.md`

- [ ] **Step 1: Actualizar spec de IT4**

Agregar una sección al inicio de la spec documentando el cambio de modelo:

```markdown
## Cambio de modelo: Inscripción por Horario (no por Clase)

**Decisión (2026-06-05):** El modelo de inscripción cambió de FK a `Clase` a FK a `HorarioClase`.

**Motivación:** Un socio no se inscribe "a Yoga" genéricamente, sino a "Yoga los lunes de 8 a 10". La inscripción por horario individual es más natural y permite control de cupo granular por franja horaria.

**Cambios al modelo:**
- `InscripcionClase.ClaseId` → `InscripcionClase.HorarioClaseId`
- `CapacidadMaxima` se mantiene en `Clase` (es la capacidad de la actividad, compartida por todos sus horarios)
- El conteo de cupo/inscripciones activas se hace por `HorarioClaseId`
- `InscripcionClaseDto` incluye `DiaSemana`, `HoraInicio`, `HoraFin`, `Sala`
- `POST /api/inscripciones` recibe `{ horarioClaseId }` en vez de `{ claseId }`

**Frontend:**
- Se elimina `CatalogoClasesPage` (`portal/clases`) — vista redundante
- `HorariosPortalPage` (`portal/horarios`) es la vista unificada de inscripción
- Un socio puede inscribirse a la misma clase en distintos horarios individualmente
```

Actualizar las secciones del spec que mencionaban `ClaseId` en inscripciones (§2, §4, §9, §10, §11).

- [ ] **Step 2: Actualizar el plan de IT4**

Agregar nota en el plan indicando que el componente 4 (Catálogo de clases) fue reemplazado por la inscripción directa desde Horarios.

- [ ] **Step 3: Actualizar requerimientos**

En `docs/GymFlow_Requerimientos_Completos.md`, actualizar:
- §4 (Modelo de Dominio): `Inscripcion` ahora tiene FK a `Horario`, no a `Clase`
- CU-02: el socio se inscribe desde la vista de horarios a un horario específico
- RN-09: "un socio no puede inscribirse dos veces al mismo horario"

- [ ] **Step 4: Commit**

```
git add docs/
git commit -m "docs: actualizar spec, plan y requerimientos para inscripción por horario"
```

---

### Task 17: Test manual end-to-end

- [ ] **Step 1: Aplicar migración en BD de dev**

Run: `dotnet ef database update --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`

- [ ] **Step 2: Levantar backend y frontend**

Run: `dotnet run --project backend/src/GymFlow.API` (en una terminal)
Run: `cd frontend && npm run dev` (en otra terminal)

- [ ] **Step 3: Verificar flujo completo**

1. Login como admin → crear clase → crear horarios
2. Login como socio → ir a "Horarios" → inscribirse a un horario específico
3. Verificar que el cupo se actualiza solo para ese horario
4. Inscribirse a otro horario de la misma clase → debe permitirlo
5. Ir a "Mis Inscripciones" → ver día y hora de cada inscripción
6. Cancelar una inscripción → verificar que el cupo se libera

- [ ] **Step 4: Verificar que `portal/clases` ya no existe**

Navegar a `/portal/clases` → debe dar 404 o redirigir.

- [ ] **Step 5: Commit final si hay ajustes**

```
git add -A
git commit -m "fix: ajustes finales tras test manual de inscripción por horario"
```
