# CU-06: Gestión de Clases y Horarios

| *Campo* | |
|-|-|
| *Nombre* | Gestión de Clases y Horarios |
| *Actor principal* | Administrador |
| *Precondición* | Admin autenticado con permisos sobre el módulo Clases/Horarios. Existe al menos una unidad registrada. |
| *Postcondición* | Clase creada/modificada/cancelada con log de auditoría. Horarios asignados con validación de conflictos de sala. Socios notificados por email ante cambios o cancelaciones. |
| *RF cubiertos* | RF-08 (Clases), RF-09 (Horarios) |
| *Iteración(es) de entrega* | IT-3 — CRUD completo de clases y horarios + validación de conflictos + notificaciones por email + auditoría. IT-4 — Mejora: filtro de sede obligatorio en calendario admin + split visual de clases solapadas. |
| *Referencia original* | [GymFlow_Requerimientos_Completos.md § CU-06](../GymFlow_Requerimientos_Completos.md#cu-06--gestión-de-clases-y-horarios) |
| *Documentos de iteración* | [iteracion-3.md](../seguimiento/iteracion-3.md), [iteracion-4.md](../seguimiento/iteracion-4.md) |

**Flujo principal — Creación de Clase:**

1. Admin accede al módulo "Clases" → "Nueva Clase".
2. Sistema presenta formulario con campos: nombre, descripción, instructor, capacidad máxima, duración (minutos), unidad.
3. Admin completa los campos y confirma.
4. Sistema valida que la capacidad esté entre 1 y 500 y que los campos obligatorios estén completos.
5. Sistema registra la clase con estado "Activo", genera log de auditoría y muestra confirmación.
6. La clase queda visible en el calendario y en el portal de socios.

**Flujo alternativo — Asignación de Horarios Semanales:**

1. Admin accede al calendario semanal en "Horarios" → "Nuevo Horario".
2. Selecciona la clase, uno o más días de la semana (checkboxes), hora de inicio, hora de fin y sala (opcional).
3. Sistema verifica que no haya solapamiento de sala (misma sala, mismo día, horarios superpuestos).
4. Guarda los horarios y los muestra en la grilla semanal con código de colores.

**Flujo alternativo — Edición de Clase:**

1. Admin selecciona la clase → "Editar".
2. Sistema carga formulario con datos actuales.
3. Admin modifica los campos deseados.
4. Si se reduce la capacidad máxima, el sistema valida que no quede por debajo de las inscripciones activas.
5. Sistema guarda cambios y registra auditoría.

**Flujo alternativo — Cancelación de Clase:**

1. Admin selecciona la clase → "Cancelar".
2. Sistema solicita confirmación.
3. Sistema cancela la clase (baja lógica) y cancela todas las inscripciones activas.
4. Envía emails de notificación a los socios inscriptos (en paralelo con `Task.WhenAll`).
5. Registra en auditoría el resultado del envío de cada email (exitoso/fallido).
6. (IT-5) Crea notificaciones in-system "Cancelación de clase" para los socios afectados.

**Flujo alternativo — Reactivación de Clase:**

1. Admin selecciona una clase cancelada → "Reactivar".
2. Sistema reactiva la clase (estado "Activo").
3. Las inscripciones previas **no se restauran** — los socios deben reinscribirse.

**Flujo alternativo — Modificación de Horario:**

1. Admin selecciona un horario existente → "Editar".
2. Modifica día, horario o sala.
3. Sistema re-valida conflictos de sala.
4. Si hay socios inscriptos a esa clase, envía notificación por email informando el cambio.
5. (IT-5) Crea notificaciones in-system "Cambio de horario" para los socios inscriptos.
6. Registra auditoría con el resultado del envío de emails.

**Flujo alternativo — Vista del Socio (Portal):**

1. Socio accede a "Horarios" en el portal y ve los horarios disponibles agrupados por día.
2. Puede inscribirse a clases con cupo disponible (ver [CU-02](CU-02-inscripcion-clase.md)).

**Mejoras de IT-4 (UX admin):**

- **Filtro de sede obligatorio** en el calendario de horarios del admin (antes mostraba mezcladas las sedes).
- **Split visual de clases solapadas** en la misma franja horaria para evitar superposición de bloques en la grilla semanal.

**Flujos de excepción:**

- **E1 — Conflicto de sala:** "La sala ya está ocupada en ese horario." Bloquea el guardado.
- **E2 — Capacidad menor a inscripciones:** "No se puede reducir la capacidad por debajo de las inscripciones activas (X)."
- **E3 — Clase ya cancelada:** "La clase ya está cancelada."
- **E4 — Clase ya activa:** "La clase ya está activa."
- **E5 — Horario inválido:** Hora de fin debe ser posterior a hora de inicio.

**Reglas de negocio aplicables:**

- **Conflicto de sala:** un horario no puede coincidir con otro en la misma sala, mismo día, con solapamiento de hora.
- **Capacidad ≥ inscripciones activas:** al editar una clase no se puede reducir la capacidad por debajo del número actual de inscriptos.
- **Cancelación = baja lógica:** la clase no se elimina; queda con estado `Cancelada`.
- **Reactivación no restaura inscripciones:** los socios afectados deben reinscribirse manualmente.
- **Notificación obligatoria al socio ante cambios:** modificación de horario y cancelación de clase disparan email + notificación in-system (IT-5).
- **Auditoría de envío de emails:** el resultado de cada email (exitoso/fallido) queda en auditoría.

**Desviaciones respecto del diseño original:** *(ninguna documentada en los docs de iteración)*
