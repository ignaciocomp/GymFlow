import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import PublicLayout from '@/components/public/PublicLayout'

test('PublicLayout muestra header, footer y el contenido de la ruta', () => {
  render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route element={<PublicLayout />}>
          <Route path="/" element={<div>contenido home</div>} />
        </Route>
      </Routes>
    </MemoryRouter>
  )
  expect(screen.getByRole('banner')).toBeInTheDocument() // header
  expect(screen.getByRole('contentinfo')).toBeInTheDocument() // footer
  expect(screen.getByText('contenido home')).toBeInTheDocument()
})
