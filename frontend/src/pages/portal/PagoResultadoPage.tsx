import { useEffect } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, XCircle, Clock } from 'lucide-react'

type Estado = 'approved' | 'failure' | 'pending'

// Mercado Pago redirige a esta página con ?status=approved|failure|pending
// (más sus propios params: payment_id, collection_status, etc.).
const CONFIG: Record<Estado, {
  titulo: string
  descripcion: string
  nota?: string
  Icon: typeof CheckCircle2
  color: string
  bg: string
}> = {
  approved: {
    titulo: '¡Pago confirmado!',
    descripcion: 'Recibimos tu pago correctamente. ¡Gracias!',
    nota: 'La cuota puede tardar unos segundos en reflejarse como pagada mientras confirmamos la operación.',
    Icon: CheckCircle2,
    color: 'text-emerald-600',
    bg: 'bg-emerald-500/10',
  },
  failure: {
    titulo: 'El pago fue rechazado',
    descripcion: 'No pudimos procesar tu pago. Podés intentarlo nuevamente desde Mis Cuotas.',
    Icon: XCircle,
    color: 'text-destructive',
    bg: 'bg-destructive/10',
  },
  pending: {
    titulo: 'Pago en proceso',
    descripcion: 'Tu pago está siendo procesado. Te avisaremos cuando se confirme.',
    nota: 'Puede tardar unos minutos en acreditarse según el medio de pago elegido.',
    Icon: Clock,
    color: 'text-amber-600',
    bg: 'bg-amber-500/10',
  },
}

function normalizar(status: string | null): Estado {
  if (status === 'approved') return 'approved'
  if (status === 'pending' || status === 'in_process') return 'pending'
  return 'failure'
}

export default function PagoResultadoPage() {
  const [searchParams] = useSearchParams()
  const queryClient = useQueryClient()
  const estado = normalizar(searchParams.get('status'))
  const { titulo, descripcion, nota, Icon, color, bg } = CONFIG[estado]

  // Al aprobarse, refrescamos las cuotas para que la que se pagó aparezca actualizada.
  useEffect(() => {
    if (estado === 'approved') {
      queryClient.invalidateQueries({ queryKey: ['mis-cuotas'] })
    }
  }, [estado, queryClient])

  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-6 py-10 text-center">
      <div className={`flex h-16 w-16 items-center justify-center rounded-full ${bg} ${color}`}>
        <Icon className="h-8 w-8" />
      </div>
      <div className="space-y-2">
        <h1 className="text-2xl font-bold text-foreground">{titulo}</h1>
        <p className="text-sm text-muted-foreground">{descripcion}</p>
        {nota && <p className="text-xs text-muted-foreground">{nota}</p>}
      </div>
      <div className="flex flex-col gap-3 sm:flex-row">
        <Link
          to="/portal/mis-cuotas"
          className="inline-flex h-9 items-center justify-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
        >
          Mis cuotas
        </Link>
        <Link
          to="/portal/mis-pagos"
          className="inline-flex h-9 items-center justify-center rounded-lg border border-border bg-background px-4 text-sm font-medium text-foreground transition-colors hover:bg-muted"
        >
          Mis pagos
        </Link>
      </div>
    </div>
  )
}
