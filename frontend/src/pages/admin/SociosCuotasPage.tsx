import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { cuotasApi, unidadesApi } from '@/services/api'
import { useAuth } from '@/context/AuthContext'
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
import { CreditCard, Search, CheckCircle2, Clock, AlertTriangle, ChevronRight } from 'lucide-react'
import type { EstadoGeneralCuotas } from '@/types'

function EstadoBadge({ estado }: { estado: EstadoGeneralCuotas }) {
  if (estado === 'AlDia') {
    return (
      <Badge variant="outline" className="gap-1 border-green-500/40 text-green-400 bg-green-500/10">
        <CheckCircle2 className="h-3 w-3" />
        Al día
      </Badge>
    )
  }
  if (estado === 'Vencido') {
    return (
      <Badge variant="destructive" className="gap-1">
        <AlertTriangle className="h-3 w-3" />
        Vencido
      </Badge>
    )
  }
  return (
    <Badge variant="outline" className="gap-1 border-amber-500/40 text-amber-400 bg-amber-500/10">
      <Clock className="h-3 w-3" />
      Pendiente
    </Badge>
  )
}

export default function SociosCuotasPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [unidadFilter, setUnidadFilter] = useState<string | null>(null)
  const [estadoFilter, setEstadoFilter] = useState<string | null>(null)
  const [busqueda, setBusqueda] = useState('')

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  // Para un Dueño, constreñir el selector a sus unidades; el Admin (unidadIds vacío) ve todas.
  const unidadIdsPermitidas = user?.unidadIds ?? []
  const unidadesVisibles = unidadIdsPermitidas.length > 0
    ? unidades?.filter(u => unidadIdsPermitidas.includes(u.id))
    : unidades

  const { data: socios, isLoading } = useQuery({
    queryKey: ['socios-estado-cuota', unidadFilter],
    queryFn: () => cuotasApi.getSociosEstado(unidadFilter ?? undefined),
  })

  const filteredSocios = socios?.filter((socio) => {
    if (estadoFilter && socio.estado !== estadoFilter) return false
    if (busqueda.trim()) {
      const term = busqueda.trim().toLowerCase()
      const matchNombre = `${socio.nombre} ${socio.apellido}`.toLowerCase().includes(term)
      const matchDoc = socio.documentoIdentidad?.toLowerCase().includes(term) ?? false
      const matchCorreo = socio.correo.toLowerCase().includes(term)
      if (!matchNombre && !matchDoc && !matchCorreo) return false
    }
    return true
  })

  // Contadores para el resumen de arriba
  const total = socios?.length ?? 0
  const alDia = socios?.filter((s) => s.estado === 'AlDia').length ?? 0
  const pendientes = socios?.filter((s) => s.estado === 'Pendiente').length ?? 0
  const vencidos = socios?.filter((s) => s.estado === 'Vencido').length ?? 0

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <CreditCard className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Gestión de Cuotas</h1>
          <p className="text-sm text-muted-foreground">
            Seleccioná un socio para ver y administrar sus cuotas
          </p>
        </div>
      </div>

      {/* Tarjetas resumen — 2 cols en mobile, 4 en sm+ */}
      <div className="grid gap-3 grid-cols-2 sm:grid-cols-4">
        <button
          onClick={() => setEstadoFilter(null)}
          className={`rounded-xl border bg-card p-4 text-left transition-colors hover:bg-accent/40 cursor-pointer ${
            !estadoFilter ? 'border-primary' : ''
          }`}
        >
          <p className="text-xs uppercase tracking-wider text-muted-foreground">Total</p>
          <p className="mt-1 text-2xl font-bold">{total}</p>
        </button>
        <button
          onClick={() => setEstadoFilter('AlDia')}
          className={`rounded-xl border bg-card p-4 text-left transition-colors hover:bg-accent/40 cursor-pointer ${
            estadoFilter === 'AlDia' ? 'border-green-500' : ''
          }`}
        >
          <p className="text-xs uppercase tracking-wider text-muted-foreground">Al día</p>
          <p className="mt-1 text-2xl font-bold text-green-400">{alDia}</p>
        </button>
        <button
          onClick={() => setEstadoFilter('Pendiente')}
          className={`rounded-xl border bg-card p-4 text-left transition-colors hover:bg-accent/40 cursor-pointer ${
            estadoFilter === 'Pendiente' ? 'border-amber-500' : ''
          }`}
        >
          <p className="text-xs uppercase tracking-wider text-muted-foreground">Pendientes</p>
          <p className="mt-1 text-2xl font-bold text-amber-400">{pendientes}</p>
        </button>
        <button
          onClick={() => setEstadoFilter('Vencido')}
          className={`rounded-xl border bg-card p-4 text-left transition-colors hover:bg-accent/40 cursor-pointer ${
            estadoFilter === 'Vencido' ? 'border-destructive' : ''
          }`}
        >
          <p className="text-xs uppercase tracking-wider text-muted-foreground">Vencidos</p>
          <p className="mt-1 text-2xl font-bold text-destructive">{vencidos}</p>
        </button>
      </div>

      {/* Filtros */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Buscar por nombre, cédula o correo..."
            value={busqueda}
            onChange={(event) => setBusqueda(event.target.value)}
            className="pl-9"
          />
        </div>

        <Select value={unidadFilter} onValueChange={(v) => setUnidadFilter(v)}>
          <SelectTrigger className="w-[240px]">
            <SelectValue placeholder="Todas las unidades" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={null}>Todas las unidades</SelectItem>
            {unidadesVisibles?.map((unidad) => (
              <SelectItem key={unidad.id} value={unidad.id}>
                {unidad.nombre}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Tabla de socios */}
      <div className="rounded-xl border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Socio</TableHead>
              <TableHead>Documento</TableHead>
              <TableHead>Unidades</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead className="text-center">Pendientes</TableHead>
              <TableHead className="text-center">Vencidas</TableHead>
              <TableHead className="text-right">Acciones</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={7} className="text-center text-muted-foreground">
                  Cargando socios...
                </TableCell>
              </TableRow>
            )}
            {!isLoading && filteredSocios?.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} className="text-center text-muted-foreground py-12">
                  No se encontraron socios con los filtros actuales.
                </TableCell>
              </TableRow>
            )}
            {filteredSocios?.map((socio) => (
              <TableRow
                key={socio.socioId}
                className="cursor-pointer hover:bg-accent/40"
                onClick={() => navigate(`/admin/cuotas/${socio.socioId}`)}
              >
                <TableCell>
                  <div className="font-medium">{socio.nombre} {socio.apellido}</div>
                  <div className="text-xs text-muted-foreground">{socio.correo}</div>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {socio.documentoIdentidad || '—'}
                </TableCell>
                <TableCell>
                  <div className="flex flex-wrap gap-1">
                    {socio.unidades.length === 0 ? (
                      <span className="text-xs text-muted-foreground">Sin unidades</span>
                    ) : (
                      socio.unidades.map((u, i) => (
                        <Badge key={i} variant="outline" className="text-xs">
                          {u}
                        </Badge>
                      ))
                    )}
                  </div>
                </TableCell>
                <TableCell>
                  <EstadoBadge estado={socio.estado} />
                </TableCell>
                <TableCell className="text-center">
                  {socio.cuotasPendientes > 0 ? (
                    <span className="font-medium text-amber-400">{socio.cuotasPendientes}</span>
                  ) : (
                    <span className="text-muted-foreground">—</span>
                  )}
                </TableCell>
                <TableCell className="text-center">
                  {socio.cuotasVencidas > 0 ? (
                    <span className="font-medium text-destructive">{socio.cuotasVencidas}</span>
                  ) : (
                    <span className="text-muted-foreground">—</span>
                  )}
                </TableCell>
                <TableCell className="text-right">
                  <Button
                    size="sm"
                    variant="outline"
                    className="gap-1"
                    onClick={(e) => {
                      e.stopPropagation()
                      navigate(`/admin/cuotas/${socio.socioId}`)
                    }}
                  >
                    Ver cuotas
                    <ChevronRight className="h-3.5 w-3.5" />
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {!isLoading && filteredSocios && filteredSocios.length > 0 && (
        <p className="text-xs text-muted-foreground text-center">
          Mostrando {filteredSocios.length} de {total} socios — <span className="text-foreground">Hacé click en una fila para gestionar sus cuotas</span>
        </p>
      )}

      <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-3 text-xs text-amber-200/80 flex items-center gap-2">
        <Clock className="h-4 w-4 shrink-0" />
        <span>Indicador <strong>"Estado"</strong>: <strong>Al día</strong> = todas las cuotas pagas · <strong>Pendiente</strong> = tiene cuotas sin pagar pero ninguna vencida · <strong>Vencido</strong> = al menos una cuota pasada de fecha.</span>
      </div>
    </div>
  )
}
