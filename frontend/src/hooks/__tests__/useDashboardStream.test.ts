import { renderHook, waitFor, act } from '@testing-library/react'
import { vi, beforeEach, afterEach, test, expect } from 'vitest'
import { useDashboardStream } from '@/hooks/useDashboardStream'
import { dashboardApi } from '@/services/api'
import type { DashboardDto } from '@/types'

vi.mock('@/services/api', () => ({
  dashboardApi: { get: vi.fn() },
}))

const dto = (generadoEn: string, total = 10): DashboardDto => ({
  generadoEn,
  unidades: [{ id: 'u-1', nombre: 'Sede Centro' }],
  sociosActivos: { total, porUnidad: [{ unidadId: 'u-1', unidadNombre: 'Sede Centro', cantidad: total }] },
  cuotas: { proximasAVencer: 2, vencidas: 1, pagadasMes: 5 },
  clasesDelDia: [],
  inscripcionesRecientes: [],
  grafica: { sociosPorSede: [], cuotasPorEstado: [], inscripcionesUltimos7Dias: [] },
})

/** Stream SSE controlado: permite empujar chunks de texto como haría el backend. */
function crearStreamControlado() {
  const encoder = new TextEncoder()
  let controller!: ReadableStreamDefaultController<Uint8Array>
  const body = new ReadableStream<Uint8Array>({ start(c) { controller = c } })
  return {
    response: { ok: true, status: 200, body } as unknown as Response,
    push: (texto: string) => controller.enqueue(encoder.encode(texto)),
    close: () => controller.close(),
  }
}

const fetchMock = vi.fn()

beforeEach(() => {
  vi.clearAllMocks()
  localStorage.setItem('gymflow_token', 'tok-123')
  vi.stubGlobal('fetch', fetchMock)
  vi.mocked(dashboardApi.get).mockResolvedValue(dto('2026-07-04T10:00:00Z'))
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.useRealTimers()
  localStorage.clear()
})

test('carga el snapshot inicial por GET y expone los datos', async () => {
  const stream = crearStreamControlado()
  fetchMock.mockResolvedValue(stream.response)

  const { result, unmount } = renderHook(() => useDashboardStream())

  await waitFor(() => {
    expect(result.current.data?.sociosActivos.total).toBe(10)
  })
  expect(dashboardApi.get).toHaveBeenCalledWith(undefined)
  expect(result.current.actualizadoEn).not.toBeNull()
  unmount()
})

test('pasa a live cuando el stream emite un snapshot, incluso en chunks parciales', async () => {
  const stream = crearStreamControlado()
  fetchMock.mockResolvedValue(stream.response)

  const { result, unmount } = renderHook(() => useDashboardStream())
  await waitFor(() => expect(result.current.data).not.toBeNull())
  expect(result.current.live).toBe(false)

  // El backend manda `data: {json}\n\n`; lo partimos en dos chunks para probar el buffering.
  const json = JSON.stringify(dto('2026-07-04T10:00:10Z', 42))
  const mitad = Math.floor(json.length / 2)
  stream.push(`data: ${json.slice(0, mitad)}`)
  stream.push(`${json.slice(mitad)}\n\n`)

  await waitFor(() => {
    expect(result.current.live).toBe(true)
    expect(result.current.data?.sociosActivos.total).toBe(42)
  })
  unmount()
})

test('abre el stream con el token Bearer y la unidad seleccionada', async () => {
  const stream = crearStreamControlado()
  fetchMock.mockResolvedValue(stream.response)

  const { unmount } = renderHook(() => useDashboardStream('u-9'))

  await waitFor(() => {
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/dashboard/stream?unidadId=u-9',
      expect.objectContaining({
        headers: { Authorization: 'Bearer tok-123' },
        signal: expect.any(AbortSignal),
      }),
    )
  })
  expect(dashboardApi.get).toHaveBeenCalledWith('u-9')
  unmount()
})

test('ignora los heartbeats (: ping) sin perder el estado live', async () => {
  const stream = crearStreamControlado()
  fetchMock.mockResolvedValue(stream.response)

  const { result, unmount } = renderHook(() => useDashboardStream())
  await waitFor(() => expect(result.current.data).not.toBeNull())

  stream.push(`data: ${JSON.stringify(dto('2026-07-04T10:00:10Z', 7))}\n\n`)
  await waitFor(() => expect(result.current.live).toBe(true))

  stream.push(': ping\n\n')
  // Un heartbeat no cambia los datos ni corta el vivo.
  await waitFor(() => expect(result.current.live).toBe(true))
  expect(result.current.data?.sociosActivos.total).toBe(7)
  unmount()
})

test('tras 2 reintentos fallidos degrada a polling cada 15s con live=false', async () => {
  vi.useFakeTimers()
  fetchMock.mockRejectedValue(new Error('conexion rechazada'))

  const { result, unmount } = renderHook(() => useDashboardStream())

  // Carga inicial + intento 0 del stream (falla).
  await act(async () => { await vi.advanceTimersByTimeAsync(0) })
  expect(result.current.data?.sociosActivos.total).toBe(10)

  // Backoff: reintento 1 (1s) y reintento 2 (2s) también fallan → polling.
  await act(async () => { await vi.advanceTimersByTimeAsync(1000) })
  await act(async () => { await vi.advanceTimersByTimeAsync(2000) })
  expect(fetchMock).toHaveBeenCalledTimes(3)
  expect(result.current.live).toBe(false)
  expect(dashboardApi.get).toHaveBeenCalledTimes(1)

  // Cada 15s repite el GET manteniendo live=false.
  await act(async () => { await vi.advanceTimersByTimeAsync(15000) })
  expect(dashboardApi.get).toHaveBeenCalledTimes(2)
  expect(result.current.live).toBe(false)
  unmount()
})

test('al cambiar la unidad aborta el stream anterior y recarga con la nueva', async () => {
  const stream = crearStreamControlado()
  fetchMock.mockResolvedValue(stream.response)

  const { rerender, unmount } = renderHook(
    ({ unidadId }: { unidadId?: string }) => useDashboardStream(unidadId),
    { initialProps: { unidadId: undefined as string | undefined } },
  )
  await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1))
  const primeraSignal = (fetchMock.mock.calls[0][1] as RequestInit).signal as AbortSignal

  rerender({ unidadId: 'u-2' })

  await waitFor(() => expect(dashboardApi.get).toHaveBeenCalledWith('u-2'))
  expect(primeraSignal.aborted).toBe(true)
  await waitFor(() => {
    expect(fetchMock).toHaveBeenCalledWith('/api/dashboard/stream?unidadId=u-2', expect.anything())
  })
  unmount()
})
