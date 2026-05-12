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

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Roles</h1>
        {puedeCrear && (
          <Link to="/admin/roles/nuevo"><Button>Nuevo rol</Button></Link>
        )}
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {/* Tabla — visible en sm+ con scroll horizontal */}
      <div className="hidden sm:block rounded-lg border border-border bg-card overflow-x-auto">
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
            {loading && (
              <tr>
                <td colSpan={4} className="py-8 px-4 text-center text-sm text-muted-foreground">
                  Cargando roles...
                </td>
              </tr>
            )}
            {!loading && roles.map(r => (
              <tr key={r.id} className="border-b border-border last:border-0">
                <td className="py-3 px-4 whitespace-nowrap">{r.nombre}</td>
                <td className="py-3 px-4 text-sm">
                  {r.esSistema
                    ? <span className="rounded-full bg-muted px-2 py-0.5 text-xs whitespace-nowrap">Sistema</span>
                    : <span className="rounded-full bg-primary/10 text-primary px-2 py-0.5 text-xs whitespace-nowrap">Personalizado</span>}
                </td>
                <td className="py-3 px-4 text-sm text-muted-foreground">{r.permisos.length}</td>
                <td className="py-3 px-4 text-right whitespace-nowrap space-x-2">
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
            {!loading && roles.length === 0 && (
              <tr>
                <td colSpan={4} className="py-8 px-4 text-center text-sm text-muted-foreground">
                  No hay roles configurados.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Cards (mobile only) */}
      <div className="sm:hidden space-y-3">
        {loading && (
          <div className="rounded-lg border bg-card p-6 text-center text-muted-foreground">
            Cargando roles...
          </div>
        )}
        {!loading && roles.length === 0 && (
          <div className="rounded-lg border bg-card p-6 text-center text-muted-foreground">
            No hay roles configurados.
          </div>
        )}
        {!loading && roles.map(r => (
          <div key={r.id} className="rounded-lg border bg-card p-4 space-y-3">
            <div className="flex items-start justify-between gap-2">
              <p className="font-medium">{r.nombre}</p>
              {r.esSistema
                ? <span className="rounded-full bg-muted px-2 py-0.5 text-xs whitespace-nowrap shrink-0">Sistema</span>
                : <span className="rounded-full bg-primary/10 text-primary px-2 py-0.5 text-xs whitespace-nowrap shrink-0">Personalizado</span>}
            </div>
            <div className="text-sm text-muted-foreground">
              {r.permisos.length} {r.permisos.length === 1 ? 'permiso' : 'permisos'}
            </div>
            {(puedeEditar || puedeBorrar) && !r.esSistema && (
              <div className="flex flex-wrap gap-2 pt-1">
                {puedeEditar && (
                  <Link to={`/admin/roles/${r.id}/editar`} className="flex-1">
                    <Button size="sm" variant="outline" className="w-full">Editar</Button>
                  </Link>
                )}
                {puedeBorrar && (
                  <Button
                    size="sm"
                    variant="destructive"
                    className="flex-1"
                    onClick={() => { setDeleteError(null); setDeleteDialog({ id: r.id, nombre: r.nombre }) }}
                  >
                    Eliminar
                  </Button>
                )}
              </div>
            )}
          </div>
        ))}
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
