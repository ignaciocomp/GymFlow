# GymFlow

Sistema Integrado de Gestión para Gimnasios — Plataforma web para **Espacio Mora** y **Gimnasio Nuevo Malvín**.

## Descripción

GymFlow es una plataforma web que unifica la gestión de dos unidades de negocio (gimnasio y actividades complementarias) en un solo sistema. Gestiona socios, membresías, cuotas, clases, horarios, asistencias y más.

**Proyecto final** — Universidad ORT Uruguay, Analista en Tecnologías de la Información, 2026.

## Tech Stack

### Backend
- C# / .NET 8 — ASP.NET Core Web API
- Entity Framework Core (Code-First)
- PostgreSQL 16
- JWT Authentication
- xUnit + Moq

### Frontend
- React 19 + TypeScript
- Vite
- Tailwind CSS + shadcn/ui
- TanStack Query (React Query)
- React Router v7
- Vitest

## Arquitectura

Clean Architecture con 4 capas:

```
API (Presentation) → Application → Domain ← Infrastructure
```

## Estructura del Proyecto

```
GymFlow/
├── backend/
│   ├── GymFlow.sln
│   ├── src/
│   │   ├── GymFlow.Domain/
│   │   ├── GymFlow.Application/
│   │   ├── GymFlow.Infrastructure/
│   │   └── GymFlow.API/
│   └── tests/
│       ├── GymFlow.Domain.Tests/
│       ├── GymFlow.Application.Tests/
│       └── GymFlow.Infrastructure.Tests/
├── frontend/
│   ├── src/
│   └── public/
├── .github/workflows/
└── docs/
```

## Requisitos

- [Docker Desktop](https://www.docker.com/) (requerido)
- [Node.js 20+](https://nodejs.org/) (para el frontend)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (opcional, solo si querés correr el backend sin Docker)

## Quick Start (recomendado)

```bash
# 1. Clonar el repo
git clone https://github.com/ignaciocomp/GymFlow.git
cd GymFlow

# 2. Levantar backend + base de datos
docker compose up --build -d

# 3. Levantar frontend (en otra terminal)
cd frontend
npm install
npm run dev
```

La API estará en `http://localhost:5146` y el frontend en `http://localhost:5173`.
Las migraciones de la base de datos se aplican automáticamente al iniciar el backend.

### Comandos Docker útiles

```bash
docker compose up -d          # Levantar en background
docker compose down           # Parar todo
docker compose logs -f api    # Ver logs del backend
docker compose down -v        # Parar y borrar volumen de DB (reset completo)
```

## Getting Started (sin Docker)

Necesitás PostgreSQL 16 instalado localmente.

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/GymFlow.API
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

### Base de Datos

```bash
cd backend
dotnet ef database update --project src/GymFlow.Infrastructure --startup-project src/GymFlow.API
```

## Scripts

| Comando | Descripción |
|---------|-------------|
| `dotnet test` | Ejecutar tests del backend |
| `npm run dev` | Iniciar frontend en modo desarrollo |
| `npm run build` | Build de producción del frontend |
| `npm run test` | Ejecutar tests del frontend |

## Branching Strategy

- `main` — Código estable (merge al cierre de iteración)
- `develop` — Integración
- `feature/[nombre]` — Desarrollo de funcionalidades
- `bugfix/[nombre]` — Correcciones

## Convención de Commits

```
feat: nueva funcionalidad
fix: corrección de bug
docs: documentación
test: pruebas
refactor: reestructuración
chore: tareas técnicas
```

## Equipo

| Integrante | ID |
|------------|-----|
| Ignacio Compan | 268502 |
| Franco Notte | 243233 |
| Sebastián Acuña | 309167 |

**Tutor:** Luis Dentone Michelena

## Licencia

Proyecto académico — Universidad ORT Uruguay, 2026.
