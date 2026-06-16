---
tags:
  - spec
requerimiento: RF-02
---

# RF-02 — Fecha de Alta Seleccionable

**Plan:** [[plan-rf02-fecha-alta-seleccionable]]
**Última actualización:** 2026-05-10
**Historial:**
- 2026-05-10 — Versión inicial

## Resumen

Permitir que el admin pueda elegir la fecha de ingreso ("Miembro desde" / `FechaAlta`) al crear un socio, en vez de usar siempre `DateTime.UtcNow`. Esto es necesario para registrar socios que empezaron antes de la fecha de carga en el sistema.

## Decisiones de diseño

- `FechaAlta` se convierte en un campo **opcional** en `CreateSocioRequest`. Si no se envía, se usa `DateTime.UtcNow` como fallback (comportamiento actual).
- La fecha no puede ser futura — validación en `CreateSocioCommand`.
- `UpdateSocioRequest` también recibe `FechaAlta` opcional para poder corregirla después.
- El cambio de `FechaAlta` en edición **no** regenera cuotas — eso es responsabilidad de RF-07.

## Cambios

### 1. Backend — DTO `CreateSocioRequest`

Agregar campo opcional:

```csharp
DateTime? FechaAlta  // Si null, usa DateTime.UtcNow
```

### 2. Backend — DTO `UpdateSocioRequest`

Agregar campo opcional:

```csharp
DateTime? FechaAlta  // Si null, no cambia la fecha existente
```

### 3. Backend — `CreateSocioCommand`

- Si `request.FechaAlta` tiene valor, validar que no sea futura (`> DateTime.UtcNow`).
- Pasar `request.FechaAlta ?? DateTime.UtcNow` al constructor de `Socio`.

### 4. Backend — `UpdateSocioCommand`

- Si `request.FechaAlta` tiene valor, validar que no sea futura.
- Llamar a un nuevo método `ActualizarFechaAlta(DateTime)` en la entidad `Socio`.

### 5. Dominio — Entidad `Socio`

Agregar método:

```csharp
public void ActualizarFechaAlta(DateTime fechaAlta)
{
    if (fechaAlta > DateTime.UtcNow)
        throw new ArgumentException("La fecha de alta no puede ser futura.");
    FechaAlta = fechaAlta;
}
```

### 6. Frontend — Tipos

Agregar `fechaAlta?: string` a `CreateSocioRequest` y `UpdateSocioRequest` en `types/index.ts`.

### 7. Frontend — Formularios

Agregar input `type="date"` con label "Miembro desde" en:
- `NuevoSocioPage.tsx` — dentro de la sección "Plan y acceso"
- `EditSocioPage.tsx` — mismo lugar, pre-cargado con `socio.fechaAlta`
