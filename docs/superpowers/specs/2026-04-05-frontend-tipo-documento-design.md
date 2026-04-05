# Spec: Frontend — TipoDocumento en formularios de socio

**Fecha:** 2026-04-05
**Rama:** feature/RF_01

## Contexto

El backend ya implementa `TipoDocumento` (CI, Pasaporte, Otro), validación de cédula uruguaya y unicidad de cédula. El frontend aún no expone este campo: solo tiene un input de texto libre para `documentoIdentidad` sin tipo asociado.

## Objetivo

Agregar el selector de `TipoDocumento` y la lógica de validación correspondiente en los formularios de alta y edición de socios, y mostrar el tipo en la tabla de socios.

## Archivos afectados

| Archivo | Cambio |
|---|---|
| `frontend/src/types/index.ts` | Agregar tipo `TipoDocumento`, agregarlo a `Socio`, `CreateSocioRequest`, `UpdateSocioRequest` |
| `frontend/src/pages/admin/NuevoSocioPage.tsx` | Reemplazar campo doc por selector + campo condicional + validación |
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

## Validación del número de documento

- **CI:** antes de enviar, se eliminan puntos y guiones del valor ingresado. El resultado debe tener entre 7 y 8 dígitos numéricos. Si no cumple, se muestra error inline sin llegar al backend.
- **Pasaporte / Otro:** sin validación de formato. Se envía el valor tal cual.
- El valor enviado al backend es siempre el normalizado (sin puntos ni guiones para CI).
- Si el backend rechaza por cédula duplicada (`InvalidOperationException`), el error se muestra en el banner de error existente.

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
- La validación aplica igual que en alta.

## Fuera de alcance

- Validación del dígito verificador en frontend (la hace el backend).
- Cambios en el backend (ya está completo).
- Tests unitarios de frontend (no hay tests de componentes en el proyecto).
