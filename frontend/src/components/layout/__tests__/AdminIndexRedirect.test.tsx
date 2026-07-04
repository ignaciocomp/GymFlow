import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { vi, beforeEach, test, expect } from 'vitest'
import AdminIndexRedirect from '@/components/layout/AdminIndexRedirect'
import { usePermisos } from '@/hooks/usePermisos'
import type { Modulo } from '@/types/permisos'

vi.mock('@/hooks/usePermisos', () => ({
  usePermisos: vi.fn(),
}))

function renderIndex(conDashboard: boolean) {
  vi.mocked(usePermisos).mockReturnValue({
    tienePermiso: vi.fn(),
    puedeLeer: (m: Modulo) => (m === 'Dashboard' ? conDashboard : true),
    puedeEscribir: () => false,
    puedeModificar: () => false,
    puedeEliminar: () => false,
  })
  return render(
    <MemoryRouter initialEntries={['/admin']}>
      <Routes>
        <Route path="/admin" element={<AdminIndexRedirect />} />
        <Route path="/admin/dashboard" element={<div>PAGINA DASHBOARD</div>} />
        <Route path="/admin/socios" element={<div>PAGINA SOCIOS</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => vi.clearAllMocks())

test('con permiso Dashboard-Lectura el index de /admin aterriza en el dashboard', () => {
  renderIndex(true)
  expect(screen.getByText('PAGINA DASHBOARD')).toBeInTheDocument()
})

test('sin permiso Dashboard-Lectura el index de /admin sigue aterrizando en socios', () => {
  renderIndex(false)
  expect(screen.getByText('PAGINA SOCIOS')).toBeInTheDocument()
})
