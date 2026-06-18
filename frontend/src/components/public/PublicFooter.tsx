import { site } from '@/content/site'

/**
 * Stub mínimo del footer público (Phase 1).
 * El footer completo (marca, links rápidos, sedes, redes, copyright) se
 * construye en Phase 2 (Task 6) con el plugin ui-ux-pro-max.
 */
export default function PublicFooter() {
  return (
    <footer>
      <p>
        © {new Date().getFullYear()} {site.nombre}
      </p>
    </footer>
  )
}
