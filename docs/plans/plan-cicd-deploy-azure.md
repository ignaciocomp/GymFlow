# Pipeline de Auto-Deploy a Azure — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que cada push a `main` dispare automáticamente el deploy de GymFlow a Azure Container Apps (build de imagen en ACR + update del Container App + smoke test).

**Architecture:** GitHub Actions workflow `deploy.yml` con 2 jobs: `test` (corre la suite backend+frontend) y `deploy` (depende de test; hace `az acr build` con el Dockerfile single-container de la raíz, `az containerapp update`, y un smoke test con retry). Usa un Service Principal de Azure guardado como GitHub Secret. El workflow existente `ci.yml` no se toca.

**Tech Stack:** GitHub Actions, Azure CLI (`az`), Azure Container Registry, Azure Container Apps.

**Spec:** [[azure-deploy-design]] (docs/deploy/azure-deploy-design.md §13)

## Estado actual (verificado)

- ✅ `Dockerfile` single-container en la raíz (frontend Vite → backend publish → runtime sirve `wwwroot`, `EXPOSE 8080`).
- ✅ `Program.cs` sirve el frontend estático (`UseStaticFiles` + `MapFallbackToFile`).
- ✅ Recursos Azure ya creados: `rg-gymflow`, `acrgymflow42543` (ACR), `ca-gymflow` (Container App), región `eastus`.
- ✅ `ci.yml` existente (tests en push/PR a main/develop) — NO se modifica.
- ❌ NO existe `deploy.yml` ni el Service Principal ni los GitHub Secrets/Variables.

## Decisiones de diseño

- **Tag con SHA + `latest`:** la imagen se taggea con `${{ github.sha }}` (para rollback reproducible) y también `latest`.
- **Job `test` previo al deploy:** garantiza que un push directo a `main` (vía bypass admin) igual corra los tests antes de deployar. Es defensa en profundidad — redundante con `ci.yml` pero intencional.
- **Concurrency:** un solo deploy a la vez (`cancel-in-progress: false`) para no pisar deploys.
- **Recursos por GitHub Variables** (no secrets): `AZURE_RG`, `AZURE_ACR_NAME`, `AZURE_ACR_LOGIN_SERVER`, `AZURE_ACA_APP` — no son sensibles. El único secret es `AZURE_CREDENTIALS` (JSON del Service Principal).
- **`workflow_dispatch`:** permite disparar el deploy manual desde la UI de Actions.

---

## Task 0: Crear la rama de trabajo

**Files:** ninguno

- [ ] **Step 1: Crear la rama desde develop actualizado**

```bash
git fetch origin
git checkout -b feature/cicd-deploy-azure origin/develop
```
> Esta rama es independiente del trabajo de IT4. Todos los commits de este plan van acá.

---

## Task 1: Verificar que el doc de deploy esté actualizado con los recursos reales

**Files:**
- Read: `docs/deploy/azure-deploy-design.md`

- [ ] **Step 1: Leer el doc y confirmar §13**

Leer `docs/deploy/azure-deploy-design.md`. Confirmar que la sección de CI/CD (§13) referencia los nombres de recursos reales: `rg-gymflow`, `acrgymflow42543`, `acrgymflow42543.azurecr.io`, `ca-gymflow`. Si el doc usa placeholders genéricos (ej. `acrgymflow$RANDOM`), está bien — el workflow usa GitHub Variables, no valores hardcodeados. Solo verificar que no haya contradicciones con el `deploy.yml` que se crea en la Task 2.

- [ ] **Step 2: Si falta, anotar los nombres reales**

Si el doc no lista los valores concretos a cargar en GitHub Variables, agregá una tabla al final de §13:

```markdown
### Valores reales para GitHub Variables (deploy actual)
| Variable | Valor |
|----------|-------|
| AZURE_RG | rg-gymflow |
| AZURE_ACR_NAME | acrgymflow42543 |
| AZURE_ACR_LOGIN_SERVER | acrgymflow42543.azurecr.io |
| AZURE_ACA_APP | ca-gymflow |
```

- [ ] **Step 3: Commit (si hubo cambios al doc)**

```bash
git add docs/deploy/azure-deploy-design.md
git commit -m "docs: valores reales de recursos Azure para el pipeline de deploy"
```
(Si no hubo cambios, saltear el commit.)

---

## Task 2: Crear el workflow `deploy.yml`

**Files:**
- Create: `.github/workflows/deploy.yml`

- [ ] **Step 1: Crear el archivo con este contenido exacto**

```yaml
name: Deploy to Azure Container Apps

on:
  push:
    branches: [main]
  workflow_dispatch:

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
      - name: Frontend — build & test
        working-directory: frontend
        run: |
          npm ci
          npm run build
          npx vitest run

  deploy:
    name: Deploy to Azure
    runs-on: ubuntu-latest
    needs: test
    steps:
      - uses: actions/checkout@v4

      - name: Azure login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Build & push image to ACR (tag con SHA + latest)
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
          echo "Deploy en: https://$URL"

      - name: Smoke test — esperar 200 OK
        run: |
          for i in {1..30}; do
            STATUS=$(curl -o /dev/null -s -w "%{http_code}" ${{ steps.app-url.outputs.url }}/ || echo "000")
            if [ "$STATUS" = "200" ]; then
              echo "App responde 200 OK"
              exit 0
            fi
            echo "Intento $i/30 — status $STATUS, reintentando en 5s..."
            sleep 5
          done
          echo "App no respondió 200 después de 30 intentos"
          exit 1

      - name: Resumen
        if: always()
        run: |
          echo "## Deploy a Azure" >> $GITHUB_STEP_SUMMARY
          echo "- Imagen: \`gymflow:${{ github.sha }}\`" >> $GITHUB_STEP_SUMMARY
          echo "- URL: ${{ steps.app-url.outputs.url }}" >> $GITHUB_STEP_SUMMARY
          echo "- Commit: ${{ github.sha }}" >> $GITHUB_STEP_SUMMARY
```

- [ ] **Step 2: Verificar que es YAML válido**

Si `actionlint` está disponible: `actionlint .github/workflows/deploy.yml`.
Si no, validar la sintaxis YAML con Python:
```bash
python -c "import yaml; yaml.safe_load(open('.github/workflows/deploy.yml')); print('YAML OK')"
```
Expected: `YAML OK` (sin excepciones).

- [ ] **Step 3: Verificar coherencia con el Dockerfile**

Confirmar que el `--file Dockerfile .` apunta al Dockerfile de la raíz (que existe y es single-container). El contexto de build es `.` (raíz del repo), correcto porque el Dockerfile copia `frontend/` y `backend/`.

- [ ] **Step 4: Blindar `.gitignore` contra el JSON del Service Principal**

Verificar si `.gitignore` ya incluye `azure-credentials.json`:
```bash
grep -q "azure-credentials.json" .gitignore && echo "ya está" || echo "FALTA"
```
Si dice `FALTA`, agregarlo (el compañero generará ese archivo en la Task 3 y NUNCA debe commitearse):
```bash
printf '\n# Azure Service Principal (NUNCA commitear)\nazure-credentials.json\n' >> .gitignore
```

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/deploy.yml .gitignore
git commit -m "ci: workflow de auto-deploy a Azure Container Apps en push a main"
```

---

## Task 3: Documentar los pasos manuales del compañero (requieren credenciales Azure)

**Files:**
- Create: `docs/deploy/SETUP-CICD.md`

> El agente NO puede ejecutar estos pasos (requieren `az login` con la cuenta Azure del compañero). Se documentan como checklist para que el compañero los ejecute.

- [ ] **Step 1: Crear `docs/deploy/SETUP-CICD.md`**

```markdown
# Setup del pipeline de auto-deploy (pasos manuales)

> Estos pasos los ejecuta quien tiene las credenciales Azure (Seba). Una sola vez.
> Después de esto, cada push a `main` deploya automáticamente.

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

⚠️ `azure-credentials.json` contiene credenciales. NUNCA commitearlo (verificar que `.gitignore` lo incluya). Borrarlo después del paso 2.

## 2. Cargar secrets y variables en GitHub

En **Settings → Secrets and variables → Actions** del repo:

### Secret (New repository secret):
| Nombre | Valor |
|--------|-------|
| `AZURE_CREDENTIALS` | El JSON COMPLETO de `azure-credentials.json` |

### Variables (pestaña Variables → New variable):
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

- Opción A: hacer un push chico a `main`.
- Opción B: **Actions → Deploy to Azure Container Apps → Run workflow** (gracias a `workflow_dispatch`).

Verificar en Actions que el run termine verde: Tests → Build image → Update Container App → Smoke test 200 OK.

## Rollback

Si un deploy rompe la app:
```bash
az containerapp revision list --name ca-gymflow --resource-group rg-gymflow -o table
az containerapp revision activate --name ca-gymflow --resource-group rg-gymflow --revision <REVISION_ANTERIOR>
```
```

- [ ] **Step 2: Commit**

```bash
git add docs/deploy/SETUP-CICD.md
git commit -m "docs: checklist de setup manual del pipeline de deploy (Service Principal + GitHub secrets)"
```

---

## Task 4: Push de la rama + PR a develop

**Files:** ninguno

- [ ] **Step 1: Push de la rama creada en Task 0**

```bash
git push -u origin feature/cicd-deploy-azure
```
> La rama `feature/cicd-deploy-azure` ya fue creada en la Task 0. Verificá con `git branch --show-current` que estás parado en ella.

- [ ] **Step 2: Crear el PR**

```bash
gh pr create --base develop \
  --title "ci: pipeline de auto-deploy a Azure en push a main" \
  --body "Agrega .github/workflows/deploy.yml (build imagen en ACR + update Container App + smoke test), docs/deploy/SETUP-CICD.md con los pasos manuales del Service Principal + GitHub secrets, y azure-credentials.json al .gitignore. No toca ci.yml."
```

- [ ] **Step 3: Verificar CI del PR**

El `ci.yml` existente debe pasar verde en el PR (el `deploy.yml` NO se dispara en PRs, solo en push a main).

---

## Notas importantes

- **El workflow no deploya hasta que el compañero complete la Task 3** (sin `AZURE_CREDENTIALS` el job `deploy` falla en el login). Esto es esperado: el código del pipeline se mergea, y el deploy real empieza a funcionar cuando se cargan los secrets.
- **El `deploy.yml` se dispara en push a `main`**, no a `develop`. El flujo es: feature → PR → develop → PR → main → deploy automático.
- **`ci.yml` sigue corriendo** en push/PR a develop y main (tests). Son workflows independientes.
