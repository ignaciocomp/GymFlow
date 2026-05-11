# RF-02 — Fecha de Alta Seleccionable: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que el admin elija la fecha de ingreso (FechaAlta) al crear o editar un socio, en vez de usar siempre la fecha actual.

**Architecture:** Se agrega campo opcional `FechaAlta` a los DTOs de creación y edición. El backend valida que no sea futura. El frontend agrega un date picker en ambos formularios.

**Tech Stack:** .NET 8, React 18, TypeScript, shadcn/ui.

**Nota:** No commitear nada — el usuario se encarga del git.

---

### Task 1: Backend — Agregar FechaAlta a DTOs y Commands

**Files:**
- Modify: `backend/src/GymFlow.Application/DTOs/CreateSocioRequest.cs`
- Modify: `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`
- Modify: `backend/src/GymFlow.Domain/Entities/Socio.cs`

- [ ] **Step 1: Agregar campo `FechaAlta` a `CreateSocioRequest`**

En `backend/src/GymFlow.Application/DTOs/CreateSocioRequest.cs`, agregar el campo al record:

```csharp
public record CreateSocioRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    TipoDocumento TipoDocumento,
    string? DocumentoIdentidad,
    DateTime? FechaNacimiento,
    List<UnidadAsignacionDto> Unidades,
    bool ConsentimientoInformado,
    DateTime? FechaAlta = null);
```

- [ ] **Step 2: Validar y usar FechaAlta en `CreateSocioCommand`**

En `backend/src/GymFlow.Application/UseCases/Socios/CreateSocioCommand.cs`, en el método `ExecuteAsync`, antes de la creación del `Socio`, agregar la validación:

```csharp
if (request.FechaAlta.HasValue && request.FechaAlta.Value > DateTime.UtcNow)
    throw new ArgumentException("La fecha de alta no puede ser futura.");
```

Y cambiar la línea `fechaAlta: DateTime.UtcNow` por:

```csharp
fechaAlta: request.FechaAlta.HasValue
    ? DateTime.SpecifyKind(request.FechaAlta.Value, DateTimeKind.Utc)
    : DateTime.UtcNow,
```

- [ ] **Step 3: Agregar `ActualizarFechaAlta` a la entidad `Socio`**

En `backend/src/GymFlow.Domain/Entities/Socio.cs`, agregar el método después de `ActualizarDatosSocio`:

```csharp
public void ActualizarFechaAlta(DateTime fechaAlta)
{
    if (fechaAlta > DateTime.UtcNow)
        throw new ArgumentException("La fecha de alta no puede ser futura.", nameof(fechaAlta));
    FechaAlta = fechaAlta;
}
```

- [ ] **Step 4: Agregar `FechaAlta` a `UpdateSocioRequest` y usarlo en `UpdateSocioCommand`**

En `backend/src/GymFlow.Application/DTOs/CreateSocioRequest.cs` (donde está el record `UpdateSocioRequest` — verificar si está en el mismo archivo o buscar con grep), agregar:

El `UpdateSocioRequest` está en `frontend/src/types/index.ts` para el front. En backend, buscar dónde está definido. Si no tiene archivo propio, buscar en los DTOs.

Agregar `DateTime? FechaAlta = null` al record `UpdateSocioRequest`.

En `backend/src/GymFlow.Application/UseCases/Socios/UpdateSocioCommand.cs`, después de `socio.ActualizarDatosSocio(...)` y antes de `socio.UnidadesAsignadas.Clear()`, agregar:

```csharp
if (request.FechaAlta.HasValue)
{
    var nuevaFechaAlta = DateTime.SpecifyKind(request.FechaAlta.Value, DateTimeKind.Utc);
    if (socio.FechaAlta != nuevaFechaAlta)
    {
        cambios["FechaAlta"] = new { anterior = socio.FechaAlta, nuevo = nuevaFechaAlta };
        socio.ActualizarFechaAlta(nuevaFechaAlta);
    }
}
```

- [ ] **Step 5: Verificar que compila**

Run: `dotnet build backend/src/GymFlow.API/`
Expected: Build succeeded.

- [ ] **Step 6: Actualizar tests existentes de `CreateSocioCommand`**

En `backend/tests/GymFlow.Application.Tests/UseCases/CreateSocioCommandTests.cs`, verificar que los tests existentes siguen compilando. Si `CreateSocioRequest` cambió de shape, agregar `FechaAlta: null` a las instancias donde sea necesario.

Run: `dotnet test backend/tests/GymFlow.Application.Tests/ --filter "CreateSocioCommand" -v minimal`
Expected: Todos los tests pasan.

---

### Task 2: Frontend — Agregar date picker de FechaAlta en formularios

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/pages/admin/NuevoSocioPage.tsx`
- Modify: `frontend/src/pages/admin/EditSocioPage.tsx`

- [ ] **Step 1: Agregar `fechaAlta` a los tipos TypeScript**

En `frontend/src/types/index.ts`, agregar campo opcional a `CreateSocioRequest`:

```typescript
export interface CreateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  unidades: { unidadId: string; planId: string | null }[]
  consentimientoInformado: boolean
  fechaAlta?: string | null
}
```

Agregar campo opcional a `UpdateSocioRequest`:

```typescript
export interface UpdateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  unidades: { unidadId: string; planId: string | null }[]
  fechaAlta?: string | null
}
```

- [ ] **Step 2: Agregar date picker en `NuevoSocioPage.tsx`**

En `frontend/src/pages/admin/NuevoSocioPage.tsx`, agregar `fechaAlta: null` al estado inicial del form:

```typescript
const [form, setForm] = useState<CreateSocioRequest>({
  nombre: '', apellido: '', correo: '',
  telefono: null, tipoDocumento: null, documentoIdentidad: null, fechaNacimiento: null,
  unidades: [], consentimientoInformado: false,
  fechaAlta: null,
})
```

Dentro de la sección "Plan y acceso" (`<div className="rounded-xl border border-border bg-card p-6 space-y-5">`), agregar antes del bloque de "Espacio asignado":

```tsx
<div className="space-y-2">
  <Label className="text-muted-foreground">Miembro desde</Label>
  <Input
    type="date"
    value={form.fechaAlta || ''}
    onChange={(e) => setForm({ ...form, fechaAlta: e.target.value || null })}
    max={new Date().toISOString().split('T')[0]}
    className="bg-muted/30 border-border"
  />
  <p className="text-xs text-muted-foreground">
    Si no se selecciona, se usa la fecha de hoy.
  </p>
</div>
```

- [ ] **Step 3: Agregar date picker en `EditSocioPage.tsx`**

En `frontend/src/pages/admin/EditSocioPage.tsx`, agregar `fechaAlta: null` al estado inicial:

```typescript
const [form, setForm] = useState<UpdateSocioRequest>({
  nombre: '', apellido: '', correo: '',
  telefono: null, tipoDocumento: null, documentoIdentidad: null, fechaNacimiento: null,
  unidades: [],
  fechaAlta: null,
})
```

En el `useEffect` que carga el socio, agregar la carga de `fechaAlta`:

```typescript
useEffect(() => {
  if (socio) {
    setForm({
      nombre: socio.nombre,
      apellido: socio.apellido,
      correo: socio.correo,
      telefono: socio.telefono,
      tipoDocumento: socio.tipoDocumento,
      documentoIdentidad: socio.documentoIdentidad,
      fechaNacimiento: socio.fechaNacimiento ? socio.fechaNacimiento.split('T')[0] : null,
      unidades: socio.unidades.map((u) => ({ unidadId: u.id, planId: u.planId })),
      fechaAlta: socio.fechaAlta ? socio.fechaAlta.split('T')[0] : null,
    })
  }
}, [socio])
```

Dentro de la sección "Plan y acceso", agregar el mismo input antes de "Espacio asignado":

```tsx
<div className="space-y-2">
  <Label className="text-muted-foreground">Miembro desde</Label>
  <Input
    type="date"
    value={form.fechaAlta || ''}
    onChange={(e) => setForm({ ...form, fechaAlta: e.target.value || null })}
    max={new Date().toISOString().split('T')[0]}
    className="bg-muted/30 border-border"
  />
</div>
```

- [ ] **Step 4: Verificar que el frontend compila**

Run: `cd frontend && npm run build`
Expected: Build succeeded.
