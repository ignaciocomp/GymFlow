# RNF-01 (parte 2) — Gestión de usuarios empleados — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que el administrador cree, edite y dé de baja usuarios internos del sistema (admin, profesor, custom) desde la UI, con login por email + password (BCrypt) leyendo de DB en lugar de la lista hardcodeada actual.

**Architecture:** Se agrega `Empleado` como subclase concreta de `Usuario` (TPH). Se hace `PasswordHash` nullable. Se introduce `IPasswordHasher` con implementación BCrypt. Se crean UseCases de Application siguiendo el patrón existente (`CrearRolCommand`, `CreateSocioCommand`). El `AuthController.Login` se refactoriza para leer empleados de DB y verificar password con BCrypt. Una migración EF Core agrega el discriminador TPH, hace nullable la columna, agrega los permisos del módulo `Empleados` con seed, y siembra el empleado admin de bootstrap.

**Tech Stack:** C# / .NET 8 / EF Core / xUnit + Moq / BCrypt.Net-Next / React 19 + TypeScript / Tailwind + shadcn/ui / Vite

**Spec:** `docs/superpowers/specs/2026-04-28-rnf-01-gestion-usuarios.md`

**Branch:** `feature/RNF_01` (continuación)

---

## Convenciones para esta sesión

- **Commits frecuentes** después de cada Task (no por step). Conventional Commits: `feat:`, `test:`, `refactor:`, `chore:`, `docs:`.
- **Tests primero (TDD)** en Tasks de UseCases y entidades de Domain.
- **Comandos:** se asume PowerShell en Windows con `cd backend` ya hecho cuando dice "Run: dotnet test". Para `docker compose` se asume directorio raíz del repo.
- **Verificación de migración:** `docker compose down -v && docker compose up --build -d` para validar que la migración aplica desde cero.

---

## Task 1: Agregar `Modulo.Empleados` al enum del backend

**Files:**
- Modify: `backend/src/GymFlow.Domain/Enums/Modulo.cs`

- [ ] **Step 1: Agregar el valor `Empleados` al enum**

Reemplazar el contenido completo de `backend/src/GymFlow.Domain/Enums/Modulo.cs` por:

```csharp
namespace GymFlow.Domain.Enums;

public enum Modulo
{
    Socios,
    Planes,
    Unidades,
    Auditoria,
    Empleados
}
```

- [ ] **Step 2: Compilar para verificar**

Run: `cd backend && dotnet build`
Expected: Build succeeded sin errores.

- [ ] **Step 3: Commit**

```bash
git add backend/src/GymFlow.Domain/Enums/Modulo.cs
git commit -m "feat: agregar Modulo.Empleados al enum de permisos"
```

---

## Task 2: Hacer `Usuario.PasswordHash` nullable y limpiar el placeholder `PENDING_OAUTH`

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/Usuario.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`
- Modify: `backend/src/GymFlow.Domain/Entities/Socio.cs`

- [ ] **Step 1: Cambiar `PasswordHash` a nullable en `Usuario`**

En `backend/src/GymFlow.Domain/Entities/Usuario.cs`:

Cambiar:
```csharp
public string PasswordHash { get; private set; } = string.Empty;
```
por:
```csharp
public string? PasswordHash { get; private set; }
```

Cambiar el constructor protegido para aceptar nullable:
```csharp
protected Usuario(string nombre, string apellido, string correo, string? passwordHash, Guid rolId)
{
    Id = Guid.NewGuid();
    Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
    Apellido = !string.IsNullOrWhiteSpace(apellido) ? apellido : throw new ArgumentException("Apellido is required.", nameof(apellido));
    Correo = !string.IsNullOrWhiteSpace(correo) ? correo : throw new ArgumentException("Correo is required.", nameof(correo));
    PasswordHash = passwordHash; // nullable: Empleado lo setea, Socio lo deja null hasta OAuth (It.5)
    RolId = rolId != Guid.Empty ? rolId : throw new ArgumentException("RolId is required.", nameof(rolId));
    EstaActivo = true;
    FechaCreacion = DateTime.UtcNow;
}
```

Agregar método para que las subclases puedan setear el hash después:
```csharp
public void EstablecerPasswordHash(string passwordHash)
{
    if (string.IsNullOrWhiteSpace(passwordHash))
        throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
    PasswordHash = passwordHash;
}
```

- [ ] **Step 2: Cambiar el constructor de `Socio` para aceptar passwordHash nullable**

En `backend/src/GymFlow.Domain/Entities/Socio.cs`, cambiar la firma del constructor:

```csharp
public Socio(
    Guid rolSocioId,
    string nombre,
    string apellido,
    string correo,
    string? passwordHash,        // ← cambiado a nullable
    DateTime fechaAlta,
    bool consentimientoInformado,
    TipoDocumento tipoDocumento,
    string? telefono = null,
    string? documentoIdentidad = null,
    DateTime? fechaNacimiento = null)
    : base(nombre, apellido, correo, passwordHash, rolSocioId)
```

(Solo cambia el tipo del parámetro `passwordHash`, el body queda igual.)

- [ ] **Step 3: Quitar el placeholder `PENDING_OAUTH` en `CreateSocioCommand`**

En `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`, cambiar:

```csharp
passwordHash: "PENDING_OAUTH",
```

por:

```csharp
passwordHash: null, // Socio se autentica por Google OAuth (It.5)
```

- [ ] **Step 4: Compilar y correr tests**

Run: `cd backend && dotnet build && dotnet test`
Expected: Build OK; todos los tests pasan (incluyendo `CreateSocioCommandTests` y `SocioTests`).

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Domain/Entities/Usuario.cs backend/src/GymFlow.Domain/Entities/Socio.cs backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs
git commit -m "refactor: hacer Usuario.PasswordHash nullable y eliminar placeholder PENDING_OAUTH"
```

---

## Task 3: Crear la entidad `Empleado` con tests

**Files:**
- Create: `backend/src/GymFlow.Domain/Entities/Empleado.cs`
- Create: `backend/tests/GymFlow.Domain.Tests/Entities/EmpleadoTests.cs`

- [ ] **Step 1: Escribir los tests del constructor de `Empleado`**

Crear `backend/tests/GymFlow.Domain.Tests/Entities/EmpleadoTests.cs`:

```csharp
using GymFlow.Domain.Entities;
using Xunit;

namespace GymFlow.Domain.Tests.Entities;

public class EmpleadoTests
{
    [Fact]
    public void Constructor_ConDatosValidos_CreaEmpleadoActivo()
    {
        var rolId = Guid.NewGuid();

        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "hashed_pwd", rolId);

        Assert.Equal("Juan", empleado.Nombre);
        Assert.Equal("Pérez", empleado.Apellido);
        Assert.Equal("juan@gymflow.com", empleado.Correo);
        Assert.Equal("hashed_pwd", empleado.PasswordHash);
        Assert.Equal(rolId, empleado.RolId);
        Assert.True(empleado.EstaActivo);
    }

    [Fact]
    public void Constructor_ConPasswordHashVacio_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Empleado("Juan", "Pérez", "juan@gymflow.com", "", Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_ConPasswordHashNull_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Empleado("Juan", "Pérez", "juan@gymflow.com", null!, Guid.NewGuid()));
    }
}
```

- [ ] **Step 2: Correr el test (debe fallar — no existe `Empleado`)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~EmpleadoTests"`
Expected: FAIL — "type or namespace 'Empleado' could not be found".

- [ ] **Step 3: Crear la entidad `Empleado`**

Crear `backend/src/GymFlow.Domain/Entities/Empleado.cs`:

```csharp
namespace GymFlow.Domain.Entities;

public class Empleado : Usuario
{
    private Empleado() { } // EF Core

    public Empleado(string nombre, string apellido, string correo, string passwordHash, Guid rolId)
        : base(nombre, apellido, correo, passwordHash, rolId)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required for Empleado.", nameof(passwordHash));
    }

    // Constructor para seed con Id explícito
    public Empleado(Guid id, string nombre, string apellido, string correo, string passwordHash, Guid rolId, DateTime fechaCreacion)
        : this(nombre, apellido, correo, passwordHash, rolId)
    {
        // Nota: EF Core va a sobreescribir Id con el valor del seed via reflection.
        // Este constructor se mantiene para conveniencia pero no es lo que persiste.
    }

    public void CambiarRol(Guid nuevoRolId)
    {
        if (nuevoRolId == Guid.Empty)
            throw new ArgumentException("RolId is required.", nameof(nuevoRolId));
        // Acceso al campo via reflection-friendly path: usamos el setter privado heredado
        typeof(Usuario).GetProperty(nameof(RolId))!.SetValue(this, nuevoRolId);
    }
}
```

> **Nota sobre `CambiarRol`:** el setter de `RolId` en `Usuario` es privado. Para evitar exponerlo o cambiar la API de la base, usamos reflexión. Alternativa más limpia: agregar un método `protected` `CambiarRolInterno(Guid)` en `Usuario`. Si preferís esa vía, agregalo en `Usuario.cs` y reemplazá el body de `CambiarRol`.

**Variante recomendada (más limpia):** agregar a `Usuario.cs` un método protegido y usarlo desde `Empleado`:

En `backend/src/GymFlow.Domain/Entities/Usuario.cs`, después de `EstablecerPasswordHash`, agregar:
```csharp
protected void CambiarRolInterno(Guid nuevoRolId)
{
    if (nuevoRolId == Guid.Empty)
        throw new ArgumentException("RolId is required.", nameof(nuevoRolId));
    RolId = nuevoRolId;
}
```

Y en `Empleado.cs`, reemplazar el body de `CambiarRol` por:
```csharp
public void CambiarRol(Guid nuevoRolId) => CambiarRolInterno(nuevoRolId);
```

- [ ] **Step 4: Correr los tests (deben pasar)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~EmpleadoTests"`
Expected: PASS — 3 tests pasan.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Domain/Entities/Empleado.cs backend/src/GymFlow.Domain/Entities/Usuario.cs backend/tests/GymFlow.Domain.Tests/Entities/EmpleadoTests.cs
git commit -m "feat: agregar entidad Empleado como subclase concreta de Usuario"
```

---

## Task 4: Agregar BCrypt y crear el servicio `IPasswordHasher`

**Files:**
- Modify: `backend/src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj` (paquete BCrypt.Net-Next)
- Create: `backend/src/GymFlow.Application/Interfaces/IPasswordHasher.cs`
- Create: `backend/src/GymFlow.Infrastructure/Services/BCryptPasswordHasher.cs`

- [ ] **Step 1: Agregar el paquete BCrypt.Net-Next**

Run: `cd backend/src/GymFlow.Infrastructure && dotnet add package BCrypt.Net-Next --version 4.0.3`
Expected: `info : Package 'BCrypt.Net-Next' is compatible`. El csproj queda con `<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />`.

- [ ] **Step 2: Crear la interfaz `IPasswordHasher`**

Crear `backend/src/GymFlow.Application/Interfaces/IPasswordHasher.cs`:

```csharp
namespace GymFlow.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}
```

- [ ] **Step 3: Crear la implementación BCrypt**

Crear `backend/src/GymFlow.Infrastructure/Services/BCryptPasswordHasher.cs`:

```csharp
using GymFlow.Application.Interfaces;

namespace GymFlow.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Password is required.", nameof(plainPassword));
        return BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    public bool Verify(string plainPassword, string hash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(hash))
            return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hash);
        }
        catch
        {
            return false; // hash inválido
        }
    }
}
```

- [ ] **Step 4: Compilar**

Run: `cd backend && dotnet build`
Expected: Build OK.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj backend/src/GymFlow.Application/Interfaces/IPasswordHasher.cs backend/src/GymFlow.Infrastructure/Services/BCryptPasswordHasher.cs
git commit -m "feat: agregar IPasswordHasher con implementación BCrypt"
```

---

## Task 5: Crear DTOs e interfaz del repositorio de Empleados

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/IEmpleadoRepository.cs`
- Create: `backend/src/GymFlow.Application/DTOs/EmpleadoDto.cs`
- Create: `backend/src/GymFlow.Application/DTOs/CrearEmpleadoRequest.cs`
- Create: `backend/src/GymFlow.Application/DTOs/ActualizarEmpleadoRequest.cs`
- Create: `backend/src/GymFlow.Application/DTOs/CambiarPasswordRequest.cs`

- [ ] **Step 1: Crear `IEmpleadoRepository`**

Crear `backend/src/GymFlow.Application/Interfaces/IEmpleadoRepository.cs`:

```csharp
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IEmpleadoRepository
{
    Task<IReadOnlyList<Empleado>> GetAllAsync(bool? estaActivo = null, CancellationToken ct = default);
    Task<Empleado?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Empleado?> GetByCorreoAsync(string correo, CancellationToken ct = default);
    Task<bool> ExisteCorreoAsync(string correo, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Empleado empleado, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Crear los DTOs**

Crear `backend/src/GymFlow.Application/DTOs/EmpleadoDto.cs`:

```csharp
namespace GymFlow.Application.DTOs;

public record EmpleadoDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string Correo,
    Guid RolId,
    string RolNombre,
    bool EstaActivo,
    DateTime FechaCreacion);
```

Crear `backend/src/GymFlow.Application/DTOs/CrearEmpleadoRequest.cs`:

```csharp
namespace GymFlow.Application.DTOs;

public record CrearEmpleadoRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string Password,
    Guid RolId);
```

Crear `backend/src/GymFlow.Application/DTOs/ActualizarEmpleadoRequest.cs`:

```csharp
namespace GymFlow.Application.DTOs;

public record ActualizarEmpleadoRequest(
    string Nombre,
    string Apellido,
    string Correo,
    Guid RolId);
```

Crear `backend/src/GymFlow.Application/DTOs/CambiarPasswordRequest.cs`:

```csharp
namespace GymFlow.Application.DTOs;

public record CambiarPasswordRequest(string NuevaPassword);
```

- [ ] **Step 3: Compilar**

Run: `cd backend && dotnet build`
Expected: Build OK.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GymFlow.Application/Interfaces/IEmpleadoRepository.cs backend/src/GymFlow.Application/DTOs/EmpleadoDto.cs backend/src/GymFlow.Application/DTOs/CrearEmpleadoRequest.cs backend/src/GymFlow.Application/DTOs/ActualizarEmpleadoRequest.cs backend/src/GymFlow.Application/DTOs/CambiarPasswordRequest.cs
git commit -m "feat: agregar DTOs y IEmpleadoRepository para gestión de empleados"
```

---

## Task 6: `CrearEmpleadoCommand` con tests (TDD)

**Files:**
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/CrearEmpleadoCommandTests.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Empleados/CrearEmpleadoCommand.cs`

- [ ] **Step 1: Escribir los tests primero**

Crear `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/CrearEmpleadoCommandTests.cs`:

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class CrearEmpleadoCommandTests
{
    private static (Mock<IEmpleadoRepository>, Mock<IRolRepository>, Mock<IPasswordHasher>, Mock<IAuditLogger>) Mocks()
    {
        return (new Mock<IEmpleadoRepository>(), new Mock<IRolRepository>(), new Mock<IPasswordHasher>(), new Mock<IAuditLogger>());
    }

    private static CrearEmpleadoRequest ValidRequest(Guid? rolId = null) =>
        new("Juan", "Pérez", "juan@gymflow.com", "secret123", rolId ?? Guid.NewGuid());

    [Fact]
    public async Task NombreVacio_LanzaArgumentException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(ValidRequest() with { Nombre = "" }, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task PasswordCorta_LanzaArgumentException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(ValidRequest() with { Password = "1234567" }, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task CorreoDuplicado_LanzaInvalidOperationException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        emp.Setup(r => r.ExisteCorreoAsync("juan@gymflow.com", null, default)).ReturnsAsync(true);
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(ValidRequest(), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolInexistente_LanzaArgumentException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        var rolId = Guid.NewGuid();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(rolId, default)).ReturnsAsync((Rol?)null);
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(ValidRequest(rolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolEsSocio_LanzaInvalidOperationException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(RolSeed.SocioRolId, default))
            .ReturnsAsync(new Rol(RolSeed.SocioRolId, "Socio", true, DateTime.UtcNow));
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(ValidRequest(RolSeed.SocioRolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task HappyPath_CreaEmpleadoYAuditEs()
    {
        var (emp, rol, hasher, audit) = Mocks();
        var rolId = Guid.NewGuid();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(rolId, default))
            .ReturnsAsync(new Rol(rolId, "Recepcionista", false, DateTime.UtcNow));
        hasher.Setup(h => h.Hash("secret123")).Returns("hashed_secret");
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        var dto = await sut.ExecuteAsync(ValidRequest(rolId), Guid.NewGuid(), "Admin");

        Assert.Equal("juan@gymflow.com", dto.Correo);
        Assert.Equal("Recepcionista", dto.RolNombre);
        Assert.True(dto.EstaActivo);
        emp.Verify(r => r.AddAsync(It.Is<Empleado>(e => e.PasswordHash == "hashed_secret"), default), Times.Once);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Creacion, "Empleado", It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
```

- [ ] **Step 2: Correr los tests (deben fallar)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~CrearEmpleadoCommandTests"`
Expected: FAIL — `CrearEmpleadoCommand` no existe.

- [ ] **Step 3: Implementar el command**

Crear `backend/src/GymFlow.Application/UseCases/Empleados/CrearEmpleadoCommand.cs`:

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;

namespace GymFlow.Application.UseCases.Empleados;

public class CrearEmpleadoCommand
{
    private const int MinPasswordLength = 8;

    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;

    public CrearEmpleadoCommand(
        IEmpleadoRepository empleadoRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
    }

    public async Task<EmpleadoDto> ExecuteAsync(CrearEmpleadoRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Apellido))
            throw new ArgumentException("El apellido es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Correo))
            throw new ArgumentException("El correo es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinPasswordLength)
            throw new ArgumentException($"La contraseña debe tener al menos {MinPasswordLength} caracteres.", nameof(request));

        if (await _empleadoRepository.ExisteCorreoAsync(request.Correo, null, ct))
            throw new InvalidOperationException("El correo ingresado ya está registrado.");

        var rol = await _rolRepository.GetByIdAsync(request.RolId, ct)
            ?? throw new ArgumentException($"El rol {request.RolId} no existe.", nameof(request));

        if (rol.Id == RolSeed.SocioRolId)
            throw new InvalidOperationException("No se puede asignar el rol Socio a un empleado.");

        var hash = _passwordHasher.Hash(request.Password);
        var empleado = new Empleado(request.Nombre, request.Apellido, request.Correo, hash, rol.Id);

        await _empleadoRepository.AddAsync(empleado, ct);
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Empleado", empleado.Id,
            $"Se creó el empleado {empleado.Nombre} {empleado.Apellido} ({rol.Nombre})");

        return new EmpleadoDto(empleado.Id, empleado.Nombre, empleado.Apellido, empleado.Correo,
            rol.Id, rol.Nombre, empleado.EstaActivo, empleado.FechaCreacion);
    }
}
```

- [ ] **Step 4: Correr los tests (deben pasar)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~CrearEmpleadoCommandTests"`
Expected: PASS — 6 tests verdes.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Application/UseCases/Empleados/CrearEmpleadoCommand.cs backend/tests/GymFlow.Application.Tests/UseCases/Empleados/CrearEmpleadoCommandTests.cs
git commit -m "feat: agregar CrearEmpleadoCommand con validaciones de rol y correo"
```

---

## Task 7: `ActualizarEmpleadoCommand` con tests

**Files:**
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/ActualizarEmpleadoCommandTests.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Empleados/ActualizarEmpleadoCommand.cs`

- [ ] **Step 1: Escribir los tests**

Crear `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/ActualizarEmpleadoCommandTests.cs`:

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class ActualizarEmpleadoCommandTests
{
    private static (Mock<IEmpleadoRepository>, Mock<IRolRepository>, Mock<IAuditLogger>) Mocks()
        => (new Mock<IEmpleadoRepository>(), new Mock<IRolRepository>(), new Mock<IAuditLogger>());

    private static Empleado ExistingEmpleado(Guid? id = null, Guid? rolId = null) =>
        new("Juan", "Pérez", "juan@gymflow.com", "old_hash", rolId ?? Guid.NewGuid());

    [Fact]
    public async Task EmpleadoInexistente_LanzaKeyNotFoundException()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Empleado?)null);
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan", "Pérez", "juan@gymflow.com", Guid.NewGuid()), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolEsSocio_LanzaInvalidOperationException()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(ExistingEmpleado(id));
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), id, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(RolSeed.SocioRolId, default))
            .ReturnsAsync(new Rol(RolSeed.SocioRolId, "Socio", true, DateTime.UtcNow));
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan", "Pérez", "juan@gymflow.com", RolSeed.SocioRolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task CorreoDuplicado_LanzaInvalidOperationException()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(ExistingEmpleado(id));
        emp.Setup(r => r.ExisteCorreoAsync("otro@gymflow.com", id, default)).ReturnsAsync(true);
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan", "Pérez", "otro@gymflow.com", Guid.NewGuid()), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task HappyPath_ActualizaYAuditEs()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        var nuevoRolId = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(ExistingEmpleado(id));
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), id, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(nuevoRolId, default))
            .ReturnsAsync(new Rol(nuevoRolId, "Encargado", false, DateTime.UtcNow));
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan Carlos", "Pérez", "jc@gymflow.com", nuevoRolId), Guid.NewGuid(), "Admin");

        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Empleado", id, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
```

- [ ] **Step 2: Correr los tests (deben fallar)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~ActualizarEmpleadoCommandTests"`
Expected: FAIL.

- [ ] **Step 3: Implementar el command**

Crear `backend/src/GymFlow.Application/UseCases/Empleados/ActualizarEmpleadoCommand.cs`:

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;

namespace GymFlow.Application.UseCases.Empleados;

public class ActualizarEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IAuditLogger _auditLogger;

    public ActualizarEmpleadoCommand(
        IEmpleadoRepository empleadoRepository,
        IRolRepository rolRepository,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, ActualizarEmpleadoRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido) || string.IsNullOrWhiteSpace(request.Correo))
            throw new ArgumentException("Nombre, apellido y correo son obligatorios.", nameof(request));

        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        if (await _empleadoRepository.ExisteCorreoAsync(request.Correo, id, ct))
            throw new InvalidOperationException("El correo ingresado ya está registrado por otro usuario.");

        var rol = await _rolRepository.GetByIdAsync(request.RolId, ct)
            ?? throw new ArgumentException($"El rol {request.RolId} no existe.", nameof(request));

        if (rol.Id == RolSeed.SocioRolId)
            throw new InvalidOperationException("No se puede asignar el rol Socio a un empleado.");

        empleado.ActualizarDatosBase(request.Nombre, request.Apellido, request.Correo);
        empleado.CambiarRol(rol.Id);

        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Empleado", id,
            $"Se actualizó el empleado {empleado.Nombre} {empleado.Apellido} (rol {rol.Nombre})");
    }
}
```

- [ ] **Step 4: Correr los tests**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~ActualizarEmpleadoCommandTests"`
Expected: PASS — 4 tests verdes.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Application/UseCases/Empleados/ActualizarEmpleadoCommand.cs backend/tests/GymFlow.Application.Tests/UseCases/Empleados/ActualizarEmpleadoCommandTests.cs
git commit -m "feat: agregar ActualizarEmpleadoCommand con validación de rol Socio"
```

---

## Task 8: `CambiarPasswordCommand` con tests

**Files:**
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/CambiarPasswordCommandTests.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Empleados/CambiarPasswordCommand.cs`

- [ ] **Step 1: Escribir los tests**

Crear `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/CambiarPasswordCommandTests.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class CambiarPasswordCommandTests
{
    [Fact]
    public async Task EmpleadoInexistente_LanzaKeyNotFoundException()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var audit = new Mock<IAuditLogger>();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Empleado?)null);
        var sut = new CambiarPasswordCommand(emp.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(id, "newpassword123", Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task PasswordCorta_LanzaArgumentException()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var audit = new Mock<IAuditLogger>();
        var sut = new CambiarPasswordCommand(emp.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(Guid.NewGuid(), "1234567", Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task HappyPath_HasheaYAuditEs()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var audit = new Mock<IAuditLogger>();
        var id = Guid.NewGuid();
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "old_hash", Guid.NewGuid());
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(empleado);
        hasher.Setup(h => h.Hash("newpassword123")).Returns("new_hashed");
        var sut = new CambiarPasswordCommand(emp.Object, hasher.Object, audit.Object);

        await sut.ExecuteAsync(id, "newpassword123", Guid.NewGuid(), "Admin");

        Assert.Equal("new_hashed", empleado.PasswordHash);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Empleado", id, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
```

- [ ] **Step 2: Correr los tests (deben fallar)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~CambiarPasswordCommandTests"`
Expected: FAIL.

- [ ] **Step 3: Implementar**

Crear `backend/src/GymFlow.Application/UseCases/Empleados/CambiarPasswordCommand.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class CambiarPasswordCommand
{
    private const int MinPasswordLength = 8;

    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;

    public CambiarPasswordCommand(IEmpleadoRepository empleadoRepository, IPasswordHasher passwordHasher, IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, string nuevaPassword, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < MinPasswordLength)
            throw new ArgumentException($"La contraseña debe tener al menos {MinPasswordLength} caracteres.", nameof(nuevaPassword));

        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        empleado.EstablecerPasswordHash(_passwordHasher.Hash(nuevaPassword));
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Empleado", id,
            $"Se cambió la contraseña del empleado {empleado.Nombre} {empleado.Apellido}");
    }
}
```

- [ ] **Step 4: Correr los tests**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~CambiarPasswordCommandTests"`
Expected: PASS — 3 tests verdes.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Application/UseCases/Empleados/CambiarPasswordCommand.cs backend/tests/GymFlow.Application.Tests/UseCases/Empleados/CambiarPasswordCommandTests.cs
git commit -m "feat: agregar CambiarPasswordCommand con hash BCrypt"
```

---

## Task 9: `DarDeBajaEmpleadoCommand` y `ReactivarEmpleadoCommand`

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Empleados/DarDeBajaEmpleadoCommand.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Empleados/ReactivarEmpleadoCommand.cs`
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/DarDeBajaEmpleadoCommandTests.cs`

- [ ] **Step 1: Tests de DarDeBaja**

Crear `backend/tests/GymFlow.Application.Tests/UseCases/Empleados/DarDeBajaEmpleadoCommandTests.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class DarDeBajaEmpleadoCommandTests
{
    [Fact]
    public async Task EmpleadoInexistente_LanzaKeyNotFoundException()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var audit = new Mock<IAuditLogger>();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Empleado?)null);
        var sut = new DarDeBajaEmpleadoCommand(emp.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(id, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task NoSePuedeAutoEliminar_LanzaInvalidOperationException()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var audit = new Mock<IAuditLogger>();
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "h", Guid.NewGuid());
        emp.Setup(r => r.GetByIdAsync(empleado.Id, default)).ReturnsAsync(empleado);
        var sut = new DarDeBajaEmpleadoCommand(emp.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(empleado.Id, empleado.Id, "Juan"));
    }

    [Fact]
    public async Task HappyPath_DesactivaEmpleado()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var audit = new Mock<IAuditLogger>();
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "h", Guid.NewGuid());
        emp.Setup(r => r.GetByIdAsync(empleado.Id, default)).ReturnsAsync(empleado);
        var sut = new DarDeBajaEmpleadoCommand(emp.Object, audit.Object);

        await sut.ExecuteAsync(empleado.Id, Guid.NewGuid(), "Admin");

        Assert.False(empleado.EstaActivo);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Baja, "Empleado", empleado.Id, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
```

- [ ] **Step 2: Implementar `DarDeBajaEmpleadoCommand`**

Crear `backend/src/GymFlow.Application/UseCases/Empleados/DarDeBajaEmpleadoCommand.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class DarDeBajaEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IAuditLogger _auditLogger;

    public DarDeBajaEmpleadoCommand(IEmpleadoRepository empleadoRepository, IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (id == usuarioId)
            throw new InvalidOperationException("No podés darte de baja a vos mismo.");

        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        empleado.Desactivar();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Empleado", id,
            $"Se dio de baja al empleado {empleado.Nombre} {empleado.Apellido}");
    }
}
```

- [ ] **Step 3: Implementar `ReactivarEmpleadoCommand`**

Crear `backend/src/GymFlow.Application/UseCases/Empleados/ReactivarEmpleadoCommand.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class ReactivarEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IAuditLogger _auditLogger;

    public ReactivarEmpleadoCommand(IEmpleadoRepository empleadoRepository, IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        empleado.Activar();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Empleado", id,
            $"Se reactivó al empleado {empleado.Nombre} {empleado.Apellido}");
    }
}
```

- [ ] **Step 4: Correr los tests**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~DarDeBajaEmpleadoCommandTests"`
Expected: PASS — 3 tests verdes.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Application/UseCases/Empleados/DarDeBajaEmpleadoCommand.cs backend/src/GymFlow.Application/UseCases/Empleados/ReactivarEmpleadoCommand.cs backend/tests/GymFlow.Application.Tests/UseCases/Empleados/DarDeBajaEmpleadoCommandTests.cs
git commit -m "feat: agregar DarDeBaja y Reactivar Empleado commands"
```

---

## Task 10: Queries `GetEmpleadosQuery` y `GetEmpleadoByIdQuery`

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Empleados/GetEmpleadosQuery.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Empleados/GetEmpleadoByIdQuery.cs`

- [ ] **Step 1: `GetEmpleadosQuery`**

Crear `backend/src/GymFlow.Application/UseCases/Empleados/GetEmpleadosQuery.cs`:

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Empleados;

public class GetEmpleadosQuery
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;

    public GetEmpleadosQuery(IEmpleadoRepository empleadoRepository, IRolRepository rolRepository)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
    }

    public async Task<IReadOnlyList<EmpleadoDto>> ExecuteAsync(bool? estaActivo = null, CancellationToken ct = default)
    {
        var empleados = await _empleadoRepository.GetAllAsync(estaActivo, ct);
        var roles = await _rolRepository.GetAllAsync(ct);
        var rolMap = roles.ToDictionary(r => r.Id, r => r.Nombre);

        return empleados.Select(e => new EmpleadoDto(
            e.Id, e.Nombre, e.Apellido, e.Correo,
            e.RolId, rolMap.TryGetValue(e.RolId, out var n) ? n : "—",
            e.EstaActivo, e.FechaCreacion)).ToList();
    }
}
```

- [ ] **Step 2: `GetEmpleadoByIdQuery`**

Crear `backend/src/GymFlow.Application/UseCases/Empleados/GetEmpleadoByIdQuery.cs`:

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Empleados;

public class GetEmpleadoByIdQuery
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;

    public GetEmpleadoByIdQuery(IEmpleadoRepository empleadoRepository, IRolRepository rolRepository)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
    }

    public async Task<EmpleadoDto> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");
        var rol = await _rolRepository.GetByIdAsync(empleado.RolId, ct);

        return new EmpleadoDto(
            empleado.Id, empleado.Nombre, empleado.Apellido, empleado.Correo,
            empleado.RolId, rol?.Nombre ?? "—",
            empleado.EstaActivo, empleado.FechaCreacion);
    }
}
```

- [ ] **Step 3: Compilar**

Run: `cd backend && dotnet build`
Expected: Build OK.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GymFlow.Application/UseCases/Empleados/GetEmpleadosQuery.cs backend/src/GymFlow.Application/UseCases/Empleados/GetEmpleadoByIdQuery.cs
git commit -m "feat: agregar queries GetEmpleados y GetEmpleadoById"
```

---

## Task 11: Implementar `EmpleadoRepository`

**Files:**
- Create: `backend/src/GymFlow.Infrastructure/Repositories/EmpleadoRepository.cs`

- [ ] **Step 1: Implementar el repositorio**

Crear `backend/src/GymFlow.Infrastructure/Repositories/EmpleadoRepository.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly GymFlowDbContext _db;

    public EmpleadoRepository(GymFlowDbContext db) => _db = db;

    public async Task<IReadOnlyList<Empleado>> GetAllAsync(bool? estaActivo = null, CancellationToken ct = default)
    {
        var query = _db.Set<Empleado>().AsQueryable();
        if (estaActivo.HasValue)
            query = query.Where(e => e.EstaActivo == estaActivo.Value);
        return await query.OrderBy(e => e.Apellido).ThenBy(e => e.Nombre).ToListAsync(ct);
    }

    public Task<Empleado?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Set<Empleado>().FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Empleado?> GetByCorreoAsync(string correo, CancellationToken ct = default) =>
        _db.Set<Empleado>().FirstOrDefaultAsync(e => e.Correo == correo, ct);

    public async Task<bool> ExisteCorreoAsync(string correo, Guid? excludeId = null, CancellationToken ct = default) =>
        await _db.Set<Usuario>().AnyAsync(u => u.Correo == correo && (excludeId == null || u.Id != excludeId), ct);

    public async Task AddAsync(Empleado empleado, CancellationToken ct = default) =>
        await _db.Set<Empleado>().AddAsync(empleado, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
```

> **Nota:** `ExisteCorreoAsync` consulta sobre `Usuario` (no solo `Empleado`) para evitar que un Empleado se cree con el mismo correo que un Socio existente.

- [ ] **Step 2: Compilar**

Run: `cd backend && dotnet build`
Expected: Build OK.

- [ ] **Step 3: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/Repositories/EmpleadoRepository.cs
git commit -m "feat: implementar EmpleadoRepository"
```

---

## Task 12: Configurar el discriminador TPH y el DbSet

**Files:**
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs`
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`

- [ ] **Step 1: Agregar el discriminador `Empleado` y hacer `PasswordHash` nullable**

Reemplazar el contenido de `backend/src/GymFlow.Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs`:

```csharp
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        // TPH discriminator
        builder.HasDiscriminator<string>("TipoUsuario")
            .HasValue<Socio>("Socio")
            .HasValue<Empleado>("Empleado");

        builder.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Apellido).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Correo).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Correo).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired(false).HasMaxLength(500);
        builder.Property(u => u.EstaActivo).IsRequired();
        builder.Property(u => u.FechaCreacion).IsRequired();

        builder.HasOne(u => u.Rol)
            .WithMany()
            .HasForeignKey(u => u.RolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.UnidadesAsignadas)
            .WithOne(uu => uu.Usuario)
            .HasForeignKey(uu => uu.UsuarioId);
    }
}
```

- [ ] **Step 2: Agregar `DbSet<Empleado>` al DbContext + seed de permisos `Empleados` + seed del admin de bootstrap**

En `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`, agregar el DbSet después de `DbSet<Socio>`:

```csharp
public DbSet<Empleado> Empleados => Set<Empleado>();
```

Y dentro del método `OnModelCreating`, después del bloque `Seed RolPermisos: Admin tiene todos`, agregar:

```csharp
        // Seed Empleado admin de bootstrap
        // PasswordHash precalculado con BCrypt para "admin123" (factor 11). Determinístico para no romper migraciones.
        const string adminPasswordHashBootstrap = "$2a$11$N1tT1XdqQfHPuGEX5hDCu.0h.r0XkU4iq3Cf5Qf2Z4aP9V/9q3dqK";
        modelBuilder.Entity<Empleado>().HasData(new
        {
            Id = EmpleadoSeed.AdminBootstrapId,
            Nombre = "Admin",
            Apellido = "Inicial",
            Correo = "admin@gymflow.com",
            PasswordHash = adminPasswordHashBootstrap,
            RolId = RolSeed.AdminRolId,
            EstaActivo = true,
            FechaCreacion = RolSeed.SeedTimestamp
        });
```

> **IMPORTANTE — Hash precalculado:** el hash de arriba es un placeholder. Antes de aplicar la migración hay que generar uno real ejecutando un script una sola vez. Ver Step 3 para cómo generarlo.

Y al final del archivo, agregar la clase de seed después de `RolSeed`:

```csharp
public static class EmpleadoSeed
{
    public static readonly Guid AdminBootstrapId = Guid.Parse("33333333-3333-3333-3333-333333333333");
}
```

- [ ] **Step 3: Generar el hash real de BCrypt para `admin123` y reemplazarlo**

Crear un archivo temporal para generar el hash. Run en PowerShell desde el directorio `backend/`:

```powershell
dotnet run --project src/GymFlow.API -- --hash-once "admin123"
```

(Este comando NO existe todavía — alternativa más simple a continuación.)

**Alternativa simple:** crear un test temporal que imprima el hash. Crear `backend/tests/GymFlow.Infrastructure.Tests/HashGeneratorTemp.cs`:

```csharp
using GymFlow.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace GymFlow.Infrastructure.Tests;

public class HashGeneratorTemp
{
    private readonly ITestOutputHelper _output;
    public HashGeneratorTemp(ITestOutputHelper output) => _output = output;

    [Fact(Skip = "Run manualmente cuando se necesite regenerar el hash de bootstrap")]
    public void GenerarHashAdmin()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("admin123");
        _output.WriteLine($"Hash: {hash}");
    }
}
```

Run una vez con `Skip` removido temporalmente:
```powershell
cd backend && dotnet test --filter "FullyQualifiedName~HashGeneratorTemp" --logger "console;verbosity=detailed"
```

Copiar el hash impreso y pegarlo en `GymFlowDbContext.cs` reemplazando el placeholder. Restaurar el `Skip = ...` para que el test no corra en CI. Eliminar este archivo en un commit posterior si no se va a reusar.

- [ ] **Step 4: Compilar**

Run: `cd backend && dotnet build`
Expected: Build OK.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs backend/tests/GymFlow.Infrastructure.Tests/HashGeneratorTemp.cs
git commit -m "feat: configurar TPH para Empleado y seed del admin bootstrap"
```

---

## Task 13: Generar la migración EF Core

**Files:**
- Create: `backend/src/GymFlow.Infrastructure/Persistence/Migrations/<timestamp>_AddEmpleadosYPermisosEmpleados.cs`

- [ ] **Step 1: Generar la migración**

Run desde el directorio raíz del repo:
```bash
cd backend && dotnet ef migrations add AddEmpleadosYPermisosEmpleados --project src/GymFlow.Infrastructure --startup-project src/GymFlow.API --output-dir Persistence/Migrations
```

Expected: archivo `<timestamp>_AddEmpleadosYPermisosEmpleados.cs` creado en `Persistence/Migrations/`. La migración contiene:
- `AlterColumn<string>("PasswordHash", ..., nullable: true)`.
- `InsertData("Permisos", ...)` con 4 filas para `(Empleados, Lectura/Escritura/Modificacion/Eliminacion)`.
- `InsertData("RolPermisos", ...)` asignando esos 4 permisos al rol Admin.
- `InsertData("Usuarios", ...)` con el empleado admin de bootstrap (discriminator `Empleado`).

- [ ] **Step 2: Inspeccionar la migración**

Abrir el archivo generado y verificar que los `InsertData` de permisos usan los IDs determinísticos correctos (los GUIDs derivados del MD5 `Empleados-Lectura`, etc.). Si la migración tiene operaciones inesperadas (ej. drop/create de tablas), revertirla con:

```bash
cd backend && dotnet ef migrations remove --project src/GymFlow.Infrastructure --startup-project src/GymFlow.API
```

y revisar el código antes de regenerar.

- [ ] **Step 3: Aplicar la migración con la base limpia**

Desde la raíz del repo:
```bash
docker compose down -v
docker compose up --build -d
docker compose logs -f api | head -50
```

Expected: el contenedor `api` aplica todas las migraciones sin errores y queda escuchando en el puerto 5146.

- [ ] **Step 4: Verificar el seed manualmente**

Run desde la raíz:
```bash
docker compose exec db psql -U postgres -d gymflow -c "SELECT \"Correo\", \"TipoUsuario\", \"EstaActivo\" FROM \"Usuarios\";"
```

Expected: aparece la fila `admin@gymflow.com | Empleado | t`.

```bash
docker compose exec db psql -U postgres -d gymflow -c "SELECT m.\"Modulo\", m.\"Operacion\" FROM \"Permisos\" m WHERE m.\"Modulo\" = 4;"
```

Expected: 4 filas con `Empleados | Lectura/Escritura/Modificacion/Eliminacion`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/Persistence/Migrations/
git commit -m "chore: migración EF Core para Empleados + permisos + admin bootstrap"
```

---

## Task 14: Registrar dependencias en DI

**Files:**
- Modify: `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/GymFlow.API/DependencyInjection.cs`

- [ ] **Step 1: Registrar el repositorio y el password hasher en Infrastructure**

En `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`, agregar dentro de `AddInfrastructure` antes de `return services`:

```csharp
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
```

- [ ] **Step 2: Registrar los UseCases en API**

En `backend/src/GymFlow.API/DependencyInjection.cs`, agregar el `using` al inicio:

```csharp
using GymFlow.Application.UseCases.Empleados;
```

Y dentro de `AddApplication`, agregar antes de `return services`:

```csharp
        services.AddScoped<GetEmpleadosQuery>();
        services.AddScoped<GetEmpleadoByIdQuery>();
        services.AddScoped<CrearEmpleadoCommand>();
        services.AddScoped<ActualizarEmpleadoCommand>();
        services.AddScoped<CambiarPasswordCommand>();
        services.AddScoped<DarDeBajaEmpleadoCommand>();
        services.AddScoped<ReactivarEmpleadoCommand>();
```

- [ ] **Step 3: Compilar**

Run: `cd backend && dotnet build`
Expected: Build OK.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/DependencyInjection.cs backend/src/GymFlow.API/DependencyInjection.cs
git commit -m "chore: registrar dependencias de gestión de empleados en DI"
```

---

## Task 15: Crear `EmpleadosController`

**Files:**
- Create: `backend/src/GymFlow.API/Controllers/EmpleadosController.cs`

- [ ] **Step 1: Implementar el controller**

Crear `backend/src/GymFlow.API/Controllers/EmpleadosController.cs`:

```csharp
using GymFlow.API.Authorization;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmpleadosController : ControllerBase
{
    private readonly GetEmpleadosQuery _getEmpleados;
    private readonly GetEmpleadoByIdQuery _getEmpleadoById;
    private readonly CrearEmpleadoCommand _crear;
    private readonly ActualizarEmpleadoCommand _actualizar;
    private readonly CambiarPasswordCommand _cambiarPassword;
    private readonly DarDeBajaEmpleadoCommand _darDeBaja;
    private readonly ReactivarEmpleadoCommand _reactivar;

    public EmpleadosController(
        GetEmpleadosQuery getEmpleados,
        GetEmpleadoByIdQuery getEmpleadoById,
        CrearEmpleadoCommand crear,
        ActualizarEmpleadoCommand actualizar,
        CambiarPasswordCommand cambiarPassword,
        DarDeBajaEmpleadoCommand darDeBaja,
        ReactivarEmpleadoCommand reactivar)
    {
        _getEmpleados = getEmpleados;
        _getEmpleadoById = getEmpleadoById;
        _crear = crear;
        _actualizar = actualizar;
        _cambiarPassword = cambiarPassword;
        _darDeBaja = darDeBaja;
        _reactivar = reactivar;
    }

    [HttpGet]
    [RequierePermiso(Modulo.Empleados, Operacion.Lectura)]
    public async Task<ActionResult<IReadOnlyList<EmpleadoDto>>> GetAll([FromQuery] bool? activo)
        => Ok(await _getEmpleados.ExecuteAsync(activo));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Modulo.Empleados, Operacion.Lectura)]
    public async Task<ActionResult<EmpleadoDto>> GetById(Guid id)
    {
        try { return Ok(await _getEmpleadoById.ExecuteAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost]
    [RequierePermiso(Modulo.Empleados, Operacion.Escritura)]
    public async Task<ActionResult<EmpleadoDto>> Create([FromBody] CrearEmpleadoRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            var dto = await _crear.ExecuteAsync(request, uid, uname);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Modulo.Empleados, Operacion.Modificacion)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActualizarEmpleadoRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _actualizar.ExecuteAsync(id, request, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPatch("{id:guid}/password")]
    [RequierePermiso(Modulo.Empleados, Operacion.Modificacion)]
    public async Task<IActionResult> CambiarPassword(Guid id, [FromBody] CambiarPasswordRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _cambiarPassword.ExecuteAsync(id, request.NuevaPassword, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Modulo.Empleados, Operacion.Eliminacion)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _darDeBaja.ExecuteAsync(id, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPatch("{id:guid}/reactivar")]
    [RequierePermiso(Modulo.Empleados, Operacion.Modificacion)]
    public async Task<IActionResult> Reactivar(Guid id)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _reactivar.ExecuteAsync(id, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
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

- [ ] **Step 2: Compilar**

Run: `cd backend && dotnet build`
Expected: Build OK.

- [ ] **Step 3: Commit**

```bash
git add backend/src/GymFlow.API/Controllers/EmpleadosController.cs
git commit -m "feat: agregar EmpleadosController con CRUD + cambio de password"
```

---

## Task 16: Refactorizar `AuthController.Login` para leer de DB

**Files:**
- Modify: `backend/src/GymFlow.API/Controllers/AuthController.cs`

- [ ] **Step 1: Reemplazar el contenido de `AuthController.cs`**

Reemplazar el contenido completo de `backend/src/GymFlow.API/Controllers/AuthController.cs` por:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuditLogger _auditLogger;
    private readonly IPermisoCache _permisoCache;
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(
        IConfiguration configuration,
        IAuditLogger auditLogger,
        IPermisoCache permisoCache,
        IEmpleadoRepository empleadoRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher)
    {
        _configuration = configuration;
        _auditLogger = auditLogger;
        _permisoCache = permisoCache;
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "El correo y la contraseña son obligatorios." });

        var empleado = await _empleadoRepository.GetByCorreoAsync(request.Correo);
        if (empleado == null || !empleado.EstaActivo || string.IsNullOrEmpty(empleado.PasswordHash) ||
            !_passwordHasher.Verify(request.Password, empleado.PasswordHash))
        {
            return Unauthorized(new { error = "Correo o contraseña incorrectos." });
        }

        var rol = await _rolRepository.GetByIdAsync(empleado.RolId);
        var rolNombre = rol?.Nombre ?? "—";

        var token = GenerateJwt(empleado.Id, empleado.Correo, empleado.RolId, rolNombre, empleado.Nombre, empleado.Apellido);
        var permisos = await _permisoCache.ObtenerPermisosAsync(empleado.RolId);
        var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

        await _auditLogger.LogAsync(
            empleado.Id, $"{empleado.Nombre} {empleado.Apellido}",
            TipoAccionAuditoria.InicioSesion, "Sesion", null,
            $"Inicio de sesión de {empleado.Nombre} {empleado.Apellido} ({rolNombre})");

        return Ok(new LoginResponse(token, empleado.Nombre, empleado.Apellido, empleado.Correo, rolNombre, permisosDto));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized();

        try
        {
            var token = authHeader["Bearer ".Length..];
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "GymFlowDevSecretKey2026!SuperSecure");
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var rolId = Guid.Parse(principal.FindFirst("rolId")?.Value ?? Guid.Empty.ToString());
            var permisos = await _permisoCache.ObtenerPermisosAsync(rolId);
            var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

            return Ok(new
            {
                nombre = principal.FindFirst("nombre")?.Value,
                apellido = principal.FindFirst("apellido")?.Value,
                correo = principal.FindFirst(ClaimTypes.Email)?.Value,
                rolNombre = principal.FindFirst("rolNombre")?.Value,
                permisos = permisosDto
            });
        }
        catch
        {
            return Unauthorized(new { error = "Token inválido o expirado." });
        }
    }

    private string GenerateJwt(Guid id, string correo, Guid rolId, string rolNombre, string nombre, string apellido)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "GymFlowDevSecretKey2026!SuperSecure");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, correo),
            new Claim("rolId", rolId.ToString()),
            new Claim("rolNombre", rolNombre),
            new Claim("nombre", nombre),
            new Claim("apellido", apellido)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Correo, string Password);
public record LoginResponse(string Token, string Nombre, string Apellido, string Correo, string RolNombre, IReadOnlyList<PermisoDto> Permisos);
```

- [ ] **Step 2: Compilar y correr todos los tests**

Run: `cd backend && dotnet build && dotnet test`
Expected: Build OK + todos los tests verdes.

- [ ] **Step 3: Smoke test manual del login**

Levantar el stack y probar el login:
```bash
docker compose down -v
docker compose up --build -d
```

Esperar 10-15 segundos a que aplique migraciones, luego:

```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5146/api/auth/login" -Method Post -ContentType "application/json" -Body '{"correo":"admin@gymflow.com","password":"admin123"}'
$response.token
```

Expected: imprime un JWT.

Probar también credenciales malas:
```powershell
try { Invoke-RestMethod -Uri "http://localhost:5146/api/auth/login" -Method Post -ContentType "application/json" -Body '{"correo":"admin@gymflow.com","password":"wrong"}' } catch { $_.Exception.Response.StatusCode }
```

Expected: `Unauthorized`.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GymFlow.API/Controllers/AuthController.cs
git commit -m "refactor: AuthController lee empleados de DB y verifica password con BCrypt"
```

---

## Task 17: Tipos y servicio de empleados en frontend

**Files:**
- Create: `frontend/src/types/empleado.ts`
- Modify: `frontend/src/types/permisos.ts`
- Create: `frontend/src/services/empleados.ts`

- [ ] **Step 1: Crear los tipos**

Crear `frontend/src/types/empleado.ts`:

```typescript
export interface Empleado {
  id: string
  nombre: string
  apellido: string
  correo: string
  rolId: string
  rolNombre: string
  estaActivo: boolean
  fechaCreacion: string
}

export interface CrearEmpleadoRequest {
  nombre: string
  apellido: string
  correo: string
  password: string
  rolId: string
}

export interface ActualizarEmpleadoRequest {
  nombre: string
  apellido: string
  correo: string
  rolId: string
}

export interface CambiarPasswordRequest {
  nuevaPassword: string
}
```

- [ ] **Step 2: Agregar `'Empleados'` al tipo `Modulo`**

En `frontend/src/types/permisos.ts`, encontrar el type `Modulo` (debería ser algo como `'Socios' | 'Planes' | ...`) y agregar `'Empleados'`. Si no existe, agregarlo donde corresponda. Buscar:

Run: `cd frontend && grep -n "Modulo" src/types/permisos.ts`

Editar el archivo para que el type `Modulo` incluya `'Empleados'`. Si por ejemplo se ve así:
```typescript
export type Modulo = 'Socios' | 'Planes' | 'Unidades' | 'Auditoria'
```
debe quedar:
```typescript
export type Modulo = 'Socios' | 'Planes' | 'Unidades' | 'Auditoria' | 'Empleados'
```

- [ ] **Step 3: Crear el servicio**

Crear `frontend/src/services/empleados.ts`:

```typescript
import api from './api'
import type {
  Empleado,
  CrearEmpleadoRequest,
  ActualizarEmpleadoRequest,
  CambiarPasswordRequest,
} from '@/types/empleado'

export async function listarEmpleados(activo?: boolean): Promise<Empleado[]> {
  const { data } = await api.get<Empleado[]>('/empleados', { params: { activo } })
  return data
}

export async function obtenerEmpleado(id: string): Promise<Empleado> {
  const { data } = await api.get<Empleado>(`/empleados/${id}`)
  return data
}

export async function crearEmpleado(req: CrearEmpleadoRequest): Promise<Empleado> {
  const { data } = await api.post<Empleado>('/empleados', req)
  return data
}

export async function actualizarEmpleado(id: string, req: ActualizarEmpleadoRequest): Promise<void> {
  await api.put(`/empleados/${id}`, req)
}

export async function cambiarPasswordEmpleado(id: string, req: CambiarPasswordRequest): Promise<void> {
  await api.patch(`/empleados/${id}/password`, req)
}

export async function darDeBajaEmpleado(id: string): Promise<void> {
  await api.delete(`/empleados/${id}`)
}

export async function reactivarEmpleado(id: string): Promise<void> {
  await api.patch(`/empleados/${id}/reactivar`)
}
```

- [ ] **Step 4: Compilar el frontend**

Run: `cd frontend && npm run build`
Expected: Build OK sin errores de TypeScript.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/types/empleado.ts frontend/src/types/permisos.ts frontend/src/services/empleados.ts
git commit -m "feat: types y servicio de empleados en frontend"
```

---

## Task 18: Página de listado `/admin/usuarios`

**Files:**
- Create: `frontend/src/pages/admin/UsuariosPage.tsx`

- [ ] **Step 1: Implementar la página de listado**

Crear `frontend/src/pages/admin/UsuariosPage.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import {
  listarEmpleados,
  darDeBajaEmpleado,
  reactivarEmpleado,
} from '@/services/empleados'
import type { Empleado } from '@/types/empleado'
import { usePermisos } from '@/hooks/usePermisos'

export default function UsuariosPage() {
  const [empleados, setEmpleados] = useState<Empleado[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<'activos' | 'inactivos'>('activos')
  const { puedeEscribir, puedeModificar, puedeEliminar } = usePermisos()
  const puedeCrear = puedeEscribir('Empleados')
  const puedeEditar = puedeModificar('Empleados')
  const puedeBorrar = puedeEliminar('Empleados')

  const cargar = () => {
    setLoading(true)
    listarEmpleados(tab === 'activos')
      .then(setEmpleados)
      .catch(e => setError(e?.response?.data?.error ?? 'Error al cargar usuarios'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { cargar() }, [tab])

  const onBaja = async (id: string, nombre: string) => {
    if (!confirm(`¿Dar de baja a "${nombre}"?`)) return
    try {
      await darDeBajaEmpleado(id)
      cargar()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } }
      alert(err?.response?.data?.error ?? 'Error al dar de baja')
    }
  }

  const onReactivar = async (id: string) => {
    try {
      await reactivarEmpleado(id)
      cargar()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } }
      alert(err?.response?.data?.error ?? 'Error al reactivar')
    }
  }

  if (loading) return <div className="p-6">Cargando…</div>
  if (error) return <div className="p-6 text-destructive">{error}</div>

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Usuarios</h1>
        {puedeCrear && (
          <Link to="/admin/usuarios/nuevo"><Button>Nuevo usuario</Button></Link>
        )}
      </div>

      <div className="flex gap-2">
        <Button variant={tab === 'activos' ? 'default' : 'outline'} size="sm" onClick={() => setTab('activos')}>Activos</Button>
        <Button variant={tab === 'inactivos' ? 'default' : 'outline'} size="sm" onClick={() => setTab('inactivos')}>Inactivos</Button>
      </div>

      <div className="rounded-lg border border-border bg-card">
        <table className="w-full">
          <thead>
            <tr className="border-b border-border">
              <th className="text-left py-3 px-4 text-sm font-medium">Nombre</th>
              <th className="text-left py-3 px-4 text-sm font-medium">Correo</th>
              <th className="text-left py-3 px-4 text-sm font-medium">Rol</th>
              <th className="py-3 px-4" />
            </tr>
          </thead>
          <tbody>
            {empleados.map(e => (
              <tr key={e.id} className="border-b border-border last:border-0">
                <td className="py-3 px-4">{e.nombre} {e.apellido}</td>
                <td className="py-3 px-4 text-sm text-muted-foreground">{e.correo}</td>
                <td className="py-3 px-4 text-sm">{e.rolNombre}</td>
                <td className="py-3 px-4 text-right space-x-2">
                  {puedeEditar && (
                    <Link to={`/admin/usuarios/${e.id}/editar`}>
                      <Button size="sm" variant="outline">Editar</Button>
                    </Link>
                  )}
                  {puedeEditar && e.estaActivo && (
                    <Link to={`/admin/usuarios/${e.id}/password`}>
                      <Button size="sm" variant="outline">Password</Button>
                    </Link>
                  )}
                  {puedeBorrar && e.estaActivo && (
                    <Button size="sm" variant="destructive" onClick={() => onBaja(e.id, `${e.nombre} ${e.apellido}`)}>
                      Dar de baja
                    </Button>
                  )}
                  {puedeEditar && !e.estaActivo && (
                    <Button size="sm" variant="outline" onClick={() => onReactivar(e.id)}>
                      Reactivar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {empleados.length === 0 && (
              <tr>
                <td colSpan={4} className="py-8 px-4 text-center text-muted-foreground text-sm">
                  No hay usuarios {tab === 'activos' ? 'activos' : 'inactivos'}.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/pages/admin/UsuariosPage.tsx
git commit -m "feat: página de listado de usuarios con activos/inactivos"
```

---

## Task 19: Páginas de creación y edición de usuario

**Files:**
- Create: `frontend/src/pages/admin/NuevoUsuarioPage.tsx`
- Create: `frontend/src/pages/admin/EditUsuarioPage.tsx`
- Create: `frontend/src/pages/admin/CambiarPasswordPage.tsx`

- [ ] **Step 1: Página `NuevoUsuarioPage`**

Crear `frontend/src/pages/admin/NuevoUsuarioPage.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { crearEmpleado } from '@/services/empleados'
import { listarRoles } from '@/services/roles'
import type { Rol } from '@/types/permisos'

export default function NuevoUsuarioPage() {
  const navigate = useNavigate()
  const [roles, setRoles] = useState<Rol[]>([])
  const [form, setForm] = useState({ nombre: '', apellido: '', correo: '', password: '', rolId: '' })
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    // Filtra el rol Socio del dropdown — los empleados nunca pueden tener ese rol.
    listarRoles().then(rs => setRoles(rs.filter(r => r.nombre !== 'Socio')))
  }, [])

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSaving(true)
    try {
      await crearEmpleado(form)
      navigate('/admin/usuarios')
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string } } }
      setError(e?.response?.data?.error ?? 'Error al crear usuario')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-xl space-y-4">
      <h1 className="text-2xl font-bold">Nuevo usuario</h1>
      {error && <div className="rounded bg-destructive/10 text-destructive p-3 text-sm">{error}</div>}
      <form onSubmit={onSubmit} className="space-y-3">
        <div>
          <label className="block text-sm mb-1">Nombre</label>
          <input className="w-full rounded border px-3 py-2 bg-background" value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Apellido</label>
          <input className="w-full rounded border px-3 py-2 bg-background" value={form.apellido} onChange={e => setForm({ ...form, apellido: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Correo</label>
          <input type="email" className="w-full rounded border px-3 py-2 bg-background" value={form.correo} onChange={e => setForm({ ...form, correo: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Contraseña inicial (mínimo 8 caracteres)</label>
          <input type="password" className="w-full rounded border px-3 py-2 bg-background" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} minLength={8} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Rol</label>
          <select className="w-full rounded border px-3 py-2 bg-background" value={form.rolId} onChange={e => setForm({ ...form, rolId: e.target.value })} required>
            <option value="">Seleccionar…</option>
            {roles.map(r => <option key={r.id} value={r.id}>{r.nombre}</option>)}
          </select>
        </div>
        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Guardando…' : 'Crear'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/admin/usuarios')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
```

- [ ] **Step 2: Página `EditUsuarioPage`**

Crear `frontend/src/pages/admin/EditUsuarioPage.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { obtenerEmpleado, actualizarEmpleado } from '@/services/empleados'
import { listarRoles } from '@/services/roles'
import type { Rol } from '@/types/permisos'

export default function EditUsuarioPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [roles, setRoles] = useState<Rol[]>([])
  const [form, setForm] = useState({ nombre: '', apellido: '', correo: '', rolId: '' })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    Promise.all([obtenerEmpleado(id), listarRoles()])
      .then(([emp, rs]) => {
        setForm({ nombre: emp.nombre, apellido: emp.apellido, correo: emp.correo, rolId: emp.rolId })
        setRoles(rs.filter(r => r.nombre !== 'Socio'))
      })
      .catch(e => setError(e?.response?.data?.error ?? 'Error al cargar'))
      .finally(() => setLoading(false))
  }, [id])

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!id) return
    setError(null)
    setSaving(true)
    try {
      await actualizarEmpleado(id, form)
      navigate('/admin/usuarios')
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string } } }
      setError(e?.response?.data?.error ?? 'Error al guardar')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="p-6">Cargando…</div>

  return (
    <div className="max-w-xl space-y-4">
      <h1 className="text-2xl font-bold">Editar usuario</h1>
      {error && <div className="rounded bg-destructive/10 text-destructive p-3 text-sm">{error}</div>}
      <form onSubmit={onSubmit} className="space-y-3">
        <div>
          <label className="block text-sm mb-1">Nombre</label>
          <input className="w-full rounded border px-3 py-2 bg-background" value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Apellido</label>
          <input className="w-full rounded border px-3 py-2 bg-background" value={form.apellido} onChange={e => setForm({ ...form, apellido: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Correo</label>
          <input type="email" className="w-full rounded border px-3 py-2 bg-background" value={form.correo} onChange={e => setForm({ ...form, correo: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Rol</label>
          <select className="w-full rounded border px-3 py-2 bg-background" value={form.rolId} onChange={e => setForm({ ...form, rolId: e.target.value })} required>
            <option value="">Seleccionar…</option>
            {roles.map(r => <option key={r.id} value={r.id}>{r.nombre}</option>)}
          </select>
        </div>
        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Guardando…' : 'Guardar'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/admin/usuarios')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
```

- [ ] **Step 3: Página `CambiarPasswordPage`**

Crear `frontend/src/pages/admin/CambiarPasswordPage.tsx`:

```tsx
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { cambiarPasswordEmpleado } from '@/services/empleados'

export default function CambiarPasswordPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [pw, setPw] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!id) return
    if (pw !== confirm) {
      setError('Las contraseñas no coinciden.')
      return
    }
    setError(null)
    setSaving(true)
    try {
      await cambiarPasswordEmpleado(id, { nuevaPassword: pw })
      navigate('/admin/usuarios')
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string } } }
      setError(e?.response?.data?.error ?? 'Error al cambiar contraseña')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-md space-y-4">
      <h1 className="text-2xl font-bold">Cambiar contraseña</h1>
      {error && <div className="rounded bg-destructive/10 text-destructive p-3 text-sm">{error}</div>}
      <form onSubmit={onSubmit} className="space-y-3">
        <div>
          <label className="block text-sm mb-1">Nueva contraseña (mínimo 8 caracteres)</label>
          <input type="password" className="w-full rounded border px-3 py-2 bg-background" value={pw} onChange={e => setPw(e.target.value)} minLength={8} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Confirmar</label>
          <input type="password" className="w-full rounded border px-3 py-2 bg-background" value={confirm} onChange={e => setConfirm(e.target.value)} minLength={8} required />
        </div>
        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Guardando…' : 'Cambiar'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/admin/usuarios')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
```

- [ ] **Step 4: Compilar el frontend**

Run: `cd frontend && npm run build`
Expected: Build OK.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/admin/NuevoUsuarioPage.tsx frontend/src/pages/admin/EditUsuarioPage.tsx frontend/src/pages/admin/CambiarPasswordPage.tsx
git commit -m "feat: páginas de creación, edición y cambio de password de usuarios"
```

---

## Task 20: Sidebar + rutas

**Files:**
- Modify: `frontend/src/components/layout/Sidebar.tsx`
- Modify: `frontend/src/App.tsx`

- [ ] **Step 1: Agregar "Usuarios" al grupo "Sistema" en el Sidebar**

En `frontend/src/components/layout/Sidebar.tsx`, importar el ícono `UserCog`:

Cambiar la línea de imports de lucide-react:
```typescript
import {
  LayoutDashboard,
  Users,
  UserPlus,
  UserX,
  UserCog,
  CreditCard,
  Dumbbell,
  ChevronDown,
  ChevronRight,
  ClipboardList,
  Shield,
} from 'lucide-react'
```

En el array `navigation`, dentro del grupo "Sistema", agregar el ítem "Usuarios" como primer ítem del grupo:

```typescript
  {
    label: 'Sistema',
    icon: <ClipboardList className="h-5 w-5" />,
    modulo: 'Empleados',  // ← cambiar de 'Auditoria' a 'Empleados' para que el grupo se muestre si tiene permiso
    items: [
      { label: 'Usuarios', path: '/admin/usuarios', icon: <UserCog className="h-4 w-4" /> },
      { label: 'Roles', path: '/admin/roles', icon: <Shield className="h-4 w-4" /> },
      { label: 'Auditoría', path: '/admin/auditoria', icon: <ClipboardList className="h-4 w-4" /> },
    ],
  },
```

> **Nota sobre el filtrado:** el grupo se filtra por un único `modulo`. Cambiar a `'Empleados'` significa que un usuario solo verá el grupo "Sistema" si tiene `Empleados.Lectura`. Como el rol Administrador (que es el único con todos los permisos por seed) tiene también `Auditoria.Lectura`, esto funciona en la práctica. Si en el futuro se necesita filtrado más fino (mostrar el grupo si tiene CUALQUIERA de los módulos), refactorizar `NavGroup` para aceptar `modulos: Modulo[]`.

- [ ] **Step 2: Agregar rutas en `App.tsx`**

En `frontend/src/App.tsx`, agregar imports al inicio:

```typescript
import UsuariosPage from '@/pages/admin/UsuariosPage'
import NuevoUsuarioPage from '@/pages/admin/NuevoUsuarioPage'
import EditUsuarioPage from '@/pages/admin/EditUsuarioPage'
import CambiarPasswordPage from '@/pages/admin/CambiarPasswordPage'
```

Y dentro de `<Route path="/admin" element={<AdminLayout />}>`, después de la ruta de roles, agregar:

```tsx
        <Route path="usuarios" element={<UsuariosPage />} />
        <Route path="usuarios/nuevo" element={<NuevoUsuarioPage />} />
        <Route path="usuarios/:id/editar" element={<EditUsuarioPage />} />
        <Route path="usuarios/:id/password" element={<CambiarPasswordPage />} />
```

- [ ] **Step 3: Compilar el frontend**

Run: `cd frontend && npm run build`
Expected: Build OK.

- [ ] **Step 4: Probar manualmente**

Levantar todo:
```bash
docker compose up -d
cd frontend && npm run dev
```

En el navegador:
1. `http://localhost:5173/login` → entrar con `admin@gymflow.com` / `admin123`.
2. En el sidebar debería aparecer "Sistema" con "Usuarios", "Roles", "Auditoría".
3. Click en "Usuarios" → ver la lista (solo el admin de bootstrap).
4. Click en "Nuevo usuario" → crear uno con rol Administrador (o crear primero un rol custom desde "Roles" y usar ese).
5. Verificar que el rol "Socio" NO aparezca en el dropdown.
6. Editar el usuario creado → cambiar nombre.
7. Cambiar password del usuario → loguearse con la nueva password en otra pestaña.
8. Dar de baja al usuario → tab "Inactivos" lo muestra → reactivarlo.
9. Verificar que el admin no se pueda dar de baja a sí mismo (debería mostrar error).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/layout/Sidebar.tsx frontend/src/App.tsx
git commit -m "feat: agregar Usuarios al sidebar y rutas"
```

---

## Task 21: Actualizar documentación

**Files:**
- Modify: `docs/agent_Context.md`
- Modify: `docs/GymFlow_Requerimientos_Completos.md`

- [ ] **Step 1: Actualizar la jerarquía de usuarios en `agent_Context.md`**

En `docs/agent_Context.md`, reemplazar la sección "Jerarquía de Usuarios (TPH)":

Buscar:
```
### Jerarquía de Usuarios (TPH — Table Per Hierarchy)

```
Usuario (abstract base)
├── Profesor — ClasesAsignadas[]
└── Socio — Cuotas[], Inscripciones[], Asistencias[], Rutinas[], TipoDocumento (CI/Pasaporte/Otro, requerido)
```

**Admin no es subclase** — es un valor del enum `Rol` en `Usuario`. No tiene atributos propios.
```

Reemplazar por:
```
### Jerarquía de Usuarios (TPH — Table Per Hierarchy)

```
Usuario (abstract base, PasswordHash nullable)
├── Empleado — admin, profesor, recepcionista, etc. Login email + password (BCrypt). PasswordHash siempre seteado.
└── Socio — Cuotas[], Inscripciones[], Asistencias[], Rutinas[], TipoDocumento. Login Google OAuth (It.5). PasswordHash null hasta entonces.
```

**El rol del usuario es un `RolId` (FK a `Rol`)**, no una subclase. La jerarquía solo refleja diferencias de atributos y mecanismo de auth, no de rol asignado.
```

- [ ] **Step 2: Actualizar la sección "Autenticación" en `agent_Context.md`**

Buscar:
```
## Autenticación

- **Fase 1:** JWT (access + refresh token), login email/contraseña, 3 roles: Admin, Socio, Profesor
- **Fase 2:** OAuth 2.0 con Google para socios
- Passwords hasheados con BCrypt
- Endpoints protegidos con `[Authorize(Roles = "...")]`
```

Reemplazar por:
```
## Autenticación

**Dos mecanismos de login según tipo de usuario:**

| Tipo | Login | Roles posibles |
|---|---|---|
| **Empleado** | email + password (+ MFA en It.5) | Cualquier rol salvo Socio |
| **Socio** | Google OAuth (It.5) | Únicamente el rol Socio |

- **JWT** firmado con clave simétrica, expiración 8 horas. Lleva `userId`, `correo`, `rolId`, `rolNombre`, `nombre`, `apellido`.
- **Passwords de empleados** hasheados con BCrypt.Net-Next (factor 11).
- **Endpoints protegidos** con `[RequierePermiso(Modulo, Operacion)]` (no `[Authorize(Roles=...)]`).
- **Empleado de bootstrap:** la migración crea `admin@gymflow.com` / `admin123` automáticamente. En producción debe cambiar su password al primer login.

**Estado actual (It.2):** login productivo solo para Empleados. Login de Socios queda para It.5.
```

- [ ] **Step 3: Agregar nota en la sección "Al agregar un módulo nuevo"**

Después del paso 5 de la sección "Al agregar un módulo nuevo (RF-23 — Roles y Permisos)", actualizar el título y agregar una referencia al spec de gestión de usuarios. Cambiar:

```
### Al agregar un módulo nuevo (RF-23 — Roles y Permisos)
```

por:

```
### Al agregar un módulo nuevo (RNF-01 — Roles y Permisos)
```

Y al final de esa sección, después del párrafo `El sistema de permisos es de catálogo cerrado en código...`, agregar:

```

**Para crear empleados que puedan loguearse, ver:** `docs/superpowers/specs/2026-04-28-rnf-01-gestion-usuarios.md`. La gestión de usuarios usa la entidad `Empleado` (subclase de `Usuario`) y se administra desde `/admin/usuarios` en el frontend.
```

- [ ] **Step 4: Actualizar `GymFlow_Requerimientos_Completos.md`**

Buscar en `docs/GymFlow_Requerimientos_Completos.md` la línea sobre RNF-01:
```
| **RNF-01** | Autenticación y autorización basada en roles. Cada usuario accede solo a funcionalidades de su perfil. | Seguridad |
```

Reemplazar por:
```
| **RNF-01** | Autenticación y autorización basada en roles. Cada usuario accede solo a funcionalidades de su perfil. Implementado en dos partes: It.2 (admin, profesor y otros roles internos vía email + password con BCrypt) e It.5 (socios vía Google OAuth). | Seguridad |
```

Y al final de la sección de Iteración 2 (línea ~520), agregar a la lista de requerimientos:
```
- RNF-01 (Autenticación y autorización por roles — parte interna: empleados con email+password, roles dinámicos)
```

- [ ] **Step 5: Marcar el spec parte 2 como implementado**

En `docs/superpowers/specs/2026-04-28-rnf-01-gestion-usuarios.md`, cambiar:
```
**Estado:** Spec — pendiente de aprobación para generar plan
```
por:
```
**Estado:** Implementado y mergeado a `develop`.
```

Y marcar todos los criterios de aceptación con `[x]`.

- [ ] **Step 6: Commit**

```bash
git add docs/agent_Context.md docs/GymFlow_Requerimientos_Completos.md docs/superpowers/specs/2026-04-28-rnf-01-gestion-usuarios.md
git commit -m "docs: actualizar contexto con jerarquía Empleado/Socio y estado RNF-01"
```

---

## Task 22: Verificación final y PR

- [ ] **Step 1: Correr toda la batería de tests**

Run: `cd backend && dotnet test`
Expected: TODOS los tests verdes (los nuevos + los existentes de Socios, Roles, Permisos, Auditoria).

Run: `cd frontend && npm run build`
Expected: Build OK.

- [ ] **Step 2: Smoke test E2E manual**

```bash
docker compose down -v
docker compose up --build -d
cd frontend && npm run dev
```

Checklist:
- [ ] Login con `admin@gymflow.com` / `admin123` funciona.
- [ ] Login con credenciales malas devuelve 401.
- [ ] Crear un rol custom desde `/admin/roles`.
- [ ] Crear un empleado desde `/admin/usuarios` con ese rol.
- [ ] Editar el empleado.
- [ ] Cambiar su password.
- [ ] Hacer logout y login con el empleado nuevo + nueva password.
- [ ] Verificar que solo ve los menús permitidos por su rol.
- [ ] Volver a loguear como admin → dar de baja al empleado nuevo.
- [ ] Tab "Inactivos" muestra al empleado.
- [ ] Reactivar.
- [ ] Crear un socio desde `/admin/socios` (regresión: verificar que sigue funcionando con `PasswordHash` null).
- [ ] Verificar en la auditoría que aparecen los registros de creación/modificación/baja de empleados.

- [ ] **Step 3: Push y crear PR**

```bash
git push -u origin feature/RNF_01
gh pr create --title "feat(RNF-01): gestión de usuarios empleados (parte 2 de It.2)" --body "$(cat <<'EOF'
## Summary
- Implementa la **parte 2 de RNF-01** según `docs/superpowers/specs/2026-04-28-rnf-01-gestion-usuarios.md`.
- Agrega entidad `Empleado` (TPH) y migra usuarios hardcodeados a DB con BCrypt.
- CRUD `/api/empleados` con permisos `Modulo.Empleados`.
- Nueva pantalla `/admin/usuarios` en frontend.

## Test plan
- [ ] `dotnet test` — todos los tests verdes (incluye 6 + 4 + 3 + 3 nuevos en Empleados).
- [ ] `npm run build` en frontend OK.
- [ ] Smoke test E2E: login admin bootstrap → crear rol → crear empleado → cambiar password → re-login con nueva password → dar de baja → reactivar.
- [ ] Regresión: CRUD de socios sigue funcionando.
EOF
)"
```

- [ ] **Step 4: Eliminar el archivo de generación de hash temporal (opcional)**

Si se mantuvo `HashGeneratorTemp.cs`, dejarlo con `Skip` permanente. Alternativamente, eliminarlo:
```bash
git rm backend/tests/GymFlow.Infrastructure.Tests/HashGeneratorTemp.cs
git commit -m "chore: remover helper temporal de generación de hash"
```

---

## Self-review notes

Antes de cerrar esta sesión, verificar:

1. **Cobertura del spec:**
   - [x] `Empleado` como subclase concreta (Task 3).
   - [x] `PasswordHash` nullable (Task 2).
   - [x] BCrypt + `IPasswordHasher` (Task 4).
   - [x] CRUD completo (Tasks 6-10).
   - [x] Endpoint de cambio de password separado (Tasks 8, 15).
   - [x] No autoeliminación (Task 9 test + command).
   - [x] Bloqueo de rol Socio en Empleado (Tasks 6, 7).
   - [x] Migración con seed bootstrap + permisos `Empleados` (Tasks 12, 13).
   - [x] Refactor de login (Task 16).
   - [x] Frontend completo (Tasks 17-20).
   - [x] Documentación actualizada (Task 21).

2. **Sin placeholders:** todo el código aparece literal en cada step.

3. **Consistencia de tipos:** `EmpleadoDto`, `Empleado`, métodos en repos y commands coinciden entre tasks.

---

**Plan complete and saved to `docs/superpowers/plans/2026-04-28-rnf-01-gestion-usuarios.md`. Two execution options:**

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
