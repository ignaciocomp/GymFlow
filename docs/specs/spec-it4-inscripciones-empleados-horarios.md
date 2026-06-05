---
tags:
  - spec
  - iteracion
requerimiento: IT4
---

# Iteración 4 — Estabilización de Inscripciones, Empleados y Horarios

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] (§9 — Iteración 4)
**Última actualización:** 2026-06-04
**Historial:**
- 2026-06-04 — v1: spec inicial de IT4 tras auditoría contra criterios de aceptación CU-02 y CU-07

## Resumen

La Iteración 4 es de **estabilización y pulido**: toda la funcionalidad base de inscripción a clases (RF-10, RF-11) y gestión de empleados/profesores (RF-12) ya está implementada en iteraciones anteriores. Esta iteración cierra los **gaps detectados al auditar el código contra los criterios de aceptación** de los casos de uso CU-02 (Inscripción a Clase) y CU-07 (Gestión de Empleados y Profesores), agrega cobertura de tests, mejora la vista de Horarios y verifica responsive.

No incluye features nuevas de gran porte. **RF-13** queda cubierto por el sistema dinámico de roles + permisos (un rol con permiso `Socios:Escritura` puede registrar socios). **RF-14** (asignación profesor↔clase) se descopea de esta iteración. **MFA (RNF-10)** y **OAuth (RNF-01 socios)** se posponen a IT5.

## Alcance

### Incluido

| # | Componente | Mapea a |
|---|------------|---------|
| 1 | Notificación por email + auditoría de inscripción/cancelación | CA-09 (CU-02), RNF-11 |
| 2 | Lista de espera cuando no hay cupo | E1 (CU-02) |
| 3 | Calidad: fix N+1 en `GetMisInscripciones`, `[RequierePermiso]` en controller | RNF, consistencia |
| 4 | Catálogo de clases con filtros (espacio/día/tipo) en portal del socio | Flujo principal CU-02 |
| 5 | Mejora de Horarios: filtro de sede obligatorio + split de clases solapadas | UX (RF-09) |
| 6 | Credenciales temporales por email al crear empleado | CA-29 (CU-07) |
| 7 | Tests de inscripciones + verificación responsive | RNF-03, calidad |

### Fuera de alcance

- **RF-14** (asignación profesor↔clase, vista filtrada del profesor) — `Clase.Instructor` sigue siendo texto libre.
- **RNF-10** (MFA TOTP) — IT5.
- **OAuth 2.0 para socios** — IT5.
- **Solapamiento parcial estilo Teams** (con offset escalonado) — se implementa solo el **split simple** (N columnas iguales).
- **Forzar cambio de password en primer login** del empleado — el empleado usa el flujo existente de "Cambiar password". Se anota como deuda técnica.

## Decisiones de diseño

- **Lista de espera como flag** (`EsListaEspera` en `InscripcionClase`), no entidad separada: menos tablas, ordenamiento por `FechaInscripcion`.
- **Credenciales temporales:** el sistema autogenera un password aleatorio fuerte y lo envía por email. Si `Email:Habilitado=false` (dev/testing), el password se loguea (patrón `[EMAIL SIMULADO]`). El form de "Nuevo empleado" pierde el campo password manual.
- **Split de horarios simple:** cuando N clases caen en franjas que se solapan el mismo día, la celda se divide en N columnas de igual ancho. No se maneja offset por solapamiento parcial.
- **Filtro de sede obligatorio en Horarios:** la grilla no se renderiza hasta que el admin elige una sede (empty state inicial).
- **Emails best-effort (no transaccional):** el envío de email es secundario a la operación. Si el email falla, la operación (inscripción, promoción de lista de espera, alta de empleado) **igual se confirma**; el fallo se loguea y, donde aplica, se registra en auditoría. No se hace rollback por un email fallido. (Mismo criterio que RF-06.)

---

## 1. Componente 1 — Notificación + auditoría de inscripción

### Problema

- No se envía email al inscribirse (viola CA-09).
- No se registra auditoría de inscripción ni cancelación (viola RNF-11).

### Diseño

**Plantilla de email** — nueva clase `InscripcionEmailTemplates` (Application/UseCases/Inscripciones), con `WebUtility.HtmlEncode` en todos los valores dinámicos (mismo patrón que `EmailTemplates` de cuotas y `ClaseEmailTemplates`):

```csharp
internal static class InscripcionEmailTemplates
{
    public static (string Asunto, string Cuerpo) Confirmacion(Socio socio, Clase clase, HorarioClase? horario);
    public static (string Asunto, string Cuerpo) CupoLiberado(Socio socio, Clase clase); // para lista de espera
}
```

**`InscribirSocioCommand`:**
- Recibe `usuarioId` y `usuarioNombre` (del JWT del socio).
- Inyecta `IEmailService`, `IAuditLogger`, `ISocioRepository`.
- Tras inscribir exitosamente: envía email de confirmación + registra auditoría.

```csharp
await _auditLogger.LogAsync(usuarioId, usuarioNombre,
    TipoAccionAuditoria.Creacion, "Inscripcion", inscripcion.Id,
    $"Socio {socio.Nombre} {socio.Apellido} se inscribió a la clase '{clase.Nombre}'");
```

**`CancelarInscripcionCommand`:**
- Recibe `usuarioId`/`usuarioNombre`, inyecta `IAuditLogger`.
- Tras cancelar: registra auditoría (`TipoAccionAuditoria.Baja`, "Inscripcion").

### Criterios cubiertos
- CA-09: socio recibe notificación tras inscripción exitosa.
- RNF-11: log de auditoría de operaciones de inscripción.

---

## 2. Componente 2 — Lista de espera

### Problema

Cuando no hay cupo, `InscribirSocioCommand` tira error "está llena". El flujo E2 de CU-02 indica ofrecer lista de espera y notificar al siguiente cuando se libera un cupo.

### Diseño

**Modelo:** agregar campo a `InscripcionClase`:

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `EsListaEspera` | `bool` (default `false`) | `true` si la inscripción está en lista de espera (no ocupa cupo) |

⚠️ El campo usa setter `private` (patrón del proyecto). Hay que actualizar `InscripcionClaseConfiguration.cs` (EF config) para mapearlo — EF Core lo persiste vía backing field como el resto de las propiedades de la entidad.

Método de dominio:

```csharp
public void PromoverDeListaEspera()
{
    if (!EsListaEspera)
        throw new InvalidOperationException("La inscripción no está en lista de espera.");
    EsListaEspera = false;
}
```

**`InscribirSocioCommand`:**
- El conteo de cupo cuenta solo inscripciones **activas y NO en lista de espera** (ver "Doble método de conteo" abajo).
- Si hay cupo → inscripción normal (`EsListaEspera = false`).
- Si no hay cupo → crear inscripción con `EsListaEspera = true`. El DTO de respuesta indica `enListaEspera: true` + `posicionListaEspera`.

**`CancelarInscripcionCommand`:**
- Tras cancelar una inscripción **activa no-lista-de-espera**, buscar el primero en lista de espera de esa clase (orden por `FechaInscripcion` asc) y promoverlo.
- El promovido pasa a `EsListaEspera = false` y recibe email "Se liberó un cupo en {clase}, ya estás inscripto".
- Orden de operaciones: promover → `SaveChangesAsync` → enviar email (best-effort, no bloquea la promoción si el email falla).

### ⚠️ Doble método de conteo (riesgo a resolver)

Existen **dos** métodos homónimos de conteo de inscripciones activas:
1. `IInscripcionClaseRepository.GetInscripcionesActivasCountAsync(claseId)` — usado por `InscribirSocioCommand` y `GetMisInscripcionesQuery`.
2. `IClaseRepository.GetInscripcionesActivasCountAsync(claseId)` — usado por `UpdateClaseCommand`, `GetClasesQuery`, `GetClaseByIdQuery`, `GetHorariosQuery`, `GetHorarioByIdQuery`, `CancelClaseCommand` (vía `GetInscripcionesActivasAsync`).

**Decisión:** **ambos** deben excluir lista de espera (`EstaActiva == true && EsListaEspera == false`). Razón:
- El cupo "ocupado" que se muestra en la UI (catálogo, horarios, lista de clases) debe contar solo inscripciones reales, no la lista de espera.
- `Clase.Actualizar(...)` valida `CapacidadMaxima >= inscripcionesActivas`. Excluir lista de espera es correcto: el admin puede reducir capacidad sin que la lista de espera lo bloquee.
- `CancelClaseCommand.GetInscripcionesActivasAsync` (para notificar al cancelar la clase) **sí debe incluir** a los de lista de espera (también hay que avisarles que la clase se canceló) → ese método se mantiene incluyendo todos los activos. Se documenta la diferencia: *conteo de cupo* excluye lista de espera, *notificación de cancelación* incluye a todos.

### Métodos de repositorio (consolidados en §11)

```csharp
// IInscripcionClaseRepository
Task<int> GetInscripcionesActivasCountAsync(Guid claseId);        // AJUSTAR: excluir EsListaEspera
Task<InscripcionClase?> GetPrimeroEnListaEsperaAsync(Guid claseId); // NUEVO: orden FechaInscripcion asc
Task<int> GetPosicionEnListaEsperaAsync(Guid inscripcionId);        // NUEVO: ver semántica abajo

// IClaseRepository
Task<int> GetInscripcionesActivasCountAsync(Guid claseId);        // AJUSTAR: excluir EsListaEspera
// GetInscripcionesActivasAsync (para CancelClaseCommand) NO cambia — incluye lista de espera
```

**Semántica de `GetPosicionEnListaEsperaAsync`:** posición **1-based** contando solo las inscripciones en lista de espera de esa clase con `FechaInscripcion` anterior o igual a la dada. Si la inscripción no está en lista de espera, retorna 0.

### Cálculo de `posicionListaEspera` (mapper)

`InscripcionMapper.ToDto` es estático y no tiene acceso a repositorios, así que **no puede** calcular la posición. Solución: el cálculo se hace en los **queries/commands** (que sí tienen el repo) y se pasa al mapper como parámetro adicional, o el mapper recibe el DTO ya con la posición. Se ajusta la firma de `ToDto` para recibir `(inscripcion, clase, cuposOcupados, posicionListaEspera)`.

### Criterios cubiertos
- E2 (CU-02): ofrece lista de espera cuando no hay cupo; notifica al siguiente al liberarse.

---

## 3. Componente 3 — Calidad y consistencia

### 4a. Fix N+1 en `GetMisInscripcionesQuery`

**Problema:** el query itera las inscripciones y llama `GetInscripcionesActivasCountAsync` una vez por inscripción (N+1).

**Diseño:** un solo query que trae las inscripciones con su clase, y resolver el conteo de cupo en memoria o con una query agregada por las claseIds involucradas.

```csharp
// Nuevo método del repo: cuenta activas (no lista espera) agrupado por clase
Task<Dictionary<Guid, int>> GetConteoActivasPorClasesAsync(IEnumerable<Guid> claseIds);
```

El query carga las inscripciones (1 query), arma la lista de claseIds, hace 1 query agregada de conteos, y mapea en memoria. De N+1 a 2 queries.

### 4b. `[RequierePermiso]` en `InscripcionesController`

**Problema:** el controller solo tiene `[Authorize]`. No verifica módulo de permisos.

**Diseño:** los endpoints de inscripción son self-service del socio. Se mantiene `[Authorize]` a nivel clase (cualquier autenticado), porque el `socioId` se extrae del JWT y el command valida ownership. **No se agrega `[RequierePermiso]`** porque inscribirse no es una operación administrativa sobre un módulo — es acción del propio socio sobre sus datos. Se documenta esta decisión explícitamente en el controller con un comentario.

> Nota: este sub-item se reduce a documentar la decisión. La auditoría inicial lo marcó como inconsistencia, pero tras análisis es correcto que sea `[Authorize]` self-service.

---

## 4. Componente 4 — Catálogo de clases con filtros (portal socio)

### Problema

El portal del socio tiene "Mis inscripciones" y "Horarios", pero **no un catálogo navegable de clases disponibles con filtros** desde donde inscribirse (flujo principal de CU-02, pasos 1-4).

### Diseño

Nueva página `frontend/src/pages/portal/CatalogoClasesPage.tsx`:
- Lista de clases activas con: nombre, instructor, sede, horarios, cupos disponibles (`X / capacidadMaxima`).
- **Filtros:** por sede (Gimnasio / Espacio Mora / Todas), por día de la semana, por tipo/nombre de actividad (búsqueda de texto).
- Botón "Inscribirme" por clase → llama `POST /api/inscripciones`.
- Estados visuales: cupo disponible (verde), lleno → botón "Anotarme en lista de espera" (ámbar), ya inscripto (deshabilitado "Ya estás inscripto").
- Manejo de errores: clase llena, inscripción duplicada → toast con mensaje claro.

Endpoint de soporte (admin/portal): reutiliza `GET /api/clases?unidadId=&includeInactive=false`. `ClaseDto` ya expone `inscripcionesActivas`; el frontend calcula `cuposDisponibles = capacidadMaxima - inscripcionesActivas`. **Decisión:** no se agrega `cuposDisponibles` al DTO — se deriva en el frontend para no tocar mapper/DTO. (Recordar que `inscripcionesActivas` ahora excluye lista de espera, ver §3.)

### Criterios cubiertos
- Flujo principal CU-02 (pasos 1-4): socio ve catálogo, filtra, se inscribe.

---

## 5. Componente 5 — Mejora de Horarios (admin)

### 6a. Filtro de sede obligatorio

**Problema:** la grilla arranca en "Todas las sedes" mezclando horarios de ambas unidades → confuso.

**Diseño:** estado inicial sin sede seleccionada. Mientras `unidadFilter == null`, mostrar un empty state:

```
Seleccioná una sede para ver los horarios:
[ Gimnasio Nuevo Malvín ]   [ Espacio Mora ]
```

Al elegir una sede, se renderiza la grilla solo con los horarios de esa unidad. Se elimina la opción "Todas las sedes" del selector (o se mantiene pero no como default).

### 6b. Split simple de clases solapadas

**Problema:** si dos clases caen en el mismo día y franja horaria, hoy se pisan/superponen visualmente.

**Diseño (frontend, `HorariosPage.tsx`):**

1. Agrupar horarios por día.
2. Dentro de cada día, detectar **clusters de solapamiento**: dos horarios se solapan si `a.horaInicio < b.horaFin && b.horaInicio < a.horaFin`.
3. Para un cluster de N horarios mutuamente solapados, renderizar cada uno en una **columna de ancho `100/N %`**, posicionadas lado a lado dentro de la celda del día.
4. Cada bloque mantiene su posición vertical según hora de inicio/fin (top/height proporcional).

Algoritmo de clustering (pseudocódigo):

```
ordenar horarios del día por horaInicio
clusters = []
para cada horario h:
  si h se solapa con algún horario del último cluster:
    agregar h al último cluster
  sino:
    crear nuevo cluster con h
para cada cluster:
  N = cluster.length
  para i, horario en cluster:
    horario.width = 100/N %
    horario.left = (100/N * i) %
```

> Limitación aceptada: el clustering encadenado puede agrupar horarios que no se solapan todos entre sí (A-B solapan, B-C solapan, A-C no). Para un gym con clases en horas redondas esto es aceptable. El comportamiento exacto de Teams (offset por solapamiento parcial) queda fuera de alcance.

---

## 6. Componente 6 — Credenciales temporales por email (empleados)

### Problema

`CrearEmpleadoCommand` recibe el password del admin (`request.Password`). CA-29 pide que el sistema **envíe credenciales temporales al correo** al dar de alta.

### Diseño

- `CrearEmpleadoRequest` **pierde** el campo `Password`.
- Se **elimina** la validación `MinPasswordLength` y su constante del command (ya no recibe password del request).
- El command **autogenera** un password temporal aleatorio fuerte vía un nuevo util `GeneradorPassword.Generar()` (≥12 chars con mayúsc/minúsc/dígito/símbolo).
  - Ubicación: `GymFlow.Application.Common.GeneradorPassword` (static). Con su propio test unitario que verifica longitud y composición.
- Lo hashea con BCrypt (como hoy) y crea el empleado.
- **Inyecta `IEmailService`** en `CrearEmpleadoCommand` (constructor nuevo) y envía email con el password temporal (nueva plantilla `EmpleadoEmailTemplates.Bienvenida`, con `HtmlEncode`). Email **best-effort**: si falla, el empleado igual se crea (el fallo se loguea + se anota en la descripción de auditoría).
- Si `Email:Habilitado=false` (dev/testing), el `SmtpEmailService` loguea el envío → el password temporal queda visible en logs solo en dev.
- El frontend "Nuevo empleado" elimina el campo password.

```csharp
var passwordTemporal = GeneradorPassword.Generar();
var hash = _passwordHasher.Hash(passwordTemporal);
var empleado = new Empleado(request.Nombre, request.Apellido, request.Correo, hash, rol.Id);
// ... persistir ...
var (asunto, cuerpo) = EmpleadoEmailTemplates.Bienvenida(empleado, passwordTemporal, rol.Nombre);
var resultado = await _emailService.EnviarAsync(empleado.Correo, asunto, cuerpo);
// auditoría incluye si el email se envió o falló
```

### ⚠️ Impacto en tests existentes

`CrearEmpleadoCommandTests` se rompe con estos cambios:
- El constructor del command suma `IEmailService` → **todos los tests del archivo no compilan** hasta agregar el mock.
- El test `PasswordCorta_LanzaArgumentException` queda **obsoleto** (ya no hay password en el request) → eliminar.
- El happy path que verifica `Hash("secret123")` → cambiar para verificar que se hashea un password autogenerado (no un valor fijo) y que se llamó `EnviarAsync` una vez.
- `CrearEmpleadoRequest` sin `Password` → ajustar todos los builders de request en los tests.

### Deuda técnica anotada
- **Forzar cambio de password en primer login** no se implementa en IT4. El empleado puede cambiarlo con el flujo existente de "Cambiar password". Se agrega flag `DebeCambiarPassword` como futura mejora (IT5+).

### Criterios cubiertos
- CA-29: sistema envía credenciales temporales al correo al dar de alta.

---

## 7. Componente 7 — Tests + responsive

### Tests (cobertura objetivo)

| Use case / lógica | Tests mínimos |
|-------------------|---------------|
| `InscribirSocioCommand` | happy path, clase no existe, clase cancelada, duplicado, sin cupo→lista espera |
| `CancelarInscripcionCommand` | happy path, no es dueño, ya cancelada, **promueve lista de espera + email** |
| `GetMisInscripcionesQuery` | retorna inscripciones, **no hace N+1** (verificación anti-regresión con mock) |
| `CrearEmpleadoCommand` | autogenera password, envía email, correo duplicado |
| `InscripcionClase` (dominio) | `Cancelar`, `PromoverDeListaEspera` (válido + inválido) |

### Responsive (RNF-03)
- Verificar `CatalogoClasesPage` (nueva) en mobile/tablet/desktop.
- Verificar `HorariosPage` con split en mobile (las columnas del split deben colapsar o scrollear horizontalmente).
- Verificar `MisInscripcionesPage`.

---

## 8. Cambios consolidados al modelo de datos

| Entidad | Cambio | Migración |
|---------|--------|-----------|
| `InscripcionClase` | + campo `EsListaEspera` (bool, default false) | `AddListaEsperaInscripcion` |

> Es la única migración de schema de IT4. El resto son cambios de lógica de aplicación, no de modelo.

---

## 9. Cambios consolidados a la API

| Endpoint | Cambio |
|----------|--------|
| `POST /api/inscripciones` | Soporta lista de espera; envía email; audita |
| `DELETE /api/inscripciones/{id}` | Promueve lista de espera; envía email; audita |
| `GET /api/inscripciones/mis-inscripciones` | Indica `enListaEspera` y `posicionListaEspera` en el DTO |
| `POST /api/empleados` | Quita `password` del request; autogenera y envía por email |
| `GET /api/clases` | (sin cambio de contrato; el portal lo reutiliza para el catálogo) |

`InscripcionClaseDto` extendido (también actualizar `InscripcionMapper.ToDto` y todos sus call-sites):

| Campo nuevo | Tipo | Descripción |
|-------------|------|-------------|
| `enListaEspera` | `bool` | Si la inscripción está en lista de espera |
| `posicionListaEspera` | `int?` | Posición 1-based en la lista (null si está activa) |

---

## 10. Cambios consolidados a interfaces de repositorio

| Interfaz | Método | Acción |
|----------|--------|--------|
| `IInscripcionClaseRepository` | `GetInscripcionesActivasCountAsync(claseId)` | **Ajustar** — excluir `EsListaEspera` |
| `IInscripcionClaseRepository` | `GetPrimeroEnListaEsperaAsync(claseId)` | **Nuevo** |
| `IInscripcionClaseRepository` | `GetPosicionEnListaEsperaAsync(inscripcionId)` | **Nuevo** (1-based) |
| `IInscripcionClaseRepository` | `GetConteoActivasPorClasesAsync(claseIds)` | **Nuevo** — fix N+1 (Componente 4a) |
| `IInscripcionClaseRepository` | `GetBySocioIdAsync(socioId)` | **Verificar** — debe incluir las de lista de espera para mostrar posición en "Mis inscripciones" |
| `IClaseRepository` | `GetInscripcionesActivasCountAsync(claseId)` | **Ajustar** — excluir `EsListaEspera` |
| `IClaseRepository` | `GetInscripcionesActivasAsync(claseId)` | **NO cambia** — `CancelClaseCommand` notifica a todos, incl. lista de espera |

Cada cambio de implementación va con su actualización en `InscripcionClaseConfiguration.cs` (EF) cuando corresponda y su test de repositorio si aplica.

---

## 11. Trazabilidad a criterios de aceptación

| Criterio (CU) | Componente IT4 | Estado tras IT4 |
|---------------|----------------|-----------------|
| CA-05 (sin cupo) | 2 | ✅ Ya estaba + lista de espera |
| CA-06 (cupo decrementa) | — | ✅ Ya estaba |
| CA-07 (aparece en Mis Clases) | — | ✅ Ya estaba |
| CA-08 (notificación) | 1 | ✅ Cubierto |
| E1 (lista de espera) | 2 | ✅ Cubierto |
| E2 (duplicado) | — | ✅ Ya estaba |
| E3 (clase cancelada) | — | ✅ Ya estaba (CancelClaseCommand) |
| Filtros catálogo | 4 | ✅ Cubierto |
| CA-29 (credenciales por email) | 6 | ✅ Cubierto |
| RNF-11 (auditoría) | 1 | ✅ Cubierto |
| RNF-03 (responsive) | 7 | ✅ Verificado |

---

## 12. Fuera de alcance / deuda técnica

- **RF-14** (profesor↔clase): requiere convertir `Clase.Instructor` (string) en FK a `Empleado`. Se posterga.
- **MFA TOTP (RNF-10)** y **OAuth socios (RNF-01)**: IT5.
- **Solapamiento parcial estilo Teams** (offset escalonado): solo split simple en IT4.
- **Forzar cambio de password en primer login** del empleado: flag `DebeCambiarPassword` futuro.
- **Lista de espera con notificación a múltiples** (broadcast): solo se promueve y notifica al primero.

---

## 13. Cambio de diseño posterior: Inscripción por Horario

> **Nota (2026-06-05):** Tras completar los componentes de IT4, se decidió cambiar el modelo de inscripción de "por clase" a "por horario individual". Esto afecta los componentes 1, 2, 4 y 7 de este spec (la lógica base se mantiene, pero la FK y los conteos cambian de `ClaseId` a `HorarioClaseId`).
>
> Spec y plan del cambio: [[spec-inscripcion-por-horario]] · [[plan-inscripcion-por-horario]]
