// frontend/src/pages/portal/MisCuotasPage.tsx
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { cuotasApi, pagosApi } from '@/services/api'
import { formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { CreditCard, ChevronLeft, ChevronRight, Loader2 } from 'lucide-react'

const PAGE_SIZE = 12

function getBadgeVariant(cuota: { estado: string; fechaVencimiento: string }) {
  if (cuota.estado === 'Pagada') return 'default'
  if (cuota.estado === 'Anulada') return 'secondary'
  const vencida = new Date(cuota.fechaVencimiento) < new Date()
  return vencida ? 'destructive' : 'outline'
}

export default function MisCuotasPage() {
  const [page, setPage] = useState(0)
  // Cuota que está iniciando el pago (para el spinner por fila) y mensaje de error global.
  const [pagandoId, setPagandoId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { data: cuotas, isLoading } = useQuery({
    queryKey: ['mis-cuotas'],
    queryFn: cuotasApi.getMisCuotas,
  })

  const totalPages = cuotas ? Math.ceil(cuotas.length / PAGE_SIZE) : 0
  const paginatedCuotas = cuotas?.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE)

  const iniciarPago = async (cuotaId: string) => {
    setError(null)
    setPagandoId(cuotaId)
    try {
      const { initPoint } = await pagosApi.iniciar(cuotaId)
      // Redirige a Checkout Pro de Mercado Pago.
      window.location.href = initPoint
    } catch {
      // Un 409 significa que la cuota ya no es pagable; cualquier error lo resolvemos igual.
      setError('No se pudo iniciar el pago. Intentá de nuevo en unos minutos.')
      setPagandoId(null)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <CreditCard className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Mis Cuotas</h1>
          <p className="text-sm text-muted-foreground">Historial de cuotas y estado de pagos</p>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      {/* Vista tabla (sm+) */}
      <div className="hidden sm:block rounded-xl border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Plan</TableHead>
              <TableHead>Unidad</TableHead>
              <TableHead>Monto</TableHead>
              <TableHead>Vencimiento</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead className="text-right">Acción</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground">
                  Cargando...
                </TableCell>
              </TableRow>
            )}
            {cuotas?.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground">
                  No tenés cuotas registradas.
                </TableCell>
              </TableRow>
            )}
            {paginatedCuotas?.map((cuota) => (
              <TableRow key={cuota.id}>
                <TableCell className="font-medium">{cuota.nombrePlan}</TableCell>
                <TableCell>{cuota.nombreUnidad}</TableCell>
                <TableCell>${cuota.monto.toLocaleString()}</TableCell>
                <TableCell>{formatDate(cuota.fechaVencimiento)}</TableCell>
                <TableCell>
                  <Badge variant={getBadgeVariant(cuota)}>
                    {cuota.estado}
                  </Badge>
                </TableCell>
                <TableCell className="text-right">
                  {cuota.estado === 'Pendiente' && (
                    <Button
                      size="sm"
                      variant="outline"
                      className="cursor-pointer"
                      disabled={pagandoId === cuota.id}
                      onClick={() => iniciarPago(cuota.id)}
                    >
                      {pagandoId === cuota.id && <Loader2 className="h-3 w-3 animate-spin mr-1" />}
                      Pagar con Mercado Pago
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {/* Vista cards (mobile only) */}
      <div className="sm:hidden space-y-3">
        {isLoading && (
          <div className="rounded-xl border bg-card p-6 text-center text-muted-foreground">
            Cargando...
          </div>
        )}
        {cuotas?.length === 0 && !isLoading && (
          <div className="rounded-xl border bg-card p-6 text-center text-muted-foreground">
            No tenés cuotas registradas.
          </div>
        )}
        {paginatedCuotas?.map((cuota) => (
          <div key={cuota.id} className="rounded-xl border bg-card p-4 space-y-3">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="font-medium text-foreground">{cuota.nombrePlan}</p>
                <p className="text-xs text-muted-foreground">{cuota.nombreUnidad}</p>
              </div>
              <Badge variant={getBadgeVariant(cuota)}>{cuota.estado}</Badge>
            </div>
            <div className="grid grid-cols-2 gap-2 text-sm">
              <div>
                <p className="text-xs uppercase tracking-wider text-muted-foreground">Monto</p>
                <p className="font-medium">${cuota.monto.toLocaleString()}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-muted-foreground">Vencimiento</p>
                <p className="font-medium">{formatDate(cuota.fechaVencimiento)}</p>
              </div>
            </div>
            {cuota.estado === 'Pendiente' && (
              <Button
                size="sm"
                variant="outline"
                className="w-full cursor-pointer"
                disabled={pagandoId === cuota.id}
                onClick={() => iniciarPago(cuota.id)}
              >
                {pagandoId === cuota.id && <Loader2 className="h-3 w-3 animate-spin mr-1" />}
                Pagar con Mercado Pago
              </Button>
            )}
          </div>
        ))}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button
            size="sm"
            variant="outline"
            onClick={() => setPage((p) => Math.max(0, p - 1))}
            disabled={page === 0}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <span className="text-sm text-muted-foreground">
            Página {page + 1} de {totalPages}
          </span>
          <Button
            size="sm"
            variant="outline"
            onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
            disabled={page >= totalPages - 1}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      )}
    </div>
  )
}
