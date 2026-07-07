import { Navigate } from 'react-router-dom'
import { usePermisos } from '@/hooks/usePermisos'

/**
 * RF-18: el dashboard es la pantalla de inicio del panel admin para quien tiene el
 * permiso Dashboard-Lectura; el resto sigue aterrizando en Socios como hasta ahora.
 */
export default function AdminIndexRedirect() {
  const { puedeLeer } = usePermisos()
  return <Navigate to={puedeLeer('Dashboard') ? '/admin/dashboard' : '/admin/socios'} replace />
}
