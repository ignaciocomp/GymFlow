import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { formatDate } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import {
  listarEmpleados, darDeBajaEmpleado, reactivarEmpleado,
} from '@/services/empleados'
import { listarRoles } from '@/services/roles'
import type { Empleado } from '@/types/empleado'
import type { Rol } from '@/types/permisos'
import { usePermisos } from '@/hooks/usePermisos'

export default function UsuariosPage() {
  const [empleados, setEmpleados] = useState<Empleado[]>([])
  const [roles, setRoles] = useState<Rol[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<'activos' | 'inactivos'>('activos')

  const [bajaDialog, setBajaDialog] = useState<{ id: string; nombre: string } | null>(null)
  const [bajaError, setBajaError] = useState<string | null>(null)

  const [reactivarDialog, setReactivarDialog] = useState<Empleado | null>(null)
  const [reactivarRolId, setReactivarRolId] = useState<string>('')
  const [reactivarError, setReactivarError] = useState<string | null>(null)

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

  useEffect(() => {
    listarRoles().then(setRoles).catch(() => setRoles([]))
  }, [])

  const onConfirmarBaja = async () => {
    if (!bajaDialog) return
    try {
      await darDeBajaEmpleado(bajaDialog.id)
      setBajaDialog(null)
      setBajaError(null)
      cargar()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } }
      setBajaError(err?.response?.data?.error ?? 'Error al dar de baja')
    }
  }

  const onConfirmarReactivar = async () => {
    if (!reactivarDialog) return
    const necesitaRol = !reactivarDialog.rolId
    if (necesitaRol && !reactivarRolId) {
      setReactivarError('Debe seleccionar un rol para reactivar al empleado.')
      return
    }
    try {
      await reactivarEmpleado(reactivarDialog.id, necesitaRol ? reactivarRolId : undefined)
      setReactivarDialog(null)
      setReactivarRolId('')
      setReactivarError(null)
      cargar()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } }
      setReactivarError(err?.response?.data?.error ?? 'Error al reactivar')
    }
  }

  const rolesDisponibles = roles.filter(r => r.nombre !== 'Socio')

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
                <td className="py-3 px-4">
                  {emp.rolNombre
                    ? emp.rolNombre
                    : <span className="text-destructive text-sm">Sin rol</span>}
                </td>
                <td className="py-3 px-4 text-sm">
                  {emp.estaActivo
                    ? <span className="rounded-full bg-primary/10 text-primary px-2 py-0.5 text-xs">Activo</span>
                    : <span className="rounded-full bg-muted px-2 py-0.5 text-xs">Inactivo</span>}
                </td>
                <td className="py-3 px-4 text-sm text-muted-foreground">
                  {formatDate(emp.fechaCreacion)}
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
                    <Button size="sm" variant="destructive" onClick={() => { setBajaError(null); setBajaDialog({ id: emp.id, nombre: `${emp.nombre} ${emp.apellido}` }) }}>
                      Baja
                    </Button>
                  )}
                  {puedeEditar && !emp.estaActivo && (
                    <Button size="sm" variant="secondary" onClick={() => { setReactivarError(null); setReactivarRolId(''); setReactivarDialog(emp) }}>
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

      {/* Dialog de baja */}
      <Dialog open={!!bajaDialog} onOpenChange={() => { setBajaDialog(null); setBajaError(null) }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Confirmar baja de usuario</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Estás por dar de baja a <strong className="text-foreground">{bajaDialog?.nombre}</strong>. El usuario quedará inactivo y no podrá iniciar sesión.
            </DialogDescription>
          </DialogHeader>
          {bajaError && <p className="text-sm text-destructive">{bajaError}</p>}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setBajaDialog(null); setBajaError(null) }} className="cursor-pointer">
              Cancelar
            </Button>
            <Button variant="destructive" onClick={onConfirmarBaja} className="cursor-pointer">
              Dar de baja
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog de reactivación */}
      <Dialog open={!!reactivarDialog} onOpenChange={() => { setReactivarDialog(null); setReactivarError(null); setReactivarRolId('') }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Reactivar usuario</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Estás por reactivar a <strong className="text-foreground">{reactivarDialog?.nombre} {reactivarDialog?.apellido}</strong>.
            </DialogDescription>
          </DialogHeader>
          {!reactivarDialog?.rolId && (
            <div className="space-y-2">
              <p className="text-sm text-destructive">El rol asignado a este usuario fue eliminado. Seleccioná un nuevo rol para continuar.</p>
              {rolesDisponibles.length > 0 ? (
                <select
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={reactivarRolId}
                  onChange={e => { setReactivarRolId(e.target.value); setReactivarError(null) }}
                >
                  <option value="">Seleccionar rol...</option>
                  {rolesDisponibles.map(r => (
                    <option key={r.id} value={r.id}>{r.nombre}</option>
                  ))}
                </select>
              ) : (
                <p className="text-sm text-muted-foreground">No se pudieron cargar los roles disponibles.</p>
              )}
            </div>
          )}
          {reactivarError && <p className="text-sm text-destructive">{reactivarError}</p>}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setReactivarDialog(null); setReactivarError(null); setReactivarRolId('') }} className="cursor-pointer">
              Cancelar
            </Button>
            <Button variant="default" onClick={onConfirmarReactivar} className="cursor-pointer">
              Reactivar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
