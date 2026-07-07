import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi, beforeEach, test, expect } from 'vitest'
import Sidebar from '@/components/layout/Sidebar'
import { usePermisos } from '@/hooks/usePermisos'
import type { Modulo } from '@/types/permisos'

vi.mock('@/hooks/usePermisos', () => ({
  usePermisos: vi.fn(),
}))

function renderSidebar(modulosLegibles: Modulo[]) {
  vi.mocked(usePermisos).mockReturnValue({
    tienePermiso: vi.fn(),
    puedeLeer: (m: Modulo) => modulosLegibles.includes(m),
    puedeEscribir: () => false,
    puedeModificar: () => false,
    puedeEliminar: () => false,
  })
  return render(
    <MemoryRouter initialEntries={['/admin/socios']}>
      <Sidebar collapsed={false} mobileOpen={false} onMobileClose={() => {}} />
    </MemoryRouter>,
  )
}

beforeEach(() => vi.clearAllMocks())

test('con permiso Dashboard-Lectura muestra el ítem Dashboard apuntando a /admin/dashboard', () => {
  renderSidebar(['Dashboard', 'Socios'])

  const link = screen.getByRole('link', { name: /Dashboard/i })
  expect(link).toHaveAttribute('href', '/admin/dashboard')
})

test('sin permiso Dashboard-Lectura no muestra el ítem Dashboard', () => {
  renderSidebar(['Socios'])

  expect(screen.queryByRole('link', { name: /Dashboard/i })).not.toBeInTheDocument()
  // El resto del nav sigue visible.
  expect(screen.getByText('Socios')).toBeInTheDocument()
})
