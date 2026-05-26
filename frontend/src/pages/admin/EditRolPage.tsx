import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import RolForm from './RolForm'
import { obtenerRol, actualizarRol } from '@/services/roles'
import type { Rol } from '@/types/permisos'

export default function EditRolPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [rol, setRol] = useState<Rol | null>(null)

  useEffect(() => {
    if (id) obtenerRol(id).then(setRol)
  }, [id])

  if (!rol) return <div className="p-6">Cargando…</div>
  if (rol.esSistema) return <div className="p-6">Este rol es del sistema y no puede editarse.</div>

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Editar rol</h1>
      <RolForm
        initialNombre={rol.nombre}
        initialPermisoIds={rol.permisos.map(p => p.id)}
        onSubmit={async data => {
          await actualizarRol(rol.id, data)
          navigate('/admin/roles')
        }}
        submitLabel="Guardar cambios"
      />
    </div>
  )
}
