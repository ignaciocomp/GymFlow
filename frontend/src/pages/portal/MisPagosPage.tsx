import { useQuery } from '@tanstack/react-query'
import { pagosApi } from '@/services/api'
import { formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Receipt } from 'lucide-react'
import type { EstadoPago } from '@/types'

function getBadgeVariant(estado: EstadoPago) {
  if (estado === 'Aprobado') return 'default'
  if (estado === 'Rechazado') return 'destructive'
  return 'outline'
}

export default function MisPagosPage() {
  const { data: pagos, isLoading } = useQuery({
    queryKey: ['mis-pagos'],
    queryFn: pagosApi.getMisPagos,
  })

  const sinPagos = !isLoading && (!pagos || pagos.length === 0)

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <Receipt className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Mis Pagos</h1>
          <p className="text-sm text-muted-foreground">Historial de pagos realizados con Mercado Pago</p>
        </div>
      </div>

      {/* Vista tabla (sm+) */}
      <div className="hidden sm:block rounded-xl border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Fecha</TableHead>
              <TableHead>Plan</TableHead>
              <TableHead>Monto</TableHead>
              <TableHead>Medio de pago</TableHead>
              <TableHead>N° transacción</TableHead>
              <TableHead>Estado</TableHead>
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
            {sinPagos && (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground">
                  No tenés pagos registrados.
                </TableCell>
              </TableRow>
            )}
            {pagos?.map((pago) => (
              <TableRow key={pago.id}>
                <TableCell>{formatDate(pago.fecha)}</TableCell>
                <TableCell className="font-medium">{pago.nombrePlan}</TableCell>
                <TableCell>${pago.monto.toLocaleString()}</TableCell>
                <TableCell>{pago.medioPago ?? '-'}</TableCell>
                <TableCell className="tabular-nums">{pago.mpPaymentId ?? '-'}</TableCell>
                <TableCell>
                  <Badge variant={getBadgeVariant(pago.estado)}>{pago.estado}</Badge>
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
        {sinPagos && (
          <div className="rounded-xl border bg-card p-6 text-center text-muted-foreground">
            No tenés pagos registrados.
          </div>
        )}
        {pagos?.map((pago) => (
          <div key={pago.id} className="rounded-xl border bg-card p-4 space-y-3">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="font-medium text-foreground">{pago.nombrePlan}</p>
                <p className="text-xs text-muted-foreground">{formatDate(pago.fecha)}</p>
              </div>
              <Badge variant={getBadgeVariant(pago.estado)}>{pago.estado}</Badge>
            </div>
            <div className="grid grid-cols-2 gap-2 text-sm">
              <div>
                <p className="text-xs uppercase tracking-wider text-muted-foreground">Monto</p>
                <p className="font-medium">${pago.monto.toLocaleString()}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-muted-foreground">Medio de pago</p>
                <p className="font-medium">{pago.medioPago ?? '-'}</p>
              </div>
              <div className="col-span-2">
                <p className="text-xs uppercase tracking-wider text-muted-foreground">N° transacción</p>
                <p className="font-medium tabular-nums">{pago.mpPaymentId ?? '-'}</p>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
