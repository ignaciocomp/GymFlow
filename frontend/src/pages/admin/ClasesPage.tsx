import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { clasesApi, unidadesApi } from '@/services/api'
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
import { BookOpen, Plus, Eye, Trash2, RotateCcw } from 'lucide-react'

export default function ClasesPage() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const [unidadFilter, setUnidadFilter] = useState<string>('all')
  const [cancelDialog, setCancelDialog] = useState<{ id: string; nombre: string } | null>(null)
  const [cancelError, setCancelError] = useState<string | null>(null)
  const [reactivateDialog, setReactivateDialog] = useState<{ id: string; nombre: string } | null>(null)
  const [reactivateError, setReactivateError] = useState<string | null>(null)

  const { data: clases, isLoading } = useQuery({
    queryKey: ['clases'],
    queryFn: () => clasesApi.getAll(undefined, true),
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const cancelMutation = useMutation({
    mutationFn: (id: string) => clasesApi.cancel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clases'] })
      setCancelDialog(null)
      setCancelError(null)
    },
    onError: (err: unknown) => {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { error?: string } } }
        setCancelError(axiosErr.response?.data?.error || 'Error al cancelar la clase.')
      } else {
        setCancelError('Error al cancelar la clase.')
      }
    },
  })

  const reactivateMutation = useMutation({
    mutationFn: (id: string) => clasesApi.reactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clases'] })
      setReactivateDialog(null)
      setReactivateError(null)
    },
    onError: (err: unknown) => {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { error?: string } } }
        setReactivateError(axiosErr.response?.data?.error || 'Error al reactivar la clase.')
      } else {
        setReactivateError('Error al reactivar la clase.')
      }
    },
  })

  const filteredClases = clases?.filter((c) =>
    unidadFilter === 'all' ? true : c.unidadId === unidadFilter
  )

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <BookOpen className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Clases</h1>
            <p className="text-sm text-muted-foreground">
              Gestiona las clases del gimnasio
            </p>
          </div>
        </div>
        <Link to="/admin/clases/nueva">
          <Button className="cursor-pointer gap-2">
            <Plus className="h-4 w-4" />
            Nueva clase
          </Button>
        </Link>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <Select value={unidadFilter} onValueChange={(val) => setUnidadFilter(val ?? 'all')}>
          <SelectTrigger className="w-[200px] bg-card border-border">
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

      {/* Table */}
      <div className="rounded-xl border border-border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="border-border hover:bg-transparent">
              <TableHead className="text-muted-foreground">Nombre</TableHead>
              <TableHead className="text-muted-foreground">Instructor</TableHead>
              <TableHead className="text-muted-foreground">Sede</TableHead>
              <TableHead className="text-muted-foreground">Capacidad</TableHead>
              <TableHead className="text-muted-foreground">Duración</TableHead>
              <TableHead className="text-muted-foreground">Estado</TableHead>
              <TableHead className="text-muted-foreground text-right">Acciones</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={7} className="h-32 text-center text-muted-foreground">
                  Cargando...
                </TableCell>
              </TableRow>
            )}
            {filteredClases && filteredClases.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} className="h-32 text-center">
                  <div className="flex flex-col items-center gap-2">
                    <BookOpen className="h-8 w-8 text-muted-foreground/50" />
                    <p className="text-muted-foreground">No se encontraron clases.</p>
                    <Link to="/admin/clases/nueva">
                      <Button variant="outline" size="sm" className="mt-2 cursor-pointer gap-2">
                        <Plus className="h-4 w-4" />
                        Agregar clase
                      </Button>
                    </Link>
                  </div>
                </TableCell>
              </TableRow>
            )}
            {filteredClases?.map((clase) => (
              <TableRow key={clase.id} className="border-border hover:bg-muted/30">
                <TableCell className="font-medium text-foreground">{clase.nombre}</TableCell>
                <TableCell className="text-muted-foreground">{clase.instructor}</TableCell>
                <TableCell>
                  <Badge variant="outline" className="border-border text-muted-foreground text-xs">
                    {clase.unidadNombre}
                  </Badge>
                </TableCell>
                <TableCell className="text-muted-foreground">{clase.capacidadMaxima}</TableCell>
                <TableCell className="text-muted-foreground">
                  {clase.duracionMinutos} min
                </TableCell>
                <TableCell>
                  {clase.estaActivo ? (
                    <Badge variant="secondary" className="bg-primary/10 text-primary border-0">
                      Activa
                    </Badge>
                  ) : (
                    <Badge variant="secondary" className="bg-muted text-muted-foreground border-0">
                      Cancelada
                    </Badge>
                  )}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    {clase.estaActivo ? (
                      <>
                        <button
                          className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
                          title="Editar"
                          onClick={() => navigate(`/admin/clases/${clase.id}/editar`)}
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        <button
                          className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-destructive/10 hover:text-destructive transition-colors cursor-pointer"
                          title="Cancelar clase"
                          onClick={() => {
                            setCancelError(null)
                            setCancelDialog({ id: clase.id, nombre: clase.nombre })
                          }}
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </>
                    ) : (
                      <button
                        className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-primary/10 hover:text-primary transition-colors cursor-pointer"
                        title="Reactivar"
                        onClick={() => setReactivateDialog({ id: clase.id, nombre: clase.nombre })}
                      >
                        <RotateCcw className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>

        {filteredClases && filteredClases.length > 0 && (
          <div className="border-t border-border px-4 py-3">
            <p className="text-xs text-muted-foreground">
              Mostrando {filteredClases.length} registros
            </p>
          </div>
        )}
      </div>

      {/* Cancel Dialog */}
      <Dialog open={!!cancelDialog} onOpenChange={() => { setCancelDialog(null); setCancelError(null) }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Confirmar cancelación de clase</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Estás por cancelar la clase <strong className="text-foreground">{cancelDialog?.nombre}</strong>.
              Se notificará automáticamente a todos los socios inscriptos.
              Podrás reactivarla luego si es necesario.
            </DialogDescription>
          </DialogHeader>
          {cancelError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{cancelError}</p>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setCancelDialog(null); setCancelError(null) }} className="cursor-pointer">
              Volver
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                if (cancelDialog) cancelMutation.mutate(cancelDialog.id)
              }}
              disabled={cancelMutation.isPending}
              className="cursor-pointer"
            >
              {cancelMutation.isPending ? 'Cancelando...' : 'Cancelar clase'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reactivate Dialog */}
      <Dialog open={!!reactivateDialog} onOpenChange={() => { setReactivateDialog(null); setReactivateError(null) }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Reactivar clase</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              ¿Querés reactivar la clase <strong className="text-foreground">{reactivateDialog?.nombre}</strong>?
              Volverá a estar disponible.
            </DialogDescription>
          </DialogHeader>
          {reactivateError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{reactivateError}</p>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setReactivateDialog(null)} className="cursor-pointer">
              Cancelar
            </Button>
            <Button
              onClick={() => {
                if (reactivateDialog) reactivateMutation.mutate(reactivateDialog.id)
              }}
              disabled={reactivateMutation.isPending}
              className="cursor-pointer"
            >
              {reactivateMutation.isPending ? 'Reactivando...' : 'Reactivar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
