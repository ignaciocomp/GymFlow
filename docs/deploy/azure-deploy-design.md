# Deploy de GymFlow en Azure Container Apps

#deploy 

**Fecha:** 2026-05-21
**Tipo:** Spec ejecutable + documentación oficial del deploy
**Audiencia:** Compañero del equipo con credenciales de Azure (encargado de ejecutar el deploy) + documentación de referencia para el resto del equipo y el profesor.

Ver cheatsheet de deploy: [[gymflow-azure-cheatsheet]]

---

## 1. Resumen

Deploy de GymFlow (backend .NET 8 + frontend React/Vite) en **Azure Container Apps** usando una **única imagen Docker** que sirve la API y los archivos estáticos del SPA. Base de datos en **Azure Database for PostgreSQL Flexible Server**. Toda la infraestructura se crea con **Azure CLI**.

**Re-deploys automáticos vía GitHub Actions** en cada push a `main` (build → push imagen → update Container App → smoke test). Setup completo de CI/CD documentado en §13.

### Decisiones de diseño

| Decisión | Elegido | Por qué |
|----------|---------|---------|
| Arquitectura | Single container (front + back en una imagen) | Sin CORS, deploy atómico, 1 sola URL |
| Plan Azure | Azure for Students ($100/año) | Sin tarjeta de crédito, renovable |
| Escalado | `minReplicas=0`, `maxReplicas=1` | Solo paga cuando hay tráfico (~$0.50/mes) |
| Base de datos | PostgreSQL Flexible Server B1ms | Datos persistentes, profesional, ~$13/mes |
| Región | East US | Catálogo más completo, latencia ~130ms desde UY (aceptable) |
| Secrets | Container Apps Secrets (no Key Vault) | Suficiente para TFG, menos complejidad |

### Costo estimado

| Recurso | USD/mes |
|---------|---------|
| Postgres Flexible Server B1ms (32 GB) | 13.10 |
| Container Registry Basic | 5.00 |
| Container Apps Consumption (scale-to-zero) | ~0.50 |
| Log Analytics (50 GB/mes free) | 0 |
| Bandwidth (primeros 100 GB free) | 0 |
| **Total** | **~$18-20** |

Con el crédito de $100/año dura **5-6 meses**. Estrategias de extensión más abajo en §10.

---

## 2. Arquitectura

```
┌─────────────────────────────────────────────────────────────────┐
│  Azure Subscription (Azure for Students)                         │
│  Region: East US                                                 │
│                                                                  │
│  Resource Group: rg-gymflow                                     │
│  ├── Azure Container Registry (acrgymflow*)                     │
│  │     └─ image: gymflow:latest                                 │
│  ├── Log Analytics Workspace (log-gymflow)                      │
│  ├── Container Apps Environment (cae-gymflow)                   │
│  │     └─ Container App (ca-gymflow)                            │
│  │           ├─ ingress external (HTTPS, puerto 8080)           │
│  │           ├─ scale 0→1 réplicas                              │
│  │           ├─ image desde ACR                                 │
│  │           └─ secrets: db-connection, jwt-key                 │
│  └── PostgreSQL Flexible Server (psqlgymflow*)                  │
│        ├─ SKU Standard_B1ms (burstable, 1 vCPU, 2 GB RAM)       │
│        ├─ 32 GB storage                                         │
│        ├─ database: gymflow                                     │
│        └─ firewall: AllowAzure (0.0.0.0)                        │
└─────────────────────────────────────────────────────────────────┘
```

`*` = sufijo aleatorio porque los nombres globales (ACR + Postgres) deben ser únicos.

### Flujo de request

1. Browser → `https://ca-gymflow.{random}.eastus.azurecontainerapps.io`
2. Ingress de Container Apps termina TLS y forwardea al container en puerto 8080
3. ASP.NET Core decide:
   - `/api/*` → controllers
   - cualquier otra ruta → sirve `wwwroot/index.html` (SPA fallback)
   - assets estáticos (`*.js`, `*.css`) → desde `wwwroot/`
4. Si el endpoint accede a la DB, EF Core usa la connection string inyectada vía secret

---

## 3. Cambios necesarios en el código (PR previo al deploy)

Estos cambios deben mergearse a `develop` **antes** de empezar el deploy. Es 1 commit chico.

### 3.1. Nuevo `Dockerfile` en la raíz del repo

Reemplaza al actual `backend/Dockerfile` con uno multi-stage que compila frontend Y backend:

```dockerfile
# Stage 1 — Build frontend (Vite)
FROM node:20-alpine AS frontend-build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Stage 2 — Build backend (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY backend/GymFlow.sln .
COPY backend/src/ ./src/
COPY backend/tests/ ./tests/
RUN dotnet restore
RUN dotnet publish src/GymFlow.API/GymFlow.API.csproj -c Release -o /app/publish --no-restore

# Stage 3 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=backend-build /app/publish .
COPY --from=frontend-build /app/dist ./wwwroot
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GymFlow.API.dll"]
```

### 3.2. Modificar `backend/src/GymFlow.API/Program.cs`

Agregar **antes** de `app.MapControllers()`:

```csharp
app.UseDefaultFiles();   // sirve index.html en /
app.UseStaticFiles();    // sirve wwwroot/*
```

Agregar **después** de `app.MapControllers()`:

```csharp
app.MapFallbackToFile("index.html");   // SPA routing
```

> Las rutas `/api/*` siguen yendo a los controllers (porque ya están mapeadas antes). Cualquier otra ruta (ej. `/admin/cuotas`) cae al fallback y React Router resuelve client-side.

### 3.3. Crear `backend/src/GymFlow.API/appsettings.Production.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Todo lo demás (connection string, JWT key, CORS, Email) viene de variables de entorno + secrets.

### 3.4. Frontend — `services/api.ts`

✅ No requiere cambios. Ya usa `baseURL: '/api'` (relativo).

### 3.5. CORS

✅ No se toca. El `app.UseCors("AllowFrontend")` sigue como está — en producción es no-op porque front y back van por el mismo origen. En dev sigue habilitando `localhost:5173`.

---

## 4. Pre-requisitos del compañero que ejecuta el deploy

```bash
# 1. Azure CLI 2.50 o superior
az --version

# 2. Login con la cuenta Azure for Students
az login

# 3. Si tiene varias suscripciones, elegir la de Students
az account list --output table
az account set --subscription "Azure for Students"

# 4. Instalar/actualizar extensiones (one-time)
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights
```

Esperar ~1 minuto a que los providers terminen de registrarse:

```bash
az provider show -n Microsoft.App --query registrationState
# Debe decir "Registered"
```

---

## 5. Variables a configurar al inicio

Pegar en la terminal antes de empezar (Bash / WSL / Git Bash):

```bash
export RG="rg-gymflow"
export LOCATION="eastus"
export ACR_NAME="acrgymflow$RANDOM"           # único global
export LOG_WORKSPACE="log-gymflow"
export ACA_ENV="cae-gymflow"
export ACA_APP="ca-gymflow"

# Postgres
export PG_NAME="psqlgymflow$RANDOM"            # único global
export PG_DB="gymflow"
export PG_ADMIN_USER="gymflowadmin"
export PG_ADMIN_PASSWORD="$(openssl rand -base64 24 | tr -d '/+=' | head -c 24)Aa1!"

# Secrets de la app
export JWT_KEY="$(openssl rand -base64 48 | tr -d '/+=')"

# 👉 IMPORTANTE: guardá estos valores en un password manager
echo "PG_ADMIN_PASSWORD=$PG_ADMIN_PASSWORD"
echo "JWT_KEY=$JWT_KEY"
echo "PG_NAME=$PG_NAME"
echo "ACR_NAME=$ACR_NAME"
```

**Equivalente PowerShell** (Windows sin WSL):

```powershell
$RG = "rg-gymflow"
$LOCATION = "eastus"
$Random = Get-Random -Maximum 99999
$ACR_NAME = "acrgymflow$Random"
$LOG_WORKSPACE = "log-gymflow"
$ACA_ENV = "cae-gymflow"
$ACA_APP = "ca-gymflow"
$PG_NAME = "psqlgymflow$Random"
$PG_DB = "gymflow"
$PG_ADMIN_USER = "gymflowadmin"
$PG_ADMIN_PASSWORD = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 20 | % {[char]$_}) + "Aa1!"
$JWT_KEY = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 48 | % {[char]$_})
```

---

## 6. Comandos de creación de recursos

> Todos los comandos esperan que las variables del §5 estén exportadas. Si abrís una terminal nueva, volvé a setear las variables.

### 6.1. Resource Group

```bash
az group create --name $RG --location $LOCATION
```

### 6.2. Azure Container Registry

```bash
az acr create \
  --resource-group $RG \
  --name $ACR_NAME \
  --sku Basic \
  --admin-enabled true

export ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer -o tsv)
export ACR_USER=$(az acr credential show --name $ACR_NAME --query username -o tsv)
export ACR_PASS=$(az acr credential show --name $ACR_NAME --query passwords[0].value -o tsv)
```

### 6.3. Log Analytics Workspace + Container Apps Environment

```bash
az monitor log-analytics workspace create \
  --resource-group $RG \
  --workspace-name $LOG_WORKSPACE \
  --location $LOCATION

export LOG_ID=$(az monitor log-analytics workspace show \
  --resource-group $RG --workspace-name $LOG_WORKSPACE \
  --query customerId -o tsv)

export LOG_KEY=$(az monitor log-analytics workspace get-shared-keys \
  --resource-group $RG --workspace-name $LOG_WORKSPACE \
  --query primarySharedKey -o tsv)

az containerapp env create \
  --name $ACA_ENV \
  --resource-group $RG \
  --location $LOCATION \
  --logs-workspace-id $LOG_ID \
  --logs-workspace-key $LOG_KEY
```

### 6.4. PostgreSQL Flexible Server

```bash
# Server
az postgres flexible-server create \
  --resource-group $RG \
  --name $PG_NAME \
  --location $LOCATION \
  --admin-user $PG_ADMIN_USER \
  --admin-password "$PG_ADMIN_PASSWORD" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16 \
  --public-access 0.0.0.0

# Base de datos
az postgres flexible-server db create \
  --resource-group $RG \
  --server-name $PG_NAME \
  --database-name $PG_DB

# Regla de firewall: permitir todos los servicios de Azure
az postgres flexible-server firewall-rule create \
  --resource-group $RG \
  --name $PG_NAME \
  --rule-name AllowAzure \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Connection string formato Npgsql
export DB_CONN="Host=${PG_NAME}.postgres.database.azure.com;Port=5432;Database=${PG_DB};Username=${PG_ADMIN_USER};Password=${PG_ADMIN_PASSWORD};SSL Mode=Require;Trust Server Certificate=true"
```

### 6.5. Build y push de la imagen Docker

Desde la raíz del repo (donde está el nuevo `Dockerfile`):

```bash
cd /path/to/GymFlow

az acr build \
  --registry $ACR_NAME \
  --image gymflow:latest \
  --file Dockerfile \
  .
```

> Tarda ~5-10 minutos la primera vez. Después es más rápido por el cache.

### 6.6. Container App

```bash
az containerapp create \
  --name $ACA_APP \
  --resource-group $RG \
  --environment $ACA_ENV \
  --image $ACR_LOGIN_SERVER/gymflow:latest \
  --registry-server $ACR_LOGIN_SERVER \
  --registry-username $ACR_USER \
  --registry-password $ACR_PASS \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 \
  --max-replicas 1 \
  --cpu 0.5 --memory 1Gi \
  --secrets \
    "db-connection=$DB_CONN" \
    "jwt-key=$JWT_KEY" \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ConnectionStrings__DefaultConnection=secretref:db-connection" \
    "Jwt__Key=secretref:jwt-key" \
    "Email__Habilitado=false"

# URL pública final
export APP_URL=$(az containerapp show \
  --name $ACA_APP --resource-group $RG \
  --query properties.configuration.ingress.fqdn -o tsv)

echo "🚀 App deployada en: https://$APP_URL"
```

---

## 7. Verificación post-deploy

```bash
# 1. Esperar que el container arranque y aplique migraciones (~30s primera vez)
sleep 30

# 2. Hit a la home → debe devolver index.html
curl -i https://$APP_URL/

# 3. Hit a /api/auth/login → debe devolver token
curl -X POST https://$APP_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"correo":"admin@gymflow.com","password":"admin123"}'

# 4. Logs en vivo
az containerapp logs show --name $ACA_APP --resource-group $RG --follow
```

✅ Si los 3 pasos funcionan, **el deploy está OK**. Compartí la URL al equipo.

---

## 8. Re-deploy con código nuevo

El re-deploy se hace **automáticamente vía GitHub Actions** cada vez que se mergea código a `main` (ver §13). El flujo es:

1. Equipo abre PR contra `develop`
2. Code review + CI verde (tests)
3. Merge a `develop`
4. Cuando se quiere release a producción → PR de `develop` a `main`
5. Al mergear a `main` → GitHub Actions builduea la imagen, la pushea a ACR y actualiza el Container App
6. Tiempo total automático: ~5-7 minutos

### Re-deploy manual (fallback / hotfix)

Si el pipeline está roto o se necesita un deploy urgente bypaseando GitHub:

```bash
# 1. Build nueva imagen (mismo tag :latest)
az acr build --registry $ACR_NAME --image gymflow:latest --file Dockerfile .

# 2. Forzar update del Container App
az containerapp update \
  --name $ACA_APP \
  --resource-group $RG \
  --image $ACR_LOGIN_SERVER/gymflow:latest
```

Tiempo total: ~2-3 minutos. Sin downtime (Container Apps hace rolling update).

---

## 9. Manejo de secrets

### Listar secrets actuales

```bash
az containerapp secret list --name $ACA_APP --resource-group $RG -o table
```

### Actualizar un secret

```bash
# Rotar JWT key (invalida todos los tokens vigentes)
NEW_JWT_KEY=$(openssl rand -base64 48 | tr -d '/+=')

az containerapp secret set \
  --name $ACA_APP --resource-group $RG \
  --secrets "jwt-key=$NEW_JWT_KEY"

# El secret se actualiza pero el container sigue con el viejo hasta el próximo restart.
# Forzar restart:
az containerapp revision restart \
  --name $ACA_APP --resource-group $RG \
  --revision $(az containerapp show --name $ACA_APP --resource-group $RG --query properties.latestRevisionName -o tsv)
```

### Variables de entorno → mapeo de configuración

ASP.NET Core mapea variables con `__` a propiedades anidadas:

| Variable de entorno | Lee de `appsettings.json` |
|---|---|
| `Jwt__Key` | `Jwt.Key` |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings.DefaultConnection` |
| `Email__Habilitado` | `Email.Habilitado` |
| `Cors__AllowedOrigins__0` | `Cors.AllowedOrigins[0]` |

---

## 10. Pausar / reanudar para extender el crédito

### Pausar la DB (cuando no se está usando)

```bash
az postgres flexible-server stop --resource-group $RG --name $PG_NAME
# Solo se paga storage (~$3/mes en lugar de $13)
```

### Reanudar

```bash
az postgres flexible-server start --resource-group $RG --name $PG_NAME
# Tarda ~2 minutos en estar lista
```

### Scale-to-zero ya configurado en el Container App

Con `minReplicas=0`, el container se apaga solo cuando no hay tráfico por ~10 minutos. No requiere comando manual.

---

## 11. Troubleshooting

### Tabla de síntomas comunes

| Síntoma | Causa típica | Fix |
|---------|--------------|-----|
| Primera request tarda 10-15s | Cold start de scale-to-zero | Normal. Para evitarlo, `minReplicas=1` (cuesta ~$10/mes extra) |
| 502 / no response | Container falló al arrancar | `az containerapp logs show --follow` |
| `FK_Usuarios_Roles_RolId violation` al deployar | Datos viejos en DB | Borrar y recrear la DB (§6.4) |
| `Jwt:Key not configured` | Secret no inyectado | Verificar con `az containerapp secret list` y `--query properties.template.containers[0].env` |
| `Connection refused` a Postgres | Firewall bloqueando | `az postgres flexible-server firewall-rule list` debe tener `AllowAzure` |
| `Pull image failed` | Credenciales ACR mal | `az containerapp registry set --name $ACA_APP --server $ACR_LOGIN_SERVER --username $ACR_USER --password $ACR_PASS` |
| 404 en rutas del frontend (ej. `/admin/cuotas`) | Falta `MapFallbackToFile` | Verificar §3.2 |

### Comandos de diagnóstico

```bash
# Estado del Container App
az containerapp show --name $ACA_APP --resource-group $RG \
  --query "properties.runningStatus" -o tsv

# Variables de entorno actuales
az containerapp show --name $ACA_APP --resource-group $RG \
  --query "properties.template.containers[0].env" -o table

# Revisión actual (deploy más reciente)
az containerapp revision list --name $ACA_APP --resource-group $RG -o table

# Conexión directa a Postgres (requiere psql local)
psql "host=${PG_NAME}.postgres.database.azure.com user=${PG_ADMIN_USER} dbname=${PG_DB} sslmode=require"
```

---

## 12. Dominio custom (opcional)

Si en algún momento se quiere usar un dominio propio (ej. `gymflow.com.uy`):

```bash
# 1. Agregar el hostname
az containerapp hostname add \
  --hostname gymflow.tu-dominio.com \
  --resource-group $RG --name $ACA_APP

# 2. En el proveedor DNS: crear CNAME apuntando a $APP_URL
#    Tipo: CNAME
#    Nombre: gymflow
#    Valor: ca-gymflow.{random}.eastus.azurecontainerapps.io

# 3. Binding con cert managed (gratis, auto-renovado)
az containerapp hostname bind \
  --hostname gymflow.tu-dominio.com \
  --resource-group $RG --name $ACA_APP \
  --validation-method CNAME
```

Para la entrega del TFG, **la URL default es suficiente**. El profe entiende.

---

## 13. CI/CD — Auto-deploy desde GitHub Actions a main

**Parte obligatoria del setup.** Cada push a `main` (vía merge de PR desde `develop`) dispara automáticamente:
1. Tests del backend y frontend
2. Build de la imagen Docker en ACR
3. Update del Container App con la nueva imagen
4. Smoke test del endpoint de health

Esto **reemplaza** los pasos manuales del §8. El equipo solo mergea PRs — el deploy es automático.

### 13.1. Crear Service Principal para GitHub Actions

```bash
# Obtener el ID de la suscripción
export SUB_ID=$(az account show --query id -o tsv)

# Crear Service Principal con permisos solo sobre el Resource Group
az ad sp create-for-rbac --name "sp-gymflow-cicd" \
  --role contributor \
  --scopes /subscriptions/$SUB_ID/resourceGroups/$RG \
  --sdk-auth > azure-credentials.json

cat azure-credentials.json
# Output formato:
# {
#   "clientId": "...",
#   "clientSecret": "...",
#   "subscriptionId": "...",
#   "tenantId": "...",
#   ...
# }
```

⚠️ **El archivo `azure-credentials.json` contiene credenciales que NUNCA deben commitearse.**
- Verificar que `.gitignore` incluya `azure-credentials.json`
- Borrar el archivo después de subirlo a GitHub Secrets

### 13.2. Agregar secrets a GitHub

En **Settings → Secrets and variables → Actions** del repo `ignaciocomp/GymFlow`:

#### Secrets (`Settings → Secrets and variables → Actions → New repository secret`):

| Nombre | Valor |
|--------|-------|
| `AZURE_CREDENTIALS` | Contenido COMPLETO del JSON de `azure-credentials.json` |

#### Variables (`Settings → Secrets and variables → Actions → Variables → New variable`):

| Nombre | Valor (de las variables del §5) |
|--------|--------------------------------|
| `AZURE_RG` | `rg-gymflow` |
| `AZURE_ACR_NAME` | `acrgymflow12345` (el nombre random que generó §5) |
| `AZURE_ACR_LOGIN_SERVER` | `acrgymflow12345.azurecr.io` |
| `AZURE_ACA_APP` | `ca-gymflow` |

Las **variables** son visibles en logs (OK porque no son secretas). Los **secrets** se enmascaran automáticamente.

### 13.3. Workflow file `.github/workflows/deploy.yml`

Crear este archivo en el repo y commitearlo:

```yaml
name: Deploy to Azure Container Apps

on:
  push:
    branches: [main]
  workflow_dispatch:  # permite disparar manual desde la UI

# Evita 2 deploys simultáneos pisándose
concurrency:
  group: deploy-production
  cancel-in-progress: false

jobs:
  test:
    name: Tests
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: gymflow_test
        ports: ["5432:5432"]
        options: >-
          --health-cmd pg_isready --health-interval 10s --health-timeout 5s --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Backend — test
        working-directory: backend
        run: |
          dotnet restore
          dotnet build --no-restore
          dotnet test --no-build -v minimal
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - name: Frontend — build
        working-directory: frontend
        run: |
          npm ci
          npm run build
          npx vitest run

  deploy:
    name: Deploy to Azure
    runs-on: ubuntu-latest
    needs: test  # solo deploya si los tests pasan
    steps:
      - uses: actions/checkout@v4

      - name: Azure login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Build & push image to ACR (tag con SHA)
        run: |
          az acr build \
            --registry ${{ vars.AZURE_ACR_NAME }} \
            --image gymflow:${{ github.sha }} \
            --image gymflow:latest \
            --file Dockerfile .

      - name: Update Container App
        run: |
          az containerapp update \
            --name ${{ vars.AZURE_ACA_APP }} \
            --resource-group ${{ vars.AZURE_RG }} \
            --image ${{ vars.AZURE_ACR_LOGIN_SERVER }}/gymflow:${{ github.sha }}

      - name: Obtener URL pública
        id: app-url
        run: |
          URL=$(az containerapp show \
            --name ${{ vars.AZURE_ACA_APP }} \
            --resource-group ${{ vars.AZURE_RG }} \
            --query properties.configuration.ingress.fqdn -o tsv)
          echo "url=https://$URL" >> "$GITHUB_OUTPUT"
          echo "🚀 Deployado en: https://$URL"

      - name: Smoke test — esperar a que el container responda
        run: |
          for i in {1..30}; do
            STATUS=$(curl -o /dev/null -s -w "%{http_code}" ${{ steps.app-url.outputs.url }}/ || echo "000")
            if [ "$STATUS" = "200" ]; then
              echo "✅ App responde con 200 OK"
              exit 0
            fi
            echo "⏳ Intento $i/30 — status $STATUS, reintentando en 5s..."
            sleep 5
          done
          echo "❌ App no respondió 200 después de 30 intentos"
          exit 1

      - name: Resumen en el summary del run
        if: always()
        run: |
          echo "## 🚀 Deploy a Azure" >> $GITHUB_STEP_SUMMARY
          echo "- Imagen: \`gymflow:${{ github.sha }}\`" >> $GITHUB_STEP_SUMMARY
          echo "- URL: ${{ steps.app-url.outputs.url }}" >> $GITHUB_STEP_SUMMARY
          echo "- Commit: ${{ github.sha }}" >> $GITHUB_STEP_SUMMARY
```

### 13.4. Limpieza después de configurar

```bash
# Borrar el JSON local — ya está en GitHub Secrets
rm azure-credentials.json
```

### 13.5. Verificar el pipeline

```bash
# Hacer un cambio mínimo y pushear a main para disparar el pipeline
# (o usar workflow_dispatch desde la UI: Actions → Deploy to Azure → Run workflow)
```

Ir a **GitHub → Actions** y ver el run completo. Debe terminar con:
- ✅ Tests
- ✅ Build & push image to ACR
- ✅ Update Container App
- ✅ Smoke test 200 OK

### 13.6. Relación con el CI actual

El workflow existente `.github/workflows/ci.yml` corre tests en push/PR a `develop` y `main`. **No se modifica.** El nuevo `deploy.yml` solo dispara en push a `main` y deploya. Son workflows independientes.

> El job `test` del `deploy.yml` puede parecer redundante con `ci.yml`, pero garantiza que **si se hace un push directo a `main` (vía bypass admin)**, igual corren los tests antes de deployar. Es defensa en profundidad.

### 13.7. Branch protection en `main`

**Recomendado** (en `Settings → Rules → Rulesets` del repo):
- Require pull request before merging
- Require status checks to pass (incluir el job `test` del workflow `deploy`)
- Block force pushes
- Bypass list: solo Repository admin

Esto evita pushes directos a main y obliga al flujo `develop → PR → main`.

### 13.8. Rollback

Si un deploy a `main` rompe la app:

```bash
# 1. Ver revisions activas
az containerapp revision list \
  --name $ACA_APP --resource-group $RG -o table

# 2. Activar la revision anterior (la que funcionaba)
az containerapp revision activate \
  --name $ACA_APP --resource-group $RG \
  --revision <NOMBRE_REVISION_ANTERIOR>

# 3. (Opcional) Desactivar la revision rota
az containerapp revision deactivate \
  --name $ACA_APP --resource-group $RG \
  --revision <NOMBRE_REVISION_ROTA>
```

Container Apps mantiene historial de revisiones por default — se puede volver al estado anterior en segundos sin re-buildear.

> **Tip:** usar **tag con SHA** (`gymflow:abc1234`) en vez de `:latest` en producción permite reproducir cualquier revision con `az containerapp update --image ...sha`. El workflow ya lo hace.

### Valores reales para GitHub Variables (deploy actual)
| Variable | Valor |
|----------|-------|
| AZURE_RG | rg-gymflow |
| AZURE_ACR_NAME | acrgymflow42543 |
| AZURE_ACR_LOGIN_SERVER | acrgymflow42543.azurecr.io |
| AZURE_ACA_APP | ca-gymflow |

---

## 14. Cleanup completo

Al terminar la entrega (o para evitar gasto):

```bash
az group delete --name $RG --yes --no-wait
```

Borra **TODO** (RG, ACR, ACA Environment, Container App, Postgres, Log Analytics) en ~3 minutos.

---

## 15. Checklist final para el compañero

### Antes de empezar
- [ ] Azure CLI 2.50+ instalado (`az --version`)
- [ ] Login con la cuenta Azure for Students (`az login`)
- [ ] Suscripción correcta seleccionada (`az account show`)
- [ ] Extensiones registradas (`az provider show -n Microsoft.App`)
- [ ] Repo clonado y rama `develop` (o `main`) actualizada
- [ ] Cambios del §3 mergeados (Dockerfile, Program.cs, appsettings.Production.json)

### Durante el deploy
- [ ] Variables del §5 exportadas
- [ ] `PG_ADMIN_PASSWORD` y `JWT_KEY` **guardados en password manager**
- [ ] Resource Group creado (§6.1)
- [ ] ACR creado con admin enabled (§6.2)
- [ ] Log Analytics + ACA Environment creados (§6.3)
- [ ] Postgres + DB + firewall creados (§6.4)
- [ ] Imagen buildeada en ACR (§6.5)
- [ ] Container App creado con secrets + env vars (§6.6)

### Post-deploy (manual inicial)
- [ ] URL pública obtenida y verificada con `curl /api/auth/login` (§7)
- [ ] Login admin funciona desde el browser
- [ ] Logs sin errores (`az containerapp logs show --follow`)
- [ ] URL final documentada en `README.md`

### Configurar CI/CD (después del primer deploy manual)
- [ ] Service Principal `sp-gymflow-cicd` creado (§13.1)
- [ ] `azure-credentials.json` agregado como `AZURE_CREDENTIALS` en GitHub Secrets (§13.2)
- [ ] Variables `AZURE_RG`, `AZURE_ACR_NAME`, `AZURE_ACR_LOGIN_SERVER`, `AZURE_ACA_APP` en GitHub Variables (§13.2)
- [ ] Workflow `.github/workflows/deploy.yml` commiteado (§13.3)
- [ ] Archivo local `azure-credentials.json` borrado (§13.4)
- [ ] Test del pipeline: cambio chico → push a `main` → verificar deploy verde (§13.5)
- [ ] Branch protection en `main` configurada (§13.7)

### Final
- [ ] Avisar al equipo y al profe con la URL

---

## 16. Apéndice: archivos a crear/modificar (resumen)

| Archivo | Acción | Sección |
|---------|--------|---------|
| `Dockerfile` (raíz) | **Crear** | §3.1 |
| `backend/src/GymFlow.API/Program.cs` | **Modificar** (3 líneas) | §3.2 |
| `backend/src/GymFlow.API/appsettings.Production.json` | **Crear** | §3.3 |
| `backend/Dockerfile` (viejo) | **Mantener** o eliminar (sirve para dev) | — |

---

## 17. Apéndice: glosario rápido

- **ACR (Azure Container Registry):** repositorio privado de imágenes Docker en Azure.
- **ACA (Azure Container Apps):** servicio managed para correr containers, con autoscaling y scale-to-zero.
- **Container Apps Environment:** infra compartida donde viven una o más Container Apps (red interna, logs, etc).
- **Scale-to-zero:** la app se apaga sola cuando no hay tráfico; el primer request siguiente la despierta (cold start ~10-15s).
- **Burstable tier (B-series):** SKUs de DB con CPU que puede "burst" más allá del baseline cuando hace falta; más barato que General Purpose.
- **Connection string Npgsql:** formato de string para conectar a Postgres desde .NET (`Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require`).
- **secretref:** sintaxis de Container Apps para referenciar un secret desde una env var (`secretref:nombre-del-secret`).

---

**Autor:** equipo GymFlow
**Última actualización:** 2026-05-21
