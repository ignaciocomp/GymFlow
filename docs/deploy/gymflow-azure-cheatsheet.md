# GymFlow — Azure Cheatsheet

**Ultima actualizacion:** 2026-05-26

---

#deploy

## 1. Variables de entorno (copiar en cada terminal nueva)

```powershell
$RG = "rg-gymflow"
$ACR_NAME = "acrgymflow42543"
$ACR_LOGIN_SERVER = "acrgymflow42543.azurecr.io"
$ACA_APP = "ca-gymflow"
$ACA_ENV = "cae-gymflow"
$PG_NAME = "psqlgymflow42543"
$PG_DB = "gymflow"
$PG_ADMIN_USER = "gymflowadmin"
$LOG_WORKSPACE = "log-gymflow"
```

> Las passwords y JWT_KEY NO van aca — buscarlas en el password manager.

---

## 2. URL de la app

```
https://ca-gymflow.gentlemeadow-5931333d.eastus2.azurecontainerapps.io
```

---

## 3. Subir cambios de codigo (re-deploy manual)

```powershell
cd C:\Users\sebas\Documents\00_materias\proyecto_integrador\dev\GymFlow

# 1. Build de la imagen (usar --no-cache si los cambios de frontend no se reflejan)
docker build -t "$ACR_LOGIN_SERVER/gymflow:latest" -f Dockerfile .

# 2. Push al registry
docker push "$ACR_LOGIN_SERVER/gymflow:latest"

# 3. Actualizar el Container App
az containerapp update --name $ACA_APP --resource-group $RG --image "$ACR_LOGIN_SERVER/gymflow:latest"

# 4. Si los cambios no aparecen, forzar restart
$REV = az containerapp show --name $ACA_APP --resource-group $RG --query properties.latestRevisionName -o tsv
az containerapp revision restart --name $ACA_APP --resource-group $RG --revision $REV
```

Tiempo total: ~5 minutos. Sin downtime.

> **Importante:** Despues del update, refrescar el browser con **Ctrl + F5** (hard refresh) para limpiar cache.
> Si los cambios de frontend siguen sin verse, rebuildeá con `docker build --no-cache`.

---

## 4. Ver logs en vivo

```powershell
az containerapp logs show --name $ACA_APP --resource-group $RG --follow
```

---

## 5. Reiniciar el Container App

```powershell
# Obtener nombre de la revision activa
$REV = az containerapp show --name $ACA_APP --resource-group $RG --query properties.latestRevisionName -o tsv

# Reiniciar
az containerapp revision restart --name $ACA_APP --resource-group $RG --revision $REV
```

---

## 6. Ver estado y diagnostico

```powershell
# Estado general
az containerapp show --name $ACA_APP --resource-group $RG --query "properties.runningStatus" -o tsv

# Variables de entorno actuales
az containerapp show --name $ACA_APP --resource-group $RG --query "properties.template.containers[0].env" -o table

# Revisiones (historial de deploys)
az containerapp revision list --name $ACA_APP --resource-group $RG -o table

# Secrets configurados
az containerapp secret list --name $ACA_APP --resource-group $RG -o table
```

---

## 7. Pausar y reanudar Postgres (para ahorrar credito)

El Postgres es lo que mas gasta (~$13/mes). Cuando no lo estes usando:

```powershell
# Pausar (baja a ~$3/mes, solo storage)
az postgres flexible-server stop --resource-group $RG --name $PG_NAME

# Reanudar (~2 min en arrancar)
az postgres flexible-server start --resource-group $RG --name $PG_NAME
```

> El Container App ya tiene scale-to-zero — se apaga solo sin trafico.

---

## 8. Rollback a una version anterior

```powershell
# Ver revisiones disponibles
az containerapp revision list --name $ACA_APP --resource-group $RG -o table

# Activar revision anterior
az containerapp revision activate --name $ACA_APP --resource-group $RG --revision <NOMBRE_REVISION_ANTERIOR>

# (Opcional) Desactivar la revision rota
az containerapp revision deactivate --name $ACA_APP --resource-group $RG --revision <NOMBRE_REVISION_ROTA>
```

---

## 9. Actualizar un secret

```powershell
# Ejemplo: rotar JWT key
$NEW_JWT_KEY = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 48 | % {[char]$_})

az containerapp secret set --name $ACA_APP --resource-group $RG --secrets "jwt-key=$NEW_JWT_KEY"

# Reiniciar para que tome el nuevo valor
$REV = az containerapp show --name $ACA_APP --resource-group $RG --query properties.latestRevisionName -o tsv
az containerapp revision restart --name $ACA_APP --resource-group $RG --revision $REV
```

---

## 10. Conectarse a la DB directamente

Requiere tener `psql` instalado localmente y agregar tu IP al firewall:

```powershell
# Agregar tu IP
az postgres flexible-server firewall-rule create --resource-group $RG --name $PG_NAME --rule-name AllowMyIP --start-ip-address <TU_IP> --end-ip-address <TU_IP>

# Conectar
psql "host=psqlgymflow42543.postgres.database.azure.com user=gymflowadmin dbname=gymflow sslmode=require"
```

---

## 11. Borrar TODO (cleanup final)

Esto elimina todos los recursos (ACR, Container App, Postgres, Log Analytics):

```powershell
az group delete --name $RG --yes --no-wait
```

> Irreversible. Solo usar cuando ya no necesites el deploy.

---

## 12. Costo estimado

| Recurso | USD/mes |
|---------|---------|
| Postgres Flexible Server B1ms | ~13 |
| Container Registry Basic | ~5 |
| Container App (scale-to-zero) | ~0.50 |
| **Total** | **~$18-20** |

Con $100/anio de Azure for Students dura ~5-6 meses.

---

## 13. Regiones permitidas (politica ORT)

La suscripcion solo permite deployar en:
- `eastus2` (ACR, Container App, Log Analytics)
- `canadacentral` (Postgres)
- `southcentralus`
- `mexicocentral`
- `southafricanorth`

---

## 14. Troubleshooting rapido

| Sintoma | Causa | Fix |
|---------|-------|-----|
| Primera request tarda 10-15s | Cold start (scale-to-zero) | Normal. `minReplicas=1` lo evita (~$10/mes extra) |
| 502 / no response | Container fallo al arrancar | Ver logs: `az containerapp logs show --follow` |
| Variables perdidas en terminal | PowerShell no persiste variables | Copiar bloque del punto 1 |
| `az acr build` falla | ACR Tasks bloqueado por la ORT | Usar `docker build` + `docker push` local |
| Pull image failed | Credenciales ACR vencidas | Regenerar con `az acr credential show` |
| DB no responde | Postgres pausado | `az postgres flexible-server start` |
| Cambios de frontend no se ven | Cache de Docker o del browser | `docker build --no-cache` + Ctrl+F5 en browser |
| Cambios no se ven despues del update | Container no reinicio | Forzar restart con `az containerapp revision restart` |

---

## 15. Prerequisitos para hacer deploy

- **Docker Desktop** corriendo (verificar con `docker info`)
- **Azure CLI** logueado (`az login`)
- Variables del punto 1 copiadas en la terminal
- Estar parado en la raiz del repo para el `docker build`

---

## 16. Login en Azure (si se vence la sesion)

```powershell
az login
# Seleccionar opcion 1 (Azure for Students)
az account set --subscription "Azure for Students"
```

---

## 17. Login en Docker al ACR (si se vence la sesion)

```powershell
$ACR_USER = az acr credential show --name $ACR_NAME --query username -o tsv
$ACR_PASS = az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv
docker login $ACR_LOGIN_SERVER -u $ACR_USER -p $ACR_PASS
```
