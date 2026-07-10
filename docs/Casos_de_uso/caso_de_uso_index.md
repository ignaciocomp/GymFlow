# Casos de Uso — GymFlow

Documentación detallada por caso de uso **realmente implementado**. La fuente de verdad sobre lo entregado son los documentos de iteración en [`docs/seguimiento/`](../seguimiento/); el documento [GymFlow_Requerimientos_Completos.md](../GymFlow_Requerimientos_Completos.md) refleja el alcance planeado y puede divergir.

## Cobertura de Requerimientos Funcionales

Cada RF del documento de requerimientos, con el CU que lo satisface (o el motivo por el que no está cubierto).

| RF    | Descripción breve          | CU                                        | Cómo se satisface                                                                                        |
| ----- | -------------------------- | ----------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| RF-01 | Registrar socio            | [CU-01](CU-01-gestion-socios.md)          | Subflujo A — Registro                                                                                    |
| RF-02 | Listar socios              | [CU-01](CU-01-gestion-socios.md)          | Flujo principal — listado con búsqueda y filtros                                                         |
| RF-03 | Editar socio               | [CU-01](CU-01-gestion-socios.md)          | Subflujo B — Edición                                                                                     |
| RF-04 | Baja lógica de socio       | [CU-01](CU-01-gestion-socios.md)          | Subflujo C — Baja y reactivación                                                                         |
| RF-05 | Ver perfil del socio       | [CU-01](CU-01-gestion-socios.md)          | Subflujo D — Perfil del socio                                                                            |
| RF-06 | Recordatorio de cuota      | [CU-03](CU-03-cuotas-recordatorios.md)    | Flujo "Recordatorios automáticos"                                                                        |
| RF-07 | Control de estado de cuota | [CU-03](CU-03-cuotas-recordatorios.md)    | Flujo "Generación automática" + gestión admin                                                            |
| RF-08 | Gestionar clases           | [CU-06](CU-06-clases-horarios.md)         | Flujo ABM de clases                                                                                      |
| RF-09 | Gestionar horarios         | [CU-06](CU-06-clases-horarios.md)         | Flujo definición de horarios semanales                                                                   |
| RF-10 | Inscribirse a clase        | [CU-02](CU-02-inscripcion-clase.md)       | Flujo principal                                                                                          |
| RF-11 | Ver mis clases             | [CU-02](CU-02-inscripcion-clase.md)       | Subflujo "Mis inscripciones"                                                                             |
| RF-12 | Gestionar empleados        | [CU-07](CU-07-empleados-roles.md)         | Flujo alta con credenciales temporales                                                                   |
| RF-13 | Profesor registra socios   | [CU-07](CU-07-empleados-roles.md)         | Por configuración — rol "Profesor" con permisos sobre módulo Socios                                      |
| RF-14 | Profesor gestiona clases   | [CU-07](CU-07-empleados-roles.md)         | Por configuración — rol "Profesor" con permisos sobre módulo Clases                                      |
| RF-15 | Gestionar eventos          | [CU-08](CU-08-gestion-eventos.md)         | Flujo ABM de eventos                                                                                     |
| RF-16 | Notificaciones             | [CU-09](CU-09-notificaciones-insystem.md) | Flujo notificaciones in-system                                                                           |
| RF-17 | Crear rutinas              | —                                         | **No se hace**                                                                                           |
| RF-18 | Dashboard en tiempo real   | [CU-10](CU-10-dashboard-tiempo-real.md)   | CU de diseño (It. 6) — flujo dashboard consolidado + SSE                                                 |
| RF-19 | Sitio web público          | —                                         | Implementado en IT-4 ([spec](../specs/spec-rf19-sitio-publico.md)), no genera CU                         |
| RF-20 | Multi-espacio              | [CU-01](CU-01-gestion-socios.md)          | Prerrequisito arquitectónico — atraviesa también CU-03, CU-06 y CU-07 (filtros y asignación por unidad). |
| RF-21 | Pago online (Mercado Pago) | [CU-11](CU-11-pago-online-mercadopago.md) | CU de diseño (It. 6) — Checkout Pro + webhook HMAC + confirmación                                        |
| RF-22 | Plan por unidad de negocio | [CU-01](CU-01-gestion-socios.md)          | Subflujo A — asignación de plan por unidad                                                               |

**Resumen:** 19 RFs implementados (18 a través de un CU + RF-19 como sitio estático sin CU). RF-18 y RF-21 cuentan con CU de diseño (CU-10 y CU-11) como insumo de la It. 6, pendientes de construcción. RF-17 no se implementa.

> **Nota sobre numeración:** la numeración `CU-XX` de este índice sigue la usada en los documentos de seguimiento (donde CU-08 = Eventos y CU-09 = Notificaciones), no la del documento original de requerimientos (donde CU-08 sería el pago MP).
>
> **No existe un archivo CU-04:** en la numeración original de requerimientos, CU-04 era el "Dashboard Consolidado Multi-Espacio". Como no estaba implementado cuando se documentaron los casos de uso (IT-1 a IT-5), se salteó el número. Al entrar el dashboard al alcance de la Iteración 6 se le creó su documento de diseño como [CU-10](CU-10-dashboard-tiempo-real.md), por lo que el hueco es solo de numeración, no de cobertura.


## Convenciones

- Un archivo por caso de uso, siguiendo el formato de [_PLANTILLA.md](_PLANTILLA.md) (alineado con el "Caso de uso extendido" usado en [iteracion-3.md](../seguimiento/iteracion-3.md)).
- La tabla inicial incluye actor, precondición, postcondición, RF cubiertos, iteración(es) de entrega y referencias a docs originales / specs.
