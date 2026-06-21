import { render } from '@testing-library/react'
import { Seo } from '@/components/public/Seo'

test('setea document.title y meta description', () => {
  render(<Seo title="Planes — Gimnasio Nuevo Malvín" description="Conocé nuestros planes" />)
  expect(document.title).toBe('Planes — Gimnasio Nuevo Malvín')
  const meta = document.querySelector('meta[name="description"]')
  expect(meta?.getAttribute('content')).toBe('Conocé nuestros planes')
})
