---
tags:
  - spec
  - iteracion
requerimiento: RNF-10, RN-18, RN-28, CU-05, CA-20, E2
---

# MFA (Doble Factor TOTP) para Empleados — Spec

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] — RNF-10, RN-18, RN-28, CU-05 (flujo principal), CA-20, E2
**Plan de implementación:** [[plan-it5-mfa-empleados]]
**Spec hermana:** [[spec-it5-login-google]] (la otra mitad de IT5)
**Última actualización:** 2026-06-16 (v2: incorpora revisión de seguridad)

## Resumen

Segundo factor de autenticación obligatorio para **empleados** (usuarios no-Socio: admin, profesor y cualquier rol interno), basado en **TOTP** (RFC 6238) con apps autenticadoras (Google/Microsoft Authenticator). Los socios no usan MFA (se autentican con Google, ver spec hermana). El login pasa de un solo paso a dos: contraseña → código TOTP.

## Decisiones de diseño

### Alcance
- MFA **obligatorio para todos los empleados** (toda entidad `Empleado`). Reconcilia RN-18 (admin/profesor) con RN-28 (todo empleado) hacia la opción más segura.
- Los **socios nunca pasan por MFA**; su login es Google OAuth.

### Estándar TOTP
- RFC 6238: **6 dígitos, período 30s, HMAC-SHA1** (defaults de los authenticators).
- Tolerancia de **±1 step** (±30s) por desfasaje de reloj al validar.
- Librerías: **Otp.NET** (generación/validación TOTP) y **QRCoder** (QR del URI `otpauth://`). El secreto se genera con un RNG criptográfico (160 bits, base32). El cifrado usa `System.Security.Cryptography.AesGcm` **nativo de .NET 8** (sin librería extra).

### Flujo de login en dos pasos (token intermedio con clave separada)
El endpoint `POST /api/auth/login` deja de devolver el JWT de sesión directamente cuando el usuario es **empleado**:

1. Valida correo + contraseña (BCrypt) como hoy.
2. Si es **empleado**, responde **200** con un **`mfaToken`** y un flag de si requiere alta. **No** emite JWT de sesión.
3. El flujo de socios (Google) queda sin cambios.

**El `mfaToken` se firma con una clave DEDICADA (`Mfa:TokenSigningKey`), distinta de `Jwt:Key`.** Esto es clave de seguridad: el `AddJwtBearer` global solo conoce `Jwt:Key`, así que **cualquier `mfaToken` presentado como Bearer contra un endpoint normal es rechazado automáticamente (401)** sin depender de validar claims en cada gate. Los endpoints `/auth/mfa/*` validan el `mfaToken` **a mano** con `Mfa:TokenSigningKey` (como ya hace `/auth/me` con su propia validación), y además chequean el claim `purpose`:
- `purpose=mfa-setup` → solo válido para `/mfa/setup` y `/mfa/activate`.
- `purpose=mfa-pending` → solo válido para `/mfa/verify` y `/mfa/recovery`.
- Claims del `mfaToken`: `sub` (userId), `purpose`, `exp` (~5 min). NO lleva `rolId` ni permisos (no debe poder reconstruirse un token de sesión a partir de él).

**Reuso del `mfaToken`:** es un JWT stateless reutilizable dentro de su ventana de ~5 min (necesario para reintentos de código). El riesgo residual de un `mfaToken` robado está **acotado por el bloqueo anti-fuerza-bruta persistido en DB** (no en el token): aunque se reuse, tras 5 códigos fallidos el paso MFA se bloquea. Tras un `/mfa/verify` exitoso el token sigue siendo criptográficamente válido hasta su `exp`, pero solo permite re-verificar (no autoriza la API). Se **acepta explícitamente** el riesgo residual de que un `mfaToken` robado dentro de la ventana de 5 min permita re-emitir un JWT de sesión (re-verificando con un código aún válido), a cambio de mantener la API stateless; no se introduce lista de revocación ni `jti` de un solo uso en esta entrega.

### Enrolment (RN-28 — configurar antes de operar)
Si el empleado no tiene MFA activado, tras la contraseña entra al alta de MFA:

- `POST /api/auth/mfa/setup` (con `mfaToken` de setup): genera un secreto TOTP (aún no activado), **persiste el secreto cifrado en el empleado con `MfaHabilitado=false`**, y devuelve el URI `otpauth://`, el **QR como data URI PNG** (generado server-side con QRCoder) y la **clave manual** (base32). Todavía no genera códigos de recuperación (así un enrolment abandonado no deja códigos persistidos).
- `POST /api/auth/mfa/activate` (`mfaToken` + código de la app): valida el código contra el secreto; si es correcto, **genera 10 códigos de recuperación de un solo uso** (los devuelve en claro **una sola vez** en esta respuesta, y los persiste hasheados), marca MFA activado y **emite el JWT de sesión** (login completo).
- Hasta activarlo, el empleado no obtiene JWT de sesión ni puede operar.
- El admin semilla (`admin@gymflow.com`) pasa por este enrolment en su primer login tras el release.

### Verificación en logins siguientes
- `POST /api/auth/mfa/verify` (`mfaToken` de pending + código de 6 dígitos): valida el TOTP → emite el JWT de sesión.
- `POST /api/auth/mfa/recovery` (`mfaToken` + código de recuperación): valida y **consume** (marca usado) un código de recuperación → emite el JWT. Para cuando se pierde el dispositivo.
- Ambos endpoints **evalúan el bloqueo** (`MfaBloqueadoHasta`) al entrar y **suman intento fallido** ante código incorrecto. Los códigos de recuperación cuentan para el mismo contador de bloqueo (no son fuerza-bruteables aparte).

### Modelo de datos
Los campos MFA viven en **`Empleado`** (no en `Usuario` base): a diferencia de `GoogleUserId` —que los socios sí usan—, el MFA es exclusivo de empleados, así que no se ensucia `Socio` con columnas muertas. EF Core mapea la jerarquía con **TPH** (igual que hoy: una sola tabla `Usuarios`), por lo que físicamente son columnas **nullable** en `Usuarios`, pobladas solo en filas de tipo Empleado. La migración solo agrega columnas nullable + la tabla hija, compatible con la auto-migración de `Program.cs`.

Campos en `Empleado`:
- `MfaSecret` (string?, **cifrado en reposo**) — secreto TOTP base32 protegido.
- `MfaHabilitado` (bool, default false).
- `MfaIntentosFallidos` (int, default 0) y `MfaBloqueadoHasta` (DateTime?, UTC).

Tabla hija `CodigoRecuperacionMfa`: `Id`, `EmpleadoId` (FK), `CodigoHash`, `Usado` (bool), `FechaUso` (DateTime?).

**Métodos de dominio en `Empleado`** (setters privados, mutación vía métodos, como el resto de la jerarquía): `ActivarMfa(secretoProtegido, hashesRecuperacion)`, `RegistrarIntentoFallidoMfa()` (incrementa y, al llegar a 5, setea `MfaBloqueadoHasta = now+15m`), `EstaBloqueadoMfa(now)`, `RegistrarVerificacionExitosaMfa()` (resetea contador/bloqueo), `ResetearMfa()` (limpia secreto, `MfaHabilitado=false`, contador, bloqueo; los códigos se borran vía repositorio). El consumo de códigos de recuperación se hace en la capa de aplicación contra la tabla hija.

### Seguridad
- **Cifrado del secreto en reposo:** AES-256-GCM con `System.Security.Cryptography.AesGcm` nativo. **Nonce aleatorio de 96 bits por cada cifrado** (nunca reusado bajo la misma clave), tag de 128 bits. Formato persistido: `base64(nonce ‖ ciphertext ‖ tag)`. Interfaz `IMfaSecretProtector` (Application) con `Protect(plaintext)` / `Unprotect(blob)`, impl en Infrastructure. La clave (`Mfa:EncryptionKey`, AES-256 base64) viene de configuración (secret del Container App, mismo patrón que el SMTP; sobrevive reinicios/redeploys). El secreto nunca se guarda ni loguea en claro.
- **Códigos de recuperación:** 10 por empleado, cada uno con ~50 bits de entropía (p.ej. 10 chars base32), hasheados (BCrypt o SHA-256+sal), de un solo uso.
- **Anti-fuerza bruta:** tras **5 códigos fallidos** consecutivos (TOTP o recuperación), el paso MFA se bloquea **15 min** (`MfaBloqueadoHasta`, persistido en DB). Un código válido resetea el contador. Esto **introduce el patrón de bloqueo que E1/CA-23 también van a requerir** para el password (hoy el login con password NO tiene lockout; queda como deuda separada, no en alcance de esta spec).
- **Reset de MFA por admin:** `POST /api/empleados/{id}/mfa/reset`, protegido con `[RequierePermiso(Modulo.Empleados, Operacion.Modificacion)]`. Des-enrola a otro empleado: borra secreto, `MfaHabilitado=false`, **limpia contador e info de bloqueo**, elimina sus códigos de recuperación; lo fuerza a reconfigurar en el próximo login. Auditado. Un admin **no** puede resetear su propio MFA por este endpoint: se compara `{id}` contra el `userId` (claim `NameIdentifier`) del JWT autenticado y se rechaza si coinciden.
- **Recuperación de último recurso (admin sin dispositivo ni códigos):** los códigos de recuperación son la mitigación primaria. Para pérdida total se documenta en `SETUP-CICD.md` un procedimiento de operador (reset directo de `MfaHabilitado=false` del admin vía acceso a la DB / re-seed controlado). Se **recomienda mantener ≥2 cuentas con permiso de gestión de empleados** para que puedan resetearse mutuamente y evitar el lockout irrecuperable.
- **Auditoría** (`IAuditLogger`): se agregan valores al enum `TipoAccionAuditoria` (append, seguro para persistencia int): `MfaActivado`, `MfaVerificado`, `MfaCodigoRecuperacionUsado`, `MfaBloqueado`, `MfaReseteadoPorAdmin`.

## Endpoints

| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | anónimo | (modificado) Empleado → 200 con `LoginResultado` (desafío MFA), sin JWT de sesión |
| POST | `/api/auth/mfa/setup` | `mfaToken` (setup) | Genera secreto + QR (data URI) + clave + códigos de recuperación; no activa |
| POST | `/api/auth/mfa/activate` | `mfaToken` (setup) | Valida primer código, persiste cifrado, activa MFA, emite JWT de sesión |
| POST | `/api/auth/mfa/verify` | `mfaToken` (pending) | Valida TOTP en login, emite JWT de sesión |
| POST | `/api/auth/mfa/recovery` | `mfaToken` (pending) | Valida y consume un código de recuperación, emite JWT |
| POST | `/api/empleados/{id}/mfa/reset` | `[RequierePermiso(Empleados, Modificacion)]` | (admin) Des-enrola el MFA de un empleado |

### DTOs de respuesta
- **Paso 1 (`/login`):** se introduce `LoginResultado`:
  - empleado → `{ requiereMfa: true, setupRequerido: bool, mfaToken: string }` (sin `sesion`).
  - socio/legacy → `{ requiereMfa: false, sesion: LoginResponse }` (el `LoginResponse` actual: Token, Nombre, Apellido, Correo, RolNombre, Permisos, UnidadIds).
- **Pasos 2 (`/mfa/activate`, `/mfa/verify`, `/mfa/recovery`):** devuelven el **`LoginResponse` actual** tal cual. El JWT de sesión se arma con `GenerateJwt` (mismos claims que el login normal: `userId/correo/rolId/rolNombre/nombre/apellido`, 8h), garantizando paridad total con el login email+password y con el de Google.
- El frontend ramifica según `requiereMfa`/`setupRequerido`.
- **Cambio breaking del contrato de `/login`:** hoy `/login` devuelve un `LoginResponse` plano. Pasa a devolver `LoginResultado` (envoltorio) para **todos** los casos, incluido el login de socio/legacy por contraseña (que queda anidado en `sesion`). Cualquier consumidor del `/login` actual (frontend, colección Postman) debe adaptarse. Se documenta como cambio de versión del endpoint.

## Caso de uso (CU-05 — flujo principal, empleado)

1. El empleado ingresa correo y contraseña.
2. El sistema valida credenciales; detecta que es empleado (requiere MFA).
3. Si no tiene MFA: lo guía al enrolment (QR + clave + códigos). Si ya tiene: pide el código de 6 dígitos.
4. El empleado ingresa el código TOTP de su app.
5. El sistema valida (ventana 30s, ±1 step) y emite el JWT firmado.
6. Redirige según rol.

**Excepciones:**
- **E2 — Código inválido/expirado:** "Código incorrecto o expirado." No se completa el login; suma un intento fallido.
- **Bloqueo:** tras 5 intentos fallidos, "Demasiados intentos. Probá de nuevo en unos minutos." durante 15 min.

## Criterios de aceptación

- CA-20: un empleado no obtiene JWT de sesión hasta validar el segundo factor.
- Un empleado sin MFA configurado es forzado al enrolment antes de poder operar (RN-28).
- Un código TOTP válido dentro de la ventana (±1 step) completa el login; uno inválido/expirado lo rechaza (E2) y suma intento.
- Tras 5 intentos fallidos (TOTP o recuperación) el paso MFA se bloquea 15 min; un código de recuperación también suma al contador.
- Un código de recuperación válido permite entrar y queda inutilizado para siguientes usos.
- **Un `mfaToken` presentado como Bearer contra un endpoint con `[RequierePermiso]` (p.ej. `GET /api/socios`) o contra `/auth/me` devuelve 401** (firmado con clave separada; el pipeline global lo rechaza).
- Un `mfaToken` de `purpose=mfa-setup` no sirve para `/mfa/verify` ni viceversa.
- El secreto TOTP se guarda cifrado (AES-256-GCM, nonce único); nunca aparece en claro en DB, logs ni respuestas tras el alta.
- El JWT de sesión emitido tras el MFA tiene **los mismos claims** que el del login email+password.
- El admin puede resetear el MFA de otro empleado (limpiando también contador y bloqueo); queda auditado.
- Un admin que intenta resetear su **propio** MFA (`{id}` == su `userId`) recibe un error y no se ejecuta.
- El login de socios con Google (spec hermana) no se ve afectado.

## Frontend

- **Login:** tras la contraseña, según `LoginResultado`: si `setupRequerido` → pantalla de enrolment; si `requiereMfa` sin setup → pantalla de verificación; si `requiereMfa=false` → sesión directa (socio/legacy).
- **Enrolment:** muestra el QR (data URI), la clave manual (copiable), input para confirmar el primer código, y los códigos de recuperación (copiar/descargar, con aviso de guardarlos).
- **Verificación:** input de 6 dígitos + link "usar un código de recuperación".
- **Admin / empleados:** acción "Resetear MFA" en la ficha de un empleado.
- Reusa el manejo de errores y la sesión (token + AuthContext) del login actual.

## Configuración / deploy

- Nuevos secrets: `Mfa:EncryptionKey` (clave AES-256 base64) y `Mfa:TokenSigningKey` (clave de firma del `mfaToken`, distinta de `Jwt:Key`). En dev local en `appsettings.Development.json` / user-secrets; en producción como secrets del Container App vía workflow (mismo patrón que `configure-email.yml`). Documentar en `docs/deploy/SETUP-CICD.md`, junto con el procedimiento de reset de emergencia del admin.
- Migración EF para los campos de `Empleado` (columnas nullable en `Usuarios`, TPH) y la tabla `CodigoRecuperacionMfa` (se auto-aplica en el arranque).

## Fuera de alcance

- OTP por SMS o email (solo TOTP por app).
- WebAuthn / passkeys.
- "Recordar este dispositivo" (saltear MFA por N días) — posible mejora futura.
- Lockout del login con password (E1/CA-23) — deuda separada; esta spec solo cubre el lockout del paso MFA.
- MFA para socios (usan Google OAuth).
- Rol "Dueño" (otra entrega de IT5).
