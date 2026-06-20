import { formatDate, formatDateTime } from '@/lib/utils'

describe('formatDate', () => {
  test('formatea ISO con hora a dd/mm/aaaa con padding de ceros', () => {
    // El bug original (toLocaleDateString sin opciones) mostraba "20/6/2026".
    expect(formatDate('2026-06-20T00:00:00Z')).toBe('20/06/2026')
  })

  test('formatea fecha simple con padding de ceros en dia y mes', () => {
    expect(formatDate('2026-01-05')).toBe('05/01/2026')
  })

  test('devuelve string vacio para null', () => {
    expect(formatDate(null)).toBe('')
  })

  test('devuelve string vacio para undefined', () => {
    expect(formatDate(undefined)).toBe('')
  })

  test('devuelve string vacio para cadena vacia', () => {
    expect(formatDate('')).toBe('')
  })
})

describe('formatDateTime', () => {
  test('incluye fecha dd/mm/aaaa y hora HH:mm en formato es-UY', () => {
    const out = formatDateTime('2026-06-20T11:30:00')
    expect(out).toContain('20/06/2026')
    expect(out).toContain('11:30')
  })

  test('devuelve string vacio para null', () => {
    expect(formatDateTime(null)).toBe('')
  })

  test('devuelve string vacio para undefined', () => {
    expect(formatDateTime(undefined)).toBe('')
  })
})
