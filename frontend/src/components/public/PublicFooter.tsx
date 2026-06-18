import { Link } from 'react-router-dom'
import { Dumbbell, Camera, MapPin, Mail } from 'lucide-react'
import { site } from '@/content/site'

/**
 * Footer del sitio público (RF-19).
 *
 * Marca + tagline, links rápidos a las páginas, las 2 sedes (nombre + dirección
 * desde `site`), contacto (instagram + mail) y copyright con el año actual.
 * Todo el contenido sale de `content/site.ts`.
 */
const QUICK_LINKS = [
  { to: '/sedes', label: 'Sedes' },
  { to: '/planes', label: 'Planes' },
  { to: '/clases', label: 'Clases' },
  { to: '/contacto', label: 'Contacto' },
] as const

export default function PublicFooter() {
  const year = new Date().getFullYear()

  return (
    <footer className="border-t border-[var(--public-border)] bg-[var(--public-bg)] text-[var(--public-muted)]">
      <div className="mx-auto max-w-6xl px-4 py-12 sm:px-6">
        <div className="grid gap-10 md:grid-cols-4">
          {/* Marca */}
          <div className="md:col-span-1">
            <Link
              to="/"
              className="flex items-center gap-2 text-lg font-extrabold tracking-tight text-[var(--public-text)]"
            >
              <span
                className="grid h-8 w-8 place-items-center rounded-lg bg-[var(--public-accent)] text-[var(--public-accent-ink)]"
                aria-hidden="true"
              >
                <Dumbbell className="h-4 w-4" strokeWidth={2.5} />
              </span>
              Gym<span className="-ml-2 text-[var(--public-accent)]">FLOW</span>
            </Link>
            <p className="mt-4 max-w-xs text-sm leading-relaxed">{site.tagline}.</p>
          </div>

          {/* Links rápidos */}
          <nav aria-label="Enlaces del pie" className="md:col-span-1">
            <h2 className="text-sm font-semibold uppercase tracking-wider text-[var(--public-text)]">
              Navegación
            </h2>
            <ul className="mt-4 space-y-2 text-sm">
              {QUICK_LINKS.map((l) => (
                <li key={l.to}>
                  <Link
                    to={l.to}
                    className="transition-colors hover:text-[var(--public-accent)]"
                  >
                    {l.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>

          {/* Sedes */}
          <div className="md:col-span-1">
            <h2 className="text-sm font-semibold uppercase tracking-wider text-[var(--public-text)]">
              Sedes
            </h2>
            <ul className="mt-4 space-y-3 text-sm">
              {site.sedes.map((sede) => (
                <li key={sede.slug} className="flex gap-2">
                  <MapPin
                    className="mt-0.5 h-4 w-4 shrink-0 text-[var(--public-accent)]"
                    aria-hidden="true"
                  />
                  <span>
                    <span className="block font-medium text-[var(--public-text)]">
                      {sede.nombre}
                    </span>
                    <span className="block">{sede.direccion}</span>
                  </span>
                </li>
              ))}
            </ul>
          </div>

          {/* Contacto / redes */}
          <div className="md:col-span-1">
            <h2 className="text-sm font-semibold uppercase tracking-wider text-[var(--public-text)]">
              Seguinos
            </h2>
            <ul className="mt-4 space-y-3 text-sm">
              <li>
                <a
                  href={site.contacto.instagram}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-2 transition-colors hover:text-[var(--public-accent)]"
                >
                  <Camera className="h-4 w-4" aria-hidden="true" />
                  Instagram
                </a>
              </li>
              <li>
                <a
                  href={`mailto:${site.contacto.email}`}
                  className="inline-flex items-center gap-2 transition-colors hover:text-[var(--public-accent)]"
                >
                  <Mail className="h-4 w-4" aria-hidden="true" />
                  {site.contacto.email}
                </a>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-10 border-t border-[var(--public-border)] pt-6 text-xs">
          © {year} {site.nombre}. Todos los derechos reservados.
        </div>
      </div>
    </footer>
  )
}
