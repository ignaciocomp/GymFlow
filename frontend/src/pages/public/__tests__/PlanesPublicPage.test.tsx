import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import PlanesPublicPage from '@/pages/public/PlanesPublicPage'
import { site } from '@/content/site'

function renderPlanes() {
  return render(
    <MemoryRouter>
      <PlanesPublicPage />
    </MemoryRouter>,
  )
}

test('renderiza todos los planes con nombre y precio', () => {
  renderPlanes()
  for (const plan of site.planes) {
    expect(screen.getByText(plan.nombre)).toBeInTheDocument()
    expect(screen.getByText(plan.precio)).toBeInTheDocument()
  }
})

test('cada plan muestra al menos un beneficio', () => {
  renderPlanes()
  for (const plan of site.planes) {
    expect(screen.getAllByText(plan.beneficios[0]).length).toBeGreaterThan(0)
  }
})

test('el plan destacado tiene un badge "Recomendado"', () => {
  renderPlanes()
  const destacado = site.planes.find((p) => p.destacado)!
  expect(destacado).toBeTruthy()
  // El badge vive dentro de la card del plan destacado.
  const nombre = screen.getByText(destacado.nombre)
  const card = nombre.closest('article')!
  expect(within(card).getByText(/recomendado/i)).toBeInTheDocument()
})

test('los planes no destacados no tienen badge "Recomendado"', () => {
  renderPlanes()
  const recomendados = screen.getAllByText(/recomendado/i)
  const cantidadDestacados = site.planes.filter((p) => p.destacado).length
  expect(recomendados.length).toBe(cantidadDestacados)
})
