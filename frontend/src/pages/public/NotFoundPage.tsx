import { Link } from 'react-router-dom'
import { Home, Dumbbell } from 'lucide-react'
import Seo from '@/components/public/Seo'
import { site } from '@/content/site'

/** 404 público con diseño y link al inicio. */
export default function NotFoundPage() {
  return (
    <>
      <Seo
        title={`Página no encontrada — ${site.nombre}`}
        description="La página que buscás no existe o fue movida."
        path="/404"
      />
      <section className="relative flex min-h-[70vh] items-center justify-center overflow-hidden bg-[var(--public-bg)] px-4 py-20">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute top-0 left-1/2 h-96 w-96 -translate-x-1/2 rounded-full bg-[var(--public-accent)] opacity-10 blur-3xl"
        />
        <div className="relative text-center">
          <span
            className="mx-auto grid h-16 w-16 place-items-center rounded-2xl bg-[var(--public-accent)] text-[var(--public-accent-ink)]"
            aria-hidden="true"
          >
            <Dumbbell className="h-8 w-8" strokeWidth={2.5} />
          </span>
          <p className="mt-8 text-7xl font-extrabold tracking-tight text-[var(--public-accent)] sm:text-8xl">
            404
          </p>
          <h1 className="mt-4 text-2xl font-bold text-[var(--public-text)] sm:text-3xl">
            No la encontramos
          </h1>
          <p className="mx-auto mt-3 max-w-md text-[var(--public-muted)]">
            La página que buscás no existe o fue movida. Volvé al inicio y seguí
            explorando.
          </p>
          <Link
            to="/"
            className="mt-8 inline-flex items-center justify-center gap-2 rounded-lg bg-[var(--public-accent)] px-6 py-3 text-base font-semibold text-[var(--public-accent-ink)] transition-colors hover:bg-[var(--public-accent-hover)]"
          >
            <Home className="h-5 w-5" aria-hidden="true" />
            Volver al inicio
          </Link>
        </div>
      </section>
    </>
  )
}
