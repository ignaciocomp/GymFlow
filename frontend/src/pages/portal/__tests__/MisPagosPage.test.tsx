import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { vi, beforeEach, test, expect } from 'vitest'
import MisPagosPage from '@/pages/portal/MisPagosPage'
import { pagosApi } from '@/services/api'
import type { PagoDto } from '@/types'

vi.mock('@/services/api', () => ({
  pagosApi: {
    getMisPagos: vi.fn(),
    iniciar: vi.fn(),
  },
}))

const PAGO_APROBADO: PagoDto = {
  id: 'pago-1',
  fecha: '2026-06-15T00:00:00.000Z',
  monto: 1500,
  medioPago: 'credit_card',
  mpPaymentId: 'mp-999',
  estado: 'Aprobado',
  nombrePlan: 'Plan Full',
}

const PAGO_RECHAZADO: PagoDto = {
  id: 'pago-2',
  fecha: '2026-05-10T00:00:00.000Z',
  monto: 900,
  medioPago: null,
  mpPaymentId: null,
  estado: 'Rechazado',
  nombrePlan: 'Plan Básico',
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <MisPagosPage />
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

test('lista los pagos del socio con fecha, monto, N° de transacción y estado', async () => {
  vi.mocked(pagosApi.getMisPagos).mockResolvedValue([PAGO_APROBADO, PAGO_RECHAZADO])

  renderPage()

  // Fecha formateada (dd/mm/yyyy) del primer pago.
  await screen.findAllByText('15/06/2026')
  // Plan y N° de transacción del pago aprobado.
  expect(screen.getAllByText('Plan Full').length).toBeGreaterThan(0)
  expect(screen.getAllByText(/mp-999/).length).toBeGreaterThan(0)
  // Estados como badge.
  expect(screen.getAllByText('Aprobado').length).toBeGreaterThan(0)
  expect(screen.getAllByText('Rechazado').length).toBeGreaterThan(0)
})

test('muestra un estado vacío cuando el socio no tiene pagos', async () => {
  vi.mocked(pagosApi.getMisPagos).mockResolvedValue([])

  renderPage()

  // El estado vacío se renderiza tanto en la vista tabla como en la de cards.
  await screen.findAllByText(/No tenés pagos registrados/i)
})
