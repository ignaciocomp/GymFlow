# Spec — RF-23 / CU-08: Pago de cuota online con Mercado Pago

- **RF:** RF-23 (Pago de cuotas online). Caso de uso **CU-08** (sección 8 del doc de requerimientos).
- **Fecha:** 2026-06-21
- **Iteración:** 6 (fusionada con la 7 por el recorte de alcance #23).
- **Alcance:** **MVP sólido** (webhook-based). La reconciliación por polling y el flujo detallado de transferencia pendiente quedan fuera (anotados como futuro).
- **Estado:** Spec para revisión del usuario.

## 1. Contexto y objetivo

El socio puede **pagar su cuota online** desde el portal usando **Mercado Pago (Checkout Pro)**. El sistema crea una preferencia de pago, redirige al checkout de MP, y cuando MP confirma el pago vía **webhook** (validando la **firma HMAC**), marca la cuota como pagada, registra la transacción, audita y envía el mail de confirmación.

Hoy ya existe: gestión de cuotas con `MarcarComoPagada()` manual, el enum `EstadoCuota` (Pendiente/Pagada/Anulada) con `FechaPago`, auditoría, y el **email de "pago confirmado"** (se reutiliza).

## 2. Decisiones de diseño

| Decisión | Elección | Motivo |
|---|---|---|
| Integración MP | **Checkout Pro** (redirect a checkout hosteado) | Simple, PCI a cargo de MP, no se manejan datos de tarjeta. |
| Confirmación | **Webhook** de MP + validación **HMAC** (RN-31) | Fuente de verdad del pago; nunca se marca pagada sin webhook válido. Funciona con scale-to-zero (el POST despierta el container). |
| Registro | **Entidad `Pago` nueva** | Guarda N° de transacción MP, medio, estado, fecha → habilita el **historial de pagos** del CU-08. |
| Verificación del pago | Al recibir webhook, se **consulta el pago en la API de MP** (por id) — NO se confía en el payload | El webhook solo trae el id; el estado real se lee de MP (evita spoofing). |
| Credenciales | `MercadoPago:AccessToken` + `MercadoPago:WebhookSecret` como **secrets** (workflow tipo SMTP) | No tocan el repo. |
| Testing | Sandbox de MP (usuarios + tarjetas de prueba) | Sin plata real. Prueba E2E contra el Container App (URL pública para el webhook). |

## 3. Alcance

### Incluye (MVP)
- Botón **"Pagar con Mercado Pago"** en las cuotas pendientes/vencidas del portal.
- Crear preferencia de pago (Checkout Pro) y redirigir.
- **Webhook** `POST /api/pagos/webhook` con validación de firma HMAC.
- Al aprobarse: marcar cuota **Pagada**, registrar `Pago` (N° transacción MP, medio, monto, fecha), **auditoría**, **email** de confirmación (reuso). **Idempotente**.
- Páginas de retorno (éxito / error / pendiente) en el portal.
- **Historial de pagos** del socio.
- Manejo de: pago rechazado (E1), firma inválida (E3), cuota ya pagada (E4).

### No incluye (futuro, anotado)
- **Reconciliación por polling** (E2) a los 5/30 min → poco confiable con scale-to-zero (mismo tema que los jobs de cuotas). El webhook es la vía principal.
- Flujo detallado de **transferencia pendiente** (se muestra "pendiente" pero sin seguimiento avanzado).
- Dashboard en tiempo real (RF-18, RNF-02/SSE) — es otro RF.

## 4. Modelo de datos

Nueva entidad de dominio **`Pago`** (`backend/src/GymFlow.Domain/Entities/Pago.cs`):

```
Pago
- Id: Guid
- CuotaId: Guid (FK) + Cuota
- SocioId: Guid
- Monto: decimal
- Estado: EstadoPago  // Pendiente | Aprobado | Rechazado
- MedioPago: string?  // "credit_card", "ticket", etc. (de MP)
- MpPreferenceId: string   // preferencia de Checkout Pro
- MpPaymentId: string?      // id del pago en MP (llega por webhook)
- FechaCreacion: DateTime
- FechaAcreditacion: DateTime?
```

- Nuevo enum `EstadoPago` (Pendiente/Aprobado/Rechazado).
- Relación `Cuota (1) → (N) Pago` (una cuota puede tener varios intentos).
- `external_reference` de la preferencia MP = `Pago.Id` (para reconciliar el webhook con el pago).
- **Migración EF** para la tabla `Pagos`.

## 5. Backend (.NET 8, Clean Arch)

- **`IMercadoPagoService`** (Application/Interfaces) + impl en Infrastructure (SDK `mercadopago` NuGet o `HttpClient`):
  - `CrearPreferenciaAsync(pago, cuota, backUrls, notificationUrl)` → devuelve `init_point` (URL de Checkout Pro).
  - `ObtenerPagoAsync(mpPaymentId)` → estado + medio del pago (para el webhook).
  - `ValidarFirmaWebhook(headers, body)` → bool (HMAC-SHA256 del manifest `id:<data.id>;request-id:<x-request-id>;ts:<ts>;` con el `WebhookSecret`, comparado contra `v1` del header `x-signature`).
- **`IniciarPagoCuotaCommand`** (Application): valida que la cuota exista, sea del socio autenticado y esté **Pendiente** (no Pagada/Anulada — E4); crea `Pago` (Pendiente) + preferencia MP → devuelve `init_point`.
- **`ProcesarWebhookPagoCommand`** (Application): valida firma (si inválida → descarta + audita "evento sospechoso", E3); consulta el pago en MP; si **approved** → busca el `Pago` por `external_reference`, marca `Cuota.MarcarComoPagada()`, `Pago.Aprobado` + `MpPaymentId`/medio/fecha, **auditoría**, **email**; si **rejected** → `Pago.Rechazado`, cuota sin cambios (E1); **idempotente** (si la cuota ya está pagada, no reprocesa).
- **Endpoints** (`PagosController` / extensión de `CuotasController`):
  - `POST /api/cuotas/{id}/pagar` — socio autenticado → `{ initPoint }`.
  - `POST /api/pagos/webhook` — **`[AllowAnonymous]`**, valida HMAC → 200 siempre (para que MP no reintente de más), procesa async.
  - `GET /api/pagos/mis-pagos` — historial del socio.
- **Config:** `MercadoPago:AccessToken`, `MercadoPago:WebhookSecret`, `MercadoPago:BackUrlBase` (URL del portal para las back_urls). En dev, valores de sandbox en appsettings; en prod, secrets.

## 6. Frontend (portal del socio, React)

- **`MisCuotasPage`**: en cuotas Pendientes/Vencidas, botón **"Pagar con Mercado Pago"** → `POST /api/cuotas/{id}/pagar` → `window.location = initPoint` (redirect a Checkout Pro).
- **Páginas de retorno** (back_urls): `/portal/pago/exito`, `/portal/pago/error`, `/portal/pago/pendiente` → mensaje claro. La de éxito re-consulta la cuota (el update real llega por webhook, puede tardar unos segundos) y muestra "pago confirmado" o "procesando".
- **Historial de pagos**: nueva vista/sección en el portal (`GET /api/pagos/mis-pagos`): fecha, monto, medio, N° transacción MP, estado.

## 7. Manejo de errores (CU-08)
- **E1 Rechazado:** `Pago.Rechazado`, cuota sin cambios, mensaje claro. (CA-37)
- **E3 Firma inválida:** descartar + auditar como sospechoso, sin modificar datos. (CA-36)
- **E4 Cuota ya pagada:** idempotente, no reprocesa. Botón deshabilitado si ya está pagada.
- **E5 Error de conexión con MP:** al crear preferencia, si MP falla → mensaje "No es posible procesar el pago en este momento", sin crear preferencia.

## 8. Seguridad
- **RN-31:** nunca se actualiza el estado de la cuota sin validar la firma HMAC del webhook Y consultar el estado real en la API de MP.
- El webhook es `[AllowAnonymous]` pero **toda** modificación depende de la firma válida + confirmación de MP.
- Idempotencia por `external_reference` + estado de la cuota (evita doble marcado).
- Access token y webhook secret solo en secrets del Container App (nunca en repo ni logs).

## 9. Testing (TDD)
- **Domain:** `Pago` (invariantes, transiciones de estado), `Cuota.MarcarComoPagada` (ya existe).
- **Application:** `IniciarPagoCuotaCommand` (crea pago + llama al service mock; rechaza cuota ajena / ya pagada); `ProcesarWebhookPagoCommand` (approved → marca pagada + email; rejected → sin cambios; firma inválida → descarta; ya pagada → idempotente). Mock de `IMercadoPagoService`.
- **Infra:** test de `ValidarFirmaWebhook` (HMAC correcto/incorrecto con un secret conocido).
- **Frontend:** botón renderiza en cuota pendiente, llama al endpoint, redirige; historial renderiza.

## 10. Criterios de aceptación (del CU-08)
- CA-33: socio inicia el pago desde su perfil si la cuota está pendiente/vencida.
- CA-34: tras pago aprobado (webhook), la cuota pasa a pagada sin intervención del admin.
- CA-35: el socio recibe el mail de confirmación tras el pago aprobado.
- CA-36: webhook con firma inválida es descartado y no modifica datos.
- CA-37: pago rechazado no cambia el estado de la cuota; el socio ve un error claro.

## 11. Config / secrets (workflow tipo SMTP)
- Nuevo workflow `configure-mercadopago.yml` (manual) que toma los GitHub secrets `MP_ACCESS_TOKEN` + `MP_WEBHOOK_SECRET` y los setea como secrets del Container App (`mp-access-token`, `mp-webhook-secret`) + env vars `MercadoPago__AccessToken` / `MercadoPago__WebhookSecret` via `secretref`.
- El webhook ya está configurado en MP apuntando a `…/api/pagos/webhook` (modo de prueba).

## 12. Pendientes del usuario
- Cargar en GitHub los secrets `MP_ACCESS_TOKEN` y `MP_WEBHOOK_SECRET` (con los valores de prueba ya obtenidos) y correr el workflow.
- (Al cerrar el proyecto) regenerar el webhook secret por haber estado en el chat.
- Confirmar que el access token es el de **"Credenciales de prueba"** (sandbox).
