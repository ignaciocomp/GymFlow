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
