# RF-19 Sitio Web Público — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **REQUIRED FOR VISUALS:** Use the `ui-ux-pro-max` plugin for all visual styling work (layout, color, typography, polish). TDD guards content/routing/SEO; the plugin drives the look.

**Goal:** Agregar un sitio web público institucional (sin login) al SPA de GymFlow: 5 páginas (Inicio, Sedes, Planes, Clases, Contacto) + 404, responsive, dark con acento celeste/azul, con SEO básico y contenido estático centralizado.

**Architecture:** Rutas públicas nuevas en el mismo `react-router` del SPA, envueltas en un `PublicLayout` (header + footer), fuera de los layouts protegidos (`AdminLayout`/`SocioLayout`) — así son públicas por construcción y no tocan la auth. Todo el contenido vive tipado en `src/content/site.ts` (fuente única de verdad). SEO por página con un componente propio `<Seo>` (sin dependencias nuevas). Sin cambios de backend: `Program.cs` ya hace `MapFallbackToFile("index.html")`.

**Tech Stack:** React 18 + Vite + TypeScript, react-router-dom v7 (modo librería), Tailwind v4 (`@theme` en `index.css`, shadcn, Inter), Vitest 2 + React Testing Library + jsdom. Plugin `ui-ux-pro-max` para diseño.

**Spec:** `docs/specs/spec-rf19-sitio-publico.md`

**Branch:** `feature/rf19-sitio-publico` (ya creada off `main`).

**Pendientes del usuario (placeholders hasta tenerlos):** logo (color exacto), fotos, datos reales (precios/horarios/direcciones/contacto).

---

## File Structure

```
frontend/
├── src/
│   ├── content/
│   │   └── site.ts                      # CREATE — contenido estático tipado
│   ├── components/public/
│   │   ├── Seo.tsx                       # CREATE — meta tags por página
│   │   ├── PublicLayout.tsx             # CREATE — header + <Outlet/> + footer
│   │   ├── PublicHeader.tsx             # CREATE — nav desktop + menú mobile
│   │   ├── PublicFooter.tsx             # CREATE
│   │   ├── Section.tsx                  # CREATE — wrapper de sección
│   │   ├── Hero.tsx                      # CREATE
│   │   ├── SedeCard.tsx                 # CREATE
│   │   ├── PlanCard.tsx                 # CREATE
│   │   └── ClaseHorarioTabla.tsx        # CREATE
│   ├── pages/public/
│   │   ├── HomePage.tsx                 # CREATE
│   │   ├── SedesPage.tsx                # CREATE
│   │   ├── PlanesPublicPage.tsx         # CREATE
│   │   ├── ClasesPublicPage.tsx         # CREATE
│   │   ├── ContactoPage.tsx             # CREATE
│   │   └── NotFoundPage.tsx             # CREATE
│   ├── test/
│   │   └── setup.ts                     # CREATE — import '@testing-library/jest-dom'
│   ├── App.tsx                          # MODIFY — rutas públicas + 404
│   └── index.css                        # MODIFY — tokens tema público (dark + celeste)
├── index.html                          # MODIFY — lang=es, title, meta description
├── vite.config.ts                      # MODIFY — setupFiles
├── package.json                        # MODIFY — script "test"
└── public/
    ├── sitemap.xml                     # CREATE
    └── robots.txt                      # CREATE
```

Cada test va junto a su unidad en `__tests__/` (patrón existente del repo, ej. `src/services/__tests__/...`). Para componentes: `src/components/public/__tests__/*.test.tsx` y `src/pages/public/__tests__/*.test.tsx`.

---

## Task 1: Tooling de tests (setup jest-dom + script)

**Files:**
- Create: `frontend/src/test/setup.ts`
- Modify: `frontend/vite.config.ts:25`
- Modify: `frontend/package.json` (scripts)

- [ ] **Step 1: Crear el setup file**

```ts
// frontend/src/test/setup.ts
import '@testing-library/jest-dom'
```

- [ ] **Step 2: Wire en vite.config.ts**

Cambiar `setupFiles: [],` por:
```ts
setupFiles: ['./src/test/setup.ts'],
```

- [ ] **Step 3: Agregar script de test en package.json**

En `"scripts"` agregar:
```json
"test": "vitest run",
"test:watch": "vitest"
```

- [ ] **Step 4: Verificar que corre**

Run: `npm test` (desde `frontend/`)
Expected: PASS (corre los tests existentes; `passWithNoTests` cubre el resto).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/test/setup.ts frontend/vite.config.ts frontend/package.json
git commit -m "test(frontend): setup jest-dom + script test para vitest"
```

---

## Task 2: Contenido estático tipado (`content/site.ts`)

**Files:**
- Create: `frontend/src/content/site.ts`
- Test: `frontend/src/content/__tests__/site.test.ts`

- [ ] **Step 1: Test de invariantes del contenido**

```ts
// site.test.ts
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
```

- [ ] **Step 2: Run → FAIL** (`npm test -- site.test`) — módulo inexistente.

- [ ] **Step 3: Implementar `site.ts`** con tipos + placeholders realistas (nombre, tagline, descripcion, url, contacto {whatsapp,email,instagram}, sedes[2] {slug,nombre,direccion,horarios[],mapsUrl,foto,servicios[]}, planes[] {nombre,precio,beneficios[],destacado}, clases[] {nombre,descripcion,icono}, horarios[] {dia, items[{hora,clase,sede}]}). Exportar `as const`. Marcar con comentarios los valores que el usuario debe reemplazar.

- [ ] **Step 4: Run → PASS**

- [ ] **Step 5: Commit** — `feat(public): contenido estatico tipado del sitio (content/site.ts)`

---

## Task 3: Tokens de tema público (dark + celeste) en `index.css`

**Files:**
- Modify: `frontend/src/index.css` (agregar bloque de variables del sitio público)

> Nota: no hay test unitario de CSS. Verificación visual + que el build no rompa.

- [ ] **Step 1: Agregar tokens** (custom properties) para el sitio público: `--public-bg` (casi negro azulado), `--public-surface`, `--public-text`, `--public-muted`, `--public-accent` (celeste/azul placeholder, ej. `#38BDF8`), `--public-accent-ink` (texto sobre acento). Scopearlos a una clase `.public-site` que envuelve el `PublicLayout`. El acento exacto se ajusta cuando llegue el logo.

- [ ] **Step 2: Verificar build** — Run: `npm run build` → Expected: OK.

- [ ] **Step 3: Commit** — `style(public): tokens de tema dark + acento celeste para el sitio publico`

---

## Task 4: `<Seo>` (meta tags por página)

**Files:**
- Create: `frontend/src/components/public/Seo.tsx`
- Test: `frontend/src/components/public/__tests__/Seo.test.tsx`

- [ ] **Step 1: Test**

```tsx
import { render } from '@testing-library/react'
import { Seo } from '@/components/public/Seo'

test('setea document.title y meta description', () => {
  render(<Seo title="Planes — GymFlow" description="Conocé nuestros planes" />)
  expect(document.title).toBe('Planes — GymFlow')
  const meta = document.querySelector('meta[name="description"]')
  expect(meta?.getAttribute('content')).toBe('Conocé nuestros planes')
})
```

- [ ] **Step 2: Run → FAIL**

- [ ] **Step 3: Implementar** `Seo` (props: `title`, `description`, `image?`, `path?`). En `useEffect`: setear `document.title`, crear/actualizar `<meta name="description">` y OG (`og:title`, `og:description`, `og:image`, `og:url`, `og:type=website`). Devuelve `null`.

- [ ] **Step 4: Run → PASS**

- [ ] **Step 5: Commit** — `feat(public): componente Seo para meta tags por pagina`

---

## Task 5: Routing + `PublicLayout`

**Files:**
- Create: `frontend/src/components/public/PublicLayout.tsx`
- Modify: `frontend/src/App.tsx`
- Test: `frontend/src/components/public/__tests__/PublicLayout.test.tsx`

- [ ] **Step 1: Test de PublicLayout** (renderiza header con marca, un `<main>`, footer, y el `<Outlet/>` vía `MemoryRouter`).

```tsx
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import PublicLayout from '@/components/public/PublicLayout'

test('PublicLayout muestra header, footer y el contenido de la ruta', () => {
  render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route element={<PublicLayout />}>
          <Route path="/" element={<div>contenido home</div>} />
        </Route>
      </Routes>
    </MemoryRouter>
  )
  expect(screen.getByRole('banner')).toBeInTheDocument()      // header
  expect(screen.getByRole('contentinfo')).toBeInTheDocument() // footer
  expect(screen.getByText('contenido home')).toBeInTheDocument()
})
```

- [ ] **Step 2: Run → FAIL**

- [ ] **Step 3: Implementar PublicLayout** — `<div className="public-site">` con `<PublicHeader/>`, `<main><Outlet/></main>`, `<PublicFooter/>`. (Header/Footer pueden empezar como stubs mínimos y completarse en Task 6.)

- [ ] **Step 4: Modificar `App.tsx`** — agregar el bloque de rutas públicas (ver spec §4.1): `<Route element={<PublicLayout/>}>` con `/`, `/sedes`, `/planes`, `/clases`, `/contacto`; reemplazar `/` y `*` (hoy redirigen a `/login`) por Home y `NotFoundPage`. Mantener `/login`, `/admin`, `/portal` intactos.

- [ ] **Step 5: Run → PASS** (las páginas pueden ser stubs por ahora). Verificar también que el set de tests existentes sigue verde.

- [ ] **Step 6: Commit** — `feat(public): PublicLayout + rutas publicas (home no redirige a login)`

---

## Task 6: `PublicHeader` (nav + menú mobile) y `PublicFooter`

**Files:**
- Create/finish: `frontend/src/components/public/PublicHeader.tsx`, `PublicFooter.tsx`
- Test: `frontend/src/components/public/__tests__/PublicHeader.test.tsx`

- [ ] **Step 1: Test del header** — tiene los 5 links (Inicio, Sedes, Planes, Clases, Contacto) apuntando a sus rutas, y un botón "Acceder" que linkea a `/login`.

```tsx
test('header tiene navegacion y boton Acceder a /login', () => {
  render(<MemoryRouter><PublicHeader /></MemoryRouter>)
  expect(screen.getByRole('link', { name: /sedes/i })).toHaveAttribute('href', '/sedes')
  expect(screen.getByRole('link', { name: /acceder/i })).toHaveAttribute('href', '/login')
})
```

- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** header (nav con `<Link>` de react-router; en mobile, menú hamburguesa con estado `useState`; usar la marca/logo placeholder) y footer (marca, links rápidos, direcciones de sedes desde `site`, redes, copyright). **Diseñar con `ui-ux-pro-max`** (dark + celeste, sticky header, responsive).
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(public): header con nav responsive + footer`

---

## Task 7: `HomePage` (Inicio)

**Files:**
- Create: `frontend/src/pages/public/HomePage.tsx` + `Hero.tsx`, `Section.tsx`
- Test: `frontend/src/pages/public/__tests__/HomePage.test.tsx`

- [ ] **Step 1: Test** — renderiza el tagline (`site.tagline`), los 2 CTAs ("Ver planes" → `/planes`, "Conocé las sedes" → `/sedes`), y los nombres de las 2 sedes en el preview.
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** Home: `<Seo>` + `Hero` (foto placeholder + tagline + CTAs) + sección "por qué elegirnos" (3-4 features) + preview de sedes (link a `/sedes`) + preview de clases (link a `/clases`) + CTA final. Bindear todo desde `site`. **Diseñar con `ui-ux-pro-max`.**
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(public): home con hero, features y previews`

---

## Task 8: `SedesPage`

**Files:**
- Create: `frontend/src/pages/public/SedesPage.tsx` + `SedeCard.tsx`
- Test: `frontend/src/pages/public/__tests__/SedesPage.test.tsx`

- [ ] **Step 1: Test** — renderiza las 2 sedes (`Espacio Mora`, `Gimnasio Nuevo Malvín`) con dirección y al menos un horario; cada una con un link/iframe de mapa (`mapsUrl`).
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** `SedeCard` (foto, nombre, dirección, horarios, servicios, mapa) + página que mapea `site.sedes`. `<Seo>`. **Diseñar con `ui-ux-pro-max`.**
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(public): pagina de sedes con mapa y horarios`

---

## Task 9: `PlanesPublicPage`

**Files:**
- Create: `frontend/src/pages/public/PlanesPublicPage.tsx` + `PlanCard.tsx`
- Test: `frontend/src/pages/public/__tests__/PlanesPublicPage.test.tsx`

- [ ] **Step 1: Test** — renderiza todos los `site.planes` (nombre + precio + al menos un beneficio); el plan `destacado` tiene una marca visible (ej. badge "Recomendado").
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** `PlanCard` + grilla. `<Seo>`. **Diseñar con `ui-ux-pro-max`.**
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(public): pagina de planes con precios`

---

## Task 10: `ClasesPublicPage`

**Files:**
- Create: `frontend/src/pages/public/ClasesPublicPage.tsx` + `ClaseHorarioTabla.tsx`
- Test: `frontend/src/pages/public/__tests__/ClasesPublicPage.test.tsx`

- [ ] **Step 1: Test** — renderiza los tipos de clase (`site.clases`) y la grilla de horarios (`site.horarios`: días + al menos una clase con hora). Verificar que en estructura de tabla aparezcan los días.
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** cards de clases + `ClaseHorarioTabla` (tabla semanal; en mobile colapsa a lista por día). `<Seo>`. **Diseñar con `ui-ux-pro-max`.**
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(public): pagina de clases con grilla de horarios`

---

## Task 11: `ContactoPage` + `NotFoundPage`

**Files:**
- Create: `frontend/src/pages/public/ContactoPage.tsx`, `NotFoundPage.tsx`
- Test: `frontend/src/pages/public/__tests__/ContactoPage.test.tsx`, `NotFoundPage.test.tsx`

- [ ] **Step 1: Tests** — Contacto: link de WhatsApp (`https://wa.me/...`) y `mailto:` correctos + mapa. NotFound: muestra mensaje y link a `/`.
- [ ] **Step 2: Run → FAIL**
- [ ] **Step 3: Implementar** ambas. `<Seo>`. **Diseñar con `ui-ux-pro-max`.**
- [ ] **Step 4: Run → PASS**
- [ ] **Step 5: Commit** — `feat(public): pagina de contacto + 404 publico`

---

## Task 12: SEO estático (`index.html`, `sitemap.xml`, `robots.txt`)

**Files:**
- Modify: `frontend/index.html`
- Create: `frontend/public/sitemap.xml`, `frontend/public/robots.txt`

- [ ] **Step 1:** `index.html` → `lang="es"`, `<title>` por defecto ("GymFlow — Gimnasio en Montevideo"), `<meta name="description">` por defecto, favicon propio (placeholder), tags OG por defecto.
- [ ] **Step 2:** `sitemap.xml` con las 5 URLs públicas; `robots.txt` permitiendo todo + referencia al sitemap (`Sitemap: https://.../sitemap.xml`). Disallow de `/admin` y `/portal`.
- [ ] **Step 3:** Run `npm run build` → OK; verificar que `dist/` incluye `sitemap.xml` y `robots.txt`.
- [ ] **Step 4: Commit** — `feat(public): SEO estatico (lang, meta, sitemap, robots)`

---

## Task 13: Verificación final + handoff de assets

**Files:** —

- [ ] **Step 1:** Run `npm test` (todo el front) → PASS.
- [ ] **Step 2:** Run `npm run build` → OK.
- [ ] **Step 3:** Smoke manual (`npm run dev`): navegar las 5 rutas + 404 sin login; verificar que `/admin` y `/portal` siguen pidiendo login; responsive (mobile/desktop).
- [ ] **Step 4:** Confirmar dónde van los assets del usuario: `frontend/public/img/` (fotos + logo) y los valores reales en `src/content/site.ts`. Dejar un comentario `TODO` con la lista.
- [ ] **Step 5: Commit** (si quedó algo) y abrir PR `feature/rf19-sitio-publico` → `main` con descripción (qué incluye, placeholders pendientes, sin migración, sin backend).

---

## Notas de implementación
- **DRY/YAGNI:** todo el contenido sale de `site.ts`; nada hardcodeado en los componentes. Sin formulario de contacto, sin i18n, sin API.
- **TDD:** los tests cubren contenido, links y SEO (lo verificable); el diseño visual lo aporta `ui-ux-pro-max` y se valida a ojo.
- **No tocar** la auth ni `/admin` /`/portal`. Verificar que sus tests siguen verdes tras modificar `App.tsx`.
- **Acento celeste/azul:** placeholder hasta el logo; está aislado en un token (`--public-accent`) para cambiarlo en un solo lugar.
