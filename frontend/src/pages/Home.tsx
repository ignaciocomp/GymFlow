import { useQuery } from '@tanstack/react-query'
import { unidadesApi } from '@/services/api'
import { Button } from '@/components/ui/button'

export default function Home() {
  const { data: unidades, isLoading, error } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  return (
    <div className="min-h-screen bg-background p-8">
      <div className="mx-auto max-w-4xl">
        <h1 className="text-4xl font-bold tracking-tight">GymFlow</h1>
        <p className="mt-2 text-muted-foreground">
          Sistema integrado de gestión para gimnasios
        </p>

        <div className="mt-8">
          <h2 className="text-2xl font-semibold">Unidades</h2>
          {isLoading && <p className="mt-4 text-muted-foreground">Cargando...</p>}
          {error && <p className="mt-4 text-destructive">Error al cargar unidades</p>}
          {unidades && unidades.length === 0 && (
            <p className="mt-4 text-muted-foreground">No hay unidades registradas.</p>
          )}
          {unidades && unidades.length > 0 && (
            <ul className="mt-4 space-y-2">
              {unidades.map((u) => (
                <li key={u.id} className="rounded-lg border p-4">
                  <p className="font-medium">{u.nombre}</p>
                  <p className="text-sm text-muted-foreground">{u.direccion}</p>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="mt-8">
          <Button>Empezar</Button>
        </div>
      </div>
    </div>
  )
}
