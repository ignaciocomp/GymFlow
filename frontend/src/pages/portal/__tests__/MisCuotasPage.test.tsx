import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { vi, beforeEach, afterEach, test, expect } from 'vitest'
import MisCuotasPage from '@/pages/portal/MisCuotasPage'
import { cuotasApi, pagosApi } from '@/services/api'
import type { CuotaDto } from '@/types'

vi.mock('@/services/api', () => ({
  cuotasApi: {
    getMisCuotas: vi.fn(),
  },
  pagosApi: {
    iniciar: vi.fn(),
    getMisPagos: vi.fn(),
  },
}))

const CUOTA_PENDIENTE: CuotaDto = {
  id: 'cuota-pendiente',
  nombrePlan: 'Plan Full',
  nombreUnidad: 'Sede Centro',
  nombreSocio: null,
  monto: 1500,
  fechaEmision: '2026-06-01T00:00:00.000Z',
  fechaVencimiento: '2026-07-10T00:00:00.000Z',
  estado: 'Pendiente',
  fechaPago: null,
  fechaBaja: null,
}

const CUOTA_PAGADA: CuotaDto = {
  ...CUOTA_PENDIENTE,
  id: 'cuota-pagada',
  estado: 'Pagada',
  fechaPago: '2026-06-15T00:00:00.000Z',
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <MisCuotasPage />
    </QueryClientProvider>,
  )
}

// window.location.href necesita ser reemplazable para verificar la redirección.
const originalLocation = window.location

beforeEach(() => {
  vi.clearAllMocks()
  // @ts-expect-error - reemplazamos location por un objeto simple para poder setear href.
  delete window.location
  // @ts-expect-error - stub mínimo.
  window.location = { href: '' }
})

afterEach(() => {
  // @ts-expect-error - restaurar la location real.
  window.location = originalLocation
})

test('en una cuota Pendiente, "Pagar con Mercado Pago" inicia el pago y redirige al initPoint', async () => {
  vi.mocked(cuotasApi.getMisCuotas).mockResolvedValue([CUOTA_PENDIENTE])
  vi.mocked(pagosApi.iniciar).mockResolvedValue({ initPoint: 'https://mp.com/checkout/abc' })

  renderPage()

  // La página renderiza vista desktop y mobile; ambos botones están cableados igual.
  const [boton] = await screen.findAllByRole('button', { name: /Pagar con Mercado Pago/i })
  fireEvent.click(boton)

  await waitFor(() => {
    expect(pagosApi.iniciar).toHaveBeenCalledWith('cuota-pendiente')
  })
  await waitFor(() => {
    expect(window.location.href).toBe('https://mp.com/checkout/abc')
  })
})

test('una cuota Pagada no muestra el botón de pago', async () => {
  vi.mocked(cuotasApi.getMisCuotas).mockResolvedValue([CUOTA_PAGADA])

  renderPage()

  // Esperar a que renderice la fila de la cuota pagada.
  await screen.findAllByText('Plan Full')
  expect(screen.queryByRole('button', { name: /Pagar con Mercado Pago/i })).not.toBeInTheDocument()
})

test('si iniciar el pago falla, muestra un mensaje de error y no redirige', async () => {
  vi.mocked(cuotasApi.getMisCuotas).mockResolvedValue([CUOTA_PENDIENTE])
  vi.mocked(pagosApi.iniciar).mockRejectedValue({ response: { status: 409 } })

  renderPage()

  const [boton] = await screen.findAllByRole('button', { name: /Pagar con Mercado Pago/i })
  fireEvent.click(boton)

  await screen.findByText(/No se pudo iniciar el pago/i)
  expect(window.location.href).toBe('')
})
