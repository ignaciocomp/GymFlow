import { MapPin, Clock, CheckCircle2, ExternalLink, Dumbbell } from 'lucide-react'
import type { Sede } from '@/content/site'
import PublicImage from '@/components/public/PublicImage'

/**
 * Tarjeta de una sede: foto (placeholder), nombre, dirección, horarios,
 * servicios y un enlace al mapa (`mapsUrl`).
 */
export default function SedeCard({ sede }: { sede: Sede }) {
  return (
    <article className="overflow-hidden rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)]">
      <PublicImage
        src={sede.foto}
        alt={`Sede ${sede.nombre}`}
        icon={Dumbbell}
        label="Foto de la sede"
        className="aspect-[16/9]"
      />

      <div className="p-6 sm:p-8">
        <h2 className="text-2xl font-bold text-[var(--public-text)]">{sede.nombre}</h2>

        <p className="mt-2 flex items-start gap-2 text-[var(--public-muted)]">
          <MapPin className="mt-0.5 h-5 w-5 shrink-0 text-[var(--public-accent)]" aria-hidden="true" />
          <span>{sede.direccion}</span>
        </p>

        {/* Horarios */}
        <div className="mt-6">
          <h3 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-wider text-[var(--public-text)]">
            <Clock className="h-4 w-4 text-[var(--public-accent)]" aria-hidden="true" />
            Horarios
          </h3>
          <dl className="mt-3 space-y-1.5 text-sm">
            {sede.horarios.map((h) => (
              <div key={h.dias} className="flex justify-between gap-4">
                <dt className="text-[var(--public-muted)]">{h.dias}</dt>
                <dd className="font-medium tabular-nums text-[var(--public-text)]">{h.horas}</dd>
              </div>
            ))}
          </dl>
        </div>

        {/* Servicios */}
        <div className="mt-6">
          <h3 className="text-sm font-semibold uppercase tracking-wider text-[var(--public-text)]">
            Servicios
          </h3>
          <ul className="mt-3 grid gap-2 sm:grid-cols-2">
            {sede.servicios.map((s) => (
              <li key={s} className="flex items-center gap-2 text-sm text-[var(--public-muted)]">
                <CheckCircle2 className="h-4 w-4 shrink-0 text-[var(--public-accent)]" aria-hidden="true" />
                {s}
              </li>
            ))}
          </ul>
        </div>

        {/* Mapa */}
        <a
          href={sede.mapsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="mt-8 inline-flex items-center gap-2 rounded-lg border border-[var(--public-border)] px-4 py-2.5 text-sm font-semibold text-[var(--public-text)] transition-colors hover:bg-[var(--public-surface-2)]"
        >
          <MapPin className="h-4 w-4 text-[var(--public-accent)]" aria-hidden="true" />
          Ver en Google Maps
          <ExternalLink className="h-4 w-4 opacity-60" aria-hidden="true" />
        </a>
      </div>
    </article>
  )
}
