# CU-02: Inscripción a Clase (por horario)

| *Campo* | |
|-|-|
| *Nombre* | Inscripción a Clase por Horario |
| *Actor principal* | Socio |
| *Precondición* | Socio autenticado, estado `Activo`, cuota al día en la unidad de la clase. Existen clases con horarios y cupo disponible. |
| *Postcondición* | Socio inscripto al horario. Cupo ocupado incrementado. Inscripción visible en "Mis Inscripciones". Email de confirmación enviado. Notificación in-system creada (IT-5). Auditoría registrada. |
| *RF cubiertos* | RF-10, RF-11 |
| *Iteración(es) de entrega* | IT-3 (base: backend + frontend de inscripción y "Mis Clases") → IT-4 (completo: inscripción por `HorarioClaseId`, validación de cuota al día, email, auditoría, query optimizada batch). |
| *Referencia original* | [GymFlow_Requerimientos_Completos.md § CU-02](../GymFlow_Requerimientos_Completos.md#cu-02--inscripción-a-clase) |
| *Referencia spec* | [spec-inscripcion-por-horario](../specs/spec-inscripcion-por-horario.md), [spec-it4-inscripciones-empleados-horarios](../specs/spec-it4-inscripciones-empleados-horarios.md) |
| *Referencia plan* | [plan-inscripcion-por-horario](../plans/plan-inscripcion-por-horario.md), [plan-it4-inscripciones-empleados-horarios](../plans/plan-it4-inscripciones-empleados-horarios.md) |
| *Documentos de iteración* | [iteracion-3.md](../seguimiento/iteracion-3.md), [iteracion-4.md](../seguimiento/iteracion-4.md) |

**Flujo principal:**

1. El socio accede a "Horarios" en el portal (`/portal/horarios`).
2. El sistema muestra los horarios agrupados por día con nombre de clase, instructor, hora, sala y cupos disponibles.
3. El socio selecciona el filtro de sede (obligatorio).
4. El socio selecciona un horario con cupo disponible → "Inscribirme".
5. El sistema verifica: (a) no inscripto previamente a ese horario (RN-09), (b) cupo disponible, (c) cuota al día en la unidad de la clase, (d) clase activa.
6. El sistema registra la inscripción con `HorarioClaseId`, actualiza el conteo de cupo y genera log de auditoría.
7. El sistema envía email de confirmación al socio con día, hora y sala.
8. (IT-5) El sistema crea una notificación in-system "Confirmación de inscripción" en el portal del socio.
9. El horario actualiza el cupo visible. La inscripción aparece en "Mis Inscripciones".

**Flujo alternativo — Desinscripción:**

1. El socio accede a "Mis Inscripciones" (`/portal/mis-inscripciones`).
2. Selecciona "Cancelar inscripción" → confirma.
3. El sistema cancela la inscripción, libera el cupo y registra auditoría.

**Flujo alternativo — Misma clase en otro horario:**

- El socio puede inscribirse a la misma clase en un horario distinto (ej. Yoga lunes y Yoga miércoles). La RN-09 solo impide duplicar el mismo horario.

**Flujos de excepción:**

- **E1 — Sin cupo:** "Este horario no tiene cupos disponibles." Botón deshabilitado con texto "Cupo lleno". (Lista de espera descartada: si no hay cupo se bloquea.)
- **E2 — Inscripción duplicada (RN-09):** "Ya estás inscripto en este horario."
- **E3 — Clase cancelada:** No permite nuevas inscripciones a horarios de clases canceladas.
- **E4 — Cuota vencida:** "No podés inscribirte con cuota vencida en esta sede."

**Reglas de negocio aplicables:**

- **RN-09:** un socio no puede inscribirse dos veces al mismo `HorarioClaseId`, pero sí a otros horarios de la misma clase.
- **Cuota al día por sede:** la validación de cuota se hace contra la unidad específica de la clase, no global.
- **Cancelación libera cupo inmediatamente** y queda en auditoría con timestamp y usuario.
- **Query optimizada (IT-4):** "Mis Inscripciones" usa conteo batch (2 queries) para evitar N+1.

**Desviaciones respecto del diseño original:**

- **Inscripción por horario, no por clase:** el diseño original tenía una sola entidad `Clase` con horario. Se rediseñó para que el socio se inscriba a un `HorarioClaseId` específico. La vista `portal/clases` fue reemplazada por `portal/horarios`.
- **Lista de espera descartada:** el doc original contemplaba lista de espera cuando no hay cupo. En IT-4 se decidió bloquear directamente la inscripción para simplificar la experiencia.
