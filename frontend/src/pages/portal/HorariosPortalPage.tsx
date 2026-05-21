import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { horariosApi, unidadesApi } from '@/services/api'
import { Badge } from '@/components/ui/badge'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { CalendarDays } from 'lucide-react'
import type { HorarioClase, DiaSemana } from '@/types'

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
  const [unidadFilter, setUnidadFilter] = useState<string>('all')

  const { data: horarios, isLoading } = useQuery({
    queryKey: ['horarios-portal', unidadFilter],
    queryFn: () => horariosApi.getAll(unidadFilter === 'all' ? undefined : unidadFilter),
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  // Group by day
  const horariosByDay: Record<DiaSemana, HorarioClase[]> = {
    Lunes: [], Martes: [], Miercoles: [], Jueves: [], Viernes: [], Sabado: [], Domingo: [],
  }
  horarios?.forEach(h => {
    if (horariosByDay[h.diaSemana]) {
      horariosByDay[h.diaSemana].push(h)
    }
  })

  // Sort each day by start time
  for (const dia of DIAS) {
    horariosByDay[dia].sort((a, b) => a.horaInicio.localeCompare(b.horaInicio))
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <CalendarDays className="h-6 w-6 text-primary" />
        <h1 className="text-2xl font-bold tracking-tight text-foreground">Horarios de Clases</h1>
      </div>

      {/* Sede filter */}
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
                          {lleno ? (
                            <Badge variant="secondary" className="bg-destructive/10 text-destructive border-0 text-xs">
                              Lleno
                            </Badge>
                          ) : (
                            <Badge variant="secondary" className="bg-muted text-muted-foreground border-0 text-xs">
                              {h.inscripcionesActivas}/{h.capacidadMaxima} ({ocupacion}%)
                            </Badge>
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
