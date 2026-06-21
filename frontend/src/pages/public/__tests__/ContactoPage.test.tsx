import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import ContactoPage from '@/pages/public/ContactoPage'
import { site } from '@/content/site'

function renderContacto() {
  return render(
    <MemoryRouter>
      <ContactoPage />
    </MemoryRouter>,
  )
}

test('tiene un enlace de WhatsApp a wa.me con el numero sin signos', () => {
  renderContacto()
  const digits = site.contacto.whatsapp.replace(/\D/g, '')
  const wa = screen.getByRole('link', { name: /whatsapp/i })
  expect(wa).toHaveAttribute('href', `https://wa.me/${digits}`)
})

test('tiene un enlace mailto al email del sitio', () => {
  renderContacto()
  const mail = screen.getByRole('link', { name: new RegExp(site.contacto.email, 'i') })
  expect(mail).toHaveAttribute('href', `mailto:${site.contacto.email}`)
})

test('tiene un enlace a instagram', () => {
  renderContacto()
  const ig = screen.getByRole('link', { name: /instagram/i })
  expect(ig).toHaveAttribute('href', site.contacto.instagram)
})

test('muestra un mapa (link o iframe) de al menos una sede', () => {
  renderContacto()
  const mapLinks = screen.getAllByRole('link', { name: /maps|mapa|cómo llegar|como llegar/i })
  const urls: string[] = site.sedes.map((s) => s.mapsUrl)
  expect(mapLinks.some((l) => urls.includes(l.getAttribute('href') ?? ''))).toBe(true)
})
