import { Link } from 'react-router-dom'
import { Check, Star } from 'lucide-react'
import type { Plan } from '@/content/site'

/**
 * Tarjeta de un plan. El plan `destacado` se resalta con un borde de acento,
 * fondo elevado y un badge "Recomendado".
 */
export default function PlanCard({ plan }: { plan: Plan }) {
  const destacado = plan.destacado

  return (
    <article
      className={[
        'relative flex flex-col rounded-2xl border p-8 transition-colors',
        destacado
          ? 'border-[var(--public-accent)] bg-[var(--public-surface-2)] shadow-[0_0_0_1px_var(--public-accent)] lg:scale-[1.03]'
          : 'border-[var(--public-border)] bg-[var(--public-surface)]',
      ].join(' ')}
    >
      {destacado && (
        <span className="absolute -top-3 left-1/2 inline-flex -translate-x-1/2 items-center gap-1 rounded-full bg-[var(--public-accent)] px-3 py-1 text-xs font-bold uppercase tracking-wide text-[var(--public-accent-ink)]">
          <Star className="h-3.5 w-3.5" aria-hidden="true" />
          Recomendado
        </span>
      )}

      <h2 className="text-xl font-bold text-[var(--public-text)]">{plan.nombre}</h2>
      <p className="mt-4 text-3xl font-extrabold tracking-tight text-[var(--public-text)]">
        {plan.precio}
      </p>

      <ul className="mt-6 flex-1 space-y-3">
        {plan.beneficios.map((b) => (
          <li key={b} className="flex items-start gap-3 text-sm text-[var(--public-muted)]">
            <Check
              className="mt-0.5 h-5 w-5 shrink-0 text-[var(--public-accent)]"
              aria-hidden="true"
            />
            <span>{b}</span>
          </li>
        ))}
      </ul>

      <Link
        to="/contacto"
        className={[
          'mt-8 inline-flex items-center justify-center rounded-lg px-5 py-3 text-base font-semibold transition-colors',
          destacado
            ? 'bg-[var(--public-accent)] text-[var(--public-accent-ink)] hover:bg-[var(--public-accent-hover)]'
            : 'border border-[var(--public-border)] text-[var(--public-text)] hover:bg-[var(--public-surface-2)]',
        ].join(' ')}
      >
        Quiero este plan
      </Link>
    </article>
  )
}
