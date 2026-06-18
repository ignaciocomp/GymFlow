import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { sociosApi, unidadesApi, planesApi } from '@/services/api'
import { useAuth } from '@/context/AuthContext'
import { formatDate } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
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
import { Link, useSearchParams, useNavigate } from 'react-router-dom'
import { UserPlus, Search, Eye, Trash2, RotateCcw, Users, UserX } from 'lucide-react'

export default function SociosPage() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const tab = searchParams.get('tab') || 'activos'

  const [search, setSearch] = useState('')
  const [unidadFilter, setUnidadFilter] = useState<string>('all')
  const [planFilter, setPlanFilter] = useState<string>('all')
  const [deleteDialog, setDeleteDialog] = useState<{ id: string; nombre: string } | null>(null)
  const [motivoBaja, setMotivoBaja] = useState('')

  const isActiveTab = tab === 'activos'

  const { data: socios, isLoading } = useQuery({
    queryKey: ['socios', search, unidadFilter, planFilter, tab],
    queryFn: () =>
      sociosApi.getAll({
        nombre: search || undefined,
        unidadId: unidadFilter !== 'all' ? unidadFilter : undefined,
        planId: planFilter !== 'all' ? planFilter : undefined,
        estaActivo: isActiveTab,
      }),
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  // Para un Dueño, constreñir el selector a sus unidades; el Admin (unidadIds vacío) ve todas.
  const unidadIdsPermitidas = user?.unidadIds ?? []
  const unidadesVisibles = unidadIdsPermitidas.length > 0
    ? unidades?.filter(u => unidadIdsPermitidas.includes(u.id))
    : unidades

  const { data: planes } = useQuery({
    queryKey: ['planes'],
    queryFn: () => planesApi.getAll(),
  })

  const reactivateMutation = useMutation({
    mutationFn: (id: string) => sociosApi.reactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['socios'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: ({ id, motivo }: { id: string; motivo: string | null }) =>
      sociosApi.delete(id, { motivo }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['socios'] })
      setDeleteDialog(null)
      setMotivoBaja('')
    },
  })

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Users className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              {isActiveTab ? 'Socios activos' : 'Socios inactivos'}
            </h1>
            <p className="text-sm text-muted-foreground">
              Gestiona los socios {isActiveTab ? 'activos' : 'inactivos'} del gimnasio
            </p>
          </div>
        </div>
        <Link to="/admin/socios/nuevo">
          <Button className="cursor-pointer gap-2">
            <UserPlus className="h-4 w-4" />
            Nuevo socio
          </Button>
        </Link>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 rounded-lg bg-muted/50 p-1 w-fit">
        <button
          onClick={() => setSearchParams({ tab: 'activos' })}
          className={`flex items-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-colors cursor-pointer ${
            isActiveTab
              ? 'bg-card text-primary shadow-sm'
              : 'text-muted-foreground hover:text-foreground'
          }`}
        >
          <Users className="h-4 w-4" />
          Socios activos
        </button>
        <button
          onClick={() => setSearchParams({ tab: 'inactivos' })}
          className={`flex items-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-colors cursor-pointer ${
            !isActiveTab
              ? 'bg-card text-primary shadow-sm'
              : 'text-muted-foreground hover:text-foreground'
          }`}
        >
          <UserX className="h-4 w-4" />
          Socios inactivos
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por nombre o correo..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 bg-card border-border"
          />
        </div>

        <Select value={unidadFilter} onValueChange={(val) => setUnidadFilter(val ?? 'all')}>
          <SelectTrigger className="w-[200px] bg-card border-border">
            <SelectValue>
              {unidadFilter === 'all'
                ? 'Todas las unidades'
                : unidadesVisibles?.find(u => u.id === unidadFilter)?.nombre || 'Unidad'}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todas las unidades</SelectItem>
            {unidadesVisibles?.map((u) => (
              <SelectItem key={u.id} value={u.id}>{u.nombre}</SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={planFilter} onValueChange={(val) => setPlanFilter(val ?? 'all')}>
          <SelectTrigger className="w-[200px] bg-card border-border">
            <SelectValue>
              {planFilter === 'all'
                ? 'Todos los planes'
                : planes?.find(p => p.id === planFilter)?.nombre || 'Plan'}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los planes</SelectItem>
            {planes?.map((p) => (
              <SelectItem key={p.id} value={p.id}>{p.nombre} — {p.unidadNombre}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      <div className="rounded-xl border border-border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="border-border hover:bg-transparent">
              <TableHead className="text-muted-foreground">Nombre y apellido</TableHead>
              <TableHead className="text-muted-foreground">Doc. identidad</TableHead>
              <TableHead className="text-muted-foreground">Correo</TableHead>
              <TableHead className="text-muted-foreground">Celular</TableHead>
              <TableHead className="text-muted-foreground">Unidades / plan</TableHead>
              <TableHead className="text-muted-foreground">Fecha alta</TableHead>
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
            {socios && socios.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} className="h-32 text-center">
                  <div className="flex flex-col items-center gap-2">
                    <Users className="h-8 w-8 text-muted-foreground/50" />
                    <p className="text-muted-foreground">No se encontraron socios.</p>
                    {isActiveTab && (
                      <Link to="/admin/socios/nuevo">
                        <Button variant="outline" size="sm" className="mt-2 cursor-pointer gap-2">
                          <UserPlus className="h-4 w-4" />
                          Agregar socio
                        </Button>
                      </Link>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            )}
            {socios?.map((socio) => (
              <TableRow key={socio.id} className="border-border hover:bg-muted/30">
                <TableCell>
                  <div className="flex items-center gap-3">
                    <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary text-sm font-medium">
                      {socio.nombre[0]}{socio.apellido[0]}
                    </div>
                    <span className="font-medium text-foreground">
                      {socio.apellido}, {socio.nombre}
                    </span>
                  </div>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {socio.tipoDocumento && socio.documentoIdentidad
                    ? `${socio.tipoDocumento}: ${socio.documentoIdentidad}`
                    : socio.documentoIdentidad || '-'}
                </TableCell>
                <TableCell className="text-muted-foreground">{socio.correo}</TableCell>
                <TableCell className="text-muted-foreground">{socio.telefono || '-'}</TableCell>
                <TableCell>
                  <div className="flex flex-col gap-1">
                    {socio.unidades.map((u) => (
                      <div key={u.id} className="flex items-center gap-1.5">
                        <Badge variant="outline" className="border-border text-muted-foreground text-xs">
                          {u.nombre}
                        </Badge>
                        <span className="text-xs text-muted-foreground">
                          {u.planNombre || 'Sin plan'}
                        </span>
                      </div>
                    ))}
                    {socio.unidades.length === 0 && (
                      <span className="text-muted-foreground">-</span>
                    )}
                  </div>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {formatDate(socio.fechaAlta)}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <button
                      className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
                      title="Ver"
                      onClick={() => navigate(`/admin/socios/${socio.id}/editar`)}
                    >
                      <Eye className="h-4 w-4" />
                    </button>
                    {isActiveTab ? (
                      <button
                        className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-destructive/10 hover:text-destructive transition-colors cursor-pointer"
                        title="Dar de baja"
                        onClick={() =>
                          setDeleteDialog({
                            id: socio.id,
                            nombre: `${socio.nombre} ${socio.apellido}`,
                          })
                        }
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    ) : (
                      <button
                        className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-primary/10 hover:text-primary transition-colors cursor-pointer"
                        title="Reactivar"
                        onClick={() => reactivateMutation.mutate(socio.id)}
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

        {/* Footer */}
        {socios && socios.length > 0 && (
          <div className="border-t border-border px-4 py-3">
            <p className="text-xs text-muted-foreground">
              Mostrando {socios.length} registros
            </p>
          </div>
        )}
      </div>

      {/* Delete Dialog */}
      <Dialog open={!!deleteDialog} onOpenChange={() => setDeleteDialog(null)}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Confirmar baja de socio</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Estás por dar de baja a <strong className="text-foreground">{deleteDialog?.nombre}</strong>.
              Esta acción marca al socio como inactivo (no se eliminan sus datos).
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Input
              placeholder="Motivo de la baja (opcional)"
              value={motivoBaja}
              onChange={(e) => setMotivoBaja(e.target.value)}
              className="bg-muted/50 border-border"
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteDialog(null)} className="cursor-pointer">
              Cancelar
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                if (deleteDialog) {
                  deleteMutation.mutate({ id: deleteDialog.id, motivo: motivoBaja || null })
                }
              }}
              disabled={deleteMutation.isPending}
              className="cursor-pointer"
            >
              {deleteMutation.isPending ? 'Procesando...' : 'Confirmar baja'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
