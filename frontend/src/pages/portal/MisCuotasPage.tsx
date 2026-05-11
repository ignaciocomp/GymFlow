// frontend/src/pages/portal/MisCuotasPage.tsx
import { useQuery } from '@tanstack/react-query'
import { cuotasApi } from '@/services/api'
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
import { CreditCard } from 'lucide-react'

export default function MisCuotasPage() {
  const { data: cuotas, isLoading } = useQuery({
    queryKey: ['mis-cuotas'],
    queryFn: cuotasApi.getMisCuotas,
  })

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

      <div className="rounded-xl border bg-card overflow-hidden">
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
            {cuotas?.map((cuota) => (
              <TableRow key={cuota.id}>
                <TableCell className="font-medium">{cuota.nombrePlan}</TableCell>
                <TableCell>{cuota.nombreUnidad}</TableCell>
                <TableCell>${cuota.monto.toLocaleString()}</TableCell>
                <TableCell>{formatDate(cuota.fechaVencimiento)}</TableCell>
                <TableCell>
                  <Badge variant={cuota.estado === 'Pagada' ? 'default' : 'destructive'}>
                    {cuota.estado}
                  </Badge>
                </TableCell>
                <TableCell className="text-right">
                  {cuota.estado === 'Pendiente' && (
                    <Button size="sm" variant="outline" disabled>
                      Pagar
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
