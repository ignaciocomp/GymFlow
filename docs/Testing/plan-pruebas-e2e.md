---
title: Plan de Pruebas End-to-End (E2E) — GymFlow
tags:
  - testing
  - seguimiento
related:
  - "[[caso_de_uso_index]]"
  - "[[pruebas-automatizadas-it1-4]]"
  - "[[iteracion-6]]"
---

# Plan de Pruebas End-to-End (E2E) — GymFlow

> Plan de pruebas manuales de punta a punta sobre el sistema desplegado. Cubre los flujos completos de usuario de los casos de uso CU-01 a CU-11, más una sección de pruebas de seguridad y permisos. Los resultados de ejecución se registran en este mismo documento.

## 1. Introducción — qué es una prueba E2E y dónde encaja

Una prueba end-to-end verifica un **flujo completo de usuario contra el sistema real integrado**: navegador, API, base de datos y servicios externos reales (Mercado Pago en modo sandbox, Google OAuth, servidor de email). A diferencia de los niveles inferiores de la pirámide de pruebas, no aísla componentes ni simula dependencias: valida que todas las piezas conectadas funcionan juntas como las va a usar el usuario final.

GymFlow ya cuenta con tres niveles de pruebas documentados. Este plan agrega el cuarto:

| Nivel | Herramienta | Qué verifica | Dónde está documentado |
|-|-|-|-|
| Unitarias / integración | xUnit + Moq (~353 casos) | Reglas de negocio, casos de uso y servicios en aislamiento | [[pruebas-automatizadas-it1-4]] |
| API | Postman | Contrato HTTP de los endpoints contra el backend levantado | Colección `GymFlow API Tests` + docs de iteración |
| Funcionales de frontend | Manuales, por iteración | Pantallas y comportamientos de la funcionalidad nueva de cada iteración | Docs de iteración (`docs/seguimiento/`) |
| **End-to-end (este plan)** | **Manual, contra el despliegue** | **Flujos completos de usuario atravesando frontend + API + BD + integraciones, incluyendo efectos colaterales (emails, notificaciones, auditoría)** | **Este documento** |

El diferencial del nivel E2E respecto de las pruebas funcionales por iteración es doble: (a) se ejecuta contra el **sistema desplegado con integraciones reales**, no contra el entorno local, y (b) cada caso verifica también los **efectos del otro lado del sistema** — por ejemplo: el socio se inscribe y el admin ve el cupo descontado; el pago se aprueba y llega el email y la auditoría lo registra.

## 2. Alcance

**Incluye:** los flujos principales y las excepciones de mayor riesgo de todos los casos de uso implementados (CU-01, CU-02, CU-03, CU-05, CU-06, CU-07, CU-08, CU-09, CU-11 y CU-10) y un conjunto de pruebas transversales de seguridad y permisos (RNF-01, RNF-05).

**No incluye:**

- Cobertura exhaustiva de todos los flujos alternativos de cada CU (ya cubiertos por xUnit y Postman en los niveles inferiores).
- RF-17 (rutinas): fuera de alcance del proyecto.
- Pruebas de carga o rendimiento.
- Verificación sistemática responsive y SEO (RNF-03/RNF-04): documentadas en la iteración 6 con el sitio público (RF-19).

> **Nota sobre numeración:** no existe caso de uso CU-04; en la numeración original de requerimientos era el dashboard, hoy documentado como CU-10. Ver la nota de numeración en [[caso_de_uso_index]].

## 3. Estrategia y entorno de ejecución

### 3.1 Entorno

- **Sistema bajo prueba:** despliegue en Azure (frontend + API + PostgreSQL), sobre una versión identificada por tag de `main`. Es el único entorno donde funcionan de punta a punta el webhook de Mercado Pago, el envío de email y el login con Google.
- **Mercado Pago:** modo sandbox — credenciales de prueba, usuario comprador de prueba y tarjetas de test (aprobada / rechazada / pendiente).
- **Google OAuth:** cuenta de Google de prueba registrada como socio.
- **Email:** casilla de prueba accesible por el equipo para verificar los envíos.

### 3.2 Usuarios y datos de prueba

| Rol | Usuario | Uso en el plan |
|-|-|-|
| Administrador | `admin@gymflow.com` (o admin propio del equipo) | ABMs, gestión de cuotas, roles, auditoría |
| Dueño | Empleado de prueba con rol Dueño y 1 sede asignada | Casos de filtrado por sede |
| Profesor | Empleado de prueba con rol "Profesor" (permisos Socios-Escritura, Clases-Lectura/Escritura/Modificación) | Casos RF-13/RF-14 y permisos |
| Socio | Cuenta de Google de prueba vinculada a un socio activo con plan y cuota pendiente | Portal, inscripciones, pagos |
| Comprador MP | Usuario comprador de prueba de Mercado Pago | Checkout sandbox |

**Convención de datos:** todo dato creado durante la ejecución lleva el sufijo o apellido **"E2E-Test"** (socios, clases, eventos, roles) para poder identificarlo y darlo de baja lógica al terminar la corrida. El entorno de Azure no se resetea: no usar datos reales de socios y no modificar datos que no tengan la convención.

### 3.3 Criterios de entrada y salida

**Entrada (para iniciar la corrida):**

- Versión desplegada en Azure identificada (tag).
- Suite xUnit en verde y pipeline CI verde sobre esa versión.
- Usuarios y datos de prueba de 3.2 disponibles.

**Salida (para dar por aprobado el plan):**

- 100% de los casos de prioridad Crítica en estado "Pasó".
- Sin defectos de severidad alta o crítica abiertos.
- Los casos "Bloqueado" tienen su causa documentada (ej. CU-10 sin integrar).

### 3.4 Formato de los casos y estados

Cada caso tiene: identificador (`E2E-XX`), trazabilidad (CU y RF/RNF), prioridad, precondiciones, pasos numerados desde el navegador, resultado esperado (incluyendo efectos colaterales) y su tabla de registro de ejecución.

Estados posibles: **Pasó** | **Falló** (se registra defecto en la sección 8) | **Bloqueado** (no se pudo ejecutar; se indica causa) | **Pendiente** (aún no ejecutado).

## 4. Trazabilidad

| CU | Caso de uso | RF / RNF | Casos E2E |
|-|-|-|-|
| [[CU-01-gestion-socios\|CU-01]] | Gestión de socios | RF-01..05, RF-20, RF-22, RNF-09 | E2E-01 a E2E-04 |
| [[CU-02-inscripcion-clase\|CU-02]] | Inscripción a clase | RF-10, RF-11 | E2E-05 a E2E-07 |
| [[CU-03-cuotas-recordatorios\|CU-03]] | Cuotas y recordatorios | RF-06, RF-07 | E2E-08 a E2E-11 |
| [[CU-05-autenticacion\|CU-05]] | Autenticación y control de acceso | RNF-01, RNF-05, RNF-10 | E2E-12 a E2E-15 |
| [[CU-06-clases-horarios\|CU-06]] | Clases y horarios | RF-08, RF-09 | E2E-16 a E2E-19 |
| [[CU-07-empleados-roles\|CU-07]] | Empleados, roles y permisos | RF-12, RF-13, RF-14, RNF-01 | E2E-20 a E2E-23 |
| [[CU-08-gestion-eventos\|CU-08]] | Gestión de eventos | RF-15 | E2E-24, E2E-25 |
| [[CU-09-notificaciones-insystem\|CU-09]] | Notificaciones in-system | RF-16 | E2E-26 |
| [[CU-10-dashboard-tiempo-real\|CU-10]] | Dashboard en tiempo real | RF-18, RNF-02 | E2E-27, E2E-28 |
| [[CU-11-pago-online-mercadopago\|CU-11]] | Pago online Mercado Pago | RF-21, RNF-05 | E2E-29 a E2E-32 |
| — | Seguridad transversal | RNF-01, RNF-05 | E2E-SEC-01 a E2E-SEC-05 |

## 5. Casos de prueba por caso de uso

### CU-01 — Gestión de Socios

#### E2E-01 — Alta de socio con plan por unidad y cuota inicial

| | |
|-|-|
| **Trazabilidad** | CU-01 Subflujo A — RF-01, RF-20, RF-22, RNF-09, RNF-11 |
| **Prioridad** | Crítica |
| **Precondiciones** | Admin logueado. Existen las dos unidades con al menos un plan activo cada una. |

**Pasos:**

1. Ir a `/admin/socios` → "Nuevo Socio".
2. Completar datos personales con apellido "E2E-Test", tipo de documento CI con una cédula uruguaya válida, y correo de la casilla de prueba.
3. Asignar el socio a ambas unidades y elegir un plan distinto en cada una (RF-22).
4. Aceptar el checkbox de consentimiento Ley 18.331 y confirmar.
5. Verificar el listado de socios y el detalle del socio creado.
6. Ir a la gestión de cuotas del admin y buscar al socio por documento.
7. Ir a `/admin` → Auditoría y filtrar los registros recientes.

**Resultado esperado:**

- El socio aparece en el listado como Activo, con sus dos unidades y el plan correcto en cada una.
- Existen **dos cuotas pendientes** (una por unidad) con vencimiento a fecha de alta + 30 días y el monto del plan correspondiente.
- La auditoría registra el alta con usuario, timestamp y detalle, incluyendo el timestamp de aceptación del consentimiento.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-02 — Edición, baja lógica y reactivación de socio

| | |
|-|-|
| **Trazabilidad** | CU-01 Subflujos B y C — RF-03, RF-04, RNF-11 |
| **Prioridad** | Alta |
| **Precondiciones** | Existe el socio E2E-Test creado en E2E-01. |

**Pasos:**

1. Editar el socio: modificar el teléfono y guardar.
2. Dar de baja al socio ingresando un motivo.
3. Verificar la tab de inactivos y que el socio no aparece entre los activos.
4. Reactivar al socio desde la tab de inactivos.
5. Revisar auditoría.

**Resultado esperado:**

- La edición persiste y la auditoría registra los campos modificados.
- La baja es lógica: el socio pasa a Inactivo con su motivo, sin borrarse.
- La reactivación lo devuelve a Activo. Las tres operaciones quedan auditadas.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-03 — Excepciones del alta: consentimiento y cédula inválida

| | |
|-|-|
| **Trazabilidad** | CU-01 E1, E3 — RF-01, RNF-09 |
| **Prioridad** | Alta |
| **Precondiciones** | Admin logueado. |

**Pasos:**

1. Intentar dar de alta un socio sin marcar el checkbox de consentimiento.
2. Intentar dar de alta un socio con tipo de documento CI y un número con dígito verificador incorrecto.

**Resultado esperado:**

- Ambos intentos son bloqueados con mensaje de validación; no se crea ningún socio ni registro de auditoría de alta.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-04 — Perfil del socio y solicitudes ARCO

| | |
|-|-|
| **Trazabilidad** | CU-01 Subflujos D, E y F — RF-05, RNF-09b |
| **Prioridad** | Alta |
| **Precondiciones** | Socio de prueba logueado en el portal (vía Google). |

**Pasos:**

1. Verificar que al loguearse el socio queda en `/portal` (no accede al layout admin).
2. En `/portal/perfil`, revisar "Datos personales" y "Plan y acceso" (unidades y plan por unidad).
3. Hacer clic en "Solicitar modificación de datos", describir un cambio y enviar.
4. Como admin, verificar en auditoría la solicitud de modificación (badge celeste).
5. Como socio, hacer clic en "Solicitar baja de cuenta", confirmar y observar el comportamiento.
6. Como admin, verificar en auditoría la solicitud de baja (badge naranja).

**Resultado esperado:**

- El perfil muestra los datos correctos del socio autenticado.
- Ambas solicitudes quedan en auditoría sin modificar los datos del socio.
- Tras solicitar la baja, la sesión se cierra sola a los ~3 segundos y redirige a `/login`.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

### CU-02 — Inscripción a Clase

#### E2E-05 — Inscripción a un horario con efectos completos

| | |
|-|-|
| **Trazabilidad** | CU-02 flujo principal — RF-10, RF-11, RF-16, RNF-11 |
| **Prioridad** | Crítica |
| **Precondiciones** | Socio activo con cuota al día en la sede, logueado en el portal. Existe una clase E2E-Test activa con horario y cupo disponible en esa sede. |

**Pasos:**

1. En `/portal/horarios`, seleccionar la sede y ubicar el horario de la clase E2E-Test. Anotar el cupo disponible.
2. Hacer clic en "Inscribirme" y confirmar.
3. Verificar `/portal/mis-inscripciones`.
4. Verificar la campanita: debe existir la notificación "Confirmación de inscripción".
5. Revisar la casilla de email del socio.
6. Como admin, verificar en el calendario el cupo del horario y en auditoría el registro de la inscripción.

**Resultado esperado:**

- La inscripción aparece en "Mis Inscripciones" con día, hora y sala.
- El cupo visible se descuenta en 1, tanto en el portal como en la vista admin.
- Llega el email de confirmación y se crea la notificación in-system.
- La auditoría registra la inscripción.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-06 — Cancelación de inscripción libera el cupo

| | |
|-|-|
| **Trazabilidad** | CU-02 flujo alternativo — RF-11, RNF-11 |
| **Prioridad** | Alta |
| **Precondiciones** | Inscripción activa creada en E2E-05. |

**Pasos:**

1. En `/portal/mis-inscripciones`, cancelar la inscripción y confirmar.
2. Volver a `/portal/horarios` y verificar el cupo del horario.
3. Verificar que la reinscripción al mismo horario vuelve a estar disponible.

**Resultado esperado:**

- La inscripción desaparece de las activas, el cupo se libera de inmediato y el socio puede reinscribirse. La cancelación queda auditada.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-07 — Excepciones de inscripción: sin cupo, duplicada, cuota vencida

| | |
|-|-|
| **Trazabilidad** | CU-02 E1, E2, E4 — RF-10, RN-09 |
| **Prioridad** | Alta |
| **Precondiciones** | Una clase E2E-Test con capacidad 1 y un horario. Un socio con cuota vencida en la sede (puede prepararse revirtiendo el pago de una cuota vencida desde el admin). |

**Pasos:**

1. Con el socio A, inscribirse al horario de capacidad 1.
2. Con el socio B (cuota al día), intentar inscribirse al mismo horario: verificar el estado del botón y el mensaje.
3. Con el socio A, intentar inscribirse de nuevo al mismo horario (por API o UI si el botón lo permite).
4. Con un socio con cuota vencida en la sede, intentar inscribirse a cualquier horario de esa sede.

**Resultado esperado:**

- Paso 2: botón "Cupo lleno" deshabilitado / mensaje "Este horario no tiene cupos disponibles".
- Paso 3: rechazo con "Ya estás inscripto en este horario" (RN-09).
- Paso 4: rechazo con "No podés inscribirte con cuota vencida en esta sede". En ningún caso se crea inscripción ni se altera el cupo.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

### CU-03 — Gestión de Cuotas y Recordatorios

#### E2E-08 — Marcar cuota pagada y revertir el pago

| | |
|-|-|
| **Trazabilidad** | CU-03 flujos alternativos admin — RF-07, RNF-11 |
| **Prioridad** | Crítica |
| **Precondiciones** | Admin logueado. Socio E2E-Test con cuota pendiente. |

**Pasos:**

1. En la gestión de cuotas del admin, buscar al socio por documento.
2. Sobre la cuota pendiente, "Marcar como pagada" y confirmar.
3. Como socio, verificar `/portal/mis-cuotas`.
4. Como admin, "Revertir pago" sobre esa cuota y confirmar.
5. Verificar de nuevo la vista del socio y la auditoría.

**Resultado esperado:**

- Tras el paso 2 la cuota queda Pagada (badge verde) en admin y en el portal del socio, con fecha de pago.
- Tras el paso 4 vuelve a Pendiente y se limpia la fecha de pago.
- Ambas operaciones quedan en auditoría.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-09 — Anulación y reactivación de cuota; no se anula una pagada

| | |
|-|-|
| **Trazabilidad** | CU-03 flujos alternativos + E2 — RF-07 |
| **Prioridad** | Media |
| **Precondiciones** | Socio E2E-Test con una cuota pendiente y una pagada. |

**Pasos:**

1. Anular la cuota pendiente y confirmar.
2. Filtrar por estado Anulada y reactivarla.
3. Intentar anular la cuota pagada.

**Resultado esperado:**

- La anulación es soft-delete (estado Anulada) y la reactivación la devuelve a Pendiente; ambas quedan en historial/auditoría.
- El intento sobre la cuota pagada es rechazado.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-10 — Notificación manual de cuota

| | |
|-|-|
| **Trazabilidad** | CU-03 flujo alternativo — RF-06 |
| **Prioridad** | Media |
| **Precondiciones** | Socio E2E-Test con correo de prueba y cuota pendiente. |

**Pasos:**

1. En la gestión de cuotas, buscar al socio y hacer clic en "Notificar" sobre la cuota pendiente.
2. Revisar la casilla del socio y la campanita del portal.
3. Intentar notificar de nuevo la misma cuota el mismo día.

**Resultado esperado:**

- Llega el email con nombre, plan, unidad, monto y vencimiento; se crea la notificación in-system "Recordatorio de cuota".
- El segundo intento el mismo día es rechazado (límite de una notificación manual por cuota por día).

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-11 — Recordatorios automáticos de cuota (job)

| | |
|-|-|
| **Trazabilidad** | CU-03 flujo principal recordatorios — RF-06, RF-16 |
| **Prioridad** | Alta |
| **Precondiciones** | Socio E2E-Test con cuota pendiente cuyo vencimiento cae en la ventana de recordatorio (5 días antes, 1 día antes o el día del vencimiento — se puede preparar creando la cuota con la fecha adecuada). Acceso al endpoint manual de disparo del job (`POST /api/cuotas/procesar-recordatorios`, solo admin) o esperar la corrida diaria programada. |

**Pasos:**

1. Disparar el job de recordatorios (o esperar su corrida diaria).
2. Revisar la casilla del socio.
3. Revisar la campanita del portal del socio.
4. Volver a disparar el job el mismo día.

**Resultado esperado:**

- Llega el email del tipo correspondiente a la ventana (informativo / urgente / vencimiento) y se crea la notificación in-system.
- El segundo disparo del mismo día no duplica el recordatorio del mismo tipo (idempotencia por `RecordatorioCuota`).

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

### CU-05 — Autenticación y Control de Acceso

#### E2E-12 — Alta de MFA en el primer login de un empleado

| | |
|-|-|
| **Trazabilidad** | CU-05 alta de MFA — RNF-01, RNF-10 |
| **Prioridad** | Crítica |
| **Precondiciones** | Empleado E2E-Test recién creado (ver E2E-20) con credenciales temporales, sin MFA configurado. App autenticadora disponible. |

**Pasos:**

1. En `/login`, ingresar correo y contraseña temporal del empleado.
2. En la pantalla de alta de MFA, escanear el QR con la app autenticadora.
3. Ingresar el primer código de 6 dígitos generado.
4. Guardar los 10 códigos de recuperación mostrados.
5. Verificar el acceso al panel según los permisos del rol.

**Resultado esperado:**

- Tras validar el código, el sistema muestra los códigos de recuperación una única vez, activa el MFA y emite la sesión.
- El empleado accede solo a los módulos que su rol permite.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-13 — Login de empleado con MFA ya activado

| | |
|-|-|
| **Trazabilidad** | CU-05 flujo principal empleado — RNF-01, RNF-10 |
| **Prioridad** | Crítica |
| **Precondiciones** | Empleado con MFA activo (E2E-12). |

**Pasos:**

1. Login con correo y contraseña.
2. Ingresar el código TOTP vigente de la app.
3. Verificar el acceso y que un código incorrecto es rechazado (probar primero uno inválido).

**Resultado esperado:**

- Con código inválido no se emite sesión; con código válido el empleado entra al panel. El login queda auditado.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-14 — Login de socio con Google

| | |
|-|-|
| **Trazabilidad** | CU-05 flujo alternativo Google — RNF-01, RNF-10 |
| **Prioridad** | Crítica |
| **Precondiciones** | Cuenta de Google de prueba cuyo correo corresponde a un socio activo. Otra cuenta de Google cuyo correo no está registrado. |

**Pasos:**

1. En `/login`, "Iniciar sesión con Google" con la cuenta del socio.
2. Verificar la redirección a `/portal` y el acceso a las secciones del socio.
3. Cerrar sesión y repetir el login (verifica que la vinculación del Google ID es estable).
4. Intentar el login con la cuenta de Google no registrada.

**Resultado esperado:**

- El socio entra al portal en ambos logins.
- La cuenta no registrada recibe el mensaje genérico "No encontramos una cuenta asociada a este correo", sin revelar más información.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-15 — Código de recuperación, bloqueo por intentos y reset por admin

| | |
|-|-|
| **Trazabilidad** | CU-05 recuperación, E1, reset — RNF-01, RNF-10 |
| **Prioridad** | Alta |
| **Precondiciones** | Empleado con MFA activo y sus códigos de recuperación (E2E-12). |

**Pasos:**

1. En la pantalla de verificación, elegir "Usar código de recuperación" e ingresar uno válido.
2. Cerrar sesión e intentar reutilizar el mismo código.
3. Provocar 5 intentos fallidos consecutivos de código TOTP.
4. Intentar un login normal: verificar el bloqueo.
5. Como admin, ejecutar "Resetear MFA" sobre el empleado.
6. Login del empleado: verificar que pasa de nuevo por el alta de MFA.

**Resultado esperado:**

- El código de recuperación funciona una sola vez; el reuso es rechazado.
- Tras 5 fallos el paso MFA queda bloqueado y requiere reset del admin (acción auditada).
- Tras el reset, el próximo login repite el alta de MFA (QR nuevo).

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

### CU-06 — Gestión de Clases y Horarios

#### E2E-16 — Crear clase con horarios, visible en el portal

| | |
|-|-|
| **Trazabilidad** | CU-06 flujo principal — RF-08, RF-09 |
| **Prioridad** | Crítica |
| **Precondiciones** | Admin logueado. |

**Pasos:**

1. Crear la clase "Clase E2E-Test" con instructor, capacidad, duración y unidad.
2. Crear un horario semanal para esa clase (día, hora inicio/fin, sala).
3. Verificar la grilla semanal del admin (con el filtro de sede).
4. Como socio de esa sede, verificar `/portal/horarios`.

**Resultado esperado:**

- La clase queda Activa y auditada; el horario aparece en la grilla del admin y en el portal del socio con su cupo.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-17 — Conflicto de sala y reducción de capacidad inválida

| | |
|-|-|
| **Trazabilidad** | CU-06 E1, E2 — RF-08, RF-09 |
| **Prioridad** | Alta |
| **Precondiciones** | Clase E2E-Test con horario en una sala y al menos 1 inscripto. |

**Pasos:**

1. Intentar crear otro horario en la misma sala, mismo día, con horas solapadas.
2. Editar la clase e intentar reducir la capacidad por debajo de las inscripciones activas.

**Resultado esperado:**

- Paso 1: bloqueado con "La sala ya está ocupada en ese horario."
- Paso 2: bloqueado con el mensaje de capacidad mínima (indica el número de inscriptos).

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-18 — Modificación de horario notifica a los inscriptos

| | |
|-|-|
| **Trazabilidad** | CU-06 modificación de horario — RF-09, RF-16, RNF-11 |
| **Prioridad** | Alta |
| **Precondiciones** | Clase E2E-Test con un socio de prueba inscripto a su horario. |

**Pasos:**

1. Como admin, editar el horario (cambiar la hora).
2. Revisar la casilla del socio inscripto.
3. Revisar la campanita del portal del socio.
4. Revisar auditoría.

**Resultado esperado:**

- El horario se actualiza en la grilla y el portal.
- El socio inscripto recibe email y notificación in-system "Cambio de horario". La auditoría registra el resultado del envío.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-19 — Cancelación de clase con inscriptos y reactivación

| | |
|-|-|
| **Trazabilidad** | CU-06 cancelación y reactivación — RF-08, RF-16, RNF-11 |
| **Prioridad** | Crítica |
| **Precondiciones** | Clase E2E-Test con al menos un inscripto. |

**Pasos:**

1. Como admin, cancelar la clase y confirmar.
2. Verificar "Mis Inscripciones" del socio, su casilla y su campanita.
3. Verificar que el horario ya no admite inscripciones en el portal.
4. Reactivar la clase.
5. Verificar el estado de las inscripciones previas.

**Resultado esperado:**

- La cancelación es lógica, cancela las inscripciones activas y dispara email + notificación "Cancelación de clase" a los inscriptos, con el resultado de cada envío en auditoría.
- Tras reactivar, la clase vuelve a estar disponible pero las inscripciones previas no se restauran: el socio debe reinscribirse.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

### CU-07 — Empleados, Roles y Permisos

> **Modelo de roles del sistema:** existen tres roles fijos (hardcoded) — **Admin**, **Dueño** y **Socio** — y un mecanismo de **roles por configuración** (creados desde `/admin/roles` combinando permisos por módulo × operación). Cada rol fijo tiene su caso E2E propio: Admin (E2E-21), Dueño (E2E-22) y Socio (verificado en E2E-SEC-02 más todos los casos del portal, que ejercen lo que el Socio sí puede hacer). Los roles por configuración se verifican con un caso general (E2E-23) usando "Profesor" como ejemplo representativo: el comportamiento depende de la matriz de permisos, no del nombre del rol, por lo que un caso alcanza para cubrir el mecanismo.

#### E2E-20 — Alta de empleado con credenciales temporales por email

| | |
|-|-|
| **Trazabilidad** | CU-07 flujo principal — RF-12, RNF-11 |
| **Prioridad** | Crítica |
| **Precondiciones** | Admin logueado. Casilla de prueba disponible para el empleado. |

**Pasos:**

1. En "Empleados y Profesores" → "Nuevo", completar nombre "Empleado E2E-Test", correo de prueba, teléfono, rol y sede. Verificar que el formulario no pide contraseña.
2. Confirmar el alta.
3. Revisar la casilla del empleado.
4. Hacer login con las credenciales recibidas (continúa en E2E-12 con el alta de MFA).

**Resultado esperado:**

- Llega el email de bienvenida con la contraseña temporal autogenerada.
- El alta y el resultado del envío quedan en auditoría.
- Las credenciales permiten el primer login.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-21 — Rol Admin (hardcoded): acceso total y asignación exclusiva del rol Dueño

| | |
|-|-|
| **Trazabilidad** | CU-07 rol Admin + E4 — RNF-01, RNF-11 |
| **Prioridad** | Crítica |
| **Precondiciones** | Admin logueado con MFA activo. Datos existentes en ambas sedes. Empleado no-Admin disponible (el Dueño de E2E-22 u otro con permisos de Empleados). |

**Pasos:**

1. Recorrer los listados (socios, clases, cuotas, empleados, eventos): verificar que muestra datos de **ambas sedes sin ningún filtro** aplicado por defecto.
2. Acceder al módulo Auditoría y verificar que puede consultar los registros.
3. Crear (o editar) un empleado asignándole el rol **Dueño** con una sede: verificar que la operación funciona.
4. Login como el empleado no-Admin y, desde la edición de empleados, intentar asignar el rol Dueño a otro empleado.

**Resultado esperado:**

- El Admin ve la información de todas las sedes sin filtrado y accede a Auditoría (a diferencia del Dueño, E2E-22).
- La asignación del rol Dueño por el Admin funciona y queda auditada.
- El intento del paso 4 es bloqueado: **solo el Admin puede asignar el rol Dueño** (CU-07 E4).

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-22 — Rol Dueño (hardcoded): filtrado automático por sede

| | |
|-|-|
| **Trazabilidad** | CU-07 rol Dueño — RNF-01 |
| **Prioridad** | Alta |
| **Precondiciones** | Empleado con rol Dueño y una sola sede asignada, MFA activo. Datos existentes en ambas sedes. |

**Pasos:**

1. Como admin, verificar que al crear/editar un empleado con rol Dueño el sistema exige al menos una sede.
2. Login como Dueño.
3. Recorrer los listados (socios, clases, cuotas, eventos): verificar que solo aparecen datos de su sede.
4. Intentar consultar explícitamente la otra sede (filtro o URL).
5. Verificar que el módulo Auditoría no está disponible.

**Resultado esperado:**

- El filtrado por sede se aplica en todos los listados sin depender del cliente.
- Consultar una sede no asignada devuelve resultado vacío (no error).
- El Dueño no accede a Auditoría.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-23 — Rol por configuración (caso general; ejemplo: "Profesor", RF-13 / RF-14)

| | |
|-|-|
| **Trazabilidad** | CU-07 roles dinámicos — RF-13, RF-14, RNF-01 |
| **Prioridad** | Alta |
| **Precondiciones** | Rol "Profesor E2E-Test" creado desde `/admin/roles` con permisos: Socios-Escritura (y Lectura), Clases-Lectura/Escritura/Modificación. Sin permisos sobre Cuotas, Empleados ni Auditoría. Empleado de prueba con ese rol, MFA activo. |

**Pasos:**

1. Como admin, crear el rol "Profesor E2E-Test" marcando exactamente la matriz de permisos de las precondiciones, y asignárselo al empleado de prueba.
2. Login como el profesor.
3. Verificar qué módulos muestra el sidebar.
4. Registrar un socio nuevo (RF-13).
5. Editar una clase (RF-14).
6. Intentar acceder a Cuotas, Empleados y Auditoría (por URL directa).
7. Como admin, quitarle al rol el permiso Clases-Modificación y, como profesor, reintentar la edición de una clase.

**Resultado esperado:**

- El sidebar solo muestra los módulos permitidos por la matriz del rol.
- El alta de socio y la edición de clase funcionan; los accesos a módulos sin permiso son rechazados (403 / pantalla de acceso denegado).
- Tras el cambio de permisos del paso 7, la edición de clase pasa a ser rechazada: el acceso lo gobierna la matriz de permisos vigente, no el nombre del rol. La verificación aplica a cualquier rol creado por configuración.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

### CU-08 — Gestión de Eventos

#### E2E-24 — Crear evento con notificación a los socios de la sede

| | |
|-|-|
| **Trazabilidad** | CU-08 flujo principal — RF-15, RF-16, RNF-11 |
| **Prioridad** | Alta |
| **Precondiciones** | Admin logueado. Socio de prueba activo en la sede destino, con correo de prueba. |

**Pasos:**

1. Crear el evento "Evento E2E-Test" con fecha futura en la sede del socio.
2. Revisar la casilla del socio y su campanita.
3. Como socio, verificar "Próximos eventos" en el portal.
4. Revisar auditoría (conteo de emails enviados/fallidos).
5. Intentar crear un evento con fecha pasada.

**Resultado esperado:**

- El evento queda creado y auditado; el socio recibe email y notificación in-system "Evento nuevo" y lo ve en el portal.
- La fecha pasada es bloqueada por validación.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-25 — Actualizar, re-notificar y cancelar evento

| | |
|-|-|
| **Trazabilidad** | CU-08 flujos alternativos — RF-15 |
| **Prioridad** | Media |
| **Precondiciones** | Evento E2E-Test creado (E2E-24). |

**Pasos:**

1. Editar el evento (cambiar la descripción) y verificar que no se reenvían emails automáticamente.
2. Usar la acción manual "Notificar" y verificar que el email sí llega.
3. Cancelar el evento.
4. Como socio, verificar "Próximos eventos".

**Resultado esperado:**

- La actualización no dispara emails; la re-notificación manual sí (y se audita).
- Tras la cancelación (baja lógica) el evento desaparece del portal del socio pero sigue consultable en el admin.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

### CU-09 — Notificaciones in-system

#### E2E-26 — Campanita: contador, inbox y marcar como leída

| | |
|-|-|
| **Trazabilidad** | CU-09 completo — RF-16 |
| **Prioridad** | Alta |
| **Precondiciones** | Socio de prueba con notificaciones no leídas generadas por los casos anteriores (inscripción, cuota, evento). |

**Pasos:**

1. Login como socio y observar el badge de la campanita.
2. Abrir el inbox: verificar tipo, título, mensaje y fecha de cada notificación.
3. Marcar una como leída y verificar que el contador baja.
4. Volver a abrirla / marcarla de nuevo: verificar que la fecha de lectura no cambia (idempotencia).
5. Generar una notificación nueva (ej. otra inscripción) y verificar que el badge se actualiza sin re-login.

**Resultado esperado:**

- El contador refleja las no-leídas; marcar como leída es idempotente; el badge se actualiza por el polling periódico del portal.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

### CU-10 — Dashboard en Tiempo Real

> **Estado: Bloqueado** hasta que RF-18 quede integrado a la rama principal y desplegado. Casos redactados a partir de los criterios de aceptación del CU-10; ajustar si la implementación diverge.

#### E2E-27 — Carga consolidada y filtro por unidad

| | |
|-|-|
| **Trazabilidad** | CU-10 CA-01, CA-03, CA-04 — RF-18 |
| **Prioridad** | Alta |
| **Precondiciones** | RF-18 desplegado. Datos operativos en ambas sedes. Admin y Dueño (1 sede) disponibles. |

**Pasos:**

1. Como admin, abrir el Dashboard: verificar las métricas consolidadas (socios activos, cuotas pendientes, clases del día, inscripciones recientes).
2. Filtrar por una unidad y verificar que todas las métricas se recalculan.
3. Volver a "Todas".
4. Como Dueño, abrir el Dashboard: verificar que solo ve sus sedes en el filtro.
5. Como socio (u otro rol sin permiso Dashboard), intentar acceder.

**Resultado esperado:**

- Vista consolidada por defecto (RN-14); el filtro recalcula todo; el Dueño queda acotado a sus sedes; sin permiso no hay acceso (CA-04).

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Bloqueado | RF-18 sin integrar |

#### E2E-28 — Actualización sin recarga (antigüedad máxima 30 s)

| | |
|-|-|
| **Trazabilidad** | CU-10 CA-02 — RF-18, RNF-02 |
| **Prioridad** | Alta |
| **Precondiciones** | RF-18 desplegado. Dashboard abierto en una sesión de admin. |

**Pasos:**

1. Dejar el dashboard visible sin interactuar.
2. Desde otra sesión (otro navegador), inscribir un socio a una clase de hoy o marcar una cuota como pagada.
3. Cronometrar cuánto tarda el dashboard en reflejar el cambio, sin recargar la página.

**Resultado esperado:**

- El cambio se refleja en no más de 30 segundos sin recarga (RN-15, vía SSE o su mecanismo definitivo).

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Bloqueado | RF-18 sin integrar |

### CU-11 — Pago online con Mercado Pago

#### E2E-29 — Pago aprobado de punta a punta (sandbox)

| | |
|-|-|
| **Trazabilidad** | CU-11 flujo principal — RF-21, RNF-05, RNF-11 |
| **Prioridad** | Crítica |
| **Precondiciones** | Socio con cuota pendiente. Credenciales sandbox y webhook configurados hacia la URL pública de la API. Usuario comprador de prueba y tarjeta de test de pago aprobado. |

**Pasos:**

1. Como socio, en `/portal/mis-cuotas`, clic en "Pagar con Mercado Pago" sobre la cuota pendiente.
2. En el checkout, pagar con el comprador de prueba y la tarjeta de pago aprobado.
3. Verificar la página de resultado.
4. Verificar "Mis Cuotas" y `/portal/mis-pagos`.
5. Revisar la casilla del socio.
6. Como admin, revisar auditoría.

**Resultado esperado:**

- El socio vuelve a "¡Pago confirmado!"; a los pocos segundos la cuota figura Pagada.
- El pago aparece Aprobado en "Mis Pagos" con fecha, plan, monto, medio y número de transacción.
- Llega el email de confirmación y la auditoría registra la operación (confirmación vía webhook auténtico).

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-30 — Pago rechazado y pago pendiente

| | |
|-|-|
| **Trazabilidad** | CU-11 flujos alternativos — RF-21, RN-34 |
| **Prioridad** | Crítica |
| **Precondiciones** | Las de E2E-29, con tarjetas de test de rechazo y de acreditación demorada. |

**Pasos:**

1. Repetir el flujo de pago con la tarjeta de pago rechazado.
2. Verificar la página de resultado, "Mis Cuotas" y "Mis Pagos".
3. Repetir con un medio que quede pendiente.
4. Verificar la página de resultado y el estado de la cuota.

**Resultado esperado:**

- Rechazado: página "El pago fue rechazado"; la cuota sigue Pendiente con su botón de pago; el intento figura Rechazado en el historial; el acceso al portal no se bloquea (RN-34).
- Pendiente: página "Pago en proceso"; la cuota no cambia hasta la aprobación definitiva.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-31 — Abandono del checkout y reintento

| | |
|-|-|
| **Trazabilidad** | CU-11 E6 — RF-21 |
| **Prioridad** | Alta |
| **Precondiciones** | Socio con cuota pendiente. |

**Pasos:**

1. Iniciar el pago y, en el checkout, cerrar la pestaña sin pagar.
2. Volver a "Mis Cuotas" y verificar el estado de la cuota.
3. Reintentar el pago y completarlo con la tarjeta de pago aprobado.

**Resultado esperado:**

- El abandono no altera la cuota ni el portal; el intento queda Pendiente en el historial.
- El reintento genera un intento nuevo y funciona con normalidad hasta confirmar el pago.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-32 — Cuota no pagable: interfaz y servidor

| | |
|-|-|
| **Trazabilidad** | CU-11 E1 — RF-21 |
| **Prioridad** | Alta |
| **Precondiciones** | Socio con una cuota ya pagada. Herramienta para llamar la API con el token del socio (Postman). |

**Pasos:**

1. En "Mis Cuotas", verificar la fila/tarjeta de la cuota pagada.
2. Con el token del socio, llamar `POST /api/pagos/iniciar` con el id de la cuota pagada.
3. Repetir el llamado con el id de una cuota de otro socio.

**Resultado esperado:**

- La cuota pagada no ofrece el botón de pago.
- Ambos llamados por API son rechazados con el error correspondiente; no se crea ningún intento de pago.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

## 6. Pruebas transversales de seguridad y permisos

#### E2E-SEC-01 — Acceso sin autenticación

| | |
|-|-|
| **Trazabilidad** | RNF-01, RNF-05 — CU-01 E4, CU-05 E7 |
| **Prioridad** | Crítica |
| **Precondiciones** | Navegador sin sesión. Postman sin token. |

**Pasos:**

1. Navegar directo a `/portal/mis-cuotas` y a `/admin/socios` sin sesión.
2. Llamar por API `GET /api/cuotas/mis-cuotas` y `GET /api/socios` sin token.

**Resultado esperado:**

- El frontend redirige a `/login`; la API responde 401 en ambos endpoints. El sitio público (RF-19) sigue accesible sin login.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-SEC-02 — Rol Socio (hardcoded) no accede al panel admin

| | |
|-|-|
| **Trazabilidad** | RNF-01 — CU-05 E6 |
| **Prioridad** | Crítica |
| **Precondiciones** | Socio logueado (token de socio disponible). |

**Pasos:**

1. Con la sesión del socio, navegar directo a `/admin/socios`, `/admin/roles` y `/admin` (dashboard).
2. Con el token del socio, llamar por API endpoints administrativos (ej. `GET /api/empleados`, `GET /api/cuotas/admin`).

**Resultado esperado:**

- El frontend no renderiza el layout admin para el socio; la API responde 403 en todos los endpoints administrativos.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-SEC-03 — IDOR: un socio no accede a datos de otro

| | |
|-|-|
| **Trazabilidad** | RNF-05 — CU-01 D, CU-03, CU-09 E1, CU-11 |
| **Prioridad** | Crítica |
| **Precondiciones** | Dos socios de prueba (A y B) con cuotas, pagos y notificaciones propios. Token del socio A. |

**Pasos:**

1. Con el token de A, llamar `GET /api/cuotas/mis-cuotas`, `GET /api/pagos/mis-pagos` y el perfil: verificar que solo devuelven datos de A (se resuelven por JWT, sin id en la URL).
2. Con el token de A, intentar `POST /api/portal/notificaciones/{id}/leer` con el id de una notificación de B.
3. Con el token de A, intentar iniciar el pago de una cuota de B (ver E2E-32 paso 3).

**Resultado esperado:**

- Ningún endpoint expone ni permite operar sobre datos del socio B; los intentos son rechazados.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-SEC-04 — Webhook de pagos con firma inválida

| | |
|-|-|
| **Trazabilidad** | RNF-05, RN-31, CA-36 — CU-11 E2 |
| **Prioridad** | Crítica |
| **Precondiciones** | Postman apuntando al webhook público `POST /api/pagos/webhook`. Estado de cuotas conocido. |

**Pasos:**

1. Enviar una notificación con formato moderno y firma inventada.
2. Verificar el código de respuesta.
3. Como admin, revisar auditoría.
4. Verificar que ninguna cuota ni intento de pago cambió de estado.
5. Enviar una notificación en formato IPN (sin firma) con un id de pago forjado y verificar que se ignora sin tocar datos.

**Resultado esperado:**

- Firma inválida: respuesta 401, evento sospechoso en auditoría, cero cambios de datos.
- IPN forjado: respuesta 200 (para que la pasarela no reintente) pero sin ningún efecto, porque el estado real se consulta a la API de Mercado Pago y el id no corresponde a un pago propio.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

#### E2E-SEC-05 — El ticket temporal de MFA no sirve como sesión

| | |
|-|-|
| **Trazabilidad** | RNF-10 — CU-05 E2 |
| **Prioridad** | Alta |
| **Precondiciones** | Empleado con MFA activo. Posibilidad de capturar el ticket temporal del paso 1 del login (herramientas de desarrollador / Postman). |

**Pasos:**

1. Hacer login con email y contraseña del empleado y capturar el ticket temporal devuelto.
2. Usar ese ticket como token Bearer contra un endpoint normal de la API (ej. `GET /api/socios`).
3. Esperar a que expire (~5 minutos) e intentar completar el paso MFA con él.

**Resultado esperado:**

- El ticket es rechazado como credencial en cualquier endpoint que no sea el segundo factor, y una vez expirado también es rechazado en el paso MFA.

**Registro de ejecución:**

| Fecha | Ejecutor | Versión | Resultado | Evidencia |
|-|-|-|-|-|
| | | | Pendiente | |

## 7. Resumen de ejecución

Completar al cierre de cada corrida.

| Corrida | Fecha | Versión (tag) | Ejecutores | Pasó | Falló | Bloqueado | Pendiente |
|-|-|-|-|-|-|-|-|
| 1 | | | | | | | |

**Resultado por caso de uso:**

| CU | Casos | Pasó | Falló | Bloqueado | Observaciones |
|-|-|-|-|-|-|
| CU-01 | E2E-01 a 04 | | | | |
| CU-02 | E2E-05 a 07 | | | | |
| CU-03 | E2E-08 a 11 | | | | |
| CU-05 | E2E-12 a 15 | | | | |
| CU-06 | E2E-16 a 19 | | | | |
| CU-07 | E2E-20 a 23 | | | | |
| CU-08 | E2E-24, 25 | | | | |
| CU-09 | E2E-26 | | | | |
| CU-10 | E2E-27, 28 | | | 2 | RF-18 sin integrar |
| CU-11 | E2E-29 a 32 | | | | |
| Seguridad | E2E-SEC-01 a 05 | | | | |

## 8. Defectos encontrados

Registrar cada defecto detectado durante la ejecución, con su issue de GitHub.

| ID | Caso E2E | Severidad | Descripción breve | Issue | Estado |
|-|-|-|-|-|-|
| | | | | | |
