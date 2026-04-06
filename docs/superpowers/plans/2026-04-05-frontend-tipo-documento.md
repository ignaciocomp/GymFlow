# Frontend TipoDocumento Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Agregar el selector de TipoDocumento en los formularios de alta y edición de socios, normalizando CI antes de enviar al backend, y mostrando el tipo en la tabla.

**Architecture:** Se agregan los tipos en `types/index.ts`, se modifica la sección de documento en `NuevoSocioPage` y `EditSocioPage` para incluir un `<Select>` de tipo y un campo de número condicional, y se actualiza la celda de documento en `SociosPage`. Toda validación de formato queda en el backend; el frontend solo normaliza CI (quita puntos y guiones) antes de enviar.

**Tech Stack:** React 18, TypeScript, shadcn/ui (`Select`, `Input`, `Label`), TanStack React Query, Axios.

---

### Task 1: Agregar TipoDocumento a los tipos TypeScript

**Files:**
- Modify: `frontend/src/types/index.ts`

- [ ] **Step 1: Agregar el tipo y actualizar las interfaces**

Reemplazar el contenido de `frontend/src/types/index.ts`:

```typescript
export interface Unidad {
  id: string
  nombre: string
  direccion: string
}

export interface Plan {
  id: string
  nombre: string
  precio: number
  descripcion: string
  unidadId: string
}

export type TipoDocumento = 'CI' | 'Pasaporte' | 'Otro'

export interface Socio {
  id: string
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  fechaAlta: string
  estaActivo: boolean
  planId: string | null
  planNombre: string | null
  unidades: Unidad[]
}

export interface CreateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  planId: string | null
  unidadIds: string[]
  consentimientoInformado: boolean
}

export interface UpdateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  planId: string | null
  unidadIds: string[]
}

export interface DeleteSocioRequest {
  motivo: string | null
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/types/index.ts
git commit -m "feat: agregar TipoDocumento a tipos TypeScript"
```

---

### Task 2: Actualizar NuevoSocioPage con selector de TipoDocumento

**Files:**
- Modify: `frontend/src/pages/admin/NuevoSocioPage.tsx`

- [ ] **Step 1: Actualizar el estado inicial del formulario**

En `NuevoSocioPage.tsx`, agregar `tipoDocumento: null` al estado inicial del form:

```typescript
const [form, setForm] = useState<CreateSocioRequest>({
  nombre: '', apellido: '', correo: '',
  telefono: null, tipoDocumento: null, documentoIdentidad: null, fechaNacimiento: null,
  planId: null, unidadIds: [], consentimientoInformado: false,
})
```

- [ ] **Step 2: Agregar helper de normalización**

Antes del `return`, agregar la función que normaliza CI antes de enviar:

```typescript
const buildPayload = (): CreateSocioRequest => ({
  ...form,
  documentoIdentidad: form.tipoDocumento === 'CI' && form.documentoIdentidad
    ? form.documentoIdentidad.replace(/[.\-]/g, '')
    : form.documentoIdentidad,
})
```

- [ ] **Step 3: Usar buildPayload en handleSubmit**

Cambiar `createMutation.mutate(form)` por `createMutation.mutate(buildPayload())`:

```typescript
const handleSubmit = (e: React.FormEvent) => {
  e.preventDefault()
  setError(null)
  if (!form.nombre.trim() || !form.apellido.trim() || !form.correo.trim()) {
    setError('Nombre, apellido y correo son obligatorios.')
    return
  }
  if (!form.tipoDocumento) {
    setError('El tipo de documento es obligatorio.')
    return
  }
  if (form.unidadIds.length === 0) {
    setError('Debe seleccionar al menos una unidad.')
    return
  }
  if (!form.consentimientoInformado) {
    setError('El consentimiento informado es obligatorio (Ley 18.331).')
    return
  }
  createMutation.mutate(buildPayload())
}
```

- [ ] **Step 4: Reemplazar la sección de documento en el JSX**

Localizar el bloque del grid de 2 columnas que contiene Teléfono y Doc. Identidad:

```tsx
<div className="grid grid-cols-2 gap-4">
  <div className="space-y-2">
    <Label className="text-muted-foreground">Teléfono</Label>
    <Input
      value={form.telefono || ''}
      onChange={(e) => setForm({ ...form, telefono: e.target.value || null })}
      placeholder="099 123 456"
      className="bg-muted/30 border-border"
    />
  </div>
  <div className="space-y-2">
    <Label className="text-muted-foreground">Doc. Identidad (CI)</Label>
    <Input
      value={form.documentoIdentidad || ''}
      onChange={(e) => setForm({ ...form, documentoIdentidad: e.target.value || null })}
      placeholder="1.234.567-8"
      className="bg-muted/30 border-border"
    />
  </div>
</div>
```

Reemplazarlo con:

```tsx
<div className="grid grid-cols-2 gap-4">
  <div className="space-y-2">
    <Label className="text-muted-foreground">Teléfono</Label>
    <Input
      value={form.telefono || ''}
      onChange={(e) => setForm({ ...form, telefono: e.target.value || null })}
      placeholder="099 123 456"
      className="bg-muted/30 border-border"
    />
  </div>
  <div className="space-y-2">
    <Label className="text-muted-foreground">Tipo de Documento *</Label>
    <Select
      value={form.tipoDocumento || ''}
      onValueChange={(val) => setForm({
        ...form,
        tipoDocumento: val as import('@/types').TipoDocumento,
        documentoIdentidad: null,
      })}
    >
      <SelectTrigger className="bg-muted/30 border-border">
        <SelectValue placeholder="Seleccionar tipo" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="CI">Cédula de Identidad</SelectItem>
        <SelectItem value="Pasaporte">Pasaporte</SelectItem>
        <SelectItem value="Otro">Otro</SelectItem>
      </SelectContent>
    </Select>
  </div>
</div>

{form.tipoDocumento && (
  <div className="space-y-2">
    <Label className="text-muted-foreground">
      {form.tipoDocumento === 'CI' ? 'Número de cédula' : 'Número de documento'}
    </Label>
    <Input
      value={form.documentoIdentidad || ''}
      onChange={(e) => setForm({ ...form, documentoIdentidad: e.target.value || null })}
      placeholder={form.tipoDocumento === 'CI' ? '12345678' : ''}
      className="bg-muted/30 border-border"
    />
  </div>
)}
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/admin/NuevoSocioPage.tsx
git commit -m "feat: agregar selector TipoDocumento en NuevoSocioPage"
```

---

### Task 3: Actualizar EditSocioPage con selector de TipoDocumento

**Files:**
- Modify: `frontend/src/pages/admin/EditSocioPage.tsx`

- [ ] **Step 1: Agregar tipoDocumento al estado inicial y al useEffect**

Estado inicial (agregar `tipoDocumento: null`):

```typescript
const [form, setForm] = useState<UpdateSocioRequest>({
  nombre: '', apellido: '', correo: '',
  telefono: null, tipoDocumento: null, documentoIdentidad: null, fechaNacimiento: null,
  planId: null, unidadIds: [],
})
```

Dentro del `useEffect` que popula el form cuando carga el socio, agregar `tipoDocumento`:

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
      planId: socio.planId,
      unidadIds: socio.unidades.map((u) => u.id),
    })
  }
}, [socio])
```

- [ ] **Step 2: Agregar helper de normalización y validación en handleSubmit**

Antes del `return`, agregar:

```typescript
const buildPayload = (): UpdateSocioRequest => ({
  ...form,
  documentoIdentidad: form.tipoDocumento === 'CI' && form.documentoIdentidad
    ? form.documentoIdentidad.replace(/[.\-]/g, '')
    : form.documentoIdentidad,
})
```

Actualizar `handleSubmit` para validar `tipoDocumento` y usar `buildPayload()`:

```typescript
const handleSubmit = (e: React.FormEvent) => {
  e.preventDefault()
  setError(null)
  if (!form.nombre.trim() || !form.apellido.trim() || !form.correo.trim()) {
    setError('Nombre, apellido y correo son obligatorios.')
    return
  }
  if (!form.tipoDocumento) {
    setError('El tipo de documento es obligatorio.')
    return
  }
  if (form.unidadIds.length === 0) {
    setError('Debe seleccionar al menos una unidad.')
    return
  }
  updateMutation.mutate(buildPayload())
}
```

- [ ] **Step 3: Reemplazar la sección de documento en el JSX**

Localizar el bloque del grid de 2 columnas que contiene Teléfono y Doc. Identidad (líneas 209-228 del archivo original):

```tsx
<div className="grid grid-cols-2 gap-4">
  <div className="space-y-2">
    <Label className="text-muted-foreground">Telefono</Label>
    <Input
      value={form.telefono || ''}
      onChange={(e) => setForm({ ...form, telefono: e.target.value || null })}
      placeholder="099 123 456"
      className="bg-muted/30 border-border"
    />
  </div>
  <div className="space-y-2">
    <Label className="text-muted-foreground">Doc. Identidad (CI)</Label>
    <Input
      value={form.documentoIdentidad || ''}
      onChange={(e) => setForm({ ...form, documentoIdentidad: e.target.value || null })}
      placeholder="1.234.567-8"
      className="bg-muted/30 border-border"
    />
  </div>
</div>
```

Reemplazarlo con:

```tsx
<div className="grid grid-cols-2 gap-4">
  <div className="space-y-2">
    <Label className="text-muted-foreground">Telefono</Label>
    <Input
      value={form.telefono || ''}
      onChange={(e) => setForm({ ...form, telefono: e.target.value || null })}
      placeholder="099 123 456"
      className="bg-muted/30 border-border"
    />
  </div>
  <div className="space-y-2">
    <Label className="text-muted-foreground">Tipo de Documento *</Label>
    <Select
      value={form.tipoDocumento || ''}
      onValueChange={(val) => setForm({
        ...form,
        tipoDocumento: val as import('@/types').TipoDocumento,
        documentoIdentidad: null,
      })}
    >
      <SelectTrigger className="bg-muted/30 border-border">
        <SelectValue placeholder="Seleccionar tipo" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="CI">Cédula de Identidad</SelectItem>
        <SelectItem value="Pasaporte">Pasaporte</SelectItem>
        <SelectItem value="Otro">Otro</SelectItem>
      </SelectContent>
    </Select>
  </div>
</div>

{form.tipoDocumento && (
  <div className="space-y-2">
    <Label className="text-muted-foreground">
      {form.tipoDocumento === 'CI' ? 'Número de cédula' : 'Número de documento'}
    </Label>
    <Input
      value={form.documentoIdentidad || ''}
      onChange={(e) => setForm({ ...form, documentoIdentidad: e.target.value || null })}
      placeholder={form.tipoDocumento === 'CI' ? '12345678' : ''}
      className="bg-muted/30 border-border"
    />
  </div>
)}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/pages/admin/EditSocioPage.tsx
git commit -m "feat: agregar selector TipoDocumento en EditSocioPage"
```

---

### Task 4: Actualizar SociosPage para mostrar TipoDocumento en tabla

**Files:**
- Modify: `frontend/src/pages/admin/SociosPage.tsx`

- [ ] **Step 1: Actualizar la celda de documento en la tabla**

Localizar la celda:

```tsx
<TableCell className="text-muted-foreground">
  {socio.documentoIdentidad || '-'}
</TableCell>
```

Reemplazar por:

```tsx
<TableCell className="text-muted-foreground">
  {socio.tipoDocumento && socio.documentoIdentidad
    ? `${socio.tipoDocumento}: ${socio.documentoIdentidad}`
    : socio.documentoIdentidad || '-'}
</TableCell>
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/pages/admin/SociosPage.tsx
git commit -m "feat: mostrar TipoDocumento en tabla de socios"
```

---

### Task 5: Push y verificación

- [ ] **Step 1: Push a la rama**

```bash
git push origin feature/RF_01
```

- [ ] **Step 2: Verificar en el navegador**

Levantar el frontend (`npm run dev` dentro de `frontend/`) y verificar:

1. **NuevoSocioPage** (`/admin/socios/nuevo`):
   - El selector "Tipo de Documento" aparece en la fila junto a Teléfono
   - Al elegir un tipo, aparece el campo de número debajo
   - El label del campo cambia según el tipo (CI / documento)
   - Sin elegir tipo, el formulario no se envía y muestra error
   - Con CI, el valor se normaliza antes de enviar (sin puntos ni guiones)

2. **EditSocioPage** (`/admin/socios/:id/editar`):
   - Al cargar un socio existente, el selector muestra el tipo guardado
   - El campo de número muestra el documento existente
   - La edición funciona igual que el alta

3. **SociosPage** (`/admin/socios`):
   - La columna Doc. Identidad muestra `CI: 12345678` o `Pasaporte: AB123`
   - Si no tiene documento, muestra `-`
