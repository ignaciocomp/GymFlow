import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { portalApi } from '@/services/api'
import { formatDateTime } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Bell,
  CreditCard,
  CalendarClock,
  CalendarX,
  CheckCircle2,
  PartyPopper,
  Check,
  CheckCheck,
  Loader2,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import type { TipoNotificacion } from '@/types'

const ICONOS_POR_TIPO: Record<TipoNotificacion, LucideIcon> = {
  RecordatorioCuota: CreditCard,
  CambioHorario: CalendarClock,
  CancelacionClase: CalendarX,
  ConfirmacionInscripcion: CheckCircle2,
  EventoNuevo: PartyPopper,
}

export default function NotificacionesPortalPage() {
  const queryClient = useQueryClient()

  const { data: notificaciones, isLoading } = useQuery({
    queryKey: ['notificaciones'],
    queryFn: () => portalApi.getNotificaciones({ take: 50 }),
  })

  const invalidar = () => {
    queryClient.invalidateQueries({ queryKey: ['notificaciones'] })
    queryClient.invalidateQueries({ queryKey: ['notif-count'] })
  }

  const marcarLeidaMutation = useMutation({
    mutationFn: (id: string) => portalApi.marcarLeida(id),
    onSuccess: invalidar,
  })

  const marcarTodasMutation = useMutation({
    mutationFn: () => portalApi.marcarTodasLeidas(),
    onSuccess: invalidar,
  })

  const noLeidas = notificaciones?.filter((n) => !n.leida).length ?? 0

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <Bell className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Notificaciones</h1>
          {noLeidas > 0 && (
            <span className="rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-semibold text-primary tabular-nums">
              {noLeidas} sin leer
            </span>
          )}
        </div>
        {noLeidas > 0 && (
          <Button
            variant="outline"
            size="sm"
            className="cursor-pointer"
            disabled={marcarTodasMutation.isPending}
            onClick={() => marcarTodasMutation.mutate()}
          >
            {marcarTodasMutation.isPending ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : (
              <CheckCheck className="h-3.5 w-3.5" />
            )}
            Marcar todas como leídas
          </Button>
        )}
      </div>

      {isLoading ? (
        <div className="flex h-32 items-center justify-center text-muted-foreground">
          Cargando notificaciones...
        </div>
      ) : notificaciones && notificaciones.length > 0 ? (
        <div className="overflow-hidden rounded-xl border border-border bg-card">
          <div className="divide-y divide-border">
            {notificaciones.map((n) => {
              const Icono = ICONOS_POR_TIPO[n.tipo] ?? Bell
              return (
                <div
                  key={n.id}
                  className={cn(
                    'flex items-start gap-4 px-4 py-4 transition-colors sm:px-5',
                    !n.leida && 'bg-primary/[0.04]'
                  )}
                >
                  <div
                    className={cn(
                      'flex h-9 w-9 shrink-0 items-center justify-center rounded-lg',
                      n.leida
                        ? 'bg-muted text-muted-foreground'
                        : 'bg-primary/10 text-primary'
                    )}
                  >
                    <Icono className="h-4 w-4" aria-hidden="true" />
                  </div>

                  <div className="min-w-0 flex-1 space-y-1">
                    <div className="flex items-center gap-2">
                      {!n.leida && (
                        <span
                          className="h-2 w-2 shrink-0 rounded-full bg-primary"
                          aria-label="No leída"
                        />
                      )}
                      <p
                        className={cn(
                          'truncate text-sm',
                          n.leida ? 'font-medium text-foreground' : 'font-semibold text-foreground'
                        )}
                      >
                        {n.titulo}
                      </p>
                    </div>
                    <p className="text-sm text-muted-foreground">{n.mensaje}</p>
                    <p className="text-xs text-muted-foreground tabular-nums">
                      {formatDateTime(n.fechaCreacion)}
                    </p>
                  </div>

                  {!n.leida && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="shrink-0 cursor-pointer text-muted-foreground hover:text-foreground"
                      disabled={marcarLeidaMutation.isPending}
                      onClick={() => marcarLeidaMutation.mutate(n.id)}
                      aria-label="Marcar como leída"
                    >
                      <Check className="h-3.5 w-3.5" />
                      <span className="hidden sm:inline">Marcar leída</span>
                    </Button>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      ) : (
        <div className="flex h-40 flex-col items-center justify-center gap-2 rounded-xl border border-border bg-card text-center">
          <Bell className="h-8 w-8 text-muted-foreground/50" />
          <p className="text-muted-foreground">No tenés notificaciones por el momento.</p>
        </div>
      )}
    </div>
  )
}
