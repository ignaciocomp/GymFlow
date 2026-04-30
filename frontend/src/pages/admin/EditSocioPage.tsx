import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { sociosApi, unidadesApi, planesApi } from '@/services/api'
import type { UpdateSocioRequest, TipoDocumento } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { Pencil, ArrowLeft } from 'lucide-react'

function UnitPlanDropdown({
  unidadId,
  unidadNombre,
  selectedPlanId,
  onPlanChange,
}: {
  unidadId: string
  unidadNombre: string
  selectedPlanId: string | null
  onPlanChange: (planId: string | null) => void
}) {
  const { data: planes } = useQuery({
    queryKey: ['planes', unidadId],
    queryFn: () => planesApi.getAll(unidadId),
  })

  const selectedPlan = planes?.find(p => p.id === selectedPlanId)

  return (
    <div className="space-y-2">
      <Label className="text-muted-foreground">{unidadNombre} — Plan</Label>
      <Select
        value={selectedPlanId || 'none'}
        onValueChange={(val) => onPlanChange(!val || val === 'none' ? null : val)}
      >
        <SelectTrigger className="bg-muted/30 border-border">
          <SelectValue>
            {selectedPlan ? `${selectedPlan.nombre} — $${selectedPlan.precio}` : 'Sin plan'}
          </SelectValue>
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
  )
}

export default function EditSocioPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [form, setForm] = useState<UpdateSocioRequest>({
    nombre: '', apellido: '', correo: '',
    telefono: null, tipoDocumento: null, documentoIdentidad: null, fechaNacimiento: null,
    unidades: [],
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

  const buildPayload = (): UpdateSocioRequest => ({
    ...form,
    documentoIdentidad: form.tipoDocumento === 'CI' && form.documentoIdentidad
      ? form.documentoIdentidad.replace(/[.\-]/g, '')
      : form.documentoIdentidad,
  })

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
    if (form.unidades.length === 0) {
      setError('Debe seleccionar al menos una unidad.')
      return
    }
    updateMutation.mutate(buildPayload())
  }

  const toggleUnidad = (unidadId: string) => {
    setForm((prev) => {
      const exists = prev.unidades.some(u => u.unidadId === unidadId)
      return {
        ...prev,
        unidades: exists
          ? prev.unidades.filter(u => u.unidadId !== unidadId)
          : [...prev.unidades, { unidadId, planId: null }],
      }
    })
  }

  const updateUnitPlan = (unidadId: string, planId: string | null) => {
    setForm((prev) => ({
      ...prev,
      unidades: prev.unidades.map(u =>
        u.unidadId === unidadId ? { ...u, planId } : u
      ),
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
              <h1 className="text-2xl font-bold tracking-tight text-foreground">Editar socio</h1>
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
            Volver a socios
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
            Información personal
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
            <Label className="text-muted-foreground">Correo electrónico *</Label>
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
              <Label className="text-muted-foreground">Teléfono</Label>
              <Input
                value={form.telefono || ''}
                onChange={(e) => setForm({ ...form, telefono: e.target.value || null })}
                placeholder="099 123 456"
                className="bg-muted/30 border-border"
              />
            </div>
            <div className="space-y-2">
              <Label className="text-muted-foreground">Tipo de documento *</Label>
              <Select
                value={form.tipoDocumento || ''}
                onValueChange={(val) => setForm({
                  ...form,
                  tipoDocumento: val as TipoDocumento,
                  documentoIdentidad: null,
                })}
              >
                <SelectTrigger className="bg-muted/30 border-border">
                  <SelectValue placeholder="Seleccionar tipo" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="CI">Cédula de identidad</SelectItem>
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
            Plan y acceso
          </h2>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Espacio asignado *</Label>
            <div className="flex gap-4">
              {unidades?.map((u) => (
                <label
                  key={u.id}
                  className={`flex items-center gap-2.5 rounded-lg border px-4 py-3 cursor-pointer transition-colors ${
                    form.unidades.some(su => su.unidadId === u.id)
                      ? 'border-primary bg-primary/5 text-primary'
                      : 'border-border text-muted-foreground hover:border-primary/50'
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={form.unidades.some(su => su.unidadId === u.id)}
                    onChange={() => toggleUnidad(u.id)}
                    className="sr-only"
                  />
                  <span className="text-sm font-medium">{u.nombre}</span>
                </label>
              ))}
            </div>
          </div>

          {form.unidades.length > 0 && (
            <div className="space-y-4">
              {form.unidades.map((su) => {
                const unidad = unidades?.find(u => u.id === su.unidadId)
                return (
                  <UnitPlanDropdown
                    key={su.unidadId}
                    unidadId={su.unidadId}
                    unidadNombre={unidad?.nombre || ''}
                    selectedPlanId={su.planId}
                    onPlanChange={(planId) => updateUnitPlan(su.unidadId, planId)}
                  />
                )
              })}
            </div>
          )}
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
            {updateMutation.isPending ? 'Guardando...' : 'Guardar cambios'}
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
