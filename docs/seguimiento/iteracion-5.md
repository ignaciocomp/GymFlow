---
title: DOCUMENTACION ITERACIÓN 5 FASE DE CONSTRUCCIÓN
tags:
  - seguimiento
related:
  - "[[seguimiento_index]]"
  - "[[spec-it5-login-google]]"
  - "[[plan-it5-login-google]]"
---

# DOCUMENTACION ITERACIÓN 5 FASE DE CONSTRUCCIÓN

**Iteración 5 --- Fase de Construcción (14/06/2026 -- 28/06/2026)**
**Prioridad:** DESEABLE/OPCIONAL

## Descripción general

La quinta iteración aborda la **autenticación avanzada** (RNF-01 parte socios, RNF-10). El primer entregable, ya completado, es el **login de socios con Google (OAuth 2.0)**: los socios inician sesión con su cuenta de Google sin gestionar contraseña propia (RN-19), reusando el JWT y el sistema de roles/permisos existente. Quedan pendientes en la iteración el **MFA TOTP** para admin/profesor y el **rol Dueño**.

El flujo elegido es **ID token con Google Identity Services (GIS)**: el frontend muestra el botón oficial de Google, que devuelve un ID token (JWT firmado por Google) en el navegador; el backend lo valida con `Google.Apis.Auth` (firma, expiración y *audience* contra el Client ID propio) y busca un socio activo por el correo verificado. No se usa client secret ni redirects server-side, porque solo se necesita autenticar (no acceder a APIs de Google del usuario).

## Tareas planificadas

Funcionalidades a implementar:

- **RNF-01 (parte socios) / RNF-10 (OAuth)** — Login de socios con Google OAuth 2.0, sin contraseña propia. ✅ **Completado**
- **RNF-10 (MFA)** — MFA TOTP (Google/Microsoft Authenticator) para Admin y Profesor. ⏳ Pendiente
- **RNF-01 (rol Dueño)** — Rol "Dueño" con filtro por unidades asignadas. ⏳ Pendiente

## ¿Qué se implementó?

**Login de socios con Google (CU-05 flujo alternativo):**

- **Dominio:** campo `Usuario.GoogleUserId` (nullable, máx. 64) con método `VincularGoogle(sub)` idempotente (no pisa un vínculo existente). Migración EF `AgregarGoogleUserIdAUsuario` (solo agrega la columna; se aplica sola en el arranque vía `db.Database.Migrate()`).
- **Application:** interfaz `IGoogleTokenValidator` + `LoginConGoogleCommand` con toda la lógica del CU-05: valida el token, exige email verificado, busca socio activo **con rol** por correo, vincula `GoogleUserId` en el primer login y devuelve el socio. Lógica cubierta por tests con mocks.
- **Infrastructure:** `GoogleIdTokenValidator` (paquete `Google.Apis.Auth`) que valida firma, expiración y audience contra `Google:ClientId`; un token inválido devuelve `null` sin loguear el token. Fallas inesperadas (config/red) se loguean por tipo, nunca el token.
- **API:** endpoint `POST /api/auth/google` que recibe `{ idToken }` y replica exactamente el armado de respuesta del login de socio (JWT propio, permisos por caché, auditoría `InicioSesion`, `unidadIds`). Errores → `401 { error }`.
- **Frontend:** botón oficial "Iniciar sesión con Google" (GIS) en el Login, con divisor "o" debajo del formulario, carga única del script y manejo de errores en el mismo lugar que el login normal. Client ID público vía `VITE_GOOGLE_CLIENT_ID`.

**Reglas de negocio aplicadas:**

- Solo socios **pre-registrados**: si el correo de Google no corresponde a un socio activo, se rechaza con E3 — *"No encontramos una cuenta asociada a este correo."* (mismo mensaje para inexistente/inactivo/sin rol, para no permitir enumeración de estados).
- Token inválido, expirado, de otra app (audience distinto) o con email no verificado → 401 genérico (fail-closed).
- Empleados no pueden ingresar por esta vía (el comando solo consulta el repositorio de socios). El login email + password queda intacto.

## Tareas pendientes

- **MFA TOTP** para admin y profesor (RNF-10, CU-05 flujo principal, CA-20): enrolamiento con app autenticadora, validación de código de 6 dígitos, RN-18/RN-28.
- **Rol Dueño** con filtro por unidades asignadas (RNF-01).
- Bloqueo tras 5 intentos fallidos (E1/CA-23) — quedó asociado a la entrega de MFA.
- Publicación del proyecto OAuth en Google Cloud (hoy en modo **Prueba**: solo los usuarios de prueba cargados pueden autenticarse).

## Configuración de Google Cloud

- Proyecto `gymflow-499222`, pantalla de consentimiento **Externa** en estado **Prueba**.
- Cliente OAuth "GymFlow Web" (Aplicación web). Orígenes autorizados de JavaScript: `http://localhost:5173` (dev) y la URL del Container App en Azure (prod). Sin URIs de redirección (el flujo GIS de ID token no las requiere).
- El **Client ID es público** y se versiona en el repo (`appsettings.json` → `Google:ClientId` y `frontend/.env` → `VITE_GOOGLE_CLIENT_ID`). No se usa client secret.
- Mientras la app esté en modo Prueba, solo los correos cargados como "usuarios de prueba" pueden iniciar sesión.

## Estructura de API --- endpoints implementados

| Método | Endpoint | Descripción | Auth |
|---|---|---|---|
| POST | `/api/auth/google` | Login de socio con ID token de Google; devuelve el mismo `LoginResponse` que `/api/auth/login` | Anónimo |

## Caso de uso extendido --- Iteración 5

### CU-05: Autenticación con Google (flujo alternativo — Socio)

**Flujo principal:**

1. El socio hace clic en "Iniciar sesión con Google" en el Login.
2. Google Identity Services autentica y devuelve un ID token al navegador.
3. El frontend envía el ID token a `POST /api/auth/google`.
4. El sistema valida el token (firma, expiración, audience) y que el correo verificado corresponda a un **socio activo con rol**.
5. Emite el JWT de GymFlow con rol Socio y redirige al portal.

**Excepciones:**

- **E3 — Correo no registrado / socio inactivo:** *"No encontramos una cuenta asociada a este correo."* El login no se completa.
- **Token inválido/expirado/audience incorrecto/email no verificado:** 401 genérico, sin revelar el detalle.

## Pruebas automatizadas

Cobertura agregada en esta entrega (suite backend en verde, 0 fallos):

- **Dominio** (`UsuarioTests`): `GoogleUserId` por defecto null; `VincularGoogle` setea el valor, es idempotente y no pisa un vínculo existente; argumento null/vacío lanza excepción.
- **Application** (`LoginConGoogleCommandTests`): login exitoso; E3 por socio inexistente, inactivo y sin rol; token inválido; email no verificado; primer login vincula y persiste; segundo login no re-vincula.
- **Infrastructure** (`GoogleIdTokenValidatorTests`): token malformado, vacío y JWT con firma inválida devuelven `null` sin propagar excepción.

## Pruebas funcionales de frontend

### Prueba 5.1 --- Login de socio con Google (correo registrado)

*(captura de pantalla)*

**Pasos:**

1. En el Login, hacer clic en "Iniciar sesión con Google".
2. Elegir una cuenta de Google cuyo correo esté registrado como socio activo (y cargado como usuario de prueba en Google Cloud).

**Resultado esperado:** El socio inicia sesión y es redirigido al portal, sin haber ingresado contraseña.

**Descripción:** Verifica el flujo feliz de OAuth 2.0 y la emisión del JWT propio reusando el sistema de roles existente.

### Prueba 5.2 --- Login con Google de correo no registrado

*(captura de pantalla)*

**Pasos:**

1. En el Login, hacer clic en "Iniciar sesión con Google".
2. Elegir una cuenta de Google cuyo correo **no** corresponda a un socio activo.

**Resultado esperado:** Se muestra el mensaje *"No encontramos una cuenta asociada a este correo."* y el login no se completa.

**Descripción:** Verifica el flujo de excepción E3 y que no se auto-crean cuentas.

### Prueba 5.3 --- El login con email y contraseña sigue intacto

*(captura de pantalla)*

**Pasos:**

1. Iniciar sesión como empleado con correo y contraseña.

**Resultado esperado:** El login tradicional funciona sin cambios; el botón de Google convive con el formulario.

**Descripción:** Verifica la no regresión del login email + password tras agregar la vía de Google.
