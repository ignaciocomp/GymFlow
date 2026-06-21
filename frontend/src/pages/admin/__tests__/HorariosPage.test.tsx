import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { vi, beforeEach, test, expect } from 'vitest'
import HorariosPage from '@/pages/admin/HorariosPage'
import { clasesApi, unidadesApi, horariosApi } from '@/services/api'
import type { Clase, Unidad } from '@/types'

vi.mock('@/services/api', () => ({
  clasesApi: { getAll: vi.fn() },
  unidadesApi: { getAll: vi.fn() },
  horariosApi: { getAll: vi.fn(), create: vi.fn(), update: vi.fn(), delete: vi.fn() },
}))

// Admin: unidadIds vacío => ve todas las sedes.
vi.mock('@/context/AuthContext', () => ({
  useAuth: () => ({ user: { unidadIds: [] } }),
}))

const UNIDAD_A: Unidad = { id: 'u-a', nombre: 'Sede Centro', direccion: 'Calle 1' }
const UNIDAD_B: Unidad = { id: 'u-b', nombre: 'Sede Norte', direccion: 'Calle 2' }

function clase(id: string, nombre: string, unidadId: string, unidadNombre: string): Clase {
  return {
    id, nombre, descripcion: '', capacidadMaxima: 10, duracionMinutos: 60,
    instructor: 'Profe', unidadId, unidadNombre, estaActivo: true,
  }
}

const CLASE_A = clase('c-a', 'Yoga Centro', 'u-a', 'Sede Centro')
const CLASE_B = clase('c-b', 'Spinning Norte', 'u-b', 'Sede Norte')

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <HorariosPage />
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(unidadesApi.getAll).mockResolvedValue([UNIDAD_A, UNIDAD_B])
  vi.mocked(horariosApi.getAll).mockResolvedValue([])
  // El backend, si recibe unidadId, devuelve solo las clases de esa sede.
  vi.mocked(clasesApi.getAll).mockImplementation(async (unidadId?: string) => {
    const todas = [CLASE_A, CLASE_B]
    return unidadId ? todas.filter(c => c.unidadId === unidadId) : todas
  })
})

test('no pide clases hasta que hay una sede seleccionada', async () => {
  renderPage()
  // Esperar a que carguen las unidades (botones del estado vacío).
  await screen.findByText('Sede Centro')
  expect(clasesApi.getAll).not.toHaveBeenCalled()
})

test('las clases se piden filtradas por la sede activa, no por todas', async () => {
  renderPage()

  // Estado vacío: botón por cada sede. Seleccionar la sede A.
  const botonSede = await screen.findByRole('button', { name: 'Sede Centro' })
  fireEvent.click(botonSede)

  // El query de clases debe usar el unidadId de la sede activa (bug: usaba undefined).
  await waitFor(() => {
    expect(clasesApi.getAll).toHaveBeenCalledWith('u-a', false)
  })
  expect(clasesApi.getAll).not.toHaveBeenCalledWith(undefined, false)
})
