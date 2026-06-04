---
tags:
  - spec
requerimiento: RNF-01
---

# RNF-01 (parte 1) — Roles dinámicos y autorización por permisos

**Iteración:** 2
**Branch:** `feature/RNF_01`
**Estado:** Implementado y mergeado a `develop`.
**Plan:** [[plan-rnf01-roles-y-permisos]]
**Última actualización:** 2026-04-26
**Historial:**
- 2026-04-26 — Versión inicial

> **Nota sobre la reclasificación:** Este trabajo originalmente se documentó como "RF-23". Tras revisar el documento académico actualizado (ATI-268502-243233-309167), quedó claro que **no es un requerimiento funcional nuevo** sino la implementación técnica del **RNF-01** ("Autenticación y autorización basada en roles"). El documento académico explicita que RNF-01 se entrega en dos partes: **It.2** (admin, profesor y otros roles internos) y **It.5** (socios con OAuth). Este spec corresponde a la **parte 1 de It.2** (catálogo de roles + permisos + atributo de autorización). La **parte 2 de It.2** (gestión de usuarios empleados) está en un spec aparte: [[spec-rnf01-gestion-usuarios]].

---

## Contexto y motivación

El sistema actualmente tiene tres roles fijos definidos como un enum en el código (`Admin`, `Profesor`, `Socio`). Durante la reunión de revisión con el cliente, las responsabilidades concretas del rol **Profesor** no quedaron definidas, por lo que no se sabe a qué funcionalidades debe tener acceso ni cómo diferenciarlo del Administrador.

Antes que dejar bloqueada la implementación de autenticación esperando esa definición, se decide reemplazar el modelo de roles fijos por un esquema flexible donde el administrador pueda crear roles personalizados combinando permisos por módulo y operación CRUD.

Esta decisión se toma **en Iteración 2**, en paralelo a la migración de los usuarios hardcodeados a base de datos (parte 2). Hacerlo en este momento, antes de implementar el resto del sistema de autenticación productivo, evita rehacer trabajo después.

## Objetivo

Reemplazar el enum `Rol` por un sistema de **roles dinámicos** con permisos granulares por módulo y operación CRUD, dejando al administrador la capacidad de crear roles personalizados (incluyendo el rol "Profesor" cuando el cliente defina sus responsabilidades).

## Alcance

### Incluido
- Modelo de dominio: entidades `Rol`, `Permiso`, `RolPermiso`.
- Migración EF Core que crea las tablas y elimina la columna `Rol` (enum) de `Usuarios`, reemplazándola por `RolId`.
- Seed data para roles base (`Administrador`, `Socio`).
- Atributo de autorización `[RequierePermiso(Modulo, Operacion)]` que reemplaza a `[Authorize(Roles=...)]`.
- Cache en memoria de permisos por rol con invalidación al editar.
- JWT que incluye `RolId` (en lugar del nombre del rol como string).
- Endpoints CRUD de gestión de roles (`/api/roles`) — solo accesibles por admin.
- Endpoint de catálogo de permisos (`/api/permisos`) para consumo del frontend.
- `LoginResponse` actualizado: incluye nombre del rol y lista de permisos.
- Frontend: hook `usePermisos()` y reemplazo de checks `if (rol === 'Admin')` por `tienePermiso(modulo, operacion)`.
- Pantalla de gestión de roles en frontend (matriz módulo × operación con checkboxes).
- Eliminación completa del enum `Rol` y arreglo de todos los call sites rotos.
- Tests de casos borde y happy path en Domain, Application y autorización.

### Fuera de alcance
- Permisos a nivel de acción específica (ej. "exportar reporte"). El sistema solo modela CRUD por módulo.
- Definición del rol "Profesor". El admin lo creará cuando el cliente especifique responsabilidades.
- Migración de usuarios hardcodeados a base de datos. Se mantienen hardcodeados en `AuthController` pero apuntando al `RolId` correcto.
- Lógica de ownership del Socio (verificar que solo accede a sus propios datos). Se maneja en cada endpoint relevante, no en el sistema de permisos.
- Auditoría de cambios en roles/permisos (la auditoría general ya existe; no se agrega nada específico).

## Decisiones de diseño

### 1. Catálogo de módulos y operaciones cerrado en código
`Modulo` y `Operacion` se modelan como enums en C#, no como tablas editables. El admin no puede inventar módulos nuevos — son lo que el dev define en el código.

- **Módulos (solo los que existen hoy):** `Socios, Planes, Unidades, Auditoria`.
- **Operaciones:** `Lectura, Escritura, Modificacion, Eliminacion`.

La tabla `Permiso` se llena por seed data como producto cartesiano (4 × 4 = 16 filas iniciales). Crece cuando el dev agrega un módulo nuevo: en ese caso debe extender el enum `Modulo` **y** agregar las 4 filas correspondientes vía nueva migración EF Core. Esta convención queda documentada en `docs/agent_Context.md` para que cualquier agente o desarrollador la respete al crear un módulo nuevo.

### 2. Roles de sistema vs. personalizados
La tabla `Rol` tiene un flag `EsSistema`. Los roles base (`Administrador`, `Socio`) tienen `EsSistema = true` y no pueden ser editados ni eliminados desde la UI. El rol `Profesor` **no se crea** en el seed — el admin lo crea desde la UI cuando el cliente especifique sus permisos.

### 3. JWT lleva `RolId`, no la lista de permisos
El token JWT incluye el `RolId` del usuario. El backend resuelve los permisos contra la cache cada request. Esto evita tokens enormes y permite revocar/cambiar permisos sin invalidar tokens activos.

### 4. Cache de permisos en memoria
**Problema:** cada request a un endpoint protegido necesita resolver "¿este rol tiene tal permiso?". Hacerlo contra la base de datos cada vez genera una query SQL por cada request a cada endpoint protegido — innecesario porque los permisos cambian rarísimo (solo cuando un admin edita un rol).

**Solución:** se usa `IMemoryCache` (cache en RAM del proceso ASP.NET) con TTL de 30 minutos. La primera consulta de un rol va a DB y guarda el resultado; las siguientes leen de RAM. Cuando un endpoint de gestión modifica/elimina un rol, llama explícitamente a `cache.Invalidar(rolId)` para que la próxima request relea de DB.

**Limitación conocida:** funciona en una sola instancia del backend. Si más adelante el sistema corre con varias instancias en paralelo, la invalidación de cache no se propaga entre ellas y quedan inconsistentes hasta que expire el TTL. Para iteración 1 con un solo proceso es perfecto. Si se escala horizontalmente, reemplazar por Redis u otro cache distribuido — fuera de alcance.

### 5. Ownership del Socio fuera del sistema de permisos
El rol `Socio` no tiene permisos asignados en `RolPermiso`. Los endpoints que un socio necesita consumir (ver sus propios datos, ver sus cuotas, etc.) implementan lógica de ownership directamente: verifican que el `userId` del JWT coincida con el dueño del recurso. Esto evita modelar "permisos sobre uno mismo" en el sistema, que sería una excepción contaminante.

### 6. Borrado limpio del enum `Rol`
El enum `Rol` se elimina completamente. Todo código que lo use queda roto y se arregla guiados por el compilador. No se mantiene compatibilidad temporal.

### 7. Hardcoded users mantienen su lugar
Los tres usuarios hardcodeados en `AuthController` se conservan pero apuntando al `RolId` real (en lugar del string `"Admin"`, etc.). El usuario "profesor@gymflow.com" se elimina por ahora (no hay rol Profesor); queda solo `admin` y `socio`. Cuando se implemente el módulo de Empleados se migrará a base de datos.

## Modelo de datos

```
Rol
├── Id (Guid, PK)
├── Nombre (string, único)
├── EsSistema (bool)
└── FechaCreacion (DateTime)

Permiso
├── Id (Guid, PK)
├── Modulo (enum: Socios, Clases, Cuotas, Empleados, Eventos, Rutinas, Dashboard)
└── Operacion (enum: Lectura, Escritura, Modificacion, Eliminacion)
    [Unique constraint: (Modulo, Operacion)]

RolPermiso
├── RolId (FK → Rol)
└── PermisoId (FK → Permiso)
    [PK compuesta: (RolId, PermisoId)]

Usuario
├── ... (campos existentes)
└── RolId (FK → Rol)   ← reemplaza la columna Rol enum
```

### Seed data inicial
- Rol `Administrador` (EsSistema=true) con los 16 permisos asignados.
- Rol `Socio` (EsSistema=true) sin permisos asignados (usa lógica de ownership).
- 16 filas en `Permiso` (producto cartesiano de los enums actuales: 4 módulos × 4 operaciones).

## Cambios en endpoints

### Nuevos
- `GET /api/permisos` — devuelve catálogo de permisos. Requiere autenticación.
- `GET /api/roles` — lista roles. Requiere `[RequierePermiso(Empleados, Lectura)]`.
- `GET /api/roles/{id}` — detalle de rol con sus permisos.
- `POST /api/roles` — crea rol personalizado. Requiere `[RequierePermiso(Empleados, Escritura)]`.
- `PUT /api/roles/{id}` — actualiza rol (nombre y permisos). Bloquea si `EsSistema = true`.
- `DELETE /api/roles/{id}` — elimina rol. Bloquea si `EsSistema = true` o si hay usuarios asignados.

### Modificados
- Todos los controladores existentes: reemplazar `[Authorize(Roles="Admin")]` por `[RequierePermiso(Modulo.X, Operacion.Y)]`.
- `AuthController.Login`: el `LoginResponse` ahora incluye `{ rolNombre, permisos: [...] }` en lugar del string `Rol`.
- `AuthController.Me`: idem.

## Cambios en frontend

- `AuthContext`: almacenar lista de permisos del usuario logueado.
- Hook `usePermisos()`: expone `tienePermiso(modulo, operacion)`.
- Reemplazar todos los checks `user.rol === 'Admin'` por `tienePermiso(...)`.
- Esconder ítems del menú lateral según permisos (si no tiene `Lectura` en un módulo, no se muestra).
- Nueva pantalla `/admin/roles` con:
  - Lista de roles existentes.
  - Botón "Nuevo rol".
  - Form de creación/edición: input `nombre` + matriz de checkboxes (filas = módulos, columnas = operaciones).
  - Roles `EsSistema = true` se muestran read-only.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Romper endpoints existentes al cambiar `[Authorize(Roles=...)]` por `[RequierePermiso(...)]` | Cambio guiado por compilador (al borrar el enum, todo lo que lo usa rompe). Tests de autorización para los endpoints críticos. |
| Cache de permisos desactualizada tras editar un rol | Endpoint de modificación invalida la cache del rol afectado explícitamente. |
| Admin se queda sin permisos al editar su propio rol | El rol `Administrador` es `EsSistema = true` y no se puede editar. |
| Usuario hardcodeado de `profesor@gymflow.com` pierde sentido | Se elimina ese usuario. Quedan `admin` y `socio` hasta que se implemente el módulo de Empleados. |

## Estrategia de testing

- **Domain:** tests unitarios de `Rol`, `Permiso`, validación de invariantes (no permitir borrar rol de sistema, etc.).
- **Application:** tests de los UseCases de gestión de roles (crear, modificar, eliminar) — happy path + casos borde (rol con usuarios asignados no se puede borrar, nombre duplicado, rol de sistema inmutable).
- **Autorización:** test del atributo `[RequierePermiso]` con un controlador de prueba — verifica que devuelve 403 sin permiso y 200 con permiso.
- **No se hacen** tests E2E ni de cada endpoint individualmente. Confiamos en que el atributo de autorización está testeado en aislamiento.

## Criterios de aceptación

- [x] El enum `Rol` no existe en el código.
- [x] Existe la tabla `Roles`, `Permisos`, `RolPermisos` en la base de datos con seed data.
- [x] Un usuario sin el permiso requerido recibe 403 al llamar a un endpoint protegido.
- [x] El admin puede crear un rol nuevo desde la UI con cualquier combinación de permisos.
- [x] El admin no puede eliminar ni editar los roles `EsSistema = true`.
- [x] El frontend esconde menús e ítems para los que el usuario no tiene permiso.
- [x] Login devuelve permisos del usuario y el frontend los usa para los checks.
- [x] Tests de Domain, Application y autorización pasan.

## Trabajo futuro (fuera de alcance de esta parte)

**Parte 2 de It.2 — Gestión de usuarios empleados** (spec separado: `2026-04-28-rnf-01-gestion-usuarios.md`):
- Migrar usuarios hardcodeados a base de datos.
- CRUD de empleados (admin, profesor, roles custom) desde la UI.
- BCrypt para hasheo de contraseñas.
- `PasswordHash` nullable en `Usuario` para soportar futuro flujo OAuth de socios.

**It.5 — Autenticación productiva completa:**
- Definir e implementar el rol "Profesor" cuando el cliente especifique responsabilidades.
- MFA (TOTP) para roles internos (admin, profesor, custom).
- OAuth 2.0 con Google para login de socios.
- Flujo de credenciales temporales por mail (CU-07).
- Auditoría específica de cambios en roles/permisos.
- Permisos a nivel de acción específica (más allá de CRUD por módulo).
