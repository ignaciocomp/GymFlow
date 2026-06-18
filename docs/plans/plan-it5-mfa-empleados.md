# MFA TOTP para Empleados — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: usar superpowers:subagent-driven-development. Cada tarea con TDD estricto (test rojo → mínimo verde → suite completa → commit). Pasos con checkbox `- [ ]`.

**Goal:** Agregar segundo factor TOTP obligatorio para empleados (no socios) en el login de GymFlow.

**Architecture:** Login en dos pasos con `mfaToken` intermedio firmado con clave dedicada (`Mfa:TokenSigningKey`, distinta de `Jwt:Key`, así el pipeline JWT global lo rechaza solo). Secreto TOTP cifrado en reposo con AES-256-GCM nativo. Códigos de recuperación hasheados de un solo uso. Lockout de 5 intentos / 15 min persistido en DB. Campos MFA en `Empleado` (TPH). Clean Architecture: Domain (entidad+métodos), Application (interfaces+commands), Infrastructure (TOTP/cifrado/repos), API (endpoints).

**Tech Stack:** .NET 8, EF Core (PostgreSQL, TPH), Otp.NET (TOTP), QRCoder (QR), `System.Security.Cryptography.AesGcm` (nativo), xUnit+Moq. Frontend React 18 + TS.

**Spec:** [[spec-it5-mfa-empleados]]. **Rama:** `feature/it5-mfa-empleados` (base develop).

**Regla de oro:** TODO mensaje al usuario en español rioplatense. Tras CADA tarea: `dotnet test backend/GymFlow.sln` debe quedar 100% verde antes del commit.

---

## File Structure

- `backend/src/GymFlow.Domain/Enums/TipoAccionAuditoria.cs` — (modif) append valores MFA
- `backend/src/GymFlow.Domain/Entities/Empleado.cs` — (modif) campos + métodos MFA
- `backend/src/GymFlow.Domain/Entities/CodigoRecuperacionMfa.cs` — (nuevo) entidad
- `backend/src/GymFlow.Application/Interfaces/ITotpService.cs` — (nuevo)
- `backend/src/GymFlow.Application/Interfaces/IMfaSecretProtector.cs` — (nuevo)
- `backend/src/GymFlow.Application/Interfaces/IMfaTokenService.cs` — (nuevo)
- `backend/src/GymFlow.Application/Interfaces/ICodigoRecuperacionMfaRepository.cs` — (nuevo)
- `backend/src/GymFlow.Application/UseCases/Auth/Mfa/*.cs` — (nuevo) commands
- `backend/src/GymFlow.Infrastructure/Services/TotpService.cs` — (nuevo)
- `backend/src/GymFlow.Infrastructure/Services/AesGcmMfaSecretProtector.cs` — (nuevo)
- `backend/src/GymFlow.Infrastructure/Services/MfaTokenService.cs` — (nuevo)
- `backend/src/GymFlow.Infrastructure/Repositories/CodigoRecuperacionMfaRepository.cs` — (nuevo)
- `backend/src/GymFlow.Infrastructure/Persistence/Configurations/*` + Migrations — (modif/nuevo)
- `backend/src/GymFlow.API/Controllers/AuthController.cs` — (modif) login + endpoints MFA
- `backend/src/GymFlow.API/Controllers/EmpleadosController.cs` — (modif) reset
- `backend/src/GymFlow.API/DependencyInjection.cs` + `Infrastructure/DependencyInjection.cs` + `appsettings*.json` — (modif) DI + config
- `frontend/src/pages/Login.tsx`, `context/AuthContext.tsx`, `services/api.ts`, nuevos componentes MFA — (modif/nuevo)

---

### Task 1: Valores de auditoría MFA

**Files:** Modify `backend/src/GymFlow.Domain/Enums/TipoAccionAuditoria.cs`

- [ ] **Step 1:** Append al enum (después del último valor, preservando los ints existentes): `MfaActivado`, `MfaVerificado`, `MfaCodigoRecuperacionUsado`, `MfaBloqueado`, `MfaReseteadoPorAdmin`.
- [ ] **Step 2:** `dotnet build backend/GymFlow.sln` → compila.
- [ ] **Step 3:** Commit `feat(auth): valores de auditoria para MFA`.

(No requiere test propio; es un enum. Se ejercita en tareas siguientes.)

---

### Task 2: Entidad `CodigoRecuperacionMfa` + campos y métodos MFA en `Empleado`

**Files:**
- Create: `backend/src/GymFlow.Domain/Entities/CodigoRecuperacionMfa.cs`
- Modify: `backend/src/GymFlow.Domain/Entities/Empleado.cs`
- Test: `backend/tests/GymFlow.Domain.Tests/Entities/EmpleadoMfaTests.cs`

- [ ] **Step 1 (RED):** Escribir tests de dominio en `EmpleadoMfaTests`:
  - `ActivarMfa_SeteaSecretoYHabilita`: tras `ActivarMfa(secretoProtegido)`, `MfaHabilitado==true` y `MfaSecret==secretoProtegido`.
  - `RegistrarIntentoFallido_AlQuinto_Bloquea`: 4 intentos → no bloqueado; el 5º setea `MfaBloqueadoHasta = ahora+15m` y `EstaBloqueadoMfa(ahora) == true`.
  - `RegistrarVerificacionExitosa_ReseteaContadorYBloqueo`: tras fallos, una verificación exitosa deja contador en 0 y `MfaBloqueadoHasta==null`.
  - `EstaBloqueadoMfa_TrasExpirar_DevuelveFalse`: con `MfaBloqueadoHasta` en el pasado → false.
  - `ResetearMfa_LimpiaTodo`: `MfaHabilitado==false`, `MfaSecret==null`, contador 0, bloqueo null.
  - Nota: los métodos reciben `DateTime ahora` como parámetro (no usar `DateTime.UtcNow` interno) para testeabilidad.
- [ ] **Step 2:** Correr `dotnet test --filter EmpleadoMfaTests` → FAIL de compilación (métodos/campos inexistentes). Verificar.
- [ ] **Step 3 (GREEN):** Implementar en `Empleado` los campos (`MfaSecret` string?, `MfaHabilitado` bool, `MfaIntentosFallidos` int, `MfaBloqueadoHasta` DateTime?) con setters privados, y los métodos: `ActivarMfa(string secretoProtegido)`, `RegistrarIntentoFallidoMfa(DateTime ahora)` (incrementa; al llegar a 5 setea bloqueo +15m), `EstaBloqueadoMfa(DateTime ahora)`, `RegistrarVerificacionExitosaMfa()`, `ResetearMfa()`. Crear `CodigoRecuperacionMfa` (Id, EmpleadoId, CodigoHash, Usado, FechaUso?) con método `MarcarUsado(DateTime)`.
- [ ] **Step 4:** `dotnet test --filter EmpleadoMfaTests` → PASS. Luego suite completa → PASS.
- [ ] **Step 5:** Commit `feat(auth): campos y metodos de dominio MFA en Empleado + CodigoRecuperacionMfa`.

---

### Task 3: Cifrado del secreto — `IMfaSecretProtector` (AES-256-GCM)

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/IMfaSecretProtector.cs` (`string Protect(string)`, `string Unprotect(string)`)
- Create: `backend/src/GymFlow.Infrastructure/Services/AesGcmMfaSecretProtector.cs`
- Test: `backend/tests/GymFlow.Infrastructure.Tests/Services/AesGcmMfaSecretProtectorTests.cs`

- [ ] **Step 1 (RED):** Tests:
  - `ProtectUnprotect_RoundTrip`: `Unprotect(Protect("JBSWY3DPEHPK3PXP")) == "JBSWY3DPEHPK3PXP"`.
  - `Protect_DosVeces_DaResultadosDistintos`: nonce fresco → dos `Protect` del mismo input difieren (no determinista).
  - `Unprotect_ConBlobAdulterado_Lanza`: alterar un byte del blob → excepción (tag GCM detecta).
  - El protector se construye con una clave AES-256 (32 bytes) de prueba.
- [ ] **Step 2:** Correr → FAIL (clase inexistente). Verificar.
- [ ] **Step 3 (GREEN):** Implementar con `System.Security.Cryptography.AesGcm`: nonce aleatorio 96 bits por op (`RandomNumberGenerator`), tag 128 bits, salida `base64(nonce ‖ ciphertext ‖ tag)`. La clave se inyecta desde config (`Mfa:EncryptionKey`, base64 de 32 bytes); validar longitud en el ctor.
- [ ] **Step 4:** Tests → PASS, suite completa → PASS.
- [ ] **Step 5:** Commit `feat(auth): IMfaSecretProtector con AES-256-GCM`.

---

### Task 4: TOTP + códigos de recuperación — `ITotpService`

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/ITotpService.cs`
- Create: `backend/src/GymFlow.Infrastructure/Services/TotpService.cs` (NuGet `Otp.NET`)
- Test: `backend/tests/GymFlow.Infrastructure.Tests/Services/TotpServiceTests.cs`

- [ ] **Step 1:** `dotnet add backend/src/GymFlow.Infrastructure package Otp.NET`.
- [ ] **Step 2 (RED):** Tests:
  - `GenerarSecreto_DevuelveBase32Valido` (160 bits).
  - `ValidarCodigo_ConCodigoGeneradoAhora_True`: generar el código actual con el mismo secreto (vía Otp.NET) y validarlo → true.
  - `ValidarCodigo_ConCodigoIncorrecto_False`.
  - `GenerarUri_DevuelveOtpauthConIssuerYCuenta`: el `otpauth://totp/...` contiene issuer `GymFlow` y el correo.
  - `GenerarCodigosRecuperacion_Devuelve10Distintos` con ~50 bits cada uno.
  - Interface: `string GenerarSecreto()`, `bool ValidarCodigo(string secreto, string codigo)` (ventana ±1 step), `string GenerarUriOtpauth(string secreto, string cuenta)`, `IReadOnlyList<string> GenerarCodigosRecuperacion()`.
  - **Hash de códigos de recuperación (DECISIÓN ÚNICA, vale para Tasks 8 y 9):** se reusa el `IPasswordHasher` (BCrypt) **ya existente y registrado** (`BCryptPasswordHasher`). Al persistir: `hasher.Hash(codigo)`. Al verificar: iterar los códigos activos del empleado y `hasher.Verify(codigoIngresado, codigo.CodigoHash)`. No se introduce SHA-256 ni dependencia nueva. `ITotpService` NO hashea (solo genera en claro); el hash lo hace el command con `IPasswordHasher`.
- [ ] **Step 3:** Correr → FAIL. Verificar.
- [ ] **Step 4 (GREEN):** Implementar con Otp.NET (`Totp`, `Base32Encoding`), ventana de verificación ±1 (`VerificationWindow(1,1)`).
- [ ] **Step 5:** Tests → PASS, suite completa → PASS.
- [ ] **Step 6:** Commit `feat(auth): ITotpService con Otp.NET (TOTP + codigos de recuperacion)`.

---

### Task 5: `IMfaTokenService` — token intermedio con clave separada

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/IMfaTokenService.cs`
- Create: `backend/src/GymFlow.Infrastructure/Services/MfaTokenService.cs`
- Test: `backend/tests/GymFlow.Infrastructure.Tests/Services/MfaTokenServiceTests.cs`

- [ ] **Step 1 (RED):** Tests:
  - `EmitirYValidar_RoundTrip`: `Emitir(userId, "mfa-pending")` → `Validar(token, "mfa-pending")` devuelve el userId.
  - `Validar_ConPurposeDistinto_Falla`: token de `mfa-setup` validado contra `mfa-pending` → null/false.
  - `Validar_ConOtraClave_Falla`: un token firmado con `Jwt:Key` no valida con la clave de MFA (firma distinta).
  - `Validar_Expirado_Falla`.
  - Interface: `string Emitir(Guid userId, string purpose)` (exp ~5 min), `Guid? Validar(string token, string purposeEsperado)`. Claims: `sub`, `purpose`, `exp`. SIN rolId/permisos.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Implementar con `JwtSecurityTokenHandler` usando `Mfa:TokenSigningKey` (de config). **Formato de la clave:** string UTF-8 de ≥32 chars (igual patrón que `Jwt:Key`), usada con `new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))` y firma **HmacSha256** (idéntico a `GenerateJwt`). DEBE ser distinta de `Jwt:Key`. Validar firma + purpose + exp con `ValidateIssuerSigningKey=true`, `ValidateIssuer/Audience=false`, `ClockSkew=Zero`.
- [ ] **Step 4:** Tests → PASS, suite completa → PASS.
- [ ] **Step 5:** Commit `feat(auth): IMfaTokenService firmado con clave dedicada`.

---

### Task 6: Repositorio de códigos de recuperación + EF config + migración

**Files:**
- Create: `backend/src/GymFlow.Application/Interfaces/ICodigoRecuperacionMfaRepository.cs`
- Create: `backend/src/GymFlow.Infrastructure/Repositories/CodigoRecuperacionMfaRepository.cs`
- Modify: EmpleadoRepository / ISocioRepository? — usar el repo de empleados para persistir el Empleado; agregar `SaveChangesAsync` si falta.
- Create: `CodigoRecuperacionMfaConfiguration` (config EF de la entidad hija). Agregar `DbSet<CodigoRecuperacionMfa>` a `GymFlowDbContext`.
- Migración EF.

- [ ] **Step 1:** Interface `ICodigoRecuperacionMfaRepository`: `Task AgregarRangoAsync(IEnumerable<CodigoRecuperacionMfa>)`, `Task<IReadOnlyList<CodigoRecuperacionMfa>> GetActivosPorEmpleadoAsync(Guid empleadoId)`, `Task EliminarPorEmpleadoAsync(Guid empleadoId)`, `Task SaveChangesAsync()`. Para persistir el `Empleado` modificado, reusar el repo de empleados existente (`IEmpleadoRepository.SaveChangesAsync` si existe; si no, usar el mismo `DbContext`/repo que ya usan los commands de empleados).
- [ ] **Step 2:** EF config. **Las columnas escalares de MFA se mapean POR CONVENCIÓN** al ser props de `Empleado` (igual que las props de `Socio`): NO hace falta `EmpleadoConfiguration` ni tocar `UsuarioConfiguration`. Lo obligatorio: (a) `DbSet<CodigoRecuperacionMfa>` en `GymFlowDbContext`; (b) `CodigoRecuperacionMfaConfiguration` (FK a `Usuarios` por EmpleadoId, índice por EmpleadoId, `CodigoHash` requerido).
- [ ] **Step 3:** Implementar el repo.
- [ ] **Step 4:** Generar migración: `dotnet ef migrations add AgregarMfaEmpleado --project backend/src/GymFlow.Infrastructure --startup-project backend/src/GymFlow.API`. Verificar que solo agregue columnas nullable + la tabla nueva (sin cambios espurios).
- [ ] **Step 5:** `dotnet build` + suite completa → PASS.
- [ ] **Step 6:** Commit `feat(auth): persistencia MFA (repo codigos + EF config + migracion)`.

---

### Task 7: Command de setup — `IniciarMfaSetupCommand`

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Auth/Mfa/IniciarMfaSetupCommand.cs`
- Test: `backend/tests/GymFlow.Application.Tests/UseCases/Auth/Mfa/IniciarMfaSetupCommandTests.cs`

**Decisión cerrada del reparto setup/activate (DESVÍA de la spec — la spec entregaba los códigos en setup; acá van en activate, para no persistir códigos de un enrolment abandonado; registrar la desviación en Task 14):**
- `setup`: genera el secreto (`ITotpService.GenerarSecreto`), lo **cifra** (`IMfaSecretProtector.Protect`) y lo **persiste en el Empleado con `MfaHabilitado=false`**; devuelve `{ secretoBase32 (en claro, para el QR/clave manual), uriOtpauth }`. **No** genera ni persiste códigos de recuperación.
- `activate` (Task 8): valida el primer código; recién ahí genera, persiste (hasheados) y **devuelve en claro** los 10 códigos de recuperación, y pone `MfaHabilitado=true`.

- [ ] **Step 1 (RED):** Test: dado un empleado sin MFA, `IniciarMfaSetupAsync(empleadoId)` genera secreto vía `ITotpService`, lo cifra vía `IMfaSecretProtector`, lo persiste en el Empleado (`MfaHabilitado` sigue false), y devuelve `{ secretoBase32, uriOtpauth }`. Mock de repos/servicios.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Implementar.
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(auth): IniciarMfaSetupCommand`.

---

### Task 8: Command de activación — `ActivarMfaCommand`

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Auth/Mfa/ActivarMfaCommand.cs`
- Test: `backend/tests/GymFlow.Application.Tests/UseCases/Auth/Mfa/ActivarMfaCommandTests.cs`

- [ ] **Step 1 (RED):** Tests:
  - `CodigoValido_ActivaYGeneraCodigos`: valida el código contra el secreto guardado; si ok, marca `MfaHabilitado=true`, genera 10 códigos de recuperación, los persiste hasheados, y devuelve los códigos en claro + señal de éxito.
  - `CodigoInvalido_NoActiva_Lanza`: código incorrecto → excepción, `MfaHabilitado` sigue false, no se generan códigos.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Implementar (descifra secreto, valida con `ITotpService`, hashea códigos, persiste, audita `MfaActivado`).
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(auth): ActivarMfaCommand`.

---

### Task 9: Commands de verificación — `VerificarMfaCommand` + `UsarCodigoRecuperacionCommand`

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Auth/Mfa/VerificarMfaCommand.cs`, `.../Mfa/UsarCodigoRecuperacionCommand.cs`
- Test: `backend/tests/GymFlow.Application.Tests/UseCases/Auth/Mfa/VerificarMfaCommandTests.cs`, `.../UsarCodigoRecuperacionCommandTests.cs`

- [ ] **Step 1 (RED):** Tests Verificar:
  - `Bloqueado_Lanza`: si `EstaBloqueadoMfa(now)` → excepción sin validar.
  - `CodigoValido_DevuelveEmpleadoYResetea`: ok → registra verificación exitosa (resetea contador), audita `MfaVerificado`, devuelve el empleado para emitir JWT.
  - `CodigoInvalido_SumaIntento_Lanza`: incorrecto → `RegistrarIntentoFallidoMfa`, persiste, excepción E2.
  - `QuintoFallo_Bloquea`: el 5º incorrecto deja bloqueado y audita `MfaBloqueado`.
  - Tests Recuperación:
  - `CodigoRecuperacionValido_ConsumeYEntra`: matchea un código activo (hash), lo marca usado, resetea contador, audita `MfaCodigoRecuperacionUsado`, devuelve empleado.
  - `CodigoRecuperacionInvalido_SumaIntento`: cuenta para el mismo lockout.
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Implementar ambos. **Manejo del tiempo:** el command captura `DateTime.UtcNow` al entrar y se lo pasa a los métodos de dominio (`EstaBloqueadoMfa(now)`, `RegistrarIntentoFallidoMfa(now)`). NO se introduce `IClock` (no existe en el repo); el comportamiento de bloqueo con tiempo controlado ya queda testeado a nivel dominio en Task 2, y los tests de command verifican el efecto observable (intentos incrementados / excepción / códigos consumidos) con repos mockeados. El hash de recuperación: `IPasswordHasher.Verify(codigoIngresado, codigo.CodigoHash)` (BCrypt, decisión de Task 4).
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(auth): VerificarMfaCommand + UsarCodigoRecuperacionCommand`.

---

### Task 10: Command de reset por admin — `ResetearMfaEmpleadoCommand`

**Files:**
- Create: `backend/src/GymFlow.Application/UseCases/Auth/Mfa/ResetearMfaEmpleadoCommand.cs`
- Test: `backend/tests/GymFlow.Application.Tests/UseCases/Auth/Mfa/ResetearMfaEmpleadoCommandTests.cs`

- [ ] **Step 1 (RED):** Tests:
  - `Reset_LimpiaTodoYAudita`: `ResetearMfa()` en el empleado, elimina sus códigos, audita `MfaReseteadoPorAdmin`.
  - `NoSePuedeResetearseASiMismo`: si `adminId == empleadoId` → excepción (la regla también se valida en el controller, pero el command la enforce).
- [ ] **Step 2:** Correr → FAIL. Verificar.
- [ ] **Step 3 (GREEN):** Implementar.
- [ ] **Step 4:** Tests + suite → PASS.
- [ ] **Step 5:** Commit `feat(auth): ResetearMfaEmpleadoCommand`.

---

### Task 11: API — login en dos pasos + endpoints MFA + reset

**Files:**
- Modify: `backend/src/GymFlow.API/Controllers/AuthController.cs`
- Modify: `backend/src/GymFlow.API/Controllers/EmpleadosController.cs`
- Modify: `backend/src/GymFlow.API/DependencyInjection.cs`, `Infrastructure/DependencyInjection.cs`, `appsettings.json`
- NuGet `QRCoder` en Infrastructure (o API) para el QR.

- [ ] **Step 1:** Config: agregar `Mfa:EncryptionKey` (base64 de 32 bytes) y `Mfa:TokenSigningKey` (string UTF-8 ≥32 chars, distinta de `Jwt:Key`) a **`appsettings.json`** (NO existe `appsettings.Development.json` en el repo; las claves de dev viven en `appsettings.json`, igual que `Jwt:Key`). En producción se sobreescriben por env-vars/secrets del Container App (Task 13). Registrar en DI: `ITotpService`, `IMfaSecretProtector`, `IMfaTokenService`, `ICodigoRecuperacionMfaRepository`, y los 5 commands (`AddScoped`).
- [ ] **Step 2:** Definir DTOs: `LoginResultado(bool RequiereMfa, bool SetupRequerido, string? MfaToken, LoginResponse? Sesion)`; requests `MfaActivarRequest(string Codigo)`, `MfaVerificarRequest(string Codigo)`, `MfaRecoveryRequest(string Codigo)`. Los endpoints `/mfa/*` reciben el `mfaToken` por header `Authorization: Bearer` y lo validan con `IMfaTokenService` (NO `[Authorize]`/`[RequierePermiso]`).
- [ ] **Step 3:** Modificar `/auth/login`: si el correo corresponde a un **empleado** válido (password ok), en vez del JWT devolver `LoginResultado{ RequiereMfa=true, SetupRequerido = !empleado.MfaHabilitado, MfaToken = emitir(purpose setup|pending) }`. Socio/legacy → `LoginResultado{ RequiereMfa=false, Sesion=LoginResponse(...) }`. Reutilizar `GenerateJwt` para la sesión.
- [ ] **Step 4:** Endpoints: `POST /auth/mfa/setup` (valida mfaToken setup → `IniciarMfaSetupCommand` → devuelve uri, **QR data URI** generado con QRCoder, clave manual; los códigos NO acá), `POST /auth/mfa/activate` (valida mfaToken setup → `ActivarMfaCommand` → devuelve `LoginResponse` + códigos de recuperación una vez), `POST /auth/mfa/verify` (valida mfaToken pending → `VerificarMfaCommand` → `LoginResponse`), `POST /auth/mfa/recovery` (→ `UsarCodigoRecuperacionCommand` → `LoginResponse`). Mapear excepciones: bloqueo → **429 Too Many Requests** con mensaje "Demasiados intentos. Probá de nuevo en unos minutos."; código inválido → 401 "Código incorrecto o expirado." (Usar tipos de excepción distinguibles desde los commands, p.ej. `MfaBloqueadoException` vs `InvalidOperationException`/`UnauthorizedAccessException`, para mapear bien el status.) Extracción del `mfaToken`: replicar el patrón de `AuthController.Me()` (`authHeader["Bearer ".Length..]`).
- [ ] **Step 5:** `EmpleadosController`: `POST /{id}/mfa/reset` con `[RequierePermiso(Modulo.Empleados, Operacion.Modificacion)]`; comparar `{id}` con `userId` del JWT (NameIdentifier) → si igual, 400; si no, `ResetearMfaEmpleadoCommand`.
- [ ] **Step 6:** `dotnet build` + suite completa → PASS. (Los commands ya tienen cobertura; el controller se valida por compilación + smoke manual.)
- [ ] **Step 7:** Commit `feat(auth): login en dos pasos + endpoints MFA + reset (API)`.

---

### Task 12: Frontend — pantallas de enrolment y verificación + reset

**Files:**
- Modify: `frontend/src/services/api.ts` (authApi: el login devuelve `LoginResultado`; nuevos `mfaSetup/mfaActivate/mfaVerify/mfaRecovery`, `resetearMfaEmpleado`)
- Modify: `frontend/src/context/AuthContext.tsx` (manejar `LoginResultado`: si requiere MFA, guardar `mfaToken` en memoria y exponer estado; al completar, `aplicarSesion`)
- Modify: `frontend/src/pages/Login.tsx` (ramificar a pantalla MFA)
- Create: `frontend/src/pages/mfa/MfaSetupPage.tsx` (QR + clave + confirmar código + códigos de recuperación) y `MfaVerifyPage.tsx` (6 dígitos + link recuperación) — o componentes inline en Login.
- Modify: ficha de empleado en admin (botón "Resetear MFA").

- [ ] **Step 1:** `api.ts`: tipar `LoginResultado` y agregar las llamadas MFA (mandan `Authorization: Bearer <mfaToken>`).
- [ ] **Step 2:** `AuthContext`: `login()` ahora devuelve el `LoginResultado`; agregar flujo MFA (guardar mfaToken, funciones `mfaVerify`/`mfaActivate`/`mfaRecovery` que al éxito hacen `aplicarSesion`).
- [ ] **Step 3:** `Login.tsx`: tras password, si `requiereMfa` → mostrar setup o verify según `setupRequerido`. Setup: mostrar QR (img src=dataUri), clave copiable, input de código, y al activar mostrar los códigos de recuperación (copiar/descargar) antes de entrar. Verify: input de 6 dígitos + "usar código de recuperación". Errores en el lugar de error actual.
- [ ] **Step 4:** Admin: botón "Resetear MFA" en la ficha/lista de empleados (llama `resetearMfaEmpleado(id)`, con confirmación).
- [ ] **Step 5:** `npm run build` + `npx vitest run` → PASS.
- [ ] **Step 6:** Commit `feat(auth): UI de MFA (enrolment, verificacion, reset) (IT5)`.

---

### Task 13: Docs de deploy + cierre

**Files:** Modify `docs/deploy/SETUP-CICD.md`.

- [ ] **Step 1:** Sección "Activar MFA": generar `Mfa:EncryptionKey` (32 bytes base64) y `Mfa:TokenSigningKey`, cargarlas como secrets del Container App (vía un workflow análogo a configure-email, o documentar el comando `az containerapp secret set` + `--set-env-vars Mfa__EncryptionKey=secretref:...`). Documentar el **procedimiento de reset de emergencia del admin** (reset de `MfaHabilitado=false` vía DB) y la recomendación de ≥2 cuentas con permiso de gestión de empleados.
- [ ] **Step 2:** Commit `docs(deploy): activacion de MFA y reset de emergencia`.

---

### Task 14: Review final

- [ ] Reviewer adversarial sobre el diff completo vs develop: seguridad (la separación de claves que garantiza el rechazo del `mfaToken` en endpoints normales queda cubierta por el test unitario `MfaTokenService.Validar_ConOtraClave_Falla` de Task 5 — NO se monta `WebApplicationFactory` nuevo; verificar que ese test existe y es real; secreto nunca en claro en DB/logs/respuestas; lockout no bypasseable; QR/secreto no logueado), migración consistente, contrato `/login` (chequear que no haya otros consumidores del shape plano además del frontend — p.ej. la colección Postman), build backend+frontend y suites completas en verde. Crear el PR a develop con descripción del cambio breaking de `/login`.
