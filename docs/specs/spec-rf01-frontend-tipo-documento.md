---
tags:
  - spec
requerimiento: RF-01
---

# Spec: Frontend — TipoDocumento en formularios de socio

**Rama:** feature/RF_01
**Plan:** [[plan-rf01-frontend-tipo-documento]]
**Última actualización:** 2026-04-05
**Historial:**
- 2026-04-05 — Versión inicial

## Contexto

El backend ya implementa `TipoDocumento` (CI, Pasaporte, Otro), validación de cédula uruguaya y unicidad de cédula. El frontend aún no expone este campo: solo tiene un input de texto libre para `documentoIdentidad` sin tipo asociado.

## Objetivo

Agregar el selector de `TipoDocumento` en los formularios de alta y edición de socios, y mostrar el tipo en la tabla de socios. Toda validación del documento queda en el backend.

## Archivos afectados

| Archivo | Cambio |
|---|---|
| `frontend/src/types/index.ts` | Agregar tipo `TipoDocumento`, agregarlo a `Socio`, `CreateSocioRequest`, `UpdateSocioRequest` |
| `frontend/src/pages/admin/NuevoSocioPage.tsx` | Reemplazar campo doc por selector + campo condicional |
| `frontend/src/pages/admin/EditSocioPage.tsx` | Ídem, pre-poblando valores existentes |
| `frontend/src/pages/admin/SociosPage.tsx` | Mostrar tipo de documento junto al número en la tabla |

## Diseño de la sección "Documento"

La sección actual "Doc. Identidad (CI)" se reemplaza por dos campos en la misma fila:

```
[ Tipo de Documento * ]     [ Número de documento ]
  CI / Pasaporte / Otro       (aparece al elegir tipo)
```

- `TipoDocumento` es **obligatorio** — no se puede guardar sin elegir.
- El campo de número aparece una vez elegido el tipo.
- Ambos campos van en la misma fila en el grid de 2 columnas que ya existe.

## Normalización y validación del número de documento

- El frontend no valida formato — toda validación (formato de cédula uruguaya, dígito verificador, unicidad) la hace el backend.
- **Solo para CI:** antes de enviar, se eliminan puntos y guiones del valor ingresado (`replace(/[.\-]/g, '')`).
- **Pasaporte / Otro:** se envía el valor tal cual.
- Los errores del backend (cédula inválida, duplicada, etc.) se muestran en el banner de error existente.

## Tipos

```typescript
export type TipoDocumento = 'CI' | 'Pasaporte' | 'Otro'

// Agregar a Socio, CreateSocioRequest y UpdateSocioRequest:
tipoDocumento: TipoDocumento   // requerido en request, presente en response
```

## Tabla de socios (SociosPage)

La columna "Doc. Identidad" pasa a mostrar el tipo junto al número:

```
CI: 12345678
Pasaporte: AB123456
```

Si no tiene documento, queda vacío como ahora.

## Comportamiento en edición (EditSocioPage)

- Al cargar el socio, se pre-poblan `tipoDocumento` y `documentoIdentidad` con los valores del backend.

## Fuera de alcance

- Cualquier validación de formato en frontend (la hace el backend).
- Cambios en el backend (ya está completo).
- Tests unitarios de frontend (no hay tests de componentes en el proyecto).
