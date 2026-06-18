# Spec — RF-19: Sitio web público

- **RF:** RF-19 (Sitio web público) — pendiente de Iteración 4 (issue #37).
- **Fecha:** 2026-06-18
- **Estado:** Spec para revisión del usuario.
- **Relacionado:** issue #23 (RF-19 adelantado a It4).

## 1. Contexto y objetivo

GymFlow es un sistema de gestión para un gimnasio multi-sede (**Espacio Mora** y **Gimnasio Nuevo Malvín**, Montevideo). Hoy el frontend es un SPA privado: al entrar a `/` redirige a `/login`, y todo cuelga de `/admin` (staff) y `/portal` (socios).

El objetivo de RF-19 es agregar un **sitio web público institucional** —sin login— que presente el gimnasio a visitantes: quiénes son, dónde están, qué planes y clases ofrecen, y cómo contactarlos. Responsive y con SEO básico.

## 2. Decisiones de diseño (acordadas)

| Decisión | Elección | Motivo |
|---|---|---|
| Contenido | **Estático** (hardcodeado en el front, sin API) | Alcance acotado; no requiere endpoints públicos nuevos ni exponer datos del sistema. |
| Estructura | **Multipágina** (`/`, `/sedes`, `/planes`, `/clases`, `/contacto`) | Más institucional; URLs limpias y SEO por página. |
| Estilo visual | **Dark, alto contraste, acento celeste/azul** (del logo) | Vibra de gimnasio moderno, alineado a la marca. |
| Integración | **Mismo SPA / mismo contenedor** | Cero infra nueva; reusa build, deploy y SPA fallback existentes. |
| SEO | Meta tags por página + OG + `sitemap.xml` + `robots.txt`, HTML semántico | Suficiente para el alcance; sin SSR/prerender (YAGNI). |
| Contacto | Links directos (WhatsApp / mail / mapa) | Sin formulario → sin backend nuevo. |

## 3. Alcance

### Incluye
- 5 páginas públicas responsive: Inicio, Sedes, Planes, Clases, Contacto.
- `PublicLayout` con header (nav + botón "Acceder") y footer.
- Contenido centralizado y tipado en un único módulo editable (`content.ts`).
- Paleta y tipografía propias del sitio público (dark + celeste/azul), aisladas del panel.
- SEO: títulos/descripciones por ruta, Open Graph, `sitemap.xml`, `robots.txt`, `lang="es"`, `alt` en imágenes, headings semánticos.
- Tests de las páginas y el layout (render + contenido + navegación).

### No incluye (fuera de alcance — YAGNI)
- Integración en vivo con la API (planes/horarios reales por endpoint).
- Formulario de contacto con envío de mail.
- CMS / edición de contenido desde el panel admin.
- Multi-idioma.
- SSR / prerendering / generación estática.
- Blog, testimonios dinámicos, pasarela de pago.

## 4. Arquitectura e integración

### 4.1 Routing (`src/App.tsx`)
Hoy `/` y `*` redirigen a `/login`. Cambian así:

```
<Route element={<PublicLayout />}>
  <Route path="/" element={<HomePage />} />
  <Route path="/sedes" element={<SedesPage />} />
  <Route path="/planes" element={<PlanesPublicPage />} />
  <Route path="/clases" element={<ClasesPublicPage />} />
  <Route path="/contacto" element={<ContactoPage />} />
</Route>
<Route path="/login" element={<Login />} />     // sin cambios
<Route path="/admin" ...>   // sin cambios (auth en AdminLayout)
<Route path="/portal" ...>  // sin cambios (auth en SocioLayout)
<Route path="*" element={<NotFoundPage />} />   // 404 público (antes iba a /login)
```

- **No toca la autenticación.** El guard de auth vive dentro de `AdminLayout`/`SocioLayout`; las rutas públicas simplemente no usan esos layouts, así que son públicas por construcción.
- El botón "Acceder" del header linkea a `/login` (socios y staff entran por ahí, como hoy).
- **Sin cambios de backend:** `Program.cs` ya hace `app.MapFallbackToFile("index.html")`, así que los deep-links públicos (`/sedes`, etc.) se sirven correctamente.

### 4.2 SEO sin dependencia nueva
React 18 (no hoistea `<title>`/`<meta>` nativo) + react-router en modo librería (sin meta API). Para evitar sumar `react-helmet-async`, se crea un componente liviano **`<Seo title description image path />`** que en un `useEffect` setea `document.title` y los `<meta>`/OG (creándolos/actualizándolos y limpiándolos al desmontar). Cada página renderiza su `<Seo>`. (Alternativa documentada: `react-helmet-async`, si más adelante se quiere algo más robusto.)

Además:
- `frontend/index.html`: `lang="es"`, `<title>` y `<meta name="description">` por defecto, favicon propio, viewport (ya está).
- `frontend/public/sitemap.xml` y `frontend/public/robots.txt`: estáticos, servidos tal cual.

### 4.3 Tema visual (Tailwind v4)
El proyecto usa Tailwind v4 con `@theme` en `index.css` (shadcn, Inter). Se agrega un **bloque de tokens del sitio público** (celeste/azul + neutros oscuros) y las páginas públicas se renderizan sobre un contenedor con fondo oscuro propio (`bg-[var(--public-bg)]` o clase `.public`), sin alterar el tema del panel. El acento exacto se calibra con el **logo** cuando el usuario lo provea.

## 5. Estructura de archivos nueva

```
frontend/src/
├── pages/public/
│   ├── HomePage.tsx
│   ├── SedesPage.tsx
│   ├── PlanesPublicPage.tsx
│   ├── ClasesPublicPage.tsx
│   ├── ContactoPage.tsx
│   └── NotFoundPage.tsx
├── components/public/
│   ├── PublicLayout.tsx        // <Outlet/> + header + footer
│   ├── PublicHeader.tsx        // nav desktop + menú mobile (hamburguesa)
│   ├── PublicFooter.tsx
│   ├── Seo.tsx                 // meta tags por página (useEffect)
│   ├── Hero.tsx
│   ├── SedeCard.tsx
│   ├── PlanCard.tsx
│   ├── ClaseHorarioTabla.tsx
│   └── Section.tsx             // wrapper de sección con título
├── content/
│   └── site.ts                 // TODO el contenido estático, tipado
└── public/
    ├── sitemap.xml
    ├── robots.txt
    └── img/                    // fotos del gym + logo (placeholders por ahora)
```

## 6. Modelo de contenido (`content/site.ts`)

Fuente única de verdad, tipada, para editar en un solo lugar:

```ts
export const site = {
  nombre: 'GymFlow',
  tagline: 'Entrená sin límites',
  descripcion: '...',           // meta description default
  url: 'https://gymflow.uy',
  whatsapp: '+598...', email: '...', instagram: '...',
  sedes: [
    { slug:'espacio-mora', nombre:'Espacio Mora', direccion:'...', horarios:[...],
      mapsUrl:'...', foto:'/img/sede-mora.jpg', servicios:[...] },
    { slug:'nuevo-malvin', nombre:'Gimnasio Nuevo Malvín', ... },
  ],
  planes: [ { nombre:'Mensual', precio:'$...', beneficios:[...], destacado:false }, ... ],
  clases: [ { nombre:'Funcional', descripcion:'...', icono:'...' }, ... ],
  horarios: [ { dia:'Lunes', items:[ { hora:'19:00', clase:'Funcional', sede:'...' } ] }, ... ],
} as const
```

Valores reales (precios, horarios, direcciones, contacto) los completa el usuario; el spec arranca con placeholders realistas.

## 7. Detalle de páginas

- **Inicio (`/`)** — `Hero` (foto + tagline + 2 CTAs: "Ver planes" y "Conocé las sedes") · franja "por qué elegirnos" (3–4 cards con ícono) · preview de las 2 sedes (link a `/sedes`) · preview de clases (link a `/clases`) · CTA final + footer.
- **Sedes (`/sedes`)** — una `SedeCard` por sede: foto, dirección, horarios, mapa (iframe Google Maps o link), servicios.
- **Planes (`/planes`)** — grilla de `PlanCard` con precio y beneficios; uno opcionalmente "destacado".
- **Clases (`/clases`)** — tipos de clase (cards) + `ClaseHorarioTabla` (grilla semanal); responsive (tabla → lista apilada en mobile).
- **Contacto (`/contacto`)** — datos (tel/WhatsApp, mail, redes) + ubicación con mapa; botones a WhatsApp/mail.
- **404 público** — mensaje + link al inicio.

## 8. Diseño visual

- **Paleta:** fondo casi-negro/azulado, superficies oscuras, texto claro, **acento celeste/azul** del logo (calibrar con el logo real). Definida como tokens en `index.css`.
- **Tipografía:** Inter (ya en el proyecto); headings bold, jerarquía marcada.
- **Componentes:** botones (primario lleno celeste / secundario outline), cards de superficie oscura, secciones con buen aire.
- **Responsive:** mobile-first; header colapsa a menú hamburguesa; grids `auto-fit`.
- **Imágenes:** placeholders ahora; reemplazables sin tocar layout (rutas en `content/site.ts`).
- La implementación usa el plugin **UI/UX Pro Max** para el detalle visual.

## 9. Testing

Con Vitest + React Testing Library (stack ya presente):
- `PublicLayout` renderiza header (con los 5 links + "Acceder") y footer.
- Cada página renderiza su `<Seo>` y su contenido clave desde `content/site.ts` (ej. nombres de las sedes, al menos un plan, una clase).
- La home muestra los CTAs y los previews enlazados correctamente.
- 404 renderiza y linkea a `/`.
- (Opcional) test de que `/` ya **no** redirige a `/login`.

## 10. Criterios de aceptación

1. Un visitante sin sesión navega `/`, `/sedes`, `/planes`, `/clases`, `/contacto` sin que se le pida login.
2. Los deep-links públicos funcionan al recargar (SPA fallback).
3. `/admin` y `/portal` siguen protegidos e intactos; "Acceder" lleva a `/login`.
4. Cada página tiene `<title>` y meta description propios; existe `sitemap.xml` y `robots.txt`; `lang="es"`.
5. Responsive verificado en mobile y desktop.
6. El contenido sale 100% de `content/site.ts` (un solo lugar para editar).
7. Tests verdes; build de producción OK.

## 11. Pendientes del usuario (para la implementación final)

- **Logo** (para calibrar el celeste/azul exacto y el header/footer).
- **Fotos** del gym (hero, 1 por sede, 4–6 para galería/secciones).
- **Datos reales:** precios de planes, horarios de clases, direcciones, teléfono/WhatsApp, mail, redes.

> Hasta tenerlos, se implementa con placeholders realistas y se reemplazan sin cambiar el layout.
