import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { horariosApi, clasesApi, unidadesApi } from '@/services/api'
import { Button } from '@/components/ui/button'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { CalendarDays, Plus, Trash2, Pencil } from 'lucide-react'
import type { HorarioClase, DiaSemana, UpdateHorarioClaseRequest } from '@/types'

const DIAS: DiaSemana[] = ['Lunes', 'Martes', 'Miercoles', 'Jueves', 'Viernes', 'Sabado', 'Domingo']
const DIAS_LABEL: Record<DiaSemana, string> = {
  Lunes: 'Lunes',
  Martes: 'Martes',
  Miercoles: 'Miércoles',
  Jueves: 'Jueves',
  Viernes: 'Viernes',
  Sabado: 'Sábado',
  Domingo: 'Domingo',
}

const HORAS = Array.from({ length: 16 }, (_, i) => {
  const h = i + 7 // 07:00 a 22:00
  return `${h.toString().padStart(2, '0')}:00`
})

function getColorForClase(claseId: string) {
  const colors = [
    'bg-blue-500/20 border-blue-500/40 text-blue-200',
    'bg-emerald-500/20 border-emerald-500/40 text-emerald-200',
    'bg-violet-500/20 border-violet-500/40 text-violet-200',
    'bg-amber-500/20 border-amber-500/40 text-amber-200',
    'bg-rose-500/20 border-rose-500/40 text-rose-200',
    'bg-cyan-500/20 border-cyan-500/40 text-cyan-200',
    'bg-pink-500/20 border-pink-500/40 text-pink-200',
    'bg-teal-500/20 border-teal-500/40 text-teal-200',
  ]
  let hash = 0
  for (let i = 0; i < claseId.length; i++) {
    hash = claseId.charCodeAt(i) + ((hash << 5) - hash)
  }
  return colors[Math.abs(hash) % colors.length]
}

function timeToMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number)
  return h * 60 + m
}

function clustersDeSolapamiento(horarios: HorarioClase[]): HorarioClase[][] {
  const ordenados = [...horarios].sort((a, b) => a.horaInicio.localeCompare(b.horaInicio))
  const clusters: HorarioClase[][] = []
  for (const h of ordenados) {
    const ultimo = clusters[clusters.length - 1]
    const solapa = ultimo?.some(x => x.horaInicio < h.horaFin && h.horaInicio < x.horaFin)
    if (solapa) ultimo.push(h)
    else clusters.push([h])
  }
  return clusters
}

export default function HorariosPage() {
  const queryClient = useQueryClient()
  const [unidadFilter, setUnidadFilter] = useState<string>('')
  const [createDialog, setCreateDialog] = useState(false)
  const [editDialog, setEditDialog] = useState<HorarioClase | null>(null)
  const [deleteDialog, setDeleteDialog] = useState<HorarioClase | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  // Form state
  const [formClaseId, setFormClaseId] = useState<string | null>('')
  const [formDias, setFormDias] = useState<DiaSemana[]>(['Lunes'])
  const [formInicio, setFormInicio] = useState('08:00')
  const [formFin, setFormFin] = useState('09:00')
  const [formSala, setFormSala] = useState('')
  const [creating, setCreating] = useState(false)

  const { data: horarios, isLoading } = useQuery({
    queryKey: ['horarios', unidadFilter],
    queryFn: () => horariosApi.getAll(unidadFilter),
    enabled: !!unidadFilter,
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const { data: clases } = useQuery({
    queryKey: ['clases-activas'],
    queryFn: () => clasesApi.getAll(undefined, false),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpdateHorarioClaseRequest }) => horariosApi.update(id, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['horarios'] })
      setEditDialog(null)
      resetForm()
    },
    onError: (err: unknown) => {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { error?: string } } }
        setFormError(axiosErr.response?.data?.error || 'Error al actualizar el horario.')
      } else {
        setFormError('Error al actualizar el horario.')
      }
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => horariosApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['horarios'] })
      setDeleteDialog(null)
    },
    onError: (err: unknown) => {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { error?: string } } }
        setFormError(axiosErr.response?.data?.error || 'Error al eliminar el horario.')
      } else {
        setFormError('Error al eliminar el horario.')
      }
    },
  })

  const resetForm = () => {
    setFormClaseId('')
    setFormDias(['Lunes'])
    setFormInicio('08:00')
    setFormFin('09:00')
    setFormSala('')
    setFormError(null)
  }

  const openCreate = () => {
    resetForm()
    setCreateDialog(true)
  }

  const openEdit = (h: HorarioClase) => {
    setFormClaseId(h.claseId)
    setFormDias([h.diaSemana])
    setFormInicio(h.horaInicio)
    setFormFin(h.horaFin)
    setFormSala(h.sala || '')
    setFormError(null)
    setEditDialog(h)
  }

  const handleCreate = async () => {
    if (!formClaseId) { setFormError('Selecciona una clase.'); return }
    if (formDias.length === 0) { setFormError('Selecciona al menos un día.'); return }
    setFormError(null)
    setCreating(true)
    try {
      for (const dia of formDias) {
        await horariosApi.create({
          claseId: formClaseId,
          diaSemana: dia,
          horaInicio: formInicio,
          horaFin: formFin,
          sala: formSala?.trim() || null,
        })
      }
      queryClient.invalidateQueries({ queryKey: ['horarios'] })
      setCreateDialog(false)
      resetForm()
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { error?: string } } }
      setFormError(axiosErr?.response?.data?.error || 'Error al crear el horario.')
      queryClient.invalidateQueries({ queryKey: ['horarios'] })
    } finally {
      setCreating(false)
    }
  }

  const handleUpdate = () => {
    if (!editDialog) return
    updateMutation.mutate({
      id: editDialog.id,
      req: {
        diaSemana: formDias[0],
        horaInicio: formInicio,
        horaFin: formFin,
        sala: formSala?.trim() || null,
      },
    })
  }

  // Group horarios by day
  const horariosByDay: Record<DiaSemana, HorarioClase[]> = {
    Lunes: [], Martes: [], Miercoles: [], Jueves: [], Viernes: [], Sabado: [], Domingo: [],
  }
  horarios?.forEach(h => {
    if (horariosByDay[h.diaSemana]) {
      horariosByDay[h.diaSemana].push(h)
    }
  })

  const gridStartHour = 7
  const gridEndHour = 23
  const totalMinutes = (gridEndHour - gridStartHour) * 60

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <CalendarDays className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Horarios</h1>
            <p className="text-sm text-muted-foreground">
              Gestiona los horarios semanales de las clases
            </p>
          </div>
        </div>
        <Button className="cursor-pointer gap-2" onClick={openCreate}>
          <Plus className="h-4 w-4" />
          Nuevo horario
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <Select value={unidadFilter} onValueChange={(val) => setUnidadFilter(val ?? '')}>
          <SelectTrigger className="w-[200px] bg-card border-border">
            <SelectValue placeholder="Seleccionar sede">
              {unidadFilter
                ? unidades?.find(u => u.id === unidadFilter)?.nombre || 'Sede'
                : undefined}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {unidades?.map((u) => (
              <SelectItem key={u.id} value={u.id}>{u.nombre}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        {unidadFilter && horarios && (
          <span className="text-sm text-muted-foreground">
            {horarios.length} horario{horarios.length !== 1 ? 's' : ''} programado{horarios.length !== 1 ? 's' : ''}
          </span>
        )}
      </div>

      {/* Weekly Calendar Grid */}
      {!unidadFilter ? (
        <div className="flex h-64 flex-col items-center justify-center gap-5 rounded-xl border border-border bg-card text-center">
          <p className="text-lg font-medium text-foreground">
            📅 Seleccioná una sede para ver los horarios
          </p>
          <div className="flex flex-wrap items-center justify-center gap-3">
            {unidades?.map((u) => (
              <Button
                key={u.id}
                variant="outline"
                className="cursor-pointer"
                onClick={() => setUnidadFilter(u.id)}
              >
                {u.nombre}
              </Button>
            ))}
          </div>
        </div>
      ) : isLoading ? (
        <div className="flex h-64 items-center justify-center text-muted-foreground">
          Cargando horarios...
        </div>
      ) : (
        <div className="rounded-xl border border-border bg-card overflow-x-auto">
          <div className="min-w-[900px]">
            {/* Day headers */}
            <div className="grid grid-cols-[80px_repeat(7,1fr)] border-b border-border">
              <div className="p-3 text-xs font-medium text-muted-foreground" />
              {DIAS.map(dia => (
                <div key={dia} className="p-3 text-center text-sm font-semibold text-foreground border-l border-border">
                  {DIAS_LABEL[dia]}
                </div>
              ))}
            </div>

            {/* Time grid */}
            <div className="grid grid-cols-[80px_repeat(7,1fr)] relative" style={{ minHeight: `${(gridEndHour - gridStartHour) * 60}px` }}>
              {/* Hour labels */}
              <div className="relative">
                {HORAS.map((hora) => (
                  <div
                    key={hora}
                    className="absolute w-full text-right pr-3 text-xs text-muted-foreground"
                    style={{ top: `${(timeToMinutes(hora) - gridStartHour * 60)}px`, transform: 'translateY(-50%)' }}
                  >
                    {hora}
                  </div>
                ))}
              </div>

              {/* Day columns */}
              {DIAS.map(dia => (
                <div key={dia} className="relative border-l border-border" style={{ minHeight: `${totalMinutes}px` }}>
                  {/* Hour lines */}
                  {HORAS.map((hora) => (
                    <div
                      key={hora}
                      className="absolute w-full border-t border-border/30"
                      style={{ top: `${timeToMinutes(hora) - gridStartHour * 60}px` }}
                    />
                  ))}

                  {/* Horario blocks (split de clases solapadas lado a lado) */}
                  {clustersDeSolapamiento(horariosByDay[dia]).flatMap(cluster =>
                    cluster.map((h, indiceEnCluster) => {
                      const top = timeToMinutes(h.horaInicio) - gridStartHour * 60
                      const height = timeToMinutes(h.horaFin) - timeToMinutes(h.horaInicio)
                      const colorClass = getColorForClase(h.claseId)
                      const width = 100 / cluster.length
                      const left = width * indiceEnCluster

                      return (
                        <div
                          key={h.id}
                          className={`absolute rounded-md border px-2 py-1 cursor-pointer hover:opacity-80 transition-opacity group ${colorClass}`}
                          style={{
                            top: `${top}px`,
                            height: `${Math.max(height, 30)}px`,
                            left: `calc(${left}% + 2px)`,
                            width: `calc(${width}% - 4px)`,
                          }}
                          onClick={() => openEdit(h)}
                        >
                          <div className="flex items-start justify-between gap-1">
                            <div className="min-w-0 flex-1 overflow-hidden">
                              <p className="text-xs font-semibold truncate">{h.claseNombre}</p>
                              {height >= 40 && (
                                <p className="text-[10px] opacity-80 truncate">{h.instructor}</p>
                              )}
                              {height >= 55 && (
                                <p className="text-[10px] opacity-70">
                                  {h.horaInicio} - {h.horaFin}
                                  {h.sala && ` | ${h.sala}`}
                                </p>
                              )}
                              {height >= 70 && (
                                <p className="text-[10px] opacity-60">
                                  {h.inscripcionesActivas}/{h.capacidadMaxima}
                                </p>
                              )}
                            </div>
                            <button
                              className="opacity-0 group-hover:opacity-100 flex-shrink-0 p-0.5 rounded hover:bg-white/10 transition-opacity cursor-pointer"
                              title="Eliminar"
                              onClick={(e) => { e.stopPropagation(); setDeleteDialog(h); setFormError(null) }}
                            >
                              <Trash2 className="h-3 w-3" />
                            </button>
                          </div>
                        </div>
                      )
                    })
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Create Dialog */}
      <Dialog open={createDialog} onOpenChange={(open) => { if (!open) { setCreateDialog(false); resetForm() } }}>
        <DialogContent className="bg-card border-border sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-foreground">Nuevo horario</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Agrega un nuevo bloque horario para una clase.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Clase</Label>
              <Select value={formClaseId ?? undefined} onValueChange={setFormClaseId}>
                <SelectTrigger className="bg-card border-border">
                  <SelectValue placeholder="Seleccionar clase">
                    {formClaseId
                      ? (() => { const c = clases?.find(cl => cl.id === formClaseId); return c ? `${c.nombre} (${c.unidadNombre})` : 'Seleccionar clase' })()
                      : undefined}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {clases?.filter(c => c.estaActivo).map(c => (
                    <SelectItem key={c.id} value={c.id}>
                      {c.nombre} ({c.unidadNombre})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Días de la semana</Label>
              <div className="grid grid-cols-2 gap-2">
                {DIAS.map(d => (
                  <label key={d} className="flex items-center gap-2 cursor-pointer rounded-md border border-border px-3 py-2 hover:bg-muted/50 transition-colors">
                    <input
                      type="checkbox"
                      checked={formDias.includes(d)}
                      onChange={() => {
                        setFormDias(prev =>
                          prev.includes(d) ? prev.filter(x => x !== d) : [...prev, d]
                        )
                      }}
                      className="h-4 w-4 cursor-pointer"
                    />
                    <span className="text-sm">{DIAS_LABEL[d]}</span>
                  </label>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Hora inicio</Label>
                <Input type="time" value={formInicio} onChange={e => setFormInicio(e.target.value)} className="bg-card border-border" />
              </div>
              <div className="space-y-2">
                <Label>Hora fin</Label>
                <Input type="time" value={formFin} onChange={e => setFormFin(e.target.value)} className="bg-card border-border" />
              </div>
            </div>

            <div className="space-y-2">
              <Label>Sala (opcional)</Label>
              <Input
                value={formSala}
                onChange={e => setFormSala(e.target.value)}
                placeholder="Ej: Sala A, Piscina..."
                className="bg-card border-border"
              />
            </div>
          </div>

          {formError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{formError}</p>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => { setCreateDialog(false); resetForm() }} className="cursor-pointer">
              Cancelar
            </Button>
            <Button onClick={handleCreate} disabled={creating} className="cursor-pointer">
              {creating ? 'Creando...' : `Crear horario${formDias.length > 1 ? `s (${formDias.length})` : ''}`}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editDialog} onOpenChange={(open) => { if (!open) { setEditDialog(null); resetForm() } }}>
        <DialogContent className="bg-card border-border sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-foreground">
              <Pencil className="h-4 w-4" />
              Editar horario
            </DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Modifica el horario de <strong className="text-foreground">{editDialog?.claseNombre}</strong>.
              Se notificara a los socios inscriptos si cambia el horario.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Dia de la semana</Label>
              <Select value={formDias[0]} onValueChange={(v) => setFormDias([v as DiaSemana])}>
                <SelectTrigger className="bg-card border-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {DIAS.map(d => (
                    <SelectItem key={d} value={d}>{DIAS_LABEL[d]}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Hora inicio</Label>
                <Input type="time" value={formInicio} onChange={e => setFormInicio(e.target.value)} className="bg-card border-border" />
              </div>
              <div className="space-y-2">
                <Label>Hora fin</Label>
                <Input type="time" value={formFin} onChange={e => setFormFin(e.target.value)} className="bg-card border-border" />
              </div>
            </div>

            <div className="space-y-2">
              <Label>Sala (opcional)</Label>
              <Input
                value={formSala}
                onChange={e => setFormSala(e.target.value)}
                placeholder="Ej: Sala A, Piscina..."
                className="bg-card border-border"
              />
            </div>
          </div>

          {formError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{formError}</p>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => { setEditDialog(null); resetForm() }} className="cursor-pointer">
              Cancelar
            </Button>
            <Button onClick={handleUpdate} disabled={updateMutation.isPending} className="cursor-pointer">
              {updateMutation.isPending ? 'Guardando...' : 'Guardar cambios'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={!!deleteDialog} onOpenChange={(open) => { if (!open) { setDeleteDialog(null); setFormError(null) } }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Eliminar horario</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Estas por eliminar el horario de <strong className="text-foreground">{deleteDialog?.claseNombre}</strong> el{' '}
              <strong className="text-foreground">{deleteDialog && DIAS_LABEL[deleteDialog.diaSemana]}</strong> de{' '}
              {deleteDialog?.horaInicio} a {deleteDialog?.horaFin}.
              Esta accion no se puede deshacer.
            </DialogDescription>
          </DialogHeader>
          {formError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{formError}</p>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setDeleteDialog(null); setFormError(null) }} className="cursor-pointer">
              Volver
            </Button>
            <Button
              variant="destructive"
              onClick={() => { if (deleteDialog) deleteMutation.mutate(deleteDialog.id) }}
              disabled={deleteMutation.isPending}
              className="cursor-pointer"
            >
              {deleteMutation.isPending ? 'Eliminando...' : 'Eliminar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
