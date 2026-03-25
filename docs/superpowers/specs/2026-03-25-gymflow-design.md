# GymFlow - Spec de Diseño

**Fecha:** 2026-03-25
**Proyecto:** Sistema Integrado de Gestión para Gimnasios
**Cliente:** Espacio Mora (Maurice) - Montevideo, Uruguay
**Equipo:** Ignacio Compan, Franco Notte, Sebastián Acuña
**Universidad:** ORT Uruguay - ATI 2026

---

## 1. Visión General

GymFlow es una plataforma web que unifica la gestión de **Gimnasio Nuevo Malvín** y **Espacio Mora** (telas aéreas, artes marciales, actividades infantiles) en un solo sistema. Reemplaza las dos suscripciones independientes a SmartGym con una solución a medida.

### Problema que resuelve
- Costos duplicados (2 suscripciones SmartGym)
- Gestión fragmentada entre dos sistemas
- Sin visión consolidada del negocio
- Recordatorios de pago manuales
- Plataforma genérica que no se adapta al negocio

### 4 vistas de usuario
1. **Página web pública** — Landing SEO-optimizada para captación
2. **Panel de administración** — Dashboard, gestión completa, filtrado multi-espacio
3. **Portal de socios** — Horarios, inscripciones, perfil, rutinas
4. **Vista de profesores** — Clases asignadas, registro de asistencia

---

## 2. Arquitectura

Clean Architecture, monorepo, cliente-servidor.

```
┌─────────────────────────────────────────────────┐
│                   Frontend                       │
│         React + TypeScript + Vite                │
│         Tailwind CSS + shadcn/ui                 │
│     React Query + Context API + React Router     │
├─────────────────────────────────────────────────┤
│                  REST/JSON (axios)                │
├─────────────────────────────────────────────────┤
│              Backend (.NET 8)                     │
│  ┌───────────────────────────────────────────┐   │
│  │  GymFlow.API (Presentation)               │   │
│  │  Controllers, Middlewares, JWT Config      │   │
│  ├───────────────────────────────────────────┤   │
│  │  GymFlow.Application                      │   │
│  │  Use Cases, DTOs, Interfaces, Validators  │   │
│  ├───────────────────────────────────────────┤   │
│  │  GymFlow.Domain                           │   │
│  │  Entities, Value Objects, Business Rules  │   │
│  ├───────────────────────────────────────────┤   │
│  │  GymFlow.Infrastructure                   │   │
│  │  EF Core, Repos, Email/WhatsApp Services  │   │
│  └───────────────────────────────────────────┘   │
├─────────────────────────────────────────────────┤
│            PostgreSQL 16 (Code-First)            │
└─────────────────────────────────────────────────┘
```

**Dependencias unidireccionales:** API → Application → Domain ← Infrastructure

---

## 3. Estructura del Monorepo

```
GymFlow/
├── backend/
│   ├── GymFlow.sln
│   ├── src/
│   │   ├── GymFlow.Domain/           # Entidades, enums, interfaces de dominio
│   │   ├── GymFlow.Application/      # Casos de uso, DTOs, validaciones, interfaces
│   │   ├── GymFlow.Infrastructure/   # EF Core, repos, servicios externos, migraciones
│   │   └── GymFlow.API/              # Controllers, middleware, auth JWT, Program.cs
│   └── tests/
│       ├── GymFlow.Domain.Tests/
│       ├── GymFlow.Application.Tests/
│       └── GymFlow.Infrastructure.Tests/
├── frontend/
│   ├── package.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   ├── tailwind.config.ts
│   ├── src/
│   │   ├── components/       # shadcn/ui + componentes custom
│   │   ├── pages/            # Vistas por ruta
│   │   ├── hooks/            # Custom hooks
│   │   ├── services/         # API calls (axios)
│   │   ├── context/          # Auth context, theme
│   │   ├── types/            # TypeScript interfaces
│   │   ├── lib/              # Utilidades
│   │   └── assets/           # Imágenes, fuentes
│   └── public/
├── .github/
│   └── workflows/            # CI/CD pipelines
├── docs/                     # Documentación del proyecto
├── .gitignore
└── README.md
```

---

## 4. Modelo de Dominio

### Entidades

```
Usuario (base, abstract)
├── Profesor → tiene muchas Clases
└── Socio → tiene muchas Cuotas, Inscripciones, Rutinas, Asistencias

Unidad (Gimnasio Nuevo Malvín | Espacio Mora)
├── agrupa Clases
├── agrupa Planes
└── filtra toda la vista admin

Usuario N:M Unidad (un socio/profesor puede pertenecer a ambas unidades)
```

**Nota sobre Administrador:** El rol Admin se maneja como valor del enum `Rol` en `Usuario`, no como subclase. No agrega atributos propios, así que una subclase vacía sería innecesaria.

### Entidades detalladas

| Entidad | Atributos principales |
|---------|----------------------|
| **Usuario** (base) | Id, Nombre, Apellido, Email, PasswordHash, Rol (Admin/Socio/Profesor), Estado, FechaCreacion, Unidades[] (N:M) |
| **Profesor** | Hereda de Usuario, ClasesAsignadas[] |
| **Socio** | Hereda de Usuario, PlanActivoId (FK a Plan), FechaAlta, ConsentimientoInformado, Cuotas[], Inscripciones[], Rutinas[], Asistencias[] |
| **Unidad** | Id, Nombre, Direccion |
| **Clase** | Id, Nombre, Descripcion, CupoMaximo, Duracion, UnidadId, ProfesorId, Horarios[], EstaActiva (soft delete) |
| **Horario** | Id, DiaSemana, HoraInicio, HoraFin, ClaseId |
| **Inscripcion** | Id, FechaInscripcion, Estado (Activa/Cancelada), HorarioId (FK), SocioId. El cupo se controla por Horario. |
| **Asistencia** | Id, Fecha, SocioId, ClaseId, HorarioId. Registrada por el profesor. |
| **Plan** | Id, Nombre, Precio, Descripcion, UnidadId, EstaActivo (soft delete) |
| **Cuota** | Id, FechaVencimiento, FechaPago (nullable), MontoPagado (nullable), PlanId, SocioId. El estado (AlDia/ProximaAVencer/Vencida) se **calcula** en Application a partir de FechaVencimiento y FechaPago, no se persiste. |
| **Rutina** | Id, Nombre, Descripcion, SocioId, Ejercicios[] |
| **Ejercicio** | Id, Nombre, Series, Repeticiones, Peso, RutinaId |
| **Evento** | Id, Titulo, Descripcion, Fecha, UnidadId |
| **Notificacion** | Id, Tipo (Recordatorio/Evento/CambioHorario), Mensaje, FechaEnvio, SocioId |

### Estrategia de herencia EF Core

**TPH (Table Per Hierarchy)** para Usuario → Profesor/Socio. Una sola tabla `Usuarios` con columna discriminadora. Admin es un valor de `Rol`, no una subclase.

### Soft Delete
Las entidades `Socio`, `Clase` y `Plan` usan soft delete (campo `EstaActivo` / `EstaActiva`) para preservar integridad referencial con datos históricos (cuotas, inscripciones, asistencias).

### Relaciones principales
- Socio 1:N Cuotas
- Socio 1:N Inscripciones
- Socio 1:N Asistencias
- Socio 1:N Rutinas
- Clase 1:N Horarios
- Horario 1:N Inscripciones (cupo por horario)
- Horario 1:N Asistencias
- Profesor 1:N Clases
- Usuario N:M Unidades (tabla intermedia UsuarioUnidad)
- Unidad 1:N Clases, Planes
- Rutina 1:N Ejercicios
- Plan 1:N Cuotas

---

## 5. Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| Frontend | React 19, TypeScript, Vite |
| UI | Tailwind CSS, shadcn/ui |
| State/Data | React Query (TanStack Query), Context API, axios |
| Routing | React Router v7 |
| Backend | C# / .NET 8, ASP.NET Core Web API |
| ORM | Entity Framework Core (Code-First) |
| DB | PostgreSQL 16 |
| Auth | JWT (fase 1) + OAuth 2.0 Google (fase 2) |
| Testing Backend | xUnit + Moq |
| Testing Frontend | Vitest |
| CI/CD | GitHub Actions |
| Versionado | Git + GitHub, Conventional Commits |
| UML | Astah |

---

## 6. Autenticación y Autorización

### Fase 1 — JWT con roles
- Login con email/contraseña
- 3 roles: Admin, Socio, Profesor
- JWT access token + refresh token
- Endpoints protegidos con `[Authorize(Roles = "...")]`
- Passwords hasheados con BCrypt

### Fase 2 — OAuth 2.0 (Google)
- Solo para portal de socios
- Login con Google como alternativa
- Se vincula cuenta Google a Socio existente

---

## 7. Patrones de Diseño

| Patrón | Uso |
|--------|-----|
| **Repository** | Abstracción de persistencia en Infrastructure |
| **Unit of Work** | Transacciones atómicas con EF Core |
| **CQRS simplificado** | Commands (escritura) y Queries (lectura) separados en Application |
| **Strategy** | Reglas de cálculo de cuotas según plan |
| **Observer** | Notificaciones del dashboard |
| **Facade** | Interacción multi-espacio simplificada |

---

## 8. API REST — Endpoints principales

```
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/google          (fase 2)

GET    /api/socios
POST   /api/socios
GET    /api/socios/{id}
PUT    /api/socios/{id}
DELETE /api/socios/{id}

GET    /api/clases
POST   /api/clases
GET    /api/clases/{id}
PUT    /api/clases/{id}
DELETE /api/clases/{id}
GET    /api/clases/{id}/horarios

POST   /api/inscripciones
DELETE /api/inscripciones/{id}

GET    /api/asistencias
POST   /api/asistencias               (profesor registra asistencia)
GET    /api/asistencias/clase/{claseId}

GET    /api/cuotas
GET    /api/cuotas/pendientes
PUT    /api/cuotas/{id}/pagar          (admin registra pago)
POST   /api/cuotas/recordatorios

GET    /api/planes
POST   /api/planes
PUT    /api/planes/{id}
DELETE /api/planes/{id}               (soft delete)

GET    /api/dashboard/resumen
GET    /api/dashboard/socios-activos
GET    /api/dashboard/cuotas-pendientes
GET    /api/dashboard/clases-hoy

GET    /api/profesores
POST   /api/profesores
PUT    /api/profesores/{id}

GET    /api/eventos
POST   /api/eventos

GET    /api/rutinas/{socioId}
POST   /api/rutinas
PUT    /api/rutinas/{id}

GET    /api/unidades
```

Todos los endpoints admin soportan query param `?unidadId=` para filtrado multi-espacio.

---

## 9. CI/CD

### Pipeline GitHub Actions
- **En cada PR a develop:**
  - Build backend (.NET 8)
  - Run xUnit tests contra PostgreSQL 16 (Docker container)
  - Build frontend (Vite)
  - Run Vitest
  - PR bloqueado si algo falla

### Branching Strategy
- `main` — código estable, solo merge desde develop al cierre de iteración
- `develop` — integración, todo lo que pasa PR review
- `feature/[nombre]` — ramas de trabajo
- `bugfix/[nombre]` — correcciones

### Conventional Commits
- `feat:` nueva funcionalidad
- `fix:` corrección de bug
- `docs:` documentación
- `test:` pruebas
- `refactor:` reestructuración sin cambio funcional
- `chore:` tareas técnicas

---

## 10. Decisiones Técnicas

| Decisión | Opción elegida | Razón |
|----------|---------------|-------|
| Repo | Monorepo | Equipo de 3, coordinación simple |
| Estructura .NET | 4 proyectos Clean Architecture + tests separados | Estándar académico |
| Bundler frontend | Vite | Moderno, rápido, estándar 2026 |
| Componentes UI | shadcn/ui + Tailwind | Customizable, profesional, rápido |
| State management | React Query + Context API | Server state vs client state bien separados |
| Routing | React Router v7 | Maduro, documentado |
| Auth inicio | JWT solo, Google OAuth después | Incrementalidad, menor complejidad inicial |
| EF Core approach | Code-First | Dominio manda, migraciones versionadas |
| Herencia EF | TPH | Mejor performance para queries |
| Diseño frontend | UI/UX Pro Max plugin | Diseño profesional y consistente |
| Soft delete | Clase, Plan, Socio usan EstaActivo/a | Preservar integridad referencial histórica |
| Usuario-Unidad | Relación N:M | Un socio puede estar en ambas unidades |
| Cuota.Estado | Calculado en Application | Evitar jobs de sincronización, estado siempre correcto |
| Admin | Valor de enum Rol, no subclase | No tiene atributos propios |

---

## 11. Alcance Negativo (NO hacer)

- No procesar pagos online (solo registrar estado de cuota manualmente)
- No hacer app móvil nativa (solo web responsive)
- No integrar QR/molinete para acceso físico
- No hacer tienda de productos
- No hacer programa de fidelización
- No migrar datos desde SmartGym
- No cargar datos personales reales de socios durante desarrollo
- No incluir hosting/mantenimiento post-entrega (se define con Maurice después)
