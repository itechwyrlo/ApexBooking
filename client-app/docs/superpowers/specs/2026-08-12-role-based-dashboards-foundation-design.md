# Role-Based Dashboards — Foundation Design

## Context

The tenant dashboard (`/:slug/dashboard`) is currently a single static page (`BookingOverviewPage.tsx`) shown identically to every role. The business has specified three distinct dashboards with different reports and quick tools per role:

- **Tenant Dashboard (Owner)** — revenue, payout status, refund log, staff performance, personal lineup, Scan QR / Cancel & Refund tools.
- **Admin Dashboard** — master visual grid (multi-column per-staff schedule), daily booking counters, unassigned-bookings alert, Scan QR / Quick Walk-In / Reassign Barber / Collect Pay on Visit tools.
- **Staff Dashboard** — personal daily lineup timeline, client preference view, Block My Time / Save Chair Notes tools.

This is too large for a single implementation plan. It decomposes into four sub-projects, in dependency order:

1. **Foundation** (this spec) — split routing/layout so each role lands on its own page.
2. Staff Dashboard — build out its real reports/tools.
3. Admin Dashboard — build out its real reports/tools.
4. Owner Dashboard — build out its real reports/tools.

Sub-projects 2–4 each get their own spec + plan once foundation lands.

## Scope of this sub-project

Split the single shared dashboard page into three role-specific pages, wired up so the right one renders for the logged-in user. No real report data or working tool logic yet — every section is a labeled placeholder (`EmptyState`) naming what will eventually live there. This produces working, testable software on its own: navigating to `/:slug/dashboard` as an Owner/Admin/Staff test account shows a visibly different, correctly-labeled page per role.

No backend changes are required — role is already available client-side via the decoded JWT (`useAuth().user.roles`).

## Role precedence

`IUser.roles` is an array (a user can hold multiple roles on one membership — e.g., a solo operator might be Owner only, but the type allows more). When determining which dashboard to render, precedence is **Owner > Admin > Staff** — the highest-authority role a user holds wins. This is a simple ordered check, not a separate reusable utility, since nothing else in the app currently needs this precedence logic (`getDashboardPath` in `dashboardRoutes.ts` returns a single fixed path regardless of role and needs no change).

## Architecture

- **`src/pages/booking/BookingOverviewPage.tsx` is renamed to `src/pages/booking/DashboardPage.tsx`** and rewritten as a thin dispatcher: reads `useAuth().user.roles`, picks the highest-precedence role, and renders the matching page component. It replaces the current static content entirely (the existing hardcoded "Today's Appointments: 0" cards, empty-state calendar/team/status/activity cards, and the generic Quick Actions card are all superseded — none of that content was real data, and the new role-specific skeletons replace it).
- **Three new page components**, one per role, each following the existing page-file convention (a `PageHeader` plus a grid of section cards):
  - `src/pages/booking/OwnerDashboardPage.tsx`
  - `src/pages/booking/AdminDashboardPage.tsx`
  - `src/pages/booking/StaffDashboardPage.tsx`
- **`src/routes/AppRoutes.tsx`** — update the one import and the index route element from `BookingOverviewPage` to `DashboardPage`.

## Page content (skeleton stage)

Each section uses the existing `EmptyState` component (icon + title + description), labeled per the spec so it's clear what's coming. Quick Tools render as a row of disabled buttons (existing button styling, `disabled` prop, no click handler) — not wired to the already-existing Scan QR / Quick Walk-In components yet, since wiring those in is part of each dashboard's own later sub-project, not foundation.

**OwnerDashboardPage** — sections: Total Shop Revenue, Online Payout Status, Refund Log, Staff Performance List, My Personal Lineup. Quick Tools: Scan Booking QR, Cancel & Refund (both disabled placeholders for now).

**AdminDashboardPage** — sections: Master Visual Grid, Daily Booking Counters, Unassigned Bookings Alert. Quick Tools: Scan Booking QR, Quick Walk-In, Reassign Barber, Collect Pay on Visit (all disabled placeholders for now).

**StaffDashboardPage** — sections: My Daily Lineup Timeline, Client Preference View. Quick Tools: Block My Time, Save Chair Notes (disabled placeholders for now).

"My Personal Lineup" on the Owner page is conditional per the spec ("if the owner has bookings directly assigned to their user ID") — at skeleton stage it's shown unconditionally as a placeholder; the real conditional-visibility logic is added when Owner Dashboard's real data is wired in (sub-project 4).

## Testing

LocalFlow has no test runner configured (`package.json` has no `test`/`vitest`/`jest` script). Verification for this sub-project is manual: run the dev server, log in as (or otherwise simulate) an Owner, Admin, and Staff account, and confirm each lands on its own correctly-labeled skeleton page at `/:slug/dashboard`.

## Out of scope (deferred to later sub-projects)

- Any real backend query for revenue, payout status, refund log, staff performance, booking counters, or unassigned bookings.
- The "unassigned bookings" concept itself does not exist in the domain yet (`Booking.StaffId` is required, non-nullable) — this needs its own design decision when Admin Dashboard's sub-project starts.
- Wiring the already-existing Scan QR (`AdmitScanModal`) and Quick Walk-In (`NewWalkInModal`) components into the new dashboards.
- New backend/frontend work for: staff reassignment on an existing booking, collect-pay-on-visit action, block-time tool, chair/client notes (none of these exist today).
