import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { sociosApi, unidadesApi, planesApi } from '@/services/api'
import type { UpdateSocioRequest } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { Pencil, ArrowLeft } from 'lucide-react'

export default function EditSocioPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [form, setForm] = useState<UpdateSocioRequest>({
    nombre: '', apellido: '', correo: '',
    telefono: null, documentoIdentidad: null, fechaNacimiento: null,
    planId: null, unidadIds: [],
  })
  const [error, setError] = useState<string | null>(null)

  const { data: socio, isLoading: isLoadingSocio, isError } = useQuery({
    queryKey: ['socio', id],
    queryFn: () => sociosApi.getById(id!),
    enabled: !!id,
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const { data: planes } = useQuery({
    queryKey: ['planes'],
    queryFn: () => planesApi.getAll(),
  })

  useEffect(() => {
    if (socio) {
      setForm({
        nombre: socio.nombre,
        apellido: socio.apellido,
        correo: socio.correo,
        telefono: socio.telefono,
        documentoIdentidad: socio.documentoIdentidad,
        fechaNacimiento: socio.fechaNacimiento ? socio.fechaNacimiento.split('T')[0] : null,
        planId: socio.planId,
        unidadIds: socio.unidades.map((u) => u.id),
      })
    }
  }, [socio])

  const updateMutation = useMutation({
    mutationFn: (request: UpdateSocioRequest) => sociosApi.update(id!, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['socios'] })
      queryClient.invalidateQueries({ queryKey: ['socio', id] })
      navigate('/admin/socios')
    },
    onError: (err: unknown) => {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { error?: string } } }
        setError(axiosErr.response?.data?.error || 'Error al actualizar el socio.')
      } else {
        setError('Error al actualizar el socio.')
      }
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!form.nombre.trim() || !form.apellido.trim() || !form.correo.trim()) {
      setError('Nombre, apellido y correo son obligatorios.')
      return
    }
    if (form.unidadIds.length === 0) {
      setError('Debe seleccionar al menos una unidad.')
      return
    }
    updateMutation.mutate(form)
  }

  const toggleUnidad = (unidadId: string) => {
    setForm((prev) => ({
      ...prev,
      unidadIds: prev.unidadIds.includes(unidadId)
        ? prev.unidadIds.filter((uid) => uid !== unidadId)
        : [...prev.unidadIds, unidadId],
    }))
  }

  if (isLoadingSocio) {
    return (
      <div className="mx-auto max-w-2xl space-y-6">
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/admin/socios')}
            className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
          >
            <ArrowLeft className="h-5 w-5" />
          </button>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Pencil className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-foreground">Editar Socio</h1>
              <p className="text-sm text-muted-foreground">Cargando datos del socio...</p>
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-border bg-card p-6">
          <p className="text-center text-muted-foreground">Cargando...</p>
        </div>
      </div>
    )
  }

  if (isError || (!isLoadingSocio && !socio)) {
    return (
      <div className="mx-auto max-w-2xl space-y-6">
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/admin/socios')}
            className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
          >
            <ArrowLeft className="h-5 w-5" />
          </button>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Socio no encontrado</h1>
        </div>
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6">
          <p className="text-sm text-destructive">No se pudo cargar el socio. Es posible que haya sido eliminado.</p>
          <Button variant="outline" className="mt-4 cursor-pointer" onClick={() => navigate('/admin/socios')}>
            Volver a Socios
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate('/admin/socios')}
          className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
        >
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Pencil className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Editar Socio</h1>
            <p className="text-sm text-muted-foreground">
              Los campos marcados con * son obligatorios
            </p>
          </div>
        </div>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Informacion Personal */}
        <div className="rounded-xl border border-border bg-card p-6 space-y-5">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
            Informacion Personal
          </h2>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label className="text-muted-foreground">Nombre *</Label>
              <Input
                value={form.nombre}
                onChange={(e) => setForm({ ...form, nombre: e.target.value })}
                placeholder="Nombre"
                className="bg-muted/30 border-border"
              />
            </div>
            <div className="space-y-2">
              <Label className="text-muted-foreground">Apellido *</Label>
              <Input
                value={form.apellido}
                onChange={(e) => setForm({ ...form, apellido: e.target.value })}
                placeholder="Apellido"
                className="bg-muted/30 border-border"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Correo electronico *</Label>
            <Input
              type="email"
              value={form.correo}
              onChange={(e) => setForm({ ...form, correo: e.target.value })}
              placeholder="correo@ejemplo.com"
              className="bg-muted/30 border-border"
            />
          </div>

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

          <div className="space-y-2">
            <Label className="text-muted-foreground">Fecha de nacimiento</Label>
            <Input
              type="date"
              value={form.fechaNacimiento || ''}
              onChange={(e) => setForm({ ...form, fechaNacimiento: e.target.value || null })}
              className="bg-muted/30 border-border"
            />
          </div>
        </div>

        {/* Plan y Acceso */}
        <div className="rounded-xl border border-border bg-card p-6 space-y-5">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
            Plan y Acceso
          </h2>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Espacio asignado *</Label>
            <div className="flex gap-4">
              {unidades?.map((u) => (
                <label
                  key={u.id}
                  className={`flex items-center gap-2.5 rounded-lg border px-4 py-3 cursor-pointer transition-colors ${
                    form.unidadIds.includes(u.id)
                      ? 'border-primary bg-primary/5 text-primary'
                      : 'border-border text-muted-foreground hover:border-primary/50'
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={form.unidadIds.includes(u.id)}
                    onChange={() => toggleUnidad(u.id)}
                    className="sr-only"
                  />
                  <span className="text-sm font-medium">{u.nombre}</span>
                </label>
              ))}
            </div>
          </div>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Plan</Label>
            <Select
              value={form.planId || 'none'}
              onValueChange={(val) => setForm({ ...form, planId: !val || val === 'none' ? null : val })}
            >
              <SelectTrigger className="bg-muted/30 border-border">
                <SelectValue placeholder="Seleccionar plan" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">Sin plan</SelectItem>
                {planes?.map((p) => (
                  <SelectItem key={p.id} value={p.id}>
                    {p.nombre} — ${p.precio}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Error */}
        {error && (
          <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
            <p className="text-sm text-destructive">{error}</p>
          </div>
        )}

        {/* Actions */}
        <div className="flex gap-3">
          <Button type="submit" disabled={updateMutation.isPending} className="cursor-pointer gap-2">
            <Pencil className="h-4 w-4" />
            {updateMutation.isPending ? 'Guardando...' : 'Guardar Cambios'}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate('/admin/socios')}
            className="cursor-pointer"
          >
            Cancelar
          </Button>
        </div>
      </form>
    </div>
  )
}
