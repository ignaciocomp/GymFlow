import { useEffect, useState } from 'react'
import { dashboardApi } from '@/services/api'
import type { DashboardDto } from '@/types'

export interface DashboardStreamState {
  /** Último snapshot recibido (por GET o por el stream). */
  data: DashboardDto | null
  /** true mientras el stream SSE está entregando datos; false en carga inicial o polling. */
  live: boolean
  /** Momento local de la última actualización, para el "Actualizado hh:mm:ss". */
  actualizadoEn: Date | null
}

/** Reintentos de conexión al stream antes de degradar a polling (E2 del CU-10). */
const MAX_REINTENTOS = 2
/** Base del backoff exponencial entre reintentos: 1s, 2s. */
const BACKOFF_BASE_MS = 1_000
/** Intervalo del polling de fallback. */
const POLLING_MS = 15_000

/**
 * RF-18: mantiene el dashboard actualizado en tiempo real.
 *
 * `EventSource` no permite mandar el header Authorization (y el JWT no puede ir en la URL),
 * así que el stream SSE se consume con `fetch` leyendo `response.body` y parseando las
 * líneas `data:` a mano (con buffer para chunks parciales). Si el stream falla, reintenta
 * con backoff hasta MAX_REINTENTOS y después degrada a polling por GET con `live=false`.
 */
export function useDashboardStream(unidadId?: string): DashboardStreamState {
  const [state, setState] = useState<DashboardStreamState>({
    data: null,
    live: false,
    actualizadoEn: null,
  })

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller
    let pollTimer: ReturnType<typeof setInterval> | null = null
    let retryTimer: ReturnType<typeof setTimeout> | null = null

    const aplicar = (data: DashboardDto, live: boolean) => {
      if (signal.aborted) return
      setState({ data, live, actualizadoEn: new Date() })
    }

    const cargarSnapshot = async (live: boolean) => {
      try {
        aplicar(await dashboardApi.get(unidadId), live)
      } catch {
        // Falla puntual del GET: conservamos el último snapshot conocido.
      }
    }

    const iniciarPolling = () => {
      if (signal.aborted) return
      setState(prev => ({ ...prev, live: false }))
      pollTimer = setInterval(() => { void cargarSnapshot(false) }, POLLING_MS)
    }

    const abrirStream = async (intento: number): Promise<void> => {
      try {
        const qs = unidadId ? `?unidadId=${encodeURIComponent(unidadId)}` : ''
        const response = await fetch(`/api/dashboard/stream${qs}`, {
          headers: { Authorization: `Bearer ${localStorage.getItem('gymflow_token')}` },
          signal,
        })
        if (!response.ok || !response.body) throw new Error(`stream HTTP ${response.status}`)

        const reader = response.body.getReader()
        const decoder = new TextDecoder()
        let buffer = ''
        for (;;) {
          const { done, value } = await reader.read()
          if (done) break
          buffer += decoder.decode(value, { stream: true })
          // Las líneas completas terminan en \n; lo que quede sin \n es un chunk parcial.
          const lineas = buffer.split('\n')
          buffer = lineas.pop() ?? ''
          for (const linea of lineas) {
            if (!linea.startsWith('data:')) continue // heartbeats (: ping) y líneas vacías
            const json = linea.slice('data:'.length).trim()
            if (!json) continue
            aplicar(JSON.parse(json) as DashboardDto, true)
            intento = 0 // con datos fluyendo, un corte futuro arranca los reintentos de cero
          }
        }
        // El servidor cerró el stream de forma ordenada: tratarlo como corte.
        throw new Error('stream cerrado por el servidor')
      } catch (err) {
        if (signal.aborted) return
        if (intento < MAX_REINTENTOS) {
          setState(prev => ({ ...prev, live: false }))
          retryTimer = setTimeout(() => { void abrirStream(intento + 1) }, BACKOFF_BASE_MS * 2 ** intento)
        } else {
          iniciarPolling()
        }
      }
    }

    void cargarSnapshot(false).then(() => {
      if (!signal.aborted) void abrirStream(0)
    })

    return () => {
      controller.abort()
      if (pollTimer) clearInterval(pollTimer)
      if (retryTimer) clearTimeout(retryTimer)
    }
  }, [unidadId])

  return state
}
