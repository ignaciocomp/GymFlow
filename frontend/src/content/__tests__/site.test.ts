import { site } from '@/content/site'

test('hay exactamente 2 sedes con campos requeridos', () => {
  expect(site.sedes).toHaveLength(2)
  for (const s of site.sedes) {
    expect(s.slug).toBeTruthy()
    expect(s.nombre).toBeTruthy()
    expect(s.direccion).toBeTruthy()
    expect(Array.isArray(s.horarios)).toBe(true)
  }
})

test('hay al menos un plan y una clase', () => {
  expect(site.planes.length).toBeGreaterThan(0)
  expect(site.clases.length).toBeGreaterThan(0)
})

test('datos de contacto presentes', () => {
  expect(site.contacto.whatsapp).toBeTruthy()
  expect(site.contacto.email).toBeTruthy()
})
