import { useState, useEffect, Fragment } from 'react'
import { auditoriaApi } from '@/services/api'
import { formatDateTime } from '@/lib/utils'
import type { AuditoriaEntry, TipoAccionAuditoria } from '@/types'
import { ChevronDown, ChevronRight } from 'lucide-react'

const TIPO_ACCIONES: { value: TipoAccionAuditoria; label: string }[] = [
  { value: 'Creacion', label: 'Creación' },
  { value: 'Modificacion', label: 'Modificación' },
  { value: 'Baja', label: 'Baja' },
  { value: 'Reactivacion', label: 'Reactivación' },
  { value: 'InicioSesion', label: 'Inicio de sesión' },
  { value: 'SolicitudModificacion', label: 'Solicitud de modificación' },
  { value: 'SolicitudBaja', label: 'Solicitud de baja' },
]

const accionBadgeColor: Record<TipoAccionAuditoria, string> = {
  Creacion: 'bg-green-100 text-green-800',
  Modificacion: 'bg-blue-100 text-blue-800',
  Baja: 'bg-red-100 text-red-800',
  Reactivacion: 'bg-amber-100 text-amber-800',
  InicioSesion: 'bg-purple-100 text-purple-800',
  SolicitudModificacion: 'bg-sky-100 text-sky-800',
  SolicitudBaja: 'bg-orange-100 text-orange-800',
}

export default function AuditoriaPage() {
  const [registros, setRegistros] = useState<AuditoriaEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [tipoAccionFilter, setTipoAccionFilter] = useState<string>('')
  const [desde, setDesde] = useState<string>('')
  const [hasta, setHasta] = useState<string>('')
  const [expandedRow, setExpandedRow] = useState<string | null>(null)

  const fetchRegistros = async () => {
    setLoading(true)
    try {
      const params: Record<string, string> = {}
      if (tipoAccionFilter) params.tipoAccion = tipoAccionFilter
      if (desde) params.desde = new Date(desde).toISOString()
      if (hasta) params.hasta = new Date(hasta + 'T23:59:59').toISOString()

      const data = await auditoriaApi.getAll(params)
      setRegistros(data)
    } catch (err) {
      console.error('Error fetching audit log:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchRegistros()
  }, [tipoAccionFilter, desde, hasta])


  const renderDetalles = (json: string) => {
    try {
      const cambios = JSON.parse(json) as Record<string, { anterior: string; nuevo: string }>
      return (
        <div className="space-y-1 text-sm">
          {Object.entries(cambios).map(([campo, vals]) => (
            <div key={campo} className="flex gap-2">
              <span className="font-medium text-muted-foreground w-32">{campo}:</span>
              <span className="text-red-600 line-through">{String(vals.anterior ?? '(vacío)')}</span>
              <span className="text-muted-foreground">-&gt;</span>
              <span className="text-green-700">{String(vals.nuevo ?? '(vacío)')}</span>
            </div>
          ))}
        </div>
      )
    } catch {
      return <pre className="text-xs text-muted-foreground">{json}</pre>
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Auditoría</h1>
        <p className="text-muted-foreground">Registro de operaciones críticas del sistema (RNF-11)</p>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-4 items-end">
        <div>
          <label className="block text-sm font-medium text-muted-foreground mb-1">Tipo de acción</label>
          <select
            value={tipoAccionFilter}
            onChange={(e) => setTipoAccionFilter(e.target.value)}
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          >
            <option value="">Todas</option>
            {TIPO_ACCIONES.map((t) => (
              <option key={t.value} value={t.value}>{t.label}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-muted-foreground mb-1">Desde</label>
          <input
            type="date"
            value={desde}
            onChange={(e) => setDesde(e.target.value)}
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-muted-foreground mb-1">Hasta</label>
          <input
            type="date"
            value={hasta}
            onChange={(e) => setHasta(e.target.value)}
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          />
        </div>
      </div>

      {/* Tabla — visible en sm+ con scroll horizontal */}
      <div className="hidden sm:block rounded-lg border border-border bg-card overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-border bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground w-8"></th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground whitespace-nowrap">Fecha/hora</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground">Usuario</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground">Acción</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground">Entidad</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground">Descripción</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                  Cargando registros...
                </td>
              </tr>
            ) : registros.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                  No se encontraron registros de auditoría.
                </td>
              </tr>
            ) : (
              registros.map((r) => (
                <Fragment key={r.id}>
                  <tr
                    className={`border-b border-border hover:bg-muted/30 transition-colors ${
                      r.detallesCambios ? 'cursor-pointer' : ''
                    }`}
                    onClick={() =>
                      r.detallesCambios &&
                      setExpandedRow(expandedRow === r.id ? null : r.id)
                    }
                  >
                    <td className="px-4 py-3">
                      {r.detallesCambios && (
                        expandedRow === r.id
                          ? <ChevronDown className="h-4 w-4 text-muted-foreground" />
                          : <ChevronRight className="h-4 w-4 text-muted-foreground" />
                      )}
                    </td>
                    <td className="px-4 py-3 text-sm whitespace-nowrap">{formatDateTime(r.fechaHora)}</td>
                    <td className="px-4 py-3 text-sm whitespace-nowrap">{r.usuarioNombre}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium whitespace-nowrap ${
                        accionBadgeColor[r.tipoAccion] || 'bg-gray-100 text-gray-800'
                      }`}>
                        {TIPO_ACCIONES.find((t) => t.value === r.tipoAccion)?.label || r.tipoAccion}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm">{r.entidadAfectada}</td>
                    <td className="px-4 py-3 text-sm max-w-md">{r.descripcion}</td>
                  </tr>
                  {expandedRow === r.id && r.detallesCambios && (
                    <tr className="bg-muted/20">
                      <td></td>
                      <td colSpan={5} className="px-4 py-3">
                        <div className="text-xs font-medium text-muted-foreground mb-1">Campos modificados:</div>
                        {renderDetalles(r.detallesCambios)}
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Cards (mobile only) */}
      <div className="sm:hidden space-y-3">
        {loading && (
          <div className="rounded-lg border bg-card p-6 text-center text-muted-foreground">
            Cargando registros...
          </div>
        )}
        {!loading && registros.length === 0 && (
          <div className="rounded-lg border bg-card p-6 text-center text-muted-foreground">
            No se encontraron registros de auditoría.
          </div>
        )}
        {!loading && registros.map((r) => {
          const isExpanded = expandedRow === r.id
          return (
            <div
              key={r.id}
              className={`rounded-lg border bg-card p-4 space-y-2 ${r.detallesCambios ? 'cursor-pointer' : ''}`}
              onClick={() => r.detallesCambios && setExpandedRow(isExpanded ? null : r.id)}
            >
              <div className="flex items-start justify-between gap-2">
                <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium whitespace-nowrap ${
                  accionBadgeColor[r.tipoAccion] || 'bg-gray-100 text-gray-800'
                }`}>
                  {TIPO_ACCIONES.find((t) => t.value === r.tipoAccion)?.label || r.tipoAccion}
                </span>
                {r.detallesCambios && (
                  isExpanded
                    ? <ChevronDown className="h-4 w-4 text-muted-foreground" />
                    : <ChevronRight className="h-4 w-4 text-muted-foreground" />
                )}
              </div>
              <p className="text-sm text-foreground">{r.descripcion}</p>
              <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
                <span>{formatDateTime(r.fechaHora)}</span>
                <span>·</span>
                <span>{r.usuarioNombre}</span>
                <span>·</span>
                <span>{r.entidadAfectada}</span>
              </div>
              {isExpanded && r.detallesCambios && (
                <div className="mt-2 border-t border-border pt-2">
                  <div className="text-xs font-medium text-muted-foreground mb-1">Campos modificados:</div>
                  {renderDetalles(r.detallesCambios)}
                </div>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
