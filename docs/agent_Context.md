# GymFlow — Contexto para Agentes de IA

> Este archivo es para que cualquier integrante del equipo le dé contexto a su agente de IA (Claude Code, etc.) sobre el proyecto. Leé este archivo antes de hacer cualquier cosa.

---

## Qué es GymFlow

GymFlow es una **plataforma web de gestión integral** para **Espacio Mora**, un emprendimiento familiar en Montevideo, Uruguay. El negocio tiene dos unidades:

1. **Gimnasio Nuevo Malvín** — Musculación y fitness tradicional
2. **Espacio Mora** — Telas aéreas, artes marciales, actividades infantiles

Actualmente usan 2 suscripciones separadas de SmartGym. GymFlow las reemplaza con una solución unificada a medida.

**Proyecto académico** — Universidad ORT Uruguay, carrera ATI, 1er semestre 2026.
**Equipo:** Ignacio Compan (268502), Franco Notte (243233), Sebastián Acuña (309167).
**Tutor:** Sebastián Pombo.
**Cliente:** Maurice (propietario de Espacio Mora).

---

## Arquitectura

**Clean Architecture** con 4 capas, dependencias unidireccionales hacia adentro:

```
API (Presentation) → Application → Domain ← Infrastructure
```

- **Domain** — Entidades, enums, reglas de negocio puras. SIN dependencias externas.
- **Application** — Casos de uso, DTOs, interfaces, validaciones. Depende solo de Domain.
- **Infrastructure** — Implementación de repos (EF Core), servicios externos. Implementa interfaces de Application.
- **API** — Controllers ASP.NET Core, middlewares, configuración JWT. Punto de entrada HTTP.

---

## Stack Tecnológico

### Backend
- **C# / .NET 8** con ASP.NET Core Web API
- **Entity Framework Core** (Code-First, migraciones)
- **PostgreSQL 16**
- **JWT** para autenticación (+ login de socios con Google OAuth 2.0, implementado en It.5)
- **xUnit + Moq** para testing

### Frontend
- **React 19** con **TypeScript**
- **Vite** como bundler
- **Tailwind CSS** + **shadcn/ui** para componentes
- **React Query (TanStack Query)** para server state
- **Context API** para client state (auth, tema)
- **React Router v7** para ruteo
- **axios** para comunicación con API
- **Vitest** para testing

### Herramientas
- **Docker + Docker Compose** — Entorno de desarrollo (PostgreSQL + Backend API)
- **Git + GitHub** — Conventional Commits
- **GitHub Actions** — CI/CD pipeline
- **Astah** — Diagramas UML

---

## Estructura del Monorepo

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
│   ├── src/
│   │   ├── components/       # shadcn/ui + componentes custom
│   │   ├── pages/            # Vistas por ruta
│   │   ├── hooks/            # Custom hooks
│   │   ├── services/         # API calls (axios instances)
│   │   ├── context/          # Auth context, theme
│   │   ├── types/            # TypeScript interfaces/types
│   │   ├── lib/              # Utilidades
│   │   └── assets/           # Imágenes, fuentes
│   └── public/
├── .github/workflows/        # CI/CD
├── docker-compose.yml        # PostgreSQL + Backend API
├── docs/                     # Documentación
└── README.md
```

---

## Entorno de Desarrollo (Docker)

El proyecto usa Docker Compose para levantar el entorno completo. Un compañero clona el repo y:

```bash
# Levantar backend + PostgreSQL
docker compose up --build -d

# Levantar frontend (otra terminal)
cd frontend && npm install && npm run dev
```

**Qué hace `docker compose up`:**
- Levanta PostgreSQL 16 en puerto 5432
- Levanta la API .NET en puerto 5146 (conectada a la DB)
- Aplica migraciones de EF Core automáticamente al iniciar

**Frontend:** corre local con `npm run dev` en puerto 5173. Vite tiene un proxy configurado que redirige `/api/*` al backend en `localhost:5146`.

**Comandos útiles:**
```bash
docker compose logs -f api    # Ver logs del backend
docker compose down           # Parar todo
docker compose down -v        # Reset completo (borra DB)
```

**Agregar nuevas migraciones (cuando se modifica el modelo):**
```bash
cd backend
dotnet ef migrations add NombreMigracion --project src/GymFlow.Infrastructure --startup-project src/GymFlow.API --output-dir Persistence/Migrations
```
La migración se aplica automáticamente en el próximo `docker compose up`.

---

## Modelo de Dominio — Entidades Principales

### Jerarquía de Usuarios (TPH — Table Per Hierarchy)

```
Usuario (abstract base, PasswordHash nullable)
├── Empleado — admin, profesor, recepcionista, etc. Login email + password (BCrypt). PasswordHash siempre seteado.
└── Socio — Cuotas[], Inscripciones[], Asistencias[], Rutinas[], TipoDocumento, GoogleUserId. Login con Google OAuth (It.5, implementado). PasswordHash null (no gestiona contraseña propia).
```

**El rol del usuario es un `RolId` (FK a `Rol`)**, no una subclase. La jerarquía solo refleja diferencias de atributos y mecanismo de auth, no de rol asignado.

**Relación Usuario-Unidad es N:M** — un socio o profesor puede pertenecer a ambas unidades (tabla intermedia `UsuarioUnidad`). La tabla `UsuarioUnidad` incluye `PlanId` (nullable, FK a `Planes`): cada socio puede tener un Plan distinto por Unidad. Un socio ya no tiene un Plan global único.

### Entidades del negocio

| Entidad | Descripción | Relaciones clave |
|---------|-------------|------------------|
| **Unidad** | Gimnasio Nuevo Malvín o Espacio Mora | Agrupa Clases, Planes. Usuarios via N:M. |
| **Clase** | Actividad con cupo y duración | Pertenece a Unidad y Profesor, tiene Horarios[]. Soft delete (`EstaActiva`). |
| **Horario** | Día + hora inicio/fin | Pertenece a Clase. El cupo se controla por Horario. |
| **Inscripcion** | Socio inscrito a un Horario específico | Tiene HorarioId (no ClaseId). Estado: Activa/Cancelada. |
| **Asistencia** | Registro de presencia | SocioId, ClaseId, HorarioId, Fecha. Registrada por profesor. |
| **Plan** | Tipo de membresía con precio | Pertenece a Unidad. Soft delete (`EstaActivo`). CRUD completo vía UI (RF_22). Eliminación bloqueada si hay socios asignados. |
| **Cuota** | Pago periódico del socio | FechaVencimiento, FechaPago (nullable), MontoPagado (nullable). **Estado se calcula en Application**, no se persiste. |
| **Rutina** | Rutina de ejercicios personalizada | Pertenece a Socio, tiene Ejercicios[] |
| **Ejercicio** | Series, repeticiones, peso | Pertenece a Rutina |
| **Evento** | Actividad especial | Pertenece a Unidad |
| **Notificacion** | Recordatorio, evento, cambio horario | Dirigida a Socio |

---

## 4 Vistas de Usuario

1. **Página web pública** — Landing con info del negocio, horarios, planes, SEO optimizado. Sin login.
2. **Panel de administración** — Dashboard en tiempo real, CRUD de socios/clases/cuotas/profesores/eventos, filtrado por unidad.
3. **Portal de socios** — Ver horarios, inscribirse a clases, ver perfil, cuotas, rutinas. Login requerido.
4. **Vista de profesores** — Ver clases asignadas, registrar asistencia. Login requerido.

---

## Autenticación

**Dos mecanismos de login según tipo de usuario:**

| Tipo | Login | Roles posibles |
|---|---|---|
| **Empleado** | email + password (MFA It.5: pendiente) | Cualquier rol salvo Socio |
| **Socio** | Google OAuth 2.0 (It.5: implementado) | Únicamente el rol Socio |

- **JWT** firmado con clave simétrica, expiración 8 horas. Lleva `userId`, `correo`, `rolId`, `rolNombre`, `nombre`, `apellido`.
- **Passwords de empleados** hasheados con BCrypt.Net-Next (factor 11).
- **Endpoints protegidos** con `[RequierePermiso(Modulo, Operacion)]` (no `[Authorize(Roles=...)]`).
- **Empleado de bootstrap:** la migración crea `admin@gymflow.com` / `admin123` automáticamente. En producción debe cambiar su password al primer login.

**Estado actual (It.2):** login productivo solo para Empleados. Login de Socios queda para It.5.

---

## Patrones de Diseño en Uso

| Patrón | Dónde |
|--------|-------|
| Repository | Infrastructure — abstracción de EF Core |
| Unit of Work | Infrastructure — transacciones con EF Core |
| CQRS simplificado | Application — Commands y Queries separados |
| Strategy | Application — cálculo de cuotas según plan |
| Observer | Notificaciones del dashboard |
| Facade | Interacción multi-espacio |

---

## Branching, Git y CI/CD

### Ramas
- `main` — Código estable, testeado y aceptado. Solo se actualiza mediante merge desde `develop` al cierre de cada iteración, una vez que el incremento fue integrado, probado y aceptado por Maurice.
- `develop` — Rama de integración. Todo lo que está acá compila, pasa tests y está listo para validar. PR review obligatorio por al menos otro integrante.
- `feature/[nombre]` — Ramas de trabajo por funcionalidad. Se crean desde `develop`. Ejemplos: `feature/registro-socios`, `feature/dashboard`, `feature/pagina-web-publica`.
- `bugfix/[nombre]` — Correcciones de defectos. Ejemplo: `bugfix/calculo-cuota-vencida`.

### Flujo de trabajo Git

```
1. Crear rama feature desde develop:
   git checkout develop && git pull
   git checkout -b feature/nombre-descriptivo

2. Trabajar con commits frecuentes (conventional commits).

3. Cuando la feature está lista:
   - Verificar que compila: dotnet build (backend) / npm run build (frontend)
   - Verificar que tests pasan: dotnet test / npx vitest run
   - Push y crear Pull Request hacia develop

4. Otro integrante revisa el código y aprueba o pide ajustes.

5. Merge a develop solo cuando:
   - El desarrollo está completo
   - Compila correctamente
   - Tests pasan
   - PR aprobado por al menos 1 integrante
   - Pipeline CI verde

6. Merge de develop a main:
   - Solo al cierre de cada iteración
   - Todas las features de la iteración integradas
   - Tests del plan de pruebas ejecutados
   - Incremento aceptado por Maurice
   - Se tagea con versión (v1.0, v1.1, v2.0)
```

### IMPORTANTE: Nunca pushear directo a `main` o `develop`
Todo va por Pull Request. No se hacen commits directos a main ni a develop.

### Conventional Commits
```
feat: nueva funcionalidad
  Ejemplo: feat: agregar registro de socios con validación de duplicados
fix: corrección de bug
  Ejemplo: fix: corregir cálculo de fecha de vencimiento de cuota
docs: documentación
  Ejemplo: docs: actualizar diagrama de arquitectura
test: pruebas
  Ejemplo: test: agregar pruebas unitarias para ClaseService
refactor: reestructuración sin cambio funcional
chore: tareas técnicas o configuración
  Ejemplo: chore: configurar variables de entorno del proyecto
```

### Pipeline (GitHub Actions)
En cada PR a develop:
1. Build backend (.NET 8)
2. Run xUnit tests con PostgreSQL 16 (Docker)
3. Build frontend (Vite)
4. Run Vitest
5. **PR bloqueado si algo falla**

### Trazabilidad de versiones
Cada merge a main se etiqueta con un tag de versión. Esto permite trazar:
- Qué requerimientos se implementaron en cada iteración
- Qué funcionalidades se integraron
- Qué pruebas se ejecutaron
- Qué versión corresponde a cada entrega

---

## Reglas para el Agente

1. **Mantené actualizados los documentos de contexto** — Cada vez que hagas un cambio de código que lo implique, actualizá `docs/agent_Context.md` y `docs/GymFlow_Requerimientos_Completos.md`. Esto incluye: agregar nuevas entidades/campos, cambios en reglas de negocio, nuevos endpoints, cambios en validaciones, o cualquier decisión de diseño relevante.
2. **Seguí Clean Architecture** — Domain no depende de nada externo. Las dependencias van hacia adentro.
2. **Code-First** — Las entidades se definen en Domain, se mapean en Infrastructure con EF Core.
3. **No pongas lógica de negocio en controllers** — Va en Application (casos de uso) o Domain.
4. **Conventional Commits** — Siempre usá el formato `feat:`, `fix:`, etc.
5. **TypeScript estricto** — No uses `any`. Definí types en `src/types/`.
6. **Tailwind + shadcn/ui** — No uses CSS modules ni styled-components.
7. **React Query para server state** — No pongas data de API en useState.
8. **Tests** — Backend con xUnit, frontend con Vitest.
9. **Multi-espacio** — Toda query administrativa debe soportar filtrado por UnidadId.
10. **No agregues features fuera del alcance** — No hay pagos online, no hay app móvil nativa, no hay QR/molinete.

### Al agregar un módulo nuevo (RNF-01 — Roles y Permisos)

Cuando se crea un módulo nuevo en el backend (ej. `Cuotas`, `Eventos`), **es obligatorio**:

1. Agregar el valor al enum `GymFlow.Domain.Enums.Modulo` (en `backend/src/GymFlow.Domain/Enums/Modulo.cs`).
2. Generar una migración EF Core que inserte las 4 filas correspondientes en la tabla `Permisos`:
   - `(NuevoModulo, Lectura)`
   - `(NuevoModulo, Escritura)`
   - `(NuevoModulo, Modificacion)`
   - `(NuevoModulo, Eliminacion)`
3. Decidir si el rol `Administrador` debe tener esos 4 permisos automáticamente (lo más común: sí). Si sí, la misma migración inserta las 4 filas en `RolPermisos` apuntando al `RolSeed.AdminRolId`.
4. Aplicar `[RequierePermiso(Modulo.NuevoModulo, Operacion.X)]` a los endpoints del controller.
5. En el frontend, actualizar el tipo `Modulo` en `frontend/src/types/permisos.ts` y agregar el grupo correspondiente en `Sidebar.tsx` con la propiedad `modulo` para que se filtre por permiso.



**Para crear empleados que puedan loguearse, ver:** `docs/superpowers/specs/2026-04-28-rnf-01-gestion-usuarios.md`. La gestión de usuarios usa la entidad `Empleado` (subclase de `Usuario`) y se administra desde `/admin/usuarios` en el frontend.

> **Validación de cédula uruguaya (en `Socio.EsCedulaUruguayaValida`):**
> Normalizar eliminando puntos y guiones. Paddear a 8 dígitos con cero a la izquierda. Pesos: `[2,9,8,7,6,3,4]`.
> Válida si `(suma_ponderada + dígito_verificador) % 10 == 0`.

---

## Alcance Negativo (NO hacer)

- No procesar pagos online (solo registrar estado de cuota)
- No hacer app móvil nativa (solo web responsive)
- No integrar QR/molinete
- No hacer tienda de productos
- No hacer programa de fidelización
- No migrar datos desde SmartGym
- No cargar datos personales reales de socios durante desarrollo

---

## Links y Referencias

- **Spec completo:** `docs/superpowers/specs/2026-03-25-gymflow-design.md`
- **RF_22 — Gestión de Planes y Plan por Unidad:** `docs/superpowers/specs/2026-04-06-rf22-planes-plan-por-unidad-design.md`
- **Documento académico:** Archivo .docx del anteproyecto ORT
- **Metodología:** MCS-OpenUP de AGESIC
