# RNF-01 (parte 2) — Gestión de usuarios empleados

**Fecha:** 2026-04-28
**Iteración:** 2
**Branch:** `feature/RNF_01` (continuación)
**Estado:** Spec — pendiente de aprobación para generar plan

> **Contexto:** este spec es la continuación de `2026-04-26-rnf-01-roles-y-permisos.md` (parte 1, ya implementada). Juntas, ambas partes cubren la entrega de **RNF-01** prevista para Iteración 2 según el documento académico actualizado.

---

## Contexto y motivación

La parte 1 de RNF-01 dejó implementado el sistema de roles dinámicos y permisos por módulo/operación. Sin embargo, los usuarios que se autentican siguen estando **hardcodeados** en `AuthController` (un admin y un socio con passwords en texto plano). Esto significa que:

- El admin no puede crear nuevos usuarios desde la UI.
- No se pueden agregar profesores ni empleados con roles custom (recepcionistas, encargados, etc.) sin tocar código.
- Los passwords no están hasheados.
- El sistema de permisos construido en la parte 1 está desconectado de la realidad operativa: hay roles configurables pero nadie a quien asignárselos.

Esta parte cierra el loop: persiste los usuarios en base de datos, agrega un CRUD para gestionarlos desde la UI, y hashea passwords con BCrypt.

## Objetivo

Permitir que el administrador cree, edite y dé de baja usuarios internos del sistema (admin, profesor, custom) desde la interfaz, asignándoles cualquier rol configurado (excepto Socio). Reemplazar la lista hardcodeada de `AuthController` por lectura desde DB con verificación de password BCrypt.

## Alcance

### Incluido

- **Modelo de dominio:** nueva entidad concreta `Empleado` como subclase de `Usuario` (sin atributos extra).
- **Cambio en `Usuario`:** `PasswordHash` pasa a ser nullable para soportar Socios que se autenticarán por OAuth (parte de It.5).
- **Migración EF Core:**
  - Agrega el discriminador TPH `"Empleado"`.
  - Hace `PasswordHash` nullable.
  - Seed de un empleado admin inicial (`admin@gymflow.com` / `admin123`, hasheado con BCrypt) — bootstrap del sistema.
- **Dependencia BCrypt:** agregar paquete `BCrypt.Net-Next` al proyecto Application (o Infrastructure, según convenga).
- **Servicio de hashing:** interfaz `IPasswordHasher` en Application + implementación `BCryptPasswordHasher` en Infrastructure.
- **Refactor de `AuthController.Login`:** elimina la lista hardcodeada; busca el empleado por correo en DB y verifica password con BCrypt.
- **CRUD de empleados** — endpoints `/api/empleados`:
  - `GET` — lista con filtro opcional por activo/inactivo.
  - `GET /{id}` — detalle.
  - `POST` — crea empleado (valida correo único, password mínimo, `RolId ≠ RolSocio`).
  - `PUT /{id}` — edita datos básicos (nombre, apellido, correo) y/o cambia `RolId`. Cambio de password es endpoint aparte.
  - `PATCH /{id}/password` — cambia password (solo el admin puede cambiar passwords ajenas en esta iteración; el flujo "cambiar mi propia password" queda para It.5 cuando se implemente MFA).
  - `DELETE /{id}` — baja lógica (`EstaActivo = false`).
  - `PATCH /{id}/reactivar` — reactiva empleado dado de baja.
- **Nuevo módulo de permisos:** agregar `Modulo.Empleados` al enum + migración que inserta los 4 permisos (`Lectura`, `Escritura`, `Modificacion`, `Eliminacion`) y los asigna al rol Administrador automáticamente. La pantalla de Roles existente pasa a permitir asignar estos nuevos permisos a roles custom.
- **Frontend:**
  - Nueva página `/admin/usuarios` con lista de empleados (nombre, correo, rol, estado).
  - Form de creación: nombre, apellido, correo, password inicial, rol (dropdown filtrado sin Socio).
  - Form de edición: mismos campos editables salvo password (que va por su propio botón).
  - Acciones: dar de baja, reactivar, cambiar password.
  - Sidebar: agregar ítem "Usuarios" en el grupo "Sistema" (junto con Auditoría y Roles), filtrado por permiso `Empleados.Lectura`.
  - Service `services/empleados.ts` + types `types/empleado.ts`.
- **Tests:**
  - Domain: constructor de `Empleado`, validaciones.
  - Application: UseCases CRUD (happy path + casos borde — correo duplicado, rol Socio prohibido, password vacío, empleado inexistente).
  - Autorización: el endpoint requiere permiso `Empleados.X`.
  - Refactor del login: test que verifica que `AuthController.Login` consulta DB y rechaza credenciales inválidas.

### Fuera de alcance

- **MFA (TOTP)** — queda para It.5.
- **Flujo de credenciales temporales por correo** (CU-07: el sistema envía un mail con instrucciones para configurar password) — queda para It.5 cuando se integre el envío de mails. Por ahora el admin asigna la password inicial directamente en el form.
- **"Cambiar mi propia password"** desde un perfil del usuario logueado — queda para It.5.
- **Login de socios** (Google OAuth) — queda para It.5.
- **Recuperación de password** ("olvidé mi contraseña") — queda para It.5.
- **Bloqueo por intentos fallidos** — queda para It.5 con MFA.

## Decisiones de diseño

### 1. `Empleado` como subclase concreta sin atributos extra

La jerarquía TPH actual tiene `Usuario` (abstracta) → `Socio`. `Profesor` no existe como clase (era aspiracional en el doc original pero nunca se implementó). Para poder persistir admins y otros roles internos en DB, hace falta una subclase concreta de `Usuario`.

Se elige `Empleado` (no `Admin`) porque:
- **Cubre todos los roles internos** (admin, profesor cuando se cree, recepcionista, encargado, etc.) sin necesidad de una subclase por cada uno.
- **Lo que distingue admin de profesor de recepcionista es el `RolId` asignado**, no la clase.
- **Es coherente con el lenguaje del cliente** (Maurice habla de "empleados" para referirse a su staff).

Si en el futuro se necesita atributos específicos para profesores (ej. `ClasesAsignadas`), se podrá introducir una subclase `Profesor : Empleado`. Por ahora no se hace porque YAGNI.

### 2. Login de empleado vs login de socio: separación clara

| Tipo | Login | Roles posibles |
|---|---|---|
| **Empleado** | email + password (+ MFA en It.5) | Cualquier rol salvo Socio |
| **Socio** | Google OAuth (It.5) | Únicamente el rol Socio |

El flag `EsSistema = true` en el rol `Socio` cobra un significado adicional: **no se puede asignar a un Empleado**. Esto se valida explícitamente en el `CreateEmpleadoCommand`.

### 3. `PasswordHash` nullable

Hoy el constructor de `Usuario` exige `PasswordHash` como obligatorio. Como `Socio` no usa password (va por OAuth en It.5), `CreateSocioCommand` actualmente le pasa el placeholder `"PENDING_OAUTH"` — un string mágico que ensucia el modelo.

Se cambia `PasswordHash` a nullable:
- **Empleado:** siempre seteado.
- **Socio:** `null` hasta It.5. El placeholder `"PENDING_OAUTH"` se elimina.

Esto deja el modelo limpio y preparado para It.5 sin trabajo desperdiciado.

### 4. Bootstrap del primer admin: seed automático

Para evitar el problema "solo un admin crea otro admin pero no hay admin inicial", se sigue el patrón estándar de **seed automático en migración**: la migración crea un `Empleado` admin inicial (`admin@gymflow.com` / `admin123`, hasheado con BCrypt) que reemplaza al admin hardcodeado actual.

En producción (futuro), ese admin entra una vez, crea su usuario real con su correo personal y desactiva (o cambia password de) el admin de bootstrap. Para el proyecto académico se mantiene tal cual.

**Nota técnica sobre BCrypt en seed:** BCrypt usa salt aleatorio, lo que normalmente impediría tener un hash determinista en un seed de migración. Solución: el hash se precalcula una sola vez (por ejemplo, ejecutando `BCrypt.HashPassword("admin123")` localmente) y se hardcodea en la migración. Las migraciones EF Core están diseñadas para tener valores literales — esto es estándar.

### 5. Endpoint separado para cambio de password

`PUT /api/empleados/{id}` no acepta password. El cambio de password va por `PATCH /api/empleados/{id}/password` con `{ nuevaPassword: string }`. Razones:
- **Separación de concerns:** editar datos básicos no requiere validación de password mínimo, ni hashing, ni flujo distinto de auditoría.
- **Granularidad de permisos a futuro:** cuando se introduzca MFA en It.5, "cambiar password" se va a separar conceptualmente de "editar datos" — el flujo correcto requerirá la password actual + el segundo factor.
- **Seguridad:** evita que el form de edición exponga el campo password en el GET (no se devuelve nunca).

### 6. Reuso del módulo `Empleados` en el sistema de permisos

El módulo `Empleados` ya está mencionado en el enum `Modulo` del documento original de spec parte 1, pero no estaba implementado (solo existían `Socios, Planes, Unidades, Auditoria`). Esta parte agrega el valor al enum **y** la migración de seed con sus 4 permisos asignados al rol Administrador. Esto hace coherente que las pantallas de gestión de roles ya existentes muestren `Empleados` como módulo seleccionable para roles custom.

## Modelo de datos

```
Usuario (abstracta, ya existe — campo PasswordHash pasa a nullable)
├── Socio    (existente — ahora con PasswordHash null hasta It.5)
└── Empleado (NUEVA — sin atributos extra, solo constructor)

Permiso (sin cambios estructurales — se agregan 4 filas para Modulo.Empleados)
RolPermiso (sin cambios — Admin gana automáticamente los 4 nuevos permisos)
```

### Cambios en seed

- **Permisos:** se agregan 4 filas para `(Empleados, Lectura/Escritura/Modificacion/Eliminacion)` con IDs deterministas (mismo patrón MD5 actual).
- **RolPermisos:** se agregan 4 filas asignando esos permisos al rol Administrador.
- **Empleado admin de bootstrap:** se inserta un registro en `Usuarios` con discriminador `Empleado`, correo `admin@gymflow.com`, password hasheado, `RolId = AdminRolId`.

### Migración

Una sola migración que cubre:
1. Hace `PasswordHash` nullable.
2. Agrega el discriminador TPH `"Empleado"`.
3. Inserta el seed del empleado admin.
4. Inserta los 4 permisos del módulo `Empleados` y los asigna al rol Administrador.

## Cambios en endpoints

### Nuevos

- `GET /api/empleados` — lista. Requiere `[RequierePermiso(Empleados, Lectura)]`. Query opcional `?activo=true|false`.
- `GET /api/empleados/{id}` — detalle. Requiere `[RequierePermiso(Empleados, Lectura)]`.
- `POST /api/empleados` — crea. Requiere `[RequierePermiso(Empleados, Escritura)]`. Body: `{ nombre, apellido, correo, password, rolId }`. Bloquea si `rolId == RolSocioId` o si correo ya existe.
- `PUT /api/empleados/{id}` — edita datos básicos y/o `RolId`. Requiere `[RequierePermiso(Empleados, Modificacion)]`. Body: `{ nombre, apellido, correo, rolId }`. No acepta password.
- `PATCH /api/empleados/{id}/password` — cambia password. Requiere `[RequierePermiso(Empleados, Modificacion)]`. Body: `{ nuevaPassword }`. Valida mínimo 8 caracteres.
- `DELETE /api/empleados/{id}` — baja lógica. Requiere `[RequierePermiso(Empleados, Eliminacion)]`. Bloquea si el empleado intenta darse de baja a sí mismo.
- `PATCH /api/empleados/{id}/reactivar` — reactiva. Requiere `[RequierePermiso(Empleados, Modificacion)]`.

### Modificados

- `AuthController.Login`: deja de leer la lista hardcodeada; busca el empleado por correo en DB, verifica password con `IPasswordHasher.Verify`, devuelve 401 si no coincide o si el empleado está inactivo.
- `AuthController.Me`: sin cambios estructurales (sigue leyendo del JWT y resolviendo permisos por cache).

## Cambios en frontend

- **Nueva página `/admin/usuarios`** (lista) — análoga a `/admin/socios`:
  - Tabla con columnas: nombre completo, correo, rol, estado (activo/inactivo).
  - Botón "Nuevo usuario" si tiene permiso `Empleados.Escritura`.
  - Acciones por fila (visibles según permisos): Editar, Cambiar password, Dar de baja / Reactivar.
- **`/admin/usuarios/nuevo`** — form de alta: nombre, apellido, correo, password, rol (dropdown que lista todos los roles excepto Socio).
- **`/admin/usuarios/:id/editar`** — form de edición (mismos campos salvo password).
- **`/admin/usuarios/:id/password`** o modal — form de cambio de password (un solo campo + confirmación).
- **Sidebar:** el grupo "Sistema" (que ya existe con Auditoría y Roles) suma "Usuarios". El ítem se filtra por permiso `Empleados.Lectura`. Se renombra el `modulo` del grupo en `Sidebar.tsx` o se cambia la lógica de filtrado para que el grupo sea visible si el usuario tiene permiso de lectura en **cualquiera** de los módulos del grupo (Auditoria, Empleados o roles administrativos).
- **`services/empleados.ts`:** funciones `listarEmpleados`, `obtenerEmpleado`, `crearEmpleado`, `actualizarEmpleado`, `cambiarPassword`, `darDeBajaEmpleado`, `reactivarEmpleado`.
- **`types/empleado.ts`:** interfaces `Empleado`, `CrearEmpleadoRequest`, `ActualizarEmpleadoRequest`, `CambiarPasswordRequest`.
- **`types/permisos.ts`:** agregar `'Empleados'` al tipo `Modulo`.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Romper el login al migrar de hardcoded a DB | Tests del refactor de `AuthController.Login`. Probar manualmente con el usuario `admin@gymflow.com` / `admin123` después de la migración. |
| El admin se borra a sí mismo y nadie puede entrar | El endpoint `DELETE` valida que el `id` no sea el del propio usuario logueado. |
| Hash de BCrypt en seed difiere entre máquinas | Se hardcodea un valor literal precalculado una sola vez. La migración no genera hashes en runtime. |
| Subclase `Empleado` rompe queries existentes que filtran por `Socio` | El discriminador TPH es transparente: `db.Socios` sigue devolviendo solo socios (filtra por discriminador). Los queries existentes no cambian. Tests de regresión en los endpoints de socios. |
| Olvidar agregar el filtro `RolId != RolSocioId` permite crear un Empleado con rol Socio | Validación explícita en `CreateEmpleadoCommand` y `UpdateEmpleadoCommand` + test que verifica el caso. |
| Password de bootstrap (`admin123`) es débil | Aceptable para entorno académico/dev. Para producción se documenta en el README/agent_Context que el admin inicial debe cambiar su password al primer login. |

## Estrategia de testing

- **Domain:**
  - `EmpleadoTests`: constructor válido, constructor con campos vacíos lanza `ArgumentException`.
- **Application:**
  - `CrearEmpleadoCommandTests`: happy path, correo duplicado, rol Socio prohibido, password vacío.
  - `ActualizarEmpleadoCommandTests`: happy path, empleado inexistente, correo duplicado al cambiarlo, rol Socio prohibido.
  - `CambiarPasswordCommandTests`: happy path, password muy corta, empleado inexistente.
  - `DarDeBajaEmpleadoCommandTests`: happy path, empleado inexistente, no puede borrarse a sí mismo.
- **Autorización:**
  - Tests que verifican que cada endpoint devuelve 403 sin el permiso correspondiente y 200/201/204 con él.
- **Login refactor:**
  - Test del nuevo `LoginCommand` (o método refactorizado): credenciales válidas → JWT con permisos; credenciales inválidas → 401; empleado inactivo → 401.

## Criterios de aceptación

- [ ] La entidad `Empleado` existe en el dominio y es subclase concreta de `Usuario`.
- [ ] `Usuario.PasswordHash` es nullable.
- [ ] El placeholder `"PENDING_OAUTH"` ya no aparece en el código (los Socios persistidos quedan con `PasswordHash = null`).
- [ ] Una migración EF Core crea el discriminador TPH para `Empleado`, hace `PasswordHash` nullable, agrega los permisos del módulo `Empleados` y los asigna al rol Administrador, y siembra el empleado admin de bootstrap.
- [ ] El módulo `Empleados` aparece en el enum `Modulo` y en el tipo `Modulo` del frontend.
- [ ] La lista hardcodeada de usuarios en `AuthController` ya no existe.
- [ ] El login funciona contra DB con BCrypt: `admin@gymflow.com` / `admin123` entra exitosamente; cualquier otra combinación devuelve 401.
- [ ] Existen los endpoints `/api/empleados` (CRUD completo + cambio de password + reactivar) protegidos con `[RequierePermiso(Empleados, X)]`.
- [ ] No se puede crear ni editar un empleado con `rolId == RolSocioId`.
- [ ] El admin no puede darse de baja a sí mismo desde la API.
- [ ] La pantalla `/admin/usuarios` permite crear, editar, dar de baja, reactivar y cambiar password de empleados.
- [ ] El sidebar muestra "Usuarios" en el grupo "Sistema" si el usuario logueado tiene permiso `Empleados.Lectura`.
- [ ] Tests de Domain, Application y autorización para todo lo anterior pasan en CI.
- [ ] El CRUD de socios sigue funcionando idéntico a antes (regresión).

## Trabajo futuro (queda para It.5)

- MFA (TOTP) para empleados.
- Google OAuth para socios (sustituye `PasswordHash` por `GoogleUserId`).
- Flujo de "olvidé mi contraseña" / recuperación.
- Flujo de "cambiar mi propia password" desde un perfil de usuario logueado.
- Envío de credenciales temporales por correo al alta (CU-07).
- Bloqueo por intentos fallidos.
- Sub-jerarquía `Profesor : Empleado` con `ClasesAsignadas` cuando se implemente RF-12.
