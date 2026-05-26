import { useNavigate } from 'react-router-dom'
import RolForm from './RolForm'
import { crearRol } from '@/services/roles'

export default function NuevoRolPage() {
  const navigate = useNavigate()
  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Nuevo rol</h1>
      <RolForm
        onSubmit={async data => {
          await crearRol(data)
          navigate('/admin/roles')
        }}
        submitLabel="Crear"
      />
    </div>
  )
}
