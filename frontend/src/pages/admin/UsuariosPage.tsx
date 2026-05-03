import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import {
  listarEmpleados,
  darDeBajaEmpleado,
  reactivarEmpleado,
} from '@/services/empleados'
import type { Empleado } from '@/types/empleado'
import { usePermisos } from '@/hooks/usePermisos'

export default function UsuariosPage() {
  const [empleados, setEmpleados] = useState<Empleado[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<'activos' | 'inactivos'>('activos')
  const { puedeEscribir, puedeModificar, puedeEliminar } = usePermisos()
  const puedeCrear = puedeEscribir('Empleados')
  const puedeEditar = puedeModificar('Empleados')
  const puedeBorrar = puedeEliminar('Empleados')

  const cargar = () => {
    setLoading(true)
    setError(null)
    listarEmpleados(tab === 'activos')
      .then(setEmpleados)
      .catch(e => setError(e?.response?.data?.error ?? 'Error al cargar usuarios'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { cargar() }, [tab])

  const onBaja = async (id: string, nombre: string) => {
    if (!confirm(`¿Dar de baja a "${nombre}"?`)) return
    try {
      await darDeBajaEmpleado(id)
      cargar()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } }
      alert(err?.response?.data?.error ?? 'Error al dar de baja')
    }
  }

  const onReactivar = async (id: string) => {
    try {
      await reactivarEmpleado(id)
      cargar()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } }
      alert(err?.response?.data?.error ?? 'Error al reactivar')
    }
  }

  if (loading) return <div className="p-6">Cargando...</div>
  if (error) return <div className="p-6 text-destructive">{error}</div>

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Usuarios</h1>
        {puedeCrear && (
          <Link to="/admin/usuarios/nuevo"><Button>Nuevo usuario</Button></Link>
        )}
      </div>

      <div className="flex gap-2">
        <Button variant={tab === 'activos' ? 'default' : 'outline'} size="sm" onClick={() => setTab('activos')}>
          Activos
        </Button>
        <Button variant={tab === 'inactivos' ? 'default' : 'outline'} size="sm" onClick={() => setTab('inactivos')}>
          Inactivos
        </Button>
      </div>

      <div className="rounded-lg border border-border bg-card">
        <table className="w-full">
          <thead>
            <tr className="border-b border-border">
              <th className="text-left py-3 px-4 text-sm font-medium">Nombre</th>
              <th className="text-left py-3 px-4 text-sm font-medium">Correo</th>
              <th className="text-left py-3 px-4 text-sm font-medium">Rol</th>
              <th className="text-left py-3 px-4 text-sm font-medium">Estado</th>
              <th className="text-left py-3 px-4 text-sm font-medium">Alta</th>
              <th className="py-3 px-4" />
            </tr>
          </thead>
          <tbody>
            {empleados.map(emp => (
              <tr key={emp.id} className="border-b border-border last:border-0">
                <td className="py-3 px-4">{emp.apellido}, {emp.nombre}</td>
                <td className="py-3 px-4 text-sm text-muted-foreground">{emp.correo}</td>
                <td className="py-3 px-4">{emp.rolNombre}</td>
                <td className="py-3 px-4 text-sm">
                  {emp.estaActivo
                    ? <span className="rounded-full bg-primary/10 text-primary px-2 py-0.5 text-xs">Activo</span>
                    : <span className="rounded-full bg-muted px-2 py-0.5 text-xs">Inactivo</span>}
                </td>
                <td className="py-3 px-4 text-sm text-muted-foreground">
                  {new Date(emp.fechaCreacion).toLocaleDateString('es-UY')}
                </td>
                <td className="py-3 px-4 text-right space-x-2">
                  {puedeEditar && (
                    <>
                      <Link to={`/admin/usuarios/${emp.id}/editar`}>
                        <Button size="sm" variant="outline">Editar</Button>
                      </Link>
                      <Link to={`/admin/usuarios/${emp.id}/password`}>
                        <Button size="sm" variant="outline">Password</Button>
                      </Link>
                    </>
                  )}
                  {puedeBorrar && emp.estaActivo && (
                    <Button size="sm" variant="destructive" onClick={() => onBaja(emp.id, `${emp.nombre} ${emp.apellido}`)}>
                      Baja
                    </Button>
                  )}
                  {puedeEditar && !emp.estaActivo && (
                    <Button size="sm" variant="secondary" onClick={() => onReactivar(emp.id)}>
                      Reactivar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {empleados.length === 0 && (
              <tr>
                <td colSpan={6} className="py-8 px-4 text-center text-sm text-muted-foreground">
                  No hay usuarios {tab === 'activos' ? 'activos' : 'inactivos'}.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
