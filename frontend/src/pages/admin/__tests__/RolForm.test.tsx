import { render, screen } from '@testing-library/react'
import { vi, test, expect } from 'vitest'
import RolForm from '@/pages/admin/RolForm'

vi.mock('@/services/permisos', () => ({
  listarPermisos: vi.fn().mockResolvedValue([]),
}))

// RN-16: todos los módulos con permisos asignables deben poder otorgarse desde la UI de roles.
test('la matriz de permisos incluye los módulos Dashboard y Eventos', async () => {
  render(<RolForm onSubmit={vi.fn()} submitLabel="Guardar" />)

  expect(await screen.findByText('Dashboard')).toBeInTheDocument()
  expect(screen.getByText('Eventos')).toBeInTheDocument()
})
