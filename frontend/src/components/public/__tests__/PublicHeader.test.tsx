import { render, screen, within } from '@testing-library/react'
import { fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import PublicHeader from '@/components/public/PublicHeader'

function renderHeader(initialPath = '/') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <PublicHeader />
    </MemoryRouter>,
  )
}

test('header tiene los 5 links de navegacion apuntando a sus rutas', () => {
  renderHeader()
  const nav = screen.getByRole('navigation', { name: /principal/i })
  expect(within(nav).getByRole('link', { name: /inicio/i })).toHaveAttribute('href', '/')
  expect(within(nav).getByRole('link', { name: /sedes/i })).toHaveAttribute('href', '/sedes')
  expect(within(nav).getByRole('link', { name: /planes/i })).toHaveAttribute('href', '/planes')
  expect(within(nav).getByRole('link', { name: /clases/i })).toHaveAttribute('href', '/clases')
  expect(within(nav).getByRole('link', { name: /contacto/i })).toHaveAttribute('href', '/contacto')
})

test('header tiene boton Acceder que linkea a /login', () => {
  renderHeader()
  const acceder = screen.getAllByRole('link', { name: /acceder/i })[0]
  expect(acceder).toHaveAttribute('href', '/login')
})

test('la marca linkea al inicio', () => {
  renderHeader()
  const brand = screen.getByRole('link', { name: /nuevo\s*malv/i })
  expect(brand).toHaveAttribute('href', '/')
})

test('el menu mobile se puede abrir y cerrar con el boton hamburguesa', () => {
  renderHeader()
  const toggle = screen.getByRole('button', { name: /abrir menú/i })
  expect(toggle).toHaveAttribute('aria-expanded', 'false')
  fireEvent.click(toggle)
  const closeToggle = screen.getByRole('button', { name: /cerrar menú/i })
  expect(closeToggle).toHaveAttribute('aria-expanded', 'true')
})
