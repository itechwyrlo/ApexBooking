# ApexBooking Landing Page & Nav Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename LocalFlow → ApexBooking repo-wide and rebuild the marketing landing page + nav into a mobile-first, bento-styled PWA landing experience per `docs/superpowers/specs/2026-08-07-apexbooking-landing-page-design.md`.

**Architecture:** No new component library. Existing `src/components/common/` primitives (`Button`, `Card`, `Badge`, `Icon`) are reused everywhere; three new shared primitives (`BrowserFrame`, `Reveal`, `useRevealOnScroll`) are added for this feature and consumed across sections. Landing-page-specific CSS lives in one new file (`src/styles/landing.css`), appended to task-by-task, kept separate from the shared `theme.css` design tokens.

**Tech Stack:** React 19, TypeScript, Vite, Bootstrap 5 (utility classes + custom CSS), `vite-plugin-pwa` + `@vite-pwa/assets-generator` (new devDependency, generates PWA icon PNGs from one source SVG).

## Global Constraints

- **No build/test/commit steps per task.** The user is verifying and committing manually. Do not run `tsc`, `vite build`, `oxlint`, or start the dev server as a "check my work" step at the end of a task, and do not run `git commit`. The only commands that should run are ones that *produce a required deliverable* (the PowerShell rename script in Task 1, `npm install` in Task 4, the PWA asset generator CLI in Task 4, the Playwright screenshot capture in Task 7) — those are the task's actual output, not verification, so they do run.
- Mobile-first: build the 375px layout, then add `md`/`lg` breakpoint overrides. Never author desktop-first and shrink.
- All interactive touch targets ≥ 44×44px.
- Respect `prefers-reduced-motion: reduce` everywhere an animation is added (strip `transform`, keep opacity fades only).
- Light theme only — no dark mode.
- `--color-primary` (`#4f46e5`, indigo) stays the shared anchor color across landing + authenticated app. Do not change existing `--color-*` tokens in `theme.css` — only add new ones.
- New hand-authored SVG icons match the existing style exactly: `viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"`.
- Only one new npm dependency is introduced in this whole plan: `@vite-pwa/assets-generator` (devDependency, Task 4).
- Scope is landing page + nav + repo-wide rename only. Do not touch authenticated-app pages/business logic/routing/auth beyond the literal text rename and the single Task 7 screenshot capture.

---

### Task 1: Repo-wide rename sweep (LocalFlow → ApexBooking)

**Files:**
- Modify (via script, not individual edits): `package.json`, `PROJECT_TRACKER.md`, `Claude/Technology_Stack.md`, `docs/superpowers/specs/2026-07-20-landing-page-design.md`, `docs/superpowers/specs/2026-07-20-login-request-access-design.md`, `docs/superpowers/plans/2026-07-20-landing-page.md`, `docs/superpowers/plans/2026-07-20-login-request-access.md`, `docs/superpowers/plans/2026-07-21-booking-dashboard-navigation.md`, `src/api/clients/authClient.ts`, `src/layouts/AuthLayout.tsx`, `src/layouts/SuperAdminLayout.tsx`, `src/layouts/SuperAdminAuthLayout.tsx`, `src/layouts/DashboardLayout.tsx`, `src/pages/LoginPage.tsx`, `src/pages/admin/TenantRequestManagementPage.tsx`, `src/pages/admin/SuperAdminLoginPage.tsx`, `src/components/admin/AdminSidebar.tsx`, `src/components/admin/TenantRequestTable.tsx`, `src/config/howItWorks.ts`, `src/components/layout/Footer.tsx`, `src/components/layout/ModuleSwitcher.tsx`

**Interfaces:**
- Produces: every occurrence of the literal strings `LocalFlow` and `localflow` in the listed files is replaced with `ApexBooking` / `apexbooking` respectively (case-sensitive, so `localflow.accessToken` → `apexbooking.accessToken`, `localflow.sidebar.collapsed` → `apexbooking.sidebar.collapsed`, `localflow.adminSidebar.collapsed` → `apexbooking.adminSidebar.collapsed`, `package.json`'s `"name": "localflow"` → `"name": "apexbooking"`).
- Note: `index.html` and `vite.config.ts` are deliberately **excluded** from this task — they're fully rewritten in Task 4 (PWA/manifest) to avoid two tasks editing the same lines. Landing components (`Header.tsx`, `BusinessSection.tsx`, `CallToActionSection.tsx`, `FeaturesSection.tsx`, landing `Footer.tsx`) are also excluded — they're rewritten from scratch in Tasks 6, 8, 9, 10, 13 and will contain "ApexBooking" directly rather than being patched twice.

- [ ] **Step 1: Run the rename script**

```powershell
$files = @(
  'package.json',
  'PROJECT_TRACKER.md',
  'Claude\Technology_Stack.md',
  'docs\superpowers\specs\2026-07-20-landing-page-design.md',
  'docs\superpowers\specs\2026-07-20-login-request-access-design.md',
  'docs\superpowers\plans\2026-07-20-landing-page.md',
  'docs\superpowers\plans\2026-07-20-login-request-access.md',
  'docs\superpowers\plans\2026-07-21-booking-dashboard-navigation.md',
  'src\api\clients\authClient.ts',
  'src\layouts\AuthLayout.tsx',
  'src\layouts\SuperAdminLayout.tsx',
  'src\layouts\SuperAdminAuthLayout.tsx',
  'src\layouts\DashboardLayout.tsx',
  'src\pages\LoginPage.tsx',
  'src\pages\admin\TenantRequestManagementPage.tsx',
  'src\pages\admin\SuperAdminLoginPage.tsx',
  'src\components\admin\AdminSidebar.tsx',
  'src\components\admin\TenantRequestTable.tsx',
  'src\config\howItWorks.ts',
  'src\components\layout\Footer.tsx',
  'src\components\layout\ModuleSwitcher.tsx'
)

foreach ($file in $files) {
  $content = Get-Content -Path $file -Raw
  $updated = $content -creplace 'LocalFlow', 'ApexBooking' -creplace 'localflow', 'apexbooking'
  Set-Content -Path $file -Value $updated -NoNewline -Encoding utf8
}
```

- [ ] **Step 2: Confirm no stray matches remain outside the excluded files**

```powershell
Get-ChildItem -Path . -Recurse -Include *.ts,*.tsx,*.html,*.json,*.md -Exclude node_modules,dist |
  Where-Object { $_.FullName -notmatch '\\node_modules\\' -and $_.FullName -notmatch '\\dist\\' } |
  Select-String -Pattern 'LocalFlow','localflow' -CaseSensitive
```

Expected output: only matches inside `index.html`, `vite.config.ts`, and the landing components listed above as excluded (those are handled in later tasks). If anything else shows up, it means a file was missed — add it to the `$files` list and rerun Step 1 for that file only.

---

### Task 2: Brand identity — teal token + display type

**Files:**
- Modify: `src/styles/theme.css:1-32` (tokens block), `index.html:9-14` (font link)

**Interfaces:**
- Produces: CSS custom properties `--color-teal`, `--color-teal-strong`, `--color-teal-soft`; utility class `.font-display`. Both consumed by every landing component from Task 6 onward.

- [ ] **Step 1: Add teal tokens to `theme.css`**

In `src/styles/theme.css`, inside the existing `:root { ... }` block, add these three lines directly after the existing `--color-danger-soft` line (do not touch any other existing token):

```css
  --color-teal: #0d9488;
  --color-teal-strong: #0f766e;
  --color-teal-soft: rgba(13, 148, 136, 0.1);
```

- [ ] **Step 2: Add the `.font-display` utility**

Append this block to the end of `src/styles/theme.css`:

```css

/* ---------------------------------------------------------------------- */
/* Landing page display type                                             */
/* ---------------------------------------------------------------------- */

.font-display {
  font-family: 'Manrope', 'Inter', sans-serif;
}
```

- [ ] **Step 3: Load Manrope alongside the existing fonts**

In `index.html`, the existing font link is:

```html
    <link
      href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,500;9..144,600&family=IBM+Plex+Mono:wght@400;500;600&display=swap"
      rel="stylesheet"
    />
```

Replace it with (adds Manrope 700/800 without removing the existing Fraunces/IBM Plex Mono families, since `PaymentGatewayCard.tsx` and `publicBooking.css` still use them):

```html
    <link
      href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,500;9..144,600&family=IBM+Plex+Mono:wght@400;500;600&family=Manrope:wght@700;800&display=swap"
      rel="stylesheet"
    />
```

---

### Task 3: New icon and logo assets

**Files:**
- Create: `public/assets/icons/double-booking.svg`, `public/assets/icons/no-shows.svg`, `public/assets/icons/messaging.svg`, `public/assets/icons/no-visibility.svg`, `public/assets/icons/salon.svg`, `public/assets/icons/barbershop.svg`, `public/assets/icons/clinic.svg`, `public/assets/icons/fitness.svg`
- Modify: `public/favicon.svg` (replace Vite scaffold logo with the ApexBooking mark)

**Interfaces:**
- Produces: 8 new icon files at `/assets/icons/<name>.svg` (consumed by Task 8's `problems.ts` and Task 9's `industries.ts`), plus a new `/favicon.svg` brand mark (consumed by Task 4's PWA asset generator, Task 6's `Header.tsx`, and Task 13's landing `Footer.tsx`).

- [ ] **Step 1: Create the four "problem" icons**

`public/assets/icons/double-booking.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <rect x="3" y="8" width="12" height="12" rx="2"/>
  <rect x="9" y="4" width="12" height="12" rx="2"/>
</svg>
```

`public/assets/icons/no-shows.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <circle cx="11" cy="12" r="8"/>
  <line x1="11" y1="7" x2="11" y2="12"/>
  <line x1="11" y1="12" x2="14" y2="14"/>
  <line x1="16" y1="16" x2="21" y2="21"/>
  <line x1="21" y1="16" x2="16" y2="21"/>
</svg>
```

`public/assets/icons/messaging.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <path d="M2 4h12v8H8l-3.5 3.5V12H2Z"/>
  <path d="M10 9h12v8h-3.5v3.5L15 17h-5Z"/>
</svg>
```

`public/assets/icons/no-visibility.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <line x1="4" y1="20" x2="20" y2="20"/>
  <line x1="7" y1="20" x2="7" y2="13"/>
  <line x1="12" y1="20" x2="12" y2="9"/>
  <line x1="17" y1="20" x2="17" y2="15"/>
  <line x1="3" y1="3" x2="21" y2="21"/>
</svg>
```

- [ ] **Step 2: Create the four vertical icons**

`public/assets/icons/salon.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <circle cx="6" cy="6" r="2.5"/>
  <circle cx="6" cy="18" r="2.5"/>
  <line x1="8.5" y1="7.5" x2="20" y2="19"/>
  <line x1="8.5" y1="16.5" x2="20" y2="5"/>
</svg>
```

`public/assets/icons/barbershop.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <rect x="9" y="2" width="6" height="20" rx="3"/>
  <path d="M9 6l6 4M9 10l6 4M9 14l6 4"/>
</svg>
```

`public/assets/icons/clinic.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <circle cx="12" cy="12" r="9"/>
  <line x1="12" y1="8" x2="12" y2="16"/>
  <line x1="8" y1="12" x2="16" y2="12"/>
</svg>
```

`public/assets/icons/fitness.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <rect x="2" y="9" width="3" height="6" rx="1"/>
  <rect x="19" y="9" width="3" height="6" rx="1"/>
  <line x1="5" y1="12" x2="19" y2="12"/>
  <rect x="6.5" y="7" width="2.5" height="10" rx="1"/>
  <rect x="15" y="7" width="2.5" height="10" rx="1"/>
</svg>
```

- [ ] **Step 3: Replace the favicon with the new ApexBooking mark**

Overwrite `public/favicon.svg` (currently the default Vite scaffold gradient blob) with:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32">
  <rect width="32" height="32" rx="8" fill="#4f46e5"/>
  <path d="M9 17.5l4.5 4.5L23 11.5" stroke="#2dd4bf" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
</svg>
```

This is a rounded-square indigo badge with a teal checkmark (indigo primary + teal accent — the same duo-tone pairing as the rest of the brand). It's the source image Task 4's PWA asset generator reads from.

---

### Task 4: PWA icon generation + manifest

**Files:**
- Create: `pwa-assets.config.ts`
- Modify: `package.json` (devDependency + script), `vite.config.ts`, `index.html:5-15`

**Interfaces:**
- Consumes: `public/favicon.svg` from Task 3.
- Produces: `public/favicon.ico`, `public/pwa-64x64.png`, `public/pwa-192x192.png`, `public/pwa-512x512.png`, `public/maskable-icon-512x512.png`, `public/apple-touch-icon-180x180.png` (generated files, not hand-written).

- [ ] **Step 1: Install the generator**

```bash
npm install -D @vite-pwa/assets-generator
```

- [ ] **Step 2: Add the generator config**

Create `pwa-assets.config.ts` at the repo root:

```ts
import { defineConfig, minimal2023Preset } from '@vite-pwa/assets-generator/config'

export default defineConfig({
  headLinkOptions: {
    preset: '2023',
  },
  preset: minimal2023Preset,
  images: ['public/favicon.svg'],
})
```

- [ ] **Step 3: Add a generator script and run it**

In `package.json`, add to `"scripts"`:

```json
    "generate-pwa-assets": "pwa-assets-generator"
```

Run it once to produce the files into `public/`:

```bash
npm run generate-pwa-assets
```

- [ ] **Step 4: Update the PWA manifest in `vite.config.ts`**

Replace the entire `manifest: { ... }` block in `vite.config.ts` with:

```ts
      manifest: {
        name: 'ApexBooking',
        short_name: 'ApexBooking',
        description: 'Online booking and appointment scheduling for local businesses.',
        theme_color: '#4f46e5',
        background_color: '#ffffff',
        display: 'standalone',
        icons: [
          {
            src: 'pwa-64x64.png',
            sizes: '64x64',
            type: 'image/png',
          },
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png',
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any',
          },
          {
            src: 'maskable-icon-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable',
          },
        ],
      },
```

- [ ] **Step 5: Fix `index.html`'s head — favicon links, title, meta description, theme-color**

Replace:

```html
    <link rel="icon" type="image/svg+xml" href="/src/assets/favicon.png" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="description" content="LocalFlow is online booking and scheduling software for local businesses — manage appointments, staff schedules, and customer bookings in one place." />
    <meta name="theme-color" content="#0d6efd" />
```

with:

```html
    <link rel="icon" href="/favicon.ico" sizes="48x48" />
    <link rel="icon" href="/favicon.svg" sizes="any" type="image/svg+xml" />
    <link rel="apple-touch-icon" href="/apple-touch-icon-180x180.png" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="description" content="ApexBooking is online booking and scheduling software for local businesses — manage appointments, staff schedules, and customer bookings in one place." />
    <meta name="theme-color" content="#4f46e5" />
```

And replace:

```html
    <title>LocalFlow</title>
```

with:

```html
    <title>ApexBooking</title>
```

(This is independent of Task 2's font-link edit — different lines in the same file — so order between Task 2 and this task doesn't matter.)

---

### Task 5: Shared primitives — `BrowserFrame`, `Reveal`, `useRevealOnScroll`

**Files:**
- Create: `src/hooks/useRevealOnScroll.ts`, `src/components/common/Reveal.tsx`, `src/components/common/BrowserFrame.tsx`, `src/styles/landing.css`
- Modify: `src/main.tsx:8` (add CSS import)

**Interfaces:**
- Produces:
  - `useRevealOnScroll<T extends HTMLElement>(): { ref: RefObject<T>; isVisible: boolean }`
  - `<Reveal className?: string; delayStep?: number; children: ReactNode />` — always renders a `<div>` with `.reveal` / `.reveal--visible` classes (no `as` prop — every call site in this plan wraps block-level content, so a fixed `div` avoids unsound generic-element typing for no actual benefit).
  - `<BrowserFrame url?: string; className?: string; children: ReactNode />`
- Consumed by: every task from Task 6 onward.

- [ ] **Step 1: Write the scroll-reveal hook**

`src/hooks/useRevealOnScroll.ts`:

```ts
import { useEffect, useRef, useState } from 'react'

export function useRevealOnScroll<T extends HTMLElement>() {
  const ref = useRef<T>(null)
  const [isVisible, setIsVisible] = useState(false)

  useEffect(() => {
    const node = ref.current
    if (!node) {
      return
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsVisible(true)
          observer.unobserve(node)
        }
      },
      { threshold: 0.15 },
    )

    observer.observe(node)

    return () => observer.disconnect()
  }, [])

  return { ref, isVisible }
}
```

- [ ] **Step 2: Write the `Reveal` wrapper component**

`src/components/common/Reveal.tsx`:

```tsx
import type { ReactNode } from 'react'
import { useRevealOnScroll } from '../../hooks/useRevealOnScroll'

interface IRevealProps {
  children: ReactNode
  className?: string
  delayStep?: number
}

export function Reveal({ children, className = '', delayStep = 0 }: IRevealProps) {
  const { ref, isVisible } = useRevealOnScroll<HTMLDivElement>()

  const style = delayStep ? { transitionDelay: `${Math.min(delayStep, 6) * 90}ms` } : undefined

  return (
    <div ref={ref} className={`reveal ${isVisible ? 'reveal--visible' : ''} ${className}`.trim()} style={style}>
      {children}
    </div>
  )
}
```

- [ ] **Step 3: Write the `BrowserFrame` component**

`src/components/common/BrowserFrame.tsx`:

```tsx
import type { ReactNode } from 'react'

interface IBrowserFrameProps {
  url?: string
  children: ReactNode
  className?: string
}

export function BrowserFrame({ url = 'app.apexbooking.com', children, className = '' }: IBrowserFrameProps) {
  return (
    <div className={`browser-frame ${className}`.trim()}>
      <div className="browser-frame__bar">
        <span className="browser-frame__dot" />
        <span className="browser-frame__dot" />
        <span className="browser-frame__dot" />
        <span className="browser-frame__url">{url}</span>
      </div>
      <div className="browser-frame__body">{children}</div>
    </div>
  )
}
```

- [ ] **Step 4: Create `landing.css` with the reveal + browser-frame styles**

Create `src/styles/landing.css`:

```css
/* ---------------------------------------------------------------------- */
/* Scroll reveal                                                          */
/* ---------------------------------------------------------------------- */

.reveal {
  opacity: 0;
  transform: translateY(24px);
  transition: opacity 0.4s ease-out, transform 0.4s ease-out;
}

.reveal--visible {
  opacity: 1;
  transform: translateY(0);
}

@media (prefers-reduced-motion: reduce) {
  .reveal {
    transform: none;
    transition: opacity 0.4s ease-out;
  }
}

/* ---------------------------------------------------------------------- */
/* Browser frame                                                          */
/* ---------------------------------------------------------------------- */

.browser-frame {
  border-radius: var(--radius-lg);
  overflow: hidden;
  background: var(--color-surface);
  box-shadow: var(--shadow-md);
  border: 1px solid var(--color-border);
}

.browser-frame__bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 0.875rem;
  background: var(--color-canvas);
  border-bottom: 1px solid var(--color-border);
}

.browser-frame__dot {
  width: 9px;
  height: 9px;
  border-radius: 50%;
  background: var(--color-border);
}

.browser-frame__url {
  margin-left: 0.5rem;
  font-size: 0.75rem;
  color: var(--color-muted);
  background: var(--color-surface);
  border-radius: 999px;
  padding: 0.15rem 0.75rem;
  flex: 1;
  max-width: 220px;
}

.browser-frame__body img {
  display: block;
  width: 100%;
  height: auto;
}
```

- [ ] **Step 5: Import `landing.css` globally**

In `src/main.tsx`, the existing import block is:

```tsx
import './index.css'
import './styles/theme.css'
import './styles/publicBooking.css'
```

Add the new stylesheet after `theme.css`:

```tsx
import './index.css'
import './styles/theme.css'
import './styles/landing.css'
import './styles/publicBooking.css'
```

---

### Task 6: Navigation bar rebuild

**Files:**
- Modify: `src/components/landing/Header.tsx` (full rewrite)
- Modify: `src/styles/landing.css` (append)

**Interfaces:**
- Consumes: `NAV_ITEMS` from `src/config/navigation.ts` (existing, unchanged), `Button` from `src/components/common/Button.tsx` (existing), `InstallAppButton` from `src/components/pwa/InstallAppButton.tsx` (existing), `scrollToPricing` from `src/utils/scrollToPricing.ts` (existing).
- Produces: `<Header />` — no props. Renders a fixed header plus a full-screen mobile menu as siblings.

- [ ] **Step 1: Rewrite `Header.tsx`**

Replace the full contents of `src/components/landing/Header.tsx`:

```tsx
import { useEffect, useRef, useState } from 'react'
import { NAV_ITEMS } from '../../config/navigation'
import { InstallAppButton } from '../pwa/InstallAppButton'
import { Button } from '../common/Button'
import { scrollToPricing } from '../../utils/scrollToPricing'

export function Header() {
  const [isScrolled, setIsScrolled] = useState(false)
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const node = sentinelRef.current
    if (!node) {
      return
    }

    const observer = new IntersectionObserver(([entry]) => {
      setIsScrolled(!entry.isIntersecting)
    })

    observer.observe(node)

    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    if (!isMenuOpen) {
      return
    }

    const previouslyFocused = document.activeElement as HTMLElement | null
    menuRef.current?.querySelector<HTMLElement>('button, a')?.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setIsMenuOpen(false)
      }
    }

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.body.style.overflow = previousOverflow
      document.removeEventListener('keydown', handleKeyDown)
      previouslyFocused?.focus()
    }
  }, [isMenuOpen])

  function closeMenu() {
    setIsMenuOpen(false)
  }

  return (
    <>
      <div ref={sentinelRef} className="scroll-sentinel" aria-hidden="true" />

      <header className={`site-header ${isScrolled ? 'site-header--scrolled' : ''}`}>
        <div className="container d-flex align-items-center justify-content-between py-3">
          <a className="navbar-brand d-flex align-items-center gap-2" href="#home">
            <img src="/favicon.svg" alt="ApexBooking logo" width={32} height={32} />
            <span className="fw-bold fs-5 font-display">ApexBooking</span>
          </a>

          <nav className="d-none d-md-flex align-items-center gap-4">
            {NAV_ITEMS.map((item) => (
              <a key={item.href} className="nav-link fw-medium" href={item.href}>
                {item.label}
              </a>
            ))}
          </nav>

          <div className="d-none d-md-flex align-items-center gap-2">
            <InstallAppButton />
            <Button to="/login" variant="outline-primary">
              Login
            </Button>
            <Button to="/#pricing" onClick={scrollToPricing}>
              Request Access
            </Button>
          </div>

          <button
            type="button"
            className="mobile-nav-toggle d-md-none"
            aria-expanded={isMenuOpen}
            aria-controls="mobile-nav-menu"
            aria-label={isMenuOpen ? 'Close menu' : 'Open menu'}
            onClick={() => setIsMenuOpen((open) => !open)}
          >
            <span className={`mobile-nav-toggle__bar ${isMenuOpen ? 'mobile-nav-toggle__bar--open' : ''}`} />
          </button>
        </div>
      </header>

      <div
        id="mobile-nav-menu"
        ref={menuRef}
        className={`mobile-nav-menu ${isMenuOpen ? 'mobile-nav-menu--open' : ''}`}
        role="dialog"
        aria-modal="true"
        aria-label="Site menu"
      >
        <div className="mobile-nav-menu__bar">
          <span className="fw-bold fs-5 font-display text-white">ApexBooking</span>
          <button type="button" className="mobile-nav-menu__close" aria-label="Close menu" onClick={closeMenu}>
            <span aria-hidden="true">&times;</span>
          </button>
        </div>

        <ul className="list-unstyled d-flex flex-column gap-4 mb-0 mobile-nav-menu__links">
          {NAV_ITEMS.map((item) => (
            <li key={item.href}>
              <a href={item.href} className="mobile-nav-menu__link font-display" onClick={closeMenu}>
                {item.label}
              </a>
            </li>
          ))}
          <li>
            <a
              href="/login"
              className="mobile-nav-menu__link mobile-nav-menu__link--muted font-display"
              onClick={closeMenu}
            >
              Login
            </a>
          </li>
        </ul>

        <Button
          to="/#pricing"
          size="lg"
          className="mobile-nav-menu__cta w-100"
          onClick={(event) => {
            closeMenu()
            scrollToPricing(event)
          }}
        >
          Get Started
        </Button>
      </div>
    </>
  )
}
```

- [ ] **Step 2: Append nav styles to `landing.css`**

```css

/* ---------------------------------------------------------------------- */
/* Site header                                                            */
/* ---------------------------------------------------------------------- */

.scroll-sentinel {
  position: absolute;
  top: 0;
  left: 0;
  width: 1px;
  height: 40px;
  visibility: hidden;
  pointer-events: none;
}

.site-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1030;
  background-color: transparent;
  border-bottom: 1px solid transparent;
  transition: background-color 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;
}

.site-header--scrolled {
  background-color: var(--color-surface);
  border-bottom-color: var(--color-border);
  box-shadow: var(--shadow-sm);
}

main.pt-nav {
  padding-top: 80px;
}

/* ---------------------------------------------------------------------- */
/* Mobile nav toggle + full-screen menu                                   */
/* ---------------------------------------------------------------------- */

.mobile-nav-toggle {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 0;
  background: transparent;
  padding: 0;
}

.mobile-nav-toggle__bar,
.mobile-nav-toggle__bar::before,
.mobile-nav-toggle__bar::after {
  content: '';
  display: block;
  width: 22px;
  height: 2px;
  background: var(--color-ink);
  border-radius: 2px;
  transition: transform 0.2s ease, opacity 0.2s ease, background-color 0.2s ease;
}

.mobile-nav-toggle__bar {
  position: relative;
  background: var(--color-ink);
}

.mobile-nav-toggle__bar::before {
  position: absolute;
  top: -7px;
  left: 0;
}

.mobile-nav-toggle__bar::after {
  position: absolute;
  top: 7px;
  left: 0;
}

.mobile-nav-toggle__bar--open {
  background: transparent;
}

.mobile-nav-toggle__bar--open::before {
  top: 0;
  transform: rotate(45deg);
}

.mobile-nav-toggle__bar--open::after {
  top: 0;
  transform: rotate(-45deg);
}

.mobile-nav-menu {
  position: fixed;
  inset: 0;
  z-index: 1040;
  display: flex;
  flex-direction: column;
  background: var(--color-ink);
  padding: 1rem 1.5rem 2rem;
  transform: translateY(-16px);
  opacity: 0;
  visibility: hidden;
  pointer-events: none;
  transition: transform 0.22s ease-out, opacity 0.22s ease-out, visibility 0.22s;
}

.mobile-nav-menu--open {
  transform: translateY(0);
  opacity: 1;
  visibility: visible;
  pointer-events: auto;
}

.mobile-nav-menu__bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.5rem 0 1rem;
}

.mobile-nav-menu__close {
  width: 44px;
  height: 44px;
  border: 0;
  background: transparent;
  color: #fff;
  font-size: 1.75rem;
  line-height: 1;
}

.mobile-nav-menu__links {
  padding-top: 1.5rem;
}

.mobile-nav-menu__link {
  display: flex;
  align-items: center;
  min-height: 44px;
  color: #fff;
  font-size: 1.5rem;
  font-weight: 700;
  text-decoration: none;
}

.mobile-nav-menu__link--muted {
  color: rgba(255, 255, 255, 0.6);
}

.mobile-nav-menu__cta {
  margin-top: auto;
}

@media (prefers-reduced-motion: reduce) {
  .mobile-nav-menu {
    transition: opacity 0.22s ease-out, visibility 0.22s;
    transform: none;
  }
}
```

---

### Task 7: Hero section rebuild + screenshot capture

**Files:**
- Modify: `src/components/landing/HeroSection.tsx` (full rewrite)
- Create: `src/assets/hero-dashboard.png` (captured, not hand-written)
- Modify: `src/styles/landing.css` (append)

**Interfaces:**
- Consumes: `BrowserFrame` and `Reveal` from Task 5, `Button` and `scrollToPricing` (existing).
- Produces: `<HeroSection />` — no props.

- [ ] **Step 1: Capture the dashboard screenshot**

With the dev server running (`npm run dev`, default `http://localhost:5173`) — `/app/booking` is reachable without logging in per `ProtectedRoute`'s current dev-preview safeguard:

```bash
npx playwright install chromium
npx playwright screenshot http://localhost:5173/app/booking src/assets/hero-dashboard.png --viewport-size=1280,900
```

This produces a real PNG of the Booking dashboard overview — the hero's "real product screenshot," not a fabricated mockup. `src/assets/` currently has no files, so this also becomes the first real asset in that folder.

- [ ] **Step 2: Rewrite `HeroSection.tsx`**

Replace the full contents of `src/components/landing/HeroSection.tsx`:

```tsx
import { Button } from '../common/Button'
import { BrowserFrame } from '../common/BrowserFrame'
import { Reveal } from '../common/Reveal'
import { scrollToPricing } from '../../utils/scrollToPricing'
import heroScreenshot from '../../assets/hero-dashboard.png'

export function HeroSection() {
  return (
    <section id="home" className="hero-section py-5 py-lg-6 border-bottom">
      <div className="container">
        <div className="row align-items-center gy-5">
          <Reveal className="col-lg-6">
            <p className="text-eyebrow mb-3">Online booking for local businesses</p>
            <h1 className="display-5 fw-bold font-display mb-3">
              Booking software that stops the back-and-forth.
            </h1>
            <p className="lead text-secondary mb-4">
              Online booking, staff schedules, and today&apos;s appointments in one place — so customers book
              themselves in, and you stop chasing confirmations.
            </p>
            <div className="d-grid d-sm-flex gap-3">
              <Button to="/#pricing" size="lg" onClick={scrollToPricing}>
                Request Access
              </Button>
              <a href="#features" className="btn btn-outline-primary btn-lg">
                Explore Features
              </a>
            </div>
            <p className="hero-trust-line mt-3 mb-0">No setup fees. Live in minutes.</p>
          </Reveal>

          <Reveal className="col-lg-6" delayStep={1}>
            <div className="hero-frame-wrap">
              <BrowserFrame url="app.apexbooking.com/booking">
                <img
                  src={heroScreenshot}
                  alt="ApexBooking dashboard showing today's schedule"
                  className="w-100 d-block"
                />
              </BrowserFrame>
            </div>
          </Reveal>
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 3: Append hero styles to `landing.css`**

```css

/* ---------------------------------------------------------------------- */
/* Hero                                                                   */
/* ---------------------------------------------------------------------- */

.hero-trust-line {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-muted);
}

.hero-frame-wrap {
  animation: hero-float 3.5s ease-in-out infinite;
}

@keyframes hero-float {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-10px);
  }
}

@media (max-width: 991.98px) {
  @keyframes hero-float {
    0%,
    100% {
      transform: translateY(0);
    }
    50% {
      transform: translateY(-4px);
    }
  }
}

@media (prefers-reduced-motion: reduce) {
  .hero-frame-wrap {
    animation: none;
  }
}
```

---

### Task 8: Problem section (new)

**Files:**
- Create: `src/interfaces/IProblem.ts`, `src/config/problems.ts`, `src/components/landing/ProblemSection.tsx`

**Interfaces:**
- Consumes: icons from Task 3 (`double-booking`, `no-shows`, `messaging`, `no-visibility`), `Card` and `Reveal` (existing/Task 5).
- Produces: `PROBLEMS: IProblem[]` (consumed nowhere else — this section owns it), `<ProblemSection />` — consumed by Task 13's `LandingPage.tsx`.

- [ ] **Step 1: Add the `IProblem` interface**

`src/interfaces/IProblem.ts`:

```ts
export interface IProblem {
  id: string
  icon: string
  title: string
  description: string
}
```

- [ ] **Step 2: Add the problems config**

`src/config/problems.ts`:

```ts
import type { IProblem } from '../interfaces/IProblem'

export const PROBLEMS: IProblem[] = [
  {
    id: 'double-bookings',
    icon: 'double-booking',
    title: 'Double bookings',
    description: "Manual calendars and spreadsheets can't catch scheduling conflicts before they happen.",
  },
  {
    id: 'no-shows',
    icon: 'no-shows',
    title: 'No-shows',
    description: 'Without automatic reminders, missed appointments quietly eat into revenue every week.',
  },
  {
    id: 'back-and-forth-messaging',
    icon: 'messaging',
    title: 'Endless back-and-forth',
    description: 'Confirming a time over text or phone wastes time for you and your customers.',
  },
  {
    id: 'no-visibility',
    icon: 'no-visibility',
    title: 'No visibility',
    description: "Without a dashboard, you don't know your busiest hours or your best-performing staff.",
  },
]
```

- [ ] **Step 3: Build the section**

`src/components/landing/ProblemSection.tsx`:

```tsx
import { PROBLEMS } from '../../config/problems'
import { Card } from '../common/Card'
import { Icon } from '../common/Icon'
import { Reveal } from '../common/Reveal'

export function ProblemSection() {
  return (
    <section className="py-5 py-lg-6 border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">Sound familiar?</p>
          <h2 className="fw-bold font-display">What booking software should fix</h2>
        </Reveal>
        <div className="row row-cols-2 row-cols-lg-4 g-4">
          {PROBLEMS.map((problem, index) => (
            <div className="col" key={problem.id}>
              <Reveal delayStep={index}>
                <Card className="h-100 text-center">
                  <Icon name={problem.icon} size={32} className="mb-3" />
                  <p className="fw-semibold mb-1">{problem.title}</p>
                  <p className="text-secondary small mb-0">{problem.description}</p>
                </Card>
              </Reveal>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
```

---

### Task 9: Business verticals section — Live / Coming Soon

**Files:**
- Modify: `src/interfaces/IIndustry.ts`, `src/config/industries.ts` (full rewrite), `src/components/landing/BusinessSection.tsx` (full rewrite)

**Interfaces:**
- Consumes: `salon`/`barbershop`/`clinic`/`fitness` icons from Task 3, `Badge`, `Card`, `Reveal`.
- Produces: `IIndustry` gains `status: 'live' | 'coming-soon'`. `INDUSTRIES` now has exactly 4 entries (was 8) — this array is only consumed by `BusinessSection.tsx`, confirmed via repo search, so no other file needs updating.

- [ ] **Step 1: Add `status` to `IIndustry`**

`src/interfaces/IIndustry.ts`:

```ts
export interface IIndustry {
  id: string
  name: string
  icon: string
  status: 'live' | 'coming-soon'
}
```

- [ ] **Step 2: Rewrite `industries.ts`**

Replace the full contents of `src/config/industries.ts` (this also fixes the currently-broken imports from an empty `src/assets/` folder by switching to public-path string references, the same convention `features.ts` already uses):

```ts
import type { IIndustry } from '../interfaces/IIndustry'

export const INDUSTRIES: IIndustry[] = [
  { id: 'salon', name: 'Salon', icon: '/assets/icons/salon.svg', status: 'live' },
  { id: 'barbershop', name: 'Barbershop', icon: '/assets/icons/barbershop.svg', status: 'live' },
  { id: 'clinic', name: 'Clinic', icon: '/assets/icons/clinic.svg', status: 'coming-soon' },
  { id: 'fitness', name: 'Fitness', icon: '/assets/icons/fitness.svg', status: 'coming-soon' },
]
```

- [ ] **Step 3: Rewrite `BusinessSection.tsx`**

Replace the full contents of `src/components/landing/BusinessSection.tsx`:

```tsx
import { INDUSTRIES } from '../../config/industries'
import { Card } from '../common/Card'
import { Badge } from '../common/Badge'
import { Reveal } from '../common/Reveal'

export function BusinessSection() {
  return (
    <section id="businesses" className="py-5 py-lg-6 bg-light border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">Built for appointment-based businesses</p>
          <h2 className="fw-bold font-display">If your business runs on bookings, ApexBooking fits</h2>
          <p className="text-secondary mb-0">From first-time customers to regulars, every visit starts with a booking.</p>
        </Reveal>
        <div className="row g-4">
          {INDUSTRIES.map((industry, index) => (
            <div className="col-6 col-lg-3" key={industry.id}>
              <Reveal delayStep={index}>
                <Card hover className="h-100 text-center position-relative">
                  <Badge
                    tone={industry.status === 'live' ? 'success' : 'neutral'}
                    className="position-absolute top-0 end-0 mt-2 me-2"
                  >
                    {industry.status === 'live' ? 'Live' : 'Coming Soon'}
                  </Badge>
                  <img src={industry.icon} alt="" width={40} height={40} className="mb-3" />
                  <p className="fw-medium mb-0">{industry.name}</p>
                </Card>
              </Reveal>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
```

---

### Task 10: Feature grid → bento layout

**Files:**
- Modify: `src/interfaces/IFeature.ts`, `src/config/features.ts`, `src/components/landing/FeaturesSection.tsx` (full rewrite)
- Create: `src/components/landing/FeatureIllustration.tsx`
- Modify: `src/styles/landing.css` (append)

**Interfaces:**
- Produces: `IFeature` gains `size?: 'large'`. `<FeatureIllustration variant: 'online-booking' | 'booking-calendar' | 'dashboard-reports' />`.

- [ ] **Step 1: Add `size` to `IFeature`**

`src/interfaces/IFeature.ts`:

```ts
export interface IFeature {
  id: string
  title: string
  description: string
  icon: string
  comingSoon?: boolean
  size?: 'large'
}
```

- [ ] **Step 2: Flag the three large cards in `features.ts`**

In `src/config/features.ts`, add `size: 'large',` to the `online-booking`, `booking-calendar`, and `dashboard-reports` entries only:

```ts
  {
    id: 'online-booking',
    title: 'Online Booking',
    description: 'Customers can book appointments online, any time, without calling in.',
    icon: '/assets/icons/globe.svg',
    size: 'large',
  },
```

```ts
  {
    id: 'booking-calendar',
    title: 'Booking Calendar',
    description: 'A shared calendar showing every appointment across your team.',
    icon: '/assets/icons/calendar.svg',
    size: 'large',
  },
```

```ts
  {
    id: 'dashboard-reports',
    title: 'Dashboard Reports',
    description: 'See booking volume, team activity, and business insights at a glance.',
    icon: '/assets/icons/chart.svg',
    size: 'large',
  },
```

(Every other entry in the array is unchanged.)

- [ ] **Step 3: Build the mini-illustration component**

`src/components/landing/FeatureIllustration.tsx`:

```tsx
interface IFeatureIllustrationProps {
  variant: 'online-booking' | 'booking-calendar' | 'dashboard-reports'
}

const CALENDAR_DAY_LABELS = ['S', 'M', 'T', 'W', 'T', 'F', 'S']
const CALENDAR_BOOKED_INDEXES = new Set([3, 9, 14, 17])
const CHART_BAR_HEIGHTS = [40, 65, 50, 80, 60, 90, 45]

export function FeatureIllustration({ variant }: IFeatureIllustrationProps) {
  if (variant === 'online-booking') {
    return (
      <div className="feature-illustration">
        <div className="feature-illustration__row">
          <span className="feature-illustration__chip">Haircut &amp; Style</span>
          <span className="feature-illustration__chip feature-illustration__chip--muted">45 min</span>
        </div>
        <div className="feature-illustration__slots">
          {['9:00', '10:30', '1:00', '2:30'].map((slot, index) => (
            <span
              key={slot}
              className={`feature-illustration__slot ${index === 1 ? 'feature-illustration__slot--active' : ''}`}
            >
              {slot}
            </span>
          ))}
        </div>
      </div>
    )
  }

  if (variant === 'booking-calendar') {
    return (
      <div className="feature-illustration feature-illustration--calendar">
        {CALENDAR_DAY_LABELS.map((label, index) => (
          <span key={`label-${index}`} className="feature-illustration__day-label">
            {label}
          </span>
        ))}
        {Array.from({ length: 21 }).map((_, index) => (
          <span
            key={`day-${index}`}
            className={`feature-illustration__day ${CALENDAR_BOOKED_INDEXES.has(index) ? 'feature-illustration__day--booked' : ''}`}
          />
        ))}
      </div>
    )
  }

  return (
    <div className="feature-illustration feature-illustration--chart">
      {CHART_BAR_HEIGHTS.map((height, index) => (
        <span key={index} className="feature-illustration__bar" style={{ height: `${height}%` }} />
      ))}
    </div>
  )
}
```

- [ ] **Step 4: Rewrite `FeaturesSection.tsx`**

Replace the full contents of `src/components/landing/FeaturesSection.tsx`:

```tsx
import { BOOKING_FEATURES } from '../../config/features'
import { Card } from '../common/Card'
import { Badge } from '../common/Badge'
import { Reveal } from '../common/Reveal'
import { FeatureIllustration } from './FeatureIllustration'

const ILLUSTRATED_IDS = new Set(['online-booking', 'booking-calendar', 'dashboard-reports'])

export function FeaturesSection() {
  return (
    <section id="features" className="py-5 py-lg-6 border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">Everything runs through booking</p>
          <h2 className="fw-bold font-display">Everything you need to manage bookings</h2>
          <p className="text-secondary mb-0">The Booking module is the first available product on ApexBooking.</p>
        </Reveal>
        <div className="feature-grid">
          {BOOKING_FEATURES.map((feature, index) => (
            <Reveal
              key={feature.id}
              delayStep={index}
              className={feature.size === 'large' ? 'feature-grid__item feature-grid__item--large' : 'feature-grid__item'}
            >
              <Card hover className="h-100">
                <div className="d-flex align-items-center gap-2 mb-2">
                  <img src={feature.icon} alt="" width={24} height={24} />
                  <h3 className="h5 mb-0">{feature.title}</h3>
                  {feature.comingSoon && (
                    <Badge tone="neutral" className="ms-auto">
                      Coming Soon
                    </Badge>
                  )}
                </div>
                <p className="text-secondary mb-0">{feature.description}</p>
                {ILLUSTRATED_IDS.has(feature.id) && (
                  <FeatureIllustration variant={feature.id as 'online-booking' | 'booking-calendar' | 'dashboard-reports'} />
                )}
              </Card>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 5: Append bento grid + illustration styles to `landing.css`**

```css

/* ---------------------------------------------------------------------- */
/* Feature bento grid                                                     */
/* ---------------------------------------------------------------------- */

.feature-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.5rem;
}

.feature-grid__item--large .card {
  min-height: 220px;
}

@media (min-width: 992px) {
  .feature-grid {
    grid-template-columns: repeat(3, 1fr);
    grid-auto-rows: minmax(160px, auto);
  }

  .feature-grid__item--large {
    grid-row: span 2;
  }

  .feature-grid__item--large .card {
    min-height: 100%;
  }
}

.feature-illustration {
  margin-top: 1rem;
  padding: 0.875rem;
  border-radius: var(--radius-md);
  background: var(--color-canvas);
}

.feature-illustration__row {
  display: flex;
  justify-content: space-between;
  margin-bottom: 0.625rem;
}

.feature-illustration__chip {
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  background: var(--color-primary-soft);
  color: var(--color-primary-strong);
}

.feature-illustration__chip--muted {
  background: var(--color-surface);
  color: var(--color-muted);
}

.feature-illustration__slots {
  display: flex;
  gap: 0.5rem;
}

.feature-illustration__slot {
  flex: 1;
  text-align: center;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.375rem 0;
  border-radius: var(--radius-sm);
  background: var(--color-surface);
  color: var(--color-body);
  border: 1px solid var(--color-border);
}

.feature-illustration__slot--active {
  background: var(--color-teal);
  border-color: var(--color-teal);
  color: #fff;
}

.feature-illustration--calendar {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.3rem;
}

.feature-illustration__day-label {
  font-size: 0.65rem;
  text-align: center;
  color: var(--color-muted);
  font-weight: 700;
}

.feature-illustration__day {
  aspect-ratio: 1;
  border-radius: 4px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
}

.feature-illustration__day--booked {
  background: var(--color-primary-soft);
  border-color: var(--color-primary);
}

.feature-illustration--chart {
  display: flex;
  align-items: flex-end;
  gap: 0.5rem;
  height: 90px;
}

.feature-illustration__bar {
  flex: 1;
  border-radius: 4px 4px 0 0;
  background: linear-gradient(180deg, var(--color-teal) 0%, var(--color-primary) 100%);
}
```

---

### Task 11: Dashboard preview section

**Files:**
- Modify: `src/components/landing/DashboardPreviewSection.tsx` (full rewrite)

**Interfaces:**
- Consumes: `BrowserFrame`, `Reveal` (Task 5), `SchedulePreviewCard` (existing, unchanged — accepts `className?: string`).

- [ ] **Step 1: Rewrite `DashboardPreviewSection.tsx`**

Replace the full contents of `src/components/landing/DashboardPreviewSection.tsx`:

```tsx
import { Icon } from '../common/Icon'
import { SchedulePreviewCard } from './SchedulePreviewCard'
import { BrowserFrame } from '../common/BrowserFrame'
import { Reveal } from '../common/Reveal'

const DASHBOARD_HIGHLIGHTS = [
  "Today's bookings at a glance",
  'A shared calendar for every team member',
  'Upcoming appointments for the week ahead',
  'Team availability, always up to date',
  'Booking reports without spreadsheets',
]

export function DashboardPreviewSection() {
  return (
    <section className="py-5 py-lg-6 bg-light border-bottom">
      <div className="container">
        <div className="row align-items-center gy-5">
          <Reveal className="col-lg-6 order-2 order-lg-1">
            <p className="text-eyebrow mb-2">The booking dashboard</p>
            <h2 className="fw-bold font-display mb-3">See your whole day at a glance</h2>
            <p className="text-secondary mb-4">
              The Booking dashboard brings today&apos;s schedule, team availability, and booking activity together
              in one operational view — no digging through menus to find out what's happening.
            </p>
            <ul className="list-unstyled d-flex flex-column gap-3 mb-0">
              {DASHBOARD_HIGHLIGHTS.map((item) => (
                <li key={item} className="d-flex align-items-center gap-2">
                  <Icon name="check-circle" size={20} />
                  {item}
                </li>
              ))}
            </ul>
          </Reveal>
          <Reveal className="col-lg-6 order-1 order-lg-2" delayStep={1}>
            <BrowserFrame url="app.apexbooking.com/booking/calendar">
              <div className="p-3 p-lg-4">
                <SchedulePreviewCard className="border-0 shadow-none" />
              </div>
            </BrowserFrame>
          </Reveal>
        </div>
      </div>
    </section>
  )
}
```

Mobile: the `order-1`/`order-2` classes put the frame (browser preview) above the bullet list, per spec. Desktop (`lg`+): `order-lg-1`/`order-lg-2` restores text-left/preview-right, matching the original layout.

---

### Task 12: How It Works — typography-only steps

**Files:**
- Modify: `src/components/landing/HowItWorksSection.tsx` (full rewrite)
- Modify: `src/styles/landing.css` (append)

**Interfaces:**
- Consumes: `HOW_IT_WORKS_STEPS` from `src/config/howItWorks.ts` (existing, unchanged data shape — only its copy was renamed in Task 1), `Reveal` (Task 5).

- [ ] **Step 1: Rewrite `HowItWorksSection.tsx`**

Replace the full contents of `src/components/landing/HowItWorksSection.tsx`:

```tsx
import { HOW_IT_WORKS_STEPS } from '../../config/howItWorks'
import { Reveal } from '../common/Reveal'

export function HowItWorksSection() {
  return (
    <section className="py-5 py-lg-6 bg-light border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">From sign-up to first booking</p>
          <h2 className="fw-bold font-display">How it works</h2>
        </Reveal>
        <div className="row g-4">
          {HOW_IT_WORKS_STEPS.map((item, index) => (
            <div className="col-sm-6 col-lg-3" key={item.step}>
              <Reveal delayStep={index}>
                <div className="text-center text-sm-start">
                  <p className="step-number font-display mb-2">{String(item.step).padStart(2, '0')}</p>
                  <h3 className="h6">{item.title}</h3>
                  <p className="text-secondary mb-0">{item.description}</p>
                </div>
              </Reveal>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 2: Append step-number styles to `landing.css`**

```css

/* ---------------------------------------------------------------------- */
/* How it works — step numbers                                           */
/* ---------------------------------------------------------------------- */

.step-number {
  font-size: 2.75rem;
  font-weight: 800;
  color: var(--color-primary);
  line-height: 1;
}
```

---

### Task 13: Assemble `LandingPage`, wrap remaining sections, finish rename

**Files:**
- Modify: `src/pages/LandingPage.tsx` (insert `ProblemSection`, add `pt-nav` to `<main>`)
- Modify: `src/components/landing/Footer.tsx` (full rewrite — rename + logo fix)
- Modify: `src/components/landing/CallToActionSection.tsx` (full rewrite — rename + `Reveal`)
- Modify: `src/components/landing/PricingSection.tsx` (full rewrite — `Reveal` only, no rename needed)

**Interfaces:**
- Consumes: `ProblemSection` (Task 8), `Reveal` (Task 5), `NAV_ITEMS` (existing).

- [ ] **Step 1: Insert `ProblemSection` and fix header offset in `LandingPage.tsx`**

Replace the full contents of `src/pages/LandingPage.tsx`:

```tsx
import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { Header } from '../components/landing/Header'
import { HeroSection } from '../components/landing/HeroSection'
import { ProblemSection } from '../components/landing/ProblemSection'
import { BusinessSection } from '../components/landing/BusinessSection'
import { FeaturesSection } from '../components/landing/FeaturesSection'
import { DashboardPreviewSection } from '../components/landing/DashboardPreviewSection'
import { PricingSection } from '../components/landing/PricingSection'
import { HowItWorksSection } from '../components/landing/HowItWorksSection'
import { CallToActionSection } from '../components/landing/CallToActionSection'
import { Footer } from '../components/landing/Footer'

export function LandingPage() {
  const location = useLocation()

  useEffect(() => {
    const shouldScrollToPricing = location.hash === '#pricing'

    if (!shouldScrollToPricing) {
      return
    }

    const pricingSection = document.getElementById('pricing')
    if (pricingSection) {
      pricingSection.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }, [location.hash])

  return (
    <>
      <Header />
      <main className="pt-nav">
        <HeroSection />
        <ProblemSection />
        <BusinessSection />
        <FeaturesSection />
        <DashboardPreviewSection />
        <PricingSection />
        <HowItWorksSection />
        <CallToActionSection />
      </main>
      <Footer />
    </>
  )
}
```

- [ ] **Step 2: Rewrite the landing `Footer.tsx`**

Replace the full contents of `src/components/landing/Footer.tsx` (renames the copy and switches the logo from the broken `brandLogo` import to the same `/favicon.svg` public-path reference `Header.tsx` now uses):

```tsx
import { NAV_ITEMS } from '../../config/navigation'

const LEGAL_LINKS = ['Privacy Policy', 'Terms of Service']

export function Footer() {
  const year = new Date().getFullYear()

  return (
    <footer id="contact" className="py-5 bg-light border-top">
      <div className="container">
        <div className="row gy-4">
          <div className="col-md-4">
            <div className="d-flex align-items-center gap-2 mb-2">
              <img src="/favicon.svg" alt="ApexBooking logo" width={28} height={28} />
              <span className="fw-bold fs-5 font-display">ApexBooking</span>
            </div>
            <p className="text-secondary mb-2">Online booking for local businesses.</p>
            <p className="text-secondary mb-0">&copy; {year} ApexBooking. All rights reserved.</p>
          </div>
          <div className="col-md-4">
            <h3 className="h6 text-uppercase text-secondary mb-3">Navigation</h3>
            <ul className="list-unstyled d-flex flex-column gap-2">
              {NAV_ITEMS.map((item) => (
                <li key={item.href}>
                  <a href={item.href} className="link-dark text-decoration-none">
                    {item.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>
          <div className="col-md-4">
            <h3 className="h6 text-uppercase text-secondary mb-3">Legal</h3>
            <ul className="list-unstyled d-flex flex-column gap-2 text-secondary">
              {LEGAL_LINKS.map((label) => (
                <li key={label}>{label}</li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </footer>
  )
}
```

- [ ] **Step 3: Rewrite `CallToActionSection.tsx`**

Replace the full contents of `src/components/landing/CallToActionSection.tsx`:

```tsx
import { Button } from '../common/Button'
import { Reveal } from '../common/Reveal'
import { scrollToPricing } from '../../utils/scrollToPricing'

export function CallToActionSection() {
  return (
    <section className="py-5 py-lg-6 bg-gradient-brand text-white text-center">
      <div className="container">
        <Reveal>
          <h2 className="fw-bold font-display mb-3">Ready to bring your bookings online?</h2>
          <p className="mb-4 opacity-75">
            Join local businesses already using ApexBooking to fill their schedule and delight customers.
          </p>
          <div className="d-flex flex-wrap justify-content-center gap-3">
            <Button to="/#pricing" variant="light" size="lg" onClick={scrollToPricing}>
              Request Access
            </Button>
          </div>
        </Reveal>
      </div>
    </section>
  )
}
```

- [ ] **Step 4: Rewrite `PricingSection.tsx`**

Replace the full contents of `src/components/landing/PricingSection.tsx` (no rename needed here — `pricing.ts` doesn't reference the brand name — just adds `Reveal` and the display font class):

```tsx
import { PRICING_PLANS } from '../../config/pricing'
import { Card } from '../common/Card'
import { Badge } from '../common/Badge'
import { Button } from '../common/Button'
import { Icon } from '../common/Icon'
import { Reveal } from '../common/Reveal'

export function PricingSection() {
  return (
    <section id="pricing" className="py-5 py-lg-6 border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">Plans for every stage</p>
          <h2 className="fw-bold font-display">Simple, transparent pricing</h2>
          <p className="text-secondary mb-0">Choose the plan that fits your business.</p>
        </Reveal>
        <div className="row g-4 justify-content-center">
          {PRICING_PLANS.map((plan, index) => (
            <div className="col-md-6 col-lg-5" key={plan.id}>
              <Reveal delayStep={index}>
                <Card
                  hover
                  className={`h-100 ${plan.recommended ? 'border border-2 border-primary' : ''}`}
                  bodyClassName="d-flex flex-column"
                >
                  {plan.recommended && (
                    <Badge tone="primary" className="align-self-start mb-2">
                      Recommended
                    </Badge>
                  )}
                  <h3 className="h4 mb-1">{plan.name}</h3>
                  <p className="text-secondary">{plan.description}</p>
                  <ul className="list-unstyled d-flex flex-column gap-2 mb-4">
                    {plan.features.map((feature) => (
                      <li key={feature} className="d-flex align-items-center gap-2">
                        <Icon name="check-circle" size={16} />
                        {feature}
                      </li>
                    ))}
                  </ul>

                  <Button
                    to={`/request-access?plan=${plan.id}`}
                    variant={plan.recommended ? 'primary' : 'outline-primary'}
                    className="mt-auto"
                  >
                    {plan.ctaLabel}
                  </Button>
                </Card>
              </Reveal>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
```

---

## Manual Verification (performed by the user, not part of this plan's tasks)

Per the user's instruction, no build/lint/test/dev-server-preview steps are included above. Once all 13 tasks are applied, the user will run their own `npm run dev` / `tsc -b` / `oxlint` / `vite build` pass and visually check the result. Two things worth knowing going in:

1. Task 7's screenshot capture is the one step that *requires* the dev server to be running — it's listed inside that task because the PNG it produces is a required file for Task 7 and Task 11, not because it's a verification step.
2. `AuthLayout.tsx`, `SuperAdminAuthLayout.tsx`, `ModuleSwitcher.tsx`, and `AdminSidebar.tsx` all import a `brandLogo` from `../../assets/favicon.png`, which doesn't exist and predates this plan (see the spec's "Current-State Findings"). This plan renames their text but does not fix that broken import, since those files are outside the landing-page/nav scope this plan covers.
