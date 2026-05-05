import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import { listarRoles, eliminarRol } from '@/services/roles'
import type { Rol } from '@/types/permisos'
import { usePermisos } from '@/hooks/usePermisos'

export default function RolesPage() {
  const [roles, setRoles] = useState<Rol[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [deleteDialog, setDeleteDialog] = useState<{ id: string; nombre: string } | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const { puedeEscribir, puedeModificar, puedeEliminar } = usePermisos()
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

  const onConfirmarEliminar = async () => {
    if (!deleteDialog) return
    try {
      await eliminarRol(deleteDialog.id)
      setDeleteDialog(null)
      setDeleteError(null)
      cargar()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } }
      setDeleteError(err?.response?.data?.error ?? 'Error al eliminar')
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
                    <Button size="sm" variant="destructive" onClick={() => { setDeleteError(null); setDeleteDialog({ id: r.id, nombre: r.nombre }) }}>
                      Eliminar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Dialog open={!!deleteDialog} onOpenChange={() => { setDeleteDialog(null); setDeleteError(null) }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Confirmar eliminación de rol</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Estás por eliminar el rol <strong className="text-foreground">{deleteDialog?.nombre}</strong>. Esta acción no se puede deshacer.
            </DialogDescription>
          </DialogHeader>
          {deleteError && (
            <p className="text-sm text-destructive">{deleteError}</p>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setDeleteDialog(null); setDeleteError(null) }} className="cursor-pointer">
              Cancelar
            </Button>
            <Button variant="destructive" onClick={onConfirmarEliminar} className="cursor-pointer">
              Eliminar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
