---
tags:
  - proyect_index
---

# GymFlow — Especificación Completa para Implementación

> **Sistema Integrado de Gestión para Gimnasios — Espacio Mora**
> Documento de referencia para agentes de programación.
> Extraído del documento académico ATI-268502-243233-309167 (Universidad ORT Uruguay, 2026).

Docs relacionados: [[agent_Context]] · [[spec-gymflow-design]]

---

## 1. Contexto del Proyecto

**Cliente:** Espacio Mora — emprendimiento familiar en Malvín, Montevideo, Uruguay.
**Propietario/contraparte:** Maurice.

El negocio opera dos unidades bajo un mismo espacio físico:
- **Gimnasio Nuevo Malvín**: entrenamiento físico general, musculación y fitness.
- **Espacio Mora**: actividades complementarias (telas aéreas, artes marciales, actividades infantiles).

**Datos clave del negocio:**
- ~300 socios activos distribuidos entre ambas unidades.
- Horario: 7:00 a 22:00 hs, lunes a sábado.
- 6 profesores/instructores.
- Ingresos por cuotas mensuales diferenciadas por plan y unidad.

**Problema actual:** Gestión fragmentada con 2 suscripciones independientes a SmartGym, duplicación de costos, datos no consolidados, recordatorios manuales, sin dashboard unificado.

**Solución:** Plataforma web unificada "GymFlow" que centraliza socios, membresías, pagos, asistencias, horarios y clases de ambas unidades.

---

## 2. Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| **Backend** | C# / ASP.NET Core Web API / .NET 8 |
| **Arquitectura** | Clean Architecture (Domain, Application, Infrastructure, Presentation) |
| **Base de Datos** | PostgreSQL 16 |
| **ORM** | Entity Framework Core |
| **Frontend** | React.js + TypeScript + Tailwind CSS |
| **HTTP Client** | Axios (REST/JSON) |
| **Autenticación** | JWT (JSON Web Tokens) |
| **MFA** | TOTP (Google Authenticator / Microsoft Authenticator) para Admin y Profesor |
| **OAuth** | Google OAuth 2.0 para Socios |
| **Testing** | xUnit + Moq |
| **CI/CD** | GitHub Actions (tests contra contenedor PostgreSQL 16 en cada PR a develop) |
| **Control de versiones** | Git + GitHub |
| **Deploy** | AWS o Azure (según taller ORT) |

### Patrones de Diseño a Aplicar
- **Observer**: notificaciones en tiempo real del dashboard.
- **Strategy**: diferentes reglas de cálculo de cuotas según plan del socio.
- **Singleton**: gestión de configuración del sistema.
- **Facade**: interacción entre módulos de gestión multi-espacio.

---

## 3. Actores del Sistema

| Actor | Descripción |
|-------|-------------|
| **Administrador (Maurice)** | Gestión completa: socios, clases, cuotas, empleados, dashboard, eventos. Requiere MFA. |
| **Profesor** | Gestiona solo sus clases asignadas, puede registrar socios con permisos limitados. Requiere MFA. |
| **Socio** | Consulta horarios, se inscribe a clases, ve su perfil, crea rutinas. Login con Google OAuth 2.0. |

---

## 4. Modelo de Dominio (Entidades Principales)

### Entidades y Atributos

**Usuario** (base): id, nombre, apellido, correo, contraseña, rol, estado, espacioAsignado (FK Unidad), fechaCreacion.

**Socio** (extiende Usuario): fechaAlta, consentimientoInformado (bool + timestamp), tipoDocumento, documentoIdentidad, telefono, fechaNacimiento. Relaciones: Cuota (1:N), Inscripcion (1:N), Rutina (1:N). **El plan ya no es un atributo directo del Socio** — cada Socio puede tener un Plan distinto por Unidad, almacenado en la tabla intermedia `UsuarioUnidad.PlanId` (ver RF-22).

**Administrador** (extiende Usuario): acceso completo a ambas unidades.

**Profesor** (extiende Usuario): clasesAsignadas (colección de Clase, 1:N).

**Unidad**: id, nombre ("Gimnasio Nuevo Malvín" / "Espacio Mora"), dirección. Pivote multi-espacio del sistema.

**Clase**: id, nombre, descripción, cupoMaximo, duracion, unidad (FK), horarios (colección de Horario, 1:N).

**Horario**: id, diaSemana, horaInicio, horaFin, clase (FK).

**Inscripcion**: id, fechaInscripcion, estado (activo / cancelado), horario (FK), socio (FK). *(Cambio de diseño: la FK apunta a Horario, no a Clase — ver [[spec-inscripcion-por-horario]])*

**Cuota**: id, fechaVencimiento, estado (alDia / proximaAVencer / vencida), plan (FK), socio (FK).

**Plan**: id, nombre, precio, descripcion, unidad (FK).

**Rutina**: id, nombre, descripcion, ejercicios (colección de Ejercicio, 1:N), socio (FK).

**Ejercicio**: id, nombre, series, repeticiones, peso, rutina (FK).

**Evento**: id, titulo, descripcion, fecha, unidad (FK). Vinculado a Notificacion.

**Notificacion**: id, tipo (recordatorio / evento / cambioHorario), mensaje, fechaEnvio, socio (FK).

### Nota sobre Herencia EF Core
La estrategia de herencia (TPH, TPT o TPC) desde Usuario hacia Administrador/Profesor/Socio se define en la Iteración 1. Si no se llega a una decisión fundamentada, usar **TPH** (Table Per Hierarchy) como fallback de menor complejidad.

---

## 5. Requerimientos Funcionales

| Código    | Descripción                                                                                                                                                                                                                                                                       | Necesidades      | Módulo                   |
| --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------- | ------------------------ |
| **RF-01** | Registrar socio: alta con nombre, contacto, plan, fecha de alta y espacio (gimnasio, actividades o ambos).                                                                                                                                                                        | N-01, N-02       | Gestión de Socios        |
| **RF-02** | Listar socios: vista de todos los socios con búsqueda por nombre y filtros por estado de cuota, espacio y plan. Fecha de alta seleccionable ([[spec-rf02-fecha-alta-seleccionable]]).                                                                                             | N-02             | Gestión de Socios        |
| **RF-03** | Editar socio: modificar datos manteniendo historial de cambios.                                                                                                                                                                                                                   | N-02             | Gestión de Socios        |
| **RF-04** | Baja lógica de socio: marcar como inactivo sin eliminar el registro.                                                                                                                                                                                                              | N-02             | Gestión de Socios        |
| **RF-05** | Ver perfil del socio: el socio consulta sus datos, estado de cuota y plan activo.                                                                                                                                                                                                 | N-02, N-05       | Gestión de Socios        |
| **RF-06** | Recordatorio de cuota: envío automático a socios con cuota próxima a vencer o vencida.                                                                                                                                                                                            | N-03             | Cuotas y Pagos           |
| **RF-07** | Control de estado de cuota: visualización de socios al día, próximos a vencer o vencidos ([[spec-rf07-gestion-cuotas]]).                                                                                                                                                          | N-02, N-03, N-04 | Cuotas y Pagos           |
| **RF-08** | Gestionar clases: crear, editar y eliminar clases/actividades con nombre, horario y cupo máximo.                                                                                                                                                                                  | N-01, N-06       | Clases y Horarios        |
| **RF-09** | Gestionar horarios: definir horarios semanales de clases contemplando ambos espacios.                                                                                                                                                                                             | N-05, N-06       | Clases y Horarios        |
| **RF-10** | Inscribirse a clase: socio se inscribe si hay cupo disponible y cuota al día.                                                                                                                                                                                                     | N-05, N-06       | Clases y Horarios        |
| **RF-11** | Ver mis clases: socio visualiza clases a las que está inscripto.                                                                                                                                                                                                                  | N-05, N-06       | Clases y Horarios        |
| **RF-12** | Gestionar empleados/profesores: fichas con roles y permisos diferenciados.                                                                                                                                                                                                        | N-08             | Empleados y Profesores   |
| **RF-13** | Profesor registra socios: alta de socios con permisos limitados y mismas validaciones que admin.                                                                                                                                                                                  | N-02, N-08       | Empleados y Profesores   |
| **RF-14** | Profesor gestiona sus clases: contemplado por roles y permisos configurables desde interfaz. El administrador puede crear un rol Profesor y asignarle permisos por modulo.                                                                                                        | N-06, N-08       | Empleados y Profesores   |
| **RF-15** | Gestionar eventos: crear eventos especiales (torneos, charlas, promociones) y notificar socios.                                                                                                                                                                                   | N-11             | Eventos y Notificaciones |
| **RF-16** | Notificaciones: socio recibe avisos de eventos, recordatorios de cuota y cambios de horario por email y dentro del sistema.                                                                                                                                                       | N-03, N-05, N-11 | Eventos y Notificaciones |
| **RF-17** | Crear rutinas: socio crea/guarda rutinas de entrenamiento con ejercicios, series, repeticiones y peso.                                                                                                                                                                            | N-09             | Rutinas                  |
| **RF-18** | Dashboard en tiempo real: panel con socios activos, cuotas pendientes, clases del día, inscripciones recientes, filtros por unidad.                                                                                                                                               | N-01, N-04       | Dashboard                |
| **RF-19** | Sitio web público: página con info de Espacio Mora, fotos, horarios, planes, ubicación y formulario de contacto.                                                                                                                                                                  | N-07             | Página Web Pública       |
| **RF-20** | Gestión unificada multi-espacio: administrar ambas unidades desde una plataforma con separación y filtrado por unidad.                                                                                                                                                            | N-01             | Multi-Espacio            |
| **RF-21** | Pago de cuotas online: el socio puede abonar su cuota a través del portal web utilizando Mercado Pago como pasarela de pago (Checkout Pro). El sistema registra el pago vía webhook, actualiza el estado de la cuota automáticamente y emite confirmación por correo electrónico. | N-02, N-03, N-05 | Cuotas y Pagos           |
| **RF-22** | Plan por unidad de negocio: al registrar o modificar un socio, asignar un plan independiente por cada unidad seleccionada. Un socio con dos unidades puede tener planes distintos en cada una.                                                                                    | N-01, N-02       | Gestión de Socios        |


---

## 6. Requerimientos No Funcionales

| Código | Descripción | Tipo |
|--------|-------------|------|
| **RNF-01** | Autenticación y autorización basada en roles. Cada usuario accede solo a funcionalidades de su perfil. Implementado en dos partes: It.2 (admin, profesor y otros roles internos vía email + password con BCrypt) e It.5 (socios vía Google OAuth). Specs: [[spec-rnf01-roles-y-permisos]] · [[spec-rnf01-gestion-usuarios]]. | Seguridad |
| **RNF-02** | Dashboard actualiza en tiempo real mediante Server-Sent Events (SSE) sin recarga de página. | Rendimiento |
| **RNF-03** | Plataforma responsive: móvil, tablet y escritorio. | Usabilidad |
| **RNF-04** | Sitio público optimizado SEO: semántica HTML5, metaetiquetas, URLs amigables, carga rápida. | Rendimiento/Marketing |
| **RNF-05** | Datos personales almacenados de forma segura, acceso restringido, contraseñas cifradas. | Seguridad |
| **RNF-06** | Disponibilidad ≥ 95% en horario de operación. | Confiabilidad |
| **RNF-07** | Compatible con Chrome, Firefox, Safari y Edge (últimas versiones). | Compatibilidad |
| **RNF-08** | Arquitectura multi-unidad sin degradación significativa de rendimiento. | Arquitectura |
| **RNF-09** | Cumplimiento Ley 18.331 (Protección de Datos Personales de Uruguay): consentimiento informado al registro, permitir baja/modificación de datos, almacenar evidencia de consentimiento (log con timestamp, usuario y versión de política). | Privacidad/Legal |
| **RNF-10** | MFA (TOTP) para Admin y Profesor. OAuth 2.0 con Google para Socios. | Seguridad/Autenticación |
| **RNF-11** | Log de auditoría de operaciones críticas: creación/modificación/baja de socios, cambios de estado de cuota, accesos de admin. Cada registro: usuario, timestamp, descripción. | Seguridad/Auditoría |

---

## 7. Reglas de Negocio

| ID | Regla |
|----|-------|
| RN-01 | Un socio puede pertenecer a uno o ambos espacios. La cuota se calcula según espacio y plan. |
| RN-02 | La baja es siempre lógica. No se eliminan registros de la BD. |
| RN-03 | Toda modificación y baja genera registro de auditoría (RNF-11). |
| RN-04 | El consentimiento informado (Ley 18.331) queda registrado con timestamp al dar de alta un socio. |
| RN-05 | El correo electrónico es único por socio en el sistema. |
| RN-06 | Un socio solo puede inscribirse a clases del espacio al que pertenece, salvo que esté en ambos. |
| RN-08 | El cupo máximo por clase es definido por el admin al crear la clase. |
| RN-09 | Un socio no puede inscribirse dos veces al mismo horario. Puede inscribirse a la misma clase en distintos horarios. ([[spec-inscripcion-por-horario]]) |
| RN-10 | Recordatorios automáticos: 5 días antes, 1 día antes y el día del vencimiento si no se registró pago. |
| RN-11 | No más de un recordatorio del mismo tipo por socio por día. |
| RN-12 | Estado de cuota ("Al día", "Próxima a vencer", "Vencida") se calcula dinámicamente según fecha del sistema. |
| RN-13 | Solo el admin puede registrar un pago y actualizar fecha de vencimiento de cuota. |
| RN-14 | El dashboard muestra vista consolidada de ambos espacios por defecto al cargar. |
| RN-15 | Datos del dashboard: antigüedad máxima de 30 segundos respecto al estado real de la BD. |
| RN-16 | Dashboard: acceso restringido a Admin y Profesor (vista limitada para profesor). |
| RN-17 | Métricas de cuotas se calculan dinámicamente en base al estado actual. |
| RN-18 | Roles Admin y Profesor requieren MFA obligatorio (TOTP). |
| RN-19 | Rol Socio usa OAuth 2.0 con Google; no gestiona contraseña propia. |
| RN-20 | JWT incluye rol y espacio; backend valida ambos en cada request. |
| RN-21 | Tras 5 intentos fallidos de login se bloquea la cuenta por 15 minutos. |
| RN-22 | Una clase pertenece a un único espacio. |
| RN-24 | El cupo máximo no puede reducirse por debajo del número de inscripciones activas. |
| RN-25 | Si se modifica el horario de una clase con socios inscriptos, se les notifica automáticamente. |
| RN-26 | Un profesor solo puede gestionar las clases que tiene asignadas explícitamente. |
| RN-27 | La baja de un empleado es lógica; historial y clases dictadas se conservan. |
| RN-28 | Todo empleado/profesor debe configurar MFA antes de operar en el sistema. |
| RN-29 | El correo es único por usuario en todo el sistema. |
| RN-30 | Solo se puede pagar una cuota cuyo estado sea "Próxima a vencer" o "Vencida". No se generan pagos anticipados de cuotas futuras. |
| RN-31 | El sistema nunca actualiza el estado de cuota sin haber validado la autenticidad del webhook de Mercado Pago (firma HMAC). |
| RN-32 | El monto del pago es determinado por el sistema según el plan activo del socio. El socio no puede modificarlo. |
| RN-33 | Cada pago queda registrado en el log de auditoría con: usuario, timestamp, monto, número de transacción MP y estado resultante de la cuota. |
| RN-34 | En caso de pago rechazado o pendiente, no se modifica el estado de la cuota ni se bloquea el acceso del socio al portal. |

---

## 8. Casos de Uso Detallados

### CU-01 — Gestión de Socios (Alta, Baja, Modificación)

**Actores:** Administrador
**RF:** RF-01, RF-02, RF-03, RF-04, RF-05
**Necesidades:** N-01, N-02

**Precondiciones:**
1. Admin autenticado con MFA aprobado.
2. Admin tiene rol 'Administrador'.
3. Para modificación/baja: el socio existe en el sistema.

**Postcondiciones:**
- Alta: socio con estado 'Activo', asociado a espacio, con fecha de alta y plan.
- Modificación: datos actualizados + registro de auditoría (usuario, timestamp, campos modificados).
- Baja lógica: socio marcado como 'Inactivo'. Registro permanece en BD.

#### Flujo Principal — Alta de Socio
1. Admin accede a módulo 'Socios' → 'Nuevo Socio'.
2. Sistema presenta formulario de registro.
3. Admin completa: nombre completo, documento de identidad, teléfono, correo electrónico, fecha de nacimiento, espacio (Gimnasio / Espacio Mora / Ambos), plan activo, fecha de alta.
4. Sistema muestra cláusula de consentimiento informado (Ley 18.331). Admin confirma consentimiento del socio.
5. Sistema valida que correo no esté registrado y campos obligatorios completos.
6. Sistema registra socio con estado 'Activo', genera log de auditoría, muestra confirmación.
7. Sistema envía correo de bienvenida al socio (si tiene correo).

#### Flujo Alternativo — Modificación de Socio
1. Admin localiza socio por búsqueda (nombre o documento).
2. Selecciona 'Editar'.
3. Sistema carga formulario con datos actuales.
4. Admin modifica campos deseados.
5. Sistema valida datos.
6. Sistema guarda cambios + registro de auditoría (campos modificados, usuario, timestamp).

#### Flujo Alternativo — Baja Lógica
1. Admin localiza socio → 'Dar de baja'.
2. Sistema solicita confirmación y motivo de baja.
3. Admin confirma.
4. Sistema marca socio como 'Inactivo', registra motivo/fecha/usuario en log de auditoría.
5. Socio no aparece en listados activos pero permanece en historial.

#### Flujos de Excepción
- **E1 — Correo duplicado:** Error "El correo ingresado ya está registrado". Formulario permanece abierto.
- **E2 — Campos obligatorios incompletos:** Resalta en rojo los campos faltantes, bloquea guardado.
- **E3 — Formato inválido:** Valida formato correo y teléfono. Mensaje específico por campo.
- **E4 — Error de conexión:** "No se pudo completar la operación. Intente nuevamente." Datos no se pierden.

#### Criterios de Aceptación
- CA-01: Rechaza altas con correo duplicado o campos obligatorios incompletos.
- CA-02: Toda modificación queda en log de auditoría con usuario, timestamp y detalle.
- CA-03: Socios dados de baja no aparecen en listado activo pero sí en vista de inactivos.
- CA-04: Consentimiento informado es visible y obligatorio durante el alta.

**Mejoras implementadas (iteración 1):**
- `TipoDocumento` (enum requerido): CI | Pasaporte | Otro
  - Si `TipoDocumento == CI`: `DocumentoIdentidad` es obligatorio y debe ser una cédula uruguaya válida (algoritmo de dígito verificador).
  - Si `TipoDocumento == Pasaporte` u `Otro`: `DocumentoIdentidad` es opcional, sin validación de formato.
- Specs y planes: [[spec-rf01-tipo-documento]] | [[spec-rf01-frontend-tipo-documento]]

**Mejoras implementadas (RF-22 — iteración 1):**
- El formulario de alta y modificación de socio permite seleccionar un Plan por cada Unidad asignada.
- Los planes disponibles en cada dropdown se filtran a los planes activos de esa Unidad.
- El Plan ya no es un campo global del Socio; se almacena en `UsuarioUnidad.PlanId`.
- Specs y planes: [[spec-rf22-planes-por-unidad]]

---

### CU-02 — Inscripción a Clase

> **Actualizacion 2026-06-05:** este caso de uso se implementa por horario individual. El socio se inscribe desde `Horarios` a un `HorarioClaseId`; RN-09 impide duplicar la inscripcion al mismo horario, pero permite inscribirse a la misma clase en otros horarios. La vista `portal/clases` fue reemplazada por `portal/horarios`. La lista de espera fue desestimada: si no hay cupo, se bloquea la inscripcion.

**Actores:** Socio
**RF:** RF-10, RF-11, RF-09
**Necesidades:** N-05, N-06

**Precondiciones:**
1. Socio autenticado en el portal.
2. Socio con estado 'Activo' (cuota al día).
3. Existen clases cargadas con horario y cupo.

**Postcondiciones:**
- Socio inscripto en la clase. Cupo disponible se reduce en 1.
- Clase aparece en 'Mis clases'.
- Socio recibe notificación de confirmación por email.

#### Flujo Principal
1. Socio accede a 'Clases y Horarios'.
2. Sistema muestra listado con nombre, horario, espacio y cupos disponibles.
3. Socio puede filtrar por espacio (Gimnasio / Espacio Mora), día de la semana o tipo de actividad.
4. Socio selecciona clase → 'Inscribirme'.
5. Sistema verifica: (a) no inscripto previamente, (b) cupo disponible, (c) cuota al día.
6. Sistema registra inscripción, actualiza cupo, confirma con mensaje de éxito.
7. Socio recibe notificación de confirmación por email.

#### Flujo Alternativo — Desinscripción
1. Socio accede a 'Mis clases'.
2. Selecciona 'Cancelar inscripción'.
3. Sistema solicita confirmación.
4. Sistema elimina inscripción y libera cupo.
#### Flujos de Excepción
- **E1 — Sin cupo:** "Esta clase no tiene cupos disponibles." Bloquea la inscripcion.
- **E2 — Inscripción duplicada:** "Ya estás inscripto en esta clase."
- **E3 — Clase cancelada:** Notifica al socio y elimina inscripción automáticamente.

#### Criterios de Aceptación
- CA-05: Bloquea inscripción si sin cupo.
- CA-07: Cupo se decrementa correctamente al inscribirse.
- CA-08: Clase inscripta aparece en 'Mis clases' inmediatamente.
- CA-09: Socio recibe notificación tras inscripción exitosa.

---

### CU-03 — Gestión de Cuotas y Recordatorios Automáticos

**Actores:** Sistema (proceso automático), Administrador
**RF:** RF-06, RF-07
**Necesidades:** N-02, N-03, N-04

**Precondiciones:**
1. Socio tiene cuota registrada con fecha de vencimiento.
2. Socio tiene correo registrado.
3. Job de recordatorios automáticos está activo (proceso background).

#### Flujo Principal — Recordatorio Automático
1. Sistema ejecuta diariamente un job programado.
2. Evalúa todos los socios activos y sus fechas de vencimiento.
3. **Cuota vence en 5 días:** envía correo de recordatorio preventivo.
4. **Cuota vence en 1 día:** envía recordatorio urgente.
5. **Cuota vencida:** cambia estado a 'Cuota vencida' y envía notificación.
6. Registra cada notificación con timestamp y tipo de recordatorio.

#### Flujo Alternativo — Gestión Manual por Admin
1. Admin accede a módulo Cuotas o Dashboard.
2. Sistema muestra métricas: socios al día, próximos a vencer (7 días) y vencidos.
3. Admin filtra socios por estado de cuota.
4. Admin selecciona socio → registra pago manualmente → actualiza fecha de vencimiento.
5. Sistema actualiza estado a 'Al día' + registro de auditoría.

#### Flujos de Excepción
- **E1 — Correo no entregado:** Registra fallo en log. Admin ve error en panel.
- **E2 — Socio sin correo:** Omite envío, genera alerta visible en panel del admin.
- **E3 — Error en job automático:** Registra error, notifica admin en dashboard.

#### Criterios de Aceptación
- CA-10: Recordatorio automático a 5 días y 1 día antes del vencimiento.
- CA-11: Estado cambia a 'Cuota vencida' automáticamente en la fecha de vencimiento sin pago.
- CA-13: Dashboard refleja en tiempo real socios por estado de cuota.
- CA-14: No envía recordatorios duplicados del mismo tipo en el mismo día.

---

### CU-04 — Dashboard Consolidado Multi-Espacio

**Actores:** Administrador
**RF:** RF-18, RF-20
**RNF:** RNF-02, RNF-08
**Necesidades:** N-01, N-04

**Precondiciones:**
1. Admin autenticado con MFA.
2. Existen socios, clases y cuotas cargadas.

#### Flujo Principal
1. Admin accede al sistema → redirigido automáticamente al Dashboard como pantalla de inicio.
2. Sistema presenta indicadores consolidados (ambos espacios):
   - a. Total de socios activos.
   - b. Socios con cuota al día / próxima a vencer / vencida.
   - c. Clases programadas para hoy con cupos disponibles.
   - d. Inscripciones en últimas 24 horas.
   - e. Alertas activas (cuota vencida).
3. Admin puede filtrar por espacio (Gimnasio Nuevo Malvín, Espacio Mora, ambos).
4. Métricas se actualizan en tiempo real mediante Server-Sent Events (SSE) sin recarga.
5. Clic en cualquier métrica navega al módulo correspondiente con filtro aplicado.

#### Criterios de Aceptación
- CA-15: Dashboard carga en menos de 3 segundos desde login.
- CA-16: Cambios en socio o pago reflejados en máximo 30 segundos sin recarga.
- CA-17: Filtro por espacio muestra solo métricas del seleccionado.
- CA-18: Clic en métrica navega al módulo con filtro apropiado.
- CA-19: Dashboard no incluye socios inactivos en contadores principales.

---

### CU-05 — Autenticación y Control de Acceso (Login + MFA)

**Actores:** Administrador, Profesor, Socio
**RF:** RNF-01, RNF-10

#### Flujo Principal — Login con MFA (Admin / Profesor)
1. Usuario ingresa correo y contraseña.
2. Sistema valida credenciales (contraseña hasheada).
3. Detecta que el rol requiere MFA.
4. Solicita código TOTP de 6 dígitos de app autenticadora.
5. Usuario ingresa código.
6. Sistema valida código TOTP (ventana de 30 segundos).
7. Emite JWT firmado con rol y espacio, con tiempo de expiración.
8. Redirige según rol (Dashboard para Admin, vista clases para Profesor).

#### Flujo Alternativo — Login con OAuth 2.0 (Socio)
1. Socio hace clic en 'Iniciar sesión con Google'.
2. Redirige al flujo de autenticación Google OAuth 2.0.
3. Google autentica y retorna token.
4. Sistema verifica que el correo de Google está registrado como socio activo.
5. Emite JWT con rol Socio → redirige al portal de socios.

#### Flujos de Excepción
- **E1 — Credenciales incorrectas:** "Correo o contraseña incorrectos" (no especifica cuál). Tras 5 intentos fallidos → bloqueo 15 minutos.
- **E2 — TOTP inválido/expirado:** "Código incorrecto o expirado." Login no se completa.
- **E3 — Correo Google no registrado:** "No encontramos una cuenta asociada a este correo."
- **E4 — JWT expirado:** Redirige a login: "Tu sesión expiró."

#### Criterios de Aceptación
- CA-20: Admin no completa login sin MFA aprobado.
- CA-21: Socio puede autenticarse con Google si correo registrado.
- CA-22: Profesor no accede a vistas de administración.
- CA-23: Bloqueo tras 5 intentos fallidos.
- CA-24: JWT expirado rechazado, fuerza re-autenticación.

---

### CU-06 — Gestión de Clases y Horarios

**Actores:** Administrador
**RF:** RF-08, RF-09
**Necesidades:** N-01, N-06

**Precondiciones:**
1. Existe al menos un espacio registrado.

#### Flujo Principal — Creación de Clase
1. Admin → módulo 'Clases' → 'Nueva Clase'.
2. Formulario: nombre, descripción, espacio (Gimnasio / Espacio Mora), cupo máximo, duración en minutos.
3. Admin completa y confirma.
4. Registra la clase → visible en calendario y portal de socios.

#### Flujo Alternativo — Asignación de Horarios Semanales
1. Admin accede al calendario semanal de la clase.
2. Define días y horarios (ej: lunes y miércoles 18:00–19:00).
3. Sistema verifica que no haya conflicto de sala.
4. Guarda horarios → muestra en calendario consolidado.

#### Flujo Alternativo — Edición de Clase
1. Admin selecciona clase → 'Editar'.
2. Sistema carga datos actuales.
3. Admin modifica campos.
4. Si se modifica horario → re-valida conflictos de sala.
5. Si hay socios inscriptos y cambia horario → notifica automáticamente a afectados.
6. Guarda cambios y actualiza calendario.

#### Flujo Alternativo — Cancelación de Clase
1. Admin → 'Cancelar clase' en instancia del calendario.
2. Sistema solicita confirmación y motivo.
3. Cancela instancia, libera cupos, notifica socios inscriptos.

#### Flujos de Excepción
- **E1 — Conflicto de sala:** "La sala ya está ocupada en ese horario." Bloquea guardado.
- **E3 — Cupo máximo inválido:** Rechaza valores < 1 o no numéricos.

#### Criterios de Aceptación
- CA-25: Clase aparece en portal de socios inmediatamente tras ser creada.
- CA-27: Al cancelar clase, todos los inscriptos reciben notificación.
- CA-28: Cupo no puede quedar por debajo de inscripciones activas al editar.

---

### CU-07 — Gestión de Empleados y Profesores

**Actores:** Administrador
**RF:** RF-12, RF-13, RF-14
**Necesidades:** N-08

#### Flujo Principal — Alta de Empleado/Profesor
1. Admin → módulo 'Empleados y Profesores' → 'Nuevo'.
2. Formulario: nombre, correo, teléfono, rol (Empleado / Profesor), espacio asignado (Gimnasio / Espacio Mora / Ambos).
3. Admin completa y confirma.
4. Sistema crea cuenta con credenciales temporales → envía al correo instrucciones para contraseña y activar MFA.
5. Registra con estado 'Activo' + log de auditoría.

#### Flujo Alternativo — Asignación de Clases a Profesor
1. Admin accede al perfil del profesor → 'Gestionar clases asignadas'.
2. Sistema muestra clases disponibles en el espacio del profesor.
3. Admin asigna o desasigna clases.
4. Profesor solo verá y gestionará clases asignadas.

#### Flujo Alternativo — Baja Lógica de Empleado
1. Admin → 'Dar de baja'.
2. Sistema solicita confirmación.
3. Desactiva cuenta (estado 'Inactivo'), revoca acceso, registra en log.
4. Si tenía clases asignadas → alerta al admin para reasignar.

#### Flujos de Excepción
- **E1 — Correo duplicado:** "Ya existe un usuario registrado con ese correo." Bloquea alta.
- **E2 — Baja con clases activas:** "Este profesor tiene X clases activas. Reasigná antes o confirmá la baja."
- **E3 — Error envío credenciales:** Registra error; admin puede reenviar manualmente.

#### Criterios de Aceptación
- CA-29: Sistema envía credenciales temporales al correo al dar de alta.
- CA-30: Profesor dado de baja no puede autenticarse.
- CA-31: Profesor solo visualiza y gestiona clases asignadas.
- CA-32: Alerta al admin si da de baja profesor con clases activas.

---

### CU-08 — Pago de Cuota Online mediante Mercado Pago

**Actores:** Socio, Sistema (Mercado Pago — actor externo)
**RF:** RF-21, RF-05, RF-06, RF-07
**Necesidades:** N-02, N-03, N-05

**Precondiciones:**
1. Socio autenticado en el portal.
2. Socio tiene al menos una cuota con estado "Próxima a vencer" o "Vencida".
3. Integración con API de Mercado Pago activa y configurada con credenciales válidas.
4. Ambiente de testing en nube operativo para recibir webhooks.

**Postcondiciones:**
- Pago registrado con número de transacción MP.
- Estado de cuota actualizado a "Al día".
- Socio recibe confirmación por correo electrónico.
- Admin visualiza el cambio en el dashboard en tiempo real.
- Operación registrada en log de auditoría.

#### Flujo Principal — Pago Exitoso
1. Socio accede a su perfil → visualiza estado de cuota ("Próxima a vencer" o "Vencida").
2. Socio selecciona "Pagar cuota" → sistema muestra monto según plan activo.
3. Socio confirma → sistema genera preferencia de pago vía API de Mercado Pago (Checkout Pro).
4. Sistema redirige al Checkout Pro de Mercado Pago.
5. Socio completa el pago en la plataforma de Mercado Pago (tarjeta, transferencia u otro medio).
6. Mercado Pago notifica al sistema el resultado de la transacción vía webhook.
7. Sistema valida autenticidad del webhook (firma HMAC) y procesa la notificación.
8. Sistema actualiza estado de cuota a "Al día", registra número de transacción MP, genera log de auditoría y envía confirmación por correo.
9. Socio es redirigido al portal con mensaje de pago confirmado y cuota actualizada.

#### Flujo Alternativo — Pago Pendiente (transferencia bancaria)
1. Socio selecciona medio de pago que queda en estado pendiente (ej. transferencia bancaria).
2. Mercado Pago notifica al sistema con estado "pending".
3. Sistema mantiene cuota en estado actual → muestra "Tu pago está siendo procesado. Te notificaremos cuando se confirme."
4. Cuando MP confirma el pago, el sistema recibe webhook de confirmación → ejecuta flujo principal desde paso 7.

#### Flujo Alternativo — Consulta de Historial de Pagos
1. Socio accede a su perfil → "Historial de pagos".
2. Sistema muestra pagos realizados con fecha, monto, medio de pago y número de transacción MP.

#### Flujos de Excepción
- **E1 — Pago rechazado:** MP notifica rechazo. Sistema mantiene estado de cuota sin cambios. "Tu pago fue rechazado. Intentá con otro medio de pago o contactá al gimnasio."
- **E2 — Webhook no recibido / timeout:** Sistema implementa reconciliación consultando estado de transacción en API de MP a los 5 y 30 minutos. Si confirma → actualiza. Si no → alerta al admin en dashboard.
- **E3 — Firma de webhook inválida:** Sistema descarta notificación y registra intento en log de auditoría como evento sospechoso.
- **E4 — Cuota ya pagada:** Sistema detecta cuota "Al día" → "Tu cuota ya se encuentra al día." Sin procesar nuevo pago.
- **E5 — Error de conexión con MP:** "No es posible procesar el pago en este momento. Intentá más tarde." Sin generar preferencia de pago.

#### Criterios de Aceptación
- CA-33: Socio puede iniciar flujo de pago desde su perfil si cuota próxima a vencer o vencida.
- CA-34: Tras pago exitoso confirmado por webhook, estado de cuota se actualiza a "Al día" sin intervención manual del admin.
- CA-35: Socio recibe correo de confirmación dentro de los 2 minutos posteriores al pago exitoso.
- CA-36: Webhook con firma inválida es descartado y no modifica datos del sistema.
- CA-37: Pago rechazado no modifica estado de cuota; socio ve mensaje de error claro.
- CA-38: Admin visualiza cambio de estado de cuota en dashboard en máximo 30 segundos tras confirmación del pago.

---

## 9. Plan de Iteraciones (Fase de Construcción)

### Iteración 1 — Base del Sistema, Seguridad, Multi-Espacio y Gestión Inicial de Socios
**Fechas:** 15/04/2026 → 29/04/2026 (~60 horas)
**Prioridad:** OBLIGATORIO

**Requerimientos:**
- RF-01 (Registrar socio con consentimiento Ley 18.331)
- RF-02 (Listar socios con búsqueda y filtros)
- RF-03 (Editar socio con log de auditoría)
- RF-04 (Baja lógica de socio)
- RF-20 (Gestión unificada multi-espacio)
- RF-22 (Plan por unidad de negocio)
- RNF-05 (Almacenamiento seguro, contraseñas cifradas)
- RNF-08 (Arquitectura multi-unidad)
- RNF-09 (Cumplimiento Ley 18.331)
- RNF-11 (Log de auditoría)

**Resultado esperado:** Primera versión operativa donde el admin puede iniciar sesión y registrar, listar, editar y dar de baja socios, contemplando ambos espacios. Estructura de Clean Architecture establecida. Modelo de datos con estrategia de herencia definida. Pipeline CI/CD configurado.

**Dependencias:** Fase de Elaboración completada.

---

### Iteración 2 — Portal Básico del Socio y Control de Cuotas
**Fechas:** 30/04/2026 → 14/05/2026 (~60 horas)
**Prioridad:** OBLIGATORIO

**Requerimientos:**
- RF-05 (Ver perfil del socio)
- RF-06 (Recordatorios automáticos de cuota)
- RF-07 (Control de estado de cuota)
- RNF-01 (Autenticación y autorización por roles — parte interna: empleados con email+password, roles dinámicos)
- RNF-03 (Responsive)
- RNF-07 (Compatibilidad navegadores)

**Resultado esperado:** El socio puede ver su perfil; admin controla estado de cuota; sistema envía recordatorios automáticos por email.

**Dependencias:** Iteración 1.

---

### Iteración 3 — Gestión de Clases y Horarios
**Fechas:** 15/05/2026 → 29/05/2026 (~60 horas)
**Prioridad:** OBLIGATORIO

**Requerimientos:**
- RF-08 (Gestionar clases: crear, editar, eliminar)
- RF-09 (Gestionar horarios semanales)

**Resultado esperado:** Admin puede crear, editar y eliminar clases; definir horarios y cupos por espacio.

**Dependencias:** Iteración 1.

---

### Iteración 4 — Inscripción a Clases, Empleados y Profesores (unificada)
**Fechas:** 30/05/2026 → 13/06/2026 (~60 horas)
**Prioridad:** OBLIGATORIO

**Requerimientos:**
- RF-10 (Inscripción a clase con cupo + cuota)
- RF-11 (Ver mis clases)
- RF-12 (Gestionar empleados y profesores)
- RF-13 (Profesor registra socios)
- RF-14 (Profesor gestiona sus clases)
- RNF-03 (Responsive)
- RF-19 (Sitio web público)

**Nota:** La funcionalidad base de RF-10, RF-11, RF-12 y RF-13 fue implementada en iteraciones anteriores. RF-14 queda contemplado por roles y permisos configurables desde interfaz, permitiendo crear un rol Profesor con permisos especificos por modulo. Esta iteracion se enfoca en mejoras, pulido de UX y testing.

**Resultado esperado:** Modulos de inscripcion a clases y gestion de empleados/profesores estabilizados, con mejoras de UX y validaciones. Los permisos del profesor se administran mediante roles configurables.

**Dependencias:** Iteraciones 2 y 3.

---

### Iteración 5 — Autenticación Avanzada (MFA, OAuth y Rol Dueño)
**Fechas:** 14/06/2026 → 28/06/2026 (~40 horas)
**Prioridad:** DESEABLE/OPCIONAL

**Requerimientos:**
- RNF-10 (MFA TOTP para admin/profesor, OAuth 2.0 Google para socios)
- RNF-01 (Autenticación y autorización por roles — parte socios: Google OAuth + rol Dueño)
- RF-15 (Gestionar eventos)

**Resultado esperado:** Admin y profesor requieren MFA (TOTP) para iniciar sesión. Socios se autentican con Google OAuth 2.0 sin gestionar contraseña propia. Rol "Dueño" hardcoded con filtro por unidades asignadas.

**Dependencias:** Iteraciones 1 y 4.

---

### Iteración 6 — Eventos, Notificaciones, Rutinas y Sitio Web Público
**Fechas:** 29/06/2026 → 13/07/2026 (~60 horas)
**Prioridad:** DESEABLE

**Requerimientos:**
- RF-16 (Notificaciones al socio por email)
- RNF-03 (Responsive)
- RNF-04 (SEO: semántica, metaetiquetas, URLs amigables, carga rápida)
- RNF-06 (Disponibilidad ≥ 95%)
- RF-18 (Dashboard en tiempo real multi-espacio)
- RF-21 (Pago de cuotas online con Mercado Pago)
- RNF-02 (Actualización sin recarga vía SSE)

**Resultado esperado:** Admin crea eventos; sistema notifica socios por eventos, cambios de horario y recordatorios de cuota. Socio puede crear rutinas. Sitio web público con info institucional, responsive y SEO.

**Dependencias:** Iteraciones 2 y 3.

---

## 10. Alcance Negativo (Lo que NO se implementa)

- **NO** acceso automatizado (QR/molinete).
- **NO** tienda de productos.
- **NO** programa de fidelización (puntos/premios).
- **NO** migración de datos desde SmartGym.
- **NO** mantenimiento/hosting posterior a la entrega.
- **NO** aplicación móvil nativa (es web responsive).
- El sistema se entrega con **datos de prueba** (no datos personales reales).

---

## 11. Vistas del Frontend (4 vistas diferenciadas)

1. **Página Web Pública**: accesible sin registro, SEO, captación de socios.
2. **Portal de Socios**: horarios, inscripción a clases, perfil, rutinas.
3. **Panel de Administración**: dashboard, gestión de socios, clases, cuotas, eventos, filtrado multi-espacio.
4. **Vista de Profesores**: gestión de clases asignadas, registro de socios.

---

## 12. Sistema de Roles — Diseño Actual y Evolución (It.5)

### Estado actual (implementado en It.2)
- **Admin** y **Socio** son roles hardcoded (`EsSistema = true`).
- Los demás roles (ej. Profesor) se crean por interfaz por el Admin.
- Permisos granulares por módulo × operación CRUD.
- Spec completo: [[spec-rnf01-roles-y-permisos]]

### Rol "Dueño" — Planificado para Iteración 5 (RNF-01)

**Motivación:** El Admin representa al equipo de desarrollo/soporte técnico. Maurice (el cliente) necesita un rol que le permita operar su negocio sin ser Admin global del sistema. El sistema es multi-compañía: a futuro podría haber múltiples Dueños con distintas unidades.

**Jerarquía:**
```
Admin (EsSistema=true) — ve TODO, crea Dueños, configuraciones globales
  └── Dueño (EsSistema=true) — opera SUS unidades, crea roles y empleados
       └── [Roles dinámicos] — Profesor, Recepcionista, etc.
            └── Permisos granulares por módulo × operación
Socio (EsSistema=true) — portal de socios, lógica de ownership
```

**Decisiones de diseño:**
| Decisión | Resolución |
|----------|-----------|
| Quién crea Dueños | Solo el Admin |
| Unidades del Dueño | Un Dueño puede tener 1 o más unidades asignadas |
| Roles creados por un Dueño | Visibles para todas las unidades del Dueño que los creó |
| El Dueño puede crear otros Dueños | No |
| Módulo de Auditoría | Solo visible para Admin |
| Filtro de unidad en sesión | Admin elige qué unidades ver al loguearse (o "todas"). Dueño solo ve sus unidades asignadas |
| Configuraciones futuras exclusivas de Admin | A priori no las hay; se reserva la posibilidad |

**Cambios técnicos requeridos (It.5):**
1. Seed del rol `Dueño` con `EsSistema = true`.
2. Relación `Usuario ↔ Unidad` para Dueños (qué unidades tiene asignadas).
3. Filtro de unidad en el contexto de sesión (JWT o claim adicional con `unidadesActivas[]`).
4. Todos los endpoints con datos de unidad aplican filtro automático según rol: Admin ve todo, Dueño ve solo sus unidades.
5. Endpoint de gestión de roles accesible también por Dueño (CRUD de roles, limitado a roles que él creó).
6. El Dueño puede crear empleados/usuarios dentro de sus unidades.

---

## 13. Deuda Técnica Planificada

| Tema | Descripción | Prioridad |
|------|-------------|-----------|
| **Migración a .NET 10** | El proyecto se desarrolla sobre .NET 8. Se planifica migrar a .NET 10 (LTS) cuando esté disponible como versión estable, aprovechando mejoras de rendimiento y soporte a largo plazo. | Media |
