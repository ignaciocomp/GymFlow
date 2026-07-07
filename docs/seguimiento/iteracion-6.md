---
title: DOCUMENTACION ITERACIÓN 6 FASE DE CONSTRUCCIÓN
tags:
  - seguimiento
related:
  - "[[seguimiento_index]]"
  - "[[spec-rf21-mercadopago]]"
  - "[[plan-rf21-mercadopago]]"
  - "[[spec-rf19-sitio-publico]]"
  - "[[plan-rf19-sitio-publico]]"
  - "[[spec-rf18-dashboard]]"
  - "[[plan-rf18-dashboard]]"
  - "[[CU-10-dashboard-tiempo-real]]"
  - "[[CU-11-pago-online-mercadopago]]"
---

# DOCUMENTACION ITERACIÓN 6 FASE DE CONSTRUCCIÓN

**Iteración 6 --- Fase de Construcción (29/06/2026 -- 13/07/2026)**
**Prioridad:** DESEABLE

## Descripción general

La sexta iteración está dedicada al **pago online de cuotas con Mercado Pago** (RF-21) y al **dashboard operativo en tiempo real** (RF-18 + RNF-02), además de cerrar los requerimientos no funcionales de **responsive** (RNF-03), **SEO** (RNF-04) y **disponibilidad** (RNF-06).

## Tareas planificadas


Funcionalidades a implementar:

- **RF-21** — Pago de cuotas online con Mercado Pago (Checkout Pro + confirmación por webhook).
- **RF-18** — Dashboard en tiempo real multi-espacio.

Requerimientos no funcionales:

- **RNF-02** — Actualización del dashboard sin recarga de página (SSE).
- **RNF-03** — Interfaz responsive (cubierto por el sitio público, adelantado).
- **RNF-04** — SEO: semántica, metaetiquetas, URLs amigables, carga rápida (cubierto por el sitio público, adelantado).
- **RNF-06** — Disponibilidad ≥ 95%.

Tareas técnicas de base:

- Nueva entidad de **pago** en la base de datos, separada de la cuota: una cuota puede tener varios intentos de pago, cada uno con su estado (pendiente, aprobado o rechazado), medio de pago, número de transacción de Mercado Pago y fecha de acreditación. Migración de base de datos correspondiente.
- Servicio de integración con Mercado Pago: crea la "preferencia de pago" del checkout, consulta el estado real de un pago en la pasarela y valida la firma de las notificaciones entrantes.
- Endpoint público de **webhook** para recibir las notificaciones de Mercado Pago, con validación de firma mediante clave secreta compartida (RN-31). Soporta además el formato de notificación antiguo de la pasarela (IPN), que no trae firma: en ese caso la seguridad se preserva consultando siempre el estado real del pago a la API de Mercado Pago con las credenciales propias.
- Construcción centralizada de las URLs de retorno del checkout (éxito / error / pendiente) y de la URL de notificación, pidiéndole a Mercado Pago el formato de notificación moderno (firmado).
- Credenciales de la pasarela gestionadas como **secrets** (nunca en el repositorio): workflow manual que las carga en el contenedor de producción, análogo al ya existente para el email.

## Configuración de Mercado Pago

Para operar la pasarela, GymFlow está registrado como aplicación en Mercado Pago. Durante la construcción se trabaja en **modo prueba (sandbox)**: se usan las "credenciales de prueba" de la aplicación, un usuario comprador de prueba y tarjetas de test que simulan pagos aprobados o rechazados, sin mover dinero real.

Aspectos a destacar:

- Las dos credenciales (token de acceso y clave secreta del webhook) viven como secrets de GitHub y del contenedor en Azure; un workflow manual las inyecta como variables de entorno. No están en el repositorio ni en los logs.
- El webhook de Mercado Pago está configurado apuntando a la URL pública de la API (`.../api/pagos/webhook`). Como la pasarela necesita una URL accesible desde internet para notificar, la prueba de punta a punta se hace contra el despliegue en Azure.
- Al pasar a producción real solo hay que reemplazar las credenciales de prueba por las productivas y volver a correr el workflow.

## ¿Qué se implementó?

Funcionalidades implementadas:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RF-21 | CU-11 — Pago online con Mercado Pago | Completado | El socio paga su cuota pendiente desde "Mis Cuotas" con el botón "Pagar con Mercado Pago", que lo lleva al checkout de la pasarela. El monto lo fija el sistema según el plan del socio (RN-32). La cuota se marca "Pagada" únicamente cuando la pasarela lo confirma por webhook auténtico; entonces se registra la transacción, se audita y se envía el email de confirmación (se reutiliza la misma plantilla del pago manual). Incluye páginas de resultado (aprobado / rechazado / pendiente) e historial de pagos del socio con fecha, plan, monto, medio, número de transacción y estado. |
| RF-18 | CU-10 — Dashboard en tiempo real | Completado | Dashboard operativo de solo lectura con la vista consolidada de ambas sedes por defecto (RN-14) y filtro por unidad: socios activos (total y por sede), cuotas próximas a vencer (ventana de 5 días), cuotas vencidas y pagadas del mes —calculadas en vivo, RN-17—, clases del día con cupo e inscriptos, últimas inscripciones y una gráfica con selector de vista (socios por sede, cuotas por estado, inscripciones de los últimos 7 días; la elección se recuerda en el navegador). El acceso se controla con un nuevo módulo **Dashboard** en el sistema de permisos (RN-16): se sembró el permiso de lectura para Admin y Dueño, es otorgable a roles custom desde el formulario de roles, y el Dueño solo ve sus unidades asignadas (el filtrado se resuelve en el servidor). Para los roles con permiso, el dashboard pasa a ser la pantalla de inicio del panel de administración. |

Requerimientos no funcionales implementados:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RNF-02 | Dashboard sin recarga (SSE) | Completado | El dashboard se actualiza sin recargar la página mediante **Server-Sent Events**: el backend recalcula el snapshot cada ~10 segundos y lo emite solo si cambió (si no, envía un latido para mantener viva la conexión), lo que garantiza una antigüedad máxima de 30 segundos respecto de la base de datos (RN-15). Como `EventSource` no permite enviar el token de autenticación por header, el stream se consume con `fetch` leyendo la respuesta como flujo. Si la conexión se corta, el frontend reintenta con espera creciente y, si no logra reconectar, degrada a un refresco periódico cada 15 segundos mostrando el indicador "Actualización en pausa" en lugar de "En vivo"; los datos nunca quedan sin marca de antigüedad (se muestra la hora de la última actualización). |
| RNF-05 | Seguridad — Webhook de pagos | Completado | La notificación de Mercado Pago se valida con una firma calculada sobre una clave secreta compartida; si la firma es inválida se descarta sin tocar datos y se audita como evento sospechoso (RN-31, CA-36). Además, nunca se confía en el contenido de la notificación: el estado real del pago siempre se consulta a la API de Mercado Pago con las credenciales propias. El procesamiento es idempotente: un reintento de la pasarela sobre una cuota ya pagada no la reprocesa ni re-envía el email. Para el formato antiguo de notificación (IPN, sin firma) rige la misma regla de consultar el estado real: un identificador forjado no existe en la pasarela o no corresponde a ningún pago propio, y se ignora. |
| RNF-11 | Auditoría | Completado | Se audita la confirmación del pago (cuota marcada como pagada vía Mercado Pago, con plan y socio) y todo webhook con firma inválida queda registrado como evento sospechoso. Cada request al webhook deja rastro en los logs del servidor con su formato y resultado. |
| RNF-03 | Responsive | Completado *(adelantado)* | Cubierto por el sitio web público (RF-19), entregado el 18/06/2026, antes del inicio de esta iteración: cinco páginas institucionales mobile-first con menú colapsable. Las pantallas nuevas del portal de esta iteración (historial de pagos, resultado de pago) también tienen su variante mobile. |
| RNF-04 | SEO | Completado *(adelantado)* | Cubierto por el sitio web público (RF-19): título y descripción propios por página, etiquetas Open Graph, mapa del sitio, robots.txt, HTML semántico e idioma declarado en español. |

## Tareas pendientes

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RF-16 (canal email) | — | Pendiente (arrastrado de IT5) | Unificación del canal email con el centro de notificaciones in-system. Los emails ya se envían; falta que pasen por la misma infraestructura que maneja la campanita del portal. Sin avances en esta iteración. |
| RNF-06 | — | En medición | Disponibilidad ≥ 95% del despliegue en Azure. Resultado de la medición del período: [COMPLETAR]. |
| Pruebas Postman de la iteración | — | Pendiente | La colección de Postman todavía no incluye tests de los endpoints de pagos ni del snapshot del dashboard (ver sección de pruebas de API). |

## Pantallas implementadas

**Pantalla Mis Cuotas — botón "Pagar con Mercado Pago" (portal del socio)**

*(captura de pantalla)*

**Ruta:** /portal/mis-cuotas

**Descripción:** En las cuotas en estado pendiente, el botón "Pagar (próximamente)" que estaba deshabilitado desde iteraciones anteriores se reemplaza por **"Pagar con Mercado Pago"**, tanto en la tabla de escritorio como en las tarjetas de la vista mobile. Al hacer clic, el sistema crea la preferencia de pago y redirige al checkout de Mercado Pago. Si la pasarela no responde, se muestra el mensaje "No se pudo iniciar el pago. Intentá de nuevo en unos minutos." sin salir del portal. Las cuotas ya pagadas no ofrecen el botón.

**Pantalla Resultado del pago (portal del socio)**

*(captura de pantalla)*

**Ruta:** /portal/pago/resultado

**Descripción:** Página a la que Mercado Pago redirige al socio al terminar el checkout, con tres variantes según el resultado: **aprobado** ("¡Pago confirmado!", con la aclaración de que la cuota puede tardar unos segundos en reflejarse mientras se confirma la operación), **rechazado** ("El pago fue rechazado", con invitación a reintentar desde Mis Cuotas) y **pendiente** ("Pago en proceso", para medios de pago que demoran la acreditación). En el caso aprobado se refresca automáticamente el listado de cuotas. Ofrece accesos a "Mis cuotas" y "Mis pagos".

**Pantalla Mis Pagos — historial (portal del socio)**

*(captura de pantalla)*

**Ruta:** /portal/mis-pagos

**Descripción:** Nueva sección del portal (con su entrada en el menú de navegación del socio) que lista el historial de pagos realizados con Mercado Pago: fecha, plan, monto, medio de pago, número de transacción de la pasarela y estado (Aprobado / Rechazado / Pendiente) con código de color. En escritorio se muestra como tabla y en mobile como tarjetas apiladas.

**Pantalla Dashboard en tiempo real (panel de administración)**

*(captura de pantalla)*

**Ruta:** /admin/dashboard

**Descripción:** Nueva pantalla de inicio del panel de administración para los roles con permiso de lectura sobre el módulo Dashboard (los demás siguen aterrizando en Socios y no ven el ítem en el menú). Muestra cuatro tarjetas de métricas —socios activos con desglose por sede, cuotas próximas a vencer (5 días), cuotas vencidas (con las pagadas del mes como dato secundario) y clases de hoy—, una gráfica "Actividad" con selector de vista (socios por sede, cuotas por estado o inscripciones de los últimos 7 días; la vista elegida se recuerda entre sesiones), la lista de clases del día con horario e inscriptos sobre cupo, y las últimas inscripciones. Arriba a la derecha, el selector de sede ("Todas las sedes" por defecto) recalcula todas las métricas, y el indicador de estado muestra "En vivo" con la hora de la última actualización, o "Actualización en pausa" si la conexión de tiempo real cayó y se pasó al refresco periódico.

## Estructura de API --- endpoints implementados

| **Método** | **Endpoint** | **Descripción** | **Auth** |
|-|-|-|-|
| POST | `/api/pagos/iniciar` | El socio inicia el pago online de una de sus cuotas pendientes; devuelve la URL del checkout de Mercado Pago a la que el frontend redirige. Rechaza cuotas inexistentes, de otro socio o ya pagadas | Socio (portal) |
| POST | `/api/pagos/webhook` | Recibe la notificación de pago de Mercado Pago, en formato moderno (firmado) o IPN antiguo (sin firma, con consulta del estado real a la pasarela). Responde 401 solo ante firma inválida; en el resto de los casos responde 200 para que la pasarela no reintente | Anónimo (protegido por firma) |
| GET | `/api/pagos/mis-pagos` | Historial de pagos del socio autenticado, ordenado del más reciente al más antiguo | Socio (portal) |
| GET | `/api/dashboard?unidadId=` | Snapshot del dashboard (métricas, clases del día, inscripciones recientes y series de la gráfica), consolidado o filtrado por unidad. Se usa en la carga inicial y como refresco de respaldo; una unidad fuera de las visibles del usuario se rechaza (403) | Permiso Dashboard (Lectura) |
| GET | `/api/dashboard/stream?unidadId=` | Stream **SSE** del dashboard: recalcula el snapshot cada ~10 segundos y lo emite solo si cambió (si no, envía un latido). El permiso y la unidad se validan antes de iniciar el stream | Permiso Dashboard (Lectura) |

## Casos de uso extendidos --- Iteración 6

### CU-10: Dashboard en tiempo real multi-espacio

| *Campo* | |
|-|-|
| *Nombre* | Dashboard operativo en tiempo real con filtros por unidad |
| *Actor principal* | Administrador / Dueño (acotado a sus unidades). En general, cualquier rol con permiso de lectura sobre el módulo Dashboard. |
| *Precondición* | Actor autenticado con permiso de lectura sobre el módulo Dashboard (sembrado para Admin y Dueño; otorgable a roles custom desde el formulario de roles). |
| *Postcondición* | El actor visualiza métricas operativas consolidadas y actualizadas (antigüedad máxima de 30 segundos respecto de la BD, RN-15) sin recargar la página. No modifica datos: el dashboard es de solo lectura (CA-05). |

**Flujo principal --- Cargar y observar el dashboard:**

1. El actor entra al panel de administración: si su rol tiene el permiso, el **dashboard es la pantalla de inicio** (también accesible desde el ítem "Dashboard" del menú lateral).
2. El sistema verifica el permiso de lectura sobre el módulo Dashboard (RN-16) y resuelve en el servidor las unidades visibles del actor (el Dueño solo las suyas).
3. El sistema carga, por defecto, la **vista consolidada de todas las sedes** (RN-14): socios activos (total y por sede), cuotas próximas a vencer (pendientes con vencimiento dentro de los próximos 5 días), cuotas vencidas y pagadas del mes —calculadas en vivo sobre el estado actual, RN-17—, clases del día con cupo e inscriptos, las últimas 10 inscripciones y la gráfica de actividad con su selector de vista.
4. El frontend abre la conexión **SSE** contra el endpoint de stream (RNF-02). Como `EventSource` no permite enviar el token de autenticación, el stream se consume con `fetch` leyendo la respuesta como flujo.
5. El backend recalcula el snapshot cada ~10 segundos y lo emite **solo si cambió** respecto del último enviado; si no cambió envía un latido que mantiene viva la conexión. Así el panel refleja el estado real con una antigüedad máxima de 30 segundos, sin recarga.
6. El actor observa las métricas actualizándose, con el indicador "En vivo" y la hora de la última actualización.

**Flujo alternativo --- Filtrar por unidad:**

1. El actor selecciona una sede en el filtro (por defecto "Todas las sedes").
2. El sistema recalcula todas las métricas acotadas a esa unidad, en el servidor; el stream se reabre con el filtro aplicado.
3. El Dueño solo puede filtrar entre sus unidades asignadas: una unidad fuera de las visibles se rechaza en el servidor.
4. Volver a "Todas" restituye la vista consolidada.

**Flujos de excepción:**

- **E1 --- Actor sin permiso:** no ve el ítem "Dashboard" en el menú y su pantalla de inicio del panel sigue siendo Socios; un acceso directo por URL o por API es denegado.
- **E2 --- Caída de la conexión SSE:** el frontend reintenta con espera creciente y, si no logra reconectar, degrada a un refresco periódico cada 15 segundos: el indicador pasa a "Actualización en pausa" y los datos conservan su marca de antigüedad.
- **E3 --- Unidad sin datos (o Dueño sin unidades asignadas):** se muestran las métricas en cero, no un error.

**Desviaciones respecto del diseño original (CU-10 de diseño):**

- El mecanismo de tiempo real se concretó como **snapshot periódico con detección de cambios** sobre la conexión SSE (recalcular cada ~10 segundos y emitir solo si difiere), en lugar de push por eventos de dominio: más simple y suficiente para el requisito de ≤30 segundos.
- El stream SSE se consume con `fetch` en lugar del `EventSource` estándar, porque `EventSource` no permite el header de autenticación y poner el JWT en la URL está prohibido.
- La métrica "cuotas pendientes" del diseño se desglosó en **próximas a vencer** (ventana de 5 días) y **vencidas**, y se agregó **pagadas del mes** como dato complementario.
- Se agregó la **gráfica de actividad con selector de vista** (socios por sede, cuotas por estado, inscripciones de los últimos 7 días), no prevista en el diseño; la vista elegida se persiste en el navegador.
- De paso se corrigió un defecto preexistente del formulario de roles: no ofrecía el módulo Eventos, por lo que no se podía otorgar a roles custom; ahora ofrece Eventos y Dashboard, como exige RN-16.

### CU-11: Pago online de cuota con Mercado Pago

| *Campo* | |
|-|-|
| *Nombre* | Pago online de cuota del socio vía Mercado Pago (Checkout Pro) |
| *Actor principal* | Socio. Actores secundarios: Mercado Pago (pasarela) y el Sistema (procesa la notificación de pago). |
| *Precondición* | Socio autenticado en el portal con al menos una cuota en estado Pendiente. Credenciales de Mercado Pago configuradas y webhook apuntando a la URL pública de la API. |
| *Postcondición* | Si el pago se aprueba y la notificación es auténtica: la cuota pasa a "Pagada", queda registrado el pago (número de transacción, medio, fecha de acreditación), se audita y se envía el email de confirmación. Si el pago es rechazado o queda pendiente: la cuota no cambia de estado y el socio conserva el acceso al portal. |

**Flujo principal --- Pago aprobado:**

1. El socio accede a "Mis Cuotas" en el portal y, sobre una cuota pendiente, hace clic en **"Pagar con Mercado Pago"**.
2. El sistema valida que la cuota exista, sea del socio autenticado y esté pendiente, y registra un **intento de pago** propio (en estado pendiente). El monto lo determina el sistema según el plan del socio; el socio no puede modificarlo (RN-32).
3. El sistema crea la preferencia de pago en Mercado Pago —usando el identificador del intento como referencia para reconciliar después— y redirige al socio al checkout de la pasarela.
4. El socio completa el pago en Mercado Pago.
5. Mercado Pago notifica el resultado al endpoint de webhook del sistema.
6. El sistema **valida la autenticidad de la notificación** con la clave secreta compartida (RN-31) y, superada esa validación, **consulta el estado real del pago a la API de Mercado Pago** — nunca confía en el contenido de la notificación.
7. Con el pago confirmado como aprobado, el sistema reconcilia la notificación con su intento de pago por la referencia, marca la cuota como **"Pagada"** y el intento como **aprobado** (guardando número de transacción, medio de pago y fecha de acreditación) en una única operación atómica, y registra la transacción en auditoría.
8. El sistema envía al socio el email de confirmación de pago (misma plantilla que el pago manual; envío best-effort: una falla del email no revierte el pago).
9. Mercado Pago redirige al socio a la página de resultado del portal ("¡Pago confirmado!") y la cuota aparece actualizada en "Mis Cuotas". El pago queda visible en el historial "Mis Pagos".

**Flujo alternativo --- Pago rechazado:**

1. Mercado Pago informa el pago como rechazado.
2. El sistema valida la notificación, consulta el estado real y marca el intento de pago como **rechazado**, sin modificar el estado de la cuota ni bloquear el acceso del socio (RN-34).
3. El socio ve la página "El pago fue rechazado" y puede reintentar desde Mis Cuotas: cada reintento genera un nuevo intento de pago sobre la misma cuota.

**Flujo alternativo --- Pago pendiente:**

1. Mercado Pago informa el pago como pendiente (por ejemplo, medios de pago que demoran la acreditación).
2. El sistema no modifica nada: la cuota queda en su estado actual hasta que llegue la aprobación definitiva, momento en que se ejecuta el flujo principal desde el paso 6.
3. El socio ve la página "Pago en proceso".

**Flujo alternativo --- Notificación en formato antiguo (IPN):**

1. Mercado Pago envía la notificación en su formato legado, que no incluye firma validable.
2. El sistema la procesa por un camino específico que omite la validación de firma pero **consulta igualmente el estado real del pago a la API de Mercado Pago** con sus propias credenciales, y solo actúa si la referencia corresponde a un intento de pago propio. Un identificador forjado no existe en la pasarela o no mapea a ningún pago del sistema, y se ignora.

**Flujos de excepción:**

- **E1 --- Cuota no pagable:** si la cuota no existe, es de otro socio o ya está pagada/anulada, el sistema no inicia el pago y responde con el error correspondiente. En la interfaz, las cuotas pagadas no ofrecen el botón.
- **E2 --- Notificación con firma inválida:** se descarta sin alterar ningún dato y se audita como evento sospechoso (RN-31, CA-36). Es el único caso en que el webhook responde con rechazo (401).
- **E3 --- Notificación duplicada / reintento de Mercado Pago:** el procesamiento es idempotente; si la cuota ya está pagada no se reprocesa ni se envía el email de nuevo.
- **E4 --- Falla del email de confirmación:** no revierte el pago; la cuota queda "Pagada" igual.
- **E5 --- Error de conexión con Mercado Pago al iniciar el pago:** el socio ve "No se pudo iniciar el pago. Intentá de nuevo en unos minutos." y no queda redirigido.
- **E6 --- Socio abandona el checkout:** la cuota permanece sin cambios; el intento de pago queda registrado como pendiente y no afecta reintentos posteriores.

**Desviaciones respecto del diseño original (CU-11 de diseño):**

- La referencia de reconciliación con la pasarela es el **intento de pago** (no la cuota): una cuota puede acumular varios intentos (rechazados, abandonados) y cada uno queda registrado con su estado, lo que habilita el historial de pagos.
- Se agregó el soporte del formato de notificación antiguo (IPN) de Mercado Pago, no previsto en el diseño, porque la pasarela puede enviar ese formato según la configuración del webhook; la regla de seguridad de no confiar nunca en la notificación se mantiene en ambos formatos.
- La condición de "cuota próxima a vencer o vencida" (RN-30) se implementó como "cuota en estado Pendiente" (el estado que agrupa ambas situaciones en el sistema); no se pueden pagar cuotas ya pagadas ni anuladas.

## Reuniones con el cliente

[COMPLETAR]

Funcionalidades a presentar en la próxima reunión:

- Pago online de una cuota con Mercado Pago de punta a punta (checkout de prueba, cuota que pasa a "Pagada", email de confirmación).
- Historial de pagos del socio en el portal.
- Dashboard en tiempo real con las métricas consolidadas de ambas sedes, el filtro por unidad y la actualización en vivo.

## Pruebas automatizadas

Suite backend en verde (0 fallos). Cobertura agregada en esta iteración:

**Pago online con Mercado Pago (RF-21):**

- *Dominio:* el intento de pago nace en estado pendiente; un monto negativo es rechazado; las transiciones de estado son estrictas (no se puede aprobar dos veces, ni rechazar un pago aprobado, ni aprobar uno rechazado); al aprobar quedan registrados el número de transacción, el medio de pago y la fecha de acreditación.
- *Application:* el inicio del pago crea el intento, usa su identificador como referencia para la pasarela y devuelve la URL del checkout; rechaza cuotas inexistentes, de otro socio o ya pagadas; si la pasarela falla al crear la preferencia, el error se propaga de forma controlada. El procesamiento del webhook: con firma inválida no consulta la pasarela ni toca datos, audita el evento sospechoso y devuelve el rechazo; con pago aprobado marca la cuota pagada y el intento aprobado en un único guardado atómico, audita y envía el email; si la cuota ya estaba pagada es idempotente y no re-envía el email; con pago rechazado marca solo el intento, la cuota no cambia; estados no terminales (pendiente) no producen cambios; referencias desconocidas o malformadas se ignoran; el camino IPN procesa sin firma pero consultando el estado real, y un identificador forjado se ignora sin tocar datos; una falla del email no impide confirmar el pago. El historial mapea los datos del pago con su plan y respeta el orden por fecha descendente. A nivel del endpoint se verifica el contrato HTTP completo: 401 solo ante firma inválida y 200 en el resto, lectura de la firma y del identificador desde donde la pasarela los manda, soporte de ambos formatos de notificación, descarte de notificaciones que no son de pagos, y que el webhook es el único acceso anónimo del módulo.
- *Infrastructure:* la validación de la firma acepta el valor correcto (sin distinguir mayúsculas) y rechaza firmas alteradas, encabezados malformados o ausentes y la configuración sin clave secreta, incluyendo las variantes del manifiesto con y sin identificador de request; el repositorio de pagos cubre alta, búsqueda por referencia externa, pagos por cuota e historial por socio ordenado; el armado de URLs verifica las tres URLs de retorno del checkout y la URL de notificación con el pedido de formato moderno, con su fallback de configuración.
- *Frontend:* el botón "Pagar con Mercado Pago" aparece solo en cuotas pendientes, llama al endpoint de inicio y redirige al checkout; la página de resultado muestra el mensaje correcto según el estado recibido; el historial de pagos renderiza los datos del socio.

**Dashboard en tiempo real (RF-18):**

- *Application:* la consulta del dashboard consolida las métricas de todas las unidades visibles y las recalcula al filtrar por una sede; el Dueño queda restringido a sus unidades asignadas y pedir una unidad fuera de las permitidas se rechaza; un Dueño sin unidades asignadas recibe todas las métricas en cero en lugar de un error; las series de la gráfica se arman correctamente, incluida la serie de los últimos 7 días que completa con cero los días sin inscripciones. A nivel del endpoint se verifica que tanto el snapshot como el stream exigen el permiso de lectura del módulo Dashboard, que una unidad no visible responde con el rechazo (403) —en el stream, antes de iniciar la transmisión— y la lógica de "emitir solo si cambió" del stream, aislada en un helper puro: el primer snapshot se envía siempre y los siguientes solo cuando difieren del último enviado.
- *Infrastructure:* los agregados nuevos de los repositorios devuelven los conteos correctos con y sin filtro de unidad: cuotas pendientes dentro de la ventana de vencimiento, vencidas y pagadas del mes; horarios del día de la semana actual con sus inscriptos; últimas inscripciones ordenadas por fecha; e inscripciones activas por día para la serie de la gráfica. Los tests de seed existentes se actualizaron por el permiso nuevo: el rol Dueño pasa a contar el permiso de lectura sobre Dashboard.
- *Frontend:* el hook de tiempo real entrega el snapshot inicial, pasa a "en vivo" cuando el stream empieza a emitir datos y, si la conexión falla tras los reintentos, degrada al refresco periódico reportando que ya no está en vivo; la página renderiza las tarjetas y listas desde los datos, cambia la vista de la gráfica y la recuerda entre sesiones, muestra el indicador de pausa cuando corresponde y tolera métricas en cero sin error; el ítem "Dashboard" del menú lateral solo aparece con el permiso, el inicio del panel de administración redirige al dashboard o a Socios según el permiso del rol, y el formulario de roles ofrece los módulos Dashboard y Eventos.

## Pruebas de API realizadas con Postman

**PENDIENTE** — La colección `GymFlow API Tests.postman_collection.json` todavía no incorpora tests para los endpoints de esta iteración: los de pagos (`/api/pagos/iniciar`, `/api/pagos/webhook`, `/api/pagos/mis-pagos`) ni el snapshot del dashboard (`/api/dashboard`, incluyendo el control de permiso y el rechazo de una unidad no visible). Al agregarlos se documentarán aquí con las tablas de tests agregados/modificados y el resumen de aserciones, como en iteraciones anteriores.

> Nota: el flujo feliz del webhook requiere una notificación real firmada por Mercado Pago contra una URL pública, por lo que —igual que el login con Google en IT5— la colección podrá cubrir principalmente los caminos de error (firma inválida, cuota ajena, cuota ya pagada, sin autenticación) y el flujo completo se valida de punta a punta contra el despliegue (ver Prueba 6.1). El stream SSE del dashboard tampoco es testeable desde Postman (la conexión queda abierta); se cubre con las pruebas automatizadas y la prueba funcional 6.8.

## Pruebas funcionales de frontend

### Prueba 6.1 --- Pago aprobado de punta a punta (sandbox)

*(captura de pantalla)*

**Pasos:**

1. Loguear como socio con una cuota pendiente, ir a "Mis Cuotas".
2. Hacer clic en "Pagar con Mercado Pago".
3. En el checkout de Mercado Pago, pagar con el usuario comprador de prueba y una tarjeta de test de pago aprobado.

**Resultado esperado:** El socio vuelve a la página "¡Pago confirmado!". A los pocos segundos la cuota figura "Pagada" en Mis Cuotas, el pago aparece como "Aprobado" en Mis Pagos con su número de transacción, llega el email de confirmación y la auditoría registra la operación.

**Descripción:** Verifica el flujo feliz completo: creación de la preferencia, checkout, confirmación por webhook auténtico, actualización de la cuota, registro del pago, email y auditoría.

### Prueba 6.2 --- Pago rechazado

*(captura de pantalla)*

**Pasos:**

1. Repetir la Prueba 6.1 pero pagando con la tarjeta de test de pago rechazado.

**Resultado esperado:** El socio ve "El pago fue rechazado". La cuota sigue "Pendiente" y conserva el botón de pago; en Mis Pagos el intento figura como "Rechazado". El acceso al portal no se bloquea.

**Descripción:** Verifica que un rechazo no modifica el estado de la cuota (RN-34) y que el intento queda registrado para trazabilidad.

### Prueba 6.3 --- Pago pendiente

*(captura de pantalla)*

**Pasos:**

1. Repetir la Prueba 6.1 eligiendo un medio de pago que quede en estado pendiente (o simulando el retorno con estado pendiente).

**Resultado esperado:** El socio ve "Pago en proceso". La cuota no cambia de estado hasta que la pasarela confirme la aprobación.

**Descripción:** Verifica que solo la aprobación definitiva marca la cuota como pagada.

### Prueba 6.4 --- Cuota pagada no ofrece pago

*(captura de pantalla)*

**Pasos:**

1. Loguear como socio con una cuota ya pagada.
2. Revisar la fila/tarjeta de esa cuota en "Mis Cuotas".

**Resultado esperado:** La cuota pagada no muestra el botón "Pagar con Mercado Pago". Si se fuerza el pedido por API, el sistema lo rechaza.

**Descripción:** Verifica la regla de que no se pagan cuotas ya saldadas, en interfaz y en servidor.

### Prueba 6.5 --- Historial "Mis Pagos"

*(captura de pantalla)*

**Pasos:**

1. Con un socio que tenga pagos aprobados y rechazados, entrar a "Mis Pagos" desde el menú del portal.
2. Repetir en un viewport mobile.

**Resultado esperado:** El historial lista los pagos del más reciente al más antiguo con fecha, plan, monto, medio, número de transacción y estado con su color. En mobile se muestra como tarjetas apiladas legibles.

**Descripción:** Verifica el historial del socio y su variante responsive.

### Prueba 6.6 --- Abandono del checkout

*(captura de pantalla)*

**Pasos:**

1. Iniciar el pago de una cuota pendiente.
2. En el checkout de Mercado Pago, cerrar la pestaña o volver al portal sin pagar.

**Resultado esperado:** La cuota sigue "Pendiente" con su botón de pago disponible; se puede reintentar normalmente y el nuevo intento funciona.

**Descripción:** Verifica que abandonar el checkout no deja la cuota ni el portal en un estado inconsistente.

### Prueba 6.7 --- Error al iniciar el pago

*(captura de pantalla)*

**Pasos:**

1. Con la integración de Mercado Pago deshabilitada o sin conectividad con la pasarela, intentar pagar una cuota pendiente.

**Resultado esperado:** Se muestra el mensaje "No se pudo iniciar el pago. Intentá de nuevo en unos minutos." y el socio permanece en Mis Cuotas, sin redirección ni cambios en la cuota.

**Descripción:** Verifica el manejo controlado de la indisponibilidad de la pasarela (E5).

### Prueba 6.8 --- Dashboard en vivo y filtro por unidad

*(captura de pantalla)*

**Pasos:**

1. Loguear como Admin: el panel abre directamente en el dashboard con la vista consolidada de todas las sedes y el indicador "En vivo".
2. Desde otra ventana, registrar un cambio operativo (por ejemplo, una inscripción a una clase o un nuevo socio).
3. Observar el dashboard sin tocar nada.
4. Seleccionar una sede en el filtro y luego volver a "Todas las sedes".

**Resultado esperado:** El cambio del paso 2 se refleja en las métricas en menos de 30 segundos, sin recargar la página, y se actualiza la hora de "Actualizado". Al filtrar por sede, todas las métricas, listas y la gráfica se recalculan para esa unidad; al volver a "Todas" se restituye el consolidado.

**Descripción:** Verifica la actualización en tiempo real por SSE (RNF-02, RN-15), la vista consolidada por defecto (RN-14) y el recálculo por unidad (CA-01, CA-02, CA-03).

### Prueba 6.9 --- Acceso al dashboard controlado por permiso

*(captura de pantalla)*

**Pasos:**

1. Loguear con un usuario cuyo rol no tiene el permiso de lectura del módulo Dashboard.
2. Observar la pantalla de inicio del panel y el menú lateral; intentar entrar por URL directa a `/admin/dashboard`.
3. Como Admin, otorgar al rol el permiso de lectura sobre Dashboard desde el formulario de roles y repetir el ingreso.

**Resultado esperado:** Sin el permiso, el usuario aterriza en Socios, no ve el ítem "Dashboard" en el menú y el acceso directo es denegado. Con el permiso otorgado, el dashboard pasa a ser su pantalla de inicio y aparece en el menú.

**Descripción:** Verifica el control de acceso por el permiso del módulo (RN-16, CA-04) y que el permiso es otorgable a roles custom desde la interfaz.
