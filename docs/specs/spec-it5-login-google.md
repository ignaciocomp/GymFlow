---
tags:
  - spec
  - iteracion
requerimiento: RNF-01, RNF-10, RN-19, CU-05
---

# Login de Socios con Google (OAuth 2.0) - Spec

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] - RNF-01 (parte socios), RNF-10, RN-19, CU-05 (flujo alternativo), CA-21, E3
**Plan de implementacion:** [[plan-it5-login-google]]
**Ultima actualizacion:** 2026-06-12

## Resumen

Los socios inician sesión con su cuenta de Google ("Iniciar sesión con Google"), sin gestionar contraseña propia (RN-19). Los empleados siguen con email + password. El MFA TOTP de admin/profesor es otra spec de IT5 (pendiente).

## Decisiones de diseño

- **Flujo: ID token con Google Identity Services (GIS).** El frontend muestra el botón oficial de Google; al autenticarse, GIS devuelve un *ID token* (JWT firmado por Google) directo en el navegador, sin redirects server-side. El frontend lo manda a `POST /api/auth/google` y el backend lo valida. No se usa Authorization Code flow ni client secret: solo se necesita *autenticar*, no acceder a APIs de Google del usuario.
- **Validación en backend** con el paquete NuGet `Google.Apis.Auth` (`GoogleJsonWebSignature.ValidateAsync` con audience = nuestro Client ID), detrás de una interfaz `IGoogleTokenValidator` (Application) implementada en Infrastructure, para poder testear el command con mocks.
- **Solo socios pre-registrados** (CU-05 paso 4): se busca un **socio activo** por el correo verificado del token. Si no existe → error E3 `"No encontramos una cuenta asociada a este correo."` (401). No se auto-crean cuentas. Empleados no pueden entrar por esta vía.
- **`GoogleUserId` (sub) en `Usuario`**, nullable: se guarda en el primer login con Google (vinculación). Anticipado en [[spec-rnf01-gestion-usuarios]]. La búsqueda sigue siendo por correo (fuente de verdad del CU-05); el campo queda para trazabilidad y futuros usos.
- **El endpoint emite el JWT propio de GymFlow** con los mismos claims que el login normal (reusa `GenerateJwt` del `AuthController`) + permisos por `IPermisoCache` + auditoría `InicioSesion` + `unidadIds`. Aguas abajo (roles, guards, portal) nada cambia.
- **Client ID** (público): `1046140116343-hnkj4557mu5sn51cal1jnej0cacrk5kb.apps.googleusercontent.com`. Backend: `Google:ClientId` en appsettings.json. Frontend: `VITE_GOOGLE_CLIENT_ID` en `.env` commiteado (es público, no es secreto).
- **Consent screen en modo Prueba**: solo los test users cargados en Google Cloud pueden loguearse hasta publicar la app. Orígenes autorizados: `http://localhost:5173` y la URL de Azure.

## Criterios de aceptación

- CA-21: socio activo con correo registrado inicia sesión con Google y llega a su portal.
- E3: correo de Google sin socio activo asociado → `"No encontramos una cuenta asociada a este correo."`, login no se completa.
- ID token inválido/expirado/audience incorrecto → 401 sin revelar detalle.
- Primer login con Google guarda `GoogleUserId`; logins siguientes no lo duplican.
- El login con email+password de empleados sigue intacto.

## Fuera de alcance

- MFA TOTP (admin/profesor) — spec aparte de IT5.
- Rol Dueño.
- Desvincular/cambiar cuenta Google de un socio.
