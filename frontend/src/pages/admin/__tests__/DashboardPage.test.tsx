import { render, screen, fireEvent } from '@testing-library/react'
import { vi, beforeEach, afterEach, test, expect } from 'vitest'
import DashboardPage from '@/pages/admin/DashboardPage'
import { useDashboardStream } from '@/hooks/useDashboardStream'
import type { DashboardStreamState } from '@/hooks/useDashboardStream'
import type { DashboardDto } from '@/types'

vi.mock('@/hooks/useDashboardStream', () => ({
  useDashboardStream: vi.fn(),
}))

const DTO: DashboardDto = {
  generadoEn: '2026-07-04T12:30:45Z',
  unidades: [
    { id: 'u-1', nombre: 'Sede Centro' },
    { id: 'u-2', nombre: 'Sede Este' },
  ],
  sociosActivos: {
    total: 128,
    porUnidad: [
      { unidadId: 'u-1', unidadNombre: 'Sede Centro', cantidad: 80 },
      { unidadId: 'u-2', unidadNombre: 'Sede Este', cantidad: 48 },
    ],
  },
  cuotas: { proximasAVencer: 6, vencidas: 3, pagadasMes: 41 },
  clasesDelDia: [
    { clase: 'Spinning', unidad: 'Sede Centro', horaInicio: '08:00', horaFin: '09:00', cupo: 20, inscriptos: 12 },
    { clase: 'Funcional', unidad: 'Sede Este', horaInicio: '18:30', horaFin: '19:30', cupo: 15, inscriptos: 15 },
  ],
  inscripcionesRecientes: [
    { socio: 'Ana García', clase: 'Spinning', unidad: 'Sede Centro', fecha: '2026-07-04T11:00:00Z' },
  ],
  grafica: {
    sociosPorSede: [
      { sede: 'Sede Centro', cantidad: 80 },
      { sede: 'Sede Este', cantidad: 48 },
    ],
    cuotasPorEstado: [
      { estado: 'Pagada', cantidad: 41 },
      { estado: 'Pendiente', cantidad: 9 },
    ],
    inscripcionesUltimos7Dias: [
      { fecha: '2026-06-28', cantidad: 2 },
      { fecha: '2026-06-29', cantidad: 0 },
      { fecha: '2026-06-30', cantidad: 1 },
      { fecha: '2026-07-01', cantidad: 4 },
      { fecha: '2026-07-02', cantidad: 3 },
      { fecha: '2026-07-03', cantidad: 0 },
      { fecha: '2026-07-04', cantidad: 5 },
    ],
  },
}

const DTO_VACIO: DashboardDto = {
  generadoEn: '2026-07-04T12:30:45Z',
  unidades: [],
  sociosActivos: { total: 0, porUnidad: [] },
  cuotas: { proximasAVencer: 0, vencidas: 0, pagadasMes: 0 },
  clasesDelDia: [],
  inscripcionesRecientes: [],
  grafica: { sociosPorSede: [], cuotasPorEstado: [], inscripcionesUltimos7Dias: [] },
}

function mockStream(estado: Partial<DashboardStreamState>) {
  vi.mocked(useDashboardStream).mockReturnValue({
    data: DTO,
    live: true,
    actualizadoEn: new Date('2026-07-04T12:30:45'),
    ...estado,
  })
}

beforeEach(() => {
  vi.clearAllMocks()
  mockStream({})
})

afterEach(() => {
  localStorage.clear()
})

test('renderiza las 4 métricas con el desglose por sede', () => {
  render(<DashboardPage />)

  // Socios activos con desglose.
  expect(screen.getByText('Socios activos')).toBeInTheDocument()
  expect(screen.getByText('128')).toBeInTheDocument()
  expect(screen.getAllByText(/Sede Centro/).length).toBeGreaterThan(0)
  expect(screen.getByText('80')).toBeInTheDocument()

  // Cuotas próximas a vencer y vencidas (RN-17).
  expect(screen.getByText(/Próximas a vencer/i)).toBeInTheDocument()
  expect(screen.getByText('6')).toBeInTheDocument()
  expect(screen.getByText(/Cuotas vencidas/i)).toBeInTheDocument()
  expect(screen.getByText('3')).toBeInTheDocument()

  // Clases de hoy: cantidad de horarios del día.
  expect(screen.getByText('Clases de hoy')).toBeInTheDocument()
  expect(screen.getByText('2')).toBeInTheDocument()
})

test('lista las clases del día con horario e inscriptos/cupo y las inscripciones recientes', () => {
  render(<DashboardPage />)

  expect(screen.getByText('Spinning')).toBeInTheDocument()
  expect(screen.getByText('08:00–09:00')).toBeInTheDocument()
  expect(screen.getByText(/12\/20/)).toBeInTheDocument()
  expect(screen.getByText(/15\/15/)).toBeInTheDocument()

  expect(screen.getByText('Ana García')).toBeInTheDocument()
})

test('la gráfica arranca en socios por sede y el selector cambia la vista persistiéndola', () => {
  render(<DashboardPage />)

  const botonSocios = screen.getByRole('button', { name: /Socios por sede/i })
  const botonCuotas = screen.getByRole('button', { name: /Cuotas por estado/i })
  expect(botonSocios).toHaveAttribute('aria-pressed', 'true')
  expect(botonCuotas).toHaveAttribute('aria-pressed', 'false')

  fireEvent.click(botonCuotas)

  expect(botonCuotas).toHaveAttribute('aria-pressed', 'true')
  expect(botonSocios).toHaveAttribute('aria-pressed', 'false')
  expect(localStorage.getItem('gymflow_dashboard_grafica')).toBe('cuotasPorEstado')
})

test('restaura la vista de gráfica guardada en localStorage', () => {
  localStorage.setItem('gymflow_dashboard_grafica', 'inscripcionesUltimos7Dias')

  render(<DashboardPage />)

  expect(screen.getByRole('button', { name: /Inscripciones/i })).toHaveAttribute('aria-pressed', 'true')
  expect(screen.getByRole('button', { name: /Socios por sede/i })).toHaveAttribute('aria-pressed', 'false')
})

test('muestra el badge En vivo con la hora de actualización cuando el stream está activo', () => {
  render(<DashboardPage />)

  expect(screen.getByText('En vivo')).toBeInTheDocument()
  expect(screen.queryByText(/Actualización en pausa/i)).not.toBeInTheDocument()
  expect(screen.getByText(/Actualizado 12:30:45/)).toBeInTheDocument()
})

test('muestra el badge de pausa cuando el stream degradó a polling (live=false)', () => {
  mockStream({ live: false })

  render(<DashboardPage />)

  expect(screen.getByText(/Actualización en pausa/i)).toBeInTheDocument()
  expect(screen.queryByText('En vivo')).not.toBeInTheDocument()
})

test('una unidad sin datos renderiza ceros y estados vacíos sin error (E3)', () => {
  mockStream({ data: DTO_VACIO })

  render(<DashboardPage />)

  // Los counts en cero se muestran (no rompe ni oculta las cards).
  expect(screen.getAllByText('0').length).toBeGreaterThanOrEqual(4)
  expect(screen.getByText(/No hay clases programadas para hoy/i)).toBeInTheDocument()
  expect(screen.getByText(/Todavía no hay inscripciones recientes/i)).toBeInTheDocument()
})
