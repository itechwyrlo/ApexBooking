# ApexBooking Landing Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the ApexBooking marketing landing page (`/`) â€” a single static, responsive React page composed of reusable sections, with a working native PWA install affordance. No auth, no API calls, no other pages.

**Architecture:** One route (`/`) rendering `pages/LandingPage.tsx`, which composes presentational section components from `components/landing/`. All copy/list data (nav items, industries, features, pricing, steps) lives in typed config files under `src/config/`, driven by interfaces in `src/interfaces/`. PWA install behavior is isolated in `hooks/useInstallPrompt.ts` + `components/pwa/InstallAppButton.tsx`.

**Tech Stack:** React 19 + TypeScript + Vite, React Router v7 (single route only), Bootstrap 5 (CSS + JS bundle, no react-bootstrap), vite-plugin-pwa. All already installed â€” no new dependencies.

## Global Constraints

- TypeScript only: `.ts`/`.tsx` files, never `.js`/`.jsx`.
- `verbatimModuleSyntax` is on (see `tsconfig.app.json`) â€” use `import type { X }` for type-only imports, or the build fails.
- Components: PascalCase filenames (`Header.tsx`). Hooks: `use`-prefixed (`useInstallPrompt.ts`). Interfaces: `I`-prefixed (`IIndustry`). Constants: `UPPER_SNAKE_CASE`.
- Light theme only. No dark mode, no `prefers-color-scheme` branching, no glassmorphism/neumorphism/gradients.
- Bootstrap 5 utilities first; only reach for inline styles when no utility covers it (e.g. an exact pixel circle size).
- Mobile-first, fluid layouts only â€” no fixed-width containers, Bootstrap grid + breakpoints throughout.
- Never generate icon or image files. Reference them as plain string paths (`/assets/icons/...`, `/assets/images/...`), never as ES `import`s, so a missing file 404s in the browser instead of breaking the build.
- Never use `alert()` or `confirm()` â€” not needed on this page (no destructive actions), noted here only as a standing constraint.
- Navigation must be configuration-driven (`src/config/navigation.ts`), never hardcoded lists of links in JSX.
- No test framework in this project â€” verify each task with `npx tsc -b` (typecheck) and `npm run lint` (oxlint); verify the final integration task by running the dev server and checking the page in a browser at mobile/tablet/desktop widths.
- No git repository in this project â€” no commit steps in this plan.
- Do not touch anything outside the landing page: no auth, no API integration, no dashboard, no other routes/pages.

---

## File Structure

```
src/
  interfaces/
    INavItem.ts
    IIndustry.ts
    IFeature.ts
    IPricingPlan.ts
    IProcessStep.ts
  constants/
    sectionIds.ts
  config/
    navigation.ts
    industries.ts
    features.ts
    pricing.ts
    howItWorks.ts
  hooks/
    useInstallPrompt.ts
  components/
    pwa/
      InstallAppButton.tsx
    landing/
      Header.tsx
      HeroSection.tsx
      BusinessSection.tsx
      FeaturesSection.tsx
      DashboardPreviewSection.tsx
      PricingSection.tsx
      HowItWorksSection.tsx
      CallToActionSection.tsx
      Footer.tsx
  pages/
    LandingPage.tsx
  App.tsx (rewritten)
  main.tsx (rewritten)
  index.css (rewritten)
  App.css (deleted)
  assets/react.svg, assets/vite.svg, assets/hero.png (deleted, unused template assets)
public/
  icons.svg (deleted, unused template sprite)
vite.config.ts (manifest rebranded)
index.html (title/meta rebranded)
PROJECT_TRACKER.md (created)
```

---

### Task 1: Project shell â€” global styles, Bootstrap wiring, PWA manifest, HTML metadata

**Files:**
- Modify: `src/index.css` (full rewrite)
- Modify: `src/main.tsx`
- Modify: `vite.config.ts`
- Modify: `index.html`

**Interfaces:**
- Consumes: nothing (first task)
- Produces: global Bootstrap CSS/JS available to every component built in later tasks; `#root`/`main` flex layout so the footer stays at the bottom on short pages

Note: `src/App.css` is left in place for this task â€” `App.tsx` still imports it until Task 13 rewrites `App.tsx`. Deleting it here would break `npm run dev`/`npm run build` in the interim (Vite can't resolve a CSS import to a missing file), even though `tsc -b` wouldn't catch it. It's deleted in Task 13 alongside the `App.tsx` rewrite that drops the import.

- [ ] **Step 1: Rewrite `src/index.css` to a light-only, fluid base**

```css
:root {
  color-scheme: light;
}

body {
  margin: 0;
  font-family: system-ui, 'Segoe UI', Roboto, sans-serif;
}

#root {
  min-height: 100svh;
  display: flex;
  flex-direction: column;
}

main {
  flex: 1;
}
```

- [ ] **Step 2: Rewrite `src/main.tsx` to load Bootstrap and wrap the app in a router**

```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
)
```

- [ ] **Step 3: Rebrand the PWA manifest in `vite.config.ts`**

Replace the existing placeholder `manifest` object (currently named `'My First PWA'`) with:

```ts
      manifest: {
        name: 'ApexBooking',
        short_name: 'ApexBooking',
        description: 'Booking and business management platform for local businesses.',
        theme_color: '#0d6efd',
        background_color: '#ffffff',
        display: 'standalone',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any maskable'
          }
        ]
      }
```

Leave `registerType` and `includeAssets` as they are.

- [ ] **Step 4: Update `index.html` title and metadata**

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/favicon.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="description" content="ApexBooking is a multi-tenant platform for local businesses. Start with Booking to manage appointments, staff, and services online." />
    <meta name="theme-color" content="#0d6efd" />
    <title>ApexBooking</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
```

- [ ] **Step 5: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors (App.tsx still exists with old template code at this point, unaffected by this task).

Run: `npm run lint`
Expected: no errors.

---

### Task 2: Domain interfaces and typed config data

**Files:**
- Create: `src/interfaces/INavItem.ts`
- Create: `src/interfaces/IIndustry.ts`
- Create: `src/interfaces/IFeature.ts`
- Create: `src/interfaces/IPricingPlan.ts`
- Create: `src/interfaces/IProcessStep.ts`
- Create: `src/constants/sectionIds.ts`
- Create: `src/config/navigation.ts`
- Create: `src/config/industries.ts`
- Create: `src/config/features.ts`
- Create: `src/config/pricing.ts`
- Create: `src/config/howItWorks.ts`

**Interfaces:**
- Consumes: nothing
- Produces: `INavItem`, `IIndustry`, `IFeature`, `IPricingPlan`, `IProcessStep` types; `SECTION_IDS` constant; `NAV_ITEMS`, `INDUSTRIES`, `BOOKING_FEATURES`, `PRICING_PLANS`, `HOW_IT_WORKS_STEPS` arrays â€” consumed by every component task below

- [ ] **Step 1: Create the interfaces**

`src/interfaces/INavItem.ts`
```ts
export interface INavItem {
  label: string
  href: string
}
```

`src/interfaces/IIndustry.ts`
```ts
export interface IIndustry {
  id: string
  name: string
  icon: string
}
```

`src/interfaces/IFeature.ts`
```ts
export interface IFeature {
  id: string
  title: string
  description: string
  icon: string
  comingSoon?: boolean
}
```

`src/interfaces/IPricingPlan.ts`
```ts
export interface IPricingPlan {
  id: string
  name: string
  description: string
  features: string[]
  ctaLabel: string
  recommended?: boolean
}
```

`src/interfaces/IProcessStep.ts`
```ts
export interface IProcessStep {
  step: number
  title: string
  description: string
}
```

- [ ] **Step 2: Create the section id constants**

`src/constants/sectionIds.ts`
```ts
export const SECTION_IDS = {
  HOME: 'home',
  FEATURES: 'features',
  PRICING: 'pricing',
  CONTACT: 'contact',
} as const
```

- [ ] **Step 3: Create the navigation config**

`src/config/navigation.ts`
```ts
import type { INavItem } from '../interfaces/INavItem'
import { SECTION_IDS } from '../constants/sectionIds'

export const NAV_ITEMS: INavItem[] = [
  { label: 'Home', href: `#${SECTION_IDS.HOME}` },
  { label: 'Features', href: `#${SECTION_IDS.FEATURES}` },
  { label: 'Pricing', href: `#${SECTION_IDS.PRICING}` },
  { label: 'Contact', href: `#${SECTION_IDS.CONTACT}` },
]
```

- [ ] **Step 4: Create the industries config**

`src/config/industries.ts`
```ts
import type { IIndustry } from '../interfaces/IIndustry'

export const INDUSTRIES: IIndustry[] = [
  { id: 'salon', name: 'Salon', icon: '/assets/icons/industries/salon.svg' },
  { id: 'barbershop', name: 'Barbershop', icon: '/assets/icons/industries/barbershop.svg' },
  { id: 'clinic', name: 'Clinic', icon: '/assets/icons/industries/clinic.svg' },
  { id: 'hardware-store', name: 'Hardware Store', icon: '/assets/icons/industries/hardware-store.svg' },
  { id: 'construction-supplier', name: 'Construction Supplier', icon: '/assets/icons/industries/construction-supplier.svg' },
  { id: 'retail-store', name: 'Retail Store', icon: '/assets/icons/industries/retail-store.svg' },
  { id: 'auto-repair-shop', name: 'Auto Repair Shop', icon: '/assets/icons/industries/auto-repair-shop.svg' },
  { id: 'small-wholesaler', name: 'Small Wholesaler', icon: '/assets/icons/industries/small-wholesaler.svg' },
]
```

- [ ] **Step 5: Create the booking features config**

`src/config/features.ts`
```ts
import type { IFeature } from '../interfaces/IFeature'

export const BOOKING_FEATURES: IFeature[] = [
  {
    id: 'online-booking',
    title: 'Online Booking',
    description: 'Customers can book appointments online through a dedicated booking page.',
    icon: '/assets/icons/features/online-booking.svg',
  },
  {
    id: 'customer-booking-page',
    title: 'Customer Booking Page',
    description: 'Public-facing booking experience.',
    icon: '/assets/icons/features/customer-booking-page.svg',
  },
  {
    id: 'staff-management',
    title: 'Staff Management',
    description: 'Manage staff availability and assignments.',
    icon: '/assets/icons/features/staff-management.svg',
  },
  {
    id: 'service-management',
    title: 'Service Management',
    description: 'Create and organize available services.',
    icon: '/assets/icons/features/service-management.svg',
  },
  {
    id: 'booking-calendar',
    title: 'Booking Calendar',
    description: 'Visual calendar displaying current bookings and schedules.',
    icon: '/assets/icons/features/booking-calendar.svg',
  },
  {
    id: 'dashboard-reports',
    title: 'Dashboard Reports',
    description: 'Business insights and booking statistics.',
    icon: '/assets/icons/features/dashboard-reports.svg',
  },
  {
    id: 'email-notifications',
    title: 'Email Notifications',
    description: 'Booking confirmations and reminders.',
    icon: '/assets/icons/features/email-notifications.svg',
  },
  {
    id: 'sms-notifications',
    title: 'SMS Notifications',
    description: 'Booking reminders sent directly to customer phones.',
    icon: '/assets/icons/features/sms-notifications.svg',
    comingSoon: true,
  },
]
```

- [ ] **Step 6: Create the pricing config**

`src/config/pricing.ts`
```ts
import type { IPricingPlan } from '../interfaces/IPricingPlan'

export const PRICING_PLANS: IPricingPlan[] = [
  {
    id: 'basic',
    name: 'Basic',
    description: 'Designed for small businesses.',
    features: [
      'Booking Calendar',
      'Staff Management',
      'Service Management',
      'Customer Booking Page',
      'Email Notifications',
    ],
    ctaLabel: 'Request Access',
  },
  {
    id: 'professional',
    name: 'Professional',
    description: 'Everything in Basic, plus advanced tools for growing businesses.',
    features: [
      'Everything in Basic',
      'Dashboard Reports',
      'Advanced Analytics',
      'Priority Support',
      'SMS Notifications (Coming Soon)',
      'Future Business Modules',
    ],
    ctaLabel: 'Request Access',
    recommended: true,
  },
]
```

- [ ] **Step 7: Create the how-it-works config**

`src/config/howItWorks.ts`
```ts
import type { IProcessStep } from '../interfaces/IProcessStep'

export const HOW_IT_WORKS_STEPS: IProcessStep[] = [
  { step: 1, title: 'Create your business', description: 'Set up your ApexBooking workspace in minutes.' },
  { step: 2, title: 'Configure staff and services', description: 'Add your team and the services they offer.' },
  { step: 3, title: 'Share your booking page', description: 'Give customers a link to book with you directly.' },
  { step: 4, title: 'Receive bookings online', description: 'Manage appointments as they come in.' },
]
```

- [ ] **Step 8: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 3: PWA install hook and Install App button

**Files:**
- Create: `src/hooks/useInstallPrompt.ts`
- Create: `src/components/pwa/InstallAppButton.tsx`

**Interfaces:**
- Consumes: nothing from earlier tasks
- Produces: `useInstallPrompt(): { isInstallable: boolean; isInstalled: boolean; promptInstall: () => Promise<void> }`; `<InstallAppButton />` component â€” consumed by Header in Task 4

- [ ] **Step 1: Create the install prompt hook**

`src/hooks/useInstallPrompt.ts`
```ts
import { useEffect, useState } from 'react'

interface IBeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

interface IUseInstallPromptResult {
  isInstallable: boolean
  isInstalled: boolean
  promptInstall: () => Promise<void>
}

function isRunningStandalone(): boolean {
  return window.matchMedia('(display-mode: standalone)').matches
}

export function useInstallPrompt(): IUseInstallPromptResult {
  const [deferredPrompt, setDeferredPrompt] = useState<IBeforeInstallPromptEvent | null>(null)
  const [isInstalled, setIsInstalled] = useState<boolean>(isRunningStandalone())

  useEffect(() => {
    const handleBeforeInstallPrompt = (event: Event) => {
      event.preventDefault()
      setDeferredPrompt(event as IBeforeInstallPromptEvent)
    }

    const handleAppInstalled = () => {
      setIsInstalled(true)
      setDeferredPrompt(null)
    }

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
    window.addEventListener('appinstalled', handleAppInstalled)

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
      window.removeEventListener('appinstalled', handleAppInstalled)
    }
  }, [])

  const promptInstall = async (): Promise<void> => {
    if (!deferredPrompt) {
      return
    }
    await deferredPrompt.prompt()
    await deferredPrompt.userChoice
    setDeferredPrompt(null)
  }

  return {
    isInstallable: deferredPrompt !== null && !isInstalled,
    isInstalled,
    promptInstall,
  }
}
```

- [ ] **Step 2: Create the Install App button**

`src/components/pwa/InstallAppButton.tsx`
```tsx
import { useInstallPrompt } from '../../hooks/useInstallPrompt'

export function InstallAppButton() {
  const { isInstallable, promptInstall } = useInstallPrompt()

  if (!isInstallable) {
    return null
  }

  return (
    <button type="button" className="btn btn-outline-primary" onClick={promptInstall}>
      Install App
    </button>
  )
}
```

- [ ] **Step 3: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

Note: `beforeinstallprompt` won't fire in a plain `npm run dev` session (no HTTPS/installability criteria met), so `InstallAppButton` is expected to render nothing at this stage â€” that's correct behavior, not a bug. It's exercised visually once mounted in Task 13.

---

### Task 4: Header component

**Files:**
- Create: `src/components/landing/Header.tsx`

**Interfaces:**
- Consumes: `NAV_ITEMS` from `src/config/navigation.ts` (Task 2); `InstallAppButton` from `src/components/pwa/InstallAppButton.tsx` (Task 3)
- Produces: `<Header />` â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the Header**

`src/components/landing/Header.tsx`
```tsx
import { NAV_ITEMS } from '../../config/navigation'
import { InstallAppButton } from '../pwa/InstallAppButton'

export function Header() {
  return (
    <header className="sticky-top bg-white border-bottom">
      <nav className="navbar navbar-expand-lg container py-3">
        <a className="navbar-brand d-flex align-items-center gap-2" href="#home">
          <img src="/assets/icons/logo.svg" alt="" width={32} height={32} />
          <span className="fw-semibold fs-5">ApexBooking</span>
        </a>
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#mainNav"
          aria-controls="mainNav"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon"></span>
        </button>
        <div className="collapse navbar-collapse" id="mainNav">
          <ul className="navbar-nav mx-auto my-3 my-lg-0 gap-lg-4">
            {NAV_ITEMS.map((item) => (
              <li className="nav-item" key={item.href}>
                <a className="nav-link" href={item.href}>
                  {item.label}
                </a>
              </li>
            ))}
          </ul>
          <div className="d-flex align-items-center gap-2">
            <InstallAppButton />
            <button type="button" className="btn btn-outline-secondary">
              Login
            </button>
            <button type="button" className="btn btn-primary">
              Request Access
            </button>
          </div>
        </div>
      </nav>
    </header>
  )
}
```

Login and Request Access are plain buttons with no handler: no auth flow exists yet (out of scope per the feature prompt), so they're visually present but inert.

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 5: Hero section

**Files:**
- Create: `src/components/landing/HeroSection.tsx`

**Interfaces:**
- Consumes: nothing from config (static copy per spec)
- Produces: `<HeroSection />` with `id="home"` â€” the anchor target for the Header's "Home" link â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the Hero section**

`src/components/landing/HeroSection.tsx`
```tsx
export function HeroSection() {
  return (
    <section id="home" className="py-5 border-bottom">
      <div className="container">
        <div className="row align-items-center gy-5">
          <div className="col-lg-6">
            <h1 className="display-5 fw-bold mb-3">
              Run your local business, without the busywork.
            </h1>
            <p className="lead text-secondary mb-4">
              ApexBooking is a multi-tenant platform built for local businesses â€”
              salons, clinics, auto shops, and more. Start with Booking, the
              first available module, to manage appointments, staff, and
              services from one place.
            </p>
            <div className="d-flex flex-wrap gap-3">
              <button type="button" className="btn btn-primary btn-lg">
                Request Access
              </button>
              <a href="#features" className="btn btn-outline-primary btn-lg">
                Explore Features
              </a>
            </div>
          </div>
          <div className="col-lg-6 d-none d-lg-block">
            <img
              src="/assets/images/dashboard-preview.png"
              alt="ApexBooking dashboard preview"
              className="img-fluid rounded-3 shadow-sm"
            />
          </div>
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 6: Businesses We Support section

**Files:**
- Create: `src/components/landing/BusinessSection.tsx`

**Interfaces:**
- Consumes: `INDUSTRIES` from `src/config/industries.ts` (Task 2)
- Produces: `<BusinessSection />` â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the Business section**

`src/components/landing/BusinessSection.tsx`
```tsx
import { INDUSTRIES } from '../../config/industries'

export function BusinessSection() {
  return (
    <section id="businesses" className="py-5 bg-light border-bottom">
      <div className="container">
        <div className="text-center mb-5">
          <h2 className="fw-bold">Businesses We Support</h2>
          <p className="text-secondary mb-0">
            ApexBooking is built for the way local businesses actually work.
          </p>
        </div>
        <div className="row g-4">
          {INDUSTRIES.map((industry) => (
            <div className="col-6 col-md-4 col-lg-3" key={industry.id}>
              <div className="card h-100 text-center border-0 shadow-sm">
                <div className="card-body">
                  <img src={industry.icon} alt="" width={40} height={40} className="mb-3" />
                  <p className="fw-medium mb-0">{industry.name}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 7: Booking Features section

**Files:**
- Create: `src/components/landing/FeaturesSection.tsx`

**Interfaces:**
- Consumes: `BOOKING_FEATURES` from `src/config/features.ts` (Task 2)
- Produces: `<FeaturesSection />` with `id="features"` â€” the anchor target for the Header's "Features" link and Hero's "Explore Features" button â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the Features section**

`src/components/landing/FeaturesSection.tsx`
```tsx
import { BOOKING_FEATURES } from '../../config/features'

export function FeaturesSection() {
  return (
    <section id="features" className="py-5 border-bottom">
      <div className="container">
        <div className="text-center mb-5">
          <h2 className="fw-bold">Everything you need to manage bookings</h2>
          <p className="text-secondary mb-0">
            The Booking module is the first available product on ApexBooking.
          </p>
        </div>
        <div className="row g-4">
          {BOOKING_FEATURES.map((feature) => (
            <div className="col-md-6 col-lg-4" key={feature.id}>
              <div className="card h-100 border-0 shadow-sm">
                <div className="card-body">
                  <div className="d-flex align-items-center gap-2 mb-2">
                    <img src={feature.icon} alt="" width={28} height={28} />
                    <h3 className="h5 mb-0">{feature.title}</h3>
                    {feature.comingSoon && (
                      <span className="badge text-bg-secondary ms-auto">Coming Soon</span>
                    )}
                  </div>
                  <p className="text-secondary mb-0">{feature.description}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 8: Dashboard Preview section

**Files:**
- Create: `src/components/landing/DashboardPreviewSection.tsx`

**Interfaces:**
- Consumes: nothing from config (static highlight list per spec)
- Produces: `<DashboardPreviewSection />` â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the Dashboard Preview section**

`src/components/landing/DashboardPreviewSection.tsx`
```tsx
const DASHBOARD_HIGHLIGHTS = [
  "Today's Bookings",
  'Calendar',
  'Upcoming Appointments',
  'Staff Schedule',
  'Booking Reports',
]

export function DashboardPreviewSection() {
  return (
    <section className="py-5 bg-light border-bottom">
      <div className="container">
        <div className="row align-items-center gy-5">
          <div className="col-lg-6">
            <h2 className="fw-bold mb-3">See your whole day at a glance</h2>
            <p className="text-secondary mb-4">
              The Booking dashboard brings your schedule, staff, and reports
              together in one view.
            </p>
            <ul className="list-unstyled d-flex flex-column gap-2 mb-0">
              {DASHBOARD_HIGHLIGHTS.map((item) => (
                <li key={item} className="d-flex align-items-center gap-2">
                  <span className="badge rounded-pill text-bg-primary">&#10003;</span>
                  {item}
                </li>
              ))}
            </ul>
          </div>
          <div className="col-lg-6">
            <img
              src="/assets/images/dashboard-overview.png"
              alt="ApexBooking dashboard overview"
              className="img-fluid rounded-3 shadow-sm"
            />
          </div>
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 9: Pricing section

**Files:**
- Create: `src/components/landing/PricingSection.tsx`

**Interfaces:**
- Consumes: `PRICING_PLANS` from `src/config/pricing.ts` (Task 2)
- Produces: `<PricingSection />` with `id="pricing"` â€” the anchor target for the Header's "Pricing" link â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the Pricing section**

`src/components/landing/PricingSection.tsx`
```tsx
import { PRICING_PLANS } from '../../config/pricing'

export function PricingSection() {
  return (
    <section id="pricing" className="py-5 border-bottom">
      <div className="container">
        <div className="text-center mb-5">
          <h2 className="fw-bold">Simple, transparent pricing</h2>
          <p className="text-secondary mb-0">Choose the plan that fits your business.</p>
        </div>
        <div className="row g-4 justify-content-center">
          {PRICING_PLANS.map((plan) => (
            <div className="col-md-6 col-lg-5" key={plan.id}>
              <div
                className={`card h-100 shadow-sm ${plan.recommended ? 'border-primary border-2' : 'border-0'}`}
              >
                <div className="card-body d-flex flex-column">
                  {plan.recommended && (
                    <span className="badge text-bg-primary align-self-start mb-2">
                      Recommended
                    </span>
                  )}
                  <h3 className="h4 mb-1">{plan.name}</h3>
                  <p className="text-secondary">{plan.description}</p>
                  <ul className="list-unstyled d-flex flex-column gap-2 mb-4">
                    {plan.features.map((feature) => (
                      <li key={feature} className="d-flex align-items-center gap-2">
                        <span aria-hidden="true">&#10003;</span>
                        {feature}
                      </li>
                    ))}
                  </ul>
                  <button
                    type="button"
                    className={`btn mt-auto ${plan.recommended ? 'btn-primary' : 'btn-outline-primary'}`}
                  >
                    {plan.ctaLabel}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 10: How It Works section

**Files:**
- Create: `src/components/landing/HowItWorksSection.tsx`

**Interfaces:**
- Consumes: `HOW_IT_WORKS_STEPS` from `src/config/howItWorks.ts` (Task 2)
- Produces: `<HowItWorksSection />` â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the How It Works section**

`src/components/landing/HowItWorksSection.tsx`
```tsx
import { HOW_IT_WORKS_STEPS } from '../../config/howItWorks'

export function HowItWorksSection() {
  return (
    <section className="py-5 bg-light border-bottom">
      <div className="container">
        <div className="text-center mb-5">
          <h2 className="fw-bold">How it works</h2>
        </div>
        <div className="row g-4">
          {HOW_IT_WORKS_STEPS.map((item) => (
            <div className="col-sm-6 col-lg-3" key={item.step}>
              <div className="text-center">
                <div
                  className="d-inline-flex align-items-center justify-content-center rounded-circle bg-primary text-white fw-bold mb-3"
                  style={{ width: 40, height: 40 }}
                >
                  {item.step}
                </div>
                <h3 className="h6">{item.title}</h3>
                <p className="text-secondary mb-0">{item.description}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 11: Call To Action section

**Files:**
- Create: `src/components/landing/CallToActionSection.tsx`

**Interfaces:**
- Consumes: nothing
- Produces: `<CallToActionSection />` â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the Call To Action section**

`src/components/landing/CallToActionSection.tsx`
```tsx
export function CallToActionSection() {
  return (
    <section className="py-5 bg-primary text-white text-center">
      <div className="container">
        <h2 className="fw-bold mb-3">Ready to bring your business online?</h2>
        <p className="mb-4 opacity-75">
          Join local businesses already using ApexBooking to manage bookings.
        </p>
        <div className="d-flex flex-wrap justify-content-center gap-3">
          <button type="button" className="btn btn-light btn-lg">
            Request Access
          </button>
          <button type="button" className="btn btn-outline-light btn-lg">
            Login
          </button>
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 12: Footer

**Files:**
- Create: `src/components/landing/Footer.tsx`

**Interfaces:**
- Consumes: `NAV_ITEMS` from `src/config/navigation.ts` (Task 2)
- Produces: `<Footer />` with `id="contact"` â€” the anchor target for the Header's "Contact" link â€” consumed by `LandingPage.tsx` in Task 13

- [ ] **Step 1: Create the Footer**

`src/components/landing/Footer.tsx`
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
              <img src="/assets/icons/logo.svg" alt="" width={28} height={28} />
              <span className="fw-semibold fs-5">ApexBooking</span>
            </div>
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

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 13: Compose the Landing Page, wire the route, remove template boilerplate

**Files:**
- Create: `src/pages/LandingPage.tsx`
- Modify: `src/App.tsx` (full rewrite)
- Delete: `src/App.css`
- Delete: `src/assets/react.svg`
- Delete: `src/assets/vite.svg`
- Delete: `src/assets/hero.png`
- Delete: `public/icons.svg`

**Interfaces:**
- Consumes: `Header`, `HeroSection`, `BusinessSection`, `FeaturesSection`, `DashboardPreviewSection`, `PricingSection`, `HowItWorksSection`, `CallToActionSection`, `Footer` (Tasks 3-12)
- Produces: the complete `/` route

- [ ] **Step 1: Create the Landing Page**

`src/pages/LandingPage.tsx`
```tsx
import { Header } from '../components/landing/Header'
import { HeroSection } from '../components/landing/HeroSection'
import { BusinessSection } from '../components/landing/BusinessSection'
import { FeaturesSection } from '../components/landing/FeaturesSection'
import { DashboardPreviewSection } from '../components/landing/DashboardPreviewSection'
import { PricingSection } from '../components/landing/PricingSection'
import { HowItWorksSection } from '../components/landing/HowItWorksSection'
import { CallToActionSection } from '../components/landing/CallToActionSection'
import { Footer } from '../components/landing/Footer'

export function LandingPage() {
  return (
    <>
      <Header />
      <main>
        <HeroSection />
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

- [ ] **Step 2: Rewrite `App.tsx` to route to the Landing Page**

```tsx
import { Routes, Route } from 'react-router-dom'
import { LandingPage } from './pages/LandingPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
    </Routes>
  )
}

export default App
```

- [ ] **Step 3: Delete unused template files**

Delete `src/App.css` (its import was removed from `App.tsx` in Step 2, above), `src/assets/react.svg`, `src/assets/vite.svg`, `src/assets/hero.png`, and `public/icons.svg` â€” they belonged to the Vite template demo content that `App.tsx` no longer renders.

- [ ] **Step 4: Typecheck, lint, and build**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

Run: `npm run build`
Expected: build succeeds (missing `/assets/icons/...` and `/assets/images/...` files are fine â€” they're plain `src` string paths, not imports, so nothing fails to resolve).

- [ ] **Step 5: Manual browser verification**

Run: `npm run dev`, open the printed local URL.

Check:
- Header stays sticky on scroll; hamburger menu opens/closes the nav on a narrow viewport (resize below ~992px width).
- Home/Features/Pricing/Contact links scroll to the matching section.
- All nine sections render in order: Header, Hero, Businesses We Support (8 cards), Booking Features (8 cards, SMS shows "Coming Soon"), Dashboard Preview, Pricing (2 plans, Professional marked Recommended), How It Works (4 steps), Call To Action, Footer.
- Resize the viewport through mobile (~375px), tablet (~768px), laptop (~1280px), and wide (~1920px) â€” no horizontal scrollbar, no overlapping text, cards reflow correctly at each breakpoint.
- No console errors (missing icon/image 404s are expected and fine at this stage).

---

### Task 14: Create PROJECT_TRACKER.md

**Files:**
- Create: `PROJECT_TRACKER.md`

**Interfaces:**
- Consumes: nothing
- Produces: the project's ongoing status tracker, referenced by future feature prompts per `Claude/AI_ROLE_&_Core_Principles.md`

- [ ] **Step 1: Create the tracker**

`PROJECT_TRACKER.md`
```markdown
# ApexBooking Project Tracker

## Current Scope

Only the Booking module is being developed. Future modules (Customer
Management, Staff Management, Inventory, Sales, Purchasing, Reports) are
out of scope until explicitly assigned.

## Booking Module

| Feature | Status | Notes |
|---|---|---|
| Landing Page | Complete | Static marketing page at `/`. Sections: Header, Hero, Businesses We Support, Booking Features, Dashboard Preview, Pricing, How It Works, Call To Action, Footer. PWA install button wired via `useInstallPrompt`. No auth, no API integration, no other routes. |

## Known Follow-Ups (not started, not in current scope)

- Icon/image assets referenced by the Landing Page (`/assets/icons/**`,
  `/assets/images/**`, `pwa-192x192.png`, `pwa-512x512.png`) do not exist
  yet and need to be added by hand â€” the app builds and runs without them,
  the browser just shows a broken image / default icon until they're
  supplied.
- `/login` and `/request-access` are referenced as future destinations by
  Landing Page buttons but are not implemented; the buttons are currently
  inert.
```

- [ ] **Step 2: Verify**

Run: `npm run build`
Expected: still succeeds (this task only adds a markdown file, no code changes).

Confirm `PROJECT_TRACKER.md` renders correctly (open it in the editor).
