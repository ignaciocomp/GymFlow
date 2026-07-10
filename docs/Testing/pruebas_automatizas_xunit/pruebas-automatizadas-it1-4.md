---
title: Inventario de Pruebas Automatizadas (xUnit) — Iteraciones 1 a 4
tags:
  - seguimiento
  - testing
related:
  - "[[seguimiento_index]]"
  - "[[iteracion-3]]"
  - "[[iteracion-4]]"
---

# Inventario de Pruebas Automatizadas (xUnit) — Iteraciones 1 a 4

> Documento de inventario generado a partir de `backend/tests/**`. Reúne las pruebas
> automatizadas hechas en código (xUnit) y las mapea, lo mejor posible, a las
> iteraciones 1 a 4 de la Fase de Construcción. Pensado para pegarse en la
> documentación formal de cierre de cada iteración (Issue #48).

## Resumen general

El backend cuenta con **tres proyectos de pruebas automatizadas** ubicados en
`backend/tests/`, todos sobre **xUnit 2.5.3 + Moq 4.20** (y
`Microsoft.EntityFrameworkCore.InMemory` 8.0.11 para las pruebas que necesitan un
`DbContext` en memoria). Se ejecutan con `dotnet test` desde `backend/`.

| Proyecto | Capa cubierta | Archivos de test | Casos `[Fact]`/`[Theory]` (aprox.) |
|-|-|-|-|
| `GymFlow.Domain.Tests` | Dominio (entidades y reglas de negocio puras) | 13 | ~96 |
| `GymFlow.Application.Tests` | Application (commands, queries, autorización, controllers) | 53 | ~225 |
| `GymFlow.Infrastructure.Tests` | Infraestructura (servicios, persistencia, seguridad) | 9 | ~32 |
| **Total** | | **75** | **~353** |

> Los conteos de casos son aproximados: se obtienen contando atributos `[Fact]` y
> `[Theory]` por archivo. Un `[Theory]` con varios `[InlineData]` ejecuta más casos
> de los que se cuentan acá, por lo que el número real de aserciones ejecutadas es
> mayor.

### Stack de testing

- **Framework:** xUnit 2.5.3 (`xunit`, `xunit.runner.visualstudio`).
- **Mocking:** Moq (4.20.70 / 4.20.72).
- **Persistencia en memoria:** `Microsoft.EntityFrameworkCore.InMemory` 8.0.11 (para queries/commands que tocan `DbContext`).
- **Runner/SDK:** `Microsoft.NET.Test.Sdk` 17.8.0 + `coverlet.collector` 6.0.0 (cobertura).
- **Ejecución:** `cd backend && dotnet test`.

### Nota de alcance sobre el mapeo a iteraciones

El mapeo iteración → RF sigue el Plan de Iteraciones de
`docs/GymFlow_Requerimientos_Completos.md` (sección 9). Como la suite de pruebas
crece de forma acumulativa, algunas clases de test corresponden a funcionalidades
posteriores a la iteración 4 (MFA, login con Google, eventos, notificaciones in-app,
rol Dueño y los jobs de cuotas), implementadas en la **Iteración 5**. Esas pruebas se
listan aparte, en la sección [Pruebas posteriores a la iteración 4](#pruebas-posteriores-a-la-iteración-4-iteración-5), para no atribuirlas
incorrectamente a las iteraciones 1-4.

---

## Iteración 1 — Base del Sistema, Multi-Espacio y Gestión Inicial de Socios

**RF cubiertos:** RF-01 a RF-04 (CRUD de socios), RF-20 (multi-espacio), RF-22 (plan por unidad), RNF-08 (arquitectura multi-unidad), RNF-11 (auditoría base).

### Dominio (`GymFlow.Domain.Tests`)

| Clase de test               | Entidad / área                                                                        | Casos (aprox.) |
| --------------------------- | ------------------------------------------------------------------------------------- | -------------- |
| `Entities/SocioTests.cs`    | `Socio`: validación de datos, cédula uruguaya, consentimiento Ley 18.331, baja lógica | ~11            |
| `Entities/UnidadTests.cs`   | `Unidad`: creación y validaciones de la unidad de negocio                             | ~4             |
| `Entities/UsuarioTests.cs`  | `Usuario` (base TPH): atributos comunes y estado                                      | ~6             |
| `Entities/EmpleadoTests.cs` | `Empleado` (subclase de `Usuario`): datos y password hash                             | ~5             |

### Application (`GymFlow.Application.Tests`)

| Clase de test | Caso de uso / área | Casos (aprox.) |
|-|-|-|
| `UseCases/CreateSocioCommandTests.cs` | Alta de socio (RF-01): validaciones, duplicados, consentimiento | ~4 |
| `UseCases/Socios/GetSociosQueryTests.cs` | Listado de socios con filtros (RF-02) | ~2 |
| `UseCases/GetUnidadesQueryTests.cs` | Listado de unidades / multi-espacio (RF-20) | ~2 |

**Subtotal aproximado It-1:** ~34 casos (dominio ~26 + application ~8).

---

## Iteración 2 — Portal Básico del Socio, Cuotas, Roles y Auditoría

**RF cubiertos:** RF-05 (perfil del socio), RF-06 (recordatorios de cuota), RF-07 (control de estado de cuota), RNF-01 (autenticación/autorización por roles — parte interna: empleados + roles dinámicos), RNF-11 (auditoría).

### Dominio (`GymFlow.Domain.Tests`)

| Clase de test | Entidad / área | Casos (aprox.) |
|-|-|-|
| `Entities/CuotaTests.cs` | `Cuota`: vencimiento, pago, anulación, cálculo de estado | ~11 |
| `Entities/RolTests.cs` | `Rol`: creación de roles, asignación de permisos | ~7 |
| `Entities/RegistroAuditoriaTests.cs` | `RegistroAuditoria`: registro de auditoría (RNF-11) | ~5 |

### Application (`GymFlow.Application.Tests`)

| Clase de test | Caso de uso / área | Casos (aprox.) |
|-|-|-|
| `Authorization/RequierePermisoAttributeTests.cs` | Filtro `[RequierePermiso]` (RNF-01): control de acceso por módulo/operación | ~4 |
| `UseCases/AuditLoggingTests.cs` | Registro transversal de auditoría (RNF-11) | ~4 |
| `UseCases/Roles/CrearRolCommandTests.cs` | Crear rol con permisos | ~3 |
| `UseCases/Roles/ActualizarRolCommandTests.cs` | Actualizar rol y permisos | ~4 |
| `UseCases/Roles/EliminarRolCommandTests.cs` | Eliminar rol (bloqueo si tiene usuarios) | ~4 |
| `UseCases/Cuotas/MarcarCuotaPagadaCommandTests.cs` | Marcar cuota como pagada (RF-07) | ~5 |
| `UseCases/Cuotas/RevertirPagoCuotaCommandTests.cs` | Revertir pago de cuota | ~2 |
| `UseCases/Cuotas/AnularCuotaCommandTests.cs` | Anular cuota | ~3 |
| `UseCases/Cuotas/RevertirAnulacionCuotaCommandTests.cs` | Revertir anulación de cuota | ~2 |
| `UseCases/Cuotas/GenerarCuotasCommandTests.cs` | Generación de cuotas periódicas (RF-07) | ~5 |
| `UseCases/Cuotas/GetCuotasAdminQueryTests.cs` | Listado de cuotas para admin con filtros de estado | ~6 |
| `UseCases/Cuotas/GetCuotasBySocioQueryTests.cs` | Cuotas de un socio | ~2 |
| `UseCases/Cuotas/GetSociosConEstadoCuotaQueryTests.cs` | Socios con estado de cuota (al día / pendiente / vencido) | ~8 |
| `UseCases/Portal/GetSocioPerfilQueryTests.cs` | Ver perfil del socio (RF-05) | ~2 |
| `UseCases/Portal/SolicitarModificacionCommandTests.cs` | Socio solicita modificación de datos (aprobación manual) | ~3 |
| `UseCases/Portal/SolicitarBajaCommandTests.cs` | Socio solicita baja | ~3 |

**Subtotal aproximado It-2:** ~85 casos (dominio ~23 + application ~62).

> Nota: los recordatorios automáticos de cuota (RF-06) y las notificaciones de cuota
> tienen pruebas en `UseCases/Cuotas/ProcesarRecordatoriosCommandTests.cs` y
> `UseCases/Cuotas/NotificarCuotaCommandTests.cs`, pero su forma actual (jobs
> disparables + notificaciones in-app) se consolidó en la Iteración 5; ver la sección
> final. El estado de cuota base sí corresponde a la Iteración 2.

---

## Iteración 3 — Gestión de Clases y Horarios

**RF cubiertos:** RF-08 (gestión de clases), RF-09 (gestión de horarios semanales), RF-10/RF-11 (inscripción base, formalizada en It-4), RNF-11 (auditoría).

### Dominio (`GymFlow.Domain.Tests`)

| Clase de test | Entidad / área | Casos (aprox.) |
|-|-|-|
| `Entities/ClaseTests.cs` | `Clase`: validaciones, capacidad, baja lógica, reactivación | ~13 |
| `Entities/HorarioClaseTests.cs` | `HorarioClase`: día/hora, rango válido, conflicto de sala | ~14 |
| `Entities/InscripcionClaseTests.cs` | `InscripcionClase`: estado activa/cancelada (base) | ~3 |

### Application (`GymFlow.Application.Tests`)

| Clase de test | Caso de uso / área | Casos (aprox.) |
|-|-|-|
| `UseCases/Clases/CreateClaseCommandTests.cs` | Crear clase (RF-08) | ~2 |
| `UseCases/Clases/UpdateClaseCommandTests.cs` | Editar clase, validación capacidad vs inscripciones | ~3 |
| `UseCases/Clases/CancelClaseCommandTests.cs` | Cancelar clase, cancelar inscripciones, notificar | ~8 |
| `UseCases/Clases/ReactivarClaseCommandTests.cs` | Reactivar clase cancelada | ~3 |
| `UseCases/Clases/GetClasesQueryTests.cs` | Listado de clases con filtros | ~3 |
| `UseCases/Horarios/CreateHorarioCommandTests.cs` | Crear horario con validación de conflicto de sala (RF-09) | ~5 |
| `UseCases/Horarios/UpdateHorarioCommandTests.cs` | Editar horario, revalidar conflicto, notificar inscriptos | ~7 |
| `UseCases/Horarios/DeleteHorarioCommandTests.cs` | Eliminar horario | ~2 |
| `UseCases/Horarios/GetHorariosQueryTests.cs` | Listado de horarios con filtros | ~2 |

**Subtotal aproximado It-3:** ~57 casos (dominio ~30 + application ~27).

---

## Iteración 4 — Inscripción a Clases (por horario), Empleados y Profesores

**RF cubiertos:** RF-10 (inscripción por horario con cupo + cuota), RF-11 (ver mis clases), RF-12 (gestión de empleados/profesores), RF-13/RF-14 (cubiertos por roles y permisos configurables), RNF-03/RNF-05/RNF-11.

### Application (`GymFlow.Application.Tests`)

| Clase de test | Caso de uso / área | Casos (aprox.) |
|-|-|-|
| `UseCases/Inscripciones/InscribirSocioCommandTests.cs` | Inscribir a un horario (RF-10): cupo, duplicados RN-09, cuota al día, clase activa | ~4 |
| `UseCases/Inscripciones/CancelarInscripcionCommandTests.cs` | Cancelar inscripción, liberar cupo | ~2 |
| `UseCases/Inscripciones/GetMisInscripcionesQueryTests.cs` | "Mis Inscripciones" (RF-11): conteo batch sin N+1 | ~1 |
| `UseCases/Empleados/CrearEmpleadoCommandTests.cs` | Alta de empleado con credenciales temporales autogeneradas + email (RF-12) | ~9 |
| `UseCases/Empleados/ActualizarEmpleadoCommandTests.cs` | Editar empleado | ~6 |
| `UseCases/Empleados/CambiarPasswordCommandTests.cs` | Cambio de password de empleado | ~3 |
| `UseCases/Empleados/DarDeBajaEmpleadoCommandTests.cs` | Baja lógica de empleado | ~3 |
| `UseCases/Empleados/ReactivarEmpleadoCommandTests.cs` | Reactivar empleado | ~2 |
| `UseCases/Empleados/GetEmpleadosQueryTests.cs` | Listado de empleados | ~2 |
| `Common/GeneradorPasswordTests.cs` | `GeneradorPassword`: contraseñas temporales seguras (mayús/minús/números/especiales) | ~2 |

**Subtotal aproximado It-4:** ~34 casos (todos en Application).

> RF-13 y RF-14 no tienen clases de test propias: quedan cubiertos por el sistema de
> roles y permisos (probado en It-2 con `RequierePermisoAttributeTests` y los commands
> de `Roles/`), ya que el profesor se modela como un rol configurable con permisos por
> módulo, no como una relación fija profesor-clase.

---

## Pruebas posteriores a la iteración 4 (Iteración 5)

Estas pruebas existen en la suite pero corresponden a funcionalidades implementadas
**después** de la iteración 4 (Iteración 5: MFA TOTP, login con Google, eventos,
notificaciones in-app, rol Dueño y jobs de cuotas). Se listan para que el inventario de
`backend/tests/**` quede completo y para no atribuirlas a las iteraciones 1-4.

### Dominio (`GymFlow.Domain.Tests`)

| Clase de test | Área | Casos (aprox.) |
|-|-|-|
| `Entities/EmpleadoMfaTests.cs` | MFA del empleado (TOTP, códigos de recuperación) | ~5 |
| `Entities/EventoTests.cs` | `Evento` (RF-15) | ~7 |
| `Entities/NotificacionTests.cs` | `Notificacion` in-app (RF-16) | ~5 |

### Application (`GymFlow.Application.Tests`)

| Clase de test | Área | Casos (aprox.) |
|-|-|-|
| `UseCases/Auth/LoginConGoogleCommandTests.cs` | Login de socios con Google OAuth | ~8 |
| `UseCases/Auth/Mfa/IniciarMfaSetupCommandTests.cs` | Setup de MFA | ~2 |
| `UseCases/Auth/Mfa/ActivarMfaCommandTests.cs` | Activar MFA | ~2 |
| `UseCases/Auth/Mfa/VerificarMfaCommandTests.cs` | Verificar código MFA | ~4 |
| `UseCases/Auth/Mfa/UsarCodigoRecuperacionCommandTests.cs` | Usar código de recuperación MFA | ~3 |
| `UseCases/Auth/Mfa/ResetearMfaEmpleadoCommandTests.cs` | Reset de MFA de empleado | ~2 |
| `UseCases/Eventos/*` (Crear, Actualizar, Cancelar, Notificar, GetById, GetEventos, GetEventosPortal) | Gestión de eventos (RF-15) | ~22 |
| `UseCases/Notificaciones/NotificacionesPortalTests.cs` | Notificaciones in-app del portal (RF-16) | ~10 |
| `UseCases/Cuotas/NotificarCuotaCommandTests.cs` | Notificación manual de cuota | ~12 |
| `UseCases/Cuotas/ProcesarRecordatoriosCommandTests.cs` | Job de recordatorios automáticos de cuota (RF-06) | ~12 |
| `Controllers/CuotasControllerJobsTests.cs` | Endpoints manuales para disparar jobs de cuotas | ~6 |
| `Controllers/EventosControllerNotificarTests.cs` | Endpoint de notificación de eventos | ~5 |

### Infraestructura (`GymFlow.Infrastructure.Tests`)

| Clase de test | Área | Casos (aprox.) |
|-|-|-|
| `Services/TotpServiceTests.cs` | Generación/validación TOTP | ~5 |
| `Services/MfaTokenServiceTests.cs` | Tokens de MFA | ~4 |
| `Services/AesGcmMfaSecretProtectorTests.cs` | Cifrado AES-GCM del secreto MFA | ~3 |
| `Services/GoogleIdTokenValidatorTests.cs` | Validación de ID token de Google | ~3 |
| `Services/NotificadorInAppTests.cs` | Notificador in-app | ~3 |
| `Services/CuotaGeneradorServiceTests.cs` | Servicio generador de cuotas | ~4 |
| `Services/UnidadesVisiblesResolverTests.cs` | Resolución de unidades visibles (rol Dueño / multi-unidad) | ~4 |
| `Persistence/SeedRolDuenoTests.cs` | Seed del rol Dueño | ~5 |
| `PlaceholderTests.cs` | Placeholder del proyecto de infraestructura | ~1 |

> La capa de Infraestructura concentra sus pruebas en servicios introducidos en la
> Iteración 5 (MFA, OAuth, notificador in-app, generador de cuotas, resolución de
> unidades). Las iteraciones 1-4 se apoyan principalmente en las pruebas de Dominio y
> Application listadas arriba.

---

## Cómo reproducir / regenerar este inventario

```bash
# Ejecutar toda la suite
cd backend
dotnet test

# Conteo aproximado de casos por archivo (Git Bash)
grep -rcE "\[Fact\]|\[Theory\]" backend/tests --include="*.cs" | grep -v "/obj/"
```

> Los conteos de este documento se calcularon contando atributos `[Fact]` y `[Theory]`
> por archivo. Al actualizar la suite, regenerar la tabla con el comando anterior.
</content>
</invoke>
