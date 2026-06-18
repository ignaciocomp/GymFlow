import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import SedesPage from '@/pages/public/SedesPage'
import { site } from '@/content/site'

function renderSedes() {
  return render(
    <MemoryRouter>
      <SedesPage />
    </MemoryRouter>,
  )
}

test('renderiza las 2 sedes con nombre y direccion', () => {
  renderSedes()
  for (const sede of site.sedes) {
    expect(screen.getByText(sede.nombre)).toBeInTheDocument()
    expect(screen.getByText(sede.direccion)).toBeInTheDocument()
  }
})

test('cada sede muestra al menos un horario', () => {
  renderSedes()
  for (const sede of site.sedes) {
    const primer = sede.horarios[0]
    // La etiqueta de días puede repetirse entre sedes (ej. "Lunes a Viernes").
    expect(screen.getAllByText(primer.dias).length).toBeGreaterThan(0)
    // El rango horario distingue cada sede.
    expect(screen.getAllByText(primer.horas).length).toBeGreaterThan(0)
  }
})

test('cada sede muestra sus servicios', () => {
  renderSedes()
  for (const sede of site.sedes) {
    for (const servicio of sede.servicios) {
      expect(screen.getAllByText(servicio).length).toBeGreaterThan(0)
    }
  }
})

test('cada sede tiene un enlace al mapa con su mapsUrl', () => {
  renderSedes()
  for (const sede of site.sedes) {
    const links = screen.getAllByRole('link', { name: /ver en (google )?maps|mapa/i })
    expect(links.some((l) => l.getAttribute('href') === sede.mapsUrl)).toBe(true)
  }
})
