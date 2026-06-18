import { Link } from 'react-router-dom'
import { site } from '@/content/site'

/**
 * Stub mínimo del header público (Phase 1).
 * La nav responsive completa (links + menú mobile + botón Acceder) se construye
 * en Phase 2 (Task 6) con el plugin ui-ux-pro-max.
 */
export default function PublicHeader() {
  return (
    <header>
      <Link to="/">{site.nombre}</Link>
    </header>
  )
}
