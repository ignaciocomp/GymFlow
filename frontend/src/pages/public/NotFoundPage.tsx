import { Link } from 'react-router-dom'

/** 404 público (Phase 1 stub). El diseño real se hace en Phase 2. */
export default function NotFoundPage() {
  return (
    <div>
      <p>Página no encontrada</p>
      <Link to="/">Volver al inicio</Link>
    </div>
  )
}
