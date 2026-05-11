import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { cuotasApi, unidadesApi } from '@/services/api'
import { formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { CreditCard, Search, CheckCircle, XCircle, Undo2 } from 'lucide-react'

type ConfirmAction = 'pagar' | 'anular' | 'revertir-pago' | 'revertir-anulacion'

export default function CuotasPage() {
  const queryClient = useQueryClient()
  const [cedula, setCedula] = useState('')
  const [searchedCedula, setSearchedCedula] = useState<string | null>(null)
  const [estadoFilter, setEstadoFilter] = useState<string>('all')
  const [mesFilter, setMesFilter] = useState<string>('all')
  const [anioFilter, setAnioFilter] = useState<string>('all')
  const [unidadFilter, setUnidadFilter] = useState<string>('all')
  const [confirmDialog, setConfirmDialog] = useState<{
    type: ConfirmAction
    id: string
    plan: string
  } | null>(null)

  const incluirAnuladas = estadoFilter === 'Anulada' || estadoFilter === 'all'

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const { data: cuotas, isLoading, error } = useQuery({
    queryKey: ['cuotas-admin', searchedCedula, estadoFilter, mesFilter, anioFilter, unidadFilter],
    queryFn: () =>
      cuotasApi.getAdmin({
        documentoIdentidad: searchedCedula!,
        estado: estadoFilter !== 'all' && estadoFilter !== 'Anulada' ? estadoFilter : undefined,
        mes: mesFilter !== 'all' ? parseInt(mesFilter) : undefined,
        anio: anioFilter !== 'all' ? parseInt(anioFilter) : undefined,
        unidadId: unidadFilter !== 'all' ? unidadFilter : undefined,
        incluirAnuladas,
      }),
    enabled: !!searchedCedula,
  })

  const filteredCuotas = estadoFilter === 'Anulada'
    ? cuotas?.filter(c => c.fechaBaja)
    : cuotas

  const pagarMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.marcarPagada(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] }); setConfirmDialog(null) },
  })
  const anularMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.anular(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] }); setConfirmDialog(null) },
  })
  const revertirPagoMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.revertirPago(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] }); setConfirmDialog(null) },
  })
  const revertirAnulacionMutation = useMutation({
    mutationFn: (id: string) => cuotasApi.revertirAnulacion(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['cuotas-admin'] }); setConfirmDialog(null) },
  })

  const handleSearch = () => {
    if (cedula.trim()) setSearchedCedula(cedula.trim())
  }

  const handleConfirm = () => {
    if (!confirmDialog) return
    switch (confirmDialog.type) {
      case 'pagar': pagarMutation.mutate(confirmDialog.id); break
      case 'anular': anularMutation.mutate(confirmDialog.id); break
      case 'revertir-pago': revertirPagoMutation.mutate(confirmDialog.id); break
      case 'revertir-anulacion': revertirAnulacionMutation.mutate(confirmDialog.id); break
    }
  }

  const isProcessing = pagarMutation.isPending || anularMutation.isPending ||
    revertirPagoMutation.isPending || revertirAnulacionMutation.isPending

  const getDialogTitle = (type?: ConfirmAction) => {
    switch (type) {
      case 'pagar': return 'Confirmar pago'
      case 'anular': return 'Confirmar anulacion'
      case 'revertir-pago': return 'Revertir pago'
      case 'revertir-anulacion': return 'Revertir anulacion'
      default: return ''
    }
  }

  const getDialogDescription = (type?: ConfirmAction, plan?: string) => {
    switch (type) {
      case 'pagar': return `Marcar como pagada la cuota de "${plan}"?`
      case 'anular': return `Anular la cuota de "${plan}"?`
      case 'revertir-pago': return `Revertir el pago de la cuota de "${plan}"? Volvera a estado Pendiente.`
      case 'revertir-anulacion': return `Revertir la anulacion de la cuota de "${plan}"? Volvera a estado Pendiente.`
      default: return ''
    }
  }

  const getBadge = (cuota: { estado: string; fechaBaja: string | null }) => {
    if (cuota.fechaBaja) return <Badge variant="outline">Anulada</Badge>
    if (cuota.estado === 'Pagada') return <Badge variant="default">Pagada</Badge>
    return <Badge variant="destructive">Pendiente</Badge>
  }

  const currentYear = new Date().getFullYear()
  const years = Array.from({ length: 5 }, (_, i) => currentYear - i)
  const months = [
    { value: '1', label: 'Enero' }, { value: '2', label: 'Febrero' },
    { value: '3', label: 'Marzo' }, { value: '4', label: 'Abril' },
    { value: '5', label: 'Mayo' }, { value: '6', label: 'Junio' },
    { value: '7', label: 'Julio' }, { value: '8', label: 'Agosto' },
    { value: '9', label: 'Septiembre' }, { value: '10', label: 'Octubre' },
    { value: '11', label: 'Noviembre' }, { value: '12', label: 'Diciembre' },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <CreditCard className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Gestion de Cuotas</h1>
          <p className="text-sm text-muted-foreground">Buscar por cedula del socio</p>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Cedula del socio..."
            value={cedula}
            onChange={(event) => setCedula(event.target.value)}
            onKeyDown={(event) => event.key === 'Enter' && handleSearch()}
            className="pl-9"
          />
        </div>
        <Button onClick={handleSearch} disabled={!cedula.trim()}>
          Buscar
        </Button>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
          {(error as any)?.response?.data?.error || 'Error al buscar cuotas.'}
        </div>
      )}

      {searchedCedula && (
        <div className="flex flex-wrap items-center gap-3">
          <Select value={estadoFilter} onValueChange={(value) => setEstadoFilter(value ?? 'all')}>
            <SelectTrigger className="w-[160px]">
              <SelectValue placeholder="Todos los estados" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los estados</SelectItem>
              <SelectItem value="Pendiente">Pendiente</SelectItem>
              <SelectItem value="Pagada">Pagada</SelectItem>
              <SelectItem value="Anulada">Anulada</SelectItem>
            </SelectContent>
          </Select>

          <Select value={mesFilter} onValueChange={(value) => setMesFilter(value ?? 'all')}>
            <SelectTrigger className="w-[160px]">
              <SelectValue placeholder="Todos los meses" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los meses</SelectItem>
              {months.map((month) => (
                <SelectItem key={month.value} value={month.value}>{month.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={anioFilter} onValueChange={(value) => setAnioFilter(value ?? 'all')}>
            <SelectTrigger className="w-[120px]">
              <SelectValue placeholder="Todos los anos" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los anos</SelectItem>
              {years.map((year) => (
                <SelectItem key={year} value={year.toString()}>{year}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={unidadFilter} onValueChange={(value) => setUnidadFilter(value ?? 'all')}>
            <SelectTrigger className="w-[200px]">
              <SelectValue placeholder="Todas las unidades" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todas las unidades</SelectItem>
              {unidades?.map((unidad) => (
                <SelectItem key={unidad.id} value={unidad.id}>{unidad.nombre}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      {searchedCedula && (
        <div className="rounded-xl border bg-card overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Socio</TableHead>
                <TableHead>Unidad</TableHead>
                <TableHead>Plan</TableHead>
                <TableHead>Monto</TableHead>
                <TableHead>Vencimiento</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground">
                    Cargando...
                  </TableCell>
                </TableRow>
              )}
              {filteredCuotas?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground">
                    No se encontraron cuotas.
                  </TableCell>
                </TableRow>
              )}
              {filteredCuotas?.map((cuota) => (
                <TableRow key={cuota.id} className={cuota.fechaBaja ? 'opacity-60' : ''}>
                  <TableCell className="font-medium">{cuota.nombreSocio}</TableCell>
                  <TableCell>{cuota.nombreUnidad}</TableCell>
                  <TableCell>{cuota.nombrePlan}</TableCell>
                  <TableCell>${cuota.monto.toLocaleString()}</TableCell>
                  <TableCell>{formatDate(cuota.fechaVencimiento)}</TableCell>
                  <TableCell>{getBadge(cuota)}</TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      {cuota.estado === 'Pendiente' && !cuota.fechaBaja && (
                        <>
                          <Button
                            size="sm"
                            variant="outline"
                            className="gap-1"
                            onClick={() => setConfirmDialog({ type: 'pagar', id: cuota.id, plan: cuota.nombrePlan })}
                          >
                            <CheckCircle className="h-3.5 w-3.5" />
                            Pagada
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            className="gap-1 text-destructive hover:text-destructive"
                            onClick={() => setConfirmDialog({ type: 'anular', id: cuota.id, plan: cuota.nombrePlan })}
                          >
                            <XCircle className="h-3.5 w-3.5" />
                            Anular
                          </Button>
                        </>
                      )}
                      {cuota.estado === 'Pagada' && !cuota.fechaBaja && (
                        <Button
                          size="sm"
                          variant="outline"
                          className="gap-1"
                          onClick={() => setConfirmDialog({ type: 'revertir-pago', id: cuota.id, plan: cuota.nombrePlan })}
                        >
                          <Undo2 className="h-3.5 w-3.5" />
                          Revertir pago
                        </Button>
                      )}
                      {cuota.fechaBaja && (
                        <Button
                          size="sm"
                          variant="outline"
                          className="gap-1"
                          onClick={() => setConfirmDialog({ type: 'revertir-anulacion', id: cuota.id, plan: cuota.nombrePlan })}
                        >
                          <Undo2 className="h-3.5 w-3.5" />
                          Revertir anulacion
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <Dialog open={!!confirmDialog} onOpenChange={() => setConfirmDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{getDialogTitle(confirmDialog?.type)}</DialogTitle>
            <DialogDescription>
              {getDialogDescription(confirmDialog?.type, confirmDialog?.plan)}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmDialog(null)}>
              Cancelar
            </Button>
            <Button
              variant={confirmDialog?.type === 'anular' ? 'destructive' : 'default'}
              onClick={handleConfirm}
              disabled={isProcessing}
            >
              {isProcessing ? 'Procesando...' : 'Confirmar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
