import { useState } from 'react'
import {
  BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts'
import {
  LayoutDashboard, Users, CalendarClock, AlertTriangle, BookOpen, Pause, Activity, UserPlus,
} from 'lucide-react'
import { useDashboardStream } from '@/hooks/useDashboardStream'
import { formatDateTime, cn } from '@/lib/utils'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import type { DashboardDto } from '@/types'

type VistaGrafica = 'sociosPorSede' | 'cuotasPorEstado' | 'inscripcionesUltimos7Dias'

const GRAFICA_STORAGE_KEY = 'gymflow_dashboard_grafica'
const TODAS = 'todas'

const VISTAS: { id: VistaGrafica; label: string }[] = [
  { id: 'sociosPorSede', label: 'Socios por sede' },
  { id: 'cuotasPorEstado', label: 'Cuotas por estado' },
  { id: 'inscripcionesUltimos7Dias', label: 'Inscripciones últimos 7 días' },
]

function vistaInicial(): VistaGrafica {
  const guardada = localStorage.getItem(GRAFICA_STORAGE_KEY)
  return VISTAS.some(v => v.id === guardada) ? (guardada as VistaGrafica) : 'sociosPorSede'
}

/** "yyyy-MM-dd" → "dd/MM" para los ticks de la serie diaria. */
function tickDia(fecha: string): string {
  const [, m, d] = fecha.split('-')
  return `${d}/${m}`
}

const tooltipEstilo = {
  contentStyle: {
    backgroundColor: 'var(--card)',
    border: '1px solid var(--border)',
    borderRadius: 8,
    fontSize: 12,
  },
  labelStyle: { color: 'var(--muted-foreground)' },
  itemStyle: { color: 'var(--card-foreground)' },
  cursor: { fill: 'var(--muted)', opacity: 0.4 },
}

const ejeEstilo = {
  stroke: 'var(--muted-foreground)',
  tick: { fill: 'var(--muted-foreground)', fontSize: 12 },
  tickLine: false,
  axisLine: false,
} as const

function Grafica({ vista, grafica }: { vista: VistaGrafica; grafica: DashboardDto['grafica'] }) {
  if (vista === 'inscripcionesUltimos7Dias') {
    return (
      <LineChart data={grafica.inscripcionesUltimos7Dias} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
        <XAxis dataKey="fecha" tickFormatter={tickDia} {...ejeEstilo} />
        <YAxis allowDecimals={false} {...ejeEstilo} />
        <Tooltip {...tooltipEstilo} labelFormatter={(label) => tickDia(String(label))} />
        <Line
          type="monotone"
          dataKey="cantidad"
          name="Inscripciones"
          stroke="var(--chart-1)"
          strokeWidth={2}
          dot={{ fill: 'var(--chart-1)', r: 3 }}
          activeDot={{ r: 5 }}
        />
      </LineChart>
    )
  }

  if (vista === 'sociosPorSede') {
    return (
      <BarChart data={grafica.sociosPorSede} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
        <XAxis dataKey="sede" {...ejeEstilo} />
        <YAxis allowDecimals={false} {...ejeEstilo} />
        <Tooltip {...tooltipEstilo} />
        <Bar dataKey="cantidad" name="Socios" fill="var(--chart-1)" radius={[6, 6, 0, 0]} maxBarSize={48} />
      </BarChart>
    )
  }

  return (
    <BarChart data={grafica.cuotasPorEstado} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
      <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
      <XAxis dataKey="estado" {...ejeEstilo} />
      <YAxis allowDecimals={false} {...ejeEstilo} />
      <Tooltip {...tooltipEstilo} />
      <Bar dataKey="cantidad" name="Cuotas" fill="var(--chart-3)" radius={[6, 6, 0, 0]} maxBarSize={48} />
    </BarChart>
  )
}

interface MetricCardProps {
  titulo: string
  valor: number
  icono: React.ReactNode
  acento: string
  detalle?: React.ReactNode
}

function MetricCard({ titulo, valor, icono, acento, detalle }: MetricCardProps) {
  return (
    <Card size="sm" className="gap-2">
      <CardHeader className="flex-row items-center justify-between space-y-0">
        <CardTitle className="text-sm font-medium text-muted-foreground">{titulo}</CardTitle>
        <div className={cn('flex h-8 w-8 shrink-0 items-center justify-center rounded-lg', acento)}>
          {icono}
        </div>
      </CardHeader>
      <CardContent>
        <p className="text-3xl font-bold tabular-nums text-foreground">{valor}</p>
        {detalle && <div className="mt-2">{detalle}</div>}
      </CardContent>
    </Card>
  )
}

function SkeletonDashboard() {
  return (
    <div className="space-y-6" aria-busy="true" aria-label="Cargando dashboard">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="h-28 animate-pulse rounded-xl bg-card ring-1 ring-foreground/10 motion-reduce:animate-none" />
        ))}
      </div>
      <div className="h-80 animate-pulse rounded-xl bg-card ring-1 ring-foreground/10 motion-reduce:animate-none" />
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div className="h-64 animate-pulse rounded-xl bg-card ring-1 ring-foreground/10 motion-reduce:animate-none" />
        <div className="h-64 animate-pulse rounded-xl bg-card ring-1 ring-foreground/10 motion-reduce:animate-none" />
      </div>
    </div>
  )
}

export default function DashboardPage() {
  const [unidadSel, setUnidadSel] = useState<string>(TODAS)
  const unidadId = unidadSel === TODAS ? undefined : unidadSel
  const { data, live, actualizadoEn } = useDashboardStream(unidadId)
  const [vista, setVista] = useState<VistaGrafica>(vistaInicial)

  const cambiarVista = (v: VistaGrafica) => {
    setVista(v)
    localStorage.setItem(GRAFICA_STORAGE_KEY, v)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <LayoutDashboard className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Dashboard</h1>
            <p className="text-sm text-muted-foreground">
              Vista operativa en tiempo real de tus sedes
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          {/* Indicador de vivo / pausa (RNF-02, E2) */}
          {live ? (
            <span className="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
              <span className="relative flex h-2 w-2" aria-hidden="true">
                <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75 motion-reduce:animate-none" />
                <span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-400" />
              </span>
              En vivo
            </span>
          ) : (
            <span className="inline-flex items-center gap-2 rounded-full border border-border bg-muted/40 px-3 py-1 text-xs font-medium text-muted-foreground">
              <Pause className="h-3 w-3" aria-hidden="true" />
              Actualización en pausa
            </span>
          )}
          {actualizadoEn && (
            <span className="text-xs text-muted-foreground tabular-nums">
              Actualizado {actualizadoEn.toLocaleTimeString('es-UY', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false })}
            </span>
          )}

          {/* Filtro por unidad (RN-14) */}
          <Select value={unidadSel} onValueChange={(v) => setUnidadSel(v ?? TODAS)}>
            <SelectTrigger className="w-[190px] bg-card border-border" aria-label="Filtrar por sede">
              <SelectValue>
                {unidadSel === TODAS
                  ? 'Todas las sedes'
                  : data?.unidades.find(u => u.id === unidadSel)?.nombre || 'Sede'}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={TODAS}>Todas las sedes</SelectItem>
              {data?.unidades.map((u) => (
                <SelectItem key={u.id} value={u.id}>{u.nombre}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {!data ? (
        <SkeletonDashboard />
      ) : (
        <>
          {/* Métricas */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <MetricCard
              titulo="Socios activos"
              valor={data.sociosActivos.total}
              icono={<Users className="h-4 w-4" />}
              acento="bg-primary/10 text-primary"
              detalle={
                data.sociosActivos.porUnidad.length > 0 && (
                  <ul className="space-y-1">
                    {data.sociosActivos.porUnidad.map((s) => (
                      <li key={s.unidadId} className="flex items-center justify-between text-xs text-muted-foreground">
                        <span className="truncate">{s.unidadNombre}</span>
                        <span className="font-medium tabular-nums text-foreground">{s.cantidad}</span>
                      </li>
                    ))}
                  </ul>
                )
              }
            />
            <MetricCard
              titulo="Próximas a vencer"
              valor={data.cuotas.proximasAVencer}
              icono={<CalendarClock className="h-4 w-4" />}
              acento="bg-[var(--chart-4)]/10 text-[var(--chart-4)]"
              detalle={<p className="text-xs text-muted-foreground">Cuotas con vencimiento en 5 días</p>}
            />
            <MetricCard
              titulo="Cuotas vencidas"
              valor={data.cuotas.vencidas}
              icono={<AlertTriangle className="h-4 w-4" />}
              acento="bg-destructive/10 text-destructive"
              detalle={
                <p className="text-xs text-muted-foreground">
                  <span className="font-medium tabular-nums text-foreground">{data.cuotas.pagadasMes}</span>{' '}
                  pagadas este mes
                </p>
              }
            />
            <MetricCard
              titulo="Clases de hoy"
              valor={data.clasesDelDia.length}
              icono={<BookOpen className="h-4 w-4" />}
              acento="bg-[var(--chart-2)]/10 text-[var(--chart-2)]"
              detalle={<p className="text-xs text-muted-foreground">Horarios programados para hoy</p>}
            />
          </div>

          {/* Gráfica con selector de vista */}
          <Card>
            <CardHeader className="flex-row flex-wrap items-center justify-between gap-3 space-y-0">
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-4 w-4 text-primary" aria-hidden="true" />
                Actividad
              </CardTitle>
              <div className="flex flex-wrap gap-1 rounded-lg border border-border bg-muted/30 p-1" role="group" aria-label="Vista de la gráfica">
                {VISTAS.map((v) => (
                  <button
                    key={v.id}
                    type="button"
                    aria-pressed={vista === v.id}
                    onClick={() => cambiarVista(v.id)}
                    className={cn(
                      'cursor-pointer rounded-md px-3 py-1.5 text-xs font-medium transition-colors',
                      vista === v.id
                        ? 'bg-primary/15 text-primary'
                        : 'text-muted-foreground hover:bg-sidebar-accent/40 hover:text-foreground',
                    )}
                  >
                    {v.label}
                  </button>
                ))}
              </div>
            </CardHeader>
            <CardContent>
              <div className="h-72 w-full" role="img" aria-label={`Gráfica: ${VISTAS.find(v => v.id === vista)?.label}`}>
                <ResponsiveContainer width="100%" height="100%">
                  <Grafica vista={vista} grafica={data.grafica} />
                </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>

          {/* Clases del día + inscripciones recientes */}
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader className="flex-row items-center gap-2 space-y-0">
                <BookOpen className="h-4 w-4 text-primary" aria-hidden="true" />
                <CardTitle>Clases del día</CardTitle>
              </CardHeader>
              <CardContent>
                {data.clasesDelDia.length === 0 ? (
                  <div className="flex flex-col items-center gap-2 py-8 text-center">
                    <BookOpen className="h-8 w-8 text-muted-foreground/50" aria-hidden="true" />
                    <p className="text-sm text-muted-foreground">No hay clases programadas para hoy.</p>
                  </div>
                ) : (
                  <ul className="divide-y divide-border">
                    {data.clasesDelDia.map((c, i) => {
                      const ocupacion = c.cupo > 0 ? Math.min(c.inscriptos / c.cupo, 1) : 0
                      return (
                        <li key={`${c.clase}-${c.horaInicio}-${i}`} className="flex items-center gap-4 py-3 first:pt-0 last:pb-0">
                          <div className="min-w-0 flex-1">
                            <p className="truncate text-sm font-medium text-foreground">{c.clase}</p>
                            <p className="truncate text-xs text-muted-foreground">{c.unidad}</p>
                          </div>
                          <span className="shrink-0 text-xs text-muted-foreground tabular-nums">
                            {c.horaInicio}–{c.horaFin}
                          </span>
                          <div className="flex w-24 shrink-0 flex-col items-end gap-1">
                            <span className="text-xs font-medium tabular-nums text-foreground">
                              {c.inscriptos}/{c.cupo}
                            </span>
                            <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted" aria-hidden="true">
                              <div
                                className={cn('h-full rounded-full transition-all', ocupacion >= 1 ? 'bg-[var(--chart-4)]' : 'bg-primary')}
                                style={{ width: `${ocupacion * 100}%` }}
                              />
                            </div>
                          </div>
                        </li>
                      )
                    })}
                  </ul>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex-row items-center gap-2 space-y-0">
                <UserPlus className="h-4 w-4 text-primary" aria-hidden="true" />
                <CardTitle>Inscripciones recientes</CardTitle>
              </CardHeader>
              <CardContent>
                {data.inscripcionesRecientes.length === 0 ? (
                  <div className="flex flex-col items-center gap-2 py-8 text-center">
                    <UserPlus className="h-8 w-8 text-muted-foreground/50" aria-hidden="true" />
                    <p className="text-sm text-muted-foreground">Todavía no hay inscripciones recientes.</p>
                  </div>
                ) : (
                  <ul className="divide-y divide-border">
                    {data.inscripcionesRecientes.map((insc, i) => (
                      <li key={`${insc.socio}-${insc.fecha}-${i}`} className="flex items-center gap-4 py-3 first:pt-0 last:pb-0">
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-medium text-foreground">{insc.socio}</p>
                          <p className="truncate text-xs text-muted-foreground">
                            {insc.clase} · {insc.unidad}
                          </p>
                        </div>
                        <span className="shrink-0 text-xs text-muted-foreground tabular-nums">
                          {formatDateTime(insc.fecha)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  )
}
