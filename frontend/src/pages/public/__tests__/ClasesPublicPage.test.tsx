import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import ClasesPublicPage from '@/pages/public/ClasesPublicPage'
import { site } from '@/content/site'

function renderClases() {
  return render(
    <MemoryRouter>
      <ClasesPublicPage />
    </MemoryRouter>,
  )
}

test('renderiza los tipos de clase con nombre y descripcion', () => {
  renderClases()
  for (const clase of site.clases) {
    expect(screen.getAllByText(clase.nombre).length).toBeGreaterThan(0)
    expect(screen.getByText(clase.descripcion)).toBeInTheDocument()
  }
})

test('la grilla de horarios muestra todos los dias', () => {
  renderClases()
  const tabla = screen.getByRole('table')
  for (const dia of site.horarios) {
    expect(within(tabla).getAllByText(dia.dia).length).toBeGreaterThan(0)
  }
})

test('la grilla muestra al menos una clase con su hora', () => {
  renderClases()
  const tabla = screen.getByRole('table')
  const primerItem = site.horarios[0].items[0]
  expect(within(tabla).getAllByText(primerItem.hora).length).toBeGreaterThan(0)
  expect(within(tabla).getAllByText(new RegExp(primerItem.clase, 'i')).length).toBeGreaterThan(0)
})
