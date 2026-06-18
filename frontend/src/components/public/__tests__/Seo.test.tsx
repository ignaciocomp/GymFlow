import { render } from '@testing-library/react'
import { Seo } from '@/components/public/Seo'

test('setea document.title y meta description', () => {
  render(<Seo title="Planes — GymFlow" description="Conocé nuestros planes" />)
  expect(document.title).toBe('Planes — GymFlow')
  const meta = document.querySelector('meta[name="description"]')
  expect(meta?.getAttribute('content')).toBe('Conocé nuestros planes')
})
