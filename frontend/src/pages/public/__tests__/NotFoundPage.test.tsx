import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import NotFoundPage from '@/pages/public/NotFoundPage'

function renderNotFound() {
  return render(
    <MemoryRouter>
      <NotFoundPage />
    </MemoryRouter>,
  )
}

test('muestra el codigo 404 y un mensaje', () => {
  renderNotFound()
  expect(screen.getByText('404')).toBeInTheDocument()
  expect(
    screen.getAllByText(/no (la )?encontramos|no encontrada|no existe/i).length,
  ).toBeGreaterThan(0)
})

test('tiene un link de vuelta al inicio', () => {
  renderNotFound()
  const link = screen.getByRole('link', { name: /inicio|volver/i })
  expect(link).toHaveAttribute('href', '/')
})
