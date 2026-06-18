import { site } from '@/content/site'
import Seo from '@/components/public/Seo'
import Section from '@/components/public/Section'
import ClaseHorarioTabla from '@/components/public/ClaseHorarioTabla'
import { getClaseIcon } from '@/components/public/icons'

export default function ClasesPublicPage() {
  return (
    <>
      <Seo
        title={`Clases — ${site.nombre}`}
        description={`Clases grupales en ${site.nombre}: funcional, spinning, cross training y yoga, con su grilla semanal de horarios.`}
        path="/clases"
      />

      {/* Tipos de clase */}
      <Section
        eyebrow="Clases"
        title="Nuestras clases"
        subtitle="Variedad de clases grupales para todos los niveles, guiadas por profesores."
      >
        <div className="grid gap-6 sm:grid-cols-2">
          {site.clases.map((clase) => {
            const Icon = getClaseIcon(clase.icono)
            return (
              <article
                key={clase.nombre}
                className="flex gap-4 rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)] p-6"
              >
                <span className="grid h-12 w-12 shrink-0 place-items-center rounded-xl bg-[var(--public-accent)]/10 text-[var(--public-accent)]">
                  <Icon className="h-6 w-6" aria-hidden="true" />
                </span>
                <div>
                  <h2 className="text-lg font-bold text-[var(--public-text)]">{clase.nombre}</h2>
                  <p className="mt-1 text-sm leading-relaxed text-[var(--public-muted)]">
                    {clase.descripcion}
                  </p>
                </div>
              </article>
            )
          })}
        </div>
      </Section>

      {/* Grilla de horarios */}
      <Section
        eyebrow="Horarios"
        title="Grilla semanal"
        subtitle="Consultá los días, horas y sedes de cada clase."
        className="pt-0"
      >
        <ClaseHorarioTabla horarios={site.horarios} />
      </Section>
    </>
  )
}
