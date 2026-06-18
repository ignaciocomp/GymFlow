import { useState } from 'react'
import { ImageIcon, type LucideIcon } from 'lucide-react'

interface PublicImageProps {
  /** Ruta de la imagen real (ej. site.sedes[].foto). Puede no existir aún. */
  src?: string
  /** Texto alternativo accesible (obligatorio para imágenes con contenido). */
  alt: string
  /** Ícono de lucide para el placeholder mientras no haya foto real. */
  icon?: LucideIcon
  /** Etiqueta tenue mostrada en el placeholder. */
  label?: string
  className?: string
}

/**
 * Imagen del sitio público con fallback a placeholder.
 *
 * Las fotos reales todavía no están subidas a `public/img/`. Para evitar el
 * ícono de imagen rota del navegador, intentamos cargar `src` y, si falla
 * (404) o no hay `src`, mostramos un bloque de superficie oscura con un ícono
 * de lucide. Cuando el usuario suba las fotos a la ruta indicada, aparecen
 * solas sin tocar el layout.
 */
export default function PublicImage({
  src,
  alt,
  icon: Icon = ImageIcon,
  label,
  className = '',
}: PublicImageProps) {
  const [failed, setFailed] = useState(false)
  const showImg = Boolean(src) && !failed

  return (
    <div
      className={`relative overflow-hidden bg-[var(--public-surface-2)] ${className}`}
    >
      {showImg ? (
        <img
          src={src}
          alt={alt}
          loading="lazy"
          className="h-full w-full object-cover"
          onError={() => setFailed(true)}
        />
      ) : (
        <div
          role="img"
          aria-label={alt}
          className="grid h-full w-full place-items-center bg-gradient-to-br from-[var(--public-surface-2)] to-[var(--public-surface)]"
        >
          <div className="flex flex-col items-center gap-2 px-4 py-8 text-center text-[var(--public-muted)]">
            <Icon className="h-10 w-10 opacity-60" strokeWidth={1.5} aria-hidden="true" />
            {label && <span className="text-xs font-medium opacity-70">{label}</span>}
          </div>
        </div>
      )}
    </div>
  )
}
