# Setup del pipeline de auto-deploy (pasos manuales)

> Estos pasos los ejecuta quien tiene las credenciales Azure (Seba). Una sola vez.
> Despues de esto, cada push a `main` deploya automaticamente.

## 1. Crear el Service Principal

```bash
export SUB_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac --name "sp-gymflow-cicd" \
  --role contributor \
  --scopes /subscriptions/$SUB_ID/resourceGroups/rg-gymflow \
  --sdk-auth > azure-credentials.json

cat azure-credentials.json
```

Devuelve un JSON con `clientId`, `clientSecret`, `subscriptionId`, `tenantId`.

ADVERTENCIA: `azure-credentials.json` contiene credenciales. NUNCA commitearlo (ya esta en `.gitignore`). Borrarlo despues del paso 2.

## 2. Cargar secrets y variables en GitHub

En **Settings -> Secrets and variables -> Actions** del repo:

### Secret (New repository secret):
| Nombre | Valor |
|--------|-------|
| `AZURE_CREDENTIALS` | El JSON COMPLETO de `azure-credentials.json` |

### Variables (pestana Variables -> New variable):
| Nombre | Valor |
|--------|-------|
| `AZURE_RG` | `rg-gymflow` |
| `AZURE_ACR_NAME` | `acrgymflow42543` |
| `AZURE_ACR_LOGIN_SERVER` | `acrgymflow42543.azurecr.io` |
| `AZURE_ACA_APP` | `ca-gymflow` |

## 3. Borrar el JSON local

```bash
rm azure-credentials.json
```

## 4. Probar el pipeline

- Opcion A: hacer un push chico a `main`.
- Opcion B: **Actions -> Deploy to Azure Container Apps -> Run workflow** (gracias a `workflow_dispatch`).

Verificar en Actions que el run termine verde: Tests -> Build image -> Update Container App -> Smoke test 200 OK.

## Rollback

Si un deploy rompe la app:
```bash
az containerapp revision list --name ca-gymflow --resource-group rg-gymflow -o table
az containerapp revision activate --name ca-gymflow --resource-group rg-gymflow --revision <REVISION_ANTERIOR>
```

## Activar emails reales (SMTP)

> One-time: los env vars del Container App persisten entre deploys de imagen.
> Ninguna credencial toca el repo: la password vive como secret del Container App
> y el env var `Email__SmtpPassword` la referencia via `secretref`.

### 1. Crear una App Password de Gmail

- La cuenta Gmail tiene que tener verificacion en 2 pasos (2FA) activada.
- Ir a https://myaccount.google.com/apppasswords y crear una App Password (por ejemplo con nombre "GymFlow").
- Google muestra 16 caracteres: **copiarlos SIN espacios** (Google los muestra en grupos de 4, pero la password real no lleva espacios).

### 2. Cargar secrets en GitHub

En **Settings -> Secrets and variables -> Actions -> New repository secret**:

| Nombre | Valor |
|--------|-------|
| `SMTP_USER` | La cuenta Gmail completa (ej: `gymflow.notificaciones@gmail.com`) |
| `SMTP_PASSWORD` | Los 16 caracteres de la App Password, sin espacios |

### 3. Correr el workflow

**Actions -> Configurar email (SMTP) -> Run workflow** (solo se dispara manual).

El workflow valida que los secrets existan, guarda la password como secret `smtp-password` del Container App y setea `Email__Habilitado=true`, `Email__SmtpUser`, `Email__From` y `Email__SmtpPassword=secretref:smtp-password`.

### Notas

- Gmail fuerza el `From` a la cuenta autenticada: aunque se configure otro `Email__From`, los mails salen desde la cuenta del `SMTP_USER`.
- En dev local los mails siguen simulados (`Email:Habilitado=false` en `appsettings.json`) y eso es **intencional**: solo produccion envia mails reales.
- Si cambia la App Password, alcanza con actualizar el secret `SMTP_PASSWORD` en GitHub y volver a correr el workflow.

## Activar MFA (segundo factor TOTP para empleados)

> One-time: como con SMTP, los env vars del Container App persisten entre deploys de imagen.
> Ninguna clave toca el repo: ambas viven como secrets del Container App y los env vars
> `Mfa__EncryptionKey` / `Mfa__TokenSigningKey` las referencian via `secretref`.
> En dev local las claves de `appsettings.json` (`Mfa:EncryptionKey`, `Mfa:TokenSigningKey`)
> alcanzan; en produccion se sobreescriben con estos secrets.

### Por que dos claves distintas

- **`Mfa:EncryptionKey`** (base64 de 32 bytes / 256 bits): cifra en reposo el secreto TOTP de
  cada empleado con AES-256-GCM. Si se pierde/rota, los secretos guardados quedan ilegibles y
  todos los empleados tienen que volver a enrolarse (reset + re-setup).
- **`Mfa:TokenSigningKey`** (string UTF-8 de >=32 caracteres): firma el `mfaToken` intermedio del
  login en dos pasos. **Tiene que ser distinta de `Jwt:Key`**: asi el pipeline JWT global rechaza
  el `mfaToken` en los endpoints normales (solo sirve para `/auth/mfa/*`). NO reutilizar `Jwt:Key`.

### 1. Generar las dos claves

```bash
# EncryptionKey: 32 bytes aleatorios en base64 (AES-256)
openssl rand -base64 32

# TokenSigningKey: >=32 caracteres aleatorios (string UTF-8). Cualquiera de las dos sirve:
openssl rand -base64 48          # base64 -> de sobra >=32 chars
# o, si openssl no esta a mano:
# head -c 36 /dev/urandom | base64
```

Guardar ambos valores en un gestor de secrets (no en el repo, no en chats). La `TokenSigningKey`
tiene que ser **distinta** de la `Jwt:Key` de produccion.

### 2. Cargar las claves como secrets del Container App

Con sesion `az` iniciada (el mismo Service Principal del deploy sirve):

```bash
RG=rg-gymflow
APP=ca-gymflow

# Guardar las dos claves como secrets del Container App (no se imprimen: --output none)
az containerapp secret set \
  --name "$APP" --resource-group "$RG" \
  --secrets mfa-encryption-key="<EncryptionKey base64>" \
            mfa-token-signing-key="<TokenSigningKey>" \
  --output none

# Referenciar los secrets via secretref en los env vars (doble guion bajo = ':' en .NET)
az containerapp update \
  --name "$APP" --resource-group "$RG" \
  --set-env-vars \
    "Mfa__EncryptionKey=secretref:mfa-encryption-key" \
    "Mfa__TokenSigningKey=secretref:mfa-token-signing-key" \
  --output none
```

Esto dispara una nueva revision. Verificar que arranque sana (`az containerapp revision list ... -o table`)
y probar un login de empleado: tiene que pedir el segundo factor.

### 2-bis. (Recomendado) Hacerlo con el workflow `configure-mfa.yml` — sin `az` local

Igual que el SMTP, hay un workflow manual que hace los dos pasos de arriba sin que ninguna clave toque el repo ni tu maquina:

1. Generar las dos claves (paso 1 de arriba; en Windows sin `openssl`, en PowerShell: `[Convert]::ToBase64String((1..32 | % {Get-Random -Max 256}))` para la EncryptionKey, y `[Convert]::ToBase64String((1..36 | % {Get-Random -Max 256}))` para la TokenSigningKey).
2. Cargar en GitHub (Settings → Secrets and variables → Actions → New repository secret) los secrets **`MFA_ENCRYPTION_KEY`** y **`MFA_TOKEN_SIGNING_KEY`**.
3. Con `configure-mfa.yml` ya en `main` (entra con el deploy), correrlo: pestaña **Actions → "Configurar MFA (claves)" → Run workflow**.

El workflow guarda las claves como secrets del Container App (`mfa-encryption-key`, `mfa-token-signing-key`) y setea los env vars `Mfa__*` por `secretref`. Persiste entre deploys: una sola vez.

> **Orden vs. deploy:** correr esto **antes** del primer deploy con MFA (via `az`, paso 2) o **apenas termine** (via el workflow). Sin las claves, el `/login` de un empleado devuelve 500. Como los secrets/env-vars del Container App ya existen aunque la imagen vieja no los use, lo mas seguro es cargarlos por `az` (paso 2) **antes** del deploy.

### Notas

- Si se **rota** la `TokenSigningKey`: los `mfaToken` en vuelo se invalidan (login a medias falla),
  pero los empleados ya enrolados siguen funcionando (solo tienen que rehacer el login). Sin downtime real.
- Si se **rota o pierde** la `EncryptionKey`: los secretos TOTP guardados quedan ilegibles. Hay que
  resetear el MFA de cada empleado (ver abajo) y que vuelvan a enrolarse. No rotarla a la ligera.
- El secreto TOTP nunca viaja en claro a la DB ni a los logs: solo el blob AES-GCM. El QR y la clave
  manual se muestran una sola vez en el enrolment.

## Reset de emergencia del MFA (admin sin acceso)

El reset normal de MFA de un empleado lo hace otro usuario con permiso de **gestion de empleados**
(`POST /api/empleados/{id}/mfa/reset`, requiere `Empleados:Modificacion`), y **nadie puede resetearse
a si mismo**. El problema clasico: el **unico** admin pierde su segundo factor (telefono perdido,
authenticator borrado, sin codigos de recuperacion) y no queda nadie mas que pueda resetearlo.

### Recomendacion (evita llegar a esto)

- Mantener **>=2 cuentas con permiso de gestion de empleados** (rol con `Empleados:Modificacion`),
  asi una puede resetear el MFA de la otra sin tocar la base de datos.
- Guardar los **10 codigos de recuperacion** que se muestran al activar el MFA: cada uno entra una
  sola vez por `/auth/mfa/recovery` y sirve justamente para cuando el authenticator no esta a mano.

### Reset directo en la base de datos (ultimo recurso)

Si de verdad no queda ninguna cuenta que pueda resetear, se desactiva el MFA del empleado a mano.
El MFA vive en la tabla TPH **`Usuarios`** (columnas `MfaHabilitado`, `MfaSecret`,
`MfaIntentosFallidos`, `MfaBloqueadoHasta`) y los codigos en **`CodigosRecuperacionMfa`** (FK `EmpleadoId`).

```bash
# 1. Conectarse a la base de produccion (psql, con la connection string del Container App)

# 2. Buscar el Id del empleado por su correo
#    SELECT "Id", "Correo", "MfaHabilitado" FROM "Usuarios" WHERE "Correo" = 'admin@gymflow.com';

# 3. Desactivar el MFA y limpiar el estado (reemplazar <ID> por el Id del paso 2)
#    UPDATE "Usuarios"
#       SET "MfaHabilitado" = false,
#           "MfaSecret" = NULL,
#           "MfaIntentosFallidos" = 0,
#           "MfaBloqueadoHasta" = NULL
#     WHERE "Id" = '<ID>';

# 4. Eliminar los codigos de recuperacion viejos del empleado
#    DELETE FROM "CodigosRecuperacionMfa" WHERE "EmpleadoId" = '<ID>';
```

Tras esto, el empleado entra solo con usuario y password, y el proximo login le va a pedir
**volver a enrolarse** (`SetupRequerido=true`), generando un secreto nuevo. Equivale exactamente al
reset por API; cambiarlo a mano en la DB es solo el plan B cuando no hay otra cuenta que lo haga.
