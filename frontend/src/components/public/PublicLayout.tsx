import { Outlet } from 'react-router-dom'
import PublicHeader from '@/components/public/PublicHeader'
import PublicFooter from '@/components/public/PublicFooter'

/**
 * Layout del sitio web público (RF-19).
 *
 * Envuelve las rutas públicas con header + footer y aplica el tema oscuro
 * propio vía la clase `.public-site` (tokens en index.css). No usa los layouts
 * protegidos, así que las rutas que cuelgan de acá son públicas por construcción
 * (no tocan la auth).
 */
export default function PublicLayout() {
  return (
    <div className="public-site">
      <PublicHeader />
      <main>
        <Outlet />
      </main>
      <PublicFooter />
    </div>
  )
}
