# Step 1 — Shared Design-System Adjustments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Do not execute this plan until explicitly told to.** It is written and ready; the user has not
> yet approved implementation.

**Goal:** Close the token-level gaps identified against the reference images (larger card radius,
a trend-indicator pattern, a thin capacity/progress pattern, consistent minimum form-control sizing)
while leaving every token/component that already matches the reference untouched — pure foundation
work, zero page-level visual change beyond the card-radius bump.

**Architecture:** All changes land in the existing shared layer — `src/styles/theme.css` (token +
utility-class additions, no new stylesheet) and one new presentational component,
`src/components/common/TrendPill.tsx`, built on tokens/classes that already exist
(`badge-tone-success`/`badge-tone-danger`/`badge-tone-neutral`) rather than inventing new colors.
Two new icon assets are added to the existing monochrome icon set. **No dashboard, table, or page
is modified to consume these** — `TrendPill` and the progress-bar utility ship unwired in this step
(see "Why nothing gets wired up" below).

**Tech Stack:** React 19 + TypeScript, Bootstrap 5.3.8 CSS-variable theming (same pattern already
used throughout `theme.css`), no new dependencies.

**Spec:** `docs/superpowers/plans/2026-08-14-protected-ui-consistency-roadmap.md` (§5 Visual
reference analysis, §7 Step 1 scope).

## Global Constraints

- No color/hex changes — every new rule reuses existing `--color-*` tokens or the existing
  `badge-tone-*` classes. Brand palette is locked per the user's explicit instruction.
- New icons follow the existing convention exactly: `viewBox="0 0 24 24"`, `width="24" height="24"`,
  `fill="none"`, `stroke="#475569"`, `stroke-width="1.75"`, `stroke-linecap="round"`,
  `stroke-linejoin="round"` (verified against `public/assets/icons/chevron-down.svg` and
  `check-circle.svg`).
- No new stylesheet — every addition goes into `src/styles/theme.css`, in a new dated section
  comment block, matching its existing section-header comment style
  (`/* ---- */` banner + explanatory comment, see e.g. `theme.css:840-843`).
- `--radius-lg` is NOT touched. It's shared by `.modal-content`, the `.rounded-3` utility, and
  public-page consumers (`styles/landing.css`, `styles/admin.css`'s `.admin-auth-card`,
  `SchedulePreviewCard.tsx`) — changing it would bleed into modals and public pages before their
  phases (Phase 5 / Phase 9). The card-radius bump instead gets its own token, `--radius-xl`,
  consumed only by `.card`.
- This repo has no automated test runner (`package.json` has no `test` script, no vitest/jest).
  Every task's verification is `npm run build` (runs `tsc -b`, i.e. a type-check + lint via `oxlint`
  is separate) plus a manual visual check via `npm run dev` — checked in **light mode, dark mode,
  and at least one non-default tenant palette** (e.g. add `data-palette="teal"` on `<html>` via
  devtools) since every new rule must hold up across both dimensions.
- Icon names resolve straight to `/assets/icons/{name}.svg` with no fallback (`Icon.tsx:10`) — a
  typo silently 404s as a broken image. Verify the exact filename after Task 3 before referencing it
  in Task 4.

---

## Token & component audit — confirmed unchanged (no task needed)

Covers plan items 1, 8, 9, 10 (partial), 13, 14 — checked against the reference images and found to
already match, so no code changes accompany these:

- **Colors** (`--color-primary/-strong/-soft`, `--color-accent`, `--color-success`, `--color-danger`,
  `--color-teal`, `--color-ink/body/muted/canvas/surface/border`) — exact match to the brief's
  described palette, confirmed locked, not modified by this plan.
- **Status badges** (`badge-tone-{neutral,primary,success,warning,danger,teal}`,
  `theme.css:476-504`) — same soft-pill-with-colored-text shape as the reference's "Success"/
  "Pending" pills. No token change; component consolidation (5 hardcoded badge components →
  1 shared `StatusBadge`) is Step 3, not Step 1.
- **Buttons** (`.btn`, `.btn-primary`, `.btn-outline-secondary`, `.btn-danger`, `.btn-icon`,
  `.action-icon-*`) — already restrained (soft-tint hover, no heavy borders/shadows), matches the
  reference's clean action style. **One flagged-but-deferred finding:** `.btn-icon` is 32px
  (`.btn-icon.btn-sm` is 28px) — both under the ~44px touch-target guideline. Not changed here
  because a global bump reflows every table's row-action spacing app-wide; this belongs with Step 5
  (table actions) and Step 2 (bottom-nav touch targets, which get their own sizing rule regardless).
  Flagged in the roadmap so it isn't lost.
- **Form controls** (`.form-control`, `.form-select`, `.form-check-input`) — border-radius, colors,
  focus ring, disabled/autofill handling already tokenized and correct. The one real gap
  (minimum touch-friendly height) is **not** "unchanged" — see Task 5.
- **Dark mode** (`[data-bs-theme='dark']`) — every existing token repaints correctly; nothing in this
  plan needs a dark-mode-specific override because every new rule is written in terms of existing
  tokens, which already have dark-mode values.
- **Tenant palettes** (`[data-palette='teal'|'rose'|'amber'|'forest'|'slate']`) — only override
  `--color-primary*`. Nothing added in this plan touches `--color-primary` directly except by
  reference (e.g. `.progress-thin .progress-bar` uses `var(--color-primary)`), so palette-switching
  continues to work with zero extra code.

## Why nothing gets wired up in this step

Checked every dashboard-facing data interface for a prior-period or capacity value to attach a real
`TrendPill`/progress bar to:

- `ITenantRevenue` (`interfaces/ITenantRevenue.ts`) — `onlineAmount`/`payInVisitAmount`/`total` only,
  no prior-period figure.
- `IStaffPerformanceEntry` — `servicesCompleted`/`revenueGenerated` only, no comparison.
- `IRefundLogEntry` — no aggregate/comparison at all, it's a log.
- `IPlatformDashboardSummary` (Super Admin) — point-in-time counts only.
- No interface anywhere in the app has a capacity/utilization/completion concept (`grep`-confirmed:
  zero matches for "capacity", "utilization", "completion" across `src/`).

Per the user's explicit instruction — no fake trends, no decorative progress bars — **`TrendPill`
and `.progress-thin` ship as ready-to-use, unwired primitives in this step.** Wiring either one into
a real dashboard (Step 6) requires a backend/DTO change to expose a real prior-period or
capacity value, which is out of this refactor's scope (no API contract changes). This is called out
explicitly so Step 6 doesn't get blocked by rediscovering it — the roadmap should note it as a
follow-up decision (add the backend field vs. leave dashboards without trend indicators) before
Step 6 starts.

---

## Task 1: `--radius-xl` token and card radius

**Files:**
- Modify: `src/styles/theme.css:30-32` (token block), `src/styles/theme.css:417-421` (`.card` rule)

**Interfaces:**
- Produces: `--radius-xl` custom property, consumed by `.card`'s `--bs-card-border-radius` /
  `--bs-card-inner-border-radius`. No component API changes — `Card.tsx` needs no edit, it already
  renders the plain `card` class Bootstrap/theme.css controls.

- [ ] **Step 1: Add the token**

In `theme.css`, immediately after the existing radius tokens:

```css
  --radius-sm: 0.5rem;
  --radius-md: 0.75rem;
  --radius-lg: 1rem;
  /* Reserved for primary content surfaces (Card) only — deliberately not reused by .modal-content,
     .rounded-3, or any public-page surface, so this stays a scoped, reversible bump instead of
     rerounding the whole app. See Step 1 plan, "why --radius-lg wasn't reused." */
  --radius-xl: 1.25rem;
```

- [ ] **Step 2: Point `.card` at it**

Change:

```css
.card {
  --bs-card-border-radius: var(--radius-md);
  --bs-card-inner-border-radius: var(--radius-md);
  --bs-card-bg: var(--color-surface);
}
```

to:

```css
.card {
  --bs-card-border-radius: var(--radius-xl);
  --bs-card-inner-border-radius: var(--radius-xl);
  --bs-card-bg: var(--color-surface);
}
```

- [ ] **Step 3: Verify**

Run `npm run build`. Then `npm run dev` and open any page using `<Card>` (e.g. the Services page) —
confirm cards render with a visibly larger, but not "playful," corner radius (20px vs. the old
12px), in both light and dark mode. Confirm `.modal-content` (open any modal) and any `.rounded-3`
usage (e.g. `EmptyState`'s icon circle uses inline `borderRadius`, unaffected — but check the
landing page hero, out of scope but shouldn't visibly break) are **unchanged**.

- [ ] **Step 4: Commit**

```bash
git add src/styles/theme.css
git commit -m "style: introduce --radius-xl and apply to Card surfaces"
```

---

## Task 2: Trend-pill CSS + `TrendPill` component

**Files:**
- Modify: `src/styles/theme.css` (new section, append near the badge-tone block, `theme.css:504`
  area)
- Create: `src/components/common/TrendPill.tsx`

**Interfaces:**
- Produces: `TrendPill` — `{ sentiment: 'positive' | 'negative' | 'neutral'; value: string; label?: string; icon?: 'up' | 'down' | 'none'; size?: 'sm' | 'md' }`. `sentiment` picks the color tone;
  `icon` picks the arrow direction — **kept independent** because "up" isn't always good (e.g. a
  rising cancellation rate is `sentiment="negative"` with `icon="up"`). Not consumed by any page in
  this plan (see "Why nothing gets wired up").

- [ ] **Step 1: Add the CSS**

Append to `theme.css`, after the `badge-tone-teal` rule (`theme.css:501-504`):

```css
/* ---------------------------------------------------------------------- */
/* Trend pill — "+8% from last week"-style comparison indicator            */
/* Layout only; color comes from the existing badge-tone-* classes so it   */
/* inherits dark mode and (harmlessly, since only success/danger/neutral   */
/* are used) tenant-palette switching for free.                            */
/* ---------------------------------------------------------------------- */

.trend-pill {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  line-height: 1.3;
}

.trend-pill-sm {
  padding: 0.1rem 0.4rem;
  font-size: 0.6875rem;
}

.trend-pill-icon {
  flex-shrink: 0;
}

/* A single "trending up" glyph, mirrored for the down state, instead of shipping two near-
   identical icon assets. */
.trend-pill-icon-down {
  transform: scaleY(-1);
}

.trend-pill-label {
  font-weight: 500;
  color: var(--color-muted);
}
```

- [ ] **Step 2: Write the component**

```tsx
import { Icon } from './Icon'

type TrendSentiment = 'positive' | 'negative' | 'neutral'
type TrendIcon = 'up' | 'down' | 'none'
type TrendPillSize = 'sm' | 'md'

const SENTIMENT_TONE_CLASS: Record<TrendSentiment, string> = {
  positive: 'badge-tone-success',
  negative: 'badge-tone-danger',
  neutral: 'badge-tone-neutral',
}

interface ITrendPillProps {
  /** Drives color — independent of `icon`, since a rising value isn't always good (e.g. cancellations). */
  sentiment: TrendSentiment
  /** Pre-formatted comparison value, e.g. "+8%", "-2%", "No change". */
  value: string
  /** e.g. "from last week". Rendered muted, after the value. */
  label?: string
  icon?: TrendIcon
  size?: TrendPillSize
}

export function TrendPill({ sentiment, value, label, icon = 'none', size = 'md' }: ITrendPillProps) {
  const classes = ['trend-pill', SENTIMENT_TONE_CLASS[sentiment], size === 'sm' ? 'trend-pill-sm' : '']
    .filter(Boolean)
    .join(' ')

  return (
    <span className={classes}>
      {icon !== 'none' && (
        <Icon name="trend-up" size={size === 'sm' ? 12 : 14} className={`trend-pill-icon${icon === 'down' ? ' trend-pill-icon-down' : ''}`} />
      )}
      <span>{value}</span>
      {label && <span className="trend-pill-label">{label}</span>}
    </span>
  )
}
```

- [ ] **Step 3: Verify**

Run `npm run build` (this task has no call site yet, so there's nothing to visually check in the
running app beyond confirming it compiles — a temporary render in `App.tsx` or React DevTools
console is acceptable for a one-off visual sanity check, but must be removed before commit, not left
as a demo call site per "Why nothing gets wired up").

- [ ] **Step 4: Commit**

```bash
git add src/styles/theme.css src/components/common/TrendPill.tsx
git commit -m "feat: add TrendPill component (unwired — no dashboard has comparison data yet)"
```

---

## Task 3: `trend-up` icon asset

**Files:**
- Create: `public/assets/icons/trend-up.svg`

- [ ] **Step 1: Add the file**

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <polyline points="3 17 9 11 13 15 21 7"/>
  <polyline points="14 7 21 7 21 14"/>
</svg>
```

- [ ] **Step 2: Verify**

Confirm the filename is exactly `trend-up.svg` (matches the `name="trend-up"` passed in Task 2 —
`Icon.tsx` has no fallback, a mismatch 404s silently). `npm run build`, then visually confirm via
the same temporary render used in Task 2's verification, in both light and dark mode (dark mode
inverts all `/assets/icons/` images via `theme.css:199-206`'s `filter: brightness(0) invert(1)`, so
this needs no separate dark variant).

- [ ] **Step 3: Commit**

```bash
git add public/assets/icons/trend-up.svg
git commit -m "feat: add trend-up icon asset"
```

---

## Task 4: `.progress-thin` utility

**Files:**
- Modify: `src/styles/theme.css` (new section, appended after Task 2's trend-pill block)

**Interfaces:**
- Produces: a CSS-only utility class, `.progress-thin`, applied to Bootstrap's own `.progress` /
  `.progress-bar` markup directly (`<div className="progress progress-thin"><div className="progress-bar" style={{ width: '58%' }} /></div>`) — deliberately **no React wrapper component**,
  since there is no real call site yet (see "Why nothing gets wired up") and a component with zero
  consumers would be exactly the "abstraction without a clear reuse case" the confirmed direction
  rules out. A component can be added once Step 6/8 has a real value to attach it to, if the
  call-site count justifies one then.

- [ ] **Step 1: Add the CSS**

```css
/* ---------------------------------------------------------------------- */
/* Thin capacity/progress indicator — Bootstrap .progress, restyled.       */
/* No React wrapper: apply directly (`<div class="progress progress-thin">`)  */
/* wherever a real fill/capacity value exists. Not used decoratively.      */
/* ---------------------------------------------------------------------- */

.progress-thin {
  --bs-progress-height: 0.375rem;
  --bs-progress-border-radius: 999px;
  --bs-progress-bg: var(--color-border);
}

.progress-thin .progress-bar {
  background-color: var(--color-primary);
  border-radius: 999px;
}
```

- [ ] **Step 2: Verify**

`npm run build`. Temporarily render `<div className="progress progress-thin"><div className="progress-bar" style={{ width: '58%' }} /></div>` anywhere to confirm the thin rounded bar renders
correctly in light/dark mode and picks up a non-default tenant palette's primary color (e.g.
`teal`), then remove the temporary render before committing.

- [ ] **Step 3: Commit**

```bash
git add src/styles/theme.css
git commit -m "style: add .progress-thin utility (unwired — no capacity data source exists yet)"
```

---

## Task 5: Global form-control minimum height

**Files:**
- Modify: `src/styles/theme.css:656-661` (currently `.modal-body .form-control, .modal-body .form-select`)

**Interfaces:**
- No component changes — every existing `<input className="form-control">` / `<select className="form-select">` in the app picks this up automatically, inside or outside a modal.

- [ ] **Step 1: Widen the rule's scope**

Change:

```css
/* Standard dashboard input scale inside modals. */
.modal-body .form-control,
.modal-body .form-select {
  min-height: 40px;
  padding: 0.5rem 0.75rem;
  font-size: 0.9375rem;
}
```

to:

```css
/* Standard dashboard input scale — was modal-only; every form-control/form-select in the app
   should meet the same minimum touch-friendly height, not just the ones inside a modal. */
.form-control,
.form-select {
  min-height: 40px;
  padding: 0.5rem 0.75rem;
  font-size: 0.9375rem;
}
```

(Delete the now-redundant `.modal-body`-scoped rule entirely rather than leaving both — the
unscoped version supersedes it.)

- [ ] **Step 2: Verify**

`npm run build`, then `npm run dev` and check a non-modal form page (`BookingSettingsPage` is the
reference implementation per the roadmap) — confirm `<select>`/`NumberInput` fields are now the same
height as their modal counterparts, and nothing that relies on a shorter field (e.g. a dense
inline table filter, if any exist — grep for `form-select`/`form-control` usage outside
`FormGroup`/modals first) visibly breaks. Check in both densities the app already has (page-level
forms and modal forms) to confirm they now match.

- [ ] **Step 3: Commit**

```bash
git add src/styles/theme.css
git commit -m "style: apply standard 40px form-control min-height app-wide, not just in modals"
```

---

## Task 6: Document the confirmed typography/spacing hierarchy

**Files:**
- Modify: `src/styles/theme.css` (comment-only addition, near the top-level "Base surfaces &
  typography" section, `theme.css:216-223`)

No visual or behavioral change — this task exists so the hierarchy the app already follows is
written down once, instead of staying tribal knowledge re-derived page by page in Steps 6–9.

- [ ] **Step 1: Add the documentation comment**

Insert directly above the `body { ... }` rule at `theme.css:220`:

```css
/* ---------------------------------------------------------------------- */
/* Confirmed typography & spacing hierarchy (documentation only — no new  */
/* utility classes; these are the existing Bootstrap utilities already in */
/* consistent use across the app, written down as the standard).         */
/*                                                                        */
/* Typography (Bootstrap fs-*/fw-* utilities):                           */
/*   Page title       -> fs-3 fw-bold        (PageHeader.tsx)            */
/*   Modal title       -> fs-5                (Modal.tsx)                */
/*   Card/section head -> fs-6 fw-semibold    (every dashboard/list card) */
/*   Supporting text   -> text-muted, often + small                      */
/*   Eyebrow/label     -> text-eyebrow (defined below) or                */
/*                        sidebar-section-label-style uppercase small     */
/*                                                                        */
/* Spacing:                                                               */
/*   Page padding      -> p-3 (mobile) / p-md-4 (desktop)  (DashboardLayout main) */
/*   Card grid gap      -> row g-3                                        */
/*   Section spacing    -> mb-3 (related group) / mb-4 (distinct section) */
/*   Card internal pad  -> Bootstrap's default .card-body (1.25rem)       */
/* ---------------------------------------------------------------------- */
```

- [ ] **Step 2: Verify**

`npm run build` (comment-only change, this only confirms no syntax error was introduced).

- [ ] **Step 3: Commit**

```bash
git add src/styles/theme.css
git commit -m "docs: document the confirmed typography and spacing hierarchy in theme.css"
```

---

## Self-review

**Spec coverage against the 14 points requested:**

1. Tokens unchanged → audit section above. 2. Tokens adjusted → Task 1 (radius), Task 5 (form
control height). 3. Card radius decision → Task 1, with rationale for the new token vs. reusing
`--radius-lg`. 4. Card padding/spacing → confirmed unchanged (audit section) + documented (Task 6).
5. Typography hierarchy → documented, Task 6. 6. Trend-pill → Task 2 + Task 3 (icon). 7. Progress-bar
utility → Task 4. 8. Status badge consistency → confirmed unchanged at the token level (component
consolidation is Step 3, out of this plan's scope). 9. Button/icon-button consistency → confirmed
unchanged, with the 44px touch-target gap explicitly flagged and deferred (not silently dropped).
10. Input/form-control consistency → Task 5. 11–12. Mobile/desktop spacing → documented, Task 6 (no
values changed — existing `p-3`/`p-md-4`/`g-3` scale already responsive). 13–14. Dark
mode/tenant-palette compatibility → every new rule (Tasks 1, 2, 4) is written in terms of existing
tokens/classes exclusively, so both are inherited for free; verification step in each task checks
both explicitly.

**Placeholder scan:** none found — every task has real, complete code, not a description of code.

**Type consistency:** `TrendPill`'s prop names (`sentiment`, `value`, `label`, `icon`, `size`) are
self-contained to this one new file; no cross-task signature reuse to verify.

---

## Requirements for Step 2 (documented now, not implemented)

Per the user's instruction: the bottom-navigation design requirements Step 2 must satisfy, written
down now so Step 2's own detailed plan starts from these rather than re-deriving them. **Nothing in
this section is implemented by this plan.**

- **Primary items must be capped, not all items shown.** Real role item counts from
  `config/navigation/booking.nav.config.ts` / `admin.nav.config.ts`: Owner = 11 items, Admin = 8,
  Staff = 4, Super Admin = 4. A 5-icon bar (4 destinations + 1 "More") matches the reference images
  and fits Staff/Super Admin exactly with no overflow; Owner/Admin need a per-role "most important
  4" selection (likely Dashboard + the role's top 2–3 daily-use sections) with everything else,
  including the already-separate `settings` section, collected behind "More."
- **"More" is a real destination, not a dead end** — opens a menu/sheet (the existing `MobileNav`
  offcanvas is a reasonable vehicle) listing every nav-config item not promoted to a primary slot,
  grouped the same way `Sidebar.tsx` already groups them (`scheduling` / `manage` / `settings`).
- **Role-specific navigation stays role-specific** — Tenant (Owner/Admin/Staff via
  `BOOKING_NAV_ITEMS`) and Super Admin (`ADMIN_NAV_ITEMS`) keep their own item sets through the same
  bar component, same pattern as today's `Sidebar`/`AdminSidebar` split, just unified into one
  component per §2 of the roadmap.
- **Active state** — needs a visible current-destination indicator (the reference uses a filled
  circle behind the active icon); should reuse the same "is this the active route" logic
  `SidebarNavItem`/`NavLink` already does (`isActive` from `react-router-dom`), not a new
  route-matching implementation.
- **Icon selection** — reuse each item's existing `icon` field from the nav config
  (`ISidebarNavItem.icon`/`IAdminNavItem.icon`) rather than picking new icons for the bar; the "More"
  trigger needs one new icon (a grid/dots glyph — check `public/assets/icons/` for an existing
  candidate before adding a new one, following Task 3's exact SVG convention if one must be added).
- **Safe-area spacing** — must account for `env(safe-area-inset-bottom)` on notched/home-indicator
  devices so the bar and its touch targets aren't obscured by the OS gesture area.
- **PWA viewport prerequisite** — `index.html:8`'s viewport meta is currently
  `width=device-width, initial-scale=1.0` with **no `viewport-fit=cover`**. Without it,
  `env(safe-area-inset-*)` resolves to `0` on iOS and the safe-area padding above does nothing —
  Step 2 needs to add `viewport-fit=cover` to that meta tag as a prerequisite, and re-verify nothing
  else on the page (which currently assumes no viewport insets) regresses because of it.
- **Accessibility** — the bar needs a `nav` landmark with an `aria-label` (e.g. "Primary", matching
  `Sidebar.tsx`'s existing `aria-label="Primary"` convention), `aria-current="page"` on the active
  item (mirroring what `NavLink`'s `isActive` already expresses), and correct tab/focus order;
  icon-only items need visible or `aria-label`-based text per the `Button` component's existing
  `iconOnly` contract.
- **Touch target sizing** — every bar item (including "More") needs a minimum ~44px hit area,
  independent of the icon's visual size — the same gap flagged for `.btn-icon` in this plan's audit
  section applies here and should be solved once, consistently, for both.
