import { site } from '@/content/site'
import Seo from '@/components/public/Seo'
import Section from '@/components/public/Section'
import PlanCard from '@/components/public/PlanCard'

export default function PlanesPublicPage() {
  return (
    <>
      <Seo
        title={`Planes — ${site.nombre}`}
        description={`Conocé los planes de ${site.nombre}: precios, beneficios y el plan recomendado para vos.`}
        path="/planes"
      />

      <Section
        eyebrow="Planes"
        title="Elegí tu plan"
        subtitle="Precios claros, sin permanencia. Cambiá o cancelá cuando quieras."
        centered
      >
        <div className="grid gap-8 pt-2 md:grid-cols-2 lg:grid-cols-3">
          {site.planes.map((plan) => (
            <PlanCard key={plan.nombre} plan={plan} />
          ))}
        </div>

        <p className="mt-12 text-center text-sm text-[var(--public-muted)]">
          Los precios son de referencia. Escribinos por cualquier consulta.
        </p>
      </Section>
    </>
  )
}
