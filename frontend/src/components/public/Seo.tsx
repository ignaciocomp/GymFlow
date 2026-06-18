import { useEffect } from 'react'
import { site } from '@/content/site'

export interface SeoProps {
  /** Título de la pestaña / <title>. */
  title: string
  /** Meta description de la página. */
  description: string
  /** URL de la imagen para Open Graph (absoluta o relativa). */
  image?: string
  /** Path de la página, ej. "/planes" — se compone con site.url para og:url. */
  path?: string
}

/**
 * Componente de SEO sin dependencias nuevas.
 *
 * React 18 no hoistea <title>/<meta> y react-router (modo librería) no tiene
 * meta API, así que seteamos los tags imperativamente en un useEffect:
 * document.title, <meta name="description"> y los Open Graph
 * (og:title, og:description, og:image, og:url, og:type).
 *
 * No renderiza nada (devuelve null).
 */
export function Seo({ title, description, image, path }: SeoProps) {
  useEffect(() => {
    const prevTitle = document.title
    document.title = title

    const url = path ? `${site.url}${path}` : site.url
    const ogImage = image ?? `${site.url}/img/og-default.jpg`

    // Crea o actualiza un <meta> por nombre/propiedad; recuerda si lo creamos
    // para poder limpiarlo al desmontar.
    const created: HTMLMetaElement[] = []
    const setMeta = (attr: 'name' | 'property', key: string, content: string) => {
      let el = document.head.querySelector<HTMLMetaElement>(`meta[${attr}="${key}"]`)
      if (!el) {
        el = document.createElement('meta')
        el.setAttribute(attr, key)
        document.head.appendChild(el)
        created.push(el)
      }
      el.setAttribute('content', content)
    }

    setMeta('name', 'description', description)
    setMeta('property', 'og:title', title)
    setMeta('property', 'og:description', description)
    setMeta('property', 'og:image', ogImage)
    setMeta('property', 'og:url', url)
    setMeta('property', 'og:type', 'website')

    return () => {
      document.title = prevTitle
      for (const el of created) el.remove()
    }
  }, [title, description, image, path])

  return null
}

export default Seo
