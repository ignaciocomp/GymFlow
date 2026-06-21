import type { ReactNode } from 'react'

interface SectionProps {
  /** Antetítulo corto (eyebrow) sobre el título, opcional. */
  eyebrow?: string
  /** Título de la sección. */
  title?: string
  /** Subtítulo / descripción bajo el título. */
  subtitle?: string
  /** Alinea el encabezado al centro. */
  centered?: boolean
  /** id para deep-link / aria. */
  id?: string
  className?: string
  children: ReactNode
}

/**
 * Wrapper de sección del sitio público: padding vertical consistente, ancho
 * máximo y un encabezado opcional (eyebrow + título + subtítulo).
 */
export default function Section({
  eyebrow,
  title,
  subtitle,
  centered = false,
  id,
  className = '',
  children,
}: SectionProps) {
  const hasHeader = eyebrow || title || subtitle
  return (
    <section id={id} className={`py-16 sm:py-20 ${className}`}>
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        {hasHeader && (
          <div className={`max-w-2xl ${centered ? 'mx-auto text-center' : ''}`}>
            {eyebrow && (
              <p className="text-sm font-semibold uppercase tracking-wider text-[var(--public-accent)]">
                {eyebrow}
              </p>
            )}
            {title && (
              <h2 className="mt-2 text-3xl font-extrabold tracking-tight text-[var(--public-text)] sm:text-4xl">
                {title}
              </h2>
            )}
            {subtitle && (
              <p className="mt-4 text-lg leading-relaxed text-[var(--public-muted)]">
                {subtitle}
              </p>
            )}
          </div>
        )}
        <div className={hasHeader ? 'mt-12' : ''}>{children}</div>
      </div>
    </section>
  )
}
