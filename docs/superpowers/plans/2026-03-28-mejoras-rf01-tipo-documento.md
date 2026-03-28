# RF-01 Mejoras: Tipo de Documento y Validación de Cédula Uruguaya — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Agregar el campo `TipoDocumento` (CI / Pasaporte / Otro) como obligatorio en `Socio`, con validación del algoritmo de dígito verificador uruguayo cuando el tipo es CI, tanto en creación como en actualización.

**Architecture:** Validación en Domain (`Socio`), consistente con el patrón existente de `consentimientoInformado`. El enum vive en `Domain/Enums`. Los DTOs y commands de Application se actualizan para propagar el campo. Una migración EF Core agrega la columna con default `Otro (2)` para registros existentes.

**Tech Stack:** C# / .NET 8, EF Core Code-First, PostgreSQL 16, xUnit + Moq

---

## Archivos afectados

| Archivo | Acción |
|---------|--------|
| `backend/src/GymFlow.Domain/Enums/TipoDocumento.cs` | Crear |
| `backend/src/GymFlow.Domain/Entities/Socio.cs` | Modificar |
| `backend/src/GymFlow.Application/DTOs/CreateSocioRequest.cs` | Modificar |
| `backend/src/GymFlow.Application/DTOs/UpdateSocioRequest.cs` | Modificar |
| `backend/src/GymFlow.Application/DTOs/SocioDto.cs` | Modificar |
| `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs` | Modificar |
| `backend/src/GymFlow.Application/UseCases/Socios/UpdateSocioCommand.cs` | Modificar |
| `backend/tests/GymFlow.Domain.Tests/Entities/SocioTests.cs` | Crear |
| `backend/tests/GymFlow.Application.Tests/UseCases/CreateSocioCommandTests.cs` | Crear |
| `backend/src/GymFlow.Infrastructure/Persistence/Migrations/<timestamp>_AddTipoDocumentoToSocio.cs` | Crear (generado) |

---

## Task 1: Crear el enum TipoDocumento

**Files:**
- Create: `backend/src/GymFlow.Domain/Enums/TipoDocumento.cs`

- [ ] **Step 1: Crear el archivo del enum**

```csharp
namespace GymFlow.Domain.Enums;

public enum TipoDocumento
{
    CI = 0,
    Pasaporte = 1,
    Otro = 2
}
```

- [ ] **Step 2: Verificar que el proyecto compila**

```bash
cd backend
dotnet build src/GymFlow.Domain/GymFlow.Domain.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/src/GymFlow.Domain/Enums/TipoDocumento.cs
git commit -m "feat: agregar enum TipoDocumento (CI, Pasaporte, Otro)"
```

---

## Task 2: Escribir los tests de dominio (en rojo)

**Files:**
- Create: `backend/tests/GymFlow.Domain.Tests/Entities/SocioTests.cs`

> **Contexto del algoritmo de CI uruguaya:**
> Se elimina puntos y guiones, se paddea a 8 dígitos con cero a la izquierda si tiene 7.
> Se multiplican los primeros 7 dígitos por los pesos `[2, 9, 8, 7, 6, 3, 4]`, se suman.
> Es válida si `(suma + dígitoVerificador) % 10 == 0`.
>
> CIs de prueba verificadas:
> - `"54321163"` → válida (suma=117, verificador=3, (117+3)%10=0 ✓)
> - `"1234561"` → válida con 7 dígitos (paddeada: suma=109, verificador=1, (109+1)%10=0 ✓)
> - `"5.432.116-3"` → misma que `54321163` con formato ✓
> - `"12345678"` → inválida (suma=148, verificador=8, (148+8)%10=6 ✗)

- [ ] **Step 1: Crear SocioTests.cs**

```csharp
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class SocioTests
{
    // Helper: crea un Socio válido con el tipo y documento dados
    private static Socio CrearSocio(
        TipoDocumento tipoDocumento,
        string? documentoIdentidad) =>
        new Socio(
            nombre: "Juan",
            apellido: "García",
            correo: "juan@test.com",
            passwordHash: "hash",
            planId: null,
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: tipoDocumento,
            documentoIdentidad: documentoIdentidad);

    // --- Validación de cédula en constructor ---

    [Fact]
    public void Constructor_ConCI_YCedulaValida8Digitos_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.CI, "54321163"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ConCI_YCedulaValida7Digitos_NoLanzaExcepcion()
    {
        // 7 dígitos → se paddea: 01234561, suma=109, verificador=1, (109+1)%10=0
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.CI, "1234561"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ConCI_YCedulaConPuntosYGuion_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.CI, "5.432.116-3"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ConCI_YDigitoVerificadorIncorrecto_LanzaArgumentException()
    {
        // 12345678: suma=148, verificador esperado=2, recibido=8 → inválida
        Assert.Throws<ArgumentException>(() => CrearSocio(TipoDocumento.CI, "12345678"));
    }

    [Fact]
    public void Constructor_ConCI_YTextoNoNumerico_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearSocio(TipoDocumento.CI, "ABCDEFG1"));
    }

    [Fact]
    public void Constructor_ConCI_YDocumentoNull_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearSocio(TipoDocumento.CI, null));
    }

    [Fact]
    public void Constructor_ConCI_YDocumentoVacio_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearSocio(TipoDocumento.CI, ""));
    }

    // --- Tipos sin validación de cédula ---

    [Fact]
    public void Constructor_ConPasaporte_YDocumentoArbitrario_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.Pasaporte, "XY123456"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ConOtro_SinDocumento_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.Otro, null));
        Assert.Null(ex);
    }

    // --- Validación en ActualizarDatosSocio ---

    [Fact]
    public void ActualizarDatosSocio_ConCI_YCedulaInvalida_LanzaArgumentException()
    {
        var socio = CrearSocio(TipoDocumento.Otro, null);

        Assert.Throws<ArgumentException>(() =>
            socio.ActualizarDatosSocio(
                nombre: "Juan",
                apellido: "García",
                correo: "juan@test.com",
                planId: null,
                tipoDocumento: TipoDocumento.CI,
                documentoIdentidad: "12345678",
                telefono: null,
                fechaNacimiento: null));
    }

    [Fact]
    public void ActualizarDatosSocio_ConCI_YCedulaValida_NoLanzaExcepcion()
    {
        var socio = CrearSocio(TipoDocumento.Otro, null);

        var ex = Record.Exception(() =>
            socio.ActualizarDatosSocio(
                nombre: "Juan",
                apellido: "García",
                correo: "juan@test.com",
                planId: null,
                tipoDocumento: TipoDocumento.CI,
                documentoIdentidad: "54321163",
                telefono: null,
                fechaNacimiento: null));

        Assert.Null(ex);
    }
}
```

- [ ] **Step 2: Intentar compilar — esperamos error de compilación (Socio no tiene el parámetro todavía)**

```bash
cd backend
dotnet build tests/GymFlow.Domain.Tests/GymFlow.Domain.Tests.csproj
```

Expected: Error de compilación — `'Socio' does not contain a constructor that takes X arguments` o similar. Esto confirma que los tests están en rojo antes de implementar.

---

## Task 3: Actualizar `Socio.cs` con TipoDocumento y validación de cédula

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/Socio.cs`

> **Atención:** al cambiar la firma del constructor de `Socio`, `CreateSocioCommand` y `UpdateSocioCommand` dejarán de compilar. No hagas commit hasta completar Task 4.

- [ ] **Step 1: Reemplazar el contenido completo de Socio.cs**

```csharp
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Socio : Usuario
{
    public Guid? PlanId { get; private set; }
    public Plan? Plan { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public bool ConsentimientoInformado { get; private set; }
    public DateTime? ConsentimientoTimestamp { get; private set; }
    public string? Telefono { get; private set; }
    public TipoDocumento TipoDocumento { get; private set; }
    public string? DocumentoIdentidad { get; private set; }
    public DateTime? FechaNacimiento { get; private set; }
    public string? MotivoBaja { get; private set; }

    private Socio() { } // EF Core

    public Socio(
        string nombre,
        string apellido,
        string correo,
        string passwordHash,
        Guid? planId,
        DateTime fechaAlta,
        bool consentimientoInformado,
        TipoDocumento tipoDocumento,
        string? telefono = null,
        string? documentoIdentidad = null,
        DateTime? fechaNacimiento = null)
        : base(nombre, apellido, correo, passwordHash, Rol.Socio)
    {
        PlanId = planId;
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

    public void DarDeBaja(string? motivo)
    {
        MotivoBaja = motivo;
        Desactivar();
    }

    public void Reactivar()
    {
        MotivoBaja = null;
        Activar();
    }

    public void ActualizarDatosSocio(
        string nombre,
        string apellido,
        string correo,
        Guid? planId,
        TipoDocumento tipoDocumento,
        string? telefono,
        string? documentoIdentidad,
        DateTime? fechaNacimiento)
    {
        ActualizarDatosBase(nombre, apellido, correo);
        PlanId = planId;
        Telefono = telefono;
        FechaNacimiento = fechaNacimiento;

        ValidarDocumento(tipoDocumento, documentoIdentidad);
        TipoDocumento = tipoDocumento;
        DocumentoIdentidad = documentoIdentidad;
    }

    private static void ValidarDocumento(TipoDocumento tipoDocumento, string? documentoIdentidad)
    {
        if (tipoDocumento != TipoDocumento.CI)
            return;

        if (string.IsNullOrWhiteSpace(documentoIdentidad))
            throw new ArgumentException(
                "El documento de identidad es obligatorio cuando el tipo es CI.",
                nameof(documentoIdentidad));

        if (!EsCedulaUruguayaValida(documentoIdentidad))
            throw new ArgumentException(
                "El número de cédula de identidad uruguaya no es válido.",
                nameof(documentoIdentidad));
    }

    private static bool EsCedulaUruguayaValida(string doc)
    {
        // Normalizar: eliminar puntos y guiones
        var normalizado = doc.Replace(".", "").Replace("-", "").Trim();

        // Debe tener 7 u 8 dígitos numéricos
        if (normalizado.Length is < 7 or > 8 || !normalizado.All(char.IsDigit))
            return false;

        // Paddear a 8 dígitos con cero a la izquierda si tiene 7
        if (normalizado.Length == 7)
            normalizado = "0" + normalizado;

        // Pesos para los primeros 7 dígitos
        int[] weights = [2, 9, 8, 7, 6, 3, 4];

        int sum = 0;
        for (int i = 0; i < 7; i++)
            sum += (normalizado[i] - '0') * weights[i];

        int checkDigit = normalizado[7] - '0';
        return (sum + checkDigit) % 10 == 0;
    }
}
```

---

## Task 4: Actualizar DTOs y Commands (restaurar compilación)

**Files:**
- Modify: `backend/src/GymFlow.Application/DTOs/CreateSocioRequest.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/UpdateSocioRequest.cs`
- Modify: `backend/src/GymFlow.Application/DTOs/SocioDto.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/UpdateSocioCommand.cs`

- [ ] **Step 1: Actualizar CreateSocioRequest.cs**

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
    Guid? PlanId,
    List<Guid> UnidadIds,
    bool ConsentimientoInformado);
```

- [ ] **Step 2: Actualizar UpdateSocioRequest.cs**

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
    Guid? PlanId,
    List<Guid> UnidadIds);
```

- [ ] **Step 3: Actualizar SocioDto.cs**

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
    Guid? PlanId,
    string? PlanNombre,
    List<UnidadDto> Unidades);
```

- [ ] **Step 4: Actualizar CreateSocioCommand.cs**

En el método `ExecuteAsync`, agregar `tipoDocumento: request.TipoDocumento` al constructor de `Socio`:

```csharp
var socio = new Socio(
    nombre: request.Nombre,
    apellido: request.Apellido,
    correo: request.Correo,
    passwordHash: "PENDING_OAUTH",
    planId: request.PlanId,
    fechaAlta: DateTime.UtcNow,
    consentimientoInformado: request.ConsentimientoInformado,
    tipoDocumento: request.TipoDocumento,
    telefono: request.Telefono,
    documentoIdentidad: request.DocumentoIdentidad,
    fechaNacimiento: request.FechaNacimiento.HasValue
        ? DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc)
        : null);
```

En el método `MapToDto`, agregar `TipoDocumento: socio.TipoDocumento`:

```csharp
private static SocioDto MapToDto(Socio socio)
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
        PlanId: socio.PlanId,
        PlanNombre: socio.Plan?.Nombre,
        Unidades: socio.UnidadesAsignadas
            .Select(uu => new UnidadDto(uu.UnidadId, uu.Unidad?.Nombre ?? "", uu.Unidad?.Direccion ?? ""))
            .ToList());
}
```

- [ ] **Step 5: Actualizar UpdateSocioCommand.cs**

En el método `ExecuteAsync`, agregar `tipoDocumento: request.TipoDocumento` en la llamada a `ActualizarDatosSocio`:

```csharp
socio.ActualizarDatosSocio(
    nombre: request.Nombre,
    apellido: request.Apellido,
    correo: request.Correo,
    planId: request.PlanId,
    tipoDocumento: request.TipoDocumento,
    telefono: request.Telefono,
    documentoIdentidad: request.DocumentoIdentidad,
    fechaNacimiento: request.FechaNacimiento.HasValue
        ? DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc)
        : null);
```

En el método `MapToDto`, agregar `TipoDocumento: socio.TipoDocumento` (mismo código que en CreateSocioCommand):

```csharp
private static SocioDto MapToDto(Socio socio)
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
        PlanId: socio.PlanId,
        PlanNombre: socio.Plan?.Nombre,
        Unidades: socio.UnidadesAsignadas
            .Select(uu => new UnidadDto(uu.UnidadId, uu.Unidad?.Nombre ?? "", uu.Unidad?.Direccion ?? ""))
            .ToList());
}
```

- [ ] **Step 6: Compilar todo el backend**

```bash
cd backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Correr los tests de dominio — deben pasar**

```bash
cd backend
dotnet test tests/GymFlow.Domain.Tests/GymFlow.Domain.Tests.csproj --verbosity normal
```

Expected: todos los tests pasan (incluyendo los de `SocioTests` y los existentes de `UnidadTests`).

- [ ] **Step 8: Commit**

```bash
git add backend/src/GymFlow.Domain/Entities/Socio.cs \
        backend/src/GymFlow.Application/DTOs/CreateSocioRequest.cs \
        backend/src/GymFlow.Application/DTOs/UpdateSocioRequest.cs \
        backend/src/GymFlow.Application/DTOs/SocioDto.cs \
        backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs \
        backend/src/GymFlow.Application/UseCases/Socios/UpdateSocioCommand.cs \
        backend/tests/GymFlow.Domain.Tests/Entities/SocioTests.cs
git commit -m "feat: agregar TipoDocumento a Socio con validación de cédula uruguaya"
```

---

## Task 5: Escribir y correr los tests de Application

**Files:**
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/CreateSocioCommandTests.cs`

- [ ] **Step 1: Crear CreateSocioCommandTests.cs**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Socios;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases;

public class CreateSocioCommandTests
{
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IUnidadRepository> _unidadRepo = new();
    private readonly Mock<IPlanRepository> _planRepo = new();

    private CreateSocioCommand CrearCommand() =>
        new(_socioRepo.Object, _unidadRepo.Object, _planRepo.Object);

    private static Socio SocioFake(TipoDocumento tipo, string? doc) =>
        new("Juan", "García", "juan@test.com", "PENDING_OAUTH",
            null, DateTime.UtcNow, true, tipo, null, doc, null);

    private void ConfigurarMocksBase(Guid unidadId, TipoDocumento tipo, string? doc)
    {
        _socioRepo.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>())).ReturnsAsync(false);
        _unidadRepo.Setup(r => r.GetByIdAsync(unidadId))
            .ReturnsAsync(new Unidad("Gimnasio Nuevo Malvín", "Malvín, Montevideo"));
        _socioRepo.Setup(r => r.AddAsync(It.IsAny<Socio>())).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(SocioFake(tipo, doc));
    }

    [Fact]
    public async Task ExecuteAsync_ConCI_YCedulaValida_RetornaDtoConTipoDocumentoCorrecto()
    {
        var unidadId = Guid.NewGuid();
        ConfigurarMocksBase(unidadId, TipoDocumento.CI, "54321163");

        var request = new CreateSocioRequest(
            Nombre: "Juan",
            Apellido: "García",
            Correo: "juan@test.com",
            Telefono: null,
            TipoDocumento: TipoDocumento.CI,
            DocumentoIdentidad: "54321163",
            FechaNacimiento: null,
            PlanId: null,
            UnidadIds: [unidadId],
            ConsentimientoInformado: true);

        var result = await CrearCommand().ExecuteAsync(request);

        Assert.Equal(TipoDocumento.CI, result.TipoDocumento);
        Assert.Equal("54321163", result.DocumentoIdentidad);
    }

    [Fact]
    public async Task ExecuteAsync_ConCI_YCedulaInvalida_LanzaArgumentException()
    {
        var unidadId = Guid.NewGuid();
        // Solo necesitamos los mocks previos a la creación del Socio
        _socioRepo.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>())).ReturnsAsync(false);
        _unidadRepo.Setup(r => r.GetByIdAsync(unidadId))
            .ReturnsAsync(new Unidad("Gimnasio Nuevo Malvín", "Malvín, Montevideo"));

        var request = new CreateSocioRequest(
            Nombre: "Juan",
            Apellido: "García",
            Correo: "juan@test.com",
            Telefono: null,
            TipoDocumento: TipoDocumento.CI,
            DocumentoIdentidad: "12345678",  // inválida: suma=148, verificador esperado=2
            FechaNacimiento: null,
            PlanId: null,
            UnidadIds: [unidadId],
            ConsentimientoInformado: true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_ConPasaporte_RetornaDtoConTipoDocumentoCorrecto()
    {
        var unidadId = Guid.NewGuid();
        ConfigurarMocksBase(unidadId, TipoDocumento.Pasaporte, "XY123456");

        var request = new CreateSocioRequest(
            Nombre: "Juan",
            Apellido: "García",
            Correo: "juan@test.com",
            Telefono: null,
            TipoDocumento: TipoDocumento.Pasaporte,
            DocumentoIdentidad: "XY123456",
            FechaNacimiento: null,
            PlanId: null,
            UnidadIds: [unidadId],
            ConsentimientoInformado: true);

        var result = await CrearCommand().ExecuteAsync(request);

        Assert.Equal(TipoDocumento.Pasaporte, result.TipoDocumento);
    }

    [Fact]
    public async Task ExecuteAsync_ConOtro_SinDocumento_RetornaDtoConTipoDocumentoCorrecto()
    {
        var unidadId = Guid.NewGuid();
        ConfigurarMocksBase(unidadId, TipoDocumento.Otro, null);

        var request = new CreateSocioRequest(
            Nombre: "Juan",
            Apellido: "García",
            Correo: "juan@test.com",
            Telefono: null,
            TipoDocumento: TipoDocumento.Otro,
            DocumentoIdentidad: null,
            FechaNacimiento: null,
            PlanId: null,
            UnidadIds: [unidadId],
            ConsentimientoInformado: true);

        var result = await CrearCommand().ExecuteAsync(request);

        Assert.Equal(TipoDocumento.Otro, result.TipoDocumento);
        Assert.Null(result.DocumentoIdentidad);
    }
}
```

- [ ] **Step 2: Correr todos los tests**

```bash
cd backend
dotnet test --verbosity normal
```

Expected: todos los tests pasan. Verificar que aparecen los nuevos de `SocioTests` y `CreateSocioCommandTests`.

- [ ] **Step 3: Commit**

```bash
git add backend/tests/GymFlow.Application.Tests/UseCases/CreateSocioCommandTests.cs
git commit -m "test: agregar tests de CreateSocioCommand con TipoDocumento"
```

---

## Task 6: Generar y ajustar la migración EF Core

**Files:**
- Create: `backend/src/GymFlow.Infrastructure/Persistence/Migrations/<timestamp>_AddTipoDocumentoToSocio.cs` (generado automáticamente)

- [ ] **Step 1: Generar la migración**

```bash
cd backend
dotnet ef migrations add AddTipoDocumentoToSocio \
  --project src/GymFlow.Infrastructure \
  --startup-project src/GymFlow.API \
  --output-dir Persistence/Migrations
```

Expected: aparece un nuevo archivo `*_AddTipoDocumentoToSocio.cs` en `Persistence/Migrations/`.

- [ ] **Step 2: Verificar y ajustar el método Up() de la migración generada**

Abrir el archivo generado. El método `Up()` debe contener:

```csharp
migrationBuilder.AddColumn<int>(
    name: "TipoDocumento",
    table: "Usuarios",
    type: "integer",
    nullable: false,
    defaultValue: 2);  // 2 = Otro (para registros existentes)
```

Si EF Core generó `defaultValue: 0`, cambiarlo a `2`. El valor `2` corresponde a `TipoDocumento.Otro`, que es el default correcto para registros existentes cuyos documentos no fueron validados como cédulas.

- [ ] **Step 3: Verificar que el método Down() queda así**

```csharp
migrationBuilder.DropColumn(
    name: "TipoDocumento",
    table: "Usuarios");
```

- [ ] **Step 4: Compilar para verificar que la migración es válida**

```bash
cd backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/Persistence/Migrations/
git commit -m "chore: agregar migración TipoDocumento a Usuarios con default Otro"
```

---

## Task 7: Actualizar documentación

**Files:**
- Modify: `docs/agent_Context.md`
- Modify: `docs/GymFlow_Requerimientos_Completos.md`

- [ ] **Step 1: Agregar `TipoDocumento` a la tabla de entidades en `agent_Context.md`**

En la sección **Modelo de Dominio — Entidades Principales**, en la fila de `Socio`, actualizar para mencionar que tiene `TipoDocumento` (enum CI/Pasaporte/Otro) con validación de cédula uruguaya cuando tipo es CI.

También agregar en **Patrones de Diseño en Uso** o en las **Reglas para el Agente** una nota sobre el algoritmo de validación:

```markdown
> **Validación de cédula uruguaya (en `Socio.EsCedulaUruguayaValida`):**
> Normalizar eliminando puntos y guiones. Paddear a 8 dígitos. Pesos: [2,9,8,7,6,3,4].
> Válida si `(suma_ponderada + dígito_verificador) % 10 == 0`.
```

- [ ] **Step 2: Actualizar `GymFlow_Requerimientos_Completos.md`**

Buscar la sección de RF-01 (Alta de Socio) y agregar:

```markdown
**Campos adicionales (implementados en iteración 1):**
- `TipoDocumento` (enum requerido): CI | Pasaporte | Otro
  - Si `TipoDocumento == CI`: `DocumentoIdentidad` es obligatorio y debe ser una cédula uruguaya válida (algoritmo de dígito verificador).
  - Si `TipoDocumento == Pasaporte` u `Otro`: `DocumentoIdentidad` es opcional, sin validación de formato.
```

- [ ] **Step 3: Commit**

```bash
git add docs/agent_Context.md docs/GymFlow_Requerimientos_Completos.md
git commit -m "docs: documentar TipoDocumento y validación de cédula en RF-01"
```

---

## Verificación final

- [ ] Correr el suite completo de tests

```bash
cd backend
dotnet test --verbosity normal
```

Expected: todos los tests en verde.

- [ ] Levantar el entorno con Docker para verificar que la migración se aplica

```bash
docker compose down -v   # reset completo para partir de cero
docker compose up --build -d
docker compose logs -f api
```

Expected: en los logs de la API se ve `Applying migration 'AddTipoDocumentoToSocio'` sin errores.
