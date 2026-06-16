import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { clasesApi } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { BookOpen, ArrowLeft } from 'lucide-react'

export default function EditClasePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [form, setForm] = useState({
    nombre: '',
    descripcion: null as string | null,
    capacidadMaxima: '',
    duracionMinutos: '',
    instructor: '',
  })
  const [error, setError] = useState<string | null>(null)

  const { data: clase, isLoading } = useQuery({
    queryKey: ['clase', id],
    queryFn: () => clasesApi.getById(id!),
    enabled: !!id,
  })

  useEffect(() => {
    if (clase) {
      setForm({
        nombre: clase.nombre,
        descripcion: clase.descripcion || null,
        capacidadMaxima: String(clase.capacidadMaxima),
        duracionMinutos: String(clase.duracionMinutos),
        instructor: clase.instructor,
      })
    }
  }, [clase])

  const updateMutation = useMutation({
    mutationFn: (data: { nombre: string; descripcion: string | null; capacidadMaxima: number; duracionMinutos: number; instructor: string }) =>
      clasesApi.update(id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clases'] })
      queryClient.invalidateQueries({ queryKey: ['clase', id] })
      navigate('/admin/clases')
    },
    onError: (err: unknown) => {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { error?: string } } }
        setError(axiosErr.response?.data?.error || 'Error al actualizar la clase.')
      } else {
        setError('Error al actualizar la clase.')
      }
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!form.nombre.trim()) { setError('El nombre es obligatorio.'); return }
    if (!form.instructor.trim()) { setError('El instructor es obligatorio.'); return }
    const capacidad = parseInt(form.capacidadMaxima)
    if (isNaN(capacidad) || capacidad <= 0) { setError('La capacidad debe ser un numero mayor a 0.'); return }
    const duracion = parseInt(form.duracionMinutos)
    if (isNaN(duracion) || duracion <= 0) { setError('La duracion debe ser un numero mayor a 0.'); return }

    updateMutation.mutate({
      nombre: form.nombre,
      descripcion: form.descripcion || null,
      capacidadMaxima: capacidad,
      duracionMinutos: duracion,
      instructor: form.instructor,
    })
  }

  if (isLoading) {
    return <div className="text-center text-muted-foreground py-12">Cargando...</div>
  }

  if (!clase) {
    return <div className="text-center text-muted-foreground py-12">Clase no encontrada.</div>
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate('/admin/clases')}
          className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
        >
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <BookOpen className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Editar clase</h1>
            <p className="text-sm text-muted-foreground">{clase.unidadNombre}</p>
          </div>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="rounded-xl border border-border bg-card p-6 space-y-5">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
            Datos de la clase
          </h2>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Nombre *</Label>
            <Input
              value={form.nombre}
              onChange={(e) => setForm({ ...form, nombre: e.target.value })}
              placeholder="Nombre de la clase"
              className="bg-muted/30 border-border"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Instructor *</Label>
            <Input
              value={form.instructor}
              onChange={(e) => setForm({ ...form, instructor: e.target.value })}
              placeholder="Nombre del instructor"
              className="bg-muted/30 border-border"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label className="text-muted-foreground">Capacidad maxima *</Label>
              <Input
                type="number"
                min={1}
                value={form.capacidadMaxima}
                onChange={(e) => setForm({ ...form, capacidadMaxima: e.target.value })}
                placeholder="20"
                className="bg-muted/30 border-border"
              />
            </div>
            <div className="space-y-2">
              <Label className="text-muted-foreground">Duracion (minutos) *</Label>
              <Input
                type="number"
                min={1}
                value={form.duracionMinutos}
                onChange={(e) => setForm({ ...form, duracionMinutos: e.target.value })}
                placeholder="60"
                className="bg-muted/30 border-border"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label className="text-muted-foreground">Descripcion</Label>
            <Textarea
              value={form.descripcion || ''}
              onChange={(e) => setForm({ ...form, descripcion: e.target.value || null })}
              placeholder="Descripcion de la clase (opcional)"
              className="bg-muted/30 border-border"
              rows={3}
            />
          </div>
        </div>

        {error && (
          <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
            <p className="text-sm text-destructive">{error}</p>
          </div>
        )}

        <div className="flex gap-3">
          <Button type="submit" disabled={updateMutation.isPending} className="cursor-pointer gap-2">
            <BookOpen className="h-4 w-4" />
            {updateMutation.isPending ? 'Guardando...' : 'Guardar cambios'}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate('/admin/clases')}
            className="cursor-pointer"
          >
            Cancelar
          </Button>
        </div>
      </form>
    </div>
  )
}
