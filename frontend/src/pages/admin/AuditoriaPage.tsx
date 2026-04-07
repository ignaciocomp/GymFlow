import { useState, useEffect } from 'react'
import { auditoriaApi } from '@/services/api'
import type { AuditoriaEntry, TipoAccionAuditoria } from '@/types'
import { ChevronDown, ChevronRight } from 'lucide-react'

const TIPO_ACCIONES: { value: TipoAccionAuditoria; label: string }[] = [
  { value: 'Creacion', label: 'Creación' },
  { value: 'Modificacion', label: 'Modificación' },
  { value: 'Baja', label: 'Baja' },
  { value: 'Reactivacion', label: 'Reactivación' },
  { value: 'InicioSesion', label: 'Inicio de Sesión' },
]

const accionBadgeColor: Record<TipoAccionAuditoria, string> = {
  Creacion: 'bg-green-100 text-green-800',
  Modificacion: 'bg-blue-100 text-blue-800',
  Baja: 'bg-red-100 text-red-800',
  Reactivacion: 'bg-amber-100 text-amber-800',
  InicioSesion: 'bg-purple-100 text-purple-800',
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

  const formatDate = (iso: string) => {
    const d = new Date(iso)
    return d.toLocaleDateString('es-UY', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    })
  }

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
          <label className="block text-sm font-medium text-muted-foreground mb-1">Tipo de Acción</label>
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

      {/* Table */}
      <div className="rounded-lg border border-border bg-card">
        <table className="w-full">
          <thead>
            <tr className="border-b border-border bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-8"></th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Fecha/Hora</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Usuario</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Acción</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Entidad</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Descripción</th>
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
                <>
                  <tr
                    key={r.id}
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
                    <td className="px-4 py-3 text-sm whitespace-nowrap">{formatDate(r.fechaHora)}</td>
                    <td className="px-4 py-3 text-sm">{r.usuarioNombre}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        accionBadgeColor[r.tipoAccion] || 'bg-gray-100 text-gray-800'
                      }`}>
                        {TIPO_ACCIONES.find((t) => t.value === r.tipoAccion)?.label || r.tipoAccion}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm">{r.entidadAfectada}</td>
                    <td className="px-4 py-3 text-sm">{r.descripcion}</td>
                  </tr>
                  {expandedRow === r.id && r.detallesCambios && (
                    <tr key={`${r.id}-details`} className="bg-muted/20">
                      <td></td>
                      <td colSpan={5} className="px-4 py-3">
                        <div className="text-xs font-medium text-muted-foreground mb-1">Campos modificados:</div>
                        {renderDetalles(r.detallesCambios)}
                      </td>
                    </tr>
                  )}
                </>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
