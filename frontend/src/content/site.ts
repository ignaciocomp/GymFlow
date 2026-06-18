/**
 * Contenido estático del sitio web público (RF-19).
 *
 * Fuente única de verdad: TODO el texto/datos del sitio público viven acá.
 * Nada se hardcodea en los componentes.
 *
 * TODO (usuario): reemplazar los placeholders marcados con `// PLACEHOLDER`
 * por los datos reales (precios, horarios, direcciones, contacto) y subir las
 * fotos a `frontend/public/img/` antes de publicar.
 */

export interface SedeHorario {
  /** Etiqueta del rango de días, ej. "Lunes a Viernes". */
  dias: string
  /** Rango horario, ej. "06:00 – 23:00". */
  horas: string
}

export interface Sede {
  slug: string
  nombre: string
  direccion: string
  horarios: SedeHorario[]
  /** URL de Google Maps (link o embed). */
  mapsUrl: string
  /** Ruta de la foto en `public/`. */
  foto: string
  servicios: string[]
}

export interface Plan {
  nombre: string
  /** Precio formateado como string, ej. "$1.890 / mes". */
  precio: string
  beneficios: string[]
  /** Si es el plan recomendado (badge destacado). */
  destacado: boolean
}

export interface Clase {
  nombre: string
  descripcion: string
  /** Nombre de un ícono de lucide-react. */
  icono: string
}

export interface HorarioItem {
  hora: string
  clase: string
  sede: string
}

export interface HorarioDia {
  dia: string
  items: HorarioItem[]
}

export interface Contacto {
  whatsapp: string
  email: string
  instagram: string
}

export interface Site {
  nombre: string
  /** Marca corta para el logo del header/footer, ej. "Nuevo Malvín". */
  nombreCorto: string
  tagline: string
  descripcion: string
  url: string
  contacto: Contacto
  sedes: Sede[]
  planes: Plan[]
  clases: Clase[]
  horarios: HorarioDia[]
}

export const site = {
  nombre: 'Gimnasio Nuevo Malvín',
  nombreCorto: 'Nuevo Malvín', // marca corta para el logo
  tagline: 'Entrená sin límites', // PLACEHOLDER — ajustar al claim de la marca
  descripcion:
    'Gimnasio Nuevo Malvín es un gimnasio en Montevideo con dos sedes: Nuevo Malvín y Espacio Mora. Planes flexibles, clases grupales y entrenamiento funcional para todos los niveles.', // PLACEHOLDER — meta description default
  url: 'https://nuevomalvin.com.uy', // PLACEHOLDER — dominio real

  contacto: {
    whatsapp: '+59890000000', // PLACEHOLDER — número real (formato wa.me sin signos: ver ContactoPage)
    email: 'hola@nuevomalvin.com.uy', // PLACEHOLDER — mail real
    instagram: 'https://instagram.com/nuevomalvin', // PLACEHOLDER — usuario real
  },

  sedes: [
    {
      slug: 'espacio-mora',
      nombre: 'Espacio Mora',
      direccion: 'Av. Italia 1234, Montevideo', // PLACEHOLDER — dirección real
      horarios: [
        { dias: 'Lunes a Viernes', horas: '06:00 – 23:00' }, // PLACEHOLDER
        { dias: 'Sábados', horas: '08:00 – 20:00' }, // PLACEHOLDER
        { dias: 'Domingos y feriados', horas: '09:00 – 14:00' }, // PLACEHOLDER
      ],
      mapsUrl: 'https://maps.google.com/?q=Espacio+Mora+Montevideo', // PLACEHOLDER — link/embed real
      foto: '/img/sede-mora.jpg', // PLACEHOLDER — subir foto
      servicios: [
        'Sala de musculación',
        'Clases funcionales',
        'Vestuarios con duchas',
        'Estacionamiento',
      ], // PLACEHOLDER
    },
    {
      slug: 'nuevo-malvin',
      nombre: 'Nuevo Malvín',
      direccion: 'Av. Bolivia 5678, Montevideo', // PLACEHOLDER — dirección real
      horarios: [
        { dias: 'Lunes a Viernes', horas: '07:00 – 22:00' }, // PLACEHOLDER
        { dias: 'Sábados', horas: '09:00 – 18:00' }, // PLACEHOLDER
      ],
      mapsUrl: 'https://maps.google.com/?q=Nuevo+Malvin+Montevideo', // PLACEHOLDER — link/embed real
      foto: '/img/sede-malvin.jpg', // PLACEHOLDER — subir foto
      servicios: [
        'Sala de musculación',
        'Spinning',
        'Entrenamiento personalizado',
        'Cardio',
      ], // PLACEHOLDER
    },
  ],

  planes: [
    {
      nombre: 'Pase Libre',
      precio: '$1.290 / mes', // PLACEHOLDER — precio real
      beneficios: [
        'Acceso a una sede',
        'Sala de musculación',
        'App de seguimiento',
      ],
      destacado: false,
    },
    {
      nombre: 'Full',
      precio: '$1.890 / mes', // PLACEHOLDER — precio real
      beneficios: [
        'Acceso a las 2 sedes',
        'Todas las clases grupales',
        'App de seguimiento',
        'Sin cargo de inscripción',
      ],
      destacado: true,
    },
    {
      nombre: 'Anual',
      precio: '$18.900 / año', // PLACEHOLDER — precio real
      beneficios: [
        'Acceso a las 2 sedes',
        'Todas las clases grupales',
        '2 meses bonificados',
        'Congelamiento de cuota',
      ],
      destacado: false,
    },
  ],

  clases: [
    {
      nombre: 'Funcional',
      descripcion:
        'Entrenamiento de cuerpo completo con tu propio peso y elementos. Mejorá fuerza, movilidad y resistencia.', // PLACEHOLDER
      icono: 'Dumbbell',
    },
    {
      nombre: 'Spinning',
      descripcion:
        'Clases de ciclismo indoor con música y guía del profe. Alta quema de calorías en cada sesión.', // PLACEHOLDER
      icono: 'Bike',
    },
    {
      nombre: 'Cross Training',
      descripcion:
        'Circuitos de alta intensidad que combinan fuerza y cardio para llevarte al siguiente nivel.', // PLACEHOLDER
      icono: 'Activity',
    },
    {
      nombre: 'Yoga',
      descripcion:
        'Trabajo de flexibilidad, equilibrio y respiración para soltar tensiones y ganar control corporal.', // PLACEHOLDER
      icono: 'Flower2',
    },
  ],

  horarios: [
    {
      dia: 'Lunes',
      items: [
        { hora: '19:00', clase: 'Funcional', sede: 'Espacio Mora' }, // PLACEHOLDER
        { hora: '20:00', clase: 'Spinning', sede: 'Nuevo Malvín' }, // PLACEHOLDER
      ],
    },
    {
      dia: 'Martes',
      items: [
        { hora: '08:00', clase: 'Yoga', sede: 'Espacio Mora' }, // PLACEHOLDER
        { hora: '19:00', clase: 'Cross Training', sede: 'Espacio Mora' }, // PLACEHOLDER
      ],
    },
    {
      dia: 'Miércoles',
      items: [
        { hora: '19:00', clase: 'Funcional', sede: 'Espacio Mora' }, // PLACEHOLDER
        { hora: '20:00', clase: 'Spinning', sede: 'Nuevo Malvín' }, // PLACEHOLDER
      ],
    },
    {
      dia: 'Jueves',
      items: [
        { hora: '08:00', clase: 'Yoga', sede: 'Espacio Mora' }, // PLACEHOLDER
        { hora: '19:00', clase: 'Cross Training', sede: 'Espacio Mora' }, // PLACEHOLDER
      ],
    },
    {
      dia: 'Viernes',
      items: [
        { hora: '19:00', clase: 'Funcional', sede: 'Espacio Mora' }, // PLACEHOLDER
        { hora: '20:00', clase: 'Spinning', sede: 'Nuevo Malvín' }, // PLACEHOLDER
      ],
    },
    {
      dia: 'Sábado',
      items: [
        { hora: '10:00', clase: 'Cross Training', sede: 'Nuevo Malvín' }, // PLACEHOLDER
      ],
    },
  ],
} as const satisfies Site
