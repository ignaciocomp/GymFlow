import { site } from '@/content/site'
import Seo from '@/components/public/Seo'
import Section from '@/components/public/Section'
import SedeCard from '@/components/public/SedeCard'

export default function SedesPage() {
  return (
    <>
      <Seo
        title={`Sedes — ${site.nombre}`}
        description={`Conocé las sedes de ${site.nombre} en Montevideo: dirección, horarios y servicios de cada gimnasio.`}
        path="/sedes"
      />

      <Section
        eyebrow="Nuestras sedes"
        title="Dónde entrenar"
        subtitle="Dos espacios equipados en Montevideo. Elegí el que mejor te quede."
      >
        <div className="grid gap-8 lg:grid-cols-2">
          {site.sedes.map((sede) => (
            <SedeCard key={sede.slug} sede={sede} />
          ))}
        </div>
      </Section>
    </>
  )
}
