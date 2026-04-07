import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { planesApi, unidadesApi } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import { Link, useNavigate } from 'react-router-dom'
import { CreditCard, Plus, Eye, Trash2 } from 'lucide-react'

export default function PlanesPage() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const [unidadFilter, setUnidadFilter] = useState<string>('all')
  const [deleteDialog, setDeleteDialog] = useState<{ id: string; nombre: string } | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const { data: planes, isLoading } = useQuery({
    queryKey: ['planes'],
    queryFn: () => planesApi.getAll(undefined, true),
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => planesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['planes'] })
      setDeleteDialog(null)
      setDeleteError(null)
    },
    onError: (err: unknown) => {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { error?: string } } }
        setDeleteError(axiosErr.response?.data?.error || 'Error al eliminar el plan.')
      } else {
        setDeleteError('Error al eliminar el plan.')
      }
    },
  })

  const filteredPlanes = planes?.filter((p) =>
    unidadFilter === 'all' ? true : p.unidadId === unidadFilter
  )

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <CreditCard className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Planes</h1>
            <p className="text-sm text-muted-foreground">
              Gestiona los planes del gimnasio
            </p>
          </div>
        </div>
        <Link to="/admin/planes/nuevo">
          <Button className="cursor-pointer gap-2">
            <Plus className="h-4 w-4" />
            Nuevo Plan
          </Button>
        </Link>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <Select value={unidadFilter} onValueChange={(val) => setUnidadFilter(val ?? 'all')}>
          <SelectTrigger className="w-[200px] bg-card border-border">
            <SelectValue>
              {unidadFilter === 'all'
                ? 'Todas las unidades'
                : unidades?.find(u => u.id === unidadFilter)?.nombre || 'Unidad'}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todas las unidades</SelectItem>
            {unidades?.map((u) => (
              <SelectItem key={u.id} value={u.id}>{u.nombre}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      <div className="rounded-xl border border-border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="border-border hover:bg-transparent">
              <TableHead className="text-muted-foreground">Nombre</TableHead>
              <TableHead className="text-muted-foreground">Unidad</TableHead>
              <TableHead className="text-muted-foreground">Precio</TableHead>
              <TableHead className="text-muted-foreground">Estado</TableHead>
              <TableHead className="text-muted-foreground text-right">Acciones</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center text-muted-foreground">
                  Cargando...
                </TableCell>
              </TableRow>
            )}
            {filteredPlanes && filteredPlanes.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center">
                  <div className="flex flex-col items-center gap-2">
                    <CreditCard className="h-8 w-8 text-muted-foreground/50" />
                    <p className="text-muted-foreground">No se encontraron planes.</p>
                    <Link to="/admin/planes/nuevo">
                      <Button variant="outline" size="sm" className="mt-2 cursor-pointer gap-2">
                        <Plus className="h-4 w-4" />
                        Agregar Plan
                      </Button>
                    </Link>
                  </div>
                </TableCell>
              </TableRow>
            )}
            {filteredPlanes?.map((plan) => (
              <TableRow key={plan.id} className="border-border hover:bg-muted/30">
                <TableCell className="font-medium text-foreground">{plan.nombre}</TableCell>
                <TableCell>
                  <Badge variant="outline" className="border-border text-muted-foreground text-xs">
                    {plan.unidadNombre}
                  </Badge>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  ${plan.precio.toLocaleString('es-UY', { minimumFractionDigits: 2 })}
                </TableCell>
                <TableCell>
                  {plan.estaActivo ? (
                    <Badge variant="secondary" className="bg-primary/10 text-primary border-0">
                      Activo
                    </Badge>
                  ) : (
                    <Badge variant="secondary" className="bg-muted text-muted-foreground border-0">
                      Inactivo
                    </Badge>
                  )}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <button
                      className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
                      title="Editar"
                      onClick={() => navigate(`/admin/planes/${plan.id}/editar`)}
                    >
                      <Eye className="h-4 w-4" />
                    </button>
                    <button
                      className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-destructive/10 hover:text-destructive transition-colors cursor-pointer"
                      title="Eliminar"
                      onClick={() => {
                        setDeleteError(null)
                        setDeleteDialog({ id: plan.id, nombre: plan.nombre })
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>

        {/* Footer */}
        {filteredPlanes && filteredPlanes.length > 0 && (
          <div className="border-t border-border px-4 py-3">
            <p className="text-xs text-muted-foreground">
              Mostrando {filteredPlanes.length} registros
            </p>
          </div>
        )}
      </div>

      {/* Delete Dialog */}
      <Dialog open={!!deleteDialog} onOpenChange={() => { setDeleteDialog(null); setDeleteError(null) }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Confirmar eliminacion de plan</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Estas por eliminar el plan <strong className="text-foreground">{deleteDialog?.nombre}</strong>.
              Esta accion no se puede deshacer.
            </DialogDescription>
          </DialogHeader>
          {deleteError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{deleteError}</p>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setDeleteDialog(null); setDeleteError(null) }} className="cursor-pointer">
              Cancelar
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                if (deleteDialog) {
                  deleteMutation.mutate(deleteDialog.id)
                }
              }}
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
