# RF-21 Pago Online con Mercado Pago — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax. Cada tarea: test que falla → implementación mínima → tests verdes → commit.

**Goal:** El socio paga su cuota online con Mercado Pago (Checkout Pro); un webhook con firma HMAC confirma el pago y marca la cuota como pagada, registra la transacción, audita y manda mail.

**Architecture:** Nueva entidad `Pago` (Domain) con su repo. Un `IMercadoPagoService` (Infra, HttpClient) crea la preferencia de Checkout Pro, consulta el pago y valida la firma del webhook. Dos commands (`IniciarPagoCuotaCommand`, `ProcesarWebhookPagoCommand`) orquestan. Endpoints en `PagosController`. Frontend: botón en el portal + páginas de retorno + historial. Config por secrets (workflow tipo SMTP). Reusa `Cuota.MarcarComoPagada()`, `IEmailService` y `IAuditLogger`.

**Tech Stack:** .NET 8 Clean Arch (Domain/Application/Infrastructure/API), EF Core + PostgreSQL, xUnit + Moq. Frontend React 18 + Vite + TS + Tailwind + TanStack Query + axios, Vitest + RTL. Mercado Pago **Checkout Pro** vía HTTP (`https://api.mercadopago.com`), sin SDK.

**Spec:** `docs/specs/spec-rf21-mercadopago.md`  · **Branch:** `feature/rf23-mercadopago` (off main — el branch conserva el nombre histórico con el RF mal numerado)

**Convenciones ya verificadas (seguir):**
- Command: `class XCommand { ctor(deps); Task<...> ExecuteAsync(...) }`, registrado en `backend/src/GymFlow.API/DependencyInjection.cs` con `.AddScoped<XCommand>()`.
- Repo: `IXRepository` (Application/Interfaces) + `XRepository` (Infrastructure/Repositories), registrado en `backend/src/GymFlow.Infrastructure/DependencyInjection.cs`.
- Entity config: `IEntityTypeConfiguration<T>` en `Infrastructure/Persistence/Configurations/`, auto-cargado por `ApplyConfigurationsFromAssembly`. DbSet en `GymFlowDbContext`.
- Migración: `dotnet ef migrations add "<Nombre>" --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`.
- Socio endpoint: `User.FindFirst(ClaimTypes.NameIdentifier)` → socioId. Admin: `[RequierePermiso(Modulo.Cuotas, Operacion.X)]`.
- Email: `IEmailService.EnviarAsync(destinatario, asunto, cuerpoHtml)`. Audit: `IAuditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria, "Cuota", cuotaId, descripcion, detalles?)`.
- Tests backend: `dotnet test` desde `backend/`. Frontend: `npm test` + `npm run build` desde `frontend/`.

---

## File Structure

```
backend/src/
├── GymFlow.Domain/
│   ├── Entities/Pago.cs                         # CREATE
│   └── Enums/EstadoPago.cs                       # CREATE (Pendiente|Aprobado|Rechazado)
├── GymFlow.Application/
│   ├── Interfaces/IPagoRepository.cs            # CREATE
│   ├── Interfaces/IMercadoPagoService.cs        # CREATE (+ DTOs)
│   └── UseCases/Pagos/
│       ├── IniciarPagoCuotaCommand.cs          # CREATE
│       ├── ProcesarWebhookPagoCommand.cs       # CREATE
│       ├── GetMisPagosQuery.cs                 # CREATE
│       └── PagoDto.cs                          # CREATE
├── GymFlow.Infrastructure/
│   ├── Repositories/PagoRepository.cs          # CREATE
│   ├── Services/MercadoPagoService.cs          # CREATE
│   └── Persistence/Configurations/PagoConfiguration.cs   # CREATE
│   └── Persistence/GymFlowDbContext.cs         # MODIFY (DbSet<Pago>)
│   └── Persistence/Migrations/*_AddPagos.cs    # GENERATED
└── GymFlow.API/
    ├── Controllers/PagosController.cs          # CREATE
    ├── DependencyInjection.cs                  # MODIFY (commands/query)
    └── appsettings.json / appsettings.Development.json  # MODIFY (MercadoPago section)
backend/src/GymFlow.Infrastructure/DependencyInjection.cs  # MODIFY (repo + service + HttpClient)

frontend/src/
├── services/api.ts                             # MODIFY (pagosApi)
├── pages/portal/MisCuotasPage.tsx              # MODIFY (botón Pagar)
├── pages/portal/PagoResultadoPage.tsx          # CREATE (exito/error/pendiente)
├── pages/portal/MisPagosPage.tsx               # CREATE (historial)
└── App.tsx                                     # MODIFY (rutas de retorno + historial)

.github/workflows/configure-mercadopago.yml     # CREATE
docs/deploy/SETUP-CICD.md                        # MODIFY (sección MP)
```

---

## Task 1: Enum `EstadoPago` + entidad `Pago` (Domain)

**Files:** Create `backend/src/GymFlow.Domain/Enums/EstadoPago.cs`, `backend/src/GymFlow.Domain/Entities/Pago.cs`. Test: `backend/tests/GymFlow.Domain.Tests/Entities/PagoTests.cs`.

- [ ] **Step 1: Test** — `Pago` se crea Pendiente con los datos MP; `MarcarAprobado(mpPaymentId, medio)` pasa a Aprobado + set FechaAcreditacion + MpPaymentId; `MarcarRechazado()` pasa a Rechazado; no se puede aprobar dos veces.

```csharp
public class PagoTests
{
    private static Pago NuevoPago() => new Pago(Guid.NewGuid(), Guid.NewGuid(), 1500m, "pref-123");

    [Fact]
    public void NuevoPago_QuedaPendiente()
    {
        var p = NuevoPago();
        Assert.Equal(EstadoPago.Pendiente, p.Estado);
        Assert.Equal("pref-123", p.MpPreferenceId);
        Assert.Null(p.MpPaymentId);
    }

    [Fact]
    public void MarcarAprobado_SeteaDatos()
    {
        var p = NuevoPago();
        p.MarcarAprobado("mp-999", "credit_card");
        Assert.Equal(EstadoPago.Aprobado, p.Estado);
        Assert.Equal("mp-999", p.MpPaymentId);
        Assert.Equal("credit_card", p.MedioPago);
        Assert.NotNull(p.FechaAcreditacion);
    }

    [Fact]
    public void MarcarAprobado_DosVeces_Lanza()
    {
        var p = NuevoPago();
        p.MarcarAprobado("mp-999", "credit_card");
        Assert.Throws<InvalidOperationException>(() => p.MarcarAprobado("mp-1000", "x"));
    }
}
```

- [ ] **Step 2: Run → FAIL** (`dotnet test --filter PagoTests`).
- [ ] **Step 3: Implementar** `EstadoPago { Pendiente, Aprobado, Rechazado }` y `Pago` (constructor `(Guid cuotaId, Guid socioId, decimal monto, string mpPreferenceId)`; propiedades private set; `MarcarAprobado(string mpPaymentId, string medioPago)` valida que esté Pendiente; `MarcarRechazado()`; `FechaCreacion = DateTime.UtcNow`). Seguir el estilo de `Cuota.cs`.
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(pagos): entidad Pago + estado (dominio) (#RF-21)`

---

## Task 2: Persistencia de `Pago` (config + DbSet + migración)

**Files:** Create `Infrastructure/Persistence/Configurations/PagoConfiguration.cs`; Modify `GymFlowDbContext.cs`; generar migración.

- [ ] **Step 1:** Crear `PagoConfiguration : IEntityTypeConfiguration<Pago>` (ToTable("Pagos"), HasKey, Monto `decimal(18,2)`, MpPreferenceId requerido HasMaxLength(100), MpPaymentId/MedioPago nullable, `HasOne<Cuota>().WithMany().HasForeignKey(p => p.CuotaId).OnDelete(Restrict)`). Copiar el estilo de `CuotaConfiguration.cs`.
- [ ] **Step 2:** En `GymFlowDbContext.cs` agregar `public DbSet<Pago> Pagos => Set<Pago>();`.
- [ ] **Step 3:** Generar migración: `dotnet ef migrations add "AddPagos" --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`. Verificar que crea la tabla `Pagos` con la FK.
- [ ] **Step 4:** `dotnet build` OK + `dotnet ef migrations has-pending-model-changes` → sin cambios pendientes.
- [ ] **Step 5: Commit** — `feat(pagos): persistencia y migracion de Pago (#RF-21)`

---

## Task 3: `IPagoRepository` + `PagoRepository` + DI

**Files:** Create `Application/Interfaces/IPagoRepository.cs`, `Infrastructure/Repositories/PagoRepository.cs`; Modify `Infrastructure/DependencyInjection.cs`. Test: `Infrastructure.Tests/Repositories/PagoRepositoryTests.cs` (EF InMemory, siguiendo tests de repo existentes).

- [ ] **Step 1: Test** — `AddAsync` + `GetByIdAsync`; `GetByExternalReferenceAsync(Guid pagoId)` (= GetById, el external_reference es el Pago.Id); `GetByCuotaIdAsync`; `GetBySocioIdAsync` (para historial, ordenado por fecha desc, incluye Cuota).
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** interface + repo (mirror `CuotaRepository`: `_context.Pagos.Include(p => p.Cuota)...`). Registrar en `Infrastructure/DependencyInjection.cs`: `services.AddScoped<IPagoRepository, PagoRepository>();`.
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(pagos): repositorio de Pago (#RF-21)`

---

## Task 4: `IMercadoPagoService` (interface + DTOs) y validación HMAC (Infra)

**Files:** Create `Application/Interfaces/IMercadoPagoService.cs` (+ DTOs), `Infrastructure/Services/MercadoPagoService.cs`; Modify `appsettings*.json`, `Infrastructure/DependencyInjection.cs`. Test: `Infrastructure.Tests/Services/MercadoPagoServiceFirmaTests.cs`.

> **Seguridad (RN-31, CA-36):** la validación de firma es lo más crítico. Testearla primero y sola.

- [ ] **Step 1: Test de firma HMAC** — con un `WebhookSecret` conocido, un `dataId`, `requestId` y `ts` conocidos, calcular el `v1` esperado (HMAC-SHA256 hex del manifest `id:{dataId};request-id:{requestId};ts:{ts};`) y verificar que `ValidarFirma(xSignature, xRequestId, dataId)` devuelve true; con un `v1` alterado → false; con header mal formado → false.

```csharp
[Fact]
public void ValidarFirma_ConV1Correcto_DevuelveTrue()
{
    var secret = "clave-de-prueba";
    var svc = CrearServicio(secret);
    var dataId = "12345"; var requestId = "req-abc"; var ts = "1700000000";
    var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
    var v1 = HmacHex(secret, manifest);
    var xSignature = $"ts={ts},v1={v1}";
    Assert.True(svc.ValidarFirma(xSignature, requestId, dataId));
}
```

- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar**
  - `IMercadoPagoService`:
    ```csharp
    public interface IMercadoPagoService
    {
        Task<PreferenciaResultado> CrearPreferenciaAsync(Guid pagoId, decimal monto, string descripcion, string notificationUrl, BackUrls backUrls);
        Task<PagoMpInfo?> ObtenerPagoAsync(string mpPaymentId);
        bool ValidarFirma(string? xSignature, string? xRequestId, string dataId);
    }
    public record PreferenciaResultado(string PreferenceId, string InitPoint);
    public record PagoMpInfo(string Estado, string? MedioPago, string? ExternalReference, string PaymentId);
    public record BackUrls(string Success, string Failure, string Pending);
    ```
  - `MercadoPagoService` (HttpClient + IConfiguration + ILogger): 
    - `ValidarFirma`: parsear `xSignature` ("ts=...,v1=..."), armar manifest, HMAC-SHA256 con `MercadoPago:WebhookSecret`, comparar (case-insensitive, constante-time) con `v1`. Si falta secret o header → false.
    - `CrearPreferenciaAsync`: POST `https://api.mercadopago.com/checkout/preferences` con `Authorization: Bearer {MercadoPago:AccessToken}`, body `{ items:[{title:descripcion, quantity:1, unit_price:monto, currency_id:"UYU"}], external_reference:pagoId, back_urls:{...}, auto_return:"approved", notification_url }`. Devuelve `id` + `init_point`.
    - `ObtenerPagoAsync`: GET `https://api.mercadopago.com/v1/payments/{id}` con Bearer → mapear `status`, `payment_method_id`, `external_reference`.
  - `appsettings.json`: sección `"MercadoPago": { "Habilitado": false, "AccessToken": "", "WebhookSecret": "", "BackUrlBase": "http://localhost:5173" }`. **Crear** `appsettings.Development.json` (NO existe aún) con el AccessToken/WebhookSecret de **sandbox** para dev — o dejar `Habilitado:false` para el flujo simulado.
  - DI (`Infrastructure/DependencyInjection.cs`): `services.AddHttpClient<IMercadoPagoService, MercadoPagoService>();`
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(pagos): MercadoPagoService (preferencia, consulta, firma HMAC) (#RF-21)`

---

## Task 5: `IniciarPagoCuotaCommand`

**Files:** Create `Application/UseCases/Pagos/IniciarPagoCuotaCommand.cs`, `PagoDto.cs`; Modify `API/DependencyInjection.cs`. Test: `Application.Tests/UseCases/Pagos/IniciarPagoCuotaCommandTests.cs`.

- [ ] **Step 1: Test** (mock `ICuotaRepository`, `IPagoRepository`, `IMercadoPagoService`):
  - cuota Pendiente del socio → crea Pago (Pendiente) con `AddAsync`, llama `CrearPreferenciaAsync`, devuelve `{ initPoint }`.
  - cuota de OTRO socio → lanza (Unauthorized/KeyNotFound).
  - cuota ya Pagada → lanza `InvalidOperationException` (E4).
  - si `CrearPreferenciaAsync` tira → propaga error controlado (E5).
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** `ExecuteAsync(Guid cuotaId, Guid socioId): Task<IniciarPagoResultado>`: GetById; validar `cuota.SocioId == socioId` y `Estado == Pendiente`; crear `Pago(cuota.Id, socioId, cuota.Monto, "")`... (crear preferencia primero con un id temporal NO — mejor: `AddAsync(pago)` + `SaveChanges` para tener el `pago.Id`, luego `CrearPreferenciaAsync(pago.Id, ...)`, guardar `MpPreferenceId` en el pago, `SaveChanges`). Devolver initPoint. Registrar en `API/DependencyInjection.cs`.
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(pagos): comando IniciarPagoCuota (crea preferencia) (#RF-21)`

---

## Task 6: `ProcesarWebhookPagoCommand` (el corazón)

**Files:** Create `Application/UseCases/Pagos/ProcesarWebhookPagoCommand.cs`; Modify `API/DependencyInjection.cs`. Test: `Application.Tests/UseCases/Pagos/ProcesarWebhookPagoCommandTests.cs`.

- [ ] **Step 1: Tests** (mock service + repos + email + audit):
  - **firma inválida** → NO consulta MP, NO toca datos, audita "evento sospechoso", devuelve resultado `FirmaInvalida`. (CA-36)
  - **approved** → consulta MP, encuentra Pago por external_reference, `cuota.MarcarComoPagada()`, `pago.MarcarAprobado(...)`, SaveChanges, `_auditLogger.LogAsync(...)`, `_emailService.EnviarAsync(...)`. (CA-34/35)
  - **rejected** → `pago.MarcarRechazado()`, cuota **sin cambios**. (CA-37/E1)
  - **cuota ya Pagada (idempotente)** → no reprocesa, no re-manda email.
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** `ExecuteAsync(string dataId, string? xSignature, string? xRequestId): Task<WebhookResultado>`:
  1. `if (!_mp.ValidarFirma(xSignature, xRequestId, dataId))` → audit sospechoso + return FirmaInvalida.
  2. `var info = await _mp.ObtenerPagoAsync(dataId)`; if null → return.
  3. `var pago = await _pagoRepo.GetByExternalReferenceAsync(Guid.Parse(info.ExternalReference))`; if null → return.
  4. `var cuota = await _cuotaRepo.GetByIdAsync(pago.CuotaId)`.
  5. `if (info.Estado == "approved")`: si `cuota.Estado == Pagada` → idempotente, return. Sino `cuota.MarcarComoPagada()`, `pago.MarcarAprobado(info.PaymentId, info.MedioPago)`, **un solo `SaveChangesAsync`** (el DbContext es compartido → commitea Cuota + Pago atómicamente), audit, email (best-effort try-catch).
  6. `else if (info.Estado == "rejected")`: `pago.MarcarRechazado()`, SaveChanges.
  Registrar en DI.
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(pagos): comando ProcesarWebhookPago (HMAC + idempotente) (#RF-21)`

---

## Task 7: `GetMisPagosQuery` (historial)

**Files:** Create `Application/UseCases/Pagos/GetMisPagosQuery.cs`; Modify `API/DependencyInjection.cs`. Test: `Application.Tests/UseCases/Pagos/GetMisPagosQueryTests.cs`.

- [ ] **Step 1: Test** — devuelve los pagos del socio (mapeados a `PagoDto`: fecha, monto, medio, mpPaymentId, estado, nombrePlan) ordenados por fecha desc.
- [ ] **Step 2–4:** Implementar `ExecuteAsync(Guid socioId): Task<IEnumerable<PagoDto>>` usando `IPagoRepository.GetBySocioIdAsync`. Registrar DI. Tests verdes.
- [ ] **Step 5: Commit** — `feat(pagos): historial de pagos del socio (#RF-21)`

---

## Task 8: `PagosController` (endpoints)

**Files:** Create `API/Controllers/PagosController.cs`. Test: `Application.Tests/Controllers/PagosControllerTests.cs` (mirror `CuotasControllerJobsTests`).

- [ ] **Step 1: Tests** — 
  - `POST /api/pagos/iniciar` (NO usar `/cuotas/{id}/pagar`: esa ruta **PUT** ya es la acción admin "marcar pagada"): `[Authorize]` (socio), extrae socioId de `ClaimTypes.NameIdentifier`, llama command, devuelve `{ initPoint }`.
  - `POST /api/pagos/webhook`: `[AllowAnonymous]`, lee headers `x-signature`/`x-request-id` y el `data.id` del body/query, llama command; **devuelve 401 SOLO si la firma es inválida** (spoofing — no importa que MP reintente), y **200 en todos los demás casos** (procesado / pago no encontrado / pendiente) para que MP deje de reintentar. *(Esto refina el "200 siempre" del spec — decisión deliberada.)*
  - `GET /api/pagos/mis-pagos`: `[Authorize]`, socioId del claim → historial.
  - (Reflection) el webhook tiene `[AllowAnonymous]`; iniciar/mis-pagos tienen `[Authorize]`.
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** el controller (rutas arriba; el webhook parsea `data.id` — MP lo manda en query `?data.id=` o en el body `{data:{id}}`, contemplar ambos). Devolver **401 si firma inválida, 200 en el resto**.
- [ ] **Step 4: Run → PASS** + `dotnet test` (todo el backend) verde.
- [ ] **Step 5: Commit** — `feat(pagos): endpoints iniciar/webhook/mis-pagos (#RF-21)`

---

## Task 9: Frontend — `pagosApi` + botón "Pagar con Mercado Pago"

**Files:** Modify `frontend/src/services/api.ts`, `frontend/src/pages/portal/MisCuotasPage.tsx`. Test: `frontend/src/pages/portal/__tests__/MisCuotasPage.test.tsx` (crear o extender).

- [ ] **Step 1: Test** — en una cuota Pendiente, el botón "Pagar con Mercado Pago" llama `pagosApi.iniciar(cuotaId)` y redirige a `initPoint` (mockear `window.location` / el api). Cuota Pagada → sin botón.
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** en `api.ts`: `pagosApi = { iniciar: (cuotaId) => api.post('/pagos/iniciar', { cuotaId }).then(r => r.data), getMisPagos: () => api.get('/pagos/mis-pagos').then(r => r.data) }`. En `MisCuotasPage`: reemplazar **los DOS** botones `disabled`/"Pagar (próximamente)" (el de la tabla desktop ~L90 y el de la card mobile ~L133) por uno que en `onClick` llame `iniciar` y haga `window.location.href = initPoint` (con estado de loading + manejo de error → toast "No se pudo iniciar el pago").
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(portal): boton pagar cuota con Mercado Pago (#RF-21)`

---

## Task 10: Frontend — páginas de retorno + historial + rutas

**Files:** Create `frontend/src/pages/portal/PagoResultadoPage.tsx`, `MisPagosPage.tsx`; Modify `frontend/src/App.tsx` (+ nav del portal si aplica). Tests correspondientes.

- [ ] **Step 1: Tests** — `PagoResultadoPage` muestra el mensaje según el status de la query (`?status=approved|failure|pending` o la ruta), con link a "Mis cuotas"; `MisPagosPage` lista los pagos de `getMisPagos` (fecha, monto, medio, N° transacción, estado) usando `formatDate`/`formatDateTime`.
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** las 2 páginas + rutas en `App.tsx` bajo `/portal`: `pago/resultado` (back_urls de MP apuntan acá con el status), `mis-pagos`. La de éxito puede invalidar la query `mis-cuotas` para refrescar. Agregar "Mis pagos" al nav del portal si hay uno.
- [ ] **Step 4: Run → PASS** + `npm run build` OK.
- [ ] **Step 5: Commit** — `feat(portal): paginas de retorno de pago e historial (#RF-21)`

---

## Task 11: Config de producción — workflow de secrets + docs

**Files:** Create `.github/workflows/configure-mercadopago.yml`; Modify `docs/deploy/SETUP-CICD.md`.

- [ ] **Step 1:** Crear `configure-mercadopago.yml` (mirror `configure-email.yml`): `workflow_dispatch`; toma GitHub secrets `MERCADOPAGO_ACCESS_TOKEN` + `MERCADOPAGO_WEBHOOK_SECRET`; `az containerapp secret set` → `mercadopago-access-token`, `mercadopago-webhook-secret`; `az containerapp update --set-env-vars "MercadoPago__Habilitado=true" "MercadoPago__AccessToken=secretref:mercadopago-access-token" "MercadoPago__WebhookSecret=secretref:mercadopago-webhook-secret" "MercadoPago__BackUrlBase=https://<app-url>"`. Validar que los secrets existan antes.
- [ ] **Step 2:** Documentar en `SETUP-CICD.md` la sección "Activar Mercado Pago": crear la app en MP, sacar Access Token + Webhook Secret de **prueba**, cargar los 2 GitHub secrets, configurar el webhook en MP apuntando a `…/api/pagos/webhook`, correr el workflow, probar con usuario/tarjetas de prueba.
- [ ] **Step 3: Commit** — `ci(pagos): workflow configure-mercadopago + docs (#RF-21)`

---

## Task 12: Verificación final + PR

- [ ] **Step 1:** `dotnet test` (backend) verde; `npm test` + `npm run build` (frontend) verde.
- [ ] **Step 2:** `dotnet ef migrations has-pending-model-changes` → sin pendientes.
- [ ] **Step 3:** Push `feature/rf23-mercadopago` + `gh pr create --base main` con body: qué incluye, que la migración `AddPagos` se auto-aplica al arrancar (tabla nueva, no destructivo), que requiere cargar 2 secrets + correr el workflow para probar, y "Closes #RF-21" si hay issue. NO mergear.
- [ ] **Step 4:** Reportar PR URL + conteo de tests.

---

## Notas
- **YAGNI:** sin reconciliación por polling, sin flujo avanzado de transferencia pendiente (E2 fuera del MVP — anotado en el spec).
- **Seguridad:** el webhook NUNCA modifica datos sin firma válida + confirmación real de MP (`ObtenerPagoAsync`). Idempotencia por estado de la cuota.
- **Moneda:** `UYU`. **Dev:** `MercadoPago:Habilitado=false` deja el flujo simulado si hace falta; en prod los secrets lo prenden.
- **Prueba E2E:** contra el Container App deployado (MP necesita URL pública para el webhook), con usuario comprador de prueba y tarjetas de test (`APRO`=aprobado, `OTHE`=rechazado).
