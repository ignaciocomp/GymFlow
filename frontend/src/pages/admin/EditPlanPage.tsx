import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { planesApi, unidadesApi } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select, SelectContent, SelectItem, SelectTrigger,
} from '@/components/ui/select'
import { Pencil, ArrowLeft } from 'lucide-react'

export default function EditPlanPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [form, setForm] = useState({
    nombre: '',
    precio: '',
    descripcion: null as string | null,
  })
  const [error, setError] = useState<string | null>(null)

  const { data: plan, isLoading: isLoadingPlan, isError } = useQuery({
    queryKey: ['plan', id],
    queryFn: () => planesApi.getById(id!),
    enabled: !!id,
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  useEffect(() => {
    if (plan) {
      setForm({
        nombre: plan.nombre,
        precio: String(plan.precio),
        descripcion: plan.descripcion || null,
      })
    }
  }, [plan])

  const updateMutation = useMutation({
    mutationFn: (request: { nombre: string; precio: number; descripcion: string | null }) => planesApi.update(id!, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['planes'] })
      queryClient.invalidateQueries({ queryKey: ['plan', id] })
      navigate('/admin/planes')
    },
    onError: (err: unknown) => {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { error?: string } } }
        setError(axiosErr.response?.data?.error || 'Error al actualizar el plan.')
      } else {
        setError('Error al actualizar el plan.')
      }
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!form.nombre.trim()) {
      setError('El nombre es obligatorio.')
      return
    }
    const precio = parseFloat(form.precio)
    if (isNaN(precio) || precio < 0) {
      setError('El precio debe ser un número válido no negativo.')
      return
    }
    updateMutation.mutate({ ...form, precio, descripcion: form.descripcion || null })
  }

  if (isLoadingPlan) {
    return (
      <div className="mx-auto max-w-2xl space-y-6">
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/admin/planes')}
            className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
          >
            <ArrowLeft className="h-5 w-5" />
          </button>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Pencil className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-foreground">Editar Plan</h1>
              <p className="text-sm text-muted-foreground">Cargando datos del plan...</p>
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-border bg-card p-6">
          <p className="text-center text-muted-foreground">Cargando...</p>
        </div>
      </div>
    )
  }

  if (isError || (!isLoadingPlan && !plan)) {
    return (
      <div className="mx-auto max-w-2xl space-y-6">
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/admin/planes')}
            className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
          >
            <ArrowLeft className="h-5 w-5" />
          </button>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Plan no encontrado</h1>
        </div>
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6">
          <p className="text-sm text-destructive">No se pudo cargar el plan. Es posible que haya sido eliminado.</p>
          <Button variant="outline" className="mt-4 cursor-pointer" onClick={() => navigate('/admin/planes')}>
            Volver a Planes
          </Button>
        </div>
      </div>
    )
  }

  const unidadNombre = unidades?.find((u) => u.id === plan?.unidadId)?.nombre || plan?.unidadNombre || ''

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate('/admin/planes')}
          className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
        >
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Pencil className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Editar Plan</h1>
            <p className="text-sm text-muted-foreground">
              Los campos marcados con * son obligatorios
            </p>
          </div>
        </div>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="rounded-xl border border-border bg-card p-6 space-y-5">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
            Datos del Plan
          </h2>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Nombre *</Label>
            <Input
              value={form.nombre}
              onChange={(e) => setForm({ ...form, nombre: e.target.value })}
              placeholder="Nombre del plan"
              className="bg-muted/30 border-border"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Unidad</Label>
            <Select value={plan?.unidadId || ''} disabled>
              <SelectTrigger className="bg-muted/30 border-border opacity-60">
                <span className="flex flex-1 text-left">{unidadNombre}</span>
              </SelectTrigger>
              <SelectContent>
                {unidades?.map((u) => (
                  <SelectItem key={u.id} value={u.id}>{u.nombre}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">La unidad no se puede cambiar una vez creado el plan.</p>
          </div>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Precio *</Label>
            <Input
              type="number"
              min={0}
              step="0.01"
              value={form.precio}
              onChange={(e) => setForm({ ...form, precio: e.target.value })}
              placeholder="0.00"
              className="bg-muted/30 border-border"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Descripcion</Label>
            <Textarea
              value={form.descripcion || ''}
              onChange={(e) => setForm({ ...form, descripcion: e.target.value || null })}
              placeholder="Descripcion del plan (opcional)"
              className="bg-muted/30 border-border"
              rows={3}
            />
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
            onClick={() => navigate('/admin/planes')}
            className="cursor-pointer"
          >
            Cancelar
          </Button>
        </div>
      </form>
    </div>
  )
}
