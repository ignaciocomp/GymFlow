import {
  Dumbbell,
  Bike,
  Activity,
  Flower2,
  type LucideIcon,
} from 'lucide-react'

/**
 * Mapa de los nombres de íconos usados en `site.clases[].icono` a los
 * componentes de lucide-react. Si llega un nombre desconocido cae a Dumbbell,
 * así nunca rompe el render por un ícono faltante.
 */
const ICONS: Record<string, LucideIcon> = {
  Dumbbell,
  Bike,
  Activity,
  Flower2,
}

export function getClaseIcon(name: string): LucideIcon {
  return ICONS[name] ?? Dumbbell
}
