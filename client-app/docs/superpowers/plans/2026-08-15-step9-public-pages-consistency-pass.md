# Step 9 — Public-Pages Consistency Pass — Inspection & Proposed Plan

> **Status: inspection only. No code written, nothing implemented.** Per the request, this stops
> after the inspection with a proposed scope — implementation waits for explicit approval, and two
> items below are flagged as needing a decision, not silently resolved.

**Goal (once approved):** Bring the public-facing routes toward the same design-token discipline
Steps 1–8 already established for the protected app, **without** forcing protected-app components
onto public UI whose structure or interaction model genuinely differs, and without touching the
public booking wizard's own intentional `.pb-root` visual identity beyond real, evidence-based
gaps.

**Spec:** `docs/superpowers/plans/2026-08-14-protected-ui-consistency-roadmap.md` §7 (Deferred)
Public pages: *"Will fold `landing.css`/`auth.css`/`requestAccess.css`/`publicBooking.css` toward
the shared token layer where appropriate while preserving their intentionally distinct
public-facing identity."*

---

## 1. Complete public route inventory (from `AppRoutes.tsx`)

| Route | Component | Layout used |
|---|---|---|
| `/` | `LandingPage` | none (own sections) |
| `/pwa-init` | `PwaInit` | none (bare spinner) |
| `/find-workspace`, `/:slug/login` | `FindWorkspacePage` (same component) | `AuthLayout` |
| `/:slug/reset-password` | `ResetPasswordPage` | `AuthLayout` |
| `/request-access` | `RequestAccessPage` | own shell (`.request-access-shell`) |
| `/request-access/pending` | `RequestAccessPendingPage` | own shell (`.request-access-shell`) |
| `/:slug/book` | `PublicBookingPage` | `PublicBookingLayout` (`.pb-root`) |
| `/:slug/cancel-booking` | `CancelBookingPage` | `PublicBookingLayout` (`.pb-root`) |
| `/:slug/refund-status` | `RefundStatusPage` | `PublicBookingLayout` (`.pb-root`) |
| `/superadmin/login` | `SuperAdminLoginPage` | `SuperAdminAuthLayout` (deliberately distinct dark shell) |

10 page components, 11 routes, 4 layout treatments. All confirmed outside
`ProtectedRoute`/`SuperAdminProtectedRoute`.

## 2. Public components inspected

Full read: `AuthLayout`, `SuperAdminAuthLayout`, `PublicBookingLayout`, `FindWorkspacePage`,
`ResetPasswordPage`, `SuperAdminLoginPage`, `RequestAccessPage`, `RequestAccessPendingPage`,
`BusinessTypeSelector`, `PlanSummaryCard`, `SchedulePreviewCard`, `LandingPage`, `PwaInit`,
`PublicBookingPage`, `CancelBookingPage`, `RefundStatusPage`, `EwalletSubmissionForm`.

Swept via targeted search (raw `card`/`badge`/`btn` patterns) rather than read in full, since the
goal was finding leftover raw markup, not a line-by-line review: every file under
`components/landing/`, every file under `components/publicBooking/`, plus
`ProgressTracker`/`FormSectionHeader`/`SlugValidationHint` (request-access) — no raw `card`/`badge`
markup found in any of them beyond what's reported below.

## 3. Existing design-system usage already in place

This matters as much as the gaps: public pages are **not** a blank slate.

- `FindWorkspacePage`, `ResetPasswordPage`, `SuperAdminLoginPage`, `RequestAccessPage` already use
  `FormGroup`, `Button`, `Icon`, `Reveal` extensively and correctly.
- `SchedulePreviewCard` already uses the shared `Badge` component (both its "4 booked" chip and its
  per-appointment status chips).
- `theme.css`'s design tokens (`--color-primary`, `--radius-*`, `--shadow-*`) are already the
  foundation `auth.css`/`requestAccess.css`/`landing.css` build on — e.g. `AuthLayout`'s
  `bg-gradient-brand` panel and `.stage-panel`/`.workspace-strip` transitions are custom layout, not
  color reinvention.
- The public booking wizard (`publicBooking.css`, `.pb-root`) is a **complete, deliberately separate
  design system** — its own token namespace (`--pb-accent`, `--pb-ink`, `--pb-danger`, etc., scoped
  under `.pb-root` specifically so it "never leaks into or fights the dashboard's own theme.css
  tokens" per its own file header), its own button classes (`.pb-btn-primary`, `.pb-btn-outline`,
  `.pb-btn-link`), its own typography (`.pb-display`, `.pb-mono`, `.pb-muted`). None of the 9 wizard
  step components import `Button`, `Modal`, `FormGroup`, or `Card` — confirmed by search, not
  assumption. This is architecture, not oversight.

## 4. Real inconsistencies found

### 4a. The four originally-deferred raw-`Card` usages — evaluated individually

| Component | Structure | Verdict |
|---|---|---|
| `RequestAccessPendingPage.tsx:31-32` | Passive content wrapper (intro text, stepper, list, CTA) — no special interaction on the wrapper itself | **Migrate to `Card`.** `<Card className="pending-card text-center" bodyClassName="p-4 p-md-5">` reproduces the current markup exactly — `Card`'s existing `className`/`bodyClassName` props already cover it. |
| `AuthLayout.tsx:52-54` | Passive content wrapper around whatever auth-stage `children` is passed | **Migrate to `Card`.** `<Card bodyClassName="p-4 p-md-5">{children}</Card>` — same reasoning, zero functional risk. |
| `BusinessTypeSelector.tsx:32-44` | A `<button role="radio" aria-checked>` — `Card` only ever renders a `<div>`. Structurally a selector chip, not a content container. | **Remain intentionally specialized.** Forcing this through `Card` isn't possible without changing what `Card` fundamentally renders, which would affect 40+ other call sites for one non-equivalent consumer. |
| `SchedulePreviewCard.tsx:25` | Passive container, but a deliberately **stronger** elevation (`shadow` not `shadow-sm`, plus `rounded-4`) for a landing-page hero mockup | **Remain intentionally specialized.** `Card`'s shadow is hardcoded to `shadow-sm`; naming both `shadow-sm` and `shadow` on one element creates a real CSS specificity conflict (both are `!important` in `theme.css`, whichever rule is later in source order wins — fragile, not clean). One call site wants this stronger variant; not enough to justify a `Card` size/elevation prop. |

**Net: 2 migrate, 2 remain specialized** — exactly matching "do not automatically migrate them,"
each with its own reasoning, not a blanket rule.

One related, not-required observation: `PlanSummaryCard.tsx:16`'s raw wrapper
(`plan-summary-card card border-0 shadow-sm`) happens to be classlist-identical to `Card`'s default
output, but its body has real interactive-accordion behavior (`role="button"`, `aria-expanded`,
`onClick`, `onKeyDown`) that `Card`'s `bodyClassName` (a string prop, not a passthrough) doesn't
support. Migrating it would mean extending `Card` for a single consumer — not proposed.

### 4b. Real, scoped inconsistencies beyond the original 4

1. **`ResetPasswordPage.tsx:132-136`'s submit-error alert is missing the `auth-error-banner` class**
   that its two siblings in the exact same layout use for the identical role (top-of-form submit
   error): `FindWorkspacePage.tsx:270-274` and `SuperAdminLoginPage.tsx:103-107` both render
   `<div className="alert alert-danger auth-error-banner" role="alert">`; `ResetPasswordPage` renders
   plain `<div className="alert alert-danger" role="alert">`. Same `AuthLayout` family, same visual
   role, one page simply missing a class. **Proposed fix:** add `auth-error-banner` to
   `ResetPasswordPage.tsx`'s alert.

2. **Every `.alert-danger`/`.alert-warning`/`.alert-success` in the app renders Bootstrap's stock
   colors, not the app's brand tokens** — `theme.css` re-themes `.btn`, `.table`, `.dropdown-menu`,
   `.pagination`, `.nav-tabs`, `.modal-content`, and more via Bootstrap's CSS-variable API, but never
   `.alert`. Confirmed by search: no `.alert` rule exists anywhere in `theme.css`. Real call sites
   using raw `.alert-*`: `ResetPasswordPage`, `FindWorkspacePage` (×1), `SuperAdminLoginPage` (×1),
   `CancelBookingPage` (×3: danger, warning, success), `RefundStatusPage` (×1 success),
   `EwalletSubmissionForm` (×1 danger) — 6+ real sites, all in the public/auth surface (protected
   pages use an inline `text-danger small` pattern instead, so this gap is effectively
   public-scoped in practice). **Proposed fix:** add the same Bootstrap-CSS-variable re-theming
   `theme.css` already uses everywhere else, for `.alert-danger`/`.alert-warning`/`.alert-success`
   (`--bs-alert-bg`, `--bs-alert-color`, `--bs-alert-border-color`), pointing at the existing
   `--color-danger-soft`/`--color-accent-soft`/`--color-success-soft` tokens — the exact established
   pattern, not a new one.

3. **`.pb-root` already defines a `--pb-danger`/`--pb-danger-soft` token pair and a ready-to-use
   `.pb-alert-danger` class (`publicBooking.css:421-427`) that literally zero components use.**
   `CancelBookingPage`'s and `EwalletSubmissionForm`'s danger alerts render plain `alert-danger`
   instead — meaning even after fix #2 above (shared brand color), the public booking wizard's own
   *more specific* danger identity (a distinct `#b3261e`, deliberately separate from the dashboard's
   `--color-danger`, matching how `--pb-accent` is deliberately separate from `--color-primary`)
   still goes unused. **Proposed fix:** apply the existing `pb-alert-danger` class to the danger
   alerts inside `.pb-root` pages (`CancelBookingPage`, `EwalletSubmissionForm`) — using what's
   already defined, not inventing anything.

4. **`EwalletSubmissionForm.tsx:59`'s submit button uses plain `btn btn-primary`, not
   `pb-btn-primary`.** Its sibling wizard steps (`ReviewStep.tsx:107`, `CustomerInfoStep.tsx:150`)
   both use `btn pb-btn-primary`. Since `theme.css`'s `.btn-primary` override points at
   `--color-primary` (dashboard indigo `#4f46e5`), this button currently renders the *dashboard's*
   indigo instead of the wizard's own accent color (`--pb-accent`, `#3454d1`) — a real, visible color
   mismatch within the public booking flow itself, not just a class-naming nitpick. **Proposed fix:**
   change the class to `pb-btn-primary`, matching this component's own established sibling
   convention exactly.

5. **Second raw-button-replaceable-by-`Button` case, mirroring Step 8's `CalendarPage` finding:**
   `CancelBookingPage.tsx:194-201`'s "Cancel My Booking" button is plain
   `btn btn-danger w-100`, manually toggling its own text for the loading state
   (`state === 'cancelling' ? 'Cancelling…' : 'Cancel My Booking'`) instead of using the shared
   `Button`'s built-in `isLoading` spinner — the same gap `Button.tsx` already solves for every other
   public auth-flow submit button. **Proposed fix:** `<Button variant="danger" fullWidth
   isLoading={state === 'cancelling'} onClick={handleCancel}>Cancel My Booking</Button>`. Two real
   sites now exist for "a public submit button reimplementing what `Button` already provides"
   (this one, plus Step 8's `CalendarPage` precedent), so this isn't a one-off exception to the
   pattern already established.

### 4c. Reviewed, no change proposed

- `HeroSection.tsx:25`'s `<a href="#features" className="btn btn-outline-primary btn-lg">` — a
  same-page hash-anchor scroll link. `Button`'s `to` prop always renders a router `<Link>`, not a
  plain anchor; forcing this through `Button` isn't a clean fit for a single hash-scroll CTA. Left
  as-is.
- The public booking wizard's own button/typography/summary-card classes (`.pb-btn-*`,
  `.pb-display`, `.pb-mono`, `.pb-summary-card` etc.) — confirmed intentional, internally consistent
  design system, not migration targets.
- `PlanSummaryCard`'s interactive accordion body (§4a) — no change proposed.
- Page-specific spacing/typography choices in `landing.css` (hero sections, pricing cards) — these
  are marketing-page layout decisions with no protected-app equivalent to compare against; reviewed,
  not proposed as findings, since there's nothing in the shared system for them to be inconsistent
  *with*.

## 5. A residual finding — outside Step 9's scope, reported per protocol

**`src/components/dashboard/StaffLineupTimeline.tsx:32`** (a **protected** dashboard component,
rendered on the Owner/Staff/Admin dashboards) uses `className="pb-mono small text-muted"` — the
exact same cross-domain bug Step 5 found and fixed in `RefundRequestTable.tsx`: `--pb-font-mono` is
only ever defined inside `.pb-root`, so this rule is a silent no-op outside it. This wasn't caught
in Step 5 because that search was scoped narrowly to the one file already under review, not
app-wide. This is a protected-app file — **not fixed here**, since this turn's constraints say "do
not change the protected application." Reporting it now because it surfaced during this inspection
and belongs on record. Recommend: fix in whichever future step touches dashboard components next
(or as a standalone one-line fix, given how small and low-risk it is — same treatment Step 5 already
gave the identical bug).

## 6. Deferred items confirmed (no action)

The 4 originally-named files are addressed individually in §4a (2 migrate, 2 stay). No other public
component needs deferring — everything else found either already uses the shared system correctly
or is genuinely public-specific by design.

## 7. Proposed Step 9 scope

1. Migrate `RequestAccessPendingPage.tsx` and `AuthLayout.tsx` onto `Card`.
2. Add `auth-error-banner` to `ResetPasswordPage.tsx`'s submit-error alert.
3. Add `.alert-danger`/`.alert-warning`/`.alert-success` Bootstrap-variable re-theming to
   `theme.css`, using existing `--color-*-soft` tokens.
4. Apply the existing, currently-unused `.pb-alert-danger` class to `CancelBookingPage.tsx`'s and
   `EwalletSubmissionForm.tsx`'s danger alerts.
5. Fix `EwalletSubmissionForm.tsx`'s submit button class (`btn-primary` → `pb-btn-primary`).
6. Migrate `CancelBookingPage.tsx`'s cancel button to the shared `Button` component.

Everything else inspected is either already compliant or intentionally left alone, per §4c.

## 8. Files expected to change (if approved)

- `src/pages/RequestAccessPendingPage.tsx`
- `src/layouts/AuthLayout.tsx`
- `src/pages/ResetPasswordPage.tsx`
- `src/styles/theme.css` (new `.alert-*` section, same pattern as every other Bootstrap re-theme rule)
- `src/pages/public/CancelBookingPage.tsx`
- `src/components/refunds/EwalletSubmissionForm.tsx`

## 9. Files expected to remain untouched

`SuperAdminAuthLayout.tsx`, `PublicBookingLayout.tsx`, `BusinessTypeSelector.tsx`,
`PlanSummaryCard.tsx`, `SchedulePreviewCard.tsx`, `LandingPage.tsx` and every `components/landing/*`
file, `RequestAccessPage.tsx`, `RefundStatusPage.tsx` (no danger alert on this page — its only alert
is `alert-success`, covered by the theme.css fix, no `.pb-*` equivalent exists or is proposed for
success), every `components/publicBooking/*` wizard step, `PwaInit.tsx`. Also confirmed untouched:
every protected-app file (`StaffLineupTimeline.tsx`'s finding in §5 is reported, not fixed, here).

## 10. New components

None. Every proposed change is a class swap, a small CSS addition following an already-established
pattern, or a migration onto an already-existing component (`Card`, `Button`). No new abstraction
meets or needs the 2-call-site bar to be created from scratch.

## 11. CSS changes

- `theme.css`: one new section, `.alert-danger`/`.alert-warning`/`.alert-success` Bootstrap-variable
  overrides (~10-15 lines, mirroring the existing `.btn-danger`/`.table` pattern exactly).
- No changes to `landing.css`, `requestAccess.css`, or `publicBooking.css` — item 4/5 above use
  classes that already exist in `publicBooking.css` unchanged; item 5 is a class-name swap in a
  component file, not a stylesheet edit.

## 12. Verification strategy

Same approach every step has used: `npm run build`, `npm run lint`, `npm run test:e2e`, plus
Playwright fixtures against the real stylesheet on a reachable public page (most of these routes —
`/find-workspace`, `/request-access/pending`, landing `/` — are themselves public and directly
navigable, unlike protected routes, so several of these can be verified by navigating to the real
page directly rather than only via fixture). Verify: `Card`-migrated pages render identically
(radius/shadow/padding unchanged), `.alert-*` colors now match brand tokens in light and dark mode
and at least the teal tenant palette (n/a for public pages generally, but dark mode does apply where
tested), no horizontal overflow at 320/375/430/768/1280px, `EwalletSubmissionForm`'s button renders
in `--pb-accent` not `--color-primary`, `CancelBookingPage`'s button shows a spinner while
`isLoading`.

## 13. Playwright coverage needed

- `RequestAccessPendingPage` and `AuthLayout`-based pages navigated directly (they're public — no
  auth-limitation workaround needed) to confirm the `Card`-migrated markup renders with the same
  radius/shadow/padding as before.
- A theme.css-level check that `.alert-danger`/`.alert-warning`/`.alert-success` resolve to the
  brand tokens (not Bootstrap defaults) via computed-style assertions, in light/dark mode.
- A fixture or direct navigation confirming `EwalletSubmissionForm`'s button renders `--pb-accent`,
  not `--color-primary`, when `pb-btn-primary` is applied inside `.pb-root`.
- `CancelBookingPage`'s cancel button reachable directly (public route, needs a valid cancellation
  token to reach the interactive state — the loading-spinner check may need a fixture instead if a
  real token isn't available for this environment; will confirm during implementation and report
  the limitation honestly if so, same as every protected-route limitation reported in Steps 1–8).

## 14. Decisions required from you

1. **§4b.3 — wire up the existing `.pb-alert-danger` class to `CancelBookingPage`/
   `EwalletSubmissionForm`'s danger alerts?** This uses tokens/CSS that already exist and are
   unused, so it's low-risk, but it does mean those two danger alerts will visibly change color
   (dashboard-generic red → the wizard's own `#b3261e`). Confirm this is wanted, or should danger
   alerts in `.pb-root` just inherit the theme.css-wide fix (§4b.2) instead and leave
   `.pb-alert-danger` as unused/dead CSS?
2. **No equivalent exists for `.pb-alert-warning`/`.pb-alert-success`** — `CancelBookingPage`'s
   "can't cancel online" warning and `RefundStatusPage`'s success confirmation would only get the
   generic theme.css branding (§4b.2), not a bespoke `.pb-root` treatment. Creating those would mean
   inventing new visual treatment for the public booking system, which is real design work beyond a
   consistency pass — flagging rather than deciding. Leave as a future-phase item, or is this wanted
   in Step 9?
3. **§5 — the `StaffLineupTimeline.tsx` protected-app bug.** Confirm this should wait for a future
   pass (matches this turn's "do not change the protected application" constraint) rather than be
   fixed as an aside now.

Waiting for approval before writing any code.
