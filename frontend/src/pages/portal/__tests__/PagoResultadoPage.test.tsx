import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { describe, test, expect, vi } from 'vitest'
import PagoResultadoPage from '@/pages/portal/PagoResultadoPage'

function renderConStatus(status?: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const invalidateSpy = vi.spyOn(qc, 'invalidateQueries')
  const url = status ? `/portal/pago/resultado?status=${status}` : '/portal/pago/resultado'
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[url]}>
        <PagoResultadoPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return { invalidateSpy }
}

describe('PagoResultadoPage', () => {
  test('status=approved muestra el mensaje de pago confirmado y la nota de demora', () => {
    renderConStatus('approved')
    expect(screen.getByText(/Pago confirmado/i)).toBeInTheDocument()
    expect(screen.getByText(/puede tardar unos segundos en reflejarse/i)).toBeInTheDocument()
  })

  test('status=approved invalida la query de mis-cuotas para refrescar el estado', () => {
    const { invalidateSpy } = renderConStatus('approved')
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['mis-cuotas'] })
  })

  test('status=failure muestra el mensaje de pago rechazado', () => {
    renderConStatus('failure')
    expect(screen.getByText(/rechazado/i)).toBeInTheDocument()
  })

  test('status=pending muestra el mensaje de pago en proceso', () => {
    renderConStatus('pending')
    expect(screen.getByText(/siendo procesado/i)).toBeInTheDocument()
  })

  test('muestra enlaces a Mis cuotas y Mis pagos', () => {
    renderConStatus('approved')
    const cuotasLink = screen.getByRole('link', { name: /Mis cuotas/i })
    const pagosLink = screen.getByRole('link', { name: /Mis pagos/i })
    expect(cuotasLink).toHaveAttribute('href', '/portal/mis-cuotas')
    expect(pagosLink).toHaveAttribute('href', '/portal/mis-pagos')
  })
})
