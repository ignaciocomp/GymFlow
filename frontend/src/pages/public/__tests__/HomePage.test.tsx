import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import HomePage from '@/pages/public/HomePage'
import { site } from '@/content/site'

function renderHome() {
  return render(
    <MemoryRouter>
      <HomePage />
    </MemoryRouter>,
  )
}

test('muestra el tagline del sitio', () => {
  renderHome()
  expect(screen.getByText(new RegExp(site.tagline, 'i'))).toBeInTheDocument()
})

test('muestra los 2 CTAs del hero con sus rutas', () => {
  renderHome()
  // "Ver planes" puede aparecer en el hero y en el CTA final: ambos van a /planes.
  const verPlanes = screen.getAllByRole('link', { name: /ver planes/i })
  expect(verPlanes.length).toBeGreaterThan(0)
  for (const l of verPlanes) expect(l).toHaveAttribute('href', '/planes')
  expect(
    screen.getByRole('link', { name: /conocé las sedes|conoce las sedes/i }),
  ).toHaveAttribute('href', '/sedes')
})

test('el preview de sedes muestra el nombre de las 2 sedes', () => {
  renderHome()
  for (const sede of site.sedes) {
    expect(screen.getByText(sede.nombre)).toBeInTheDocument()
  }
})

test('el preview de clases linkea a /clases y muestra al menos una clase', () => {
  renderHome()
  const link = screen.getByRole('link', { name: /ver (todas las )?clases|todas las clases/i })
  expect(link).toHaveAttribute('href', '/clases')
  expect(screen.getByText(site.clases[0].nombre)).toBeInTheDocument()
})
