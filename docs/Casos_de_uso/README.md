# Casos de Uso — GymFlow

Documentación detallada por caso de uso **realmente implementado**. La fuente de verdad sobre lo entregado son los documentos de iteración en [`docs/seguimiento/`](../seguimiento/); el documento [GymFlow_Requerimientos_Completos.md](../GymFlow_Requerimientos_Completos.md) refleja el alcance planeado y puede divergir.

## Índice

| CU | Título | Actor principal | Iteración(es) de entrega |
|----|--------|-----------------|--------------------------|
| [CU-01](CU-01-gestion-socios.md) | Gestión de Socios (ABM + perfil + ARCO) | Administrador / Socio | IT-1 (RF-01/02/03/04/20/22) + IT-2 (RF-05, RNF-09b) |
| [CU-02](CU-02-inscripcion-clase.md) | Inscripción a Clase | Socio | IT-3 (base RF-10/11) → IT-4 (completo) |
| [CU-03](CU-03-cuotas-recordatorios.md) | Gestión de Cuotas y Recordatorios | Sistema / Administrador | IT-2 (RF-06 + RF-07) |
| [CU-05](CU-05-autenticacion.md) | Autenticación y Control de Acceso | Todos | IT-1 (JWT base) → IT-2 (roles + permisos dinámicos) → IT-5 (MFA TOTP + Google OAuth) |
| [CU-06](CU-06-clases-horarios.md) | Gestión de Clases y Horarios | Administrador | IT-3 (RF-08/09) + IT-4 (mejoras UX) |
| [CU-07](CU-07-empleados-roles.md) | Gestión de Empleados, Roles y Permisos | Administrador | IT-2 (RNF-01 base + ABM empleados) → IT-4 (RF-12/13/14: credenciales + permisos profesor) → IT-5 (rol Dueño) |
| [CU-08](CU-08-gestion-eventos.md) | Gestión de Eventos | Administrador / Socio | IT-5 (RF-15) |
| [CU-09](CU-09-notificaciones-insystem.md) | Notificaciones in-system | Socio | IT-5 (RF-16, adelantado de IT-6) |

## Casos de uso del documento de requerimientos no implementados

| CU original | Título | Estado |
|-------------|--------|--------|
| CU-04 (req.) | Dashboard Consolidado Multi-Espacio | No implementado en IT-1 a IT-5. La arquitectura multi-espacio (RF-20) sí está, pero no hay vista de dashboard consolidado documentada. |
| CU-08 (req.) | Pago de Cuota Online mediante Mercado Pago (RF-21) | No implementado en IT-1 a IT-5. El botón "Pagar" del portal existe pero está deshabilitado. |

> **Nota sobre numeración:** la numeración `CU-XX` de este índice sigue la usada en los documentos de seguimiento (donde CU-08 = Eventos y CU-09 = Notificaciones), no la del documento original de requerimientos (donde CU-08 sería el pago MP).

## Convenciones

- Un archivo por caso de uso, siguiendo el formato de [_PLANTILLA.md](_PLANTILLA.md) (alineado con el "Caso de uso extendido" usado en [iteracion-3.md](../seguimiento/iteracion-3.md)).
- La tabla inicial incluye actor, precondición, postcondición, RF cubiertos, iteración(es) de entrega y referencias a docs originales / specs.
- Los cambios respecto del diseño original se documentan al final en un bloque **Desviaciones**.
