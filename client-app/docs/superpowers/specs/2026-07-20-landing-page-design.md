# ApexBooking Landing Page â€” Design Spec

Status: Approved (source: user-authored feature prompt, treated as final spec)
Scope: Landing page only. No auth, no APIs, no dashboard, no future pages.

## Source of Truth

Content, copy, section order, and component names are defined by the feature
prompt supplied by the user (Header, Hero, Businesses We Support, Booking
Features, Dashboard Preview, Pricing, How It Works, PWA install, Call To
Action, Footer). This document records the technical decisions needed to
turn that content spec into code, and resolves the handful of things the
prompt left implicit.

Governing standards: `Claude/AI_ROLE_&_Core_Principles.md`,
`Claude/Technology_Stack.md`, `Claude/Progressive_Web_App_Standards.md`
(these three files together serve as `BASE_PROMPT.md`, which does not exist
as a standalone file in this project).

## Current State

Fresh `npm create vite@latest -- --template react-ts` scaffold.
`bootstrap`, `react-router-dom`, `axios`, `vite-plugin-pwa` are already
installed but unused. `vite.config.ts` has a placeholder PWA manifest
("My First PWA") that needs ApexBooking branding. `App.tsx` / `index.css` /
`App.css` are template boilerplate to be replaced; `index.css` currently
includes a dark-mode media query, which violates the light-theme-only
standard and will be removed.

## Resolved Ambiguities

1. **Header nav (Home / Features / Pricing / Contact)** â€” these are
   in-page section anchors (`#hero`, `#features`, `#pricing`, `#contact`),
   not routes, since everything lives on one page and future pages are
   out of scope.
2. **Login / Request Access buttons** â€” no auth or target pages exist yet,
   so these render as plain `<button>` elements with no navigation wired
   up (no-op for now), not `<Link>`s to routes that don't exist. This
   avoids implying pages that haven't been built.
3. **Icon/image asset references** â€” per the "never generate icons/images,
   never generate placeholder files" standards, JSX will reference asset
   paths as plain string literals served from `public/assets/icons/` and
   `public/assets/images/` (e.g. `src="/assets/icons/logo.svg"`), not ES
   `import` statements. Plain string `src` won't break the dev server or
   build if the file is missing yet (browser just 404s the image);
   an `import` of a nonexistent module would hard-fail the build. No
   files are created in those folders â€” they're populated later by hand.
4. **Bootstrap usage** â€” plain `bootstrap` CSS + JS bundle (already
   installed), imported globally. No `react-bootstrap` package, since
   adding it isn't explicitly requested and isn't installed.
5. **PWA manifest** â€” update the existing placeholder manifest in
   `vite.config.ts` to ApexBooking name/description/theme color. Manifest
   icon files (`pwa-192x192.png`, `pwa-512x512.png`) are referenced but
   not generated, same as other image assets.

## Component Breakdown

All under `src/components/landing/` unless noted, per the folder structure
in `Technology_Stack.md`:

- `Header.tsx` â€” sticky header, logo, in-page nav, Login/Request Access
- `HeroSection.tsx`
- `BusinessSection.tsx` â€” supported-industry cards
- `FeaturesSection.tsx` â€” booking feature cards (SMS = "Coming Soon" badge)
- `DashboardPreviewSection.tsx`
- `PricingSection.tsx` â€” Basic / Professional plans
- `HowItWorksSection.tsx` â€” 4-step process
- `CallToActionSection.tsx`
- `Footer.tsx`
- `components/pwa/InstallAppButton.tsx` + `hooks/useInstallPrompt.ts` â€”
  wraps the native `beforeinstallprompt` flow; hidden when unavailable,
  installed, or running in standalone display mode

`pages/LandingPage.tsx` composes all sections. `App.tsx` renders
`LandingPage` via a minimal `react-router-dom` route for `/` only (no other
routes are added).

Shared/reusable primitives introduced only if a pattern repeats 2+ times
within this page (e.g. a `Card` wrapper for the business/feature cards,
a `SectionHeading`), per the reusable-component-philosophy standard.

## Non-Goals

No API calls, no auth, no `/login` or `/request-access` pages, no
dashboard, no other business modules, no dark mode.

## Deliverable Tracking

`PROJECT_TRACKER.md` does not exist yet; it will be created with a
Booking-module-scoped status table and updated to mark the Landing Page
feature complete when this work finishes.
