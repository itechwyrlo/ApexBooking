# Step 8 — Protected-Page Consistency Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Status: plan only. Not approved for execution.** Nothing in this document has been applied.
> Per the request, this stops after the plan is saved.

**Goal:** Confirm every remaining protected page composes from the Steps 1–7 shared system, and fix
the specific gaps a full page-by-page inspection actually found — nothing manufactured to produce a
diff.

**Architecture:** No new components. One existing shared component (`Button`) gets one optional,
non-breaking prop added so a genuine remaining consumer can migrate onto it. One page gets a
one-line markup change. Everything else in this plan is documentation of "already compliant, no
change needed" per page, which is itself the deliverable Step 8 asks for (its exit criteria is a
confirmed state, not a mandated amount of diff).

**Tech Stack:** React 19 + TypeScript, Bootstrap 5.3 — unchanged from every prior step.

**Spec:** `docs/superpowers/plans/2026-08-14-protected-ui-consistency-roadmap.md` §7 Step 8.

## Global Constraints

- No hook, service, DTO, route, or permission logic is touched anywhere in this plan.
- No new component is created — Step 7's inspection already confirmed the shared system is
  complete; Step 8 is adoption, not construction.
- Public pages are untouched (none are in Step 8's scope).
- `table-stack` remains the only table pattern; nothing here changes table structure.
- `RowActions`' Step 5 contextual hierarchy is unaffected — no table action counts changed on any
  page in this plan.

---

## Inspection findings — all 12 in-scope pages, read in full for this plan

| Page | PageHeader | Card | Shared table/form/modal components | Error/loading pattern | Gaps found |
|---|---|---|---|---|---|
| `AppointmentsPage.tsx` | ✅ | ✅ | `BookingTable`, `CancelBookingModal`, `AdmitScanModal`, `NewWalkInModal`, `Pagination` | ✅ standard | None |
| `CalendarPage.tsx` | ✅ | ✅ | `MonthCalendarGrid`, `BookingDayList`, `BookingDetailPanel`, `CalendarLegend`, `SidePanel`, `CancelBookingModal` | ✅ standard | **Legend toggle uses a raw `<button>`, not `Button`** (see below) |
| `StaffPage.tsx` | ✅ | ✅ | `TeamMemberTable`, `AddTeamMemberModal`, `EditTeamMemberModal`, `RemoveTeamMemberModal`, `Pagination` | ✅ standard | None |
| `ClientsPage.tsx` | ✅ | ✅ | `CustomerTable`, `CustomerBookingsModal`, `Pagination` | ✅ standard | None |
| `ServicesPage.tsx` | ✅ | ✅ | `ServiceTable`, `AddServiceModal`, `Pagination` | ✅ standard | None |
| `TimeOffsPage.tsx` | ✅ | ✅ | `TimeOffTable`, `RequestTimeOffModal`, `Pagination` | ✅ standard | None |
| `BusinessProfilePage.tsx` | ✅ | ✅ | `FormGroup`, `Skeleton`, `Button` (incl. link-as-button to Branches) | ✅ standard | None |
| `RefundRequestsPage.tsx` | ✅ | ✅ | `RefundRequestTable` (already `forceMenu`-aware, Step 5), `RejectRefundModal`, `MarkAsSentConfirm`, `Pagination` | ✅ standard | None |
| `BranchesPage.tsx` | ✅ | ✅ | `BranchTable`, `BranchModal` | ✅ standard | **Not named in the roadmap's Step 8 list** (documentation gap, not a code gap — see below) |
| `settings/BookingSettingsPage.tsx` | ✅ | ✅ | `FormGroup`, `FormSection`, `NumberInput`, `Skeleton` | ✅ standard | None (this is the file Step 1's plan used as the reference implementation for form pages) |
| `settings/AppearanceSettingsPage.tsx` | ✅ | ✅ | `Skeleton`, `Button` | ✅ standard | Palette swatch buttons are raw `<button>` — reviewed, **not a migration target** (see below) |
| `settings/PaymentSettingsPage.tsx` + `components/settings/PaymentGatewayCard.tsx` | ✅ | ✅ | `FormGroup`, `NumberInput`, `Skeleton` | ✅ standard | None |

**"Any reporting surface" (roadmap's Step 8 catch-all):** re-confirmed — no standalone Reports page
exists anywhere in the app (same finding as Step 6's pre-work). All reporting lives in the four
dashboards, already addressed in Step 6. Nothing further in scope here.

**Headline finding:** this codebase is already extremely consistent. Every one of the 12 pages
already uses `PageHeader`, `Card`, the correct shared table/modal/form components for its domain,
and the identical `{error && <p className="text-danger small mb-3">{error}</p>}` /
`axios.isAxiosError(...)` error-handling idiom. Steps 1–7 already did the real consolidation work as
they went. Two small, real items came out of this pass:

### Finding 1 — `CalendarPage`'s Legend toggle bypasses `Button`

```tsx
// CalendarPage.tsx:180-187 (current)
<button
  type="button"
  className="btn btn-outline-secondary btn-sm"
  aria-expanded={isLegendOpen}
  onClick={() => setIsLegendOpen((open) => !open)}
>
  {isLegendOpen ? 'Hide Legend' : 'Legend'}
</button>
```

This is the one place in the whole 12-page sweep still hand-rolling markup `Button` already covers.
It's raw only because `Button`'s prop interface (`IButtonProps`) doesn't currently accept
`aria-expanded` — every other prop it needs (`variant`, `size`, `onClick`, `children`) is already
supported.

**Why extending `Button` is justified here** (rule 7's "2+ real call sites" bar, applied
carefully): this is a *prop addition to an existing shared component*, not a new abstraction — the
comparison point isn't "how many things need a brand-new component" but "does `Button` already
almost cover this, and would a small, non-breaking addition let a real consumer stop duplicating
it." A repo-wide search for dynamic `aria-expanded` usage found exactly one other protected-scope
case with the same shape — `BottomNav.tsx`'s "More" trigger (Step 2) — which already works
correctly today and is out of this plan's "smallest safe change" mandate to touch without a reason.
Every other `aria-expanded` usage in the app (`Topbar.tsx`, `NotificationBell.tsx`, `RowActions.tsx`)
is a **static** `"false"` string on a Bootstrap `data-bs-toggle="dropdown"` trigger — Bootstrap's own
JS manages that attribute at runtime, which is the correct, already-consistent pattern and not a gap.

**Proposed fix:**

```tsx
// components/common/Button.tsx — add one optional prop, purely additive
interface IButtonProps {
  // ...existing props unchanged...
  'aria-expanded'?: boolean
}
```

Since `aria-expanded` already flows through via the existing `...rest` spread onto the native
`<button>`/`<a>` element, adding it to the interface is the entire change — no new logic. Every
existing `Button` call site is unaffected (new optional prop, not required).

```tsx
// CalendarPage.tsx — after
<Button
  variant="outline-secondary"
  size="sm"
  aria-expanded={isLegendOpen}
  onClick={() => setIsLegendOpen((open) => !open)}
>
  {isLegendOpen ? 'Hide Legend' : 'Legend'}
</Button>
```

Identical rendered classes (`btn btn-outline-secondary btn-sm`), identical behavior, identical
click handler — confirmed by reading `Button.tsx`'s class-building logic (`SIZE_CLASSES.sm` = 
`'btn-sm'`, `variant` → `btn-outline-secondary`).

### Finding 2 — `BranchesPage` isn't named in the roadmap's Step 8 scope list, but should be

The roadmap's Step 8 text lists `AppointmentsPage, CalendarPage, StaffPage, ClientsPage,
ServicesPage, TimeOffsPage, BusinessProfilePage, RefundRequestsPage` plus the three settings pages —
`BranchesPage` is a real protected page (`/:slug/dashboard/branches`, Owner-only) that was simply
left off that list, most likely an oversight from when the roadmap was first drafted (Step 1, before
any implementation). It's already fully compliant (table above) — **no code change needed**, just a
documentation correction so the roadmap's own exit criteria ("every tenant-side protected page")
is accurate. Proposed: add `BranchesPage` to the roadmap's Step 8 scope line when this plan is
executed.

### Reviewed and intentionally NOT migrated — `AppearanceSettingsPage`'s palette swatches

```tsx
<button
  key={palette.id}
  type="button"
  className="btn p-0 border-0 bg-transparent d-flex flex-column align-items-center gap-1"
  onClick={...}
  aria-pressed={values.themePaletteId === palette.id}
  aria-label={palette.name}
  title={palette.name}
>
  <span className="rounded-circle d-inline-block" style={{ ...swatch color... }} />
  <span className="small text-muted">{palette.name}</span>
</button>
```

This is a color-swatch radio-button control — structurally unlike every other `Button` usage in the
app (no icon, no text-as-primary-content, a circular color chip is the actual content,
`aria-pressed` toggle semantics rather than a momentary action). Forcing it through `Button` would
mean fighting that component's `btn btn-{variant}` class assumptions for a control that
intentionally has none of Bootstrap's button skin (`border-0 bg-transparent`). Single call site,
genuinely different shape — not a rule-7 case. **No change proposed.**

---

## Task 1: Extend `Button` to accept `aria-expanded`, migrate `CalendarPage`'s Legend toggle

**Files:**
- Modify: `src/components/common/Button.tsx`
- Modify: `src/pages/booking/CalendarPage.tsx`

- [ ] **Step 1:** Add `'aria-expanded'?: boolean` to `IButtonProps` in `Button.tsx` (purely additive
      — no change to the component's rendering logic, since `...rest` already forwards it).
- [ ] **Step 2:** In `CalendarPage.tsx`, replace the raw `<button>` (lines 180-187) with `<Button
      variant="outline-secondary" size="sm" aria-expanded={isLegendOpen} onClick={...}>` as shown
      above.
- [ ] **Step 3:** Verify: `npm run build` (type-check confirms the new prop is accepted and no
      other `Button` call site broke). Then visually confirm — since this page is behind auth with
      no seeded test account (same limitation as every prior step) — via a Playwright fixture
      reproducing `Button`'s exact rendered output with `aria-expanded` toggling true/false, at
      minimum confirming the attribute is present and the button remains keyboard-focusable.
- [ ] **Step 4:** Run `npm run lint`, `npm run test:e2e` (full suite, confirm no regression).

## Task 2: Correct the roadmap's Step 8 scope list

**Files:**
- Modify: `docs/superpowers/plans/2026-08-14-protected-ui-consistency-roadmap.md`

- [ ] Add `BranchesPage` to the Step 8 scope line (§7), noting it was already fully compliant and
      required no code change — same treatment as this plan gives every other already-compliant
      page.

---

## Self-review

**Spec coverage:** all 12 named-or-implied pages inspected and documented; the "any reporting
surface" catch-all resolved (none exists beyond Step 6's dashboards, already done).

**Placeholder scan:** none — both proposed code changes are complete, real, and shown in full
above; every "no change needed" row is a documented finding, not a placeholder.

**Rule-7 check:** the one abstraction touched (`Button`) already exists with 40+ call sites; this
plan only adds one optional, non-breaking prop to it for one additional real consumer — not a new
abstraction requiring its own 2-call-site justification.

**No backend blocker identified** — nothing in this plan needs new data, a new endpoint, or a
permission change.
