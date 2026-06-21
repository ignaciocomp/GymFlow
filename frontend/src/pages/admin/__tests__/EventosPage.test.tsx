import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { vi, beforeEach, test, expect } from 'vitest'
import EventosPage from '@/pages/admin/EventosPage'
import { eventosApi, unidadesApi } from '@/services/api'
import type { Evento, Unidad } from '@/types'

vi.mock('@/services/api', () => ({
  eventosApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    cancel: vi.fn(),
    notificar: vi.fn(),
    getDestinatarios: vi.fn(),
  },
  unidadesApi: { getAll: vi.fn() },
}))

const UNIDAD_A: Unidad = { id: 'u-a', nombre: 'Sede Centro', direccion: 'Calle 1' }

const EVENTO_A: Evento = {
  id: 'e-a',
  titulo: 'Torneo de verano',
  descripcion: 'desc',
  fecha: '2026-12-01T18:00:00.000Z',
  unidadId: 'u-a',
  unidadNombre: 'Sede Centro',
  estaActivo: true,
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <EventosPage />
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(unidadesApi.getAll).mockResolvedValue([UNIDAD_A])
  vi.mocked(eventosApi.getAll).mockResolvedValue([EVENTO_A])
})

async function abrirDialogoNotificar() {
  renderPage()
  // Seleccionar la sede desde el estado vacío.
  fireEvent.click(await screen.findByRole('button', { name: 'Sede Centro' }))
  // Abrir el diálogo de reenviar notificación (botón con title="Reenviar notificación").
  const botonNotificar = await screen.findByTitle('Reenviar notificación')
  fireEvent.click(botonNotificar)
}

test('el diálogo de notificar muestra cuántos socios y de qué sede reciben el correo', async () => {
  vi.mocked(eventosApi.getDestinatarios).mockResolvedValue({ cantidad: 3, sede: 'Sede Centro' })

  await abrirDialogoNotificar()

  await waitFor(() => {
    expect(eventosApi.getDestinatarios).toHaveBeenCalledWith('e-a')
  })

  // El texto debe indicar la cantidad (3 socios) y la sede.
  await screen.findByText(/3 socios/i)
  expect(screen.getAllByText(/Sede Centro/).length).toBeGreaterThan(0)
})

test('si no hay socios activos, avisa y deshabilita el botón Reenviar', async () => {
  vi.mocked(eventosApi.getDestinatarios).mockResolvedValue({ cantidad: 0, sede: 'Sede Centro' })

  await abrirDialogoNotificar()

  await screen.findByText(/No hay socios activos/i)
  const botonReenviar = screen.getByRole('button', { name: /Reenviar/i })
  expect(botonReenviar).toBeDisabled()
  expect(eventosApi.notificar).not.toHaveBeenCalled()
})

test('el banner de éxito muestra el mensaje del backend (con conteo y sede)', async () => {
  vi.mocked(eventosApi.getDestinatarios).mockResolvedValue({ cantidad: 3, sede: 'Sede Centro' })
  vi.mocked(eventosApi.notificar).mockResolvedValue({
    mensaje: 'Se notificó a 3 socios de Sede Centro.',
  })

  await abrirDialogoNotificar()
  await screen.findByText(/3 socios/i)

  fireEvent.click(screen.getByRole('button', { name: /Reenviar/i }))

  await waitFor(() => {
    expect(eventosApi.notificar).toHaveBeenCalledWith('e-a')
  })
  await screen.findByText('Se notificó a 3 socios de Sede Centro.')
})
