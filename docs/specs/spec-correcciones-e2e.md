---
title: Spec — Correcciones de casos E2E no exitosos (corrida 1)
tags:
  - spec
  - testing
  - correcciones
related:
  - "[[plan-pruebas-e2e]]"
  - "[[CU-02-inscripcion-clase]]"
  - "[[CU-06-clases-horarios]]"
  - "[[CU-07-empleados-roles]]"
  - "[[CU-08-gestion-eventos]]"
---

# Spec — Correcciones de casos E2E no exitosos (corrida 1)

## 1. Contexto y alcance

La primera corrida del [[plan-pruebas-e2e|Plan de Pruebas E2E]] dejó seis casos no
exitosos. El bloque "Registro de ejecución" de esos casos anota el síntoma observado
pero no la causa; este spec la determina investigando el código real (endpoints,
servicios, notificaciones, auditoría, permisos) y define, por cada caso, el cambio
concreto para resolverlo.

**Incluye:** el arreglo funcional de los seis casos (backend y, donde corresponde,
frontend), a nivel de archivos y funciones concretas.


**Convención de commits/ramas:** el trabajo se realiza sobre `feature/correcciones-e2e`.

### 1.1 Nota sobre el estado del código

La rama de trabajo está a la par de `main`. Al investigar se encontró que la lógica de
backend de varios de estos flujos ya está implementada y es correcta; los defectos
reales son puntuales. En particular, **E2E-18 parte 1** (el envío de email/notificación
al modificar un horario) ya está resuelto en el código actual. Cada caso indica abajo
si el defecto está confirmado y presente, o ya resuelto y solo pendiente de verificación
en el despliegue.

## 2. Resumen de casos

| Caso | CU | Prioridad plan | Estado corrida | Naturaleza del defecto | Capa |
|-|-|-|-|-|-|
| E2E-07 | CU-02 | Alta | Falló | Falta validación de cuota vencida en la inscripción | Backend |
| E2E-05 | CU-02 | Crítica | Falló | Borrado de horario sin guarda deja inscripciones huérfanas | Backend |
| E2E-18 | CU-06 | Alta | Falló | (1) email/notif ya resuelto · (2) falta validar horario de apertura | Backend + Front |
| E2E-19 | CU-06 | Crítica | Pasó parcial | El portal lista horarios de clases canceladas | Backend |
| E2E-21 | CU-07 | Crítica | Pasó parcial | Roles no-Admin pueden asignar el rol Admin | Backend |
| E2E-24 | CU-08 | Alta | Pasó parcial | El mensaje de fecha pasada filtra "(Parameter 'request')" | Backend |

**Orden de implementación sugerido:** E2E-07 → E2E-05 → E2E-18(2) → E2E-19 → E2E-21 →
E2E-24. Los tres primeros corresponden a los casos que fallaron; el resto son
pasó-parcial de menor riesgo.

## 3. Detalle por caso — Prioridad 1 (fallaron)

### 3.1 E2E-07 — Inscripción con cuota vencida no se bloquea

**Trazabilidad:** CU-02 E4 — RF-10, RN-09.

**Comportamiento actual observado.** Un socio con cuota vencida en la sede logra
inscribirse a un horario de esa sede. (Registro: "El socio se puede inscribir a una
clase con cuotas vencidas".)

**Comportamiento esperado.** El intento se rechaza con el mensaje "No podés inscribirte
con cuota vencida en esta sede". No se crea inscripción ni se altera el cupo. Los otros
dos sub-casos de E2E-07 (cupo lleno → botón deshabilitado; inscripción duplicada → "Ya
estás inscripto en este horario") ya funcionan y no se tocan.

**Causa raíz.** `InscribirSocioCommand.ExecuteAsync`
(`backend/src/GymFlow.Application/UseCases/Inscripciones/InscribirSocioCommand.cs`)
valida clase activa (`!clase.EstaActivo`), inscripción duplicada
(`GetActivaBySocioYHorarioAsync`) y cupo (`GetInscripcionesActivasCountAsync` vs
`CapacidadMaxima`), pero **no valida el estado de cuota del socio**. Un `grep` sobre
`backend/src` confirma que no existe ninguna validación de cuota vencida en el flujo de
inscripción (ni en el command ni en `InscripcionesController`).

**Cambio propuesto.**

1. Agregar a `ICuotaRepository`
   (`backend/src/GymFlow.Application/Interfaces/ICuotaRepository.cs`) y a su
   implementación `CuotaRepository`
   (`backend/src/GymFlow.Infrastructure/Repositories/CuotaRepository.cs`) el método:
   `Task<bool> TieneCuotaVencidaAsync(Guid socioId, Guid unidadId, DateTime hoy)`,
   que devuelve `true` si existe alguna cuota del socio en esa unidad con
   `Estado == EstadoCuota.Pendiente` y `FechaVencimiento < hoy`.
   (Alternativa sin método nuevo: reutilizar
   `SearchAsync(socioId, EstadoCuota.Pendiente, null, null, unidadId)` y filtrar por
   `FechaVencimiento < hoy` en el command; se prefiere el método dedicado por claridad
   y por consultar solo lo necesario.)
2. Inyectar `ICuotaRepository` en `InscribirSocioCommand` y, tras resolver
   `clase = horario.Clase` y antes de crear la `InscripcionClase`, invocar
   `TieneCuotaVencidaAsync(socioId, clase.UnidadId, DateTime.UtcNow)`. Si devuelve
   `true`, lanzar
   `new InvalidOperationException("No podés inscribirte con cuota vencida en esta sede.")`.
3. Sin cambios adicionales de wiring: `InscripcionesController.Inscribirse` ya mapea
   `InvalidOperationException` a `409 Conflict` con `{ error = ex.Message }`, y
   `HorariosPortalPage` ya muestra ese mensaje en `actionError`. Como la excepción se
   lanza antes de `AddAsync`/`SaveChangesAsync`, no se crea inscripción ni se descuenta
   cupo.

**Nota de ubicación de la regla.** La validación vive en el command (capa de
aplicación), consistente con las otras validaciones de inscripción. Opcionalmente puede
extraerse un helper de dominio `Cuota.EstaVencida(DateTime hoy) => Estado ==
EstadoCuota.Pendiente && FechaVencimiento < hoy` para reutilizar la definición de
"vencida", pero no es obligatorio para el arreglo.

### 3.2 E2E-05 — Borrado de horario con inscriptos deja estado inconsistente

**Trazabilidad:** CU-02 flujo principal / CU-06 — RF-09, RF-10, RF-11, RF-16, RNF-11.

**Comportamiento actual observado.** El admin borra un horario desde la interfaz y el
socio sigue viendo ese horario en el portal aunque ya no pueda inscribirse; además el
socio pierde su inscripción sin aviso. (Registro: "El socio se puede inscribir a /
sigue viendo clases que no tienen horario registrado".)

**Comportamiento esperado.** Al borrar un horario que tiene inscriptos activos, el
sistema cancela esas inscripciones y avisa a los socios afectados (email +
notificación in-system), y recién después elimina el horario. El socio deja de ver el
horario y queda notificado de la baja. La operación queda auditada con el resultado del
envío.

**Causa raíz.** `DeleteHorarioCommand.ExecuteAsync`
(`backend/src/GymFlow.Application/UseCases/Horarios/DeleteHorarioCommand.cs`) hace
`_horarioRepo.Remove(horario)` + `SaveChangesAsync` **sin verificar ni manejar las
inscripciones activas**. La FK `InscripcionClase → HorarioClase` está configurada como
`OnDelete(DeleteBehavior.Cascade)`
(`backend/src/GymFlow.Infrastructure/Persistence/Configurations/InscripcionClaseConfiguration.cs:19`),
por lo que el borrado elimina en cascada, en silencio, las inscripciones activas: el
socio pierde su inscripción sin notificación, y su vista puede quedar mostrando un
horario que ya no existe. `DeleteHorarioCommand` solo depende de
`IHorarioClaseRepository` e `IAuditLogger`; no tiene acceso a inscripciones, email ni
notificador.

**Cambio propuesto** (decisión acordada: **cancelar + notificar y luego borrar**).

1. Ampliar las dependencias de `DeleteHorarioCommand` para incluir
   `IInscripcionClaseRepository`, `IEmailService` e `INotificadorInApp` (mismo conjunto
   que ya usa `CancelClaseCommand`/`UpdateHorarioCommand`).
2. En `ExecuteAsync`, antes de eliminar:
   - obtener `inscripciones = await _inscripcionRepo.GetActivasByHorarioClaseIdAsync(id)`;
   - para cada inscripción, `inscripcion.Cancelar()` (soft-cancel, `EstaActiva = false`);
   - enviar email a cada socio inscripto y crear la notificación in-app, reutilizando el
     patrón de `CancelClaseCommand` (email vía plantilla; notificación in-app
     best-effort en `try/catch`). Para el texto se reutiliza
     `TipoNotificacion.CancelacionClase` con un mensaje específico de eliminación de
     horario ("El horario de la clase {Nombre} ({día} {hora}) fue eliminado; tu
     inscripción quedó sin efecto"). Reutilizar el tipo existente evita agregar un valor
     nuevo al enum y su render en el front; si el equipo prefiere granularidad, puede
     añadirse `TipoNotificacion.EliminacionHorario`, pero no es necesario para el
     arreglo.
   - persistir las cancelaciones (`SaveChangesAsync` sobre el repo de inscripciones o el
     contexto compartido) **antes** de remover el horario, para que la cascade no
     interfiera con el conteo de envíos.
3. Registrar en auditoría el resultado del envío (enviados / fallidos), con el mismo
   formato de detalle que `CancelClaseCommand`.
4. Recién entonces `Remove(horario)` + `SaveChangesAsync`.
5. Si no hay inscripciones activas, el comportamiento es el actual (borra y audita) sin
   enviar nada.

**Nota de frontend.** El portal del socio usa React Query; tras el borrado, la lista de
`/portal/horarios` y `/portal/mis-inscripciones` debe reflejar el cambio en el próximo
refetch/polling. Con la inscripción cancelada en backend, ambas vistas dejan de mostrar
el horario. No se requiere cambio de front específico más allá de confirmar que las
queries afectadas se invalidan/refrescan.

### 3.3 E2E-18 — Modificación de horario notifica a los inscriptos

**Trazabilidad:** CU-06 modificación de horario — RF-09, RF-16, RNF-11.

Este caso tiene dos hallazgos independientes.

#### Parte 1 — Email/notificación al modificar horario: YA RESUELTO

**Registro:** "El correo/notificación no se envió al modificar los horarios".

**Estado en el código actual.** `UpdateHorarioCommand.ExecuteAsync`
(`backend/src/GymFlow.Application/UseCases/Horarios/UpdateHorarioCommand.cs`) **sí**
envía email a cada inscripto (`HorarioEmailTemplates.CambioHorario` +
`_emailService.EnviarAsync`) y crea la notificación in-app
(`_notificador.CrearParaVariosAsync` con `TipoNotificacion.CambioHorario`) cuando hay
inscripciones activas, y audita el resultado del envío. Se verificó además que:
- el admin edita el horario vía `PUT /api/horarios/{id}` (`horariosApi.update` en
  `frontend/src/services/api.ts`), que mapea a este command;
- `GetActivasByHorarioClaseIdAsync` filtra por `EstaActiva` e incluye `Socio`, por lo
  que el conteo y los correos se resuelven correctamente.

**Conclusión.** El defecto no es reproducible en el código actual; corresponde a una
versión anterior a la desplegada al momento del test, o a un entorno con
`Email:Habilitado = false` (`SmtpEmailService` solo loguea cuando el flag está en
`false`). **Acción:** ninguna de código; verificar en el despliegue que
`Email:Habilitado = true` y sus credenciales SMTP, y re-ejecutar el caso. Documentar el
resultado en el registro de ejecución.

#### Parte 2 — Falta validación de horario de apertura del gimnasio

**Registro:** "Si le asignamos a una clase un horario fuera del horario de apertura la
clase desaparece o se ve corrida; el sistema no debería permitir seleccionar dichos
horarios (el gym no está abierto)".

**Comportamiento actual observado.** Se pueden crear/editar horarios fuera del horario
de apertura del gimnasio. La grilla semanal del admin dibuja un rango de horas fijo y
los horarios fuera de ese rango se ven corridos o desaparecen.

**Comportamiento esperado.** El gimnasio abre de **07:00 a 22:00**. Un horario cuyo
inicio sea anterior a las 07:00 o cuyo fin sea posterior a las 22:00 se rechaza con un
mensaje de validación claro; la grilla del admin se alinea a ese rango.

**Causa raíz.** `HorarioClase` (`backend/src/GymFlow.Domain/Entities/HorarioClase.cs`)
solo valida `horaFin > horaInicio` en el ctor y en `Actualizar`; no existe ninguna
validación de rango de apertura. La grilla del front
(`frontend/src/pages/admin/HorariosPage.tsx`) usa un `gridStartHour` y una lista `HORAS`
fijos, por lo que las horas fuera de rango se posicionan mal.

**Cambio propuesto.**

1. Definir el rango de apertura como constante de dominio, p. ej.
   `backend/src/GymFlow.Domain/Constants/HorarioApertura.cs` con
   `Apertura = new TimeOnly(7, 0)` y `Cierre = new TimeOnly(22, 0)`.
2. En `HorarioClase` (ctor y `Actualizar`), tras validar `horaFin > horaInicio`,
   validar `horaInicio >= HorarioApertura.Apertura && horaFin <= HorarioApertura.Cierre`;
   si no, lanzar
   `ArgumentException("El horario debe estar dentro del horario de apertura (07:00 a 22:00).")`.
   Al vivir en el dominio, la regla cubre las tres vías (`CreateHorarioCommand`,
   `UpdateHorarioCommand` y cualquier alta futura) sin duplicarla.
   `HorariosController` ya mapea `ArgumentException` a `400 BadRequest` con el mensaje.
3. En `frontend/src/pages/admin/HorariosPage.tsx`, alinear `gridStartHour` / `HORAS` al
   rango 07:00–22:00 (y, si aplica, restringir los selectores de hora del formulario de
   alta/edición al mismo rango) para que la grilla no se vea corrida y no se ofrezcan
   horas inválidas.

**Nota.** El rango se deja como constante de dominio (valor 07:00–22:00). Si más
adelante se necesita por unidad o configurable por entorno, se puede mover a
configuración sin cambiar los puntos de validación.

## 4. Detalle por caso — Prioridad 2 (pasaron parcial)

### 4.1 E2E-19 — El portal lista horarios de clases canceladas

**Trazabilidad:** CU-06 cancelación y reactivación — RF-08, RF-16, RNF-11.

**Comportamiento actual observado.** Tras cancelar una clase, sus horarios siguen
apareciendo en `/portal/horarios` y se ofrecen para inscribirse (aunque el intento
falla). (Registro: "Se podría mejorar que al cancelar una clase ya no figure para
inscribirse a los socios".)

**Comportamiento esperado.** Al cancelar la clase, sus horarios dejan de figurar en el
portal para inscripción. Al reactivar la clase, los horarios vuelven a estar
disponibles (consistente con el resto del esperado de E2E-19, que ya funciona: la
cancelación cancela inscripciones y notifica, y la reactivación no restaura las
inscripciones previas).

**Causa raíz.** `HorarioClaseRepository.GetAllAsync`
(`backend/src/GymFlow.Infrastructure/Repositories/HorarioClaseRepository.cs`), que
alimenta el portal vía `GetHorariosQuery`, **no filtra por `clase.EstaActivo`**. En
contraste, `GetByDiaAsync` en el mismo repositorio sí incluye
`.Where(h => ... && h.Clase.EstaActivo)`. La inconsistencia hace que los horarios de
clases canceladas se sigan listando.

**Cambio propuesto.**

1. En `GetAllAsync`, agregar el filtro `.Where(h => h.Clase.EstaActivo)` a la query,
   igualando el criterio de `GetByDiaAsync`. Como `CancelClaseCommand` pone
   `EstaActivo = false` y `ReactivarClaseCommand` lo vuelve a `true`, la
   cancelación/reactivación se refleja automáticamente en el listado.
2. Verificar que la grilla del admin (`GET /api/horarios` también la usa) mantenga el
   comportamiento deseado: si el admin necesita ver/gestionar horarios de clases
   canceladas, considerar un parámetro opcional `incluirInactivas` para la vista admin;
   si no lo necesita (las clases canceladas se gestionan desde el módulo de clases), el
   filtro simple alcanza. **Decisión por defecto:** aplicar el filtro para ambos
   consumidores (portal y grilla admin), ya que una clase cancelada no debería
   presentar horarios activos en ninguna de las dos vistas; sus horarios reaparecen al
   reactivar la clase.

### 4.2 E2E-21 — Roles no-Admin pueden asignar el rol Admin

**Trazabilidad:** CU-07 rol Admin + E4 — RNF-01, RNF-11.

**Comportamiento actual observado.** La regla "solo el Admin asigna el rol Dueño" está
implementada y funciona. Sin embargo, un empleado no-Admin con permiso
Empleados-Escritura/Modificación puede crear o editar un empleado asignándole el **rol
Admin**. (Registro: "Se podría mejorar que los otros roles no puedan crear usuarios con
rol admin".)

**Comportamiento esperado.** Solo un Admin puede asignar el rol Admin (igual que con el
rol Dueño). Cualquier intento de un actuante no-Admin de asignar el rol Admin se rechaza
(403).

**Causa raíz.** `AsignacionRolEmpleado`
(`backend/src/GymFlow.Application/UseCases/Empleados/AsignacionRolEmpleado.cs`) solo
guarda `RolesSeed.DuenoRolId`:

- `ValidarAsignacion` (líneas 26-28) y `ValidarSoloAdminAsignaDueno` (líneas 47-51)
  lanzan `UnauthorizedAccessException` solo cuando `rolAsignadoId == DuenoRolId &&
  actuanteRolId != AdminRolId`. **No hay ninguna guarda equivalente para
  `RolesSeed.AdminRolId`.**

El wiring de identidad ya es correcto: `EmpleadosController` obtiene `actuanteRolId` del
claim `rolId` del JWT (`GetActuanteRolId`) y lo pasa a `Crear`/`Actualizar`/`Reactivar`,
que invocan estos validadores. Solo falta la regla para el rol Admin.

**Cambio propuesto.**

1. En `AsignacionRolEmpleado.ValidarAsignacion`, agregar la guarda simétrica:
   `if (rolAsignadoId == RolesSeed.AdminRolId && actuanteRolId != RolesSeed.AdminRolId)`
   `throw new UnauthorizedAccessException("Solo el administrador puede asignar el rol Admin.");`
2. Agregar la misma guarda en `ValidarSoloAdminAsignaDueno` (usado por la reactivación),
   para cerrar también esa vía. (Opcional: renombrar el método a
   `ValidarSoloAdminAsignaRolPrivilegiado` para reflejar que ahora cubre Dueño y Admin;
   no es imprescindible.)
3. Sin cambios en el controller: `EmpleadosController` ya mapea
   `UnauthorizedAccessException` a `403 Forbidden` en `Create`, `Update` y `Reactivar`.

### 4.3 E2E-24 — El error de fecha pasada filtra "(Parameter 'request')"

**Trazabilidad:** CU-08 flujo principal — RF-15, RF-16, RNF-11.

**Comportamiento actual observado.** Al intentar crear un evento con fecha pasada, la
validación bloquea correctamente, pero el mensaje mostrado al usuario incluye plomería
de .NET: "La fecha del evento no puede ser pasada. (Parameter 'request')". (Registro:
"En el error de evento con fecha pasada muestra: (Parameter 'request')".) El resto del
caso E2E-24 (evento creado, auditado, email + notificación in-app "Evento nuevo",
visible en "Próximos eventos") funciona.

**Comportamiento esperado.** El mensaje de validación es limpio y orientado al usuario,
sin el sufijo "(Parameter 'request')".

**Causa raíz.** `CrearEventoCommand.ExecuteAsync`
(`backend/src/GymFlow.Application/UseCases/Eventos/CrearEventoCommand.cs:41-42`):

```csharp
if (fechaUtc < DateTime.UtcNow)
    throw new ArgumentException("La fecha del evento no puede ser pasada.", nameof(request));
```

Al pasar `nameof(request)` como `paramName`, `ArgumentException.Message` agrega
automáticamente el sufijo "(Parameter 'request')". `EventosController` devuelve
`ex.Message` crudo en el `400 BadRequest`, por lo que el sufijo llega al usuario.

**Cambio propuesto.**

1. Quitar el segundo argumento del `throw`: usar
   `throw new ArgumentException("La fecha del evento no puede ser pasada.")` (sin
   `paramName`), de modo que `.Message` sea exactamente el texto deseado.
   (Alternativa preferible a mediano plazo: introducir una excepción de validación de
   dominio propia —p. ej. `ValidacionException`— mapeada a 400 en el controller, para no
   depender de la semántica de `paramName` de `ArgumentException`; para este arreglo
   alcanza con quitar el `nameof`.)
2. Barrer el módulo Eventos por el mismo patrón y limpiarlo donde aplique a mensajes de
   cara al usuario:
   - el ctor de `Evento` (`backend/src/GymFlow.Domain/Entities/Evento.cs`) —validación
     de título vacío;
   - `ActualizarEventoCommand` —si repite la validación de fecha con `nameof`.
   Mantener `paramName` solo en validaciones que nunca se muestran al usuario (errores de
   programación), no en las de reglas de negocio que el front expone.

## 5. Verificación (manual, post-implementación)

Cada caso se re-ejecuta según su guion en la sección 5 del [[plan-pruebas-e2e]]; se
registra el resultado en el "Registro de ejecución" del documento oficial. Criterios de
cierre por caso:

- **E2E-07:** el socio con cuota vencida ve el rechazo "No podés inscribirte con cuota
  vencida en esta sede"; no se crea inscripción ni cambia el cupo.
- **E2E-05:** borrar un horario con inscriptos cancela sus inscripciones, envía email +
  notificación a los afectados, audita el envío y elimina el horario; el socio deja de
  verlo.
- **E2E-18:** (1) verificado en despliegue que llega email + notificación "Cambio de
  horario"; (2) no se permiten horarios fuera de 07:00–22:00 y la grilla se ve alineada.
- **E2E-19:** al cancelar la clase, sus horarios ya no figuran en el portal; al
  reactivarla, reaparecen.
- **E2E-21:** un empleado no-Admin no puede asignar el rol Admin (403); el Admin sí.
- **E2E-24:** el mensaje de fecha pasada se muestra sin "(Parameter 'request')".

## 6. Resumen de archivos afectados

| Archivo | Caso | Tipo de cambio |
|-|-|-|
| `Application/Interfaces/ICuotaRepository.cs` | E2E-07 | Nuevo método `TieneCuotaVencidaAsync` |
| `Infrastructure/Repositories/CuotaRepository.cs` | E2E-07 | Implementación del método |
| `Application/UseCases/Inscripciones/InscribirSocioCommand.cs` | E2E-07 | Inyección + validación de cuota vencida |
| `Application/UseCases/Horarios/DeleteHorarioCommand.cs` | E2E-05 | Cancelar + notificar inscriptos antes de borrar |
| `Domain/Constants/HorarioApertura.cs` (nuevo) | E2E-18(2) | Constante de rango de apertura |
| `Domain/Entities/HorarioClase.cs` | E2E-18(2) | Validación de rango de apertura |
| `frontend/src/pages/admin/HorariosPage.tsx` | E2E-18(2) | Grilla/selectores alineados a 07:00–22:00 |
| `Infrastructure/Repositories/HorarioClaseRepository.cs` | E2E-19 | Filtro `EstaActivo` en `GetAllAsync` |
| `Application/UseCases/Empleados/AsignacionRolEmpleado.cs` | E2E-21 | Guarda para `AdminRolId` |
| `Application/UseCases/Eventos/CrearEventoCommand.cs` | E2E-24 | Quitar `nameof(request)` del mensaje |
| `Domain/Entities/Evento.cs`, `Application/UseCases/Eventos/ActualizarEventoCommand.cs` | E2E-24 | Barrido del mismo patrón |
