import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { horariosApi, unidadesApi, inscripcionesApi } from '@/services/api'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { CalendarDays, CheckCircle2, Loader2 } from 'lucide-react'
import type { HorarioClase, DiaSemana, InscripcionClase } from '@/types'

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

function getColorForClase(claseId: string) {
  const colors = [
    'bg-blue-500/15 border-blue-500/30 text-blue-300',
    'bg-emerald-500/15 border-emerald-500/30 text-emerald-300',
    'bg-violet-500/15 border-violet-500/30 text-violet-300',
    'bg-amber-500/15 border-amber-500/30 text-amber-300',
    'bg-rose-500/15 border-rose-500/30 text-rose-300',
    'bg-cyan-500/15 border-cyan-500/30 text-cyan-300',
  ]
  let hash = 0
  for (let i = 0; i < claseId.length; i++) {
    hash = claseId.charCodeAt(i) + ((hash << 5) - hash)
  }
  return colors[Math.abs(hash) % colors.length]
}

export default function HorariosPortalPage() {
  const queryClient = useQueryClient()
  const [unidadFilter, setUnidadFilter] = useState<string>('all')
  const [actionError, setActionError] = useState<string | null>(null)

  const { data: horarios, isLoading } = useQuery({
    queryKey: ['horarios-portal', unidadFilter],
    queryFn: () => horariosApi.getAll(unidadFilter === 'all' ? undefined : unidadFilter),
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const { data: misInscripciones } = useQuery({
    queryKey: ['mis-inscripciones'],
    queryFn: inscripcionesApi.getMisInscripciones,
  })

  const inscripcionPorClase = new Map<string, InscripcionClase>()
  misInscripciones?.forEach(i => {
    inscripcionPorClase.set(i.claseId, i)
  })

  const inscribirseMutation = useMutation({
    mutationFn: (claseId: string) => inscripcionesApi.inscribirse(claseId),
    onSuccess: () => {
      setActionError(null)
      queryClient.invalidateQueries({ queryKey: ['mis-inscripciones'] })
      queryClient.invalidateQueries({ queryKey: ['horarios-portal'] })
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { error?: string } } }
      setActionError(axiosErr?.response?.data?.error || 'Error al inscribirse.')
    },
  })

  const cancelarMutation = useMutation({
    mutationFn: (id: string) => inscripcionesApi.cancelar(id),
    onSuccess: () => {
      setActionError(null)
      queryClient.invalidateQueries({ queryKey: ['mis-inscripciones'] })
      queryClient.invalidateQueries({ queryKey: ['horarios-portal'] })
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { error?: string } } }
      setActionError(axiosErr?.response?.data?.error || 'Error al cancelar inscripción.')
    },
  })

  const horariosByDay: Record<DiaSemana, HorarioClase[]> = {
    Lunes: [], Martes: [], Miercoles: [], Jueves: [], Viernes: [], Sabado: [], Domingo: [],
  }
  horarios?.forEach(h => {
    if (horariosByDay[h.diaSemana]) {
      horariosByDay[h.diaSemana].push(h)
    }
  })

  for (const dia of DIAS) {
    horariosByDay[dia].sort((a, b) => a.horaInicio.localeCompare(b.horaInicio))
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <CalendarDays className="h-6 w-6 text-primary" />
        <h1 className="text-2xl font-bold tracking-tight text-foreground">Horarios de Clases</h1>
      </div>

      <div className="flex items-center gap-3">
        <Select value={unidadFilter} onValueChange={(val) => setUnidadFilter(val ?? 'all')}>
          <SelectTrigger className="w-[220px] bg-card border-border">
            <SelectValue>
              {unidadFilter === 'all'
                ? 'Todas las sedes'
                : unidades?.find(u => u.id === unidadFilter)?.nombre || 'Sede'}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todas las sedes</SelectItem>
            {unidades?.map((u) => (
              <SelectItem key={u.id} value={u.id}>{u.nombre}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {actionError && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
          <p className="text-sm text-destructive">{actionError}</p>
        </div>
      )}

      {isLoading ? (
        <div className="flex h-32 items-center justify-center text-muted-foreground">
          Cargando horarios...
        </div>
      ) : (
        <div className="space-y-4">
          {DIAS.map(dia => {
            const diaHorarios = horariosByDay[dia]
            if (diaHorarios.length === 0) return null

            return (
              <div key={dia} className="rounded-xl border border-border bg-card overflow-hidden">
                <div className="bg-muted/30 px-4 py-3 border-b border-border">
                  <h2 className="text-sm font-semibold text-foreground">{DIAS_LABEL[dia]}</h2>
                </div>
                <div className="divide-y divide-border">
                  {diaHorarios.map(h => {
                    const colorClass = getColorForClase(h.claseId)
                    const ocupacion = h.capacidadMaxima > 0
                      ? Math.round((h.inscripcionesActivas / h.capacidadMaxima) * 100)
                      : 0
                    const lleno = h.inscripcionesActivas >= h.capacidadMaxima
                    const inscripcion = inscripcionPorClase.get(h.claseId)
                    const estaInscripto = !!inscripcion
                    const isBusy = inscribirseMutation.isPending || cancelarMutation.isPending

                    return (
                      <div key={h.id} className="flex items-center gap-4 px-4 py-3">
                        <div className={`flex h-12 w-20 flex-shrink-0 items-center justify-center rounded-lg border text-xs font-bold ${colorClass}`}>
                          {h.horaInicio}
                          <br />
                          {h.horaFin}
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-semibold text-foreground truncate">{h.claseNombre}</p>
                          <p className="text-xs text-muted-foreground">
                            {h.instructor}
                            {h.sala && <> &middot; {h.sala}</>}
                            {unidadFilter === 'all' && <> &middot; {h.unidadNombre}</>}
                          </p>
                        </div>
                        <div className="flex items-center gap-2 flex-shrink-0">
                          {lleno && !estaInscripto ? (
                            <Badge variant="secondary" className="bg-destructive/10 text-destructive border-0 text-xs">
                              Cupo lleno
                            </Badge>
                          ) : (
                            <Badge variant="secondary" className="bg-muted text-muted-foreground border-0 text-xs">
                              {h.inscripcionesActivas}/{h.capacidadMaxima} ({ocupacion}%)
                            </Badge>
                          )}
                          {estaInscripto ? (
                            <div className="flex items-center gap-2">
                              <Badge variant="secondary" className="bg-emerald-500/10 text-emerald-400 border-0 text-xs gap-1">
                                <CheckCircle2 className="h-3 w-3" />
                                Inscripto
                              </Badge>
                              <Button
                                variant="outline"
                                size="sm"
                                className="h-7 text-xs cursor-pointer"
                                disabled={isBusy}
                                onClick={() => cancelarMutation.mutate(inscripcion.id)}
                              >
                                {cancelarMutation.isPending ? <Loader2 className="h-3 w-3 animate-spin" /> : 'Cancelar'}
                              </Button>
                            </div>
                          ) : (
                            <Button
                              size="sm"
                              className="h-7 text-xs cursor-pointer"
                              disabled={lleno || isBusy}
                              onClick={() => inscribirseMutation.mutate(h.claseId)}
                            >
                              {inscribirseMutation.isPending ? <Loader2 className="h-3 w-3 animate-spin" /> : 'Inscribirme'}
                            </Button>
                          )}
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            )
          })}

          {horarios && horarios.length === 0 && (
            <div className="flex h-32 flex-col items-center justify-center gap-2 rounded-xl border border-border bg-card">
              <CalendarDays className="h-8 w-8 text-muted-foreground/50" />
              <p className="text-muted-foreground">No hay horarios disponibles.</p>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
