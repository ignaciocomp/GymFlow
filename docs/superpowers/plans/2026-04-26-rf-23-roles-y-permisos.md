# RF-23 — Gestión de Roles y Permisos — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar el enum `Rol` por un sistema de roles dinámicos con permisos CRUD por módulo, permitiendo al admin crear roles personalizados desde la UI.

**Architecture:** Tres tablas nuevas (`Roles`, `Permisos`, `RolPermisos`). El JWT lleva `RolId`; un atributo `[RequierePermiso(Modulo, Operacion)]` lee la cache de permisos en memoria y autoriza/rechaza. Frontend usa hook `usePermisos` para checks granulares y esconder UI.

**Tech Stack:** C# / .NET 8, EF Core 8 (Code-First), PostgreSQL, IMemoryCache, React 19 + TypeScript, axios, shadcn/ui.

**Spec:** [docs/superpowers/specs/2026-04-26-rf-23-roles-y-permisos.md](../specs/2026-04-26-rf-23-roles-y-permisos.md)

**Branch:** `feature/RF_23`

**Conventions:**
- **NO ejecutar `git commit` ni `git add` durante la ejecución.** El usuario maneja todos los commits. Los pasos titulados "**Step N: Commit ...**" en cada tarea son **sugerencias de mensaje de commit** que el usuario puede usar cuando él decida commitear — pero el agente que ejecuta el plan **no debe correr esos comandos**. Saltar esos pasos.
- Tests solo en lo importante: invariantes de dominio, UseCases (happy + edge), atributo de autorización.
- Estilo de código existente: backing fields privados con setters `private set`, constructores que validan con `throw new ArgumentException`, UseCases con método `ExecuteAsync`.
- Convención de excepciones (siguiendo el estilo del repo, ej. `PlanesController.cs:80-82`):
  - `ArgumentException` → input inválido del cliente → API responde 400.
  - `KeyNotFoundException` → recurso no existe → API responde 404.
  - `InvalidOperationException` → regla de negocio violada (estado no permite la operación) → API responde 409.

---

## File Structure

### Backend — archivos nuevos
- `backend/src/GymFlow.Domain/Enums/Modulo.cs`
- `backend/src/GymFlow.Domain/Enums/Operacion.cs`
- `backend/src/GymFlow.Domain/Entities/Permiso.cs`
- `backend/src/GymFlow.Domain/Entities/Rol.cs`
- `backend/src/GymFlow.Domain/Entities/RolPermiso.cs`
- `backend/src/GymFlow.Application/Interfaces/IRolRepository.cs`
- `backend/src/GymFlow.Application/Interfaces/IPermisoCache.cs`
- `backend/src/GymFlow.Application/DTOs/RolDto.cs`
- `backend/src/GymFlow.Application/DTOs/PermisoDto.cs`
- `backend/src/GymFlow.Application/DTOs/CrearRolRequest.cs`
- `backend/src/GymFlow.Application/DTOs/ActualizarRolRequest.cs`
- `backend/src/GymFlow.Application/UseCases/Roles/GetRolesQuery.cs`
- `backend/src/GymFlow.Application/UseCases/Roles/GetRolByIdQuery.cs`
- `backend/src/GymFlow.Application/UseCases/Roles/CrearRolCommand.cs`
- `backend/src/GymFlow.Application/UseCases/Roles/ActualizarRolCommand.cs`
- `backend/src/GymFlow.Application/UseCases/Roles/EliminarRolCommand.cs`
- `backend/src/GymFlow.Application/UseCases/Permisos/GetPermisosQuery.cs`
- `backend/src/GymFlow.Infrastructure/Persistence/Configurations/RolConfiguration.cs`
- `backend/src/GymFlow.Infrastructure/Persistence/Configurations/PermisoConfiguration.cs`
- `backend/src/GymFlow.Infrastructure/Persistence/Configurations/RolPermisoConfiguration.cs`
- `backend/src/GymFlow.Infrastructure/Repositories/RolRepository.cs`
- `backend/src/GymFlow.Infrastructure/Services/PermisoCache.cs`
- `backend/src/GymFlow.API/Authorization/RequierePermisoAttribute.cs`
- `backend/src/GymFlow.API/Controllers/RolesController.cs`
- `backend/src/GymFlow.API/Controllers/PermisosController.cs`
- `backend/tests/GymFlow.Domain.Tests/Entities/RolTests.cs`
- `backend/tests/GymFlow.Application.Tests/UseCases/Roles/CrearRolCommandTests.cs`
- `backend/tests/GymFlow.Application.Tests/UseCases/Roles/ActualizarRolCommandTests.cs`
- `backend/tests/GymFlow.Application.Tests/UseCases/Roles/EliminarRolCommandTests.cs`

### Backend — archivos a modificar
- `backend/src/GymFlow.Domain/Entities/Usuario.cs` — `Rol Rol` → `Guid RolId`
- `backend/src/GymFlow.Domain/Entities/Socio.cs` — recibe `Guid rolId` en constructor
- `backend/src/GymFlow.Domain/Enums/Rol.cs` — **eliminar**
- `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs` — agregar `DbSet`s
- `backend/src/GymFlow.Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs` — FK a Rol
- `backend/src/GymFlow.Infrastructure/DependencyInjection.cs` — registrar nuevos servicios
- `backend/src/GymFlow.API/Controllers/AuthController.cs` — JWT con RolId, LoginResponse con permisos
- `backend/src/GymFlow.API/Controllers/SociosController.cs` — `[RequierePermiso]`
- `backend/src/GymFlow.API/Controllers/PlanesController.cs` — `[RequierePermiso]`
- `backend/src/GymFlow.API/Controllers/UnidadesController.cs` — `[RequierePermiso]`
- `backend/src/GymFlow.API/Controllers/AuditoriaController.cs` — `[RequierePermiso]`

### Frontend — archivos nuevos
- `frontend/src/hooks/usePermisos.ts`
- `frontend/src/types/permisos.ts`
- `frontend/src/services/roles.ts`
- `frontend/src/services/permisos.ts`
- `frontend/src/pages/admin/RolesPage.tsx`
- `frontend/src/pages/admin/EditRolPage.tsx`
- `frontend/src/pages/admin/NuevoRolPage.tsx`

### Frontend — archivos a modificar
- `frontend/src/context/AuthContext.tsx` — `permisos[]` en User
- `frontend/src/components/layout/AdminLayout.tsx` — usar `usePermisos`
- `frontend/src/components/layout/Topbar.tsx` — mostrar `rolNombre` en lugar de `rol`
- `frontend/src/components/layout/Sidebar.tsx` — esconder ítems sin permiso
- `frontend/src/App.tsx` — rutas para gestión de roles

### Documentación
- `docs/agent_Context.md` — convención al agregar módulos nuevos

---

## Phase 1 — Domain

### Task 1: Enums Modulo y Operacion

**Files:**
- Create: `backend/src/GymFlow.Domain/Enums/Modulo.cs`
- Create: `backend/src/GymFlow.Domain/Enums/Operacion.cs`

- [ ] **Step 1: Crear `Modulo.cs`**

```csharp
namespace GymFlow.Domain.Enums;

public enum Modulo
{
    Socios,
    Planes,
    Unidades,
    Auditoria
}
```

- [ ] **Step 2: Crear `Operacion.cs`**

```csharp
namespace GymFlow.Domain.Enums;

public enum Operacion
{
    Lectura,
    Escritura,
    Modificacion,
    Eliminacion
}
```

- [ ] **Step 3: Compilar para verificar**

Run: `dotnet build backend/src/GymFlow.Domain`
Expected: Build succeeded.

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Domain/Enums/Modulo.cs backend/src/GymFlow.Domain/Enums/Operacion.cs
git commit -m "feat(domain): add Modulo and Operacion enums for RF-23"
```

---

### Task 2: Entidad Permiso

**Files:**
- Create: `backend/src/GymFlow.Domain/Entities/Permiso.cs`

- [ ] **Step 1: Crear entidad `Permiso`**

```csharp
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Permiso
{
    public Guid Id { get; private set; }
    public Modulo Modulo { get; private set; }
    public Operacion Operacion { get; private set; }

    private Permiso() { } // EF Core

    public Permiso(Modulo modulo, Operacion operacion)
    {
        Id = Guid.NewGuid();
        Modulo = modulo;
        Operacion = operacion;
    }

    // Constructor para seed data con Id explícito
    public Permiso(Guid id, Modulo modulo, Operacion operacion)
    {
        Id = id;
        Modulo = modulo;
        Operacion = operacion;
    }
}
```

- [ ] **Step 2: Compilar**

Run: `dotnet build backend/src/GymFlow.Domain`
Expected: Build succeeded.

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Domain/Entities/Permiso.cs
git commit -m "feat(domain): add Permiso entity"
```

---

### Task 3: Entidad Rol con tests

**Files:**
- Create: `backend/src/GymFlow.Domain/Entities/Rol.cs`
- Create: `backend/tests/GymFlow.Domain.Tests/Entities/RolTests.cs`

- [ ] **Step 1: Crear `Rol.cs`**

```csharp
namespace GymFlow.Domain.Entities;

public class Rol
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public bool EsSistema { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    public ICollection<RolPermiso> Permisos { get; private set; } = new List<RolPermiso>();

    private Rol() { } // EF Core

    public Rol(string nombre, bool esSistema = false)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        EsSistema = esSistema;
        FechaCreacion = DateTime.UtcNow;
    }

    // Constructor para seed con Id explícito
    public Rol(Guid id, string nombre, bool esSistema, DateTime fechaCreacion)
    {
        Id = id;
        Nombre = nombre;
        EsSistema = esSistema;
        FechaCreacion = fechaCreacion;
    }

    public void Renombrar(string nuevoNombre)
    {
        if (EsSistema)
            throw new InvalidOperationException("No se puede renombrar un rol del sistema.");

        Nombre = !string.IsNullOrWhiteSpace(nuevoNombre)
            ? nuevoNombre
            : throw new ArgumentException("Nombre is required.", nameof(nuevoNombre));
    }

    public void ReemplazarPermisos(IEnumerable<Guid> permisoIds)
    {
        if (EsSistema)
            throw new InvalidOperationException("No se pueden modificar los permisos de un rol del sistema.");

        Permisos.Clear();
        foreach (var pid in permisoIds.Distinct())
        {
            Permisos.Add(new RolPermiso(Id, pid));
        }
    }
}
```

- [ ] **Step 2: Crear `RolTests.cs`**

```csharp
using GymFlow.Domain.Entities;
using Xunit;

namespace GymFlow.Domain.Tests.Entities;

public class RolTests
{
    [Fact]
    public void Crear_ConNombreValido_AsignaCampos()
    {
        var rol = new Rol("Recepcionista");

        Assert.NotEqual(Guid.Empty, rol.Id);
        Assert.Equal("Recepcionista", rol.Nombre);
        Assert.False(rol.EsSistema);
        Assert.Empty(rol.Permisos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Crear_ConNombreInvalido_LanzaArgumentException(string? nombre)
    {
        Assert.Throws<ArgumentException>(() => new Rol(nombre!));
    }

    [Fact]
    public void Renombrar_RolDeSistema_LanzaInvalidOperationException()
    {
        var rol = new Rol("Administrador", esSistema: true);

        Assert.Throws<InvalidOperationException>(() => rol.Renombrar("Otro"));
    }

    [Fact]
    public void Renombrar_RolNormal_ActualizaNombre()
    {
        var rol = new Rol("Original");
        rol.Renombrar("Nuevo");

        Assert.Equal("Nuevo", rol.Nombre);
    }

    [Fact]
    public void ReemplazarPermisos_RolDeSistema_LanzaInvalidOperationException()
    {
        var rol = new Rol("Administrador", esSistema: true);

        Assert.Throws<InvalidOperationException>(() => rol.ReemplazarPermisos(new[] { Guid.NewGuid() }));
    }

    [Fact]
    public void ReemplazarPermisos_QuitaAnterioresYAgregaNuevos()
    {
        var rol = new Rol("Custom");
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();

        rol.ReemplazarPermisos(new[] { p1, p2 });
        Assert.Equal(2, rol.Permisos.Count);

        rol.ReemplazarPermisos(new[] { p3 });
        Assert.Single(rol.Permisos);
        Assert.Contains(rol.Permisos, rp => rp.PermisoId == p3);
    }

    [Fact]
    public void ReemplazarPermisos_ConDuplicados_DeduplicaSilenciosamente()
    {
        var rol = new Rol("Custom");
        var p1 = Guid.NewGuid();

        rol.ReemplazarPermisos(new[] { p1, p1, p1 });

        Assert.Single(rol.Permisos);
    }
}
```

- [ ] **Step 3: Correr tests**

Run: `dotnet test backend/tests/GymFlow.Domain.Tests --filter "FullyQualifiedName~RolTests"`
Expected: 7 tests passed. (Tests dependen de `RolPermiso` que se crea en Task 4 — si compila falla acá, hacer Task 4 primero y volver.)

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Domain/Entities/Rol.cs backend/tests/GymFlow.Domain.Tests/Entities/RolTests.cs
git commit -m "feat(domain): add Rol entity with system-role invariants"
```

---

### Task 4: Entidad RolPermiso (join)

**Files:**
- Create: `backend/src/GymFlow.Domain/Entities/RolPermiso.cs`

- [ ] **Step 1: Crear entidad join**

```csharp
namespace GymFlow.Domain.Entities;

public class RolPermiso
{
    public Guid RolId { get; private set; }
    public Rol Rol { get; private set; } = null!;
    public Guid PermisoId { get; private set; }
    public Permiso Permiso { get; private set; } = null!;

    private RolPermiso() { } // EF Core

    public RolPermiso(Guid rolId, Guid permisoId)
    {
        RolId = rolId;
        PermisoId = permisoId;
    }
}
```

- [ ] **Step 2: Compilar**

Run: `dotnet build backend/src/GymFlow.Domain`
Expected: Build succeeded.

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Domain/Entities/RolPermiso.cs
git commit -m "feat(domain): add RolPermiso join entity"
```

---

### Task 5: Refactor Usuario — RolId reemplaza enum Rol

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/Usuario.cs`

- [ ] **Step 1: Reemplazar el archivo completo**

```csharp
namespace GymFlow.Domain.Entities;

public abstract class Usuario
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public string Correo { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Guid RolId { get; private set; }
    public Rol Rol { get; private set; } = null!;
    public bool EstaActivo { get; private set; } = true;
    public DateTime FechaCreacion { get; private set; }

    public ICollection<UsuarioUnidad> UnidadesAsignadas { get; private set; } = new List<UsuarioUnidad>();

    protected Usuario() { } // EF Core

    protected Usuario(string nombre, string apellido, string correo, string passwordHash, Guid rolId)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Apellido = !string.IsNullOrWhiteSpace(apellido) ? apellido : throw new ArgumentException("Apellido is required.", nameof(apellido));
        Correo = !string.IsNullOrWhiteSpace(correo) ? correo : throw new ArgumentException("Correo is required.", nameof(correo));
        PasswordHash = !string.IsNullOrWhiteSpace(passwordHash) ? passwordHash : throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        RolId = rolId != Guid.Empty ? rolId : throw new ArgumentException("RolId is required.", nameof(rolId));
        EstaActivo = true;
        FechaCreacion = DateTime.UtcNow;
    }

    public void Desactivar() => EstaActivo = false;
    public void Activar() => EstaActivo = true;

    public void ActualizarDatosBase(string nombre, string apellido, string correo)
    {
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Apellido = !string.IsNullOrWhiteSpace(apellido) ? apellido : throw new ArgumentException("Apellido is required.", nameof(apellido));
        Correo = !string.IsNullOrWhiteSpace(correo) ? correo : throw new ArgumentException("Correo is required.", nameof(correo));
    }
}
```

- [ ] **Step 2: Compilar (espera errores en Socio.cs)**

Run: `dotnet build backend/src/GymFlow.Domain`
Expected: Build FAILED — `Socio` aún pasa `Rol.Socio` enum.

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Domain/Entities/Usuario.cs
git commit -m "refactor(domain): replace Rol enum with RolId FK on Usuario"
```

---

### Task 6: Refactor Socio — recibe rolId en constructor

**Files:**
- Modify: `backend/src/GymFlow.Domain/Entities/Socio.cs`

- [ ] **Step 1: Cambiar firma del constructor y llamada a `base(...)`**

Reemplazar líneas 18-29 (el constructor público) — agregar `Guid rolSocioId` como primer parámetro y pasarlo al `base(...)`:

```csharp
    public Socio(
        Guid rolSocioId,
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
        : base(nombre, apellido, correo, passwordHash, rolSocioId)
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

- [ ] **Step 2: Quitar `using GymFlow.Domain.Enums;`** si solo era para `Rol` (verificar que `TipoDocumento` también está en `Enums` — si sí, dejar el using).

- [ ] **Step 3: Compilar Domain**

Run: `dotnet build backend/src/GymFlow.Domain`
Expected: Build FAILED en `Socio.cs` por uso de `Rol.Socio` ya removido — confirmar que el nuevo constructor compila. Si todavía hay otros usos del enum, aparecerán acá.

- [ ] **Step 4: Buscar y arreglar cualquier callsite de `Socio` en Application/Infrastructure**

Run: `dotnet build backend/GymFlow.sln`
Expected: errores en UseCases que crean `Socio`. Para cada error, agregar `rolSocioId` como primer parámetro. El `rolSocioId` debe venir de `IRolRepository.GetIdByNombreAsync("Socio")` (creado en Task 15) — por ahora, **dejar un `Guid.Empty` literal con un `// TODO RF-23: leer del repo de roles`** y arreglarlo cuando esté el repo en Task 16.

- [ ] **Step 5: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Domain/Entities/Socio.cs backend/src/GymFlow.Application
git commit -m "refactor(domain): Socio constructor receives rolId instead of using Rol enum"
```

---

### Task 7: Borrar enum Rol y arreglar últimos call sites

**Files:**
- Delete: `backend/src/GymFlow.Domain/Enums/Rol.cs`
- Modify: `backend/src/GymFlow.API/Controllers/AuthController.cs` (línea 53)

- [ ] **Step 1: Borrar el archivo del enum**

```bash
rm backend/src/GymFlow.Domain/Enums/Rol.cs
```

- [ ] **Step 2: Compilar la solución completa**

Run: `dotnet build backend/GymFlow.sln`
Expected: errores en `AuthController.cs:53` (`user.Rol.ToLower()` — el `user` ahí es el `HardcodedUser` con propiedad string, así que probablemente compile, pero verificar).

- [ ] **Step 3: Si hay más errores, arreglar uno por uno**

Para cada error de "type or namespace 'Rol' could not be found": removerlo o reemplazar la lógica. Si era un `[Authorize(Roles = "Admin")]`, dejarlo como `[Authorize]` por ahora (se reemplaza por `[RequierePermiso]` en Task 19).

- [ ] **Step 4: Compilar y verificar**

Run: `dotnet build backend/GymFlow.sln`
Expected: Build succeeded (puede haber warnings).

- [ ] **Step 5: Commit (esperar OK del usuario)**

```bash
git add -A
git commit -m "refactor(domain): remove Rol enum"
```

---

## Phase 2 — Infrastructure (Persistence)

### Task 8: EF Configurations para Rol, Permiso, RolPermiso

**Files:**
- Create: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/RolConfiguration.cs`
- Create: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/PermisoConfiguration.cs`
- Create: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/RolPermisoConfiguration.cs`

- [ ] **Step 1: `RolConfiguration.cs`**

```csharp
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.Nombre).IsUnique();
        builder.Property(r => r.EsSistema).IsRequired();
        builder.Property(r => r.FechaCreacion).IsRequired();

        builder.HasMany(r => r.Permisos)
            .WithOne(rp => rp.Rol)
            .HasForeignKey(rp => rp.RolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: `PermisoConfiguration.cs`**

```csharp
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("Permisos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Modulo).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Operacion).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(p => new { p.Modulo, p.Operacion }).IsUnique();
    }
}
```

- [ ] **Step 3: `RolPermisoConfiguration.cs`**

```csharp
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class RolPermisoConfiguration : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("RolPermisos");
        builder.HasKey(rp => new { rp.RolId, rp.PermisoId });

        builder.HasOne(rp => rp.Permiso)
            .WithMany()
            .HasForeignKey(rp => rp.PermisoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 4: Compilar**

Run: `dotnet build backend/src/GymFlow.Infrastructure`
Expected: Build succeeded.

- [ ] **Step 5: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Infrastructure/Persistence/Configurations/RolConfiguration.cs backend/src/GymFlow.Infrastructure/Persistence/Configurations/PermisoConfiguration.cs backend/src/GymFlow.Infrastructure/Persistence/Configurations/RolPermisoConfiguration.cs
git commit -m "feat(infra): EF configurations for Rol, Permiso, RolPermiso"
```

---

### Task 9: Actualizar UsuarioConfiguration con FK RolId

**Files:**
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs`

- [ ] **Step 1: Reemplazar el archivo completo**

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
            .HasValue<Socio>("Socio");

        builder.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Apellido).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Correo).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Correo).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
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

- [ ] **Step 2: Compilar**

Run: `dotnet build backend/src/GymFlow.Infrastructure`
Expected: Build succeeded.

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs
git commit -m "refactor(infra): UsuarioConfiguration uses RolId FK"
```

---

### Task 10: Registrar DbSets en GymFlowDbContext

**Files:**
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`

- [ ] **Step 1: Leer el archivo**

Run: `cat backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`
(Para ver dónde van los `DbSet`s actuales y agregar los nuevos al lado.)

- [ ] **Step 2: Agregar tres `DbSet`s nuevos** justo después del último existente:

```csharp
public DbSet<Rol> Roles => Set<Rol>();
public DbSet<Permiso> Permisos => Set<Permiso>();
public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
```

- [ ] **Step 3: Compilar**

Run: `dotnet build backend/src/GymFlow.Infrastructure`
Expected: Build succeeded.

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs
git commit -m "feat(infra): register Rol/Permiso/RolPermiso DbSets"
```

---

### Task 11: Crear migración EF Core

**Files:**
- Auto-genera archivos en `backend/src/GymFlow.Infrastructure/Persistence/Migrations/`

- [ ] **Step 1: Generar migración**

Run desde la raíz del repo:
```bash
dotnet ef migrations add AddRolesYPermisos --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API
```
Expected: archivos `<timestamp>_AddRolesYPermisos.cs` y `.Designer.cs` creados.

- [ ] **Step 2: Inspeccionar la migración**

Verificar manualmente que:
- Crea tablas `Roles`, `Permisos`, `RolPermisos`.
- Agrega columna `RolId` a `Usuarios` con FK.
- Elimina la columna `Rol` (string) de `Usuarios`.

Si la migración intenta DROP la columna `Rol` antes de crear `RolId`/asignar valores, **agregar manualmente** entre los `migrationBuilder.AddColumn<Guid>(...)` de RolId y el `DropColumn` un bloque `Sql(...)` que asigne el RolId del admin a todos los registros existentes — pero como **no hay datos productivos** (todo está hardcodeado), esto se puede saltear: la migración va a correr contra una DB vacía.

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Infrastructure/Persistence/Migrations
git commit -m "feat(infra): migration AddRolesYPermisos"
```

---

### Task 12: Seed data en GymFlowDbContext.OnModelCreating

**Files:**
- Modify: `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`

- [ ] **Step 1: Definir constantes con GUIDs fijos**

Crear una clase estática nueva al final del archivo (o en un archivo separado `SeedData.cs`):

```csharp
public static class RolSeed
{
    public static readonly Guid AdminRolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SocioRolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly DateTime SeedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
```

- [ ] **Step 2: En `OnModelCreating` después del `ApplyConfigurationsFromAssembly`, agregar seed**

```csharp
// Seed Roles
modelBuilder.Entity<Rol>().HasData(
    new { Id = RolSeed.AdminRolId, Nombre = "Administrador", EsSistema = true, FechaCreacion = RolSeed.SeedTimestamp },
    new { Id = RolSeed.SocioRolId, Nombre = "Socio", EsSistema = true, FechaCreacion = RolSeed.SeedTimestamp }
);

// Seed Permisos (producto cartesiano de Modulo × Operacion)
var modulos = Enum.GetValues<Modulo>();
var operaciones = Enum.GetValues<Operacion>();
var permisoSeeds = new List<object>();
var permisoIds = new Dictionary<(Modulo, Operacion), Guid>();

foreach (var m in modulos)
{
    foreach (var o in operaciones)
    {
        var id = DeterministicGuid($"{m}-{o}");
        permisoIds[(m, o)] = id;
        permisoSeeds.Add(new { Id = id, Modulo = m, Operacion = o });
    }
}
modelBuilder.Entity<Permiso>().HasData(permisoSeeds);

// Seed RolPermisos: Admin tiene todos
var rolPermisoSeeds = permisoIds.Values
    .Select(pid => new { RolId = RolSeed.AdminRolId, PermisoId = pid })
    .Cast<object>()
    .ToList();
modelBuilder.Entity<RolPermiso>().HasData(rolPermisoSeeds);
```

Y agregar este helper al final de la clase:

```csharp
private static Guid DeterministicGuid(string input)
{
    using var md5 = System.Security.Cryptography.MD5.Create();
    var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
    return new Guid(hash);
}
```

Y los `using`:

```csharp
using GymFlow.Domain.Enums;
```

- [ ] **Step 3: Generar nueva migración con el seed**

Run:
```bash
dotnet ef migrations add SeedRolesYPermisos --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API
```
Expected: archivos generados con `InsertData(...)` calls.

- [ ] **Step 4: Inspeccionar la migración**

Verificar que `InsertData` aparece para `Roles` (2 filas), `Permisos` (16 filas) y `RolPermisos` (16 filas — admin con todos los permisos).

- [ ] **Step 5: Compilar**

Run: `dotnet build backend/GymFlow.sln`
Expected: Build succeeded.

- [ ] **Step 6: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Infrastructure
git commit -m "feat(infra): seed Roles, Permisos y RolPermisos del Administrador"
```

---

### Task 13: Aplicar migración

- [ ] **Step 1: Asegurarse de que PostgreSQL está corriendo**

Run: `docker compose up -d db` (o el comando equivalente del proyecto).

- [ ] **Step 2: Aplicar migraciones**

Run:
```bash
dotnet ef database update --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API
```
Expected: "Done." sin errores.

- [ ] **Step 3: Verificar en la DB**

Conectarse a PostgreSQL y correr:
```sql
SELECT COUNT(*) FROM "Roles";    -- 2
SELECT COUNT(*) FROM "Permisos"; -- 16
SELECT COUNT(*) FROM "RolPermisos"; -- 16
```

- [ ] **Step 4: No hay commit (es estado de DB)**

---

## Phase 3 — Application: Permission infrastructure

### Task 14: PermisoCache (cache en memoria)

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/IPermisoCache.cs`
- Create: `backend/src/GymFlow.Infrastructure/Services/PermisoCache.cs`

- [ ] **Step 1: Interface**

```csharp
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface IPermisoCache
{
    Task<bool> TienePermisoAsync(Guid rolId, Modulo modulo, Operacion operacion, CancellationToken ct = default);
    Task<IReadOnlyList<(Modulo Modulo, Operacion Operacion)>> ObtenerPermisosAsync(Guid rolId, CancellationToken ct = default);
    void Invalidar(Guid rolId);
}
```

- [ ] **Step 2: Implementación**

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GymFlow.Infrastructure.Services;

public class PermisoCache : IPermisoCache
{
    private readonly IMemoryCache _cache;
    private readonly GymFlowDbContext _db;
    private static readonly TimeSpan TTL = TimeSpan.FromMinutes(30);

    public PermisoCache(IMemoryCache cache, GymFlowDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public async Task<bool> TienePermisoAsync(Guid rolId, Modulo modulo, Operacion operacion, CancellationToken ct = default)
    {
        var permisos = await ObtenerPermisosAsync(rolId, ct);
        return permisos.Any(p => p.Modulo == modulo && p.Operacion == operacion);
    }

    public async Task<IReadOnlyList<(Modulo Modulo, Operacion Operacion)>> ObtenerPermisosAsync(Guid rolId, CancellationToken ct = default)
    {
        var key = CacheKey(rolId);
        if (_cache.TryGetValue<IReadOnlyList<(Modulo, Operacion)>>(key, out var cached) && cached is not null)
            return cached;

        var permisos = await _db.RolPermisos
            .Where(rp => rp.RolId == rolId)
            .Select(rp => new { rp.Permiso.Modulo, rp.Permiso.Operacion })
            .ToListAsync(ct);

        var result = permisos.Select(p => (p.Modulo, p.Operacion)).ToList();
        _cache.Set(key, (IReadOnlyList<(Modulo, Operacion)>)result, TTL);
        return result;
    }

    public void Invalidar(Guid rolId) => _cache.Remove(CacheKey(rolId));

    private static string CacheKey(Guid rolId) => $"permisos:rol:{rolId}";
}
```

- [ ] **Step 3: Compilar**

Run: `dotnet build backend/src/GymFlow.Infrastructure`
Expected: Build succeeded.

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Application/Interfaces/IPermisoCache.cs backend/src/GymFlow.Infrastructure/Services/PermisoCache.cs
git commit -m "feat(infra): in-memory permission cache"
```

---

### Task 15: IRolRepository interface

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/IRolRepository.cs`

- [ ] **Step 1: Crear interface**

```csharp
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IRolRepository
{
    Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken ct = default);
    Task<Rol?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Rol?> GetByNombreAsync(string nombre, CancellationToken ct = default);
    Task<bool> ExisteConNombreAsync(string nombre, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> TieneUsuariosAsignadosAsync(Guid rolId, CancellationToken ct = default);
    Task AddAsync(Rol rol, CancellationToken ct = default);
    void Remove(Rol rol);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Compilar**

Run: `dotnet build backend/src/GymFlow.Application`
Expected: Build succeeded.

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Application/Interfaces/IRolRepository.cs
git commit -m "feat(application): add IRolRepository interface"
```

---

### Task 16: RolRepository implementation y arreglar TODO de Socio

**Files:**
- Create: `backend/src/GymFlow.Infrastructure/Repositories/RolRepository.cs`
- Modify: cualquier UseCase que tenía `Guid.Empty // TODO RF-23` (ver Task 6, Step 4)

- [ ] **Step 1: Implementación**

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class RolRepository : IRolRepository
{
    private readonly GymFlowDbContext _db;

    public RolRepository(GymFlowDbContext db) => _db = db;

    public async Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Roles.Include(r => r.Permisos).OrderBy(r => r.Nombre).ToListAsync(ct);

    public async Task<Rol?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Roles.Include(r => r.Permisos).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Rol?> GetByNombreAsync(string nombre, CancellationToken ct = default) =>
        await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre, ct);

    public async Task<bool> ExisteConNombreAsync(string nombre, Guid? excludeId = null, CancellationToken ct = default) =>
        await _db.Roles.AnyAsync(r => r.Nombre == nombre && (excludeId == null || r.Id != excludeId), ct);

    public async Task<bool> TieneUsuariosAsignadosAsync(Guid rolId, CancellationToken ct = default) =>
        await _db.Set<Usuario>().AnyAsync(u => u.RolId == rolId, ct);

    public async Task AddAsync(Rol rol, CancellationToken ct = default) =>
        await _db.Roles.AddAsync(rol, ct);

    public void Remove(Rol rol) => _db.Roles.Remove(rol);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
```

- [ ] **Step 2: Buscar y arreglar TODOs de Task 6**

Run: `grep -rn "TODO RF-23" backend/src`

Para cada match, inyectar `IRolRepository` en el constructor del UseCase y reemplazar `Guid.Empty` por:
```csharp
var rolSocio = await _rolRepository.GetByNombreAsync("Socio", ct)
    ?? throw new InvalidOperationException("Rol 'Socio' no encontrado en seed data.");
// usar rolSocio.Id como rolSocioId
```

- [ ] **Step 3: Compilar**

Run: `dotnet build backend/GymFlow.sln`
Expected: Build succeeded.

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add backend/src
git commit -m "feat(infra): RolRepository and resolve Socio rolId TODOs"
```

---

### Task 17: Registrar servicios nuevos en DI

**Files:**
- Modify: `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Leer el archivo**

Run: `cat backend/src/GymFlow.Infrastructure/DependencyInjection.cs`

- [ ] **Step 2: Agregar registros** dentro del método existente, donde se registran los otros repos:

```csharp
services.AddScoped<IRolRepository, RolRepository>();
services.AddScoped<IPermisoCache, PermisoCache>();
services.AddMemoryCache(); // si no estaba ya
```

Y los `using`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Infrastructure.Repositories;
using GymFlow.Infrastructure.Services;
```

- [ ] **Step 3: Compilar**

Run: `dotnet build backend/src/GymFlow.Infrastructure`
Expected: Build succeeded.

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Infrastructure/DependencyInjection.cs
git commit -m "chore(infra): register IRolRepository and IPermisoCache"
```

---

## Phase 4 — API: Authorization

### Task 18: Atributo RequierePermiso

**Files:**
- Create: `backend/src/GymFlow.API/Authorization/RequierePermisoAttribute.cs`

- [ ] **Step 1: Crear atributo**

```csharp
using System.Security.Claims;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymFlow.API.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequierePermisoAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly Modulo _modulo;
    private readonly Operacion _operacion;

    public RequierePermisoAttribute(Modulo modulo, Operacion operacion)
    {
        _modulo = modulo;
        _operacion = operacion;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var rolIdClaim = user.FindFirst("rolId")?.Value;
        if (!Guid.TryParse(rolIdClaim, out var rolId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IPermisoCache>();
        var tiene = await cache.TienePermisoAsync(rolId, _modulo, _operacion);
        if (!tiene)
        {
            context.Result = new ForbidResult();
        }
    }
}
```

- [ ] **Step 2: Test del atributo**

Crear `backend/tests/GymFlow.Application.Tests/Authorization/RequierePermisoAttributeTests.cs` (note: este test va en Application.Tests porque API no tiene proyecto de tests; usamos un ControllerContext fake):

```csharp
using System.Security.Claims;
using GymFlow.API.Authorization;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.Authorization;

public class RequierePermisoAttributeTests
{
    [Fact]
    public async Task SinAutenticar_DevuelveUnauthorized()
    {
        var attr = new RequierePermisoAttribute(Modulo.Socios, Operacion.Lectura);
        var context = BuildContext(authenticated: false);

        await attr.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public async Task SinClaimRolId_DevuelveForbid()
    {
        var attr = new RequierePermisoAttribute(Modulo.Socios, Operacion.Lectura);
        var context = BuildContext(authenticated: true, rolId: null);

        await attr.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task ConPermiso_NoSeteaResult()
    {
        var rolId = Guid.NewGuid();
        var cache = new Mock<IPermisoCache>();
        cache.Setup(c => c.TienePermisoAsync(rolId, Modulo.Socios, Operacion.Lectura, default))
            .ReturnsAsync(true);

        var attr = new RequierePermisoAttribute(Modulo.Socios, Operacion.Lectura);
        var context = BuildContext(authenticated: true, rolId: rolId, cache: cache.Object);

        await attr.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task SinPermiso_DevuelveForbid()
    {
        var rolId = Guid.NewGuid();
        var cache = new Mock<IPermisoCache>();
        cache.Setup(c => c.TienePermisoAsync(rolId, Modulo.Socios, Operacion.Eliminacion, default))
            .ReturnsAsync(false);

        var attr = new RequierePermisoAttribute(Modulo.Socios, Operacion.Eliminacion);
        var context = BuildContext(authenticated: true, rolId: rolId, cache: cache.Object);

        await attr.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    private static AuthorizationFilterContext BuildContext(bool authenticated, Guid? rolId = null, IPermisoCache? cache = null)
    {
        var httpContext = new DefaultHttpContext();
        if (authenticated)
        {
            var claims = new List<Claim>();
            if (rolId.HasValue) claims.Add(new Claim("rolId", rolId.Value.ToString()));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
        var services = new ServiceCollection();
        services.AddSingleton(cache ?? Mock.Of<IPermisoCache>());
        httpContext.RequestServices = services.BuildServiceProvider();

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
```

- [ ] **Step 3: Agregar referencia API → tests si no existe**

Verificar `backend/tests/GymFlow.Application.Tests/GymFlow.Application.Tests.csproj` que tenga:
```xml
<ProjectReference Include="..\..\src\GymFlow.API\GymFlow.API.csproj" />
```
Si no, agregarla.

- [ ] **Step 4: Correr tests**

Run: `dotnet test backend/tests/GymFlow.Application.Tests --filter "FullyQualifiedName~RequierePermisoAttributeTests"`
Expected: 4 tests passed.

- [ ] **Step 5: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.API/Authorization backend/tests/GymFlow.Application.Tests
git commit -m "feat(api): RequierePermiso authorization attribute with tests"
```

---

### Task 19: Aplicar [RequierePermiso] a controladores existentes

**Files:**
- Modify: `backend/src/GymFlow.API/Controllers/SociosController.cs`
- Modify: `backend/src/GymFlow.API/Controllers/PlanesController.cs`
- Modify: `backend/src/GymFlow.API/Controllers/UnidadesController.cs`
- Modify: `backend/src/GymFlow.API/Controllers/AuditoriaController.cs`

- [ ] **Step 1: SociosController**

Agregar `using GymFlow.API.Authorization; using GymFlow.Domain.Enums;` y aplicar atributos:
- `GET (lista, byId)` → `[RequierePermiso(Modulo.Socios, Operacion.Lectura)]`
- `POST` → `[RequierePermiso(Modulo.Socios, Operacion.Escritura)]`
- `PUT` → `[RequierePermiso(Modulo.Socios, Operacion.Modificacion)]`
- `DELETE` → `[RequierePermiso(Modulo.Socios, Operacion.Eliminacion)]`

- [ ] **Step 2: PlanesController** — idéntico patrón con `Modulo.Planes`.

- [ ] **Step 3: UnidadesController** — `Modulo.Unidades`.

- [ ] **Step 4: AuditoriaController** — solo `Modulo.Auditoria, Operacion.Lectura` para todos los GET.

- [ ] **Step 5: Compilar**

Run: `dotnet build backend/GymFlow.sln`
Expected: Build succeeded.

- [ ] **Step 6: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.API/Controllers
git commit -m "feat(api): apply RequierePermiso to existing controllers"
```

---

## Phase 5 — Application: UseCases para Roles

### Task 20: DTOs de Roles y Permisos

**Files:**
- Create: `backend/src/GymFlow.Application/DTOs/PermisoDto.cs`
- Create: `backend/src/GymFlow.Application/DTOs/RolDto.cs`
- Create: `backend/src/GymFlow.Application/DTOs/CrearRolRequest.cs`
- Create: `backend/src/GymFlow.Application/DTOs/ActualizarRolRequest.cs`

- [ ] **Step 1: PermisoDto**

```csharp
using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record PermisoDto(Guid Id, Modulo Modulo, Operacion Operacion);
```

- [ ] **Step 2: RolDto**

```csharp
namespace GymFlow.Application.DTOs;

public record RolDto(
    Guid Id,
    string Nombre,
    bool EsSistema,
    DateTime FechaCreacion,
    IReadOnlyList<PermisoDto> Permisos);
```

- [ ] **Step 3: CrearRolRequest**

```csharp
namespace GymFlow.Application.DTOs;

public record CrearRolRequest(string Nombre, IReadOnlyList<Guid> PermisoIds);
```

- [ ] **Step 4: ActualizarRolRequest**

```csharp
namespace GymFlow.Application.DTOs;

public record ActualizarRolRequest(string Nombre, IReadOnlyList<Guid> PermisoIds);
```

- [ ] **Step 5: Compilar y commit (esperar OK del usuario)**

Run: `dotnet build backend/src/GymFlow.Application`
```bash
git add backend/src/GymFlow.Application/DTOs
git commit -m "feat(application): DTOs for roles and permissions"
```

---

### Task 21: Queries (GetRoles, GetRolById, GetPermisos)

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Roles/GetRolesQuery.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Roles/GetRolByIdQuery.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Permisos/GetPermisosQuery.cs`
- Create: `backend/src/GymFlow.Application/Interfaces/IPermisoRepository.cs`
- Create: `backend/src/GymFlow.Infrastructure/Repositories/PermisoRepository.cs`

- [ ] **Step 1: IPermisoRepository**

```csharp
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IPermisoRepository
{
    Task<IReadOnlyList<Permiso>> GetAllAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: PermisoRepository**

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class PermisoRepository : IPermisoRepository
{
    private readonly GymFlowDbContext _db;
    public PermisoRepository(GymFlowDbContext db) => _db = db;

    public async Task<IReadOnlyList<Permiso>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Permisos.OrderBy(p => p.Modulo).ThenBy(p => p.Operacion).ToListAsync(ct);
}
```

- [ ] **Step 3: Registrar en DI** (`backend/src/GymFlow.Infrastructure/DependencyInjection.cs`):

```csharp
services.AddScoped<IPermisoRepository, PermisoRepository>();
```

- [ ] **Step 4: GetRolesQuery**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Roles;

public class GetRolesQuery
{
    private readonly IRolRepository _rolRepository;

    public GetRolesQuery(IRolRepository rolRepository) => _rolRepository = rolRepository;

    public async Task<IReadOnlyList<RolDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var roles = await _rolRepository.GetAllAsync(ct);
        return roles.Select(r => new RolDto(
            r.Id,
            r.Nombre,
            r.EsSistema,
            r.FechaCreacion,
            r.Permisos.Select(rp => new PermisoDto(rp.Permiso.Id, rp.Permiso.Modulo, rp.Permiso.Operacion)).ToList()
        )).ToList();
    }
}
```

Nota: `GetAllAsync` del repo ya hace `Include(r => r.Permisos)`. Para que `rp.Permiso` no sea null, ajustar `RolRepository.GetAllAsync` a `.Include(r => r.Permisos).ThenInclude(rp => rp.Permiso)`.

- [ ] **Step 5: Ajustar RolRepository.GetAllAsync y GetByIdAsync**

```csharp
public async Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken ct = default) =>
    await _db.Roles
        .Include(r => r.Permisos).ThenInclude(rp => rp.Permiso)
        .OrderBy(r => r.Nombre)
        .ToListAsync(ct);

public async Task<Rol?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
    await _db.Roles
        .Include(r => r.Permisos).ThenInclude(rp => rp.Permiso)
        .FirstOrDefaultAsync(r => r.Id == id, ct);
```

- [ ] **Step 6: GetRolByIdQuery**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Roles;

public class GetRolByIdQuery
{
    private readonly IRolRepository _rolRepository;

    public GetRolByIdQuery(IRolRepository rolRepository) => _rolRepository = rolRepository;

    public async Task<RolDto> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var rol = await _rolRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Rol {id} no encontrado.");

        return new RolDto(
            rol.Id, rol.Nombre, rol.EsSistema, rol.FechaCreacion,
            rol.Permisos.Select(rp => new PermisoDto(rp.Permiso.Id, rp.Permiso.Modulo, rp.Permiso.Operacion)).ToList()
        );
    }
}
```

- [ ] **Step 7: GetPermisosQuery**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Permisos;

public class GetPermisosQuery
{
    private readonly IPermisoRepository _permisoRepository;

    public GetPermisosQuery(IPermisoRepository permisoRepository) => _permisoRepository = permisoRepository;

    public async Task<IReadOnlyList<PermisoDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var permisos = await _permisoRepository.GetAllAsync(ct);
        return permisos.Select(p => new PermisoDto(p.Id, p.Modulo, p.Operacion)).ToList();
    }
}
```

- [ ] **Step 8: Registrar UseCases en DI API**

En `backend/src/GymFlow.API/DependencyInjection.cs` (o donde se registren los otros UseCases — `cat` y agregar al lado):

```csharp
services.AddScoped<GetRolesQuery>();
services.AddScoped<GetRolByIdQuery>();
services.AddScoped<GetPermisosQuery>();
```

- [ ] **Step 9: Compilar y commit (esperar OK del usuario)**

Run: `dotnet build backend/GymFlow.sln`

```bash
git add backend/src
git commit -m "feat(application): queries for roles and permissions"
```

---

### Task 22: CrearRolCommand con tests

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Roles/CrearRolCommand.cs`
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Roles/CrearRolCommandTests.cs`

- [ ] **Step 1: Command**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Roles;

public class CrearRolCommand
{
    private readonly IRolRepository _rolRepository;
    private readonly IAuditLogger _auditLogger;

    public CrearRolCommand(IRolRepository rolRepository, IAuditLogger auditLogger)
    {
        _rolRepository = rolRepository;
        _auditLogger = auditLogger;
    }

    public async Task<RolDto> ExecuteAsync(CrearRolRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(request));

        if (await _rolRepository.ExisteConNombreAsync(request.Nombre, null, ct))
            throw new InvalidOperationException($"Ya existe un rol con el nombre '{request.Nombre}'.");

        var rol = new Rol(request.Nombre);
        rol.ReemplazarPermisos(request.PermisoIds ?? new List<Guid>());

        await _rolRepository.AddAsync(rol, ct);
        await _rolRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Rol", rol.Id,
            $"Se creó el rol {rol.Nombre} con {rol.Permisos.Count} permisos");

        return new RolDto(rol.Id, rol.Nombre, rol.EsSistema, rol.FechaCreacion,
            rol.Permisos.Select(rp => new PermisoDto(rp.PermisoId, default, default)).ToList());
    }
}
```

Nota: el `RolDto` post-creación tiene los `PermisoDto` con `default` Modulo/Operacion porque los Permiso navigations no están cargados. El consumer típico hace un `GET /api/roles/{id}` después si necesita el detalle. Aceptable para el caso de uso.

- [ ] **Step 2: Tests**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Roles;

public class CrearRolCommandTests
{
    [Fact]
    public async Task NombreVacio_LanzaArgumentException()
    {
        var sut = new CrearRolCommand(Mock.Of<IRolRepository>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(new CrearRolRequest("", new List<Guid>()), Guid.NewGuid(), "Test"));
    }

    [Fact]
    public async Task NombreDuplicado_LanzaInvalidOperationException()
    {
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.ExisteConNombreAsync("Recepcionista", null, default)).ReturnsAsync(true);

        var sut = new CrearRolCommand(repo.Object, Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(new CrearRolRequest("Recepcionista", new List<Guid>()), Guid.NewGuid(), "Test"));
    }

    [Fact]
    public async Task HappyPath_CreaRolYAuditEs()
    {
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.ExisteConNombreAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        var audit = new Mock<IAuditLogger>();

        var sut = new CrearRolCommand(repo.Object, audit.Object);
        var permiso1 = Guid.NewGuid();

        var dto = await sut.ExecuteAsync(
            new CrearRolRequest("Recepcionista", new[] { permiso1 }),
            Guid.NewGuid(), "Admin Test");

        Assert.Equal("Recepcionista", dto.Nombre);
        Assert.False(dto.EsSistema);
        repo.Verify(r => r.AddAsync(It.IsAny<Rol>(), default), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Creacion, "Rol", It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
    }
}
```

- [ ] **Step 3: Correr tests**

Run: `dotnet test backend/tests/GymFlow.Application.Tests --filter "FullyQualifiedName~CrearRolCommandTests"`
Expected: 3 tests passed.

- [ ] **Step 4: Registrar en DI**

En `backend/src/GymFlow.API/DependencyInjection.cs`:
```csharp
services.AddScoped<CrearRolCommand>();
```

- [ ] **Step 5: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.Application/UseCases/Roles/CrearRolCommand.cs backend/tests/GymFlow.Application.Tests/UseCases/Roles/CrearRolCommandTests.cs backend/src/GymFlow.API/DependencyInjection.cs
git commit -m "feat(application): CrearRolCommand with tests"
```

---

### Task 23: ActualizarRolCommand con tests

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Roles/ActualizarRolCommand.cs`
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Roles/ActualizarRolCommandTests.cs`

- [ ] **Step 1: Command**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Roles;

public class ActualizarRolCommand
{
    private readonly IRolRepository _rolRepository;
    private readonly IPermisoCache _cache;
    private readonly IAuditLogger _auditLogger;

    public ActualizarRolCommand(IRolRepository rolRepository, IPermisoCache cache, IAuditLogger auditLogger)
    {
        _rolRepository = rolRepository;
        _cache = cache;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, ActualizarRolRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        var rol = await _rolRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Rol {id} no encontrado.");

        if (rol.EsSistema)
            throw new InvalidOperationException("No se puede modificar un rol del sistema.");

        if (await _rolRepository.ExisteConNombreAsync(request.Nombre, id, ct))
            throw new InvalidOperationException($"Ya existe otro rol con el nombre '{request.Nombre}'.");

        rol.Renombrar(request.Nombre);
        rol.ReemplazarPermisos(request.PermisoIds ?? new List<Guid>());

        await _rolRepository.SaveChangesAsync(ct);
        _cache.Invalidar(rol.Id);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Rol", rol.Id,
            $"Se modificó el rol {rol.Nombre} ({rol.Permisos.Count} permisos)");
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Domain.Entities;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Roles;

public class ActualizarRolCommandTests
{
    [Fact]
    public async Task RolNoExiste_LanzaKeyNotFoundException()
    {
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Rol?)null);

        var sut = new ActualizarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(Guid.NewGuid(), new ActualizarRolRequest("X", new List<Guid>()), Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task RolDeSistema_LanzaInvalidOperationException()
    {
        var rol = new Rol("Administrador", esSistema: true);
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);

        var sut = new ActualizarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(rol.Id, new ActualizarRolRequest("X", new List<Guid>()), Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task NombreDuplicado_LanzaInvalidOperationException()
    {
        var rol = new Rol("Original");
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);
        repo.Setup(r => r.ExisteConNombreAsync("Otro", rol.Id, default)).ReturnsAsync(true);

        var sut = new ActualizarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(rol.Id, new ActualizarRolRequest("Otro", new List<Guid>()), Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task HappyPath_ActualizaInvalidaCacheYAuditEs()
    {
        var rol = new Rol("Original");
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);
        repo.Setup(r => r.ExisteConNombreAsync(It.IsAny<string>(), rol.Id, default)).ReturnsAsync(false);
        var cache = new Mock<IPermisoCache>();
        var audit = new Mock<IAuditLogger>();

        var sut = new ActualizarRolCommand(repo.Object, cache.Object, audit.Object);

        await sut.ExecuteAsync(rol.Id, new ActualizarRolRequest("Nuevo", new List<Guid>()), Guid.NewGuid(), "T");

        Assert.Equal("Nuevo", rol.Nombre);
        cache.Verify(c => c.Invalidar(rol.Id), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<GymFlow.Domain.Enums.TipoAccionAuditoria>(), "Rol", rol.Id, It.IsAny<string>()), Times.Once);
    }
}
```

- [ ] **Step 3: Correr tests, registrar en DI, commit**

Run: `dotnet test backend/tests/GymFlow.Application.Tests --filter "FullyQualifiedName~ActualizarRolCommandTests"`
Expected: 4 tests passed.

Agregar a `DependencyInjection.cs`: `services.AddScoped<ActualizarRolCommand>();`

```bash
git add backend/src/GymFlow.Application/UseCases/Roles/ActualizarRolCommand.cs backend/tests backend/src/GymFlow.API/DependencyInjection.cs
git commit -m "feat(application): ActualizarRolCommand with tests"
```

---

### Task 24: EliminarRolCommand con tests

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Roles/EliminarRolCommand.cs`
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/Roles/EliminarRolCommandTests.cs`

- [ ] **Step 1: Command**

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Roles;

public class EliminarRolCommand
{
    private readonly IRolRepository _rolRepository;
    private readonly IPermisoCache _cache;
    private readonly IAuditLogger _auditLogger;

    public EliminarRolCommand(IRolRepository rolRepository, IPermisoCache cache, IAuditLogger auditLogger)
    {
        _rolRepository = rolRepository;
        _cache = cache;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        var rol = await _rolRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Rol {id} no encontrado.");

        if (rol.EsSistema)
            throw new InvalidOperationException("No se puede eliminar un rol del sistema.");

        if (await _rolRepository.TieneUsuariosAsignadosAsync(id, ct))
            throw new InvalidOperationException("No se puede eliminar un rol con usuarios asignados.");

        _rolRepository.Remove(rol);
        await _rolRepository.SaveChangesAsync(ct);
        _cache.Invalidar(id);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Eliminacion, "Rol", id,
            $"Se eliminó el rol {rol.Nombre}");
    }
}
```

- [ ] **Step 2: Tests** (4 cases: no existe, es sistema, tiene usuarios, happy path)

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Domain.Entities;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Roles;

public class EliminarRolCommandTests
{
    [Fact]
    public async Task RolNoExiste_LanzaKeyNotFoundException()
    {
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Rol?)null);

        var sut = new EliminarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task RolDeSistema_LanzaInvalidOperationException()
    {
        var rol = new Rol("Admin", esSistema: true);
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);

        var sut = new EliminarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(rol.Id, Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task RolConUsuarios_LanzaInvalidOperationException()
    {
        var rol = new Rol("Recepcionista");
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);
        repo.Setup(r => r.TieneUsuariosAsignadosAsync(rol.Id, default)).ReturnsAsync(true);

        var sut = new EliminarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(rol.Id, Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task HappyPath_EliminaInvalidaCacheYAuditEs()
    {
        var rol = new Rol("Recepcionista");
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);
        repo.Setup(r => r.TieneUsuariosAsignadosAsync(rol.Id, default)).ReturnsAsync(false);
        var cache = new Mock<IPermisoCache>();
        var audit = new Mock<IAuditLogger>();

        var sut = new EliminarRolCommand(repo.Object, cache.Object, audit.Object);
        await sut.ExecuteAsync(rol.Id, Guid.NewGuid(), "T");

        repo.Verify(r => r.Remove(rol), Times.Once);
        cache.Verify(c => c.Invalidar(rol.Id), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(), GymFlow.Domain.Enums.TipoAccionAuditoria.Eliminacion, "Rol", rol.Id, It.IsAny<string>()), Times.Once);
    }
}
```

- [ ] **Step 3: Tests, DI, commit**

Run: `dotnet test backend/tests/GymFlow.Application.Tests --filter "FullyQualifiedName~EliminarRolCommandTests"`
Expected: 4 tests passed.

Agregar a `DependencyInjection.cs`: `services.AddScoped<EliminarRolCommand>();`

```bash
git add backend/src/GymFlow.Application/UseCases/Roles/EliminarRolCommand.cs backend/tests backend/src/GymFlow.API/DependencyInjection.cs
git commit -m "feat(application): EliminarRolCommand with tests"
```

---

## Phase 6 — API: Nuevos controllers + AuthController update

### Task 25: RolesController

**Files:**
- Create: `backend/src/GymFlow.API/Controllers/RolesController.cs`

- [ ] **Step 1: Controller**

```csharp
using GymFlow.API.Authorization;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly GetRolesQuery _getRoles;
    private readonly GetRolByIdQuery _getRolById;
    private readonly CrearRolCommand _crearRol;
    private readonly ActualizarRolCommand _actualizarRol;
    private readonly EliminarRolCommand _eliminarRol;

    public RolesController(
        GetRolesQuery getRoles, GetRolByIdQuery getRolById,
        CrearRolCommand crearRol, ActualizarRolCommand actualizarRol, EliminarRolCommand eliminarRol)
    {
        _getRoles = getRoles;
        _getRolById = getRolById;
        _crearRol = crearRol;
        _actualizarRol = actualizarRol;
        _eliminarRol = eliminarRol;
    }

    [HttpGet]
    [RequierePermiso(Modulo.Auditoria, Operacion.Lectura)] // se reutiliza permiso de Auditoria; alternativa: agregar Modulo.Roles si se necesita aislar
    public async Task<ActionResult<IReadOnlyList<RolDto>>> GetAll() =>
        Ok(await _getRoles.ExecuteAsync());

    [HttpGet("{id:guid}")]
    [RequierePermiso(Modulo.Auditoria, Operacion.Lectura)]
    public async Task<ActionResult<RolDto>> GetById(Guid id)
    {
        try { return Ok(await _getRolById.ExecuteAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost]
    [RequierePermiso(Modulo.Auditoria, Operacion.Escritura)]
    public async Task<ActionResult<RolDto>> Create([FromBody] CrearRolRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            var dto = await _crearRol.ExecuteAsync(request, uid, uname);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Modulo.Auditoria, Operacion.Modificacion)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActualizarRolRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _actualizarRol.ExecuteAsync(id, request, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Modulo.Auditoria, Operacion.Eliminacion)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _eliminarRol.ExecuteAsync(id, uid, uname);
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

**Nota sobre permisos del controller:** uso `Modulo.Auditoria` como proxy del permiso "gestionar roles" porque solo el admin lo tiene en el seed. Si más adelante se quiere aislar, se agrega `Modulo.Roles` al enum (siguiendo la convención de `agent_Context.md`).

- [ ] **Step 2: Compilar**

Run: `dotnet build backend/src/GymFlow.API`
Expected: Build succeeded.

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.API/Controllers/RolesController.cs
git commit -m "feat(api): RolesController CRUD"
```

---

### Task 26: PermisosController

**Files:**
- Create: `backend/src/GymFlow.API/Controllers/PermisosController.cs`

- [ ] **Step 1: Controller**

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Permisos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // cualquier usuario autenticado puede leer el catálogo
public class PermisosController : ControllerBase
{
    private readonly GetPermisosQuery _getPermisos;

    public PermisosController(GetPermisosQuery getPermisos) => _getPermisos = getPermisos;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PermisoDto>>> GetAll() =>
        Ok(await _getPermisos.ExecuteAsync());
}
```

- [ ] **Step 2: Compilar y commit (esperar OK del usuario)**

Run: `dotnet build backend/src/GymFlow.API`

```bash
git add backend/src/GymFlow.API/Controllers/PermisosController.cs
git commit -m "feat(api): PermisosController catalog endpoint"
```

---

### Task 27: AuthController actualizado (JWT con RolId, LoginResponse con permisos)

**Files:**
- Modify: `backend/src/GymFlow.API/Controllers/AuthController.cs`

- [ ] **Step 1: Reemplazar el archivo completo**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence; // para RolSeed
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

    // Hardcoded users (Iteration 1) — apuntando al RolId de seed
    private static readonly List<HardcodedUser> Users = new()
    {
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001"), "admin@gymflow.com", "admin123", "Maurice", "Admin", RolSeed.AdminRolId, "Administrador"),
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000003"), "socio@gymflow.com", "socio123", "María", "López", RolSeed.SocioRolId, "Socio")
    };

    public AuthController(IConfiguration configuration, IAuditLogger auditLogger, IPermisoCache permisoCache)
    {
        _configuration = configuration;
        _auditLogger = auditLogger;
        _permisoCache = permisoCache;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "El correo y la contraseña son obligatorios." });

        var user = Users.FirstOrDefault(u =>
            u.Correo.Equals(request.Correo, StringComparison.OrdinalIgnoreCase) &&
            u.Password == request.Password);

        if (user == null)
            return Unauthorized(new { error = "Correo o contraseña incorrectos." });

        var token = GenerateJwt(user);
        var permisos = await _permisoCache.ObtenerPermisosAsync(user.RolId);
        var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

        await _auditLogger.LogAsync(
            user.Id, $"{user.Nombre} {user.Apellido}",
            TipoAccionAuditoria.InicioSesion, "Sesion", null,
            $"Inicio de sesión de {user.Nombre} {user.Apellido} ({user.RolNombre})");

        return Ok(new LoginResponse(token, user.Nombre, user.Apellido, user.Correo, user.RolNombre, permisosDto));
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

    private string GenerateJwt(HardcodedUser user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "GymFlowDevSecretKey2026!SuperSecure");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Correo),
            new Claim("rolId", user.RolId.ToString()),
            new Claim("rolNombre", user.RolNombre),
            new Claim("nombre", user.Nombre),
            new Claim("apellido", user.Apellido)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private record HardcodedUser(Guid Id, string Correo, string Password, string Nombre, string Apellido, Guid RolId, string RolNombre);
}

public record LoginRequest(string Correo, string Password);
public record LoginResponse(string Token, string Nombre, string Apellido, string Correo, string RolNombre, IReadOnlyList<PermisoDto> Permisos);
```

- [ ] **Step 2: Compilar y probar manualmente**

Run: `dotnet run --project backend/src/GymFlow.API`

Probar (con `curl` o httpie):
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"correo":"admin@gymflow.com","password":"admin123"}'
```
Expected: respuesta con `token`, `rolNombre: "Administrador"`, `permisos: [...]` (16 permisos).

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add backend/src/GymFlow.API/Controllers/AuthController.cs
git commit -m "feat(api): JWT carries rolId; LoginResponse includes permisos"
```

---

## Phase 7 — Frontend

### Task 28: Tipos y servicios

**Files:**
- Create: `frontend/src/types/permisos.ts`
- Create: `frontend/src/services/roles.ts`
- Create: `frontend/src/services/permisos.ts`

- [ ] **Step 1: Tipos**

```typescript
export type Modulo = 'Socios' | 'Planes' | 'Unidades' | 'Auditoria'
export type Operacion = 'Lectura' | 'Escritura' | 'Modificacion' | 'Eliminacion'

export interface Permiso {
  id: string
  modulo: Modulo
  operacion: Operacion
}

export interface Rol {
  id: string
  nombre: string
  esSistema: boolean
  fechaCreacion: string
  permisos: Permiso[]
}

export interface CrearRolRequest {
  nombre: string
  permisoIds: string[]
}

export interface ActualizarRolRequest {
  nombre: string
  permisoIds: string[]
}
```

- [ ] **Step 2: Servicio roles.ts**

```typescript
import api from './api'
import type { Rol, CrearRolRequest, ActualizarRolRequest } from '@/types/permisos'

export async function listarRoles(): Promise<Rol[]> {
  const { data } = await api.get<Rol[]>('/roles')
  return data
}

export async function obtenerRol(id: string): Promise<Rol> {
  const { data } = await api.get<Rol>(`/roles/${id}`)
  return data
}

export async function crearRol(req: CrearRolRequest): Promise<Rol> {
  const { data } = await api.post<Rol>('/roles', req)
  return data
}

export async function actualizarRol(id: string, req: ActualizarRolRequest): Promise<void> {
  await api.put(`/roles/${id}`, req)
}

export async function eliminarRol(id: string): Promise<void> {
  await api.delete(`/roles/${id}`)
}
```

- [ ] **Step 3: Servicio permisos.ts**

```typescript
import api from './api'
import type { Permiso } from '@/types/permisos'

export async function listarPermisos(): Promise<Permiso[]> {
  const { data } = await api.get<Permiso[]>('/permisos')
  return data
}
```

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add frontend/src/types/permisos.ts frontend/src/services/roles.ts frontend/src/services/permisos.ts
git commit -m "feat(frontend): types and services for roles and permissions"
```

---

### Task 29: AuthContext con permisos + hook usePermisos

**Files:**
- Modify: `frontend/src/context/AuthContext.tsx`
- Create: `frontend/src/hooks/usePermisos.ts`

- [ ] **Step 1: Reemplazar `AuthContext.tsx` completo**

```typescript
import { createContext, useContext, useState, useEffect, useCallback } from 'react'
import type { ReactNode } from 'react'
import api from '@/services/api'
import type { Permiso, Modulo, Operacion } from '@/types/permisos'

interface User {
  nombre: string
  apellido: string
  correo: string
  rolNombre: string
  permisos: Permiso[]
}

interface AuthContextType {
  user: User | null
  token: string | null
  login: (correo: string, password: string) => Promise<void>
  logout: () => void
  isAuthenticated: boolean
  isLoading: boolean
  tienePermiso: (modulo: Modulo, operacion: Operacion) => boolean
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('gymflow_token'))

  const logout = useCallback(() => {
    localStorage.removeItem('gymflow_token')
    delete api.defaults.headers.common['Authorization']
    setToken(null)
    setUser(null)
  }, [])

  useEffect(() => {
    if (token) {
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`
      api.get('/auth/me')
        .then(({ data }) => setUser(data))
        .catch(() => logout())
        .finally(() => setIsLoading(false))
    } else {
      setIsLoading(false)
    }
  }, [token, logout])

  const login = async (correo: string, password: string) => {
    const { data } = await api.post('/auth/login', { correo, password })
    localStorage.setItem('gymflow_token', data.token)
    api.defaults.headers.common['Authorization'] = `Bearer ${data.token}`
    setToken(data.token)
    setUser({
      nombre: data.nombre,
      apellido: data.apellido,
      correo: data.correo,
      rolNombre: data.rolNombre,
      permisos: data.permisos ?? [],
    })
  }

  const tienePermiso = (modulo: Modulo, operacion: Operacion): boolean =>
    user?.permisos.some(p => p.modulo === modulo && p.operacion === operacion) ?? false

  return (
    <AuthContext.Provider value={{ user, token, login, logout, isAuthenticated: !!user, isLoading, tienePermiso }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
```

- [ ] **Step 2: hook `usePermisos.ts`**

```typescript
import { useAuth } from '@/context/AuthContext'
import type { Modulo, Operacion } from '@/types/permisos'

export function usePermisos() {
  const { tienePermiso } = useAuth()
  return {
    tienePermiso,
    puedeLeer: (m: Modulo) => tienePermiso(m, 'Lectura'),
    puedeEscribir: (m: Modulo) => tienePermiso(m, 'Escritura'),
    puedeModificar: (m: Modulo) => tienePermiso(m, 'Modificacion'),
    puedeEliminar: (m: Modulo) => tienePermiso(m, 'Eliminacion'),
  }
}
```

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add frontend/src/context/AuthContext.tsx frontend/src/hooks/usePermisos.ts
git commit -m "feat(frontend): AuthContext exposes permisos and tienePermiso"
```

---

### Task 30: Reemplazar checks de rol en AdminLayout y Topbar

**Files:**
- Modify: `frontend/src/components/layout/AdminLayout.tsx`
- Modify: `frontend/src/components/layout/Topbar.tsx`

- [ ] **Step 1: AdminLayout — reemplazar `user?.rol !== 'Admin'`**

Cambiar línea 24 (check de admin) por una verificación basada en si el usuario tiene **algún** permiso administrativo (ej. lectura sobre Auditoria, que solo el admin tiene en el seed):

```typescript
// Antes:
// if (user?.rol !== 'Admin') {

// Después:
const { tienePermiso } = useAuth()
const esAdmin = tienePermiso('Auditoria', 'Lectura')
if (!esAdmin) {
  // ...
}
```

Y eliminar/ajustar las otras líneas que mencionan `user?.rol === 'Profesor'` (línea 33) — el rol Profesor ya no existe; se puede mostrar `user?.rolNombre` en su lugar.

- [ ] **Step 2: Topbar — reemplazar `user?.rol`**

Cambiar línea 33 de `{user?.rol}` a `{user?.rolNombre}`.

- [ ] **Step 3: Verificar manualmente**

Run dev server (`npm run dev` en frontend). Login con admin → ver Topbar muestra "Administrador". Login con socio → ve mensaje de "no tenés permisos" o equivalente.

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add frontend/src/components/layout/AdminLayout.tsx frontend/src/components/layout/Topbar.tsx
git commit -m "refactor(frontend): replace role string checks with permission checks"
```

---

### Task 31: Esconder ítems del Sidebar según permisos

**Files:**
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Leer el archivo**

Run: `cat frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 2: Filtrar ítems del menú por permiso**

Para cada ítem del sidebar, agregar un `modulo` asociado (`'Socios'`, `'Planes'`, etc.) y filtrar la lista por `puedeLeer(modulo)`. Ejemplo de patrón:

```typescript
import { usePermisos } from '@/hooks/usePermisos'
import type { Modulo } from '@/types/permisos'

interface MenuItem {
  label: string
  to: string
  modulo: Modulo
}

const allItems: MenuItem[] = [
  { label: 'Socios', to: '/admin/socios', modulo: 'Socios' },
  { label: 'Planes', to: '/admin/planes', modulo: 'Planes' },
  { label: 'Unidades', to: '/admin/unidades', modulo: 'Unidades' },
  { label: 'Auditoría', to: '/admin/auditoria', modulo: 'Auditoria' },
]

export default function Sidebar() {
  const { puedeLeer } = usePermisos()
  const items = allItems.filter(i => puedeLeer(i.modulo))
  // ...render items
}
```

Ítem extra para gestión de roles (solo para quien tenga lectura de Auditoria, que es el proxy de admin):

```typescript
{ label: 'Roles', to: '/admin/roles', modulo: 'Auditoria' },
```

- [ ] **Step 3: Verificar en el navegador**

Login como admin → ve todos los ítems. Login como socio → ve ninguno.

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add frontend/src/components/layout/Sidebar.tsx
git commit -m "feat(frontend): hide sidebar items based on permissions"
```

---

### Task 32: Página RolesPage (lista)

**Files:**
- Create: `frontend/src/pages/admin/RolesPage.tsx`

- [ ] **Step 1: Lista de roles**

```typescript
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { listarRoles, eliminarRol } from '@/services/roles'
import type { Rol } from '@/types/permisos'
import { usePermisos } from '@/hooks/usePermisos'

export default function RolesPage() {
  const [roles, setRoles] = useState<Rol[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { puedeEscribir, puedeModificar, puedeEliminar } = usePermisos()
  // Reutilizamos Modulo.Auditoria como "permiso de gestión administrativa"
  const puedeCrear = puedeEscribir('Auditoria')
  const puedeEditar = puedeModificar('Auditoria')
  const puedeBorrar = puedeEliminar('Auditoria')

  const cargar = () => {
    setLoading(true)
    listarRoles()
      .then(setRoles)
      .catch(e => setError(e?.response?.data?.error ?? 'Error al cargar roles'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { cargar() }, [])

  const onEliminar = async (id: string, nombre: string) => {
    if (!confirm(`¿Eliminar el rol "${nombre}"?`)) return
    try {
      await eliminarRol(id)
      cargar()
    } catch (e: any) {
      alert(e?.response?.data?.error ?? 'Error al eliminar')
    }
  }

  if (loading) return <div className="p-6">Cargando…</div>
  if (error) return <div className="p-6 text-destructive">{error}</div>

  return (
    <div className="p-6 space-y-4">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Roles</h1>
        {puedeCrear && (
          <Button asChild><Link to="/admin/roles/nuevo">Nuevo rol</Link></Button>
        )}
      </div>
      <table className="w-full">
        <thead>
          <tr className="border-b">
            <th className="text-left py-2">Nombre</th>
            <th className="text-left py-2">Tipo</th>
            <th className="text-left py-2">Permisos</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {roles.map(r => (
            <tr key={r.id} className="border-b">
              <td className="py-2">{r.nombre}</td>
              <td className="py-2">{r.esSistema ? 'Sistema' : 'Personalizado'}</td>
              <td className="py-2">{r.permisos.length}</td>
              <td className="py-2 text-right space-x-2">
                {puedeEditar && !r.esSistema && (
                  <Button asChild size="sm" variant="outline">
                    <Link to={`/admin/roles/${r.id}/editar`}>Editar</Link>
                  </Button>
                )}
                {puedeBorrar && !r.esSistema && (
                  <Button size="sm" variant="destructive" onClick={() => onEliminar(r.id, r.nombre)}>
                    Eliminar
                  </Button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
```

- [ ] **Step 2: Registrar ruta en `App.tsx`**

Agregar `<Route path="/admin/roles" element={<RolesPage />} />` dentro de las rutas admin.

- [ ] **Step 3: Probar manualmente**

Login como admin → navegar a `/admin/roles` → ver lista con Administrador y Socio (ambos `Sistema`, sin botones de editar/eliminar).

- [ ] **Step 4: Commit (esperar OK del usuario)**

```bash
git add frontend/src/pages/admin/RolesPage.tsx frontend/src/App.tsx
git commit -m "feat(frontend): RolesPage list view"
```

---

### Task 33: Form de creación/edición de rol (matriz de checkboxes)

**Files:**
- Create: `frontend/src/pages/admin/NuevoRolPage.tsx`
- Create: `frontend/src/pages/admin/EditRolPage.tsx`
- Create (helper compartido inline o en `frontend/src/pages/admin/RolForm.tsx`): un componente `<RolForm>` reutilizable

- [ ] **Step 1: Componente `RolForm`** (`frontend/src/pages/admin/RolForm.tsx`)

```typescript
import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { listarPermisos } from '@/services/permisos'
import type { Permiso, Modulo, Operacion } from '@/types/permisos'

interface Props {
  initialNombre?: string
  initialPermisoIds?: string[]
  onSubmit: (data: { nombre: string; permisoIds: string[] }) => Promise<void>
  submitLabel: string
}

const MODULOS: Modulo[] = ['Socios', 'Planes', 'Unidades', 'Auditoria']
const OPERACIONES: Operacion[] = ['Lectura', 'Escritura', 'Modificacion', 'Eliminacion']

export default function RolForm({ initialNombre = '', initialPermisoIds = [], onSubmit, submitLabel }: Props) {
  const [nombre, setNombre] = useState(initialNombre)
  const [permisos, setPermisos] = useState<Permiso[]>([])
  const [seleccionados, setSeleccionados] = useState<Set<string>>(new Set(initialPermisoIds))
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => { listarPermisos().then(setPermisos) }, [])
  useEffect(() => { setSeleccionados(new Set(initialPermisoIds)) }, [initialPermisoIds.join(',')])

  const idDe = (m: Modulo, o: Operacion) => permisos.find(p => p.modulo === m && p.operacion === o)?.id

  const toggle = (id: string | undefined) => {
    if (!id) return
    setSeleccionados(prev => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await onSubmit({ nombre, permisoIds: Array.from(seleccionados) })
    } catch (err: any) {
      setError(err?.response?.data?.error ?? 'Error al guardar')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6 p-6">
      <div>
        <label className="block text-sm font-medium mb-1">Nombre del rol</label>
        <Input value={nombre} onChange={e => setNombre(e.target.value)} required />
      </div>
      <div>
        <h2 className="text-lg font-semibold mb-2">Permisos</h2>
        <table className="w-full">
          <thead>
            <tr>
              <th className="text-left py-2">Módulo</th>
              {OPERACIONES.map(o => <th key={o} className="text-center py-2">{o}</th>)}
            </tr>
          </thead>
          <tbody>
            {MODULOS.map(m => (
              <tr key={m} className="border-b">
                <td className="py-2">{m}</td>
                {OPERACIONES.map(o => {
                  const id = idDe(m, o)
                  return (
                    <td key={o} className="text-center py-2">
                      <input
                        type="checkbox"
                        checked={!!id && seleccionados.has(id)}
                        onChange={() => toggle(id)}
                        disabled={!id}
                      />
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {error && <p className="text-destructive text-sm">{error}</p>}
      <Button type="submit" disabled={submitting}>{submitLabel}</Button>
    </form>
  )
}
```

- [ ] **Step 2: NuevoRolPage**

```typescript
import { useNavigate } from 'react-router-dom'
import RolForm from './RolForm'
import { crearRol } from '@/services/roles'

export default function NuevoRolPage() {
  const navigate = useNavigate()
  return (
    <RolForm
      onSubmit={async data => {
        await crearRol(data)
        navigate('/admin/roles')
      }}
      submitLabel="Crear"
    />
  )
}
```

- [ ] **Step 3: EditRolPage**

```typescript
import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import RolForm from './RolForm'
import { obtenerRol, actualizarRol } from '@/services/roles'
import type { Rol } from '@/types/permisos'

export default function EditRolPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [rol, setRol] = useState<Rol | null>(null)

  useEffect(() => {
    if (id) obtenerRol(id).then(setRol)
  }, [id])

  if (!rol) return <div className="p-6">Cargando…</div>
  if (rol.esSistema) return <div className="p-6">Este rol es del sistema y no puede editarse.</div>

  return (
    <RolForm
      initialNombre={rol.nombre}
      initialPermisoIds={rol.permisos.map(p => p.id)}
      onSubmit={async data => {
        await actualizarRol(rol.id, data)
        navigate('/admin/roles')
      }}
      submitLabel="Guardar cambios"
    />
  )
}
```

- [ ] **Step 4: Rutas en `App.tsx`**

```typescript
<Route path="/admin/roles/nuevo" element={<NuevoRolPage />} />
<Route path="/admin/roles/:id/editar" element={<EditRolPage />} />
```

- [ ] **Step 5: Probar manualmente**

Login admin → `/admin/roles/nuevo` → crear rol "Recepcionista" con Lectura+Escritura en Socios → submit → redirige a la lista, aparece. Editar → quitar Escritura → guardar → verificar persistencia (re-cargar página).

- [ ] **Step 6: Commit (esperar OK del usuario)**

```bash
git add frontend/src/pages/admin/RolForm.tsx frontend/src/pages/admin/NuevoRolPage.tsx frontend/src/pages/admin/EditRolPage.tsx frontend/src/App.tsx
git commit -m "feat(frontend): create/edit role form with permissions matrix"
```

---

## Phase 8 — Documentación

### Task 34: Actualizar agent_Context.md con la convención

**Files:**
- Modify: `docs/agent_Context.md`

- [ ] **Step 1: Buscar la sección "Reglas para el Agente"**

Run: `grep -n "Reglas para el Agente" docs/agent_Context.md`

- [ ] **Step 2: Agregar una subsección al final de "Reglas para el Agente"**

Texto exacto a agregar:

```markdown
### Al agregar un módulo nuevo

Cuando se crea un módulo nuevo en el backend (ej. `Cuotas`, `Eventos`), **es obligatorio**:

1. Agregar el valor al enum `GymFlow.Domain.Enums.Modulo` (en `backend/src/GymFlow.Domain/Enums/Modulo.cs`).
2. Generar una migración EF Core que inserte las 4 filas correspondientes en la tabla `Permisos`:
   - `(NuevoModulo, Lectura)`
   - `(NuevoModulo, Escritura)`
   - `(NuevoModulo, Modificacion)`
   - `(NuevoModulo, Eliminacion)`
3. Decidir si el rol `Administrador` debe tener esos 4 permisos automáticamente (lo más común: sí). Si sí, la misma migración inserta las 4 filas en `RolPermisos` apuntando al `RolSeed.AdminRolId`.
4. Aplicar `[RequierePermiso(Modulo.NuevoModulo, Operacion.X)]` a los endpoints del controller.

El sistema de permisos es de catálogo cerrado en código: no se inventan módulos en runtime. La convención surge de RF-23 — ver `docs/superpowers/specs/2026-04-26-rf-23-roles-y-permisos.md`.
```

- [ ] **Step 3: Commit (esperar OK del usuario)**

```bash
git add docs/agent_Context.md
git commit -m "docs(agent_context): convention for adding new modules with permissions"
```

---

## Self-Review Checklist (post-plan)

- [ ] Spec coverage: cada criterio de aceptación del spec está cubierto por al menos una task.
  - Enum `Rol` no existe → Task 7
  - Tablas con seed → Tasks 8-13
  - 403 sin permiso → Task 18 (atributo) + Task 19 (aplicación)
  - Admin puede crear roles desde UI → Tasks 25, 32, 33
  - Roles `EsSistema` no se editan/eliminan → Tasks 23, 24 (validaciones), Task 33 (UI bloquea)
  - Frontend esconde menús → Task 31
  - Login devuelve permisos → Task 27
  - Tests pasan → Tasks 3, 18, 22, 23, 24
- [ ] No placeholders, todo el código está incluido.
- [ ] Type consistency: `RolDto`, `PermisoDto`, `Modulo`, `Operacion` usados consistentemente.
- [ ] Nombres de funciones consistentes: `tienePermiso`, `Renombrar`, `ReemplazarPermisos`, `Invalidar`.

## Riesgos conocidos del plan

- **Task 11 (migración):** si EF detecta que la columna `Rol` (string) tiene datos productivos al hacer DROP, falla. **Mitigación:** la DB se va a recrear desde cero (no hay datos productivos). Si ya tenés una DB de dev con usuarios, dropearla antes de migrar (`dotnet ef database drop`).
- **Task 27 (AuthController):** los tests existentes que apuntan a `LoginResponse.Rol` (string) van a romper. **Mitigación:** revisar `backend/tests` después del Task 27 y actualizar.
- **Task 6 (Socio):** dejé `Guid.Empty` como TODO temporal. Si el plan se ejecuta no-secuencialmente y se saltea Task 16, los UseCases que crean `Socio` quedan rotos. **Mitigación:** el grep `TODO RF-23` en Task 16 los encuentra todos.
