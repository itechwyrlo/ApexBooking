# Protected-App UI Consistency Refactor — Inspection & Roadmap

> **Status:** Inspection complete, reference images received, direction confirmed by the user. No
> code has been modified — this is still a strategic roadmap, not an executable task plan. Each
> phase below gets its own detailed implementation plan (written with `superpowers:writing-plans`,
> bite-sized/TDD-style) immediately before that phase starts, per the Scope Check rule for
> multi-subsystem work. **Do not begin implementing any phase until explicitly told to.**
>
> **Confirmed decision:** mobile navigation moves from today's hamburger-opens-offcanvas-sidebar to
> a persistent bottom tab bar (see §6). This reshapes Phase 2 (unified shell) and is now locked in.

**Repo:** ApexBooking (`package.json` name: `apexbooking`) — React 19 + TS + Vite + Bootstrap 5.3.8, PWA.

**Constraints carried into every phase below (from the request):** Bootstrap 5.3 stays the
foundation (no framework swap). No new CSS-per-page files; extend the existing shared
token/component layer instead. Preserve existing brand colors (see Design Tokens below) — no new
palette. Public pages (landing, auth, request-access, public booking) are explicitly **out of
scope** until Phase 9. No business logic, API, routing, auth/authz, or state management changes at
any point — visual/structural only.

---

## 1. Executive summary

The codebase is **further along than the brief assumes**. There is already a real design-token
layer (`src/styles/theme.css`), a real shared-component library
(`src/components/common/*`, 26 components), and most CRUD pages already compose from it correctly
(`Card`, `PageHeader`, `EmptyState`, `Badge`, `RowActions`, responsive `table-refined` /
`table-stack` / `MobileListCard` triplet). This isn't a "everything is inconsistent, start over"
situation — it's a "the system is good, adoption is incomplete, and a few areas evolved a second,
parallel pattern instead of reusing the first" situation. The roadmap below is written around that:
extend and finish the existing system rather than replace it.

The concrete gaps are narrower than the brief implies:

1. **Dashboards are low-density by design history, not by accident.** `OwnerDashboardPage`,
   `AdminDashboardPage`, `StaffDashboardPage` were built as an intentional Phase-1 "skeleton" (see
   `docs/superpowers/plans/2026-08-12-role-based-dashboards-foundation.md`) — one `Card` + one
   metric/list per section, stacked full-width in their own `row g-3 mb-3`. Real data has since been
   wired into that skeleton, but no density/hierarchy pass ever followed. This is exactly the "too
   much empty space" complaint in the brief, and it's the single best-defined target for Phase 6.
2. **Super Admin is a parallel universe, not a themed variant.** `SuperAdminLayout` +
   `AdminSidebar` + `AdminTopbar` (`src/layouts/SuperAdminLayout.tsx`,
   `src/components/admin/AdminSidebar.tsx`, `src/components/admin/AdminTopbar.tsx`) duplicate
   `DashboardLayout` + `Sidebar` + `Topbar` structurally (same collapse-width logic, same
   `MobileNav`/`Footer` wiring, same `sidebar-link` CSS class) but as fully separate components
   with separate collapsed-state storage keys, instead of one shell parameterized by nav config +
   brand. `AdminSummaryCard` similarly duplicates what should be one shared stat-tile component.
3. **A handful of pre-system pages/components never got migrated.** e.g.
   `RequestAccessPendingPage.tsx:32` still hand-rolls `className="card-body p-4 p-md-5"` instead of
   `<Card>`. These are the exception, not the rule — worth a sweep, not a rewrite.
4. **Five page-scoped stylesheets exist** (`admin.css`, `auth.css`, `landing.css`,
   `publicBooking.css`, `requestAccess.css`, all imported globally in `main.tsx`), on top of
   `theme.css`. Of these, only `admin.css` (62 lines, Super Admin auth shell) touches a protected
   surface; the rest are public-page-only and out of scope until Phase 9. `admin.css` should fold
   into the shared layer or shrink once Phase 3 unifies the admin shell.

---

## 2. Inventory (by the 20 points requested)

### 1–5. Layout architecture, shared components, CSS files, Bootstrap usage, theme variables

- **Global stylesheet load order** (`src/main.tsx:4-12`): `bootstrap.min.css` → `index.css` (18
  lines, near-empty) → `theme.css` → `landing.css` → `requestAccess.css` → `auth.css` →
  `admin.css` → `publicBooking.css`. All six load on every route (no per-route code-splitting of
  CSS), so page-scoped files must stay page-scoped by naming convention, not by load boundary.
- **Design tokens** (`src/styles/theme.css:1-36`): `--color-ink/body/muted/canvas/surface/border`,
  `--color-primary(-strong/-soft/-soft-strong)`, `--color-accent(-soft)`, `--color-success(-soft)`,
  `--color-danger(-strong/-soft)`, `--color-teal(-strong/-soft)`, `--radius-sm/md/lg`,
  `--shadow-sm/md`. This is the exact palette described in the brief (indigo primary `#4f46e5`,
  amber accent `#f59e0b`, navy ink `#14163a`, teal `#0d9488` positive-adjacent, rose/red
  `#e11d48` destructive) — **already correct, do not introduce a new one.**
- **Tenant theming**: `[data-palette='teal'|'rose'|'amber'|'forest'|'slate']` overrides on `:root`
  let each tenant pick a brand primary (`theme.css:44-114`); a parallel set targets `.pb-root` for
  the public booking page's separate token namespace. **Any new component must key off
  `--color-primary` etc., never a hardcoded indigo hex**, or it'll ignore tenant branding.
- **Dark mode**: `[data-bs-theme='dark']` re-points every token (`theme.css:123-214`), gated by
  subscription plan (`DashboardLayout.tsx:34`, Basic plan forced light). Any new component must be
  checked in both themes.
- **Bootstrap re-theming**: `theme.css:261-782` overrides Bootstrap's CSS-variable API
  (`--bs-btn-*`, `--bs-table-*`, `--bs-dropdown-*`, `--bs-pagination-*`, `--bs-nav-tabs-*`, etc.) —
  the correct, maintainable way to reskin Bootstrap without fighting its specificity. This pattern
  should be the template for any new Bootstrap component that needs reskinning in later phases.
- **Layouts** (`src/layouts/`): `DashboardLayout` (tenant app shell), `SuperAdminLayout` (duplicate
  shell, see §1 finding 2), `SettingsLayout` (secondary sub-nav inside the dashboard shell —
  `src/layouts/SettingsLayout.tsx`, correctly reuses `PageHeader`/`sidebar-link`/`tabs-scroll`),
  `AuthLayout`, `SuperAdminAuthLayout`, `PublicBookingLayout` (public, out of scope for now).

### 6–13. Buttons, cards, tables, forms, modals, dropdowns, badges, dashboard components

All of these already have a shared implementation in `src/components/common/`:

| Concern | Component | Notes |
|---|---|---|
| Buttons | `Button.tsx` | variants map 1:1 to `theme.css` `.btn-*` overrides; `iconOnly` + `action-icon-*` tone classes drive `RowActions` |
| Cards | `Card.tsx` | fixed `card border-0 shadow-sm` + `--bs-card-border-radius: var(--radius-md)`; `hover` prop adds `card-hover` lift |
| Empty states | `EmptyState.tsx` | icon circle + title + description + optional CTA — used consistently in every table/list |
| Tables (desktop) | `.table-refined` (theme.css:844-873) | used via `<table className="table table-refined align-middle">` |
| Tables (mobile) | `MobileListCard.tsx` + `.table-stack` (theme.css:934-974) | **two competing mobile-table patterns exist**, see §3 finding below |
| Table loading | `TableSkeleton.tsx` | skeleton rows sized to column count |
| Row actions | `RowActions.tsx` | inline icon buttons under 3 actions, overflow to a dropdown at 3+ or when a delete action is present |
| Forms | `FormGroup.tsx`, `FormSection.tsx`, `NumberInput.tsx`, `PasswordInput.tsx`, `TimeSelect.tsx`, `MultiSelectCombobox.tsx`, `CollapsibleField.tsx` | consistent `fieldset` + `.mb-3` label/error pattern (see `BookingSettingsPage.tsx` as a clean example) |
| Modals | `Modal.tsx` | shared open/close transition, scroll-aware header border, `.modal-form-actions` footer convention |
| Tabs | `.nav-tabs` override + `ModalTabs.tsx` (in-modal) + `.tab-link`/`.tabs-scroll` (page-level, e.g. `SettingsLayout`) | two different tab visual systems for two different contexts — intentional, not a conflict |
| Badges/status | `Badge.tsx` + `badge-tone-{neutral,primary,success,warning,danger,teal}` (theme.css:476-504) | generic tones; **but** `BookingStatusBadge`, `RequestStatusBadge`, `TeamRoleBadge`, `TeamStatusBadge`, `TimeOffStatusBadge` (5 separate components in `components/admin`, `components/team`) each hardcode their own status→tone mapping rather than sharing one status-badge factory |
| Pagination | `Pagination.tsx` + `.pagination` override | consistent, includes a mobile-specific `space-between` layout at `<576px` |
| Dashboard stat tiles | `AdminSummaryCard.tsx` (Super Admin only) | tenant dashboards (`OwnerDashboardPage` etc.) don't use a stat-tile component at all — they print one big number per full-width `Card`, no compact multi-stat row anywhere in the tenant app |

### 14–16. Navigation/sidebar, responsive behavior, mobile-specific behavior

- **Sidebar**: `Sidebar.tsx` (tenant) and `AdminSidebar.tsx` (super admin) are structurally
  identical — nav-config-driven list of `SidebarNavItem`/`NavLink` using the shared `.sidebar-link`
  class — but are two separate components with two separate collapse-state `localStorage` keys
  (`apexbooking.sidebar.collapsed` vs `apexbooking.adminSidebar.collapsed`).
- **Topbar**: `Topbar.tsx` (tenant, has dark-mode toggle + notification bell + PWA install button +
  public-page link) vs `AdminTopbar.tsx` (not yet read in detail, but instantiated with a strict
  subset of props — no dark mode, no notifications). Same duplication pattern as the sidebar.
- **Mobile nav**: `MobileNav.tsx` is a single shared offcanvas wrapper already used by both shells —
  this one *is* correctly shared.
- **Responsive breakpoint conventions actually in use**: `d-md-none` / `d-none d-md-block` for
  table↔card swaps (768px), `d-lg-none` / `d-none d-lg-block` for sidebar↔offcanvas (992px). These
  are consistent across the files inspected — a good existing convention to standardize as a rule
  going forward rather than replace.
- **PWA-specific**: `components/pwa/` (not yet enumerated in depth) + `InstallAppButton` wired into
  `Topbar` only, not `AdminTopbar` — reasonable since Super Admin isn't the installable surface, but
  confirms the two shells aren't just reskins of one component.

### 17–20. Duplicated patterns, what should become shared, what should stay page-specific

**Should become shared (candidates for Phase 2/3):**
- One `AppShell` layout (sidebar + topbar + mobile nav + footer + collapse state), parameterized by
  nav config, brand block, and an optional slot for shell-specific topbar actions (dark mode toggle,
  notifications) — collapses `DashboardLayout`+`Sidebar`+`Topbar` and
  `SuperAdminLayout`+`AdminSidebar`+`AdminTopbar` into one implementation.
- One `StatTile`/`StatCard` component generalizing `AdminSummaryCard`, for use in both Super Admin
  and tenant dashboards.
- One status-badge factory/hook (`useStatusBadge(status, domain)` or similar) instead of five
  separate hardcoded tone-mapping components.
- Pick **one** mobile-table pattern (`table-stack` CSS-only stacking vs `MobileListCard` component)
  as the standard and migrate the other's call sites — right now both exist for the same job (see
  `ServiceTable.tsx` using `MobileListCard`; need to confirm which tables use `table-stack`
  instead during Phase 4 discovery).

**Should stay page/domain-specific (do not force into shared components):**
- Domain tables themselves (`BookingTable`, `TeamMemberTable`, `CustomerTable`, `ServiceTable`,
  etc.) — they should *compose* the shared table shell/skeleton/empty-state/row-actions, but each
  has genuinely different columns and belongs in its own domain folder (`components/appointments`,
  `components/team`, etc.), not in `common`.
- Domain-specific modals (`BlockMyTimeModal`, `CollectPaymentModal`, `EditTeamMemberModal`, ...) —
  correctly already built on top of the shared `Modal`, no restructuring needed, just consistency
  passes (Phase 5).
- `publicBooking.css`, `landing.css`, `requestAccess.css`, `auth.css` — page-family-scoped by
  design (public marketing/booking surfaces have their own visual language, `.pb-root` token
  namespace, etc.) and explicitly out of scope until Phase 9.

---

## 3. Confirmed inconsistencies (concrete, file-referenced)

1. **Duplicate app shell**: `DashboardLayout.tsx` vs `SuperAdminLayout.tsx` — near-identical JSX
   (same `d-flex min-vh-100`, same `aside` collapse-width inline style, same `MobileNav`/`Footer`
   wiring), separate `Sidebar`/`AdminSidebar` and `Topbar`/`AdminTopbar` components, separate
   `localStorage` keys for the same UI state.
2. **Duplicate stat-tile concept**: `AdminSummaryCard.tsx` exists only for Super Admin; tenant
   dashboards never got an equivalent, so they fall back to one-metric-per-full-width-card, which is
   the direct cause of the "too much empty space" complaint.
3. **Five separate status-badge components** (`BookingStatusBadge`, `RequestStatusBadge`,
   `TeamRoleBadge`, `TeamStatusBadge`, `TimeOffStatusBadge`) each reimplementing a
   status-string → `badge-tone-*` switch, instead of one shared mapping utility/component.
4. **Two competing responsive-table patterns** (`table-stack` CSS stacking vs `MobileListCard`
   component) doing the same job for different tables — needs an inventory pass in Phase 4 to
   decide which tables use which today and standardize on one.
5. **Pre-system leftovers**: at least `RequestAccessPendingPage.tsx:32` bypasses `<Card>` with a
   raw `card-body p-4 p-md-5`. Likely a few more exist outside `pages/booking`/`pages/admin` — worth
   a grep sweep at the start of Phase 2 rather than assuming this list is exhaustive.
6. **`admin.css` is a protected-surface exception** inside an otherwise public-page-only stylesheet
   set — it styles the Super Admin *auth* shell (`.admin-auth-shell`, `.admin-auth-card*`), which is
   protected-adjacent (pre-auth but admin-only) and deliberately "stark, distinct from tenant auth"
   per its own header comment. Decide explicitly in Phase 3 whether this stays a deliberate
   exception or gets pulled toward the shared token system.

---

## 4. What's already solid — extend, don't rebuild

- Token layer (`theme.css`), tenant palette system, dark mode, Bootstrap CSS-variable re-theming
  pattern.
- `Card`, `Button`, `Badge`, `PageHeader`, `EmptyState`, `Modal`, `RowActions`, `Pagination`,
  `FormGroup`/`FormSection`, `Skeleton`/`TableSkeleton` — all well-designed, consistently adopted
  across the CRUD pages actually inspected (`ServicesPage`/`ServiceTable`, `BookingSettingsPage`).
- The `d-md-none`/`d-lg-none` responsive convention for table↔card and sidebar↔offcanvas swaps.
- The `action-icon-{edit,delete,view,primary}` 4-tone system for row actions — a genuinely good,
  already-consistent pattern worth documenting as a rule rather than changing.

---

## 5. Visual reference analysis (images received)

Two reference images were provided: a dashboard/stat-tile composition, and a 3-screen promo
(dashboard, appointments list, customer detail) captioned "Barbershop Admin Panel." Treated as
**pattern reference, not a literal spec** — per the user's explicit direction, ApexBooking's own
brand colors (`theme.css`) are not being replaced, and the goal is matching the reference's clarity
and restraint, not its exact template. Patterns extracted below, each mapped against what already
exists in `theme.css`/`components/common` so Phase 1/2 change only what's actually missing.

| Pattern in the reference | Current ApexBooking state | Verdict |
|---|---|---|
| Small pill status badges, soft tint bg (green "Success", amber "Pending") | `Badge.tsx` + `badge-tone-success`/`badge-tone-warning` (`theme.css:486-494`) — same visual shape already | **No change** — this is already the target look; just needs consistent adoption (Phase 3) |
| Stat tile: icon chip (rounded-square, soft tint) + label + large value + trend pill (arrow + "+8% from last week", green/red) | No equivalent — `AdminSummaryCard.tsx` has a circular icon chip + label + value but **no trend indicator**, and tenant dashboards have no stat-tile component at all | **New spec**: this is the concrete shape the Phase-2 `StatTile` should take — icon chip, value, and a small trend-pill sub-component (reusable wherever a metric needs a "vs. last period" delta) |
| Card corner radius — visually ~20–24px on primary cards | `--radius-lg: 1rem` (16px) is the largest existing token | **Token gap** — Phase 1 should evaluate a slightly larger radius for primary/stat cards (either raise `--radius-lg` or add one more step); confirm against the images at real size before deciding, don't guess a number now |
| Card internal padding — generous, ~20–24px | `Card.tsx` uses Bootstrap's default `.card-body` padding (`1.25rem` = 20px) | **No change** — already matches |
| Thin horizontal/vertical progress bars (Retention/Productivity %, schedule-fill gauge) | No equivalent exists anywhere in the app | **New spec**, small: a restyled Bootstrap `.progress` (thin, rounded, brand-primary fill) — only where a literal fill/capacity value already exists in data (e.g., "6 of 8 appointment slots filled"), not decorative |
| Donut/ring chart for category breakdown (service popularity) | No chart components exist in the app today | **Conditional** — only build if a real page has a genuine category-share decision behind it (the user's own "what decision does this help" test). Flag as a Phase 6/Reports candidate, not a default |
| List row: avatar + 2-line text (name / service) + trailing status badge + time | `MobileListCard.tsx` is close but generic (label/value stat grid, not this avatar+badge+time shape) | **Refinement**, not a new component — this is the shape `BookingTable`/appointment list rows should take on mobile in Phase 4, likely as a variant or sibling of `MobileListCard` rather than a rebuild |
| Detail panel: hero header + overlapping summary card, pill CTA buttons (soft-tint), line-item list, emphasized total row | `BookingDetailPanel.tsx`/`SidePanel.tsx` already cover the side-panel structure; pill soft-tint buttons and an emphasized total row are new | **Refinement**, lower priority — relevant to Phase 8 (protected-page consistency) for booking/customer detail views, not urgent for Phase 1–2 |
| Persistent bottom tab bar (mobile), dark pill, floating active-state circle | `MobileNav.tsx` is an offcanvas sidebar, opened via a hamburger button in `Topbar` — no bottom bar exists | **Confirmed new pattern** (§ decision above) — Phase 2 scope, see §6 below |
| Soft blue-gray panel background behind grouped card clusters | `--color-canvas: #f7f7fb` is a near-white neutral | **Treat as presentation framing, not a token change** — the images stage phone/card mockups on a colored backdrop for the marketing shot itself; ApexBooking's own page canvas should stay close to its current near-white tone unless a specific section (e.g. dashboard hero) calls for a subtle tint. Don't chase this literally. |

**Net effect on Phase 1:** small, targeted — a possible radius bump for primary cards, a new
trend-pill sub-pattern, and a thin-progress-bar utility. Everything else the images show either
already exists in `theme.css`/`components/common` or is a compositional change (Phases 2–8), not a
token change.

## 6. Confirmed decision — mobile navigation

Mobile primary navigation moves to a **persistent bottom tab bar**, replacing the current
hamburger → offcanvas-sidebar pattern on mobile. Scope, confirmed with the user:

- Bottom bar carries the sidebar's top-level nav-config sections as icons (per role — tenant vs.
  Super Admin keep their own item sets, same as today's `Sidebar`/`AdminSidebar` split, per
  `BOOKING_NAV_ITEMS`/`ADMIN_NAV_ITEMS`).
- Items that don't fit the bar (overflow + Settings) collect behind a "More" or profile entry —
  exact item selection is a Phase 2 detailed-plan decision, not decided here.
- Desktop/tablet keep the existing collapsible `Sidebar`; only the **mobile** (`<992px`, matching
  today's `d-lg-none` breakpoint) experience changes.
- `MobileNav.tsx`'s offcanvas may still be reused for secondary/overflow content (the "More" menu),
  just not as the primary nav surface anymore.
- This is UI/navigation-control only — no route changes, no new pages, no permission changes. It
  changes how existing nav-config entries are rendered on small screens, not what they link to.

## 7. Phased roadmap

Each phase produces working, reviewable software on its own and gets its own detailed
`writing-plans`-style task breakdown immediately before it starts (not now). This order supersedes
the original 10-phase sketch above (§5's numbering) — it's the user-confirmed implementation
sequence, which merges/reorders around the actual dependency chain (design system → shell →
shared primitives → dashboard → page-by-page rollout → QA). Public-route redesign stays explicitly
deferred (final step, out of scope until then).

**Restraint principles that apply across every phase below** (from the confirmed direction — not
per-phase boilerplate, genuinely load-bearing):
- No chart/graph is added unless it answers a specific decision the user needs to make; a number,
  comparison, list, ranking, or status summary wins by default.
- Empty states stay proportional to their surrounding content — never a large empty container
  compensating for missing data (`EmptyState.tsx` already renders compact; the discipline is in
  *how it's used*, not the component itself).
- Every interactive control gets an explicit hierarchy — primary / secondary / tertiary /
  destructive — and mobile shows fewer, not smaller, actions (secondary items move into an overflow
  menu rather than shrinking in place).
- Prefer extending an existing component over adding a new one; a new shared component needs a
  real second call site, not just the one page that inspired it.

### Step 1 — Shared design-system adjustments (only where required)
**Scope:** Apply §5's token deltas: evaluate the card-radius bump for primary/stat cards, add the
trend-pill sub-pattern (icon + delta % + "vs. last period" text, green/red), add a thin restyled
`.progress` utility for real fill/capacity values. No palette change (locked, see header). No
change to any token that already matches the reference (most of `theme.css`, see the §5 table).
**Exit criteria:** `theme.css` additions documented inline (mirroring its existing comment style),
zero visual change to any page that doesn't yet consume the new tokens — this step is additive
groundwork, not a repaint.

### Step 2 — Unified application shell
**Scope:** One `AppShell` (nav config + role-specific items + topbar config + content slot,
per the conceptual shape in the user's message) replacing `DashboardLayout`+`Sidebar`+`Topbar` and
`SuperAdminLayout`+`AdminSidebar`+`AdminTopbar`. Tenant and Super Admin keep their own nav-config
item sets (`BOOKING_NAV_ITEMS` vs `ADMIN_NAV_ITEMS`) through the same shell, not identical
navigation. Includes the confirmed bottom-tab-bar mobile nav (§6) replacing the offcanvas-as-primary
pattern on `<992px`; desktop/tablet keep the collapsible sidebar. Resolves the `admin.css`
auth-shell question (§3.6) as part of the same pass. Single collapse-state storage strategy.
**Exit criteria:** tenant dashboard and Super Admin both render through the same shell component;
mobile shows the new bottom bar; desktop/tablet collapse behavior unchanged; no route, permission,
or auth changes.

### Step 3 — Status badge consolidation
**Scope:** One shared `StatusBadge` (tone, icon, label, size) replacing the five hardcoded
components (`BookingStatusBadge`, `RequestStatusBadge`, `TeamRoleBadge`, `TeamStatusBadge`,
`TimeOffStatusBadge`). Domain components may still exist where they hold real domain logic (e.g.
mapping a specific enum to a tone), but delegate rendering to the shared component instead of
duplicating the pill markup — per the user's "don't lose domain labels, don't over-abstract" framing.
**Exit criteria:** one visual implementation for every status pill in the app; each domain
status-badge file (if kept) is a thin tone-mapping wrapper, not a markup duplicate.

### Step 4 — Mobile table consolidation
**Scope:** Decide between `table-stack` (CSS-only stacking) and `MobileListCard` (component) as
the single mobile-table standard, informed by §5's "list row" pattern (avatar + 2-line text +
trailing badge/time) — likely `MobileListCard` extended with that row shape, since it's already a
component with room to grow variants, versus CSS-only stacking which can't express an avatar or a
trailing badge cleanly. Migrate every table using the losing pattern.
**Exit criteria:** every data table in the protected app renders one desktop shape (`table-refined`)
and one mobile shape, chosen for record scanability (name/status/metadata/actions priority) over
literal column-preservation — no horizontal-scroll-as-primary-strategy anywhere.

### Step 5 — Table action system
**Scope:** Apply the confirmed action hierarchy to every table: compact desktop actions
(`[View] [Edit] [More]` or icon-only where meaning is obvious — `RowActions.tsx` already implements
the icon-vs-dropdown-overflow logic, this step is about *which* actions surface inline vs. overflow,
not rebuilding the component), collapsing to `[View] [More]` on mobile with secondary actions inside
the overflow menu. Icon-only actions keep accessible labels (already required by `Button`'s
`iconOnly`/`aria-label` contract); tooltips only where the icon alone is ambiguous.
**Exit criteria:** every table's row actions follow the same inline-vs-overflow rule at every
breakpoint; no table shows more than 2–3 inline actions on mobile.

### Step 6 — Dashboard redesign
**Scope:** The highest-impact step for the "too much empty space" finding (§1.1). Presentation-only:
same data sources as today, no new backend data, no new metrics, no calculation changes, no DTO
changes, no API changes. Confirmed process, per the user's explicit "existing data first" direction —
**for each dashboard, before touching it**: (1) inspect the existing page/component, (2) identify the
data it already receives, (3) identify the existing DTO/model, (4) identify how the values are
already calculated (server-side; nothing here recomputes them client-side), (5) identify what's
currently displayed, (6) only then decide whether the same information reads more clearly in the new
system. If a genuinely better presentation needs a field the DTO doesn't have, **stop and report the
requirement instead of silently adding it.**

**Pre-work already done against this checklist** (see §9 for the full per-page table): there is no
separate "Reports" page anywhere in the app today — grep-confirmed, `pages/booking/` has no
`ReportsPage`; every "report" hit outside the dashboards is landing-page marketing copy. All existing
reporting lives inside the four dashboard pages and their hooks
(`useTenantBookingCounts`, `useTenantRevenue`, `useRefundLog`, `useStaffPerformance`, `useIdleStaff`,
`useActiveStaff`, `useDashboardSummary`). There is also no chart/graph library in `package.json` and
no chart component anywhere — `MasterVisualGrid` is a per-staff schedule list, not a graph. So the
"graph rule" (§9) has nothing existing to evaluate for removal; it's a forward-looking constraint on
any new component this step considers, not a decision about anything already built.

**Restructure target** (Bootstrap grid, no new components beyond Step 1's primitives): page header →
compact key-metrics row (reusing/generalizing the KPI-row pattern that `AdminDashboardPage`'s "Daily
Booking Counters" and `SuperAdminDashboardPage`'s `AdminSummaryCard` grid *already* use today — extend
that pattern to `OwnerDashboardPage`, don't invent a new one) → operational info → appointments/
schedule → business activity → additional info. Trend pills (Step 1) attach only where real
prior-period data exists — per Step 1's audit, **none of today's dashboard DTOs carry one**, so no
dashboard gets a trend pill in this step unless that's separately resolved as a backend follow-up
(flagged, not decided, in Step 1's plan).
**Exit criteria:** all four dashboards read as dense, grouped, scannable overviews at every
breakpoint using only existing data; no full-width single-metric cards remain unless a metric
genuinely has no natural pairing; every reorganized value traces back to a field the page already
fetched before this step started.

### Step 7 — Shared component migration — ✅ complete (inspection only, no code changed)
**Scope:** Sweep for and fix pre-system leftovers identified in §3.5 (`RequestAccessPendingPage.tsx`
and any others found during the sweep) — first confirm each has no unique behavior worth preserving
distinctly, then migrate its presentation onto the shared primitives (`Card`, etc.) without changing
behavior.

**Outcome:** A full sweep (raw `card`/`badge` markup bypassing `Card`/`Badge`, raw `<table>` not on
`table-stack`, duplicated `BadgeTone`-shaped types) found **zero remaining pre-system leftovers
inside the protected application** — Steps 3 (`ServiceTable`/`BranchTable`/`CustomerBookingsModal`/
`RefundRequestTable`), 4 (`MobileListCard` retirement), 5 (`RowActions` consolidation), and 6
(`AdminSummaryCard` → `StatTile`) already found and fixed every protected-scope instance
incidentally as they went. The only remaining raw-`card` usages in the whole codebase are four
public-facing files — `RequestAccessPendingPage.tsx`, `AuthLayout.tsx`,
`BusinessTypeSelector.tsx`, `SchedulePreviewCard.tsx` — all correctly out of scope until Phase 9
(Public pages) per this roadmap's own phasing, and are **not** touched by this step. No code was
changed; no speculative diff was produced to manufacture activity.
**Exit criteria:** no protected-route component hand-rolls markup that a shared primitive already
covers. **Met**, confirmed by inspection rather than by a code change, since none was needed.

### Step 8 — Protected-page consistency pass
**Scope:** Apply the now-finished system (Steps 1–7) to every remaining protected page:
`AppointmentsPage`, `CalendarPage`, `StaffPage`, `ClientsPage`, `ServicesPage`, `TimeOffsPage`,
`BusinessProfilePage`, `RefundRequestsPage`, `BranchesPage` (omitted from this list when the
roadmap was first drafted; added when Step 8's plan caught the gap — already fully compliant, no
code change needed), settings pages (`BookingSettingsPage`, `AppearanceSettingsPage`,
`PaymentSettingsPage`), and any reporting surface. One page at a time, using shared components
rather than redesigning each independently — if two pages need the same pattern, they use the same
component.
**Exit criteria:** every tenant-side protected page uses `PageHeader`, `Card`, and the Step 1–5
primitives with no bespoke layout code left.

### Step 9 — Super Admin consistency pass
**Scope:** Same treatment as Step 8, scoped to `AdminBookingsPage`, `FailedNotificationsPage`,
`TenantRequestManagementPage`, `SuperAdminDashboardPage` (dashboard itself already covered in Step
6) — now running through the Step 2 unified shell.
**Exit criteria:** Super Admin pages are visually indistinguishable in *system* (not branding) from
the tenant app — same components, same spacing, same action hierarchy.

### Step 10 — Responsive QA
**Scope:** Full pass across every protected page at 320/375/390/430/768/1024/1280/1440px. Check for
accidental horizontal scroll, touch target sizing, desktop-table-squeezed-into-mobile, oversized
mobile cards, dark mode + every tenant palette variant, and no regressions to business logic,
routing, auth, or API contracts.
**Exit criteria:** sign-off checklist per page against the reference images and this document's
restraint principles.

### (Deferred) Public pages
Explicitly out of scope until every step above is done, per the confirmed direction. Will fold
`landing.css`/`auth.css`/`requestAccess.css`/`publicBooking.css` toward the shared token layer where
appropriate while preserving their intentionally distinct public-facing identity — scoped in detail
when this step starts.

---

## 8. Existing dashboard/reporting data inventory (Step 6 pre-work)

Confirmed direction: existing report data, calculations, DTOs, and APIs are the source of truth and
are not touched by this refactor — only how the same values are arranged and styled changes. Table
below is the "existing data first" checklist (data received → DTO → what's shown today) applied to
every dashboard, so Step 6 starts from confirmed facts rather than re-deriving them.

| Page | Data source(s) (hook → DTO) | Currently displayed as | Reorg opportunity (existing data only) |
|---|---|---|---|
| `OwnerDashboardPage` ("Business Overview") | `useTenantRevenue` → `ITenantRevenue { onlineAmount, payInVisitAmount, total, currencyCode }` | One full-width card, one big number + a sentence of online/pay-on-visit breakdown | Compact 3-value row (Total / Online / Pay on Visit) — the exact "KPI row instead of prose" pattern the user described, using fields already fetched |
| ″ | `useRefundLog` → `IRefundLogEntry[]` | Unordered list: reference, method, amount, date | Already list-shaped; tighten row density per Step 4's list-row pattern, no structural change needed |
| ″ | `useStaffPerformance` → `IStaffPerformanceEntry[] { name, servicesCompleted, revenueGenerated }` | Unordered list in hook-return order (not sorted) | Natural ranked list — sort by `revenueGenerated` (or `servicesCompleted`) client-side for display only; no new field, just an existing-data presentation choice to confirm with the user before Step 6 |
| ″ | `useTenantBookings` (self, today) → `ITenantBooking[]` | `StaffLineupTimeline` — chronological list | Already list-shaped, no change needed |
| `AdminDashboardPage` ("Front Desk") | `useTenantBookingCounts` → `ITenantBookingCounts { pending, checkedIn, completed, missed }` | **Already** a compact 4-value row inside one card (`row g-2 text-center`) | Closest existing page to the target pattern — Step 6 should generalize this into the shared KPI-row component, not redesign it |
| ″ | `useIdleStaff` → `IIdleStaffMember[]` | Simple list | No change needed |
| ″ | `useActiveStaff` + `useTenantBookings` (today) | `MasterVisualGrid` — horizontal per-staff schedule columns (not a chart) | Legitimate operational visualization per the graph rule — keep, just tighten density/spacing |
| `StaffDashboardPage` ("My Day") | `useTenantBookings` (self, today) | `StaffLineupTimeline` — list | No change needed |
| ″ | `useCustomerLatestNote` | One customer's note, prose | Inherently narrative, not a metric — this page has little to summarize numerically; its "empty space" problem is full-width single-column stacking, not missing KPIs. Fix is grouping/pairing cards side-by-side on tablet+, not new metric rows |
| `SuperAdminDashboardPage` ("Platform Overview") | `useDashboardSummary` → `IPlatformDashboardSummary { totalTenants, activeTenants, pendingRequests, approvedRequests, rejectedRequests, bookingsToday, bookingsThisMonth }` | **Already** a compact `AdminSummaryCard` grid (`col-6 col-lg-3`, two rows of metrics) | Closest existing page to the target pattern, and the direct precedent for Step 1's `StatTile`. **One concrete finding: `approvedRequests` is fetched but never rendered anywhere** — a real, already-available field that could complete the existing KPI grid without adding anything new. Flagging for confirmation before Step 6, not deciding unilaterally. |
| ″ | `useTenantRequests` → `TenantRequestTable` | Already a compact shared table + "View All" link | No change needed |

**Organizational note surfaced in passing (relevant to Step 3, not Step 6):** `BookingStatusBadge`
lives in `components/admin/` but is consumed by tenant-facing dashboard components
(`StaffLineupTimeline`, and transitively every dashboard) — a file-location mismatch worth folding
into Step 3's status-badge consolidation, not a Step 6 concern.

**Confirmed by the user (not yet implemented — Step 6 hasn't started):**

1. **Staff Performance ranking** — the existing `IStaffPerformanceEntry[]` list may be sorted for
   display. The section label must name the explicit metric it's sorted by — e.g. "Staff Performance
   by Revenue" — never an ambiguous label like "Top Staff" without the criterion visible. If both
   `revenueGenerated` and `servicesCompleted` are worth showing, they stay as **two separate,
   clearly-labeled values per row** (or two separate lists) — never combined into one artificial
   composite score. No calculation, API, or DTO change; sort order is a display-only concern applied
   to data already returned by `useStaffPerformance`.
2. **Super Admin `approvedRequests`** — surface this already-fetched field
   (`useDashboardSummary` → `IPlatformDashboardSummary.approvedRequests`) in
   `SuperAdminDashboardPage`'s existing KPI/`AdminSummaryCard` grid, placed where it fits naturally
   alongside `pendingRequests`/`rejectedRequests`. No new API call, no duplicate rendering elsewhere,
   no change to what the field means or how it's calculated — purely adding one more card to a grid
   that already exists.

**General Step 6 rule (confirmed):** existing dashboard/reporting data, calculations, DTOs, and APIs
are the source of truth for the whole step, not just these two items — no new metrics, no new
endpoints, no new DTO fields, no calculation changes, no chart library, no graphs unless a *future*
requirement explicitly calls for one and existing data already supports it. Step 6 generalizes the
already-working `AdminDashboardPage`/`SuperAdminDashboardPage` KPI-row patterns as the foundation for
`OwnerDashboardPage` (turn `ITenantRevenue`'s three fields into discrete metrics, not prose inside one
card) — it does not invent a separate, unrelated dashboard system. `StaffDashboardPage` explicitly
does **not** get KPI rows forced onto it where the data doesn't support them; its fix is regrouping
the existing day-view content, not manufacturing metrics. Across all four: fewer full-width
single-metric cards, less empty space, related information grouped, reused shared components
(Step 1–5 primitives), and mobile layouts that stack the same information intentionally rather than
by accident.

**Still not started.** Per explicit instruction, Step 6 does not begin yet — this section records
confirmed decisions for when it does. Step 1 (`docs/superpowers/plans/2026-08-14-step1-design-system-adjustments.md`) remains the current and only implementation-ready task, itself still awaiting
go-ahead.

---

## 9. Where this stands

**Steps 1–7 are complete and approved:**

- **Step 1** — design tokens/spacing adjustments (`docs/superpowers/plans/2026-08-14-step1-design-system-adjustments.md`), plus the Playwright verification setup added alongside it.
- **Step 2** — unified `AppShell`, config-driven `Sidebar`/`Topbar`, mobile bottom navigation.
- **Step 3** — shared status-tone system (`formatStatusLabel`, `ActiveStatusBadge`, `RefundRequestStatusBadge`, exported `BadgeTone`), five domain status badges deduped onto it.
- **Step 4** — `table-stack` confirmed as the sole responsive table standard; `ServiceTable` migrated onto it; `MobileListCard` retired.
- **Step 5** — `RowActions` contextual hierarchy (1-2 actions direct, 3+ overflow, `forceMenu` opt-in for `RefundRequestTable`), mobile touch targets, "Actions" label hidden on mobile.
- **Step 6** — dashboard information architecture (`StatTile`, `AdminSummaryCard` retired), Owner/Staff/Super Admin dashboards restructured around existing data only; Admin Dashboard confirmed already-correct and left unchanged.
- **Step 7** — inspected, confirmed complete with **no code changes needed**: the protected app has no remaining pre-system leftovers (Steps 3–6 already caught every one incidentally). The roadmap's only named target, plus every other instance a fresh sweep found, are all public-facing files (`RequestAccessPendingPage.tsx`, `AuthLayout.tsx`, `BusinessTypeSelector.tsx`, `SchedulePreviewCard.tsx`) and stay deferred to Phase 9.

**Next: Step 8 — Protected-page consistency pass**, detailed plan being prepared at
`docs/superpowers/plans/2026-08-15-step8-protected-page-consistency-pass.md`. Not started.

Steps 9 (Super Admin consistency pass), 10 (Responsive QA), and the deferred Public pages phase
remain after that.
