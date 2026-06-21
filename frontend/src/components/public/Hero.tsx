import { Link } from 'react-router-dom'
import { ArrowRight, MapPin, Dumbbell } from 'lucide-react'
import { site } from '@/content/site'
import PublicImage from '@/components/public/PublicImage'

/**
 * Hero de la home: tagline grande, descripción, 2 CTAs (Ver planes / Conocé las
 * sedes) y un área de imagen (placeholder hasta tener la foto real).
 */
export default function Hero() {
  return (
    <section className="relative overflow-hidden bg-[var(--public-bg)]">
      {/* glow decorativo de fondo */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-32 right-0 h-96 w-96 rounded-full bg-[var(--public-accent)] opacity-10 blur-3xl"
      />
      <div className="mx-auto grid max-w-6xl items-center gap-12 px-4 py-20 sm:px-6 lg:grid-cols-2 lg:py-28">
        <div>
          <p className="inline-flex items-center gap-2 rounded-full border border-[var(--public-border)] bg-[var(--public-surface)] px-4 py-1.5 text-sm font-medium text-[var(--public-muted)]">
            <Dumbbell className="h-4 w-4 text-[var(--public-accent)]" aria-hidden="true" />
            Gimnasio multi-sede en Montevideo
          </p>
          <h1 className="mt-6 text-4xl font-extrabold leading-[1.05] tracking-tight text-[var(--public-text)] sm:text-5xl lg:text-6xl">
            {site.tagline}
          </h1>
          <p className="mt-6 max-w-xl text-lg leading-relaxed text-[var(--public-muted)]">
            {site.descripcion}
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Link
              to="/planes"
              className="inline-flex items-center justify-center gap-2 rounded-lg bg-[var(--public-accent)] px-6 py-3 text-base font-semibold text-[var(--public-accent-ink)] transition-colors hover:bg-[var(--public-accent-hover)]"
            >
              Ver planes
              <ArrowRight className="h-5 w-5" aria-hidden="true" />
            </Link>
            <Link
              to="/sedes"
              className="inline-flex items-center justify-center gap-2 rounded-lg border border-[var(--public-border)] bg-transparent px-6 py-3 text-base font-semibold text-[var(--public-text)] transition-colors hover:bg-[var(--public-surface-2)]"
            >
              <MapPin className="h-5 w-5" aria-hidden="true" />
              Conocé las sedes
            </Link>
          </div>
        </div>

        {/* Imagen del hero (placeholder hasta tener la foto real) */}
        <div className="relative">
          <PublicImage
            // TODO (usuario): subir la foto del hero a /img/hero.jpg
            src="/img/hero.jpg"
            alt={`Sala de entrenamiento de ${site.nombre}`}
            icon={Dumbbell}
            label="Foto del gimnasio"
            className="aspect-[4/3] rounded-2xl border border-[var(--public-border)] shadow-2xl"
          />
        </div>
      </div>
    </section>
  )
}
