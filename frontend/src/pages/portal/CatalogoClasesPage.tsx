import { useState, useMemo, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { clasesApi, unidadesApi, inscripcionesApi } from '@/services/api'
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
import { BookOpen, Loader2, CheckCircle2, X } from 'lucide-react'
import type { Clase, InscripcionClase } from '@/types'

export default function CatalogoClasesPage() {
  const queryClient = useQueryClient()
  const [unidadFilter, setUnidadFilter] = useState<string>('all')
  const [search, setSearch] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [successToast, setSuccessToast] = useState<string | null>(null)

  // Auto-dismiss del toast de éxito después de 3.5s
  useEffect(() => {
    if (!successToast) return
    const timer = setTimeout(() => setSuccessToast(null), 3500)
    return () => clearTimeout(timer)
  }, [successToast])

  const { data: clases, isLoading: clasesLoading } = useQuery({
    queryKey: ['clases', 'catalogo'],
    queryFn: () => clasesApi.getAll(undefined, false),
  })

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const { data: misInscripciones } = useQuery({
    queryKey: ['mis-inscripciones'],
    queryFn: inscripcionesApi.getMisInscripciones,
  })

  const inscriptoIds = useMemo(
    () => new Set((misInscripciones ?? []).map((i) => i.claseId)),
    [misInscripciones]
  )

  const inscribirseMutation = useMutation({
    mutationFn: (claseId: string) => inscripcionesApi.inscribirse(claseId),
    onSuccess: (insc: InscripcionClase) => {
      setError(null)
      setSuccessToast(
        insc.enListaEspera ? 'Quedaste en lista de espera' : 'Inscripción confirmada'
      )
      queryClient.invalidateQueries({ queryKey: ['clases', 'catalogo'] })
      queryClient.invalidateQueries({ queryKey: ['mis-inscripciones'] })
      queryClient.invalidateQueries({ queryKey: ['horarios-portal'] })
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { error?: string } } }
      setError(axiosErr?.response?.data?.error || 'Error al inscribirse en la clase.')
    },
  })

  const clasesFiltradas = useMemo(() => {
    const term = search.trim().toLowerCase()
    return (clases ?? []).filter((c) => {
      if (unidadFilter !== 'all' && c.unidadId !== unidadFilter) return false
      if (term && !c.nombre.toLowerCase().includes(term)) return false
      return true
    })
  }, [clases, unidadFilter, search])

  const cuposDisponibles = (c: Clase) => c.capacidadMaxima - c.inscripcionesActivas

  function renderAccion(c: Clase) {
    const yaInscripto = inscriptoIds.has(c.id)
    const pending =
      inscribirseMutation.isPending && inscribirseMutation.variables === c.id
    const cupos = cuposDisponibles(c)

    if (yaInscripto) {
      return (
        <Button size="sm" variant="outline" disabled className="cursor-not-allowed">
          Ya estás inscripto
        </Button>
      )
    }

    if (cupos > 0) {
      return (
        <Button
          size="sm"
          className="cursor-pointer"
          disabled={pending}
          onClick={() => inscribirseMutation.mutate(c.id)}
        >
          {pending ? <Loader2 className="h-3 w-3 animate-spin mr-1" /> : null}
          Inscribirme
        </Button>
      )
    }

    return (
      <Button
        size="sm"
        variant="outline"
        className="cursor-pointer border-amber-500/50 text-amber-600 hover:bg-amber-500/10 dark:text-amber-400"
        disabled={pending}
        onClick={() => inscribirseMutation.mutate(c.id)}
      >
        {pending ? <Loader2 className="h-3 w-3 animate-spin mr-1" /> : null}
        Anotarme en lista de espera
      </Button>
    )
  }

  const emptyState = !clasesLoading && clasesFiltradas.length === 0

  return (
    <div className="space-y-6">
      {/* Toast flotante de éxito (auto-dismiss a los 3.5s) */}
      {successToast && (
        <div
          role="status"
          aria-live="polite"
          className="fixed bottom-6 right-6 z-50 flex items-center gap-3 rounded-xl border border-green-500/40 bg-green-500/10 px-4 py-3 shadow-lg backdrop-blur animate-in slide-in-from-bottom-4 fade-in"
        >
          <CheckCircle2 className="h-5 w-5 text-green-500 shrink-0" />
          <p className="text-sm text-green-200 font-medium">{successToast}</p>
          <button
            onClick={() => setSuccessToast(null)}
            className="ml-2 text-green-300/70 hover:text-green-200 cursor-pointer"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <BookOpen className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Clases disponibles</h1>
          <p className="text-sm text-muted-foreground">
            Inscribite en las clases de tu sede
          </p>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      {/* Filtros */}
      <div className="flex flex-wrap items-center gap-3">
        <Select value={unidadFilter} onValueChange={(val) => setUnidadFilter(val ?? 'all')}>
          <SelectTrigger className="w-[200px] bg-card border-border">
            <SelectValue>
              {unidadFilter === 'all'
                ? 'Todas las sedes'
                : unidades?.find((u) => u.id === unidadFilter)?.nombre || 'Sede'}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todas las sedes</SelectItem>
            {unidades?.map((u) => (
              <SelectItem key={u.id} value={u.id}>
                {u.nombre}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          placeholder="Buscar por nombre..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-[240px]"
        />
      </div>

      {/* Vista tabla (sm+) */}
      <div className="hidden sm:block rounded-xl border border-border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="border-border hover:bg-transparent">
              <TableHead className="text-muted-foreground">Clase</TableHead>
              <TableHead className="text-muted-foreground">Instructor</TableHead>
              <TableHead className="text-muted-foreground">Sede</TableHead>
              <TableHead className="text-muted-foreground">Cupos</TableHead>
              <TableHead className="text-muted-foreground text-right">Acción</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {clasesLoading && (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center text-muted-foreground">
                  Cargando...
                </TableCell>
              </TableRow>
            )}
            {emptyState && (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center text-muted-foreground">
                  No se encontraron clases con los filtros actuales.
                </TableCell>
              </TableRow>
            )}
            {clasesFiltradas.map((c) => (
              <TableRow key={c.id} className="border-border">
                <TableCell className="font-medium">{c.nombre}</TableCell>
                <TableCell>{c.instructor}</TableCell>
                <TableCell>{c.unidadNombre}</TableCell>
                <TableCell>
                  {Math.max(0, cuposDisponibles(c))} / {c.capacidadMaxima}
                </TableCell>
                <TableCell className="text-right">{renderAccion(c)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {/* Vista cards (mobile only) */}
      <div className="sm:hidden space-y-3">
        {clasesLoading && (
          <div className="rounded-xl border bg-card p-6 text-center text-muted-foreground">
            Cargando...
          </div>
        )}
        {emptyState && (
          <div className="rounded-xl border bg-card p-6 text-center text-muted-foreground">
            No se encontraron clases con los filtros actuales.
          </div>
        )}
        {clasesFiltradas.map((c) => (
          <div key={c.id} className="rounded-xl border bg-card p-4 space-y-3">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="font-medium text-foreground">{c.nombre}</p>
                <p className="text-xs text-muted-foreground">
                  {c.instructor} &middot; {c.unidadNombre}
                </p>
              </div>
            </div>
            <div className="text-sm">
              <p className="text-xs uppercase tracking-wider text-muted-foreground">Cupos</p>
              <p className="font-medium">
                {Math.max(0, cuposDisponibles(c))} / {c.capacidadMaxima}
              </p>
            </div>
            <div className="[&_button]:w-full">{renderAccion(c)}</div>
          </div>
        ))}
      </div>
    </div>
  )
}
