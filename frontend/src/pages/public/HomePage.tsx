import { Link } from 'react-router-dom'
import {
  ArrowRight,
  MapPin,
  Clock,
  Zap,
  Users,
  ShieldCheck,
  CalendarDays,
} from 'lucide-react'
import { site } from '@/content/site'
import Seo from '@/components/public/Seo'
import Section from '@/components/public/Section'
import Hero from '@/components/public/Hero'
import PublicImage from '@/components/public/PublicImage'
import { getClaseIcon } from '@/components/public/icons'

/** "Por qué elegirnos" — features de la marca (texto fijo del sitio). */
const FEATURES = [
  {
    icon: MapPin,
    title: '2 sedes en Montevideo',
    text: 'Entrená cerca de casa o del trabajo: accedé a Espacio Mora y Gimnasio Nuevo Malvín.',
  },
  {
    icon: Clock,
    title: 'Horarios amplios',
    text: 'Abierto de mañana a noche para que entrenes cuando mejor te quede.',
  },
  {
    icon: Users,
    title: 'Clases grupales',
    text: 'Funcional, spinning, cross training y yoga con profesores que te acompañan.',
  },
  {
    icon: ShieldCheck,
    title: 'Sin permanencia',
    text: 'Planes flexibles y claros, sin letra chica ni contratos eternos.',
  },
] as const

export default function HomePage() {
  return (
    <>
      <Seo
        title={`${site.nombre} — ${site.tagline}`}
        description={site.descripcion}
        path="/"
      />

      <Hero />

      {/* Por qué elegirnos */}
      <Section
        eyebrow="Por qué GymFlow"
        title="Todo para que entrenes mejor"
        subtitle="Equipamiento, clases y horarios pensados para que cumplas tus objetivos sin excusas."
        centered
      >
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {FEATURES.map((f) => {
            const Icon = f.icon
            return (
              <div
                key={f.title}
                className="rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)] p-6 transition-colors hover:border-[var(--public-accent)]/40"
              >
                <span className="grid h-12 w-12 place-items-center rounded-xl bg-[var(--public-accent)]/10 text-[var(--public-accent)]">
                  <Icon className="h-6 w-6" aria-hidden="true" />
                </span>
                <h3 className="mt-4 text-lg font-bold text-[var(--public-text)]">
                  {f.title}
                </h3>
                <p className="mt-2 text-sm leading-relaxed text-[var(--public-muted)]">
                  {f.text}
                </p>
              </div>
            )
          })}
        </div>
      </Section>

      {/* Preview de sedes */}
      <Section
        eyebrow="Nuestras sedes"
        title="Elegí dónde entrenar"
        subtitle="Dos espacios equipados para vos."
      >
        <div className="grid gap-6 md:grid-cols-2">
          {site.sedes.map((sede) => (
            <Link
              key={sede.slug}
              to="/sedes"
              className="group overflow-hidden rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)] transition-colors hover:border-[var(--public-accent)]/50"
            >
              <PublicImage
                src={sede.foto}
                alt={`Sede ${sede.nombre}`}
                icon={MapPin}
                label="Foto de la sede"
                className="aspect-[16/9]"
              />
              <div className="p-6">
                <h3 className="text-xl font-bold text-[var(--public-text)]">
                  {sede.nombre}
                </h3>
                <p className="mt-1 flex items-center gap-2 text-sm text-[var(--public-muted)]">
                  <MapPin className="h-4 w-4 shrink-0 text-[var(--public-accent)]" aria-hidden="true" />
                  {sede.direccion}
                </p>
                <span className="mt-4 inline-flex items-center gap-1 text-sm font-semibold text-[var(--public-accent)]">
                  Ver detalle
                  <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" aria-hidden="true" />
                </span>
              </div>
            </Link>
          ))}
        </div>
      </Section>

      {/* Preview de clases */}
      <Section
        eyebrow="Clases"
        title="Movete a tu ritmo"
        subtitle="Una variedad de clases para todos los niveles."
      >
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {site.clases.map((clase) => {
            const Icon = getClaseIcon(clase.icono)
            return (
              <div
                key={clase.nombre}
                className="rounded-2xl border border-[var(--public-border)] bg-[var(--public-surface)] p-6"
              >
                <span className="grid h-12 w-12 place-items-center rounded-xl bg-[var(--public-accent)]/10 text-[var(--public-accent)]">
                  <Icon className="h-6 w-6" aria-hidden="true" />
                </span>
                <h3 className="mt-4 text-lg font-bold text-[var(--public-text)]">
                  {clase.nombre}
                </h3>
              </div>
            )
          })}
        </div>
        <div className="mt-10 text-center">
          <Link
            to="/clases"
            className="inline-flex items-center justify-center gap-2 rounded-lg border border-[var(--public-border)] px-6 py-3 text-base font-semibold text-[var(--public-text)] transition-colors hover:bg-[var(--public-surface-2)]"
          >
            <CalendarDays className="h-5 w-5" aria-hidden="true" />
            Ver todas las clases y horarios
          </Link>
        </div>
      </Section>

      {/* CTA final */}
      <section className="bg-[var(--public-bg)] pb-20">
        <div className="mx-auto max-w-6xl px-4 sm:px-6">
          <div className="relative overflow-hidden rounded-3xl border border-[var(--public-border)] bg-gradient-to-br from-[var(--public-surface)] to-[var(--public-surface-2)] px-6 py-14 text-center sm:px-12">
            <div
              aria-hidden="true"
              className="pointer-events-none absolute -bottom-24 left-1/2 h-72 w-72 -translate-x-1/2 rounded-full bg-[var(--public-accent)] opacity-10 blur-3xl"
            />
            <Zap className="mx-auto h-10 w-10 text-[var(--public-accent)]" aria-hidden="true" />
            <h2 className="mt-4 text-3xl font-extrabold tracking-tight text-[var(--public-text)] sm:text-4xl">
              Empezá hoy a entrenar
            </h2>
            <p className="mx-auto mt-4 max-w-xl text-lg text-[var(--public-muted)]">
              Conocé los planes y elegí el que mejor se adapte a vos.
            </p>
            <div className="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
              <Link
                to="/planes"
                className="inline-flex items-center justify-center gap-2 rounded-lg bg-[var(--public-accent)] px-6 py-3 text-base font-semibold text-[var(--public-accent-ink)] transition-colors hover:bg-[var(--public-accent-hover)]"
              >
                Ver planes
                <ArrowRight className="h-5 w-5" aria-hidden="true" />
              </Link>
              <Link
                to="/contacto"
                className="inline-flex items-center justify-center gap-2 rounded-lg border border-[var(--public-border)] px-6 py-3 text-base font-semibold text-[var(--public-text)] transition-colors hover:bg-[var(--public-surface-2)]"
              >
                Contactanos
              </Link>
            </div>
          </div>
        </div>
      </section>
    </>
  )
}
