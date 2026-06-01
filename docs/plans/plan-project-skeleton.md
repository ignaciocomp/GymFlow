---
tags:
  - plan
requerimiento: proyecto
---

# GymFlow Project Skeleton — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Spec:** [[spec-gymflow-design]]
**Última actualización:** 2026-03-25
**Historial:**
- 2026-03-25 — Versión inicial

**Goal:** Create the full project skeleton — .NET Clean Architecture backend, React+Vite frontend, base domain entity (Unidad), end-to-end wiring, and CI/CD pipeline — so the team can start developing features immediately.

**Architecture:** Clean Architecture with 4 .NET projects (Domain, Application, Infrastructure, API) targeting .NET 8. React 19 + TypeScript + Vite frontend with Tailwind CSS and shadcn/ui. PostgreSQL 16 via Entity Framework Core Code-First. Monorepo structure. **Note:** We use a pragmatic variant of Clean Architecture where Infrastructure references both Domain and Application (to implement repository interfaces defined in Application). This is a common .NET convention and a conscious design decision.

**Tech Stack:** C# / .NET 8, ASP.NET Core Web API, EF Core, PostgreSQL 16, React 19, TypeScript, Vite, Tailwind CSS, shadcn/ui, React Query, React Router v7, xUnit, Vitest, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-03-25-gymflow-design.md`
**Agent context:** `agent_Context.md`

---

## File Map

### Backend — Will be created

```
backend/
├── GymFlow.sln
├── src/
│   ├── GymFlow.Domain/
│   │   ├── GymFlow.Domain.csproj
│   │   └── Entities/
│   │       └── Unidad.cs
│   ├── GymFlow.Application/
│   │   ├── GymFlow.Application.csproj
│   │   ├── Interfaces/
│   │   │   └── IUnidadRepository.cs
│   │   ├── DTOs/
│   │   │   └── UnidadDto.cs
│   │   └── UseCases/
│   │       └── Unidades/
│   │           └── GetUnidadesQuery.cs
│   ├── GymFlow.Infrastructure/
│   │   ├── GymFlow.Infrastructure.csproj
│   │   ├── Persistence/
│   │   │   ├── GymFlowDbContext.cs
│   │   │   └── Configurations/
│   │   │       └── UnidadConfiguration.cs
│   │   ├── Repositories/
│   │   │   └── UnidadRepository.cs
│   │   └── DependencyInjection.cs
│   └── GymFlow.API/
│       ├── GymFlow.API.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Controllers/
│       │   └── UnidadesController.cs
│       └── DependencyInjection.cs
└── tests/
    ├── GymFlow.Domain.Tests/
    │   ├── GymFlow.Domain.Tests.csproj
    │   └── Entities/
    │       └── UnidadTests.cs
    └── GymFlow.Application.Tests/
        ├── GymFlow.Application.Tests.csproj
        └── UseCases/
            └── GetUnidadesQueryTests.cs
```

### Frontend — Will be created

```
frontend/
├── package.json
├── vite.config.ts
├── tsconfig.json
├── components.json          (shadcn/ui config)
├── index.html
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   ├── index.css
│   ├── lib/
│   │   └── utils.ts
│   ├── components/
│   │   └── ui/           (shadcn/ui components — added via CLI)
│   ├── services/
│   │   └── api.ts
│   ├── types/
│   │   └── index.ts
│   ├── context/
│   │   └── .gitkeep
│   ├── hooks/
│   │   └── .gitkeep
│   ├── pages/
│   │   └── Home.tsx
│   └── assets/
│       └── .gitkeep
└── public/
    └── .gitkeep
```

### CI/CD — Will be created

```
.github/
└── workflows/
    └── ci.yml
```

---

## Task 1: Create .NET Solution and Domain Project

**Files:**
- Create: `backend/GymFlow.sln`
- Create: `backend/src/GymFlow.Domain/GymFlow.Domain.csproj`
- Create: `backend/src/GymFlow.Domain/Entities/Unidad.cs`

- [ ] **Step 1: Create solution and Domain project**

```bash
cd backend
dotnet new sln -n GymFlow
mkdir -p src/GymFlow.Domain
dotnet new classlib -n GymFlow.Domain -o src/GymFlow.Domain -f net8.0
dotnet sln add src/GymFlow.Domain/GymFlow.Domain.csproj
```

Delete the auto-generated `Class1.cs`:
```bash
rm src/GymFlow.Domain/Class1.cs
```

- [ ] **Step 2: Create base entity Unidad**

Create `backend/src/GymFlow.Domain/Entities/Unidad.cs`:

```csharp
namespace GymFlow.Domain.Entities;

public class Unidad
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Direccion { get; private set; } = string.Empty;

    private Unidad() { } // EF Core

    public Unidad(string nombre, string direccion)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Direccion = !string.IsNullOrWhiteSpace(direccion) ? direccion : throw new ArgumentException("Direccion is required.", nameof(direccion));
    }

    public void Actualizar(string nombre, string direccion)
    {
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Direccion = !string.IsNullOrWhiteSpace(direccion) ? direccion : throw new ArgumentException("Direccion is required.", nameof(direccion));
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/GymFlow.Domain/GymFlow.Domain.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add backend/GymFlow.sln backend/src/GymFlow.Domain/
git commit -m "feat: create .NET solution and Domain project with Unidad entity"
```

---

## Task 2: Create Application Project

**Files:**
- Create: `backend/src/GymFlow.Application/GymFlow.Application.csproj`
- Create: `backend/src/GymFlow.Application/Interfaces/IUnidadRepository.cs`
- Create: `backend/src/GymFlow.Application/DTOs/UnidadDto.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Unidades/GetUnidadesQuery.cs`
- Create: `backend/src/GymFlow.Application/UseCases/Unidades/GetUnidadesQuery.cs`

- [ ] **Step 1: Create Application project with reference to Domain**

```bash
cd backend
dotnet new classlib -n GymFlow.Application -o src/GymFlow.Application -f net8.0
dotnet sln add src/GymFlow.Application/GymFlow.Application.csproj
dotnet add src/GymFlow.Application/GymFlow.Application.csproj reference src/GymFlow.Domain/GymFlow.Domain.csproj
rm src/GymFlow.Application/Class1.cs
```

- [ ] **Step 2: Create IUnidadRepository interface**

Create `backend/src/GymFlow.Application/Interfaces/IUnidadRepository.cs`:

```csharp
using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IUnidadRepository
{
    Task<IEnumerable<Unidad>> GetAllAsync();
    Task<Unidad?> GetByIdAsync(Guid id);
}
```

- [ ] **Step 3: Create UnidadDto**

Create `backend/src/GymFlow.Application/DTOs/UnidadDto.cs`:

```csharp
namespace GymFlow.Application.DTOs;

public record UnidadDto(Guid Id, string Nombre, string Direccion);
```

- [ ] **Step 4: Create GetUnidades use case**

Create `backend/src/GymFlow.Application/UseCases/Unidades/GetUnidadesQuery.cs`:

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Unidades;

public class GetUnidadesQuery
{
    private readonly IUnidadRepository _repository;

    public GetUnidadesQuery(IUnidadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<UnidadDto>> ExecuteAsync()
    {
        var unidades = await _repository.GetAllAsync();
        return unidades.Select(u => new UnidadDto(u.Id, u.Nombre, u.Direccion));
    }
}
```

- [ ] **Step 5: Verify build**

```bash
dotnet build src/GymFlow.Application/GymFlow.Application.csproj
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GymFlow.Application/
git commit -m "feat: add Application layer with Unidad use case, DTO, and repository interface"
```

---

## Task 3: Create Infrastructure Project

**Files:**
- Create: `backend/src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj`
- Create: `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`
- Create: `backend/src/GymFlow.Infrastructure/Persistence/Configurations/UnidadConfiguration.cs`
- Create: `backend/src/GymFlow.Infrastructure/Repositories/UnidadRepository.cs`
- Create: `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Create Infrastructure project with references and NuGet packages**

```bash
cd backend
dotnet new classlib -n GymFlow.Infrastructure -o src/GymFlow.Infrastructure -f net8.0
dotnet sln add src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj
dotnet add src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj reference src/GymFlow.Domain/GymFlow.Domain.csproj
dotnet add src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj reference src/GymFlow.Application/GymFlow.Application.csproj
rm src/GymFlow.Infrastructure/Class1.cs
```

Add EF Core packages:
```bash
dotnet add src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Relational --version 8.0.11
```

- [ ] **Step 2: Create DbContext**

Create `backend/src/GymFlow.Infrastructure/Persistence/GymFlowDbContext.cs`:

```csharp
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence;

public class GymFlowDbContext : DbContext
{
    public GymFlowDbContext(DbContextOptions<GymFlowDbContext> options) : base(options) { }

    public DbSet<Unidad> Unidades => Set<Unidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GymFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 3: Create Unidad EF configuration**

Create `backend/src/GymFlow.Infrastructure/Persistence/Configurations/UnidadConfiguration.cs`:

```csharp
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class UnidadConfiguration : IEntityTypeConfiguration<Unidad>
{
    public void Configure(EntityTypeBuilder<Unidad> builder)
    {
        builder.ToTable("Unidades");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Direccion).IsRequired().HasMaxLength(200);
    }
}
```

- [ ] **Step 4: Create UnidadRepository**

Create `backend/src/GymFlow.Infrastructure/Repositories/UnidadRepository.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class UnidadRepository : IUnidadRepository
{
    private readonly GymFlowDbContext _context;

    public UnidadRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Unidad>> GetAllAsync()
    {
        return await _context.Unidades.ToListAsync();
    }

    public async Task<Unidad?> GetByIdAsync(Guid id)
    {
        return await _context.Unidades.FindAsync(id);
    }
}
```

- [ ] **Step 5: Create DI extension for Infrastructure**

Create `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GymFlowDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnidadRepository, UnidadRepository>();

        return services;
    }
}
```

- [ ] **Step 6: Verify build**

```bash
dotnet build src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj
```

Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GymFlow.Infrastructure/
git commit -m "feat: add Infrastructure layer with EF Core, DbContext, Unidad repo and config"
```

---

## Task 4: Create API Project (Presentation Layer)

**Files:**
- Create: `backend/src/GymFlow.API/GymFlow.API.csproj`
- Create: `backend/src/GymFlow.API/Program.cs`
- Create: `backend/src/GymFlow.API/appsettings.json`
- Create: `backend/src/GymFlow.API/Controllers/UnidadesController.cs`
- Create: `backend/src/GymFlow.API/DependencyInjection.cs`

- [ ] **Step 1: Create API project with references**

```bash
cd backend
dotnet new webapi -n GymFlow.API -o src/GymFlow.API -f net8.0 --no-openapi --use-controllers
dotnet sln add src/GymFlow.API/GymFlow.API.csproj
dotnet add src/GymFlow.API/GymFlow.API.csproj reference src/GymFlow.Application/GymFlow.Application.csproj
dotnet add src/GymFlow.API/GymFlow.API.csproj reference src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj
```

Remove auto-generated files that we'll replace:
```bash
rm -f src/GymFlow.API/Controllers/*.cs src/GymFlow.API/WeatherForecast.cs
```

Add EF Core Design package (for migrations):
```bash
dotnet add src/GymFlow.API/GymFlow.API.csproj package Microsoft.EntityFrameworkCore.Design --version 8.0.11
```

- [ ] **Step 2: Create appsettings.json**

Replace `backend/src/GymFlow.API/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=gymflow;Username=postgres;Password=postgres"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

- [ ] **Step 3: Create DI extension for API layer**

Create `backend/src/GymFlow.API/DependencyInjection.cs`:

```csharp
using GymFlow.Application.UseCases.Unidades;

namespace GymFlow.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetUnidadesQuery>();

        return services;
    }
}
```

- [ ] **Step 4: Create Program.cs**

Replace `backend/src/GymFlow.API/Program.cs`:

```csharp
using GymFlow.API;
using GymFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
```

- [ ] **Step 5: Create UnidadesController**

Create `backend/src/GymFlow.API/Controllers/UnidadesController.cs`:

```csharp
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Unidades;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnidadesController : ControllerBase
{
    private readonly GetUnidadesQuery _getUnidadesQuery;

    public UnidadesController(GetUnidadesQuery getUnidadesQuery)
    {
        _getUnidadesQuery = getUnidadesQuery;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UnidadDto>>> GetAll()
    {
        var unidades = await _getUnidadesQuery.ExecuteAsync();
        return Ok(unidades);
    }
}
```

- [ ] **Step 6: Verify full solution build**

```bash
cd backend
dotnet build GymFlow.sln
```

Expected: Build succeeded (0 errors).

- [ ] **Step 7: Commit**

```bash
git add backend/src/GymFlow.API/
git commit -m "feat: add API layer with UnidadesController, CORS, and DI wiring"
```

---

## Task 5: Create Backend Test Projects

**Files:**
- Create: `backend/tests/GymFlow.Domain.Tests/GymFlow.Domain.Tests.csproj`
- Create: `backend/tests/GymFlow.Domain.Tests/Entities/UnidadTests.cs`
- Create: `backend/tests/GymFlow.Application.Tests/GymFlow.Application.Tests.csproj`
- Create: `backend/tests/GymFlow.Application.Tests/UseCases/GetUnidadesQueryTests.cs`

- [ ] **Step 1: Create Domain.Tests project**

```bash
cd backend
dotnet new xunit -n GymFlow.Domain.Tests -o tests/GymFlow.Domain.Tests -f net8.0
dotnet sln add tests/GymFlow.Domain.Tests/GymFlow.Domain.Tests.csproj
dotnet add tests/GymFlow.Domain.Tests/GymFlow.Domain.Tests.csproj reference src/GymFlow.Domain/GymFlow.Domain.csproj
rm tests/GymFlow.Domain.Tests/UnitTest1.cs
```

- [ ] **Step 2: Write Unidad domain tests**

Create `backend/tests/GymFlow.Domain.Tests/Entities/UnidadTests.cs`:

```csharp
using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests.Entities;

public class UnidadTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesUnidad()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvín", "Malvín, Montevideo");

        Assert.NotEqual(Guid.Empty, unidad.Id);
        Assert.Equal("Gimnasio Nuevo Malvín", unidad.Nombre);
        Assert.Equal("Malvín, Montevideo", unidad.Direccion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidNombre_ThrowsArgumentException(string? nombre)
    {
        Assert.Throws<ArgumentException>(() => new Unidad(nombre!, "Dirección"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDireccion_ThrowsArgumentException(string? direccion)
    {
        Assert.Throws<ArgumentException>(() => new Unidad("Nombre", direccion!));
    }

    [Fact]
    public void Actualizar_WithValidData_UpdatesProperties()
    {
        var unidad = new Unidad("Nombre Original", "Dirección Original");

        unidad.Actualizar("Nombre Nuevo", "Dirección Nueva");

        Assert.Equal("Nombre Nuevo", unidad.Nombre);
        Assert.Equal("Dirección Nueva", unidad.Direccion);
    }
}
```

- [ ] **Step 3: Run domain tests**

```bash
dotnet test tests/GymFlow.Domain.Tests/ -v minimal
```

Expected: 10 tests passed (1 valid + 3 invalid nombre + 3 invalid direccion + 1 actualizar = 8... but Theory generates per InlineData, so: 1 + 3 + 3 + 1 = 8 tests).

- [ ] **Step 4: Create Application.Tests project**

```bash
cd backend
dotnet new xunit -n GymFlow.Application.Tests -o tests/GymFlow.Application.Tests -f net8.0
dotnet sln add tests/GymFlow.Application.Tests/GymFlow.Application.Tests.csproj
dotnet add tests/GymFlow.Application.Tests/GymFlow.Application.Tests.csproj reference src/GymFlow.Application/GymFlow.Application.csproj
dotnet add tests/GymFlow.Application.Tests/GymFlow.Application.Tests.csproj package Moq --version 4.20.72
rm tests/GymFlow.Application.Tests/UnitTest1.cs
```

- [ ] **Step 5: Write GetUnidadesQuery tests**

Create `backend/tests/GymFlow.Application.Tests/UseCases/GetUnidadesQueryTests.cs`:

```csharp
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Unidades;
using GymFlow.Domain.Entities;
using Moq;

namespace GymFlow.Application.Tests.UseCases;

public class GetUnidadesQueryTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsAllUnidades()
    {
        var mockRepo = new Mock<IUnidadRepository>();
        var unidades = new List<Unidad>
        {
            new("Gimnasio Nuevo Malvín", "Malvín, Montevideo"),
            new("Espacio Mora", "Malvín, Montevideo")
        };
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(unidades);

        var query = new GetUnidadesQuery(mockRepo.Object);
        var result = (await query.ExecuteAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Gimnasio Nuevo Malvín", result[0].Nombre);
        Assert.Equal("Espacio Mora", result[1].Nombre);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmpty_ReturnsEmptyList()
    {
        var mockRepo = new Mock<IUnidadRepository>();
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Unidad>());

        var query = new GetUnidadesQuery(mockRepo.Object);
        var result = (await query.ExecuteAsync()).ToList();

        Assert.Empty(result);
    }
}
```

- [ ] **Step 6: Run all backend tests**

```bash
cd backend
dotnet test GymFlow.sln -v minimal
```

Expected: 10 tests passed (8 domain + 2 application).

- [ ] **Step 7: Commit**

```bash
git add backend/tests/
git commit -m "test: add Domain and Application unit tests for Unidad"
```

---

## Task 6: Initialize Frontend with Vite + React + TypeScript

**Files:**
- Create: `frontend/` (entire Vite scaffold)
- Modify: `frontend/vite.config.ts` (add proxy)
- Modify: `frontend/tsconfig.json` (add paths)

- [ ] **Step 1: Scaffold Vite + React + TypeScript**

```bash
cd "C:/Users/Usuario/Desktop/github/GymFlow"
npm create vite@latest frontend -- --template react-ts
```

- [ ] **Step 2: Install dependencies**

```bash
cd frontend
npm install
npm install axios @tanstack/react-query react-router-dom
npm install -D vitest @testing-library/react @testing-library/jest-dom jsdom
```

- [ ] **Step 3: Configure Vite proxy for API**

Replace `frontend/vite.config.ts`:

```typescript
/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5146',
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: [],
  },
})
```

- [ ] **Step 4: Update tsconfig to support @ alias**

Add to `frontend/tsconfig.json` (inside `compilerOptions`):

```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

Note: Merge this into the existing tsconfig, don't replace the whole file. Keep existing compiler options.

- [ ] **Step 5: Verify frontend builds**

```bash
cd frontend
npm run build
```

Expected: Build succeeds, output in `dist/`.

- [ ] **Step 6: Commit**

```bash
git add frontend/
git commit -m "feat: initialize frontend with Vite, React 19, TypeScript"
```

---

## Task 7: Add Tailwind CSS and shadcn/ui to Frontend

**Files:**
- Modify: `frontend/package.json` (new deps)
- Create/Modify: `frontend/tailwind.config.ts`
- Create/Modify: `frontend/postcss.config.js`
- Modify: `frontend/src/index.css`
- Create: `frontend/src/lib/utils.ts`
- Create: `frontend/components.json` (shadcn config)

- [ ] **Step 1: Install Tailwind CSS v4**

```bash
cd frontend
npm install tailwindcss @tailwindcss/vite
```

- [ ] **Step 2: Configure Tailwind in Vite**

Update `frontend/vite.config.ts` to add the Tailwind plugin:

```typescript
/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5146',
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: [],
  },
})
```

- [ ] **Step 3: Update CSS entry point**

Replace `frontend/src/index.css`:

```css
@import "tailwindcss";
```

- [ ] **Step 4: Initialize shadcn/ui**

```bash
cd frontend
npx shadcn@latest init -d
```

This will create `components.json` and set up the project for shadcn/ui components.

- [ ] **Step 5: Create utils file if not created by shadcn**

Create `frontend/src/lib/utils.ts` (if shadcn didn't create it):

```typescript
import { type ClassValue, clsx } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

- [ ] **Step 6: Add a shadcn Button component to verify setup**

```bash
cd frontend
npx shadcn@latest add button
```

- [ ] **Step 7: Verify build**

```bash
cd frontend
npm run build
```

Expected: Build succeeds.

- [ ] **Step 8: Commit**

```bash
git add frontend/
git commit -m "feat: add Tailwind CSS v4 and shadcn/ui to frontend"
```

---

## Task 8: Wire Frontend to Backend — End-to-End

**Files:**
- Create: `frontend/src/services/api.ts`
- Create: `frontend/src/types/index.ts`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/main.tsx`
- Create: `frontend/src/pages/Home.tsx`

- [ ] **Step 1: Create TypeScript types**

Create `frontend/src/types/index.ts`:

```typescript
export interface Unidad {
  id: string
  nombre: string
  direccion: string
}
```

- [ ] **Step 2: Create API service**

Create `frontend/src/services/api.ts`:

```typescript
import axios from 'axios'
import type { Unidad } from '@/types'

const api = axios.create({
  baseURL: '/api',
})

export const unidadesApi = {
  getAll: async (): Promise<Unidad[]> => {
    const { data } = await api.get<Unidad[]>('/unidades')
    return data
  },
}

export default api
```

- [ ] **Step 3: Create Home page**

Create `frontend/src/pages/Home.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query'
import { unidadesApi } from '@/services/api'
import { Button } from '@/components/ui/button'

export default function Home() {
  const { data: unidades, isLoading, error } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  return (
    <div className="min-h-screen bg-background p-8">
      <div className="mx-auto max-w-4xl">
        <h1 className="text-4xl font-bold tracking-tight">GymFlow</h1>
        <p className="mt-2 text-muted-foreground">
          Sistema Integrado de Gestión para Gimnasios
        </p>

        <div className="mt-8">
          <h2 className="text-2xl font-semibold">Unidades</h2>
          {isLoading && <p className="mt-4 text-muted-foreground">Cargando...</p>}
          {error && <p className="mt-4 text-destructive">Error al cargar unidades</p>}
          {unidades && unidades.length === 0 && (
            <p className="mt-4 text-muted-foreground">No hay unidades registradas.</p>
          )}
          {unidades && unidades.length > 0 && (
            <ul className="mt-4 space-y-2">
              {unidades.map((u) => (
                <li key={u.id} className="rounded-lg border p-4">
                  <p className="font-medium">{u.nombre}</p>
                  <p className="text-sm text-muted-foreground">{u.direccion}</p>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="mt-8">
          <Button>Empezar</Button>
        </div>
      </div>
    </div>
  )
}
```

- [ ] **Step 4: Update main.tsx with QueryClientProvider and Router**

Replace `frontend/src/main.tsx`:

```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import App from './App'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      retry: 1,
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
```

- [ ] **Step 5: Update App.tsx with routes**

Replace `frontend/src/App.tsx`:

```tsx
import { Routes, Route } from 'react-router-dom'
import Home from '@/pages/Home'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
    </Routes>
  )
}
```

- [ ] **Step 6: Verify build**

```bash
cd frontend
npm run build
```

Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/
git commit -m "feat: wire frontend to backend API with React Query and Home page"
```

---

## Task 9: Add GitHub Actions CI Pipeline

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Create CI workflow**

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [develop]

jobs:
  backend:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: gymflow_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore
        working-directory: backend
      - name: Build
        run: dotnet build --no-restore
        working-directory: backend
      - name: Test
        run: dotnet test --no-build -v minimal
        working-directory: backend

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - name: Install
        run: npm ci
        working-directory: frontend
      - name: Build
        run: npm run build
        working-directory: frontend
      - name: Test
        run: npx vitest run
        working-directory: frontend
```

- [ ] **Step 2: Commit**

```bash
git add .github/
git commit -m "ci: add GitHub Actions pipeline for backend and frontend"
```

---

## Task 10: Create Placeholder Directories and .gitkeep Files

**Files:**
- Create: multiple `.gitkeep` files for empty directories

- [ ] **Step 1: Create placeholder directories for future work**

```bash
cd "C:/Users/Usuario/Desktop/github/GymFlow"

# Frontend placeholder dirs
mkdir -p frontend/src/context
mkdir -p frontend/src/hooks
mkdir -p frontend/src/assets
mkdir -p frontend/public

touch frontend/src/context/.gitkeep
touch frontend/src/hooks/.gitkeep
touch frontend/src/assets/.gitkeep
touch frontend/public/.gitkeep

# Backend Infrastructure Tests placeholder
mkdir -p backend/tests/GymFlow.Infrastructure.Tests
```

- [ ] **Step 2: Create Infrastructure.Tests project (placeholder)**

```bash
cd backend
dotnet new xunit -n GymFlow.Infrastructure.Tests -o tests/GymFlow.Infrastructure.Tests -f net8.0
dotnet sln add tests/GymFlow.Infrastructure.Tests/GymFlow.Infrastructure.Tests.csproj
dotnet add tests/GymFlow.Infrastructure.Tests/GymFlow.Infrastructure.Tests.csproj reference src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj
dotnet add tests/GymFlow.Infrastructure.Tests/GymFlow.Infrastructure.Tests.csproj reference src/GymFlow.Application/GymFlow.Application.csproj
rm tests/GymFlow.Infrastructure.Tests/UnitTest1.cs
```

Create a placeholder test so the project compiles. Create `backend/tests/GymFlow.Infrastructure.Tests/PlaceholderTests.cs`:

```csharp
namespace GymFlow.Infrastructure.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Infrastructure_ProjectCompiles()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 3: Verify full solution**

```bash
cd backend
dotnet build GymFlow.sln
dotnet test GymFlow.sln -v minimal
```

Expected: Build succeeded, 11 tests passed (8 domain + 2 application + 1 infra placeholder).

- [ ] **Step 4: Final commit**

```bash
cd "C:/Users/Usuario/Desktop/github/GymFlow"
git add .
git commit -m "chore: add placeholder directories and Infrastructure.Tests project"
```

---

## Summary

After completing all 10 tasks, the project has:

- **Backend:** Full Clean Architecture skeleton with Domain, Application, Infrastructure, API layers. EF Core wired to PostgreSQL. Unidad entity end-to-end (entity → repo → use case → controller → API endpoint).
- **Frontend:** Vite + React 19 + TypeScript + Tailwind CSS v4 + shadcn/ui. React Query + React Router v7. Home page calling `/api/unidades`.
- **Tests:** xUnit for Domain and Application, with Moq for mocking. 11 passing backend tests. Vitest configured for frontend.
- **CI/CD:** GitHub Actions running backend build+test and frontend build on PRs to develop.
- **Git:** Clean commit history following conventional commits.

The team can now start feature branches and implement the remaining entities and modules.
