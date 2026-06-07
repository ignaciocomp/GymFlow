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
