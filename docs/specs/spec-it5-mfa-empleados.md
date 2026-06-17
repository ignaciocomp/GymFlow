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
**Última actualización:** 2026-06-16

## Resumen

Segundo factor de autenticación obligatorio para **empleados** (usuarios no-Socio: admin, profesor y cualquier rol interno), basado en **TOTP** (RFC 6238) con apps autenticadoras (Google/Microsoft Authenticator). Los socios no usan MFA (se autentican con Google, ver spec hermana). El login pasa de un solo paso a dos: contraseña → código TOTP.

## Decisiones de diseño

### Alcance
- MFA **obligatorio para todos los empleados** (todo `Usuario` que no sea Socio). Reconcilia RN-18 (admin/profesor) con RN-28 (todo empleado) hacia la opción más segura.
- Los **socios nunca pasan por MFA**; su login es Google OAuth.

### Estándar TOTP
- RFC 6238: **6 dígitos, período 30s, HMAC-SHA1** (defaults de los authenticators).
- Tolerancia de **±1 step** (±30s) por desfasaje de reloj al validar.
- Librerías: **Otp.NET** (generación/validación TOTP) y **QRCoder** (QR del URI `otpauth://`). El secreto se genera con un RNG criptográfico (160 bits, base32).

### Flujo de login en dos pasos (token intermedio)
El endpoint `POST /api/auth/login` deja de devolver el JWT de sesión directamente cuando el usuario es empleado:

1. Valida correo + contraseña (BCrypt) como hoy.
2. Si es **empleado**, responde **200** con un **`mfaToken`**: un JWT de vida corta firmado, con un claim de propósito restringido (`purpose=mfa-pending` o `mfa-setup`), expiración ~5 min, que **solo sirve para los endpoints de MFA** (no autoriza la API). Indica `mfaRequired` o `mfaSetupRequired` según si el empleado ya tiene MFA activado.
3. Si es **socio** (login con password legacy, si existiera) o el flujo de Google: sin cambios.

El `mfaToken` evita guardar estado de sesión server-side (la API sigue stateless). El JWT de sesión completo **solo** se emite tras validar el segundo factor.

### Enrolment (RN-28 — configurar antes de operar)
Si el empleado no tiene MFA activado, tras la contraseña entra al alta de MFA:

- `POST /api/auth/mfa/setup` (con `mfaToken` de setup): genera un secreto TOTP (aún no activado), devuelve el URI `otpauth://`, el **QR** (PNG/base64), la **clave manual** (base32) y **10 códigos de recuperación** de un solo uso (se muestran **una sola vez**).
- `POST /api/auth/mfa/activate` (`mfaToken` + código de la app): valida el código contra el secreto; si es correcto, **persiste el secreto cifrado + los códigos de recuperación hasheados**, marca `MfaHabilitado=true` y **emite el JWT de sesión** (login completo).
- Hasta activarlo, el empleado no obtiene JWT de sesión ni puede operar.
- El admin semilla (`admin@gymflow.com`) pasa por este enrolment en su primer login tras el release.

### Verificación en logins siguientes
- `POST /api/auth/mfa/verify` (`mfaToken` de pending + código de 6 dígitos): valida el TOTP → emite el JWT de sesión.
- `POST /api/auth/mfa/recovery` (`mfaToken` + código de recuperación): valida y **consume** (marca usado) un código de recuperación → emite el JWT. Para cuando se pierde el dispositivo.

### Modelo de datos
Campos en `Usuario` (nullable; los socios no los usan, mismo criterio que `GoogleUserId`):
- `MfaSecret` (string?, **cifrado en reposo**) — secreto TOTP base32.
- `MfaHabilitado` (bool, default false).
- `MfaIntentosFallidos` (int, default 0) y `MfaBloqueadoHasta` (DateTime?, UTC) — para el anti-fuerza bruta.

Tabla hija `CodigoRecuperacionMfa`: `Id`, `UsuarioId` (FK), `CodigoHash` (hash del código), `Usado` (bool), `FechaUso` (DateTime?).

### Seguridad
- **Cifrado del secreto en reposo:** AES-256-GCM con una clave de configuración (`Mfa:EncryptionKey`), provista como **secret del Container App** (mismo patrón que el SMTP, sobrevive reinicios/redeploys). Interfaz `IMfaSecretProtector` (Application) con impl en Infrastructure; el secreto nunca se guarda ni loguea en claro.
- **Códigos de recuperación** hasheados (no en claro), de un solo uso.
- **Anti-fuerza bruta:** tras **5 códigos fallidos** consecutivos, el paso MFA se bloquea **15 min** (`MfaBloqueadoHasta`); alinea con E1/CA-23. Un código válido resetea el contador.
- **Reset de MFA por admin:** `POST /api/empleados/{id}/mfa/reset` (permiso de gestión de empleados) des-enrola a otro empleado (borra secreto, `MfaHabilitado=false`, limpia códigos) y lo fuerza a reconfigurar. Auditado. Un admin no puede resetear su propio MFA (se mitiga con los códigos de recuperación).
- **Auditoría** (`IAuditLogger`): alta de MFA, verificación exitosa, uso de código de recuperación, bloqueo por intentos, y reset por admin.
- El `mfaToken` intermedio se rechaza en los endpoints normales de la API (claim `purpose` validado).

## Endpoints

| Método | Endpoint | Descripción |
|---|---|---|
| POST | `/api/auth/login` | (modificado) Empleado → 200 con `mfaToken` + `mfaRequired`/`mfaSetupRequired`, sin JWT de sesión |
| POST | `/api/auth/mfa/setup` | Genera secreto + QR + clave + códigos de recuperación (no activa aún) |
| POST | `/api/auth/mfa/activate` | Valida primer código, persiste, activa MFA y emite JWT de sesión |
| POST | `/api/auth/mfa/verify` | Valida TOTP en login y emite JWT de sesión |
| POST | `/api/auth/mfa/recovery` | Valida y consume un código de recuperación, emite JWT |
| POST | `/api/empleados/{id}/mfa/reset` | (admin) Des-enrola el MFA de un empleado |

## Caso de uso (CU-05 — flujo principal, empleado)

1. El empleado ingresa correo y contraseña.
2. El sistema valida credenciales; detecta que es empleado (rol requiere MFA).
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
- Tras 5 intentos fallidos el paso MFA se bloquea 15 min.
- Un código de recuperación válido permite entrar y queda inutilizado para siguientes usos.
- El `mfaToken` intermedio no autoriza ningún endpoint de la API fuera de los de MFA.
- El secreto TOTP se guarda cifrado; nunca aparece en claro en DB, logs ni respuestas tras el alta.
- El admin puede resetear el MFA de otro empleado; queda auditado.
- El login de socios con Google (spec hermana) no se ve afectado.

## Frontend

- **Login:** tras la contraseña, si la respuesta trae `mfaSetupRequired` → pantalla de enrolment; si `mfaRequired` → pantalla de verificación.
- **Enrolment:** muestra el QR, la clave manual (copiable), input para confirmar el primer código, y los códigos de recuperación (copiar/descargar, con aviso de guardarlos).
- **Verificación:** input de 6 dígitos + link "usar un código de recuperación".
- **Admin / empleados:** acción "Resetear MFA" en la ficha de un empleado.
- Reusa el manejo de errores y la sesión (token + AuthContext) del login actual.

## Configuración / deploy

- Nuevo secret `Mfa:EncryptionKey` (clave AES-256 base64). En dev local va en `appsettings.Development.json` o user-secrets; en producción se carga como secret del Container App vía workflow (mismo patrón que `configure-email.yml`). Documentar en `docs/deploy/SETUP-CICD.md`.
- Migración EF para los campos de `Usuario` y la tabla `CodigoRecuperacionMfa` (se auto-aplica en el arranque).

## Fuera de alcance

- OTP por SMS o email (solo TOTP por app).
- WebAuthn / passkeys.
- "Recordar este dispositivo" (saltear MFA por N días) — posible mejora futura.
- MFA para socios (usan Google OAuth).
- Rol "Dueño" (otra entrega de IT5).
