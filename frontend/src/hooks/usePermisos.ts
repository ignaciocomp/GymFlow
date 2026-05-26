import { useAuth } from '@/context/AuthContext'
import type { Modulo } from '@/types/permisos'

export function usePermisos() {
  const { tienePermiso } = useAuth()
  return {
    tienePermiso,
    puedeLeer: (m: Modulo) => tienePermiso(m, 'Lectura'),
    puedeEscribir: (m: Modulo) => tienePermiso(m, 'Escritura'),
    puedeModificar: (m: Modulo) => tienePermiso(m, 'Modificacion'),
    puedeEliminar: (m: Modulo) => tienePermiso(m, 'Eliminacion'),
  }
}
