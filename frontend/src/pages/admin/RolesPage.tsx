import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { listarRoles, eliminarRol } from '@/services/roles'
import type { Rol } from '@/types/permisos'
import { usePermisos } from '@/hooks/usePermisos'

export default function RolesPage() {
  const [roles, setRoles] = useState<Rol[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { puedeEscribir, puedeModificar, puedeEliminar } = usePermisos()
  // Reutilizamos Modulo.Auditoria como "permiso de gestión administrativa"
  const puedeCrear = puedeEscribir('Auditoria')
  const puedeEditar = puedeModificar('Auditoria')
  const puedeBorrar = puedeEliminar('Auditoria')

  const cargar = () => {
    setLoading(true)
    listarRoles()
      .then(setRoles)
      .catch(e => setError(e?.response?.data?.error ?? 'Error al cargar roles'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { cargar() }, [])

  const onEliminar = async (id: string, nombre: string) => {
    if (!confirm(`¿Eliminar el rol "${nombre}"?`)) return
    try {
      await eliminarRol(id)
      cargar()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } }
      alert(err?.response?.data?.error ?? 'Error al eliminar')
    }
  }

  if (loading) return <div className="p-6">Cargando…</div>
  if (error) return <div className="p-6 text-destructive">{error}</div>

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Roles</h1>
        {puedeCrear && (
          <Link to="/admin/roles/nuevo"><Button>Nuevo rol</Button></Link>
        )}
      </div>
      <div className="rounded-lg border border-border bg-card">
        <table className="w-full">
          <thead>
            <tr className="border-b border-border">
              <th className="text-left py-3 px-4 text-sm font-medium">Nombre</th>
              <th className="text-left py-3 px-4 text-sm font-medium">Tipo</th>
              <th className="text-left py-3 px-4 text-sm font-medium">Permisos</th>
              <th className="py-3 px-4" />
            </tr>
          </thead>
          <tbody>
            {roles.map(r => (
              <tr key={r.id} className="border-b border-border last:border-0">
                <td className="py-3 px-4">{r.nombre}</td>
                <td className="py-3 px-4 text-sm">
                  {r.esSistema
                    ? <span className="rounded-full bg-muted px-2 py-0.5 text-xs">Sistema</span>
                    : <span className="rounded-full bg-primary/10 text-primary px-2 py-0.5 text-xs">Personalizado</span>}
                </td>
                <td className="py-3 px-4 text-sm text-muted-foreground">{r.permisos.length}</td>
                <td className="py-3 px-4 text-right space-x-2">
                  {puedeEditar && !r.esSistema && (
                    <Link to={`/admin/roles/${r.id}/editar`}>
                      <Button size="sm" variant="outline">Editar</Button>
                    </Link>
                  )}
                  {puedeBorrar && !r.esSistema && (
                    <Button size="sm" variant="destructive" onClick={() => onEliminar(r.id, r.nombre)}>
                      Eliminar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
