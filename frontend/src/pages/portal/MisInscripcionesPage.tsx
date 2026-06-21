import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { inscripcionesApi } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { BookOpen, Loader2, CalendarDays } from 'lucide-react'
import { formatDate } from '@/lib/utils'

export default function MisInscripcionesPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const { data: inscripciones, isLoading } = useQuery({
    queryKey: ['mis-inscripciones'],
    queryFn: inscripcionesApi.getMisInscripciones,
  })

  const cancelarMutation = useMutation({
    mutationFn: (id: string) => inscripcionesApi.cancelar(id),
    onSuccess: () => {
      setError(null)
      queryClient.invalidateQueries({ queryKey: ['mis-inscripciones'] })
      queryClient.invalidateQueries({ queryKey: ['horarios-portal'] })
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { error?: string } } }
      setError(axiosErr?.response?.data?.error || 'Error al cancelar inscripción.')
    },
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <BookOpen className="h-6 w-6 text-primary" />
        <h1 className="text-2xl font-bold tracking-tight text-foreground">Mis Inscripciones</h1>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      {isLoading ? (
        <div className="flex h-32 items-center justify-center text-muted-foreground">
          Cargando inscripciones...
        </div>
      ) : inscripciones && inscripciones.length > 0 ? (
        <div className="rounded-xl border border-border bg-card overflow-hidden">
          <div className="divide-y divide-border">
            {inscripciones.map(i => (
              <div key={i.id} className="flex items-center gap-4 px-4 py-4">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-foreground">{i.claseNombre}</p>
                  <p className="text-xs text-muted-foreground">
                    {i.instructor} &middot; {i.unidadNombre}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {i.diaSemana} {i.horaInicio} - {i.horaFin}
                    {i.sala && <> &middot; {i.sala}</>}
                  </p>
                  <p className="text-xs text-muted-foreground mt-1">
                    Inscripto el {formatDate(i.fechaInscripcion)}
                  </p>
                </div>
                <div className="flex items-center gap-3 flex-shrink-0">
                  <Badge variant="secondary" className="bg-muted text-muted-foreground border-0 text-xs">
                    {i.inscripcionesActivas}/{i.capacidadMaxima}
                  </Badge>
                  <Button
                    variant="outline"
                    size="sm"
                    className="cursor-pointer"
                    disabled={cancelarMutation.isPending}
                    onClick={() => cancelarMutation.mutate(i.id)}
                  >
                    {cancelarMutation.isPending ? <Loader2 className="h-3 w-3 animate-spin mr-1" /> : null}
                    Cancelar inscripción
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </div>
      ) : (
        <div className="flex h-32 flex-col items-center justify-center gap-2 rounded-xl border border-border bg-card">
          <CalendarDays className="h-8 w-8 text-muted-foreground/50" />
          <p className="text-muted-foreground">No tenés inscripciones activas.</p>
        </div>
      )}
    </div>
  )
}
