# ApexBooking Landing Page & Nav Refactor — Design Spec

Status: Approved (source: user-supplied refactor prompt + brainstorming session,
this document is the resolved technical spec)
Scope: Rename (LocalFlow → ApexBooking, repo-wide) + landing page (`Header` +
all `LandingPage` sections). No auth, no API changes, no dashboard/admin page
work beyond the rename sweep and the single hero screenshot capture.

## Source of Truth

Content requirements (rename, nav behavior, section-by-section redesign,
animation rules, PWA requirements) come from the user-supplied refactor
prompt for this task. This document records the technical decisions needed
to turn that prompt into code, including a few points where it conflicts
with existing project docs — those are called out explicitly below rather
than silently overridden.

Governing standards: `Claude/AI_ROLE_&_Core_Principles.md`,
`Claude/Technology_Stack.md`, `Claude/Progressive_Web_App_Standards.md`,
`Claude/UI_UX.md`.

## Deviations From Existing Governing Docs

These were surfaced during brainstorming and confirmed with the user before
writing this spec. They are intentional, scoped exceptions — not blanket
overrides of the governing docs.

1. **Landing page visual identity vs. `UI_UX.md`.** `UI_UX.md` states the
   landing page must share the exact same design language as the
   authenticated app. This refactor keeps the app's existing indigo primary
   as the shared anchor, but adds a second brand hue (teal) and a distinct
   display type pairing (Manrope headlines) *specifically on the marketing
   surface*. The dashboard/admin app's tokens are untouched. This is a
   scoped divergence for the marketing surface only, not a full rebrand.
2. **Business verticals vs. `AI_ROLE_&_Core_Principles.md`.** That doc lists
   8 target verticals (Salon, Barbershop, Clinic, Hardware Store,
   Construction Supplier, Retail Store, Auto Repair Shop, Small
   Wholesaler) — the original multi-module platform vision. Since only the
   Booking module exists today and Hardware Store/Construction
   Supplier/Retail Store/Auto Repair Shop/Small Wholesaler aren't
   appointment-based businesses, the landing page's "Businesses We Support"
   section narrows to the 4 verticals booking software actually serves:
   Salon, Barbershop (Live), Clinic, Fitness (Coming Soon). `industries.ts`
   is scoped to `BusinessSection` only, so this doesn't touch any other
   part of the app.
3. **Icon/Image/Screenshot standards vs. `Progressive_Web_App_Standards.md`.**
   That doc says "Never generate icons," "Never generate illustrations,"
   "Never generate screenshots" — assume assets are manually created
   elsewhere. The user's refactor prompt explicitly asks to "generate the
   required assets for this refactor," and confirmed (this session) that
   the hero image should be a real captured screenshot rather than a coded
   mockup. This mirrors what already happened in the prior "Booking SaaS
   Visual Refactor" (PROJECT_TRACKER.md: "18 hand-authored... SVGs were
   added" as real files, not left as broken references), so there's direct
   precedent for treating "manually created" as "created by hand in this
   process," not "supplied by an external designer." New SVG icons follow
   the existing single-color stroke style in `public/assets/icons/`; the
   hero/dashboard-preview image is a real Playwright screenshot, not a
   fabricated mockup.

## Current-State Findings (pre-existing, not caused by this task)

- `src/assets/` is **empty**. `Header.tsx` imports `../../assets/favicon.png`
  and `industries.ts` imports 8 SVGs from `../assets/*.svg` — none exist on
  disk. The landing page currently fails to build/run. This refactor
  resolves it as a side effect (new logo asset, new industry icons) but
  it's flagged here since it wasn't introduced by this change.
- `public/favicon.svg` is the default Vite scaffold logo (purple gradient
  blob), not a real brand mark.
- `vite.config.ts`'s manifest references `pwa-192x192.png`,
  `pwa-512x512.png` — neither exists in `public/`. The PWA install has
  never actually worked with real icons.

## 1. Brand Identity

New tokens added to `src/styles/theme.css` (additive — existing
`--color-primary*` tokens are unchanged so the dashboard/admin app is
unaffected):

```css
--color-teal: #0d9488;
--color-teal-strong: #0f766e;
--color-teal-soft: rgba(13, 148, 136, 0.1);
```

`--color-primary` (`#4f46e5`) remains the shared indigo anchor and is what
`theme_color` in the PWA manifest uses. Teal is landing-page-scoped: used on
eyebrow labels, the mobile-menu CTA, the "Live" vertical badge, and hero/
feature accents.

Typography: Manrope (700/800) for landing-page headings via a new
`.font-display` utility class, loaded through a `<link>` in `index.html`
(Google Fonts, same mechanism as any other static asset — no build-time
font pipeline needed). Body copy keeps the existing Inter/system stack used
elsewhere in the app.

## 2. Rename

`LocalFlow` → `ApexBooking` repo-wide, including `PROJECT_TRACKER.md`,
`Claude/*.md`, and `docs/superpowers/specs|plans/*.md` (historical dated
records included, per explicit instruction). Covers: component copy,
`index.html` `<title>`/meta tags, `vite.config.ts` PWA manifest
`name`/`short_name`, `package.json` `name`, footer/copyright text, alt
text. Executed as a repo-wide case-sensitive search-and-replace pass,
followed by a manual read-through of landing components for copy that
paraphrases the brand name without using the literal string, then a final
grep to confirm zero remaining matches outside `node_modules`/`dist`.

## 3. Navigation Bar (`Header.tsx` rebuild)

Drops Bootstrap's `collapse`/`data-bs-toggle` mechanism (no animation
control, can't do the slide+fade or full-screen takeover) for a small
component-local state machine (`isMenuOpen`, `isScrolled`).

- **Sticky/scroll behavior:** `position: fixed`, transparent background on
  load. A 1px sentinel `<div>` is placed at the top of `HeroSection`; an
  `IntersectionObserver` watching it flips `isScrolled` when it leaves the
  viewport (~equivalent to 40px of scroll, without a magic-number scroll
  listener).
- **Mobile (<768px):** logo left, single hamburger right. Tapping it opens
  a full-screen takeover (`--color-ink` background) — nav links large and
  stacked (44px+ touch targets), CTA button pinned to the bottom via
  `margin-top: auto` in a flex column. 220ms slide+fade (`transform` +
  `opacity`, `ease-out`). Closes on: link click, backdrop/X tap, `Escape`
  key. Focus is trapped while open and returns to the hamburger button on
  close.
- **Desktop (≥768px):** logo left, inline links center, Login + primary CTA
  right — same as today, just re-styled.

## 4. Hero Section

Mobile-first stack order: eyebrow → headline → subtext → two full-width
stacked CTAs → trust line ("No setup fees. Live in minutes.") → screenshot
(below the fold on mobile).

Screenshot: captured via Playwright against the dev server's
`/app/booking` (Booking dashboard overview — the most complete authenticated
screen). Saved as a real PNG under `src/assets/hero-dashboard.png`, imported
normally. Wrapped in a new shared `BrowserFrame` component (plain CSS: three
dots + a fake URL pill — no image asset for the frame itself, so it scales
cleanly).

Idle float: CSS `@keyframes` `translateY` loop, 3.5s `ease-in-out`,
amplitude 10px on desktop, reduced to 4px under a `(max-width: 991.98px)`
override, and removed entirely under `prefers-reduced-motion: reduce`.

## 5. Problem Section (new)

New `src/components/landing/ProblemSection.tsx` + `src/config/problems.ts`
(4 entries: double bookings, no-shows, back-and-forth messaging, no
visibility into performance — icon, headline, one sentence each). Inserted
between `BusinessSection` and `FeaturesSection`. New icons follow the
existing single-color stroke style (`public/assets/icons/`). Layout: `row
row-cols-2 row-cols-lg-4 g-4` (2×2 mobile, 4-across desktop) — pure
Bootstrap grid, no custom CSS needed for the layout itself.

## 6. Business Verticals Section (`BusinessSection.tsx`)

`IIndustry` gains `status: 'live' | 'coming-soon'`. `industries.ts` trimmed
to 4 entries (see Deviations #2). Card renders the existing `Badge`
component: `tone="success"` + "Live" for Salon/Barbershop, `tone="neutral"`
+ "Coming Soon" for Clinic/Fitness. New Salon/Barbershop/Clinic/Fitness
icons generated to replace the currently-missing set.

## 7. Feature Grid (`FeaturesSection.tsx` → bento)

Consumes the existing `BOOKING_FEATURES` array unchanged — no data
reshaping. `IFeature` gains an optional `size?: 'large'`, set on
`online-booking`, `booking-calendar`, and `dashboard-reports` (already
1st/5th/6th in the array). Large cards get a small coded mini-illustration
(a cropped fake-UI snippet built the same way as `SchedulePreviewCard` —
markup, not a raster image) in place of the plain icon.

- **Mobile:** single column (`row-cols-1`); large cards get extra
  min-height via a `.feature-card--large` class so they read as visually
  bigger even while stacked.
- **Desktop:** CSS Grid (`display: grid; grid-template-columns: repeat(3,
  1fr)`) with `.feature-card--large { grid-row: span 2 }` for the bento
  effect. This one section uses hand-written CSS grid instead of Bootstrap
  columns since Bootstrap's grid can't express mixed row-spans — consistent
  with "prefer Bootstrap, custom CSS only where it falls short."

## 8. Dashboard Preview Section

`DashboardPreviewSection.tsx` reuses the same `BrowserFrame` component from
the hero (shared, not duplicated) around the existing `SchedulePreviewCard`,
widened. Existing bullet-list copy is kept as-is. Mobile: frame stacks above
the bullets. Desktop: `row` with frame/bullets as two columns.

## 9. How It Works (`HowItWorksSection.tsx`)

Circle-number badge (`rounded-circle bg-primary`) replaced with plain large
type: a new `.step-number` utility (`font-display`, large size, muted-ink
color) rendering `01`–`04` (zero-padded from `item.step`), no circle, no
icon. Same `HOW_IT_WORKS_STEPS` data, no config changes.

## 10. Scroll Animation System

One reusable hook: `src/hooks/useRevealOnScroll.ts` — wraps
`IntersectionObserver`, fires once per element (`unobserve` after first
intersection), returns a ref + boolean. One wrapper component,
`src/components/common/Reveal.tsx`, applies `.reveal` / `.reveal--visible`
classes around each section and around each card grid (grid children get a
`nth-child`-based stagger delay in CSS, steps of 100ms capped at 6 steps).

```css
.reveal { opacity: 0; transform: translateY(24px); transition: opacity 0.4s ease-out, transform 0.4s ease-out; }
.reveal--visible { opacity: 1; transform: translateY(0); }

@media (prefers-reduced-motion: reduce) {
  .reveal { transform: none; transition: opacity 0.4s ease-out; }
}
```

No parallax, no carousels, no 3D tilt — none exist today, none are added.

## 11. PWA Assets & Meta

- `vite.config.ts` manifest: `name`/`short_name` → "ApexBooking",
  `theme_color` stays `#4f46e5` (already correct, just needs the icons/name
  to actually match it instead of referencing missing/generic assets).
- New devDependency: **`@vite-pwa/assets-generator`** — the official
  companion tool to the already-installed `vite-plugin-pwa`, used to
  generate `pwa-192x192.png`, `pwa-512x512.png`, `apple-touch-icon.png`,
  and `favicon.svg`/`favicon.ico` from one new source logo mark (a simple
  geometric monogram, generated as part of this task per Deviations #3).
  This is the one new dependency this refactor introduces; justified
  because hand-rasterizing PNGs isn't otherwise possible and this is the
  standard, minimal tool for exactly this job.
- `public/favicon.svg` (currently the Vite scaffold blob) is replaced by
  the generated brand mark.

## Non-Goals

No changes to authentication, routing behavior, API integration, business
logic, or any authenticated-app page beyond the single hero screenshot
capture. No dark mode. No Framer Motion or other new animation dependency.
No changes to `IBooking`/other domain types. Pricing section content/copy
is unchanged (not mentioned in the refactor prompt) — only inherits the new
type/scroll-reveal treatment for consistency.

## Component/File Summary

New files:
- `src/components/landing/ProblemSection.tsx`
- `src/config/problems.ts`
- `src/components/common/BrowserFrame.tsx`
- `src/components/common/Reveal.tsx`
- `src/hooks/useRevealOnScroll.ts`
- `src/assets/hero-dashboard.png` (captured screenshot)
- New SVGs under `public/assets/icons/` (problem-section icons, vertical
  icons) and a new logo/monogram source asset
- Generated PWA icon set under `public/` (via `@vite-pwa/assets-generator`)

Modified files:
- `src/components/landing/Header.tsx` (full rebuild)
- `src/components/landing/HeroSection.tsx`
- `src/components/landing/BusinessSection.tsx`
- `src/components/landing/FeaturesSection.tsx`
- `src/components/landing/DashboardPreviewSection.tsx`
- `src/components/landing/HowItWorksSection.tsx`
- `src/components/landing/CallToActionSection.tsx`, `Footer.tsx`,
  `PricingSection.tsx` (rename + `Reveal` wrapper only)
- `src/pages/LandingPage.tsx` (inserts `ProblemSection`)
- `src/config/industries.ts`, `src/interfaces/IIndustry.ts`
- `src/config/features.ts`, `src/interfaces/IFeature.ts`
- `src/styles/theme.css`
- `vite.config.ts`, `index.html`, `package.json`
- `PROJECT_TRACKER.md`, `Claude/*.md`, `docs/superpowers/specs|plans/*.md`
  (rename only)
