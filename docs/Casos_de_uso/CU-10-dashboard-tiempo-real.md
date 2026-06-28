# CU-10: Dashboard en Tiempo Real Multi-Espacio

> **Estado:** Diseño previo a la construcción (Iteración 6). Este CU es el insumo para redactar el spec y el plan de RF-18; los flujos y reglas pueden ajustarse durante la implementación.

| *Campo* | |
|-|-|
| *Nombre* | Dashboard operativo en tiempo real con filtros por unidad |
| *Actor principal* | Administrador / Dueño (acotado a sus unidades). En general, cualquier rol con permiso de lectura sobre el módulo Dashboard. |
| *Precondición* | Actor autenticado con permiso de lectura sobre el módulo Dashboard. Existen datos operativos (socios, cuotas, clases, inscripciones) en al menos una unidad. |
| *Postcondición* | El actor visualiza métricas operativas consolidadas y actualizadas (antigüedad máx. 30 s respecto a la BD), sin recargar la página. No modifica datos: el dashboard es de solo lectura. |
| *RF cubiertos* | RF-18 (Dashboard en tiempo real) · RNF-02 (actualización sin recarga vía SSE) |
| *Iteración de entrega* | IT-6 (planificada) — [GymFlow_Requerimientos_Completos.md § Iteración 6](../GymFlow_Requerimientos_Completos.md) |
| *Referencia original* | [GymFlow_Requerimientos_Completos.md — RF-18](../GymFlow_Requerimientos_Completos.md) |
| *Specs / planes* | *(a generar a partir de este CU)* [[spec-rf18-dashboard]] · [[plan-rf18-dashboard]] |

**Flujo principal — Cargar y observar el dashboard:**

1. El actor accede a "Dashboard" desde el panel de administración.
2. El sistema verifica que el actor tenga permiso de lectura sobre el módulo Dashboard.
3. El sistema carga, **por defecto, la vista consolidada de ambos espacios** (RN-14) con las métricas operativas:
   - **Socios activos** (total y por unidad).
   - **Cuotas pendientes** (próximas a vencer + vencidas), calculadas dinámicamente según la fecha del sistema (RN-17).
   - **Clases del día** (programadas para la fecha actual, con cupo e inscriptos).
   - **Inscripciones recientes** (últimas inscripciones a clases).
4. El frontend abre una conexión **SSE** (Server-Sent Events) contra el endpoint de dashboard (RNF-02).
5. El backend emite actualizaciones de las métricas a través de la conexión SSE de modo que el panel refleje el estado real con una **antigüedad máxima de 30 segundos** (RN-15), sin recarga de página.
6. El actor observa las métricas actualizándose en vivo.

**Flujo alternativo — Filtrar por unidad:**

1. El actor selecciona una unidad específica (ej. "Espacio Mora") en el filtro.
2. El sistema recalcula y muestra las métricas acotadas a esa unidad.
3. El Dueño solo puede filtrar entre las unidades que tiene asignadas (no ve otras).
4. Volver a "Todas" restituye la vista consolidada por defecto.

**Flujos de excepción:**

- **E1 — Actor sin permiso sobre el módulo Dashboard:** Acceso denegado.
- **E2 — Caída/no soporte de la conexión SSE:** El dashboard muestra los datos de la carga inicial y degrada a un refresco periódico (polling) o indica "actualización en pausa"; nunca queda con datos sin marca de antigüedad.
- **E3 — Unidad sin datos:** Se muestran las métricas en cero, no un error.

**Reglas de negocio aplicables:**

- **RN-14:** El dashboard muestra la vista consolidada de ambos espacios por defecto al cargar.
- **RN-15:** Los datos tienen una antigüedad máxima de 30 segundos respecto al estado real de la BD.
- **RN-16:** Acceso restringido por permisos. El dashboard es un módulo dentro del sistema de permisos configurable: solo acceden los roles a los que el Dueño/Admin haya otorgado permiso de lectura sobre el módulo Dashboard. El Dueño ve solo sus unidades asignadas.
- **RN-17:** Las métricas de cuotas se calculan dinámicamente en base al estado actual (no se almacenan precalculadas).

**Criterios de aceptación:**

- **CA-01:** Al cargar, el dashboard muestra socios activos, cuotas pendientes, clases del día e inscripciones recientes consolidados de ambas unidades.
- **CA-02:** El panel refleja cambios en la BD sin recargar la página, con un retraso no mayor a 30 segundos.
- **CA-03:** El filtro por unidad recalcula todas las métricas para la unidad elegida; el Dueño solo ve sus unidades.
- **CA-04:** El acceso se controla por el permiso del módulo Dashboard: un rol con el permiso accede a la vista completa; un rol sin él (p. ej. Socio) no puede acceder. No existe una variante "reducida" por rol.
- **CA-05:** El dashboard no permite ninguna operación de escritura.

**Desviaciones respecto del diseño original:**

- **Acceso por permisos en lugar de roles fijos:** RN-16 y RF-18 fueron escritos cuando "Profesor" era un rol hardcoded con vista limitada. A partir de iteraciones anteriores los roles se crean desde la interfaz con permisos por módulo (igual que el módulo Eventos en [CU-08](CU-08-gestion-eventos.md)). Por eso el dashboard **no** define métricas ni vista específicas para Profesor: el acceso lo determina el permiso de lectura sobre el módulo Dashboard que el Dueño/Admin haya asignado a cada rol.
- *(A completar durante la implementación si la solución técnica diverge — p. ej. mecanismo de actualización elegido frente a SSE puro.)*
