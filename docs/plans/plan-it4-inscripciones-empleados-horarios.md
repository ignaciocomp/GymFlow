# Iteración 4 — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar los gaps de inscripción a clases (RF-10/11) y gestión de empleados (RF-12) detectados contra los criterios de aceptación CU-02/CU-07, más mejoras de UX en Horarios.

**Architecture:** Backend .NET 8 Clean Architecture (Domain → Application → Infrastructure → API), patrón Command/Query, repositorios con interfaz en Application e implementación en Infrastructure. Frontend React/Vite con TanStack Query + Axios. Postgres con EF Core. Se sigue el patrón existente (emails con `IEmailService` + plantillas con `HtmlEncode`, auditoría con `IAuditLogger`).

**Tech Stack:** .NET 8, EF Core 8, Npgsql, xUnit + Moq, React 18, Vite, TanStack Query, Tailwind.

**Spec:** [[spec-it4-inscripciones-empleados-horarios]]

**Orden de dependencias:** Tareas 1-9 (backend inscripciones) → 10 (empleados) → 11-12 (frontend) → 13 (responsive). Cada tarea deja el repo compilando y con tests verdes.

---

## Task 1: Agregar `EsListaEspera` a `InscripcionClase` + migración

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/InscripcionClase.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/InscripcionClaseConfiguration.cs`
- Test: `backend/tests/GymFlow.Domain.Tests/Entities/InscripcionClaseTests.cs` (crear)
- Migration: autogenerada

- [ ] **Step 1: Escribir test de dominio que falla**

```csharp
// backend/tests/GymFlow.Domain.Tests/Entities/InscripcionClaseTests.cs
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

- [ ] **Step 2: Correr el test y verificar que falla**

Run: `cd backend && dotnet test tests/GymFlow.Domain.Tests --filter "InscripcionClaseTests"`
Expected: FALLA de compilación (`EsListaEspera` y constructor con flag no existen).

- [ ] **Step 3: Modificar la entidad**

En `InscripcionClase.cs`, agregar la propiedad y el método, y un constructor con el flag opcional:

```csharp
public bool EsListaEspera { get; private set; }

private InscripcionClase() { } // EF Core

public InscripcionClase(Guid claseId, Guid socioId, bool esListaEspera = false)
{
    Id = Guid.NewGuid();
    ClaseId = claseId;
    SocioId = socioId;
    FechaInscripcion = DateTime.UtcNow;
    EstaActiva = true;
    EsListaEspera = esListaEspera;
}

public void Cancelar() => EstaActiva = false;

public void PromoverDeListaEspera()
{
    if (!EsListaEspera)
        throw new InvalidOperationException("La inscripción no está en lista de espera.");
    EsListaEspera = false;
}
```

- [ ] **Step 4: Mapear el campo en EF config**

En `InscripcionClaseConfiguration.cs`, agregar dentro de `Configure`:

```csharp
builder.Property(i => i.EsListaEspera).IsRequired().HasDefaultValue(false);
```

- [ ] **Step 5: Correr el test y verificar que pasa**

Run: `dotnet test tests/GymFlow.Domain.Tests --filter "InscripcionClaseTests"`
Expected: PASS (4 tests).

- [ ] **Step 6: Generar migración**

Run desde `backend/`:
```bash
dotnet ef migrations add AddListaEsperaInscripcion --project src/GymFlow.Infrastructure --startup-project src/GymFlow.API
```
Expected: crea `*_AddListaEsperaInscripcion.cs` con `AddColumn EsListaEspera`.

- [ ] **Step 7: Build completo + commit**

```bash
dotnet build
git add backend/src/GymFlow.Domain backend/src/GymFlow.Infrastructure backend/tests/GymFlow.Domain.Tests
git commit -m "feat(it4): agregar EsListaEspera a InscripcionClase + migración"
```

---

## Task 2: `ICuotaRepository.TieneCuotasVencidasEnUnidadAsync`

**Files:**
- Modify: `backend/src/GymFlow.Application/Interfaces/ICuotaRepository.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Repositories/CuotaRepository.cs`
- Test: `backend/tests/GymFlow.Infrastructure.Tests/Repositories/CuotaRepositoryTests.cs` (crear si no existe; usar EF InMemory o SQLite)

> Nota: si no hay infra de tests de repositorio con DB, validar este método indirectamente vía el test del command (Task 4) con un mock. En ese caso, saltear los steps 1-2 de DB acá y solo agregar interfaz+implementación (steps 3-4), verificando con `dotnet build`.

- [ ] **Step 1: Agregar a la interfaz**

En `ICuotaRepository.cs`:
```csharp
/// <summary>True si el socio tiene al menos una cuota Pendiente y vencida en esa unidad.</summary>
Task<bool> TieneCuotasVencidasEnUnidadAsync(Guid socioId, Guid unidadId);
```

- [ ] **Step 2: Implementar en `CuotaRepository.cs`**

```csharp
public async Task<bool> TieneCuotasVencidasEnUnidadAsync(Guid socioId, Guid unidadId)
{
    var hoy = DateTime.UtcNow.Date;
    return await _context.Cuotas.AnyAsync(c =>
        c.SocioId == socioId &&
        c.UnidadId == unidadId &&
        c.Estado == EstadoCuota.Pendiente &&
        c.FechaVencimiento.Date < hoy);
}
```

- [ ] **Step 3: Build + commit**

```bash
dotnet build
git add backend/src/GymFlow.Application/Interfaces/ICuotaRepository.cs backend/src/GymFlow.Infrastructure/Repositories/CuotaRepository.cs
git commit -m "feat(it4): ICuotaRepository.TieneCuotasVencidasEnUnidadAsync"
```

---

## Task 3: Métodos de repositorio de inscripciones (lista de espera + conteo)

**Files:**
- Modify: `backend/src/GymFlow.Application/Interfaces/IInscripcionClaseRepository.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Repositories/InscripcionClaseRepository.cs`
- Modify: `backend/src/GymFlow.Application/Interfaces/IClaseRepository.cs` (ajustar conteo)
- Modify: `backend/src/GymFlow.Infrastructure/Repositories/ClaseRepository.cs`

- [ ] **Step 1: Ajustar y agregar a `IInscripcionClaseRepository`**

```csharp
Task<int> GetInscripcionesActivasCountAsync(Guid claseId); // ahora excluye EsListaEspera
Task<InscripcionClase?> GetPrimeroEnListaEsperaAsync(Guid claseId);
Task<int> GetPosicionEnListaEsperaAsync(Guid inscripcionId);
Task<Dictionary<Guid, int>> GetConteoActivasPorClasesAsync(IEnumerable<Guid> claseIds);
```

- [ ] **Step 2: Implementar en `InscripcionClaseRepository.cs`**

```csharp
public async Task<int> GetInscripcionesActivasCountAsync(Guid claseId) =>
    await _context.InscripcionesClase
        .CountAsync(i => i.ClaseId == claseId && i.EstaActiva && !i.EsListaEspera);

public async Task<InscripcionClase?> GetPrimeroEnListaEsperaAsync(Guid claseId) =>
    await _context.InscripcionesClase
        .Include(i => i.Socio)
        .Where(i => i.ClaseId == claseId && i.EstaActiva && i.EsListaEspera)
        .OrderBy(i => i.FechaInscripcion)
        .FirstOrDefaultAsync();

public async Task<int> GetPosicionEnListaEsperaAsync(Guid inscripcionId)
{
    var insc = await _context.InscripcionesClase.FindAsync(inscripcionId);
    if (insc is null || !insc.EsListaEspera) return 0;
    return await _context.InscripcionesClase.CountAsync(i =>
        i.ClaseId == insc.ClaseId && i.EstaActiva && i.EsListaEspera &&
        i.FechaInscripcion <= insc.FechaInscripcion);
}

public async Task<Dictionary<Guid, int>> GetConteoActivasPorClasesAsync(IEnumerable<Guid> claseIds)
{
    var ids = claseIds.Distinct().ToList();
    return await _context.InscripcionesClase
        .Where(i => ids.Contains(i.ClaseId) && i.EstaActiva && !i.EsListaEspera)
        .GroupBy(i => i.ClaseId)
        .Select(g => new { g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Key, x => x.Count);
}
```

- [ ] **Step 3: Ajustar `IClaseRepository.GetInscripcionesActivasCountAsync`**

En `ClaseRepository.cs`, el método existente debe excluir lista de espera:
```csharp
public async Task<int> GetInscripcionesActivasCountAsync(Guid claseId) =>
    await _context.InscripcionesClase
        .CountAsync(i => i.ClaseId == claseId && i.EstaActiva && !i.EsListaEspera);
```
> `GetInscripcionesActivasAsync` (usado por `CancelClaseCommand` para notificar) **NO se toca** — sigue incluyendo a todos los activos.

- [ ] **Step 4: Build + commit**

```bash
dotnet build
git add backend/src/GymFlow.Application/Interfaces backend/src/GymFlow.Infrastructure/Repositories
git commit -m "feat(it4): repos de lista de espera y conteo excluyendo lista de espera"
```

---

## Task 4: `InscribirSocioCommand` — cuota + lista de espera + email + auditoría

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Inscripciones/InscripcionEmailTemplates.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/InscribirSocioCommand.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/InscripcionMapper.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/InscripcionClaseDtos.cs`
- Test: `backend/tests/GymFlow.Application.Tests/UseCases/Inscripciones/InscribirSocioCommandTests.cs` (crear)

- [ ] **Step 1: Extender el DTO**

En `InscripcionClaseDtos.cs`, agregar al record `InscripcionClaseDto` los campos `bool EnListaEspera` y `int? PosicionListaEspera`.

- [ ] **Step 2: Crear plantilla de email**

```csharp
// InscripcionEmailTemplates.cs
using System.Net;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

internal static class InscripcionEmailTemplates
{
    public static (string Asunto, string Cuerpo) Confirmacion(Socio socio, Clase clase)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var asunto = $"Inscripción confirmada: {claseNombre}";
        var cuerpo = $@"<html><body style='font-family:Arial,sans-serif;'>
<h2>Hola {nombre},</h2>
<p>Tu inscripción a la clase <strong>{claseNombre}</strong> fue confirmada.</p>
<p>Saludos,<br/>Equipo GymFlow</p></body></html>";
        return (asunto, cuerpo);
    }

    public static (string Asunto, string Cuerpo) CupoLiberado(Socio socio, Clase clase)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var asunto = $"Se liberó un cupo: {claseNombre}";
        var cuerpo = $@"<html><body style='font-family:Arial,sans-serif;'>
<h2>Hola {nombre},</h2>
<p>Se liberó un cupo en <strong>{claseNombre}</strong> y ya quedaste inscripto.</p>
<p>Saludos,<br/>Equipo GymFlow</p></body></html>";
        return (asunto, cuerpo);
    }
}
```

- [ ] **Step 3: Escribir tests del command que fallan**

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
    private readonly Mock<IInscripcionClaseRepository> _inscRepo = new();
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IAuditLogger> _audit = new();

    private InscribirSocioCommand Sut() => new(
        _inscRepo.Object, _claseRepo.Object, _cuotaRepo.Object,
        _socioRepo.Object, _email.Object, _audit.Object);

    private static Clase ClaseActiva(int capacidad = 10) =>
        new("Yoga", "desc", capacidad, 60, "Laura", Guid.NewGuid());

    private static Socio SocioFake() =>
        new(Guid.NewGuid(), "María", "López", "m@test.com", "h", DateTime.UtcNow,
            true, TipoDocumento.CI, null, "12345672", null);

    [Fact]
    public async Task ConCuotaVencida_LanzaInvalidOperation()
    {
        var clase = ClaseActiva();
        _claseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(clase);
        _cuotaRepo.Setup(r => r.TieneCuotasVencidasEnUnidadAsync(It.IsAny<Guid>(), clase.UnidadId))
                  .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Sut().ExecuteAsync(Guid.NewGuid(), clase.Id, Guid.NewGuid(), "María López"));
    }

    [Fact]
    public async Task ConCupo_InscribeNormalEnviaEmailYAudita()
    {
        var socio = SocioFake();
        var clase = ClaseActiva(capacidad: 10);
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _cuotaRepo.Setup(r => r.TieneCuotasVencidasEnUnidadAsync(socio.Id, clase.UnidadId)).ReturnsAsync(false);
        _inscRepo.Setup(r => r.GetActivaBySocioYClaseAsync(socio.Id, clase.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscRepo.Setup(r => r.GetInscripcionesActivasCountAsync(clase.Id)).ReturnsAsync(3);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _email.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new EmailResultado(true));

        var dto = await Sut().ExecuteAsync(socio.Id, clase.Id, socio.Id, "María López");

        Assert.False(dto.EnListaEspera);
        _email.Verify(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Creacion, "Inscripcion", It.IsAny<Guid>(), It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task SinCupo_CreaEnListaEspera()
    {
        var socio = SocioFake();
        var clase = ClaseActiva(capacidad: 3);
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _cuotaRepo.Setup(r => r.TieneCuotasVencidasEnUnidadAsync(socio.Id, clase.UnidadId)).ReturnsAsync(false);
        _inscRepo.Setup(r => r.GetActivaBySocioYClaseAsync(socio.Id, clase.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscRepo.Setup(r => r.GetInscripcionesActivasCountAsync(clase.Id)).ReturnsAsync(3); // lleno
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);

        var dto = await Sut().ExecuteAsync(socio.Id, clase.Id, socio.Id, "María López");

        Assert.True(dto.EnListaEspera);
        _inscRepo.Verify(r => r.AddAsync(It.Is<InscripcionClase>(i => i.EsListaEspera)), Times.Once);
    }
}
```

- [ ] **Step 4: Correr y verificar que fallan**

Run: `dotnet test tests/GymFlow.Application.Tests --filter "InscribirSocioCommandTests"`
Expected: FALLA de compilación (firma del command y campos del DTO nuevos).

- [ ] **Step 5: Reescribir `InscribirSocioCommand`**

```csharp
public class InscribirSocioCommand
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IClaseRepository _claseRepo;
    private readonly ICuotaRepository _cuotaRepo;
    private readonly ISocioRepository _socioRepo;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;

    public InscribirSocioCommand(
        IInscripcionClaseRepository inscripcionRepo, IClaseRepository claseRepo,
        ICuotaRepository cuotaRepo, ISocioRepository socioRepo,
        IEmailService emailService, IAuditLogger auditLogger)
    { /* asignar todos */ }

    public async Task<InscripcionClaseDto> ExecuteAsync(Guid socioId, Guid claseId, Guid usuarioId, string usuarioNombre)
    {
        var clase = await _claseRepo.GetByIdAsync(claseId)
            ?? throw new KeyNotFoundException("La clase no existe.");
        if (!clase.EstaActivo)
            throw new InvalidOperationException("No se puede inscribir a una clase cancelada.");

        if (await _cuotaRepo.TieneCuotasVencidasEnUnidadAsync(socioId, clase.UnidadId))
            throw new InvalidOperationException("Tu cuota está vencida en esta sede. Regularizá tu pago para inscribirte.");

        if (await _inscripcionRepo.GetActivaBySocioYClaseAsync(socioId, claseId) != null)
            throw new InvalidOperationException("Ya estás inscripto en esta clase.");

        var ocupados = await _inscripcionRepo.GetInscripcionesActivasCountAsync(claseId);
        var esListaEspera = ocupados >= clase.CapacidadMaxima;

        var inscripcion = new InscripcionClase(claseId, socioId, esListaEspera);
        await _inscripcionRepo.AddAsync(inscripcion);
        await _inscripcionRepo.SaveChangesAsync();

        var socio = await _socioRepo.GetByIdAsync(socioId);
        if (!esListaEspera && socio is not null)
        {
            var (asunto, cuerpo) = InscripcionEmailTemplates.Confirmacion(socio, clase);
            await _emailService.EnviarAsync(socio.Correo, asunto, cuerpo); // best-effort
        }

        await _auditLogger.LogAsync(usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Inscripcion", inscripcion.Id,
            esListaEspera
                ? $"{socio?.Nombre} {socio?.Apellido} quedó en lista de espera de '{clase.Nombre}'"
                : $"{socio?.Nombre} {socio?.Apellido} se inscribió a '{clase.Nombre}'");

        var posicion = esListaEspera
            ? await _inscripcionRepo.GetPosicionEnListaEsperaAsync(inscripcion.Id)
            : (int?)null;

        return InscripcionMapper.ToDto(inscripcion, clase, ocupados + (esListaEspera ? 0 : 1), posicion);
    }
}
```

- [ ] **Step 6: Actualizar `InscripcionMapper.ToDto`**

Nueva firma con el último parámetro **opcional** para no romper los call-sites existentes (`GetMisInscripcionesQuery` lo llama con 3 args hasta la Task 6):

```csharp
public static InscripcionClaseDto ToDto(InscripcionClase i, Clase clase, int cuposOcupados, int? posicionListaEspera = null)
```

Setear `EnListaEspera = i.EsListaEspera` y `PosicionListaEspera = posicionListaEspera`.

> ⚠️ El parámetro opcional es clave: sin él, `GetMisInscripcionesQuery` (que llama `ToDto(i, i.Clase, count)`) deja de compilar entre la Task 4 y la Task 6, y el Step 7 ("tests verdes") fallaría.

- [ ] **Step 7: Correr tests y verificar verde**

Run: `dotnet test tests/GymFlow.Application.Tests --filter "InscribirSocioCommandTests"`
Expected: PASS (3 tests).

- [ ] **Step 8: Commit**

```bash
git add backend/src/GymFlow.Application backend/tests/GymFlow.Application.Tests/UseCases/Inscripciones
git commit -m "feat(it4): InscribirSocioCommand con cuota, lista de espera, email y auditoría"
```

---

## Task 5: `CancelarInscripcionCommand` — auditoría + promover lista de espera

**Files:**
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/CancelarInscripcionCommand.cs`
- Test: `backend/tests/GymFlow.Application.Tests/UseCases/Inscripciones/CancelarInscripcionCommandTests.cs` (crear)

- [ ] **Step 1: Escribir tests que fallan**

Casos: (a) no es dueño → excepción; (b) cancela activa y promueve al primero de lista de espera + email `CupoLiberado` + auditoría de ambas; (c) cancela y no hay lista de espera → solo auditoría.

```csharp
[Fact]
public async Task CancelaYPromueveListaEspera()
{
    var socioId = Guid.NewGuid();
    var inscActiva = new InscripcionClase(claseId, socioId); // dueño
    var enEspera = new InscripcionClase(claseId, Guid.NewGuid(), esListaEspera: true);
    typeof(InscripcionClase).GetProperty("Socio")!.SetValue(enEspera, SocioFake());
    _repo.Setup(r => r.GetByIdAsync(inscActiva.Id)).ReturnsAsync(inscActiva);
    _claseRepo.Setup(r => r.GetByIdAsync(claseId)).ReturnsAsync(ClaseActiva());
    _repo.Setup(r => r.GetPrimeroEnListaEsperaAsync(claseId)).ReturnsAsync(enEspera);
    _email.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new EmailResultado(true));

    await Sut().ExecuteAsync(inscActiva.Id, socioId, socioId, "María López");

    Assert.False(enEspera.EsListaEspera); // promovido
    _email.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
}
```

- [ ] **Step 2: Verificar que fallan** — `dotnet test ... --filter "CancelarInscripcionCommandTests"` → FALLA compilación.

- [ ] **Step 3: Reescribir el command**

Inyectar `IClaseRepository`, `IEmailService`, `IAuditLogger`. Firma: `ExecuteAsync(Guid inscripcionId, Guid socioId, Guid usuarioId, string usuarioNombre)`. Tras `inscripcion.Cancelar()` + `SaveChangesAsync`:
- Si la inscripción cancelada **no** era lista de espera:
  1. `var primero = await _inscripcionRepo.GetPrimeroEnListaEsperaAsync(inscripcion.ClaseId);`
  2. Si `primero != null`: `primero.PromoverDeListaEspera()`, `SaveChangesAsync`.
  3. **Cargar la clase** para el email: `var clase = await _claseRepo.GetByIdAsync(inscripcion.ClaseId);` (el command solo tiene el `claseId`, necesita la `Clase` para `CupoLiberado(primero.Socio, clase)`). `GetPrimeroEnListaEsperaAsync` ya hace `.Include(i => i.Socio)`, así que `primero.Socio.Correo` está disponible.
  4. Enviar email `CupoLiberado` (best-effort) + auditar la promoción.
- Auditar la cancelación (`TipoAccionAuditoria.Baja`, "Inscripcion").

> Nota sobre el test (Step 1): setear la nav property `Socio` por reflexión (`typeof(InscripcionClase).GetProperty("Socio")!.SetValue(...)`) es el patrón ya usado en `CancelClaseCommandTests` del proyecto y funciona con setters privados — `PropertyInfo.SetValue` invoca el setter privado sin flags adicionales.

- [ ] **Step 4: Verificar verde** — `dotnet test ... --filter "CancelarInscripcionCommandTests"` → PASS.

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(it4): CancelarInscripcionCommand con auditoría y promoción de lista de espera"
```

---

## Task 6: Fix N+1 en `GetMisInscripcionesQuery`

**Files:**
- Modify: `backend/src/GymFlow.Application/UseCases/Inscripciones/GetMisInscripcionesQuery.cs`
- Test: `backend/tests/GymFlow.Application.Tests/UseCases/Inscripciones/GetMisInscripcionesQueryTests.cs` (crear)

- [ ] **Step 1: Test anti-N+1**

Verifica que con 3 inscripciones, `GetInscripcionesActivasCountAsync` (por clase, individual) se llama **0 veces** y `GetConteoActivasPorClasesAsync` se llama **1 vez**.

- [ ] **Step 2: Verificar que falla** (el query actual llama el conteo en loop).

- [ ] **Step 3: Reescribir el query**

```csharp
public async Task<IEnumerable<InscripcionClaseDto>> ExecuteAsync(Guid socioId)
{
    var inscripciones = (await _inscripcionRepo.GetBySocioIdAsync(socioId)).ToList();
    var claseIds = inscripciones.Select(i => i.ClaseId).Distinct();
    var conteos = await _inscripcionRepo.GetConteoActivasPorClasesAsync(claseIds);

    return inscripciones.Select(i =>
    {
        var ocupados = conteos.GetValueOrDefault(i.ClaseId, 0);
        // "Mis inscripciones" muestra solo el flag EnListaEspera, no la posición exacta
        // (calcular la posición por inscripción reintroduciría N+1). La posición exacta
        // se devuelve solo al inscribirse (Task 4). Por eso pasamos null aquí.
        return InscripcionMapper.ToDto(i, i.Clase, ocupados, posicionListaEspera: null);
    });
}
```
> Notas:
> - `GetBySocioIdAsync` debe incluir las inscripciones en lista de espera (verificar el `Where` — no filtrar por `!EsListaEspera`).
> - Decisión: "Mis inscripciones" muestra "En lista de espera" sin número de posición, para evitar reintroducir el N+1 que esta tarea justamente elimina. El spec §3 define la posición 1-based, que se usa en la respuesta de la inscripción (Task 4), no en el listado.

- [ ] **Step 4: Verificar verde + commit**

```bash
git commit -am "perf(it4): fix N+1 en GetMisInscripcionesQuery (2 queries en vez de N+1)"
```

---

## Task 7: Controller + DI de inscripciones

**Files:**
- Modify: `backend/src/GymFlow.API/Controllers/InscripcionesController.cs`
- Verify: `backend/src/GymFlow.API/DependencyInjection.cs` (los commands ya están registrados; el contenedor resuelve las nuevas deps)

- [ ] **Step 1: Actualizar el controller**

- `Inscribirse` y `CancelarInscripcion` extraen `usuarioId`/`usuarioNombre` del JWT (helper `GetCurrentUser()` como en otros controllers) y los pasan a los commands.
- Agregar comentario documentando la decisión de mantener `[Authorize]` self-service (no `[RequierePermiso]`) porque el socio opera sobre sus propios datos y el command valida ownership.

- [ ] **Step 2: Build + verificar que toda la suite pasa**

Run: `dotnet build && dotnet test`
Expected: todos verdes (incluye tests viejos de inscripción si los hubiera — ajustar si la firma cambió).

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(it4): controller de inscripciones pasa usuario para auditoría"
```

---

## Task 8: Credenciales temporales por email para empleados

**Files:**
- Create: `backend/src/GymFlow.Application/Common/GeneradorPassword.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Empleados/EmpleadoEmailTemplates.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Empleados/CrearEmpleadoCommand.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/...CrearEmpleadoRequest`
- Modify: `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/CrearEmpleadoCommandTests.cs`
- Create: `backend/tests/GymFlow.Application.Tests/Common/GeneradorPasswordTests.cs`

- [ ] **Step 1: Test de `GeneradorPassword`**

```csharp
[Fact]
public void Generar_CumpleComposicion()
{
    var pw = GeneradorPassword.Generar();
    Assert.True(pw.Length >= 12);
    Assert.Contains(pw, char.IsUpper);
    Assert.Contains(pw, char.IsLower);
    Assert.Contains(pw, char.IsDigit);
}
```

- [ ] **Step 2: Implementar `GeneradorPassword`** (static, en `GymFlow.Application.Common`), usando `RandomNumberGenerator` para garantizar ≥12 chars con las 4 categorías.

- [ ] **Step 3: Crear `EmpleadoEmailTemplates.Bienvenida(empleado, passwordTemporal, rolNombre)`** con `HtmlEncode`.

- [ ] **Step 4: Actualizar tests de `CrearEmpleadoCommand`**

- Agregar mock de `IEmailService` al `Sut()`.
- Eliminar el test `PasswordCorta_LanzaArgumentException`.
- Quitar `Password` de los builders de `CrearEmpleadoRequest`.
- Happy path: verificar que se llamó `EnviarAsync` una vez y que el empleado se creó.

- [ ] **Step 5: Modificar `CrearEmpleadoCommand`**

- Quitar `Password` del request y la validación `MinPasswordLength`.
- Inyectar `IEmailService`.
- Autogenerar password → hashear → crear → enviar email (best-effort) → auditar (mencionar si email ok/fallo).

- [ ] **Step 6: Quitar el campo password del DTO request**

- [ ] **Step 7: Verificar suite verde**

Run: `dotnet test`
Expected: todos verdes (incluidos los tests de empleado actualizados).

- [ ] **Step 8: Commit**

```bash
git commit -am "feat(it4): credenciales temporales autogeneradas + email al crear empleado"
```

---

## Task 9: Frontend — quitar campo password de Nuevo Empleado + DTO de inscripción

**Files:**
- Modify: `frontend/src/pages/admin/NuevoUsuarioPage.tsx` (quitar input password)
- Modify: `frontend/src/services/empleados.ts` (request sin password)
- Modify: `frontend/src/types/...` (InscripcionClaseDto: `enListaEspera`, `posicionListaEspera`)
- Modify: `frontend/src/services/api.ts` o `inscripciones.ts`

- [ ] **Step 1: Quitar el input de password** del form de nuevo empleado + ajustar el tipo del request.
- [ ] **Step 2: Agregar `enListaEspera`/`posicionListaEspera`** al type del DTO de inscripción.
- [ ] **Step 3: Type-check + commit**

```bash
cd frontend && npx tsc --noEmit
git commit -am "feat(it4): frontend sin password manual de empleado + tipos de lista de espera"
```

---

## Task 10: Frontend — Catálogo de clases con filtros (portal socio)

**Files:**
- Create: `frontend/src/pages/portal/CatalogoClasesPage.tsx`
- Modify: `frontend/src/App.tsx` (ruta `/portal/clases`)
- Modify: `frontend/src/components/layout/SocioLayout.tsx` (link en nav)
- Modify: `frontend/src/services/...` (método para listar clases activas + inscribirse)

- [ ] **Step 1: Crear la página** con:
  - Query a `GET /api/clases?includeInactive=false`
  - Filtros: sede (Select), día (Select), búsqueda por nombre (Input)
  - Por clase: nombre, instructor, sede, `cuposDisponibles = capacidadMaxima - inscripcionesActivas`
  - Botón "Inscribirme" → `POST /api/inscripciones`; si lleno → "Anotarme en lista de espera"; si ya inscripto → deshabilitado
  - Toast de éxito + manejo de error 409 (cuota vencida) con el mensaje del backend
  - Responsive: tabla en desktop, cards en mobile (patrón de las otras vistas del portal)

- [ ] **Step 2: Ruta + link en SocioLayout** ("Clases").

- [ ] **Step 3: Type-check + commit**

```bash
npx tsc --noEmit
git commit -am "feat(it4): catálogo de clases con filtros en portal del socio"
```

---

## Task 11: Frontend — Horarios: filtro de sede obligatorio + split simple

**Files:**
- Modify: `frontend/src/pages/admin/HorariosPage.tsx`

- [ ] **Step 1: Filtro de sede obligatorio**

Estado inicial `unidadFilter = null`. Si `null`, renderizar empty state con 2 botones (Gimnasio Nuevo Malvín / Espacio Mora) en vez de la grilla. Al elegir → setear `unidadFilter` → renderizar grilla solo de esa unidad.

- [ ] **Step 2: Algoritmo de clustering de solapamiento**

Helper puro (testeable):
```typescript
function clustersDeSolapamiento(horarios: HorarioClase[]): HorarioClase[][] {
  const ordenados = [...horarios].sort((a,b) => a.horaInicio.localeCompare(b.horaInicio))
  const clusters: HorarioClase[][] = []
  for (const h of ordenados) {
    const ultimo = clusters[clusters.length - 1]
    const solapa = ultimo?.some(x => x.horaInicio < h.horaFin && h.horaInicio < x.horaFin)
    if (solapa) ultimo.push(h)
    else clusters.push([h])
  }
  return clusters
}
```

- [ ] **Step 3: Render con split**

Para cada cluster de N horarios, renderizar cada bloque con `width: ${100/N}%` y `left: ${(100/N)*i}%` dentro de la celda del día, manteniendo el posicionamiento vertical por hora.

- [ ] **Step 4: Type-check + commit**

```bash
npx tsc --noEmit
git commit -am "feat(it4): horarios con filtro de sede obligatorio y split de clases solapadas"
```

---

## Task 12: Verificación responsive + CI completo

**Files:** ninguno (verificación)

- [ ] **Step 1: Levantar la app** (`docker-compose up --build` + `npm run dev`) y verificar en DevTools mobile/tablet/desktop:
  - `CatalogoClasesPage` (nuevo)
  - `HorariosPage` (split — las columnas deben scrollear horizontal en mobile)
  - `MisInscripcionesPage`
- [ ] **Step 2: Correr pipeline local completo** (igual que CI):
```bash
cd backend && dotnet restore && dotnet build --no-restore && dotnet test --no-build -v minimal
cd ../frontend && npm ci && npm run build && npx vitest run
```
Expected: todo verde.
- [ ] **Step 3: Push de la rama + PR a develop**

```bash
git push -u origin feature/it4-spec-plan
gh pr create --base develop --title "feat: Iteración 4 — estabilización inscripciones, empleados y horarios" --body "Implementa el spec de IT4. Ver docs/specs/spec-it4-inscripciones-empleados-horarios.md"
```

---

## Resumen de archivos tocados

**Backend — Domain:** `InscripcionClase.cs`
**Backend — Application:** `ICuotaRepository`, `IInscripcionClaseRepository`, `IClaseRepository`, `InscribirSocioCommand`, `CancelarInscripcionCommand`, `GetMisInscripcionesQuery`, `InscripcionMapper`, `InscripcionClaseDtos`, `InscripcionEmailTemplates` (nuevo), `EmpleadoEmailTemplates` (nuevo), `CrearEmpleadoCommand`, `GeneradorPassword` (nuevo), `CrearEmpleadoRequest`
**Backend — Infrastructure:** `CuotaRepository`, `InscripcionClaseRepository`, `ClaseRepository`, `InscripcionClaseConfiguration`, migración `AddListaEsperaInscripcion`
**Backend — API:** `InscripcionesController`
**Backend — Tests:** `InscripcionClaseTests`, `InscribirSocioCommandTests`, `CancelarInscripcionCommandTests`, `GetMisInscripcionesQueryTests`, `GeneradorPasswordTests`, `CrearEmpleadoCommandTests` (actualizar)
**Frontend:** `CatalogoClasesPage` (nuevo), `HorariosPage`, `NuevoUsuarioPage`, `App.tsx`, `SocioLayout.tsx`, types y services
