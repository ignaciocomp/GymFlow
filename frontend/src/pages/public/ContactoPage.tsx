import { MessageCircle, Mail, Camera, MapPin, ExternalLink } from 'lucide-react'
import { site } from '@/content/site'
import Seo from '@/components/public/Seo'
import Section from '@/components/public/Section'

/** Número de WhatsApp sin signos ni espacios, para el formato wa.me. */
const waDigits = site.contacto.whatsapp.replace(/\D/g, '')

export default function ContactoPage() {
  return (
    <>
      <Seo
        title={`Contacto — ${site.nombre}`}
        description={`Contactá a ${site.nombre} por WhatsApp, mail o Instagram. Encontranos en nuestras sedes de Montevideo.`}
        path="/contacto"
      />

      <Section
        eyebrow="Contacto"
        title="Hablemos"
        subtitle="Escribinos y te respondemos a la brevedad. Elegí el canal que prefieras."
      >
        <div className="grid gap-8 lg:grid-cols-2">
          {/* Canales de contacto */}
          <div className="space-y-4">
            <a
              href={`https://wa.me/${waDigits}`}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-4 rounded-2xl bg-[var(--public-accent)] px-6 py-5 text-[var(--public-accent-ink)] transition-colors hover:bg-[var(--public-accent-hover)]"
            >
              <MessageCircle className="h-7 w-7 shrink-0" aria-hidden="true" />
              <span>
                <span className="block text-lg font-bold">WhatsApp</span>
                <span className="block text-sm opacity-90">Escribinos ahora mismo</span>
              </span>
            </a>

            <a
              href={`mailto:${site.contacto.email}`}
              className="flex items-center gap-4 rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)] px-6 py-5 text-[var(--public-text)] transition-colors hover:bg-[var(--public-surface-2)]"
            >
              <span className="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-[var(--public-accent)]/10 text-[var(--public-accent)]">
                <Mail className="h-6 w-6" aria-hidden="true" />
              </span>
              <span>
                <span className="block text-lg font-bold">Email</span>
                <span className="block text-sm text-[var(--public-muted)]">{site.contacto.email}</span>
              </span>
            </a>

            <a
              href={site.contacto.instagram}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-4 rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)] px-6 py-5 text-[var(--public-text)] transition-colors hover:bg-[var(--public-surface-2)]"
            >
              <span className="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-[var(--public-accent)]/10 text-[var(--public-accent)]">
                <Camera className="h-6 w-6" aria-hidden="true" />
              </span>
              <span>
                <span className="block text-lg font-bold">Instagram</span>
                <span className="block text-sm text-[var(--public-muted)]">Seguinos y escribinos por DM</span>
              </span>
            </a>
          </div>

          {/* Ubicaciones / mapa */}
          <div className="space-y-4">
            <h2 className="text-sm font-semibold uppercase tracking-wider text-[var(--public-text)]">
              Dónde estamos
            </h2>
            {site.sedes.map((sede) => (
              <div
                key={sede.slug}
                className="rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)] p-6"
              >
                <h3 className="text-lg font-bold text-[var(--public-text)]">{sede.nombre}</h3>
                <p className="mt-1 flex items-start gap-2 text-sm text-[var(--public-muted)]">
                  <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-[var(--public-accent)]" aria-hidden="true" />
                  {sede.direccion}
                </p>
                <a
                  href={sede.mapsUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="mt-4 inline-flex items-center gap-2 text-sm font-semibold text-[var(--public-accent)] hover:underline"
                >
                  Cómo llegar (Google Maps)
                  <ExternalLink className="h-4 w-4" aria-hidden="true" />
                </a>
              </div>
            ))}
          </div>
        </div>
      </Section>
    </>
  )
}
