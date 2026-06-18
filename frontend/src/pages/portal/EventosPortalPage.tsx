import { useQuery } from '@tanstack/react-query'
import { portalApi } from '@/services/api'
import { useAuth } from '@/context/AuthContext'
import { formatDateTime } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { CalendarDays, MapPin } from 'lucide-react'

export default function EventosPortalPage() {
  const { user } = useAuth()
  const variasSedes = (user?.unidadIds?.length ?? 0) > 1

  const { data: eventos, isLoading } = useQuery({
    queryKey: ['eventos-portal'],
    queryFn: portalApi.getEventos,
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <CalendarDays className="h-6 w-6 text-primary" />
        <h1 className="text-2xl font-bold tracking-tight text-foreground">Próximos Eventos</h1>
      </div>

      {isLoading ? (
        <div className="flex h-32 items-center justify-center text-muted-foreground">
          Cargando eventos...
        </div>
      ) : eventos && eventos.length > 0 ? (
        <div className="space-y-3">
          {eventos.map((evento) => (
            <div
              key={evento.id}
              className="rounded-xl border border-border bg-card p-4 sm:p-5"
            >
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div className="min-w-0 space-y-1">
                  <h2 className="text-base font-semibold text-foreground">{evento.titulo}</h2>
                  {evento.descripcion && (
                    <p className="text-sm text-muted-foreground">{evento.descripcion}</p>
                  )}
                  {variasSedes && (
                    <p className="flex items-center gap-1.5 pt-1 text-xs text-muted-foreground">
                      <MapPin className="h-3.5 w-3.5 shrink-0" />
                      {evento.unidadNombre}
                    </p>
                  )}
                </div>
                <Badge
                  variant="secondary"
                  className="shrink-0 gap-1.5 self-start border-0 bg-primary/10 text-primary tabular-nums"
                >
                  <CalendarDays className="h-3.5 w-3.5" />
                  {formatDateTime(evento.fecha)}
                </Badge>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="flex h-40 flex-col items-center justify-center gap-2 rounded-xl border border-border bg-card text-center">
          <CalendarDays className="h-8 w-8 text-muted-foreground/50" />
          <p className="text-muted-foreground">No hay eventos próximos por el momento.</p>
        </div>
      )}
    </div>
  )
}
