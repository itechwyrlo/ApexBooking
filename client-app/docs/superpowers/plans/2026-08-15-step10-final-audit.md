# Step 10 — Final Responsive, Accessibility, and Visual Consistency Audit

> **Status: inspection only. No source, CSS, test, or documentation file has been modified.**
> Per the request, this stops after the audit and proposed scope — implementation waits for
> explicit approval.

**Goal:** Close out the Steps 1–9 refactor with a full cross-application sweep — responsive
behavior, accessibility, remaining duplication, shared-component coherence, dark mode/tenant
palette, the public booking system, Playwright coverage, build/lint configuration, and dead code —
and hand back a scoped, evidence-based proposal for what (if anything) Step 10 should change.

**Note on the roadmap's own numbering:** `docs/superpowers/plans/2026-08-14-protected-ui-consistency-roadmap.md` §7 still lists its original 10-phase sketch (Step 9 = "Super Admin
consistency pass", Step 10 = "Responsive QA", public pages as a separate deferred phase after
that). That sketch was superseded in practice: Step 8 became "Protected-page consistency pass"
(12 tenant-side pages), Step 9 became "Public Pages Consistency Pass" (completed, see
`2026-08-15-step9-public-pages-consistency-pass.md`), and this Step 10 is the broader final audit
described in your message, not the narrower "Responsive QA" the roadmap doc still names. The
roadmap doc's §9 "Where this stands" section is stale on this point. Not fixed here (documentation
is out of scope for this turn) — flagged as a documentation-only note, not a code finding.

One consequence of that renumbering: **no step ever explicitly named the three Super Admin pages
outside the dashboard** (`AdminBookingsPage`, `FailedNotificationsPage`,
`TenantRequestManagementPage`) the way Step 8's plan named its 12 tenant pages. This audit closes
that gap directly — see §A and §D.

---

## A. Audit coverage

**Read in full for this audit:**
- Roadmap + all three most recent step plans (`...roadmap.md`, `...step8-protected-page-consistency-pass.md`, `...step9-public-pages-consistency-pass.md`) for established decisions.
- Shared components: `AppShell`, `Sidebar`, `SidebarNavItem`, `BottomNav`, `Topbar`, `Modal`,
  `Button`, `Card`, `Badge`, `ActiveStatusBadge`, `RowActions`, `StatTile`, `EmptyState`,
  `FormGroup`, `Icon`, `NotificationBell`, `SidePanel` (partial), `InstallAppButton`.
- Super Admin pages never individually named in a prior step's plan: `AdminBookingsPage.tsx`,
  `FailedNotificationsPage.tsx`, `TenantRequestManagementPage.tsx`, `SuperAdminDashboardPage.tsx`,
  plus their table components `FailedOutboxMessageTable.tsx`, `TenantRequestTable.tsx`.
- `DashboardLayout.tsx`, `SuperAdminLayout.tsx` (confirms Step 2's shell unification actually
  landed as designed).
- `utils/bookingDisplayStatus.ts`, `MonthCalendarGrid.tsx` (checked the calendar-chip tone system
  isn't a second, undocumented status-mapping).
- `RefundReviewReminderBanner.tsx`, `InstallAppButton.tsx` (full read after grep flagged them).
- `theme.css` in full for the `.btn-icon`, `table-stack`, `badge-tone-*`, `.alert-*` sections;
  targeted reads of the dark-mode and tenant-palette token blocks.
- `publicBooking.css` — confirmed no dark-mode/`prefers-color-scheme` rule exists anywhere in it.
- `package.json`, `tsconfig.json`/`tsconfig.app.json`/`tsconfig.playwright.json`,
  `playwright.config.ts`, `.gitignore`, `.oxlintrc.json`.

**Swept via targeted `Grep` (not full reads — looking for leftover raw markup, not reviewing every
line):** every `.tsx` file under `src/` for raw `btn btn-*` classes, raw `card border-0`/`card-body`
markup, raw `badge rounded-pill` markup, `aria-current`, `aria-expanded`, inline hex colors in
`style={{}}`, `--color-warning-subtle`/other possibly-undefined CSS custom properties, `pb-mono`
usage, `icon-tooltip` usage, `MobileListCard`/`AdminSummaryCard`/`AdminSidebar`/`AdminTopbar` file
and CSS-class survival.

**Verified empirically, not just read:** re-ran the full Playwright suite's dev server to measure
real computed contrast ratios for `badge-tone-warning`/`.alert-warning` in light vs. dark mode
(§B.4) — composited the actual `rgba()` token against the real canvas background rather than
assuming from the source values.

**Not re-read line-by-line in this pass** (carried forward from Steps 7–9's own fresh inspections,
since "treat prior decisions as established unless the current code proves otherwise" and my own
fresh greps above reproduced their findings exactly, with zero new raw-markup hits beyond what they
already catalogued): the 12 Step 8 tenant pages' internals, the 9 public booking wizard step
components' internals, `landing.css`/`landing/*` components, `AppearanceSettingsPage`'s palette
swatches (Step 8 already reviewed and justified leaving it raw).

---

## B. Confirmed issues

### B.1 — `Topbar`'s account-menu trigger has no accessible name

**File:** `src/components/layout/Topbar.tsx:104-118`

**Current behavior:**
```tsx
<button type="button" className="btn btn-light d-flex align-items-center gap-1 border-0 px-2" data-bs-toggle="dropdown" aria-expanded="false">
  <span className="... " aria-hidden="true">{initial}</span>
  <Icon name="chevron-down" size={14} />
</button>
```
`Icon` renders `<img alt="" aria-hidden="true">` (decorative, by design). The avatar-initial span is
also `aria-hidden`. The button itself has no `aria-label`, no visible text, and no `title`.

**Why it's a problem:** this button has **zero accessible name** — a screen reader announces it as
an unlabeled "button," not "account menu" or the user's email. This is the exact
"icon-only button without an accessible label" pattern the audit brief calls out, and it's on every
protected page (rendered once per `Topbar`, which every tenant and Super Admin page uses via
`AppShell`). `NotificationBell.tsx` right next to it in the same header gets this right
(`aria-label={unreadCount > 0 ? ... : 'Notifications'}`) — this is a real, local inconsistency, not
a project-wide gap.

**Proposed fix:** add `aria-label="Account menu"` (or similar) to the button. One-line change, no
visual/behavioral difference (still icon+chevron, no text budget consumed).

**Risk:** none — purely additive ARIA attribute, doesn't touch layout, styling, or the dropdown's
Bootstrap JS wiring.

---

### B.2 — `table-stack`'s mobile `<thead>` removal breaks table semantics for assistive tech

**File:** `src/styles/theme.css:1064-1121` (the `@media (max-width: 767.98px)` `.table-stack` block)

**Current behavior:** below 768px, `.table-stack thead { display: none }`, and `.table-stack`,
`.table-stack tbody`, `.table-stack tr`, `.table-stack td` are all switched to `display: block` so
each row renders as a stacked card with `data-label` pseudo-content standing in for the column
header.

**Why it's a problem (verified against the WAI-ARIA table-semantics rule, not assumed):**
Chromium/Firefox drop the implicit `table`/`rowgroup`/`row`/`cell` ARIA roles from `<table>`,
`<tbody>`, `<tr>`, `<td>` the moment their CSS `display` no longer matches
`table`/`table-row-group`/`table-row`/`table-cell` — this is standard, documented browser behavior,
not a bug in this app. Combined with `display: none` on `<thead>` (which removes the header cells
from the accessibility tree entirely, not just visually), a screen reader user on a mobile viewport
gets a sequence of unstyled generic blocks with no row/cell semantics and no column context — the
`data-label` values are CSS generated content (`content: attr(data-label)`), which is inconsistently
exposed to assistive tech across browser/AT combinations and is explicitly not treated as a robust
substitute for real accessible text in WCAG accessibility-support guidance.

**Scope:** this is the single CSS rule every data table in the app depends on for its mobile layout
— `AdminBookingsPage`, `FailedNotificationsPage`, `TenantRequestManagementPage`,
`AppointmentsPage`/`BookingTable`, `StaffPage`/`TeamMemberTable`, `ClientsPage`/`CustomerTable`,
`ServicesPage`/`ServiceTable`, `TimeOffsPage`/`TimeOffTable`, `RefundRequestsPage`/
`RefundRequestTable`, `BranchesPage`/`BranchTable` — effectively every table in the protected app.

**Proposed fix (two independent parts, either can ship alone):**
1. **Low-cost, CSS-only:** replace `.table-stack thead { display: none }` with a visually-hidden
   clip (keeps `<th>` text in the accessibility tree without showing it) — this alone restores
   column-name announcements for screen reader users navigating by content, without touching any
   component file.
2. **More complete, touches every table-stack consumer:** explicitly restore `role="table"`,
   `role="rowgroup"`, `role="row"`, `role="cell"`/`role="columnheader"` on the relevant elements so
   the ARIA table semantics survive the `display: block` override too.

**Risk:** low for part 1 (pure CSS, no visual change at any breakpoint, only affects what
assistive tech can reach). Medium effort for part 2 (touches ~10 table component files, though
mechanically identical in each) — this is the one item in this report large enough that it may not
belong in a single Step 10 pass; see §I.

---

### B.3 — `RefundReviewReminderBanner` references an undefined CSS custom property and bypasses Step 9's new `.alert-warning` rule

**File:** `src/components/refunds/RefundReviewReminderBanner.tsx:18`

**Current behavior:**
```tsx
<div className="alert d-flex ..." style={{ backgroundColor: 'var(--color-warning-subtle, #fff3cd)' }} role="alert">
```

**Why it's a problem:** `--color-warning-subtle` is not defined anywhere in `theme.css` (confirmed
by search — this app's equivalent "warning soft" token is `--color-accent-soft`, per Step 1's
palette and Step 9's own new `.alert-warning` rule). Since the custom property never resolves, the
inline style's fallback (`#fff3cd`, stock Bootstrap pale yellow) is what always renders — this
banner has never actually used this app's brand tokens, in light or dark mode, and doesn't pick up
dark-mode repainting at all (a hardcoded hex ignores `[data-bs-theme='dark']` entirely). It also
predates and duplicates what Step 9's `.alert-warning` rule (`theme.css:644-651`) now already
provides via the plain `alert-warning` class — this component just never adopted it because it
was written as `className="alert"` with a bespoke inline override instead of `alert alert-warning`.

**Proposed fix:** change `className="alert d-flex ..."` to `className="alert alert-warning d-flex ..."` and delete the inline `style={{ backgroundColor: ... }}` entirely — picks up Step 9's
already-correct, dark-mode-aware warning tone for free, using the class the rest of the app's
warning alerts should already be using.

**Risk:** low — visible color changes from the current stock-yellow to the app's actual amber/accent
warning tone, which is an intended-looking correction, not a regression (this banner's *intent* is
already "you're in the warning family," it just never got the underlying color right).

---

### B.4 — `badge-tone-warning` / `.alert-warning` fail WCAG AA contrast in dark mode (empirically confirmed)

**Files:** `src/styles/theme.css:538-541` (`.badge-tone-warning`), `src/styles/theme.css:644-651`
(`.alert-warning`, added in Step 9 and inherits the same color)

**Current behavior:** both rules pair `--color-accent-soft` (background) with a literal,
non-token hex text color `#92620a`. `--color-accent-soft` is dark-mode-aware
(`rgba(245,158,11,0.12)` in light mode → `rgba(251,191,36,0.16)` in dark mode), but `#92620a` is
not — it's the same fixed value in both themes.

**Measured, not assumed** (composited the real `rgba()` background over the real canvas color via a
throwaway Playwright check against `/find-workspace`, then computed WCAG relative-luminance
contrast):

| Mode | Composited background | Text color | Contrast ratio |
|---|---|---|---|
| Light | `rgb(247, 236, 222)` | `rgb(146, 98, 10)` | **4.54:1** — passes AA normal text (needs 4.5:1) |
| Dark | `rgb(57, 49, 41)` | `rgb(146, 98, 10)` | **2.41:1** — fails AA (needs 4.5:1; even fails the 3:1 large-text/UI-component floor) |

**Why it's a problem:** the `#92620a` value was deliberately chosen (per its own code comment) for
AA contrast against the *light-mode* soft background — that reasoning was correct for light mode
but was never re-validated for dark mode, where the same soft-tint token composites to a much
darker background that a dark-brown foreground can't sit on legibly. This affects every
`badge-tone-warning` badge (e.g. "Admitted" bookings, via `BOOKING_DISPLAY_STATUS_BADGE_TONE`) and
`.alert-warning` in dark mode.

**Proposed fix:** introduce a dark-mode-aware token for this text color (mirroring how every other
`badge-tone-*`/`.alert-*` pair already re-points through a `--color-*` token that has its own
`[data-bs-theme='dark']` redefinition elsewhere in `theme.css`) and redefine it with a lighter
amber/gold inside the dark-mode block, instead of the fixed hex.

**Risk:** low-to-medium — a real dark-mode color change for warning badges/alerts, needs a quick
visual check against the dark canvas once implemented, but the failure mode today (illegible text)
is strictly worse.

---

### B.5 — `Modal` has no keyboard focus trap

**File:** `src/components/common/Modal.tsx`

**Current behavior:** `Modal` correctly sets initial focus on open, restores focus to the trigger on
close, closes on `Escape`, and carries `role="dialog"`/`aria-modal="true"`/`aria-label`. It does
**not** intercept `Tab`/`Shift+Tab` to keep focus cycling within the dialog — a sighted keyboard
user can `Tab` past the last focusable element in the modal and land on background page content
that's still visually obscured behind the backdrop.

**Why it's a problem:** this is the one piece of the WAI-ARIA APG "Dialog (Modal)" pattern this
component doesn't implement; `aria-modal="true"` tells assistive tech to treat the rest of the page
as inert, which mitigates this for screen reader users, but doesn't help sighted keyboard-only
users.

**Proposed fix:** add a `Tab`/`Shift+Tab` keydown handler alongside the existing `Escape` handler
that cycles focus between the first and last focusable elements inside `dialogRef.current`. Single
shared component — one fix benefits every modal in the app (every domain modal already builds on
this component per Steps 1–9's own findings).

**Risk:** low — additive keydown logic, no change to any modal's existing content/behavior/props.

---

### B.6 — `.btn-icon`'s base size stays below the 44px touch-target guideline everywhere except `RowActions`

**File:** `src/styles/theme.css:382-395`, `405-414`

**Current behavior:** `.btn-icon` (no `.btn-sm`) is `2rem × 2rem` (32px). `.btn-icon.btn-sm` is
`1.75rem × 1.75rem` (28px). Only `.row-action-btn.btn-icon` gets scaled up to `2.75rem × 2.75rem`
(44px) below the `768px` breakpoint (Step 5's fix) — the code comment there says explicitly:
*"`.btn-icon`'s base 28px/32px stays untouched everywhere else in the app (Topbar, NotificationBell,
etc. are out of this step's table-actions-only scope)."*

**Why it's a problem:** this is a self-documented, deliberate deferral, not an oversight — and it's
still live. `Topbar`'s hamburger-menu button, sidebar-collapse toggle, dark-mode toggle, and
public-booking-page link, plus `NotificationBell`'s trigger, are all `.btn-icon` at 32px, rendered
on **every** protected page at **every** viewport including mobile, where the header is always
visible (unlike table row actions, which only matter once a table is on screen). These are
higher-traffic controls than table row actions, not lower.

**Proposed fix:** extend the same, already-proven pattern — a `max-width: 767.98px` media query
bumping the base `.btn-icon` (not just `.row-action-btn.btn-icon`) to 44px — or scope it to the
specific header controls if a blanket change is judged too broad.

**Risk:** low — same CSS technique already shipped and verified in Step 5, just widened in scope.
Slightly increases header button footprint on mobile only; desktop is unaffected either way since
the rule is inside the same `max-width: 767.98px` query already in use.

---

### B.7 — Two raw `<button>`s in shared/common components are unmigrated `Button` candidates

**Files:** `src/components/common/EmptyState.tsx:25`, `src/components/pwa/InstallAppButton.tsx:11`

**Current behavior:**
```tsx
// EmptyState.tsx
<button type="button" className="btn btn-primary btn-sm" onClick={onAction}>{actionLabel}</button>
// InstallAppButton.tsx
<button type="button" className="btn btn-outline-primary" onClick={promptInstall}>Install App</button>
```

**Why it's a problem:** both are plain, single-purpose action buttons with no dropdown/external-link
requirement (the two legitimate reasons every other raw `<button>` in the app stays raw, per
Steps 8–9's own findings) — a mechanical `Button` migration, not a judgment call. `EmptyState` in
particular is rendered by nearly every table/list's empty state across the whole app, so this one
raw button has the widest blast radius of anything found in this audit.

**Proposed fix:**
```tsx
<Button variant="primary" size="sm" onClick={onAction}>{actionLabel}</Button>
<Button variant="outline-primary" onClick={promptInstall}>Install App</Button>
```

**Risk:** very low — identical resulting classes (`btn btn-primary btn-sm`, `btn btn-outline-primary`), `Button` forwards `onClick`/`children` unchanged.

---

### B.8 — `RefundReviewReminderBanner`'s CTA button uses a variant `Button` doesn't support

**File:** `src/components/refunds/RefundReviewReminderBanner.tsx:26`

**Current behavior:** `<button className="btn btn-warning btn-sm" ...>Review Now</button>` —
`'warning'`/`'outline-warning'` isn't in `Button`'s `ButtonVariant` union, and this is the only
`btn-warning` call site in the app (confirmed by search), so it doesn't clear the "2+ real
call sites" bar this refactor has used throughout to justify extending a shared component's API.

**Why it's worth flagging (not a required fix):** structurally this button is otherwise a clean
`Button` candidate (plain, single `onClick`, single label) — it's blocked purely on the variant
question, not on anything structural. This is a genuine judgment call (what should this button
actually look like — is `btn-warning` even the right visual weight next to the alert it lives
inside, once B.3's fix makes that alert properly amber-toned?), not a mechanical migration, so it's
listed separately from B.7 rather than bundled with it.

**Proposed options:** (a) leave as a raw button — single call site, matches this refactor's own
"don't over-abstract for one consumer" rule; (b) migrate to `Button` using an already-supported
variant (e.g. `outline-secondary`, sitting quietly inside the now-correctly-amber alert rather than
competing with it); (c) add a `'warning'`/`'outline-warning'` variant to `Button` if you expect
future warning-toned CTAs elsewhere. Not deciding this one — see §I.

**Risk:** depends entirely on which option is chosen; (a) is zero-risk (no change).

---

## C. Existing follow-ups — reviewed and reclassified

| Follow-up (origin) | Current status (verified fresh, not assumed) | Classification |
|---|---|---|
| `.btn-icon` touch target sizing (flagged in Step 5's own code comment) | Confirmed still present — see **B.6**. Self-documented, not fixed since. | **Fix in Step 10** |
| `table-stack` mobile `<thead>` accessibility (this session's audit brief) | Confirmed real via ARIA-role/display-mode reasoning — see **B.2**. Never previously flagged in any step's plan; this is the first time it's been inspected. | **Fix in Step 10** (part 1, CSS-only) — **defer** part 2 (per-table ARIA roles) to a dedicated pass; see §I |
| `StaffLineupTimeline.tsx`'s `pb-mono` outside `.pb-root` (found + deferred in Step 9's plan, §5) | Confirmed still present at `src/components/dashboard/StaffLineupTimeline.tsx:32` — grepped fresh, unchanged. Protected-app file, same one-line fix pattern Step 5 already used for the identical bug in `RefundRequestTable.tsx`. | **Fix in Step 10** — small, low-risk, precedented |
| `StatusStepper` overflow at 320px on `/request-access/pending` (found + worked around, not fixed, in this session's Step 9 Playwright work) | Confirmed still present. Root cause: `StatusStepper.tsx`'s step-circle-and-connector layout doesn't shrink/wrap at 320px; the migrated `Card` itself fits the viewport correctly (confirmed in Step 9's own test), so this is unrelated to the Card migration. | **Fix in Step 10** — narrow, single-component fix; low risk since it's presentational sizing, not logic |
| Roadmap doc's stale Step 9/10 numbering (§ header note above) | Confirmed stale by reading the doc fresh. | **No longer applicable to code** — documentation-only, out of scope for this turn per your instructions ("do not modify documentation") |
| Two raw-`Card` usages intentionally kept specialized (`BusinessTypeSelector`, `SchedulePreviewCard` — Step 9 §4a) | Re-verified via fresh grep: still raw, for the same structural reasons Step 9 already documented (radio-chip semantics; stronger elevation). Nothing in the code has changed to invalidate that reasoning. | **No longer applicable** — confirmed correct as-is, not a follow-up |
| `PlanSummaryCard`'s interactive-accordion raw card (Step 9 §4a, related observation) | Re-verified: still raw, `Card`'s `bodyClassName` (a string prop) still can't carry `role="button"`/`aria-expanded`/`onClick`/`onKeyDown`. | **No longer applicable** — confirmed correct as-is |
| `HeroSection.tsx`'s hash-anchor CTA button (Step 9 §4c) | Re-verified: still a same-page `#features` anchor; `Button`'s `to` prop still only renders a router `<Link>`. | **No longer applicable** — confirmed correct as-is |
| `AppearanceSettingsPage`'s palette-swatch raw buttons (Step 8, "reviewed and intentionally NOT migrated") | Not re-read this pass (see §A) — no reason to expect drift; carried forward as still correct per "treat prior decisions as established." | **No longer applicable** |

---

## D. No-change findings

Confirmed already correct — listed so this audit doesn't imply more is broken than actually is:

- **Super Admin's non-dashboard pages** (`AdminBookingsPage`, `FailedNotificationsPage`,
  `TenantRequestManagementPage`) and their table components (`FailedOutboxMessageTable`,
  `TenantRequestTable`) — never individually named in any prior step's plan (see the header note),
  but all four already use `PageHeader`, `Card`, `table-stack`, `RowActions`, `TableSkeleton`,
  `EmptyState`, `Pagination` correctly, matching the Step 8 tenant-page pattern exactly. No code
  change needed.
- **Step 2's shell unification actually landed as designed** — `DashboardLayout.tsx` and
  `SuperAdminLayout.tsx` both compose the same `AppShell` + `Topbar`, `AdminSidebar.tsx`/
  `AdminTopbar.tsx` are confirmed fully deleted (not just unused), and `MobileListCard.tsx`/
  `AdminSummaryCard.tsx` are confirmed fully deleted too, with zero lingering CSS classes for any
  of them anywhere in `src/styles/`.
- **`NavLink`'s automatic `aria-current="page"`** correctly covers `Sidebar`/`BottomNav`;
  `Breadcrumb.tsx` and `Pagination.tsx` both set `aria-current` explicitly and correctly for their
  own non-`NavLink` markup. `BookingCalendar.tsx` (public wizard) uses `aria-current="date"`
  correctly for the day picker. No gaps found.
- **`FormGroup`** ties `label`/`htmlFor`/`error` (with `role="alert"`) consistently; call sites
  inspected (e.g. `ResetPasswordPage.tsx`) correctly add `aria-describedby` pointing at the error
  or help text alongside it.
- **The calendar day-chip tone system is not a duplicate status-badge system.** `MonthCalendarGrid`'s
  `BOOKING_DISPLAY_STATUS_CHIP_CLASS` reuses the exact same `badge-tone-*` classes the shared
  `Badge` component uses, sourced from one shared mapping (`utils/bookingDisplayStatus.ts`) that
  also exports the `BadgeTone`-typed version other components consume — a single source of truth,
  just applied directly instead of through `<Badge>` because chips have their own compact pill
  layout. Correct, not a gap.
- **The raw-`Card`/raw-`badge` sweep found nothing new.** A fresh, full-repo grep for
  `card border-0 shadow`/`"card-body`/`badge rounded-pill` reproduced exactly Step 9's four already-
  catalogued raw-card sites (2 migrated, 2 correctly kept specialized) and no new raw-badge markup
  beyond what Step 3 already consolidated. No drift since Step 9.
- **`RowActions`' raw-`<button>` dropdown trigger, `NotificationBell`'s trigger, and `Topbar`'s
  hamburger/account-menu triggers using `data-bs-toggle="dropdown"` as raw `<button>`s instead of
  `Button`** — confirmed structurally necessary: `Button`'s `IButtonProps` has no index signature
  for arbitrary `data-*` attributes, so passing `data-bs-toggle` to it doesn't type-check. Consistent,
  justified pattern across the whole app, not a gap (matches Step 8's own finding on this exact
  question for `RowActions`).
- **Bootstrap's CSS+JS imports are intact** (`main.tsx:4-5`) and every `package.json` dependency has
  live call sites (`axios` ×31 files, `react-router-dom` ×26, `@microsoft/signalr` and
  `@yudiel/react-qr-scanner` ×1 each — both single-purpose, both actually used, not dead weight).
- **`.pb-root` has zero dark-mode surface area** — no `[data-bs-theme='dark']` block, no
  `prefers-color-scheme` query anywhere in `publicBooking.css`. This isn't a partial/broken
  implementation; it's a deliberately light-only system with no hooks that could accidentally leak
  the dashboard's dark-mode state into it. `DashboardLayout.tsx` also explicitly resets
  `data-bs-theme`/`data-palette` to their defaults on unmount, closing the one path a leak could
  otherwise take. Confirmed correct as an intentional design boundary, not inspected for a "fix" —
  per your instruction to treat `.pb-root` as a separate, complete system.
- **Public booking system's buttons/alerts/forms** — Step 9 already brought
  `CancelBookingPage`/`EwalletSubmissionForm` onto `.pb-alert-danger`/`.pb-btn-primary`; this audit's
  fresh sweep found no further `.pb-root` inconsistency beyond the already-known, already-deferred
  `StaffLineupTimeline` bug (which is a **protected**-app file that merely uses a `.pb-*` class it
  shouldn't, not a `.pb-root` system defect).

---

## E. Playwright coverage gaps

**What's already covered** (140 tests across `step1`–`step9` specs, confirmed passing as of Step
9's report): design tokens/dark-mode/palette basics, `AppShell`/`BottomNav` structural checks,
all six `badge-tone-*` colors, `table-stack` breakpoint switching, `RowActions`' inline-vs-overflow
hierarchy (incl. `forceMenu`), `StatTile` sizing/emphasis/dark-mode/palette, `CalendarPage`'s
Legend toggle, and Step 9's public-page Card/alert/button migrations.

**Not covered — genuinely important, previously untested:**
1. **Every finding in §B above** — none of B.1–B.8 has any Playwright assertion today (they were
   found by fresh manual/grep inspection this pass, not by an existing failing test).
2. **Modal keyboard behavior** — `Modal.tsx`'s `Escape`-to-close, initial-focus, and focus-restore-
   on-close have no Playwright coverage at all despite being real, testable, reachable-without-auth
   behavior once mounted via a fixture (no protected route needed — `Modal` can be exercised
   standalone on a public host page the same way Step 6/8's fixtures already do for other
   components).
3. **Dark-mode contrast** — every prior dark-mode check (Step 1, Step 5, Step 6, Step 9) asserts
   *"the color changed"* (`not.toBe(lightValue)`), never *"the color is still legible"*. B.4 was
   only caught because this audit computed a real contrast ratio — that computation is a pattern
   worth keeping as a reusable Playwright helper, not a one-off.
4. **`Topbar`/`NotificationBell`/account-menu dropdown behavior** — no test opens either dropdown,
   checks `aria-expanded` toggles from Bootstrap's own JS, or checks focus lands inside the menu.
5. **`RowActions`' dropdown-overflow trigger's accessible name** ("More actions") — covered
   structurally (Step 5's `step5-table-actions.spec.ts`) but not for the specific accessible-name
   regression class B.1 represents elsewhere (i.e., there's no repo-wide "every icon-only trigger
   has a name" sweep test).

**Possible redundancy:** none found worth consolidating — each spec file targets a distinct step's
changes and none duplicate the same assertion at the same breakpoint; the "no horizontal overflow"
checks repeat across files by design (each step re-verifies its own touched routes/components, not
the whole app), which is intentional scoping, not redundant.

**Screenshots:** `step5b` and `step6` specs' screenshot tests remain useful (they're the only
non-assertion-based visual record of the contextual-action and dashboard layouts) — no reason to
remove them.

**Protected-route testing:** still blocked by the lack of a seeded test account, unchanged from
every prior step. Every fixture/real-navigation workaround used in Steps 1–9 remains the only
option; this audit didn't find a new way around it and didn't attempt to fabricate one.

---

## F. Dead-code/configuration findings

- **No unused imports/locals exist in `src/` right now, verifiably** — `tsconfig.app.json` sets
  `"noUnusedLocals": true`/`"noUnusedParameters": true`, and `npm run build`'s `tsc -b` step
  enforces this on every build; the last clean build (Step 9) is proof, not an inspection guess.
- **`tsc -b` (no path argument) actually type-checks `e2e/` too**, via the root `tsconfig.json`'s
  project references (`tsconfig.app.json`, `tsconfig.node.json`, `tsconfig.playwright.json` are all
  built). This is a nuance worth recording precisely for §11's question: Playwright files are *not*
  outside type-checking (they're checked, just as a separate project graph with `noEmit: true`), but
  they *are* correctly outside the production bundle — `vite build` only walks the graph reachable
  from `index.html` → `main.tsx`, and nothing in `src/` imports anything from `e2e/`.
- **`@playwright/test` and its transitive tooling are `devDependencies`-only** — confirmed in
  `package.json`. `test-results/`, `playwright-report/`, `blob-report/`, `playwright/.cache/` are
  all gitignored.
- **No dead dependency** — every entry in `package.json`'s `dependencies` has at least one live
  import (checked each individually; see §D).
- **`MobileListCard.tsx`, `AdminSummaryCard.tsx`, `AdminSidebar.tsx`, `AdminTopbar.tsx`** — confirmed
  fully deleted, not just unreferenced. Nothing to clean up.
- **Minor, optional: `SidePanel.tsx`'s backdrop** (`src/components/common/SidePanel.tsx:77-81`) is a
  bespoke inline-styled `<div style={{ backgroundColor: '#000', ... }}>` with its own
  `side-panel-backdrop-transition` class, while `Modal.tsx` achieves the same visual effect (a dim
  scrim behind an overlay) by reusing Bootstrap's own `.modal-backdrop` class. Both render correctly
  in both themes (a black scrim is theme-agnostic by nature, so this isn't a token violation like
  B.3), but it's two implementations of the same concept. Not proposed as a required fix — flagged
  only because §12 of your brief asked specifically about duplicated layout patterns, and this is
  the one instance found. Optional, see §I.

---

## G. Proposed Step 10 implementation scope

**Required fixes** (concrete accessibility/consistency defects with clear, low-risk resolutions):

| # | File(s) | Change |
|---|---|---|
| 1 | `src/components/layout/Topbar.tsx` | Add `aria-label="Account menu"` to the account-menu trigger button (B.1) |
| 2 | `src/components/refunds/RefundReviewReminderBanner.tsx` | Replace the bespoke inline `style` + bare `alert` class with `alert alert-warning`, deleting the dead `--color-warning-subtle` reference (B.3) |
| 3 | `src/styles/theme.css` | Introduce a dark-mode-aware token for `badge-tone-warning`/`.alert-warning`'s text color, replacing the fixed `#92620a` (B.4) |
| 4 | `src/components/common/Modal.tsx` | Add a `Tab`/`Shift+Tab` focus-trap handler alongside the existing `Escape` handler (B.5) |
| 5 | `src/styles/theme.css` | Extend the mobile touch-target bump from `.row-action-btn.btn-icon` to base `.btn-icon` (B.6) |
| 6 | `src/components/common/EmptyState.tsx`, `src/components/pwa/InstallAppButton.tsx` | Migrate both raw buttons to `Button` (B.7) |
| 7 | `src/components/dashboard/StaffLineupTimeline.tsx` | Fix the `pb-mono`-outside-`.pb-root` bug (same pattern as Step 5's `RefundRequestTable` fix) |
| 8 | `src/components/requestAccess/StatusStepper.tsx` | Fix the 320px overflow (§C) |
| 9 | `src/styles/theme.css` | `.table-stack thead` visually-hidden-instead-of-`display:none` (B.2, part 1 only) |

**Optional improvements** (real, but judgment calls or larger blast radius — proposing, not
assuming):

| # | File(s) | Change | Why optional |
|---|---|---|---|
| A | `src/components/refunds/RefundReviewReminderBanner.tsx` | Resolve B.8 (button variant) | Needs a decision on which variant/visual weight, not mechanical |
| B | ~10 table component files | B.2 part 2 — explicit ARIA roles restoring full table semantics under `table-stack` | Correct but touches every table-stack consumer; bigger than a "final audit" pass typically carries |
| C | `src/components/common/SidePanel.tsx` | Consolidate its backdrop onto `.modal-backdrop` instead of its own inline-styled div | Cosmetically already correct either way; pure de-duplication, zero user-facing effect |

**Not proposed:** no new component, no new abstraction, no `.pb-root` migration onto protected-app
components, no redesign of anything. Every item above is a fix to something demonstrably broken or
inconsistent, not a stylistic preference.

---

## H. Verification plan

Same discipline every prior step has used:

1. **Build:** `npm run build` (`tsc -b` + `vite build`) — clean, zero errors.
2. **Lint:** `npm run lint` (`oxlint`) — zero new warnings beyond the pre-existing baseline.
3. **Playwright — full suite:** `npm run test:e2e` — all pre-existing specs stay green; nothing
   fixed by weakening an assertion.
4. **New Playwright coverage, per fix:**
   - B.1: computed `getComputedStyle`/`accessible name` check (or `page.getByRole('button', { name: 'Account menu' })` resolves) via a fixture on `/find-workspace`.
   - B.3/B.4: re-run the same real-token-vs-fixture contrast comparison used in this audit's
     investigation, promoted into an actual spec — assert the composited contrast ratio is ≥ 4.5:1
     in both light and dark mode, not just "the color changed."
   - B.5: fixture-mount `Modal` on a public host page, `Tab` through its focusable elements, assert
     focus stays inside the dialog and wraps at both ends.
   - B.6: measure `.btn-icon` bounding box at ≤767px on `Topbar` fixtures/real pages, confirm ≥44px
     in both dimensions; confirm desktop (≥768px) is unchanged.
   - B.7: confirm rendered classes match `Button`'s output exactly (same technique as Step 8/9's
     `Button`-migration specs).
   - `StaffLineupTimeline`: confirm the mono class only renders visible monospace styling when a
     `.pb-root` ancestor is present, and that removing the bug doesn't change the visible text
     content.
   - `StatusStepper`: no-horizontal-overflow check at 320px on `/request-access/pending`, replacing
     Step 9's current workaround test rather than leaving both in place.
   - B.2 (part 1): confirm `<thead>`'s text is present in the accessibility tree (not `display:
     none`) at ≤767px while remaining visually hidden (zero visual diff at any breakpoint).
5. **Viewports:** 320/375/430/768/992/1280px, matching your list exactly (this is a superset of
   every prior step's breakpoint set, so no separate "old" set needs to be dropped).
6. **Dark mode + tenant palette:** re-verify every touched color (B.3, B.4) in light mode, dark
   mode, and at least the `teal` tenant palette (matching Step 1/5/6's existing dark+palette
   pattern), using real computed contrast ratios wherever text-on-background is involved, not just
   "did the value change."
7. **Accessibility:** for every fixed element, confirm an accessible name resolves via
   Playwright's `getByRole(..., { name })`, not just an `aria-label` string match — that's a
   stronger, more representative check of what a screen reader actually exposes.

---

## I. Decisions required from you

1. **B.2 (table-stack accessibility)** — ship part 1 only (CSS-only, visually-hidden headers) in
   Step 10, or also take on part 2 (explicit ARIA roles across ~10 table files) in the same pass?
   Part 2 is correct and precedented in approach, but meaningfully larger than everything else in
   this report combined.
2. **B.8 (`RefundReviewReminderBanner`'s CTA button variant)** — leave it raw (no change), migrate
   it to `Button` with an existing variant (which one?), or add a `warning`/`outline-warning`
   variant to `Button`'s API for it?
3. **Optional item C (`SidePanel`'s backdrop)** — worth doing as part of Step 10, or skip entirely
   (zero user-facing effect either way, pure internal de-duplication)?
4. **Scope confirmation** — does the "Required fixes" table in §G match what you want Step 10 to
   actually implement, or should any of those 9 items move to "optional"/deferred instead?

Waiting for your approval before writing any code.

---

## Implementation results (post-approval)

> **Status: implemented.** All 9 required fixes from §G were verified against the current code
> before implementation (per the "re-read and verify" instruction), then implemented, verified, and
> covered with new Playwright tests. Optional items A/B/C from §G were **not** implemented —
> A and B remain open decisions (§I), C was never approved.

### Pre-implementation verification

Every finding was re-read fresh before touching it. All held exactly as described, with one
correction: **B.6's "NotificationBell's trigger" claim didn't hold.** Re-reading
`NotificationBell.tsx` showed it doesn't use the shared `.btn-icon` class at all — it sizes itself
via its own inline `style={{ width: 40, height: 40 }}`, already closer to the 44px guideline than
the 32px `.btn-icon` problem described. It was left untouched (not a `.btn-icon` consumer, so
touching it would have been outside this finding's actual scope), and the fix was scoped to the
real `.btn-icon` consumers instead (`Topbar`'s hamburger/sidebar-toggle/dark-mode-toggle buttons,
`EditTeamMemberModal`'s per-row "Remove" break button, and any future consumer).

### What changed, per fix

1. **Dark-mode warning contrast (B.4).** Promoted the hardcoded `#92620a` into a new
   `--color-accent-strong` token (light value unchanged, zero visual difference in light mode) and
   gave it a dark-mode redefinition reusing `--color-accent`'s own already-defined dark value
   (`#fbbf24`) — no new color invented. `.badge-tone-warning` and `.alert-warning` both now read
   from the token. Re-measured with the same composited-contrast method the audit used: **light
   4.54:1 (unchanged), dark 7.65:1 (was 2.41:1)** — both comfortably clear the 4.5:1 requirement.
2. **Topbar account-menu accessible name (B.1).** Added `aria-label="Account menu"` to the existing
   button. No visual or interaction change.
3. **`table-stack` mobile header accessibility (B.2).** Investigated first, as instructed: the
   table markup itself (`<thead><tr><th scope="col">`, `data-label` on every `<td>`) is already
   well-formed and CSS-only *is* sufficient to fix the sharpest part of the defect — headers being
   completely absent from the accessibility tree. Replaced `.table-stack thead { display: none }`
   with Bootstrap's own visually-hidden clip technique (the same one `Button`/`SidebarNavItem`
   already use), so header text stays in the DOM/accessibility tree while remaining visually
   imperceptible (clipped to ~0×0px). **Explicitly did not** add ARIA roles to any table file: the
   deeper defect (display:block stripping `<table>`/`<tr>`/`<td>`'s implicit table/row/cell roles)
   can only be fixed by adding `role` attributes in HTML, which CSS cannot do — that part remains
   out of scope, exactly as flagged in §I, and is **not** implemented here.
4. **`.btn-icon` touch targets (B.6).** Added one new, independent mobile-only rule
   (`@media (max-width: 767.98px) { .btn-icon, .btn-icon.btn-sm { width/height: 2.75rem } }`)
   without touching the existing `.row-action-btn.btn-icon` rule or its comment at all — `RowActions`
   is byte-for-byte unchanged. Desktop/tablet (≥768px) sizing is unchanged for every consumer.
5. **`EmptyState`/`InstallAppButton` raw buttons (B.7).** Both migrated to `Button` with the exact
   variant/size that reproduces their prior classes (`variant="primary" size="sm"` →
   `btn btn-primary btn-sm`; `variant="outline-primary"` → `btn btn-outline-primary`). No behavior
   change — `onClick`/`children` pass through identically.
6. **`RefundReviewReminderBanner`'s undefined CSS variable (B.3).** Replaced
   `className="alert"` + the broken inline `style={{ backgroundColor: 'var(--color-warning-subtle, #fff3cd)' }}` with `className="alert alert-warning"` and no inline style — now resolves through
   the same (now dark-mode-correct, per fix 1) tokens every other warning surface in the app uses.
   **Not changed:** the "Review Now" button's `btn-warning` class (B.8) — this was explicitly left
   as an open decision in §I, not part of the approved scope for this pass.
7. **`StaffLineupTimeline.tsx`'s `pb-mono` (B.9 / §C).** Changed to `text-mono` — the protected
   app's own existing monospace utility (`theme.css`'s `--font-mono` token), already the established
   fix for this exact bug class (`RefundRequestTable.tsx`, Step 5). No `.pb-root` dependency
   introduced or removed from the protected app; the component now correctly depends only on the
   protected app's own token system.
8. **`StatusStepper` mobile overflow (§C).** Root cause confirmed: at 320px the 3-step row (fixed
   20px dots/connectors + nowrap labels) needs ~290px but only has ~248px available inside the
   card's padding. Fixed with a scoped `@media (max-width: 374.98px)` rule shrinking the decorative
   chrome only (dot size 20→14px, connector 20→10px, gaps 8/6→4px, label font-size 0.75→0.6875rem) —
   the three step labels ("Submitted"/"Under Review"/"Approved") stay full-length and fully
   readable, just smaller; nothing was truncated or hidden. **No `overflow-x: hidden` used.**
   Empirically verified via Playwright: 0px overflow at 320/340/360/374/375px.
9. **`Modal` focus trap (B.5).** Inspected all 19 consumers first — confirmed none nest a `Modal`
   inside another `Modal`, and all use the standard `isOpen`/`title`/`onClose`/`footer`/`children`
   API, so a change centralized in `Modal.tsx` itself is safe without touching any consumer. Added a
   `Tab`/`Shift+Tab` branch to the existing `Escape`-handling keydown listener: on `Tab` at the last
   focusable element, wraps to the first; on `Shift+Tab` at the first, wraps to the last. The
   pre-existing initial-focus-on-open and focus-restore-on-close behavior is untouched. No new
   dependency.

### Files changed

| File | Change |
|---|---|
| `src/styles/theme.css` | New `--color-accent-strong` token (light + dark); `.badge-tone-warning`/`.alert-warning` updated to use it; `.table-stack thead` switched from `display:none` to a visually-hidden clip; new mobile-only `.btn-icon`/`.btn-icon.btn-sm` touch-target rule |
| `src/components/layout/Topbar.tsx` | Added `aria-label="Account menu"` |
| `src/components/common/EmptyState.tsx` | Raw button → `Button` |
| `src/components/pwa/InstallAppButton.tsx` | Raw button → `Button` |
| `src/components/refunds/RefundReviewReminderBanner.tsx` | `alert` → `alert alert-warning`; removed the broken inline style |
| `src/components/dashboard/StaffLineupTimeline.tsx` | `pb-mono` → `text-mono` |
| `src/styles/requestAccess.css` | New `@media (max-width: 374.98px)` rule shrinking `.status-stepper`'s decorative sizing |
| `src/components/common/Modal.tsx` | Added `FOCUSABLE_SELECTOR` + Tab/Shift+Tab focus-trap branch in the existing keydown handler |
| `e2e/step10-final-audit.spec.ts` | New — 56 tests covering all 9 fixes |
| `e2e/step4-responsive-tables.spec.ts` | One assertion updated (`thead` is no longer Playwright-"hidden" under the new clip technique — see below) |
| `e2e/step9-public-pages.spec.ts` | Removed the now-unnecessary `/request-access/pending`-at-320px exclusion and its stale comment, now that `StatusStepper`'s overflow is fixed; tightened its Card-box test's description |

**Files intentionally untouched:** every file in §10 of your instructions (Super Admin pages,
`AppShell`, `Sidebar`, `BottomNav`, dashboard layouts, `StatTile`, existing status mappings,
`.pb-root`/public booking architecture, backend, auth, database, API contracts, business logic);
`RowActions.tsx` and its existing CSS rule/comment (preserved exactly, per instruction 4);
`NotificationBell.tsx` (not a `.btn-icon` consumer — see the correction above); the "Review Now"
button in `RefundReviewReminderBanner.tsx` (B.8, still an open decision); all ~10 `table-stack`
table component files (B.2 part 2, explicit ARIA roles — out of scope, see fix 3 above).

### A pre-existing test needed updating (not weakened)

`step4-responsive-tables.spec.ts`'s mobile-breakpoint assertion `await expect(thead).toBeHidden()`
started failing after fix 3 — correctly so: Playwright's `toBeHidden()` only checks
`display`/`visibility`/opacity plus a non-zero box, and the whole point of switching from
`display: none` to a visually-hidden clip was to make the header stay technically present (a 1×1px
box), which is exactly what now makes Playwright consider it "visible" despite being
imperceptible to any sighted user. This is a direct, correct, intended consequence of the approved
fix, not a regression — the assertion was updated to check the real new behavior directly
(`display` isn't `none`, bounding box is ≤1×1px, header text is still present via `toContainText`)
rather than relying on a Playwright helper whose definition no longer matches what "hidden" means
here. Nothing was removed or weakened; the replacement is strictly more specific than the original.

### Verification

- **Build:** `npm run build` (`tsc -b && vite build`) — clean, zero errors, both before and after
  every fix.
- **Lint:** `npm run lint` (`oxlint`) — clean; only the same 10 pre-existing warnings present since
  Step 1, unchanged by any Step 10 file.
- **Playwright — new coverage:** `e2e/step10-final-audit.spec.ts`, 56 tests (28 × 2 projects),
  covering every item in your §13 checklist: warning contrast in light/dark mode (measured, not
  assumed), account-menu accessible name, `table-stack` header accessibility at mobile vs. desktop,
  `.btn-icon` sizing at mobile vs. desktop (plus a `RowActions`-unaffected regression guard),
  `EmptyState`/`InstallAppButton` exact-output equivalence, `Modal`'s full focus-trap behavior
  (enters correctly, stays trapped under repeated Tab presses, Shift+Tab wraps too, Escape restores
  focus to the trigger), `StatusStepper` overflow at 320/375/430/768px plus a content-preserved
  check, no new console errors on two representative public routes, and no horizontal overflow at
  all six requested viewports (320/375/430/768/992/1280) across three representative public routes.
- **Playwright — full suite:** **194/194 passed** (140 pre-existing from Steps 1–9, all still
  passing unmodified in behavior — 1 assertion updated as described above to match intended new
  behavior — plus 54 new from `step10-final-audit.spec.ts`).
- **Dev server:** started via Playwright's own `webServer` config for each run, no leftover process
  on port 5173 after the final run.
- **E2E dependencies:** confirmed still dev-only (`@playwright/test` remains in
  `package.json`'s `devDependencies`), unchanged by this step.

### Remaining known issues (unresolved, by design)

- **B.2 part 2** — full ARIA `role="table"`/`"row"`/`"cell"` restoration across ~10 table-stack
  component files. Confirmed via direct investigation that this cannot be done via CSS (ARIA roles
  are HTML-attribute-only), so it remains genuinely out of this pass's scope, per your explicit
  instruction not to expand a CSS-only fix into multiple table files without stopping to report
  first. This is now the report: CSS-only measurably improves the situation (headers are no longer
  fully absent from the accessibility tree) but does not restore complete table-navigation
  semantics for assistive tech.
- **B.8** — `RefundReviewReminderBanner`'s "Review Now" button still uses `btn-warning`, a variant
  `Button` doesn't support. Left exactly as it was; still an open decision (leave raw / migrate to
  an existing `Button` variant / add a new variant), not part of this approved scope.
- **Optional item C** (`SidePanel`'s bespoke backdrop vs. reusing `.modal-backdrop`) — not
  implemented; never approved, zero user-facing effect either way.

### Scope compliance

No backend, authentication, database, API-contract, or business-logic file was touched. No file in
your §10 "do not change" list was modified. No new component, new color system, new navigation
pattern, new table layout, new dashboard layout, or redesign work was introduced — every change
above is a fix to something the audit already confirmed was broken or inconsistent, using tokens,
classes, and patterns that already existed in the codebase.
