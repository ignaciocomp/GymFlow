---
title: CI/CD auto-deploy (iteración 4)
tags:
  - seguimiento
  - deploy
  - cicd
related:
  - "[[iteracion-4]]"
  - "[[azure-deploy-design]]"
  - "[[SETUP-CICD]]"
---

## CI/CD: auto-deploy a Azure en push a `main`

A partir de esta iteración el deploy a producción es automático. Cada merge a `main` (vía PR desde `develop`) dispara un workflow de GitHub Actions que corre tests, builduea la imagen, la pushea a ACR y actualiza el Container App. Reemplaza el procedimiento manual de la iteración 3.

### Pipeline (`.github/workflows/deploy.yml`)

Tres jobs encadenados:

1. **Tests** — backend (`dotnet test`) y frontend (`npm run build` + `vitest run`). Bloquea el deploy si fallan.
2. **Deploy** — `az acr build` con tag `:<sha>` + `:latest`, `az containerapp update` con la imagen nueva. Container Apps hace rolling update sin downtime.
3. **Smoke test** — polling de `GET /` hasta recibir 200 OK (hasta 30 intentos cada 5 s). Si no responde, el job falla y dispara rollback automático a la revisión anterior.

Tag con SHA del commit en cada imagen → cualquier revisión es reproducible y el rollback es un `az containerapp revision activate <rev>`.

### Setup realizado

- **Service Principal `sp-gymflow-cicd`** con rol Contributor scoped solo al Resource Group `rg-gymflow`.
- **GitHub Secrets:** `AZURE_CREDENTIALS` (JSON del SP).
- **GitHub Variables:** `AZURE_RG`, `AZURE_ACR_NAME`, `AZURE_ACR_LOGIN_SERVER`, `AZURE_ACA_APP`.
- **Branch protection en `main`:** require PR, require status checks (`test` job), block force pushes.

### Iteraciones del pipeline durante la iteración

- Workflow inicial (07/06).
- Rollback automático ante smoke test fallido + simplificación del job de tests (sin Postgres, no hacían falta integration tests en el pipeline).
- Workflow auxiliar `workflow_dispatch` para activar SMTP en el Container App sin re-deployar.
- Fix: el `az acr build` quedó bloqueado por límites de la suscripción Students; se reemplazó por `docker build` + `docker push` corriendo directo en el runner.

### Documentación de referencia

- [[SETUP-CICD]] — checklist de setup manual (Service Principal + GitHub Secrets/Variables).
- [[azure-deploy-design]] §13 — diseño completo del pipeline y branch protection.
