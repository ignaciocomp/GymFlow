import { Clock, MapPin } from 'lucide-react'
import type { HorarioDia } from '@/content/site'

/**
 * Grilla semanal de horarios de clases.
 *
 * - Desktop: una tabla (`<table>`) con una fila por día y, en cada celda, las
 *   clases del día con su hora, nombre y sede.
 * - Mobile: la tabla se oculta y se muestra una lista apilada por día (misma
 *   info, más cómoda en pantallas chicas).
 */
export default function ClaseHorarioTabla({ horarios }: { horarios: readonly HorarioDia[] }) {
  return (
    <div>
      {/* Tabla (desktop) */}
      <div className="hidden overflow-hidden rounded-2xl border border-[var(--public-border)] md:block">
        <table className="w-full border-collapse text-left text-sm">
          <caption className="sr-only">Grilla semanal de clases por día</caption>
          <thead>
            <tr className="bg-[var(--public-surface-2)]">
              <th scope="col" className="w-40 px-5 py-3 font-semibold text-[var(--public-text)]">
                Día
              </th>
              <th scope="col" className="px-5 py-3 font-semibold text-[var(--public-text)]">
                Clases
              </th>
            </tr>
          </thead>
          <tbody>
            {horarios.map((d) => (
              <tr
                key={d.dia}
                className="border-t border-[var(--public-border)] align-top"
              >
                <th
                  scope="row"
                  className="px-5 py-4 font-semibold text-[var(--public-accent)]"
                >
                  {d.dia}
                </th>
                <td className="px-5 py-4">
                  <ul className="flex flex-wrap gap-2">
                    {d.items.map((it) => (
                      <li
                        key={`${it.hora}-${it.clase}`}
                        className="rounded-lg border border-[var(--public-border)] bg-[var(--public-surface)] px-3 py-2"
                      >
                        <span className="flex items-center gap-1.5 font-semibold tabular-nums text-[var(--public-text)]">
                          <Clock className="h-3.5 w-3.5 text-[var(--public-accent)]" aria-hidden="true" />
                          {it.hora}
                        </span>
                        <span className="mt-0.5 block text-[var(--public-text)]">{it.clase}</span>
                        <span className="mt-0.5 flex items-center gap-1 text-xs text-[var(--public-muted)]">
                          <MapPin className="h-3 w-3" aria-hidden="true" />
                          {it.sede}
                        </span>
                      </li>
                    ))}
                  </ul>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Lista por día (mobile) */}
      <div className="space-y-4 md:hidden">
        {horarios.map((d) => (
          <div
            key={d.dia}
            className="overflow-hidden rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)]"
          >
            <h3 className="bg-[var(--public-surface-2)] px-4 py-2 font-semibold text-[var(--public-accent)]">
              {d.dia}
            </h3>
            <ul className="divide-y divide-[var(--public-border)]">
              {d.items.map((it) => (
                <li key={`${it.hora}-${it.clase}`} className="flex items-center justify-between gap-3 px-4 py-3">
                  <div>
                    <span className="block font-medium text-[var(--public-text)]">{it.clase}</span>
                    <span className="flex items-center gap-1 text-xs text-[var(--public-muted)]">
                      <MapPin className="h-3 w-3" aria-hidden="true" />
                      {it.sede}
                    </span>
                  </div>
                  <span className="flex shrink-0 items-center gap-1.5 font-semibold tabular-nums text-[var(--public-text)]">
                    <Clock className="h-3.5 w-3.5 text-[var(--public-accent)]" aria-hidden="true" />
                    {it.hora}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
    </div>
  )
}
