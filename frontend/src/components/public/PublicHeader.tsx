import { useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { Menu, X, Dumbbell } from 'lucide-react'

/**
 * Header del sitio público (RF-19).
 *
 * - Sticky top bar con la marca "GymFlow" (acento celeste en "FLOW").
 * - Nav de escritorio con los 5 links + botón "Acceder" → /login.
 * - En mobile colapsa a un menú hamburguesa (estado `useState`, accesible
 *   por teclado: el toggle es un <button> con aria-expanded/aria-controls).
 *
 * Todo el contenido y las rutas son fijas del sitio (no salen de site.ts porque
 * son la navegación del propio sitio, no contenido editable).
 */
const NAV_LINKS = [
  { to: '/', label: 'Inicio', end: true },
  { to: '/sedes', label: 'Sedes' },
  { to: '/planes', label: 'Planes' },
  { to: '/clases', label: 'Clases' },
  { to: '/contacto', label: 'Contacto' },
] as const

export default function PublicHeader() {
  const [open, setOpen] = useState(false)

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    [
      'rounded-md px-3 py-2 text-sm font-medium transition-colors',
      isActive
        ? 'text-[var(--public-accent)]'
        : 'text-[var(--public-muted)] hover:text-[var(--public-text)]',
    ].join(' ')

  const mobileLinkClass = ({ isActive }: { isActive: boolean }) =>
    [
      'block rounded-lg px-4 py-3 text-base font-medium transition-colors',
      isActive
        ? 'bg-[var(--public-surface-2)] text-[var(--public-accent)]'
        : 'text-[var(--public-text)] hover:bg-[var(--public-surface-2)]',
    ].join(' ')

  return (
    <header className="sticky top-0 z-40 border-b border-[var(--public-border)] bg-[var(--public-bg)]/85 backdrop-blur supports-[backdrop-filter]:bg-[var(--public-bg)]/70">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4 sm:px-6">
        {/* Marca */}
        <Link
          to="/"
          className="flex items-center gap-2 text-xl font-extrabold tracking-tight text-[var(--public-text)]"
          onClick={() => setOpen(false)}
        >
          <span
            className="grid h-9 w-9 place-items-center rounded-lg bg-[var(--public-accent)] text-[var(--public-accent-ink)]"
            aria-hidden="true"
          >
            <Dumbbell className="h-5 w-5" strokeWidth={2.5} />
          </span>
          <span>
            Gym<span className="text-[var(--public-accent)]">FLOW</span>
          </span>
        </Link>

        {/* Nav escritorio */}
        <nav
          aria-label="Navegación principal"
          className="hidden items-center gap-1 md:flex"
        >
          {NAV_LINKS.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.end} className={linkClass}>
              {l.label}
            </NavLink>
          ))}
        </nav>

        {/* Acción escritorio */}
        <div className="hidden md:block">
          <Link
            to="/login"
            className="inline-flex items-center justify-center rounded-lg bg-[var(--public-accent)] px-4 py-2 text-sm font-semibold text-[var(--public-accent-ink)] transition-colors hover:bg-[var(--public-accent-hover)]"
          >
            Acceder
          </Link>
        </div>

        {/* Toggle mobile */}
        <button
          type="button"
          className="inline-flex h-11 w-11 items-center justify-center rounded-lg text-[var(--public-text)] hover:bg-[var(--public-surface-2)] md:hidden"
          aria-label={open ? 'Cerrar menú' : 'Abrir menú'}
          aria-expanded={open}
          aria-controls="public-mobile-menu"
          onClick={() => setOpen((v) => !v)}
        >
          {open ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
        </button>
      </div>

      {/* Menú mobile */}
      {open && (
        <div
          id="public-mobile-menu"
          className="border-t border-[var(--public-border)] bg-[var(--public-bg)] md:hidden"
        >
          <nav aria-label="Navegación principal móvil" className="space-y-1 px-4 py-4">
            {NAV_LINKS.map((l) => (
              <NavLink
                key={l.to}
                to={l.to}
                end={l.end}
                className={mobileLinkClass}
                onClick={() => setOpen(false)}
              >
                {l.label}
              </NavLink>
            ))}
            <Link
              to="/login"
              className="mt-2 block rounded-lg bg-[var(--public-accent)] px-4 py-3 text-center text-base font-semibold text-[var(--public-accent-ink)] transition-colors hover:bg-[var(--public-accent-hover)]"
              onClick={() => setOpen(false)}
            >
              Acceder
            </Link>
          </nav>
        </div>
      )}
    </header>
  )
}
