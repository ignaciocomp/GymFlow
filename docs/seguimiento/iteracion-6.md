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

Requerimientos no funcionales implementados:

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RNF-05 | Seguridad — Webhook de pagos | Completado | La notificación de Mercado Pago se valida con una firma calculada sobre una clave secreta compartida; si la firma es inválida se descarta sin tocar datos y se audita como evento sospechoso (RN-31, CA-36). Además, nunca se confía en el contenido de la notificación: el estado real del pago siempre se consulta a la API de Mercado Pago con las credenciales propias. El procesamiento es idempotente: un reintento de la pasarela sobre una cuota ya pagada no la reprocesa ni re-envía el email. Para el formato antiguo de notificación (IPN, sin firma) rige la misma regla de consultar el estado real: un identificador forjado no existe en la pasarela o no corresponde a ningún pago propio, y se ignora. |
| RNF-11 | Auditoría | Completado | Se audita la confirmación del pago (cuota marcada como pagada vía Mercado Pago, con plan y socio) y todo webhook con firma inválida queda registrado como evento sospechoso. Cada request al webhook deja rastro en los logs del servidor con su formato y resultado. |
| RNF-03 | Responsive | Completado *(adelantado)* | Cubierto por el sitio web público (RF-19), entregado el 18/06/2026, antes del inicio de esta iteración: cinco páginas institucionales mobile-first con menú colapsable. Las pantallas nuevas del portal de esta iteración (historial de pagos, resultado de pago) también tienen su variante mobile. |
| RNF-04 | SEO | Completado *(adelantado)* | Cubierto por el sitio web público (RF-19): título y descripción propios por página, etiquetas Open Graph, mapa del sitio, robots.txt, HTML semántico e idioma declarado en español. |

## Tareas pendientes

| **Requerimiento** | **Caso de uso** | **Estado** | **Detalle** |
|-|-|-|-|
| RF-18 + RNF-02 | CU-10 — Dashboard en tiempo real | Pendiente (tramo final de la iteración) | Dashboard operativo con métricas consolidadas de ambas sedes (socios activos, cuotas pendientes, clases del día, inscripciones recientes), filtro por unidad y actualización sin recarga con antigüedad máxima de 30 segundos. El diseño está cerrado en el CU-10; hay trabajo en curso en una rama de desarrollo, aún sin integrar a la rama principal. |
| RF-16 (canal email) | — | Pendiente (arrastrado de IT5) | Unificación del canal email con el centro de notificaciones in-system. Los emails ya se envían; falta que pasen por la misma infraestructura que maneja la campanita del portal. Sin avances en esta iteración. |
| RNF-06 | — | En medición | Disponibilidad ≥ 95% del despliegue en Azure. Resultado de la medición del período: [COMPLETAR]. |
| Pruebas Postman de RF-21 | — | Pendiente | La colección de Postman todavía no incluye tests de los endpoints de pagos (ver sección de pruebas de API). |

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

## Estructura de API --- endpoints implementados

| **Método** | **Endpoint** | **Descripción** | **Auth** |
|-|-|-|-|
| POST | `/api/pagos/iniciar` | El socio inicia el pago online de una de sus cuotas pendientes; devuelve la URL del checkout de Mercado Pago a la que el frontend redirige. Rechaza cuotas inexistentes, de otro socio o ya pagadas | Socio (portal) |
| POST | `/api/pagos/webhook` | Recibe la notificación de pago de Mercado Pago, en formato moderno (firmado) o IPN antiguo (sin firma, con consulta del estado real a la pasarela). Responde 401 solo ante firma inválida; en el resto de los casos responde 200 para que la pasarela no reintente | Anónimo (protegido por firma) |
| GET | `/api/pagos/mis-pagos` | Historial de pagos del socio autenticado, ordenado del más reciente al más antiguo | Socio (portal) |

## Casos de uso extendidos --- Iteración 6

> El CU-10 (Dashboard en tiempo real) no se incluye como caso de uso extendido porque RF-18 todavía no se implementó; su diseño completo está en el documento del CU-10 y se documentará aquí al cierre de la iteración.

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
- Dashboard en tiempo real (si queda integrado en el tramo final de la iteración).

## Pruebas automatizadas

Suite backend en verde (0 fallos). Cobertura agregada en esta iteración:

**Pago online con Mercado Pago (RF-21):**

- *Dominio:* el intento de pago nace en estado pendiente; un monto negativo es rechazado; las transiciones de estado son estrictas (no se puede aprobar dos veces, ni rechazar un pago aprobado, ni aprobar uno rechazado); al aprobar quedan registrados el número de transacción, el medio de pago y la fecha de acreditación.
- *Application:* el inicio del pago crea el intento, usa su identificador como referencia para la pasarela y devuelve la URL del checkout; rechaza cuotas inexistentes, de otro socio o ya pagadas; si la pasarela falla al crear la preferencia, el error se propaga de forma controlada. El procesamiento del webhook: con firma inválida no consulta la pasarela ni toca datos, audita el evento sospechoso y devuelve el rechazo; con pago aprobado marca la cuota pagada y el intento aprobado en un único guardado atómico, audita y envía el email; si la cuota ya estaba pagada es idempotente y no re-envía el email; con pago rechazado marca solo el intento, la cuota no cambia; estados no terminales (pendiente) no producen cambios; referencias desconocidas o malformadas se ignoran; el camino IPN procesa sin firma pero consultando el estado real, y un identificador forjado se ignora sin tocar datos; una falla del email no impide confirmar el pago. El historial mapea los datos del pago con su plan y respeta el orden por fecha descendente. A nivel del endpoint se verifica el contrato HTTP completo: 401 solo ante firma inválida y 200 en el resto, lectura de la firma y del identificador desde donde la pasarela los manda, soporte de ambos formatos de notificación, descarte de notificaciones que no son de pagos, y que el webhook es el único acceso anónimo del módulo.
- *Infrastructure:* la validación de la firma acepta el valor correcto (sin distinguir mayúsculas) y rechaza firmas alteradas, encabezados malformados o ausentes y la configuración sin clave secreta, incluyendo las variantes del manifiesto con y sin identificador de request; el repositorio de pagos cubre alta, búsqueda por referencia externa, pagos por cuota e historial por socio ordenado; el armado de URLs verifica las tres URLs de retorno del checkout y la URL de notificación con el pedido de formato moderno, con su fallback de configuración.
- *Frontend:* el botón "Pagar con Mercado Pago" aparece solo en cuotas pendientes, llama al endpoint de inicio y redirige al checkout; la página de resultado muestra el mensaje correcto según el estado recibido; el historial de pagos renderiza los datos del socio.

## Pruebas de API realizadas con Postman

**PENDIENTE** — La colección `GymFlow API Tests.postman_collection.json` todavía no incorpora tests para los endpoints de pagos de esta iteración (`/api/pagos/iniciar`, `/api/pagos/webhook`, `/api/pagos/mis-pagos`). Al agregarlos se documentarán aquí con las tablas de tests agregados/modificados y el resumen de aserciones, como en iteraciones anteriores.

> Nota: el flujo feliz del webhook requiere una notificación real firmada por Mercado Pago contra una URL pública, por lo que —igual que el login con Google en IT5— la colección podrá cubrir principalmente los caminos de error (firma inválida, cuota ajena, cuota ya pagada, sin autenticación) y el flujo completo se valida de punta a punta contra el despliegue (ver Prueba 6.1).

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
