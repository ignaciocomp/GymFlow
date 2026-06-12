---
tags:
  - plan
  - iteracion
spec: spec-it5-login-google
---

# Plan de Implementación: Login de Socios con Google

**Spec:** [[spec-it5-login-google]]
**Rama:** `feature/it5-login-google` (base: develop)
**Metodología:** TDD estricto por tarea, 1 subagente por tarea, commit por tarea.

## Task 1 — Domain + Infrastructure: `GoogleUserId` en Usuario

- [ ] `Usuario.GoogleUserId` (string?, private set) + método `VincularGoogle(string googleUserId)` que lo setea solo si está vacío.
- [ ] Test de dominio: vincular setea el valor; re-vincular con otro valor no lo pisa.
- [ ] EF configuration (longitud razonable, ej. 64) + migración `AgregarGoogleUserIdAUsuario` (verificar `dotnet ef` disponible; startup project = GymFlow.API).
- [ ] Suite completa verde. Commit.

## Task 2 — Application: `LoginConGoogleCommand` + `IGoogleTokenValidator`

- [ ] Interfaz `IGoogleTokenValidator` en Application/Interfaces: `Task<GoogleTokenPayload?> ValidarAsync(string idToken)` con record `GoogleTokenPayload(string Sub, string Email, bool EmailVerificado)`. Devuelve null si el token es inválido.
- [ ] `LoginConGoogleCommand` (TDD, mocks de validator/repos): valida token → null = `UnauthorizedAccessException`; busca socio activo por email (`ISocioRepository.GetByCorreoAsync`) → no existe o inactivo = `UnauthorizedAccessException` con mensaje E3 exacto `"No encontramos una cuenta asociada a este correo."`; vincula `GoogleUserId` si está vacío (persiste); devuelve el socio.
- [ ] Tests: login ok, E3 socio inexistente, E3 socio inactivo, token inválido, primer login guarda GoogleUserId, segundo login no lo re-escribe.
- [ ] Suite completa verde. Commit.

## Task 3 — Infrastructure + API: validador real y endpoint

- [ ] NuGet `Google.Apis.Auth` en GymFlow.Infrastructure.
- [ ] `GoogleIdTokenValidator` (Infrastructure/Services): `GoogleJsonWebSignature.ValidateAsync` con `Audience = [Google:ClientId]`; excepción de validación → null. Registrar en DI.
- [ ] `Google:ClientId` en appsettings.json (valor público).
- [ ] `AuthController`: `POST /api/auth/google` (`[AllowAnonymous]` implícito como login) recibe `{ idToken }`, llama al command, arma `GenerateJwt` + permisos + auditoría `InicioSesion` + `LoginResponse` con `unidadIds` — mismo shape que el login de socio. `UnauthorizedAccessException` → 401 con `{ error }`.
- [ ] Suite completa verde. Commit.

## Task 4 — Frontend: botón de Google en el Login

- [ ] `.env` con `VITE_GOOGLE_CLIENT_ID` (público, commiteado).
- [ ] Carga del script GIS (`https://accounts.google.com/gsi/client`) y render del botón oficial en `Login.tsx` (divisor "o" debajo del form actual), callback recibe `credential` → `authApi.loginConGoogle(idToken)` → mismo manejo de sesión que el login normal (token + user en AuthContext/localStorage) → redirect según rol (socio → `/portal`).
- [ ] Mostrar el error del backend (E3) en el mismo lugar que los errores de login actuales.
- [ ] `npm run build` + `npx vitest run` verdes. Commit.

## Task 5 — Review final

- [ ] Reviewer adversarial: diff completo vs develop, seguridad (audience validada, no se loguea el idToken, E3 exacto, empleados no entran por Google), migración consistente, build backend+frontend y suites completas.
