# Landing Page Header — Login Link → Find Workspace — Design Spec

Status: Approved (source: brainstorming session)
Scope: `src/components/landing/Header.tsx` only. No new routes, no new
components, no changes to `FindWorkspacePage`, `AuthLayout`, or any other
landing section.

## Problem

Login moved from a single `/login` route to per-tenant `/:slug/login`,
reached via `/find-workspace` (email → slug lookup → redirect). `Header.tsx`
was never updated: both its desktop nav button and its mobile full-screen
menu link still point at `/login`, which has no matching route in
`AppRoutes.tsx` — both are currently dead links.

## Current State

- Desktop (`Header.tsx:78`): `<Button to="/login" variant="outline-primary">Login</Button>`.
- Mobile full-screen menu (`Header.tsx:122-130`): a raw `<a href="/login">`
  styled `mobile-nav-menu__link mobile-nav-menu__link--muted`, alongside
  `Link`/anchor nav items and the `Get Started` CTA at the bottom.
- `/find-workspace` (`FindWorkspacePage.tsx`) already exists and is fully
  wired: collects an email, resolves it to a tenant slug via
  `authService.findWorkspace`, and navigates to `/${slug}/login` on success.
- `Button` (`components/common/Button.tsx`) already supports an optional
  `icon` prop that renders an `Icon` (`public/assets/icons/{name}.svg`)
  before the label. `search.svg` exists in that set.

## Change

Both links are repointed from `/login` to `/find-workspace` and relabeled
from "Login" to "Find Workspace", matching `FindWorkspacePage`'s own
heading ("Find Your Workspace") so the terminology is consistent across the
click-through.

1. **Desktop button:**
   ```tsx
   <Button to="/find-workspace" variant="outline-primary" icon="search">
     Find Workspace
   </Button>
   ```
   Same position/variant as today (right side of the desktop nav, before
   the "Request Access" CTA) — only the destination, label, and icon change.

2. **Mobile full-screen menu link:** becomes a React Router `Link` (replacing
   the raw `<a href>`, for consistency with how internal links are handled
   elsewhere in the app) pointing at `/find-workspace`, label "Find
   Workspace", same `mobile-nav-menu__link mobile-nav-menu__link--muted`
   classes and position in the link list. **No icon here** — `search.svg`'s
   stroke color is a fixed `#475569` (not `currentColor`), so as an `<img>`
   it can't inherit this menu's white text color; against the dark navy
   (`--color-ink`) full-screen background it would read as muted/low-contrast
   compared to the plain-text sibling links (`Pricing`, `How it Works`,
   etc.). Text-only keeps it visually consistent with those.

## Non-Goals

No changes to `/find-workspace`'s own page, `AppRoutes.tsx`, `AuthLayout`,
`authService`, or any other Header content (logo, nav items, scroll
behavior, mobile menu open/close mechanics, `InstallAppButton`, "Request
Access" CTA). No new icon added to `public/assets/icons/` — reuses existing
`search.svg`.

## Component/File Summary

Modified files:
- `src/components/landing/Header.tsx` (desktop button + mobile menu link
  only)
