# CU-05: Autenticación y Control de Acceso

| *Campo* | |
|-|-|
| *Nombre* | Autenticación y Control de Acceso (Login + MFA + Google OAuth + Roles dinámicos) |
| *Actor principal* | Todos los actores (Socio / Empleado / Admin / Dueño) |
| *Precondición* | Usuario pre-registrado en el sistema con estado `Activo` y rol asignado. |
| *Postcondición* | Usuario autenticado con token de sesión propio de GymFlow. La aplicación filtra el acceso a recursos según el rol y los permisos del usuario. Login auditado. |
| *RF cubiertos* | RNF-01 (autenticación + autorización por roles y permisos), RNF-05 (seguridad de datos), RNF-10 (MFA + OAuth) |
| *Iteración(es) de entrega* | IT-1 — JWT base con usuarios hardcodeados (parcial, sin guards). IT-2 — Sistema de roles dinámicos con permisos por módulo (CRUD), `Empleado` con BCrypt, middleware de autorización. IT-5 — Login con Google OAuth (socios) + MFA TOTP obligatorio (empleados). |
| *Referencia original* | [GymFlow_Requerimientos_Completos.md § CU-05](../GymFlow_Requerimientos_Completos.md#cu-05--autenticación-y-control-de-acceso-login--mfa) |
| *Documentos de iteración* | [Documentacion_It.1.docx](../seguimiento/Documentacion_It.1.docx), [Documentacion_It.2.docx](../seguimiento/Documentacion_It.2.docx), [iteracion-5.md](../seguimiento/iteracion-5.md) |

**Flujo principal — Login de Socio con email + contraseña (IT-1 base):**

1. El socio ingresa correo y contraseña en `/login`.
2. El backend valida las credenciales contra la entidad correspondiente.
3. Si son correctas, emite un JWT con `Rol = Socio`, `Nombre` y `unidadIds` (sedes asignadas al socio) — duración 8 horas.
4. El frontend almacena el token y redirige al socio a `/portal` según su rol (separación `AdminLayout` / `SocioLayout`).

**Flujo alternativo — Login de Socio con Google OAuth 2.0 (IT-5):**

1. El socio hace clic en "Iniciar sesión con Google" en el Login.
2. El servicio oficial de Google autentica al socio y devuelve un token de identidad al navegador.
3. El frontend envía ese token al backend de GymFlow.
4. El backend valida el token usando la biblioteca oficial de Google (firma, expiración y que el token sea para esta aplicación) y verifica que el correo corresponda a un **socio activo con rol**.
5. En el primer login, el identificador de Google queda vinculado al socio (idempotente — no se pisa un vínculo existente).
6. El backend emite su propio token de sesión con rol `Socio` y el frontend redirige al portal.

**Flujo principal — Login de Empleado con MFA TOTP (IT-5, MFA ya activado):**

1. El empleado ingresa correo y contraseña en `/login`.
2. El backend valida la contraseña. Al ser empleado, en vez de emitir el token de sesión, devuelve un **ticket temporal** de corta duración (~5 minutos) que solo sirve para pasar por el segundo factor. El ticket se firma con una clave separada de la del token de sesión.
3. El frontend redirige a la pantalla de código de 6 dígitos.
4. El empleado abre la app autenticadora (Google/Microsoft Authenticator), lee el código y lo ingresa.
5. El backend valida el código (con tolerancia para desfasaje de reloj del celular) y, si es correcto, emite el token de sesión real.

**Flujo alternativo — Alta de MFA (primer login del empleado tras release o reset):**

1. Tras email + contraseña, el backend responde con un ticket temporal indicando que el empleado aún no configuró MFA.
2. El frontend muestra el **QR + clave manual** generados por el backend. El secreto se guarda cifrado en la base, marcado como "no activado".
3. El empleado escanea el QR con la app, ingresa el primer código generado y lo envía.
4. El backend valida el código, genera **10 códigos de recuperación de un solo uso** (los devuelve en pantalla una sola vez y los guarda hasheados), activa el MFA y emite el token de sesión.

**Flujo alternativo — Recuperación con códigos (sin acceso a la app):**

1. En la pantalla de verificación, el empleado elige "Usar código de recuperación" y envía uno de los 10 códigos entregados al activar MFA.
2. El backend valida el código, lo marca como usado (no se puede reutilizar) y emite el token de sesión.

**Flujo alternativo — Reset de emergencia por admin:**

1. El admin abre el detalle del empleado y ejecuta "Resetear MFA".
2. El backend borra la clave TOTP y los códigos de recuperación, y desactiva el MFA del empleado. La acción queda auditada.
3. En el próximo login el empleado pasa de nuevo por el alta de MFA.

**Comportamiento de autorización (IT-2):**

- Los empleados tienen roles dinámicos (Administrador, Profesor, Recepcionista, o roles personalizados creados por el admin).
- Cada rol tiene un set de **permisos por módulo y operación**: `Modulo.{Socios,Clases,Cuotas,Empleados,Eventos,Unidades,...}` × `Operacion.{Lectura,Escritura,Modificacion,Eliminacion}`.
- Los endpoints están protegidos con el atributo `[RequierePermiso(Modulo, Operacion)]` que valida contra los permisos del JWT.
- Los permisos se cachean para no consultar la base en cada request.

**Flujos de excepción:**

- **E1 — Código TOTP inválido:** Rechazo; no se emite token. Tras **5 intentos fallidos consecutivos**, el paso MFA del empleado queda bloqueado en la base (requiere reset del admin).
- **E2 — Ticket temporal expirado, con propósito incorrecto o firma inválida:** Rechazo. Si alguien intenta usar el ticket contra otra parte de la API, es rechazado automáticamente.
- **E3 — Login con Google: correo no registrado / socio inactivo / sin rol:** *"No encontramos una cuenta asociada a este correo."* Mismo mensaje para los 3 casos (no se revela el estado de la cuenta).
- **E4 — Token de Google inválido, expirado, emitido para otra aplicación, o correo no verificado:** Rechazo con mensaje genérico.
- **E5 — Código de recuperación ya usado:** Rechazo.
- **E6 — Empleado sin permisos suficientes:** Respuesta `403 Forbidden` del middleware de autorización.
- **E7 — Request sin token o token inválido:** Respuesta `401 Unauthorized`.

**Reglas de negocio aplicables:**

- **Contraseñas con BCrypt** (`IPasswordHasher`, IT-1 hashing real implementado en IT-2 para empleados).
- **MFA obligatorio para todos los empleados** (incluido Admin y Dueño) desde IT-5.
- **Socios no usan MFA**; usan email/contraseña o Google OAuth.
- **Ticket temporal de MFA firmado con clave dedicada**, separada de la del token de sesión, para evitar reutilización en otras partes de la API.
- **Códigos de recuperación de un solo uso**, hasheados en la base.
- **Bloqueo tras 5 intentos fallidos** del segundo factor; requiere reset manual del admin.
- **No se revela información sobre cuentas inexistentes** (mismo mensaje genérico para "no existe", "inactivo" y "sin rol").
- **Vinculación de Google ID idempotente:** el primer login asocia el ID de Google al socio; logins posteriores no re-vinculan.

**Desviaciones respecto del diseño original:**

- **Roles dinámicos en lugar de roles fijos:** el diseño original tenía 3 roles fijos (Administrador / Profesor / Socio). En IT-1 (cierre de iteración) se rediseñó a un sistema de permisos flexible por módulo × operación, completado en IT-2. El admin puede crear roles personalizados (ej. "Recepcionista", "Supervisor").
- **Rol Dueño agregado en IT-5 (RNF-01):** entre Admin y los roles dinámicos. Tiene permisos operativos sobre socios, planes, clases, cuotas, empleados, eventos y vista de sedes (no accede a Auditoría). Solo el Admin puede crear Dueños. El sistema filtra automáticamente todo lo que el Dueño ve por las sedes que tiene asignadas (ver [CU-07](CU-07-empleados-roles.md)).
- **MFA TOTP en lugar de SMS/email:** se eligió TOTP por app autenticadora (más seguro y sin dependencia de proveedor externo).
