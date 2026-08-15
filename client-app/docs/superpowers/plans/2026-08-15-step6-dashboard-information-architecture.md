# Step 6 — Dashboard Information Architecture and Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Status: plan only. Not approved for execution.** Nothing in this document has been applied to
> the real app. Per the request, this stops after the plan is saved — implementation waits for
> explicit go-ahead, and several items below are flagged as needing your decision before that.

**Goal:** Reorganize the four existing dashboards (`OwnerDashboardPage`, `StaffDashboardPage`,
`AdminDashboardPage`, `SuperAdminDashboardPage`) around the data they already fetch — denser,
better-grouped, less empty space — without inventing a single new metric, trend, percentage, or
chart.

**Architecture:** One new shared component (`StatTile`, generalized from the existing
`AdminSummaryCard`) plus presentation-only edits to the four dashboard page files and one CSS
tightening pass. No hook, service, DTO, or route changes anywhere in this plan.

**Tech Stack:** React 19 + TypeScript, Bootstrap 5.3 grid/utilities, this app's existing
`theme.css` tokens — same stack as every prior step.

**Spec:** `docs/superpowers/plans/2026-08-14-protected-ui-consistency-roadmap.md` §7 Step 6 and §8
(confirmed Step 6 decisions: Staff Performance sorted and relabeled "by Revenue"; Super Admin
surfaces `approvedRequests`; no new metrics/charts anywhere; generalize the
`AdminDashboardPage`/`SuperAdminDashboardPage` KPI-row patterns rather than inventing a new
system).

## Global Constraints

- No new metrics, percentages, capacity/utilization values, or trends anywhere in this plan — every
  number shown already exists in `ITenantRevenue`, `IStaffPerformanceEntry`, `IRefundLogEntry`,
  `ITenantBookingCounts`, `IIdleStaffMember`, or `IPlatformDashboardSummary` today, confirmed by
  reading each interface fresh for this plan (unchanged since the original Step 6 pre-work).
- No chart library, no chart component — confirmed again: still zero chart dependencies in
  `package.json`, still zero chart components anywhere in `src/`.
- No hook, service, DTO, API, or route file is touched by any task in this plan.
- No change to `useTenantRevenue`, `useStaffPerformance`, `useRefundLog`, `useTenantBookingCounts`,
  `useIdleStaff`, `useActiveStaff`, `useDashboardSummary`, `useTenantRequests`, or any other hook's
  signature or behavior.
- `TrendPill` (Step 1) stays unwired in this plan — no dashboard here has a real prior-period value
  to attach it to (re-confirmed below, per dashboard). `StatTile` gets an optional slot for it so a
  *future* backend field can use it without another redesign, but nothing populates that slot here.
- Public pages, navigation, auth, and the Step 1-5 shared components (`Card`, `Badge`,
  `RowActions`, `Button`, table patterns) are unchanged — this plan only composes them.

---

## Cross-cutting: `StatTile` (new shared component)

### Current state

`components/admin/AdminSummaryCard.tsx` already has exactly the right shape (icon chip + label +
bold value, tone-driven, built on `Card`) — it's used by `SuperAdminDashboardPage` only today.
Nothing about its implementation is actually admin-specific; only its name and folder location are.
Per "generalize the existing pattern instead of inventing a new one," this plan **relocates and
renames it**, rather than building a second, competing stat-tile component:

- `components/admin/AdminSummaryCard.tsx` → `components/common/StatTile.tsx`
- Its local `AdminSummaryCardTone` union → replaced with the shared `BadgeTone` import from
  `Badge.tsx` (same dedup already done for every status badge in Step 3 — this was the one
  remaining un-deduped copy of that union, found while re-reading it for this plan).
- Adds one new optional prop, `trend`, purely as a forward-compatible slot — **not populated by
  this plan** (see Global Constraints).

### Task: Create `StatTile`, retire `AdminSummaryCard`

**Files:**
- Create: `src/components/common/StatTile.tsx`
- Modify: `src/pages/admin/SuperAdminDashboardPage.tsx` (import path only, for now)
- Delete: `src/components/admin/AdminSummaryCard.tsx` (after the one consumer is migrated)

```tsx
import type { ReactNode } from 'react'
import { Card } from './Card'
import { Icon } from './Icon'
import type { BadgeTone } from './Badge'

interface IStatTileProps {
  label: string
  value: number | string
  icon: string
  tone?: BadgeTone
  /** Reserved for a future TrendPill once a backend field provides a real prior-period value —
   * intentionally unused by every dashboard in Step 6 (see plan's Global Constraints). */
  trend?: ReactNode
}

export function StatTile({ label, value, icon, tone = 'neutral', trend }: IStatTileProps) {
  return (
    <Card>
      <div className="d-flex align-items-center gap-2 mb-2">
        <span
          className={`d-inline-flex align-items-center justify-content-center rounded-circle badge-tone-${tone}`}
          style={{ width: 36, height: 36 }}
          aria-hidden="true"
        >
          <Icon name={icon} size={18} />
        </span>
        <p className="text-muted small mb-0">{label}</p>
      </div>
      <div className="d-flex align-items-center gap-2">
        <p className="fs-3 fw-bold mb-0">{value}</p>
        {trend}
      </div>
    </Card>
  )
}
```

- [ ] Create `StatTile.tsx` as above.
- [ ] In `SuperAdminDashboardPage.tsx`, change the import from `AdminSummaryCard` to `StatTile` and
      rename every `<AdminSummaryCard .../>` usage to `<StatTile .../>` (props are identical except
      the new optional `trend`, which stays unset).
- [ ] Confirm no other file imports `AdminSummaryCard` (`grep -r AdminSummaryCard src/`), then
      delete `components/admin/AdminSummaryCard.tsx`.
- [ ] Verify: `npm run build` — clean, no unused-import/unresolved-import errors.

---

## 1. OwnerDashboardPage

### Current data available
- `ITenantRevenue` (via `useTenantRevenue(todayIso)`): `total`, `onlineAmount`, `payInVisitAmount`,
  `currencyCode`.
- `IRefundLogEntry[]` (via `useRefundLog()`): `bookingReference`, `amount`, `currencyCode`,
  `paymentMethodType`, `processedAt`.
- `IStaffPerformanceEntry[]` (via `useStaffPerformance(todayIso)`): `name`, `servicesCompleted`,
  `revenueGenerated`, `currencyCode`.
- `ITenantBooking[]` (via `useTenantBookings`, twice: the owner's own bookings for
  `StaffLineupTimeline`, and today's scheduled bookings for the cancel-picker modal).

### Current presentation
Four full-width-or-half-width stacked sections: a `col-12` card with revenue as a headline number
plus a prose sentence for the online/pay-on-visit split; a `col-md-6` + `col-md-6` pair (Refund Log
list, Staff Performance list); a `col-12` personal lineup; a `col-12` Quick Tools button row.

### Problems with the current layout
- Revenue — the one place on this page that's genuinely a "metric" — is buried inside a sentence
  (`Online: X · Pay on Visit: Y`) instead of being scannable.
- A single number occupying a full-width card is the clearest empty-space offender on this
  dashboard.
- Staff Performance is unsorted (display order = whatever the hook returns) and unlabeled by
  criterion — already flagged and confirmed in the roadmap: needs `by Revenue` in the heading and
  an explicit sort.

### What existing data should be regrouped
`ITenantRevenue`'s three fields become three discrete `StatTile`s in one row, replacing the single
prose card — no new data, just three already-fetched numbers shown as three numbers instead of one
number plus a sentence.

### Recommended component/layout structure

```tsx
<PageHeader title="Business Overview" description={TODAY_LABEL} />

<div className="row g-3 mb-3">
  <div className="col-4">
    <StatTile label="Total Revenue" value={`${revenue?.total.toFixed(2) ?? '0.00'} ${revenue?.currencyCode ?? ''}`} icon="chart" tone="primary" />
  </div>
  <div className="col-4">
    <StatTile label="Online" value={`${revenue?.onlineAmount.toFixed(2) ?? '0.00'} ${revenue?.currencyCode ?? ''}`} icon="globe" tone="success" />
  </div>
  <div className="col-4">
    <StatTile label="Pay on Visit" value={`${revenue?.payInVisitAmount.toFixed(2) ?? '0.00'} ${revenue?.currencyCode ?? ''}`} icon="qr-code" tone="neutral" />
  </div>
</div>
{/* isRevenueLoading -> three <Skeleton> tiles instead; !revenue -> the existing failure text, unchanged */}

<div className="row g-3 mb-3">
  <div className="col-12 col-md-6">
    <Card className="h-100">{/* Refund Log — unchanged from today */}</Card>
  </div>
  <div className="col-12 col-md-6">
    <Card className="h-100">
      <h2 className="fs-6 fw-semibold mb-3">Staff Performance by Revenue</h2>
      {/* same list, now over [...staffPerformance].sort((a, b) => b.revenueGenerated - a.revenueGenerated) */}
    </Card>
  </div>
</div>

<div className="row g-3 mb-3">
  <div className="col-12"><Card>{/* My Personal Lineup — unchanged */}</Card></div>
</div>
<div className="row g-3">
  <div className="col-12"><Card>{/* Quick Tools — unchanged */}</Card></div>
</div>
```

### Desktop behavior
Three revenue tiles share one row (`col-4` each, always three-across at every width ≥ the
container's own responsiveness — see the mobile decision below). Refund Log / Staff Performance
keep their existing `col-md-6` pairing. Lineup and Quick Tools stay full-width, unchanged.

### Mobile behavior
Refund Log, Staff Performance, Lineup, and Quick Tools already stack correctly today (no change
needed). The revenue row is the one open question — see **Decision 1** below.

### Components reused
`Card`, `PageHeader`, `EmptyState`, `Button`, `StaffLineupTimeline`, `Skeleton` (for the loading
state, replacing the current plain "Loading…" text to match the tile shape while loading).

### Components needing modification
None beyond the page file itself.

### Components created (only where reuse was justified)
Only `StatTile` (shared across all four dashboards, not created per-page).

### Data fetched but currently unused
None on this page — `myBookings`, `todaysBookings`, `refundLog`, `revenue`, `staffPerformance` are
all already consumed.

### Decisions requiring your approval
1. **Revenue tile grid on mobile** — `col-4` keeps all three tiles on one row at every width,
   including 320px phones, which is tight for currency text (e.g. "1,250.00 PHP"). The alternative
   is `col-6 col-md-4` (two tiles per row on phones, with "Pay on Visit" wrapping to its own row;
   three per row from tablet up) — safer on very narrow phones, less visually "grouped" as one set.
   I'd default to `col-4` and adjust if real currency values turn out to wrap, but this is a real
   trade-off worth your call before I build it.
2. **Whether "Total Revenue" should be visually emphasized** over Online/Pay-on-Visit (larger tile,
   or a `col-12` headline tile with the other two as a smaller pair beneath it) instead of three
   equal-weight tiles. I'd default to equal-weight (simplest, matches how Super Admin's tiles all
   read as one set) unless you'd rather the total read as the clear headline number.

---

## 2. StaffDashboardPage

### Current data available
`ITenantBooking[]` for the signed-in staff member today (via `useTenantBookings`), and
`ICustomerNote | null` for whichever booking is currently active (via `useCustomerLatestNote`).
That's the entire data surface — no counts, no revenue, nothing numeric.

### Current presentation
Three full-width stacked cards: My Daily Lineup, Client Preferences, Quick Tools.

### Problems with the current layout
Purely a horizontal-space problem, not an information problem: three narrow, stacked, full-width
cards on a wide desktop viewport, two of which (Client Preferences, Quick Tools) are short enough
to sit side-by-side without crowding.

### What existing data should be regrouped
Nothing — there's no numeric data to compact into tiles, and per your explicit direction this page
does **not** get KPI rows manufactured for it. This is a pure layout-grouping change, zero new
presentation elements.

### Recommended component/layout structure

```tsx
<PageHeader title="My Day" description={TODAY_LABEL} />

<div className="row g-3 mb-3">
  <div className="col-12"><Card>{/* My Daily Lineup — unchanged */}</Card></div>
</div>

<div className="row g-3">
  <div className="col-12 col-lg-8">
    <Card className="h-100">{/* Client Preferences — unchanged content */}</Card>
  </div>
  <div className="col-12 col-lg-4">
    <Card className="h-100">{/* Quick Tools — unchanged content */}</Card>
  </div>
</div>
```

### Desktop behavior
At ≥992px, Client Preferences (wide) and Quick Tools (narrow) sit side by side beneath the full-width
Lineup, instead of three separate stacked rows.

### Mobile behavior
Identical to today — both cards fall back to full width and stack, since `col-12` is the base for
both.

### Components reused
`Card`, `PageHeader`, `EmptyState`, `Button`, `StaffLineupTimeline` — every one unchanged.

### Components needing modification
None. Only the page file's grid `className`s change.

### Components created
None.

### Data fetched but currently unused
None.

### Decisions requiring your approval
3. **Whether to pair Client Preferences + Quick Tools at all.** This is the one dashboard where your
   instructions specifically caution against forcing structure onto data that doesn't support it —
   I read that as being about *metrics*, not plain Bootstrap-grid regrouping of existing cards, but
   given how explicit that caution was for this page specifically, I want your confirmation before
   changing its layout, even though no new UI element is introduced.

---

## 3. AdminDashboardPage

### Current data available
`ITenantBookingCounts` (`pending`, `checkedIn`, `completed`, `missed`) via
`useTenantBookingCounts`; `IIdleStaffMember[]` via `useIdleStaff`; `ITeamMember[]` +
`ITenantBooking[]` for `MasterVisualGrid` via `useActiveStaff`/`useTenantBookings`.

### Current presentation
Master Visual Grid (`col-12`, a horizontal-scroll per-staff schedule — not a chart, already
confirmed in the original Step 6 inspection), Daily Booking Counters + Idle Staff (`col-md-6`
pair), Quick Tools (`col-12`, four buttons).

### Problems with the current layout
Fewer than the other three dashboards — this is already the closest to the target pattern
(confirmed in the original pre-work). The "Daily Booking Counters" card is *already* a compact,
single-card 4-number row (`row g-2 text-center`, `fs-4 fw-bold` per value) — exactly the shape your
brief's own worked example asks for. Per your explicit instruction, this plan **refines, not
replaces** it.

### What existing data should be regrouped
Nothing needs regrouping — `pending`/`checkedIn`/`completed`/`missed` already share one row inside
one card, which is the correct shape.

### Recommended component/layout structure
No structural change. The only proposed edit is cosmetic: the four counter labels currently use a
different typographic weight (`text-muted small`) than `StatTile`'s label style elsewhere on the
redesigned dashboards — I'd leave this exactly as-is rather than invent a matching treatment, since
forcing this bespoke 4-across mini-grid to visually imitate `StatTile` (which is a full standalone
`Card` per metric) would mean either wrapping four `StatTile`s inside this card — a card-inside-card
nesting problem — or partially reimplementing `StatTile`'s look by hand, which is exactly the kind
of un-reused duplication Step 6 is supposed to avoid. **This dashboard's plan is: leave it
unchanged**, beyond whatever page-level spacing consistency falls out naturally from matching the
other three dashboards' row/column gap conventions (`row g-3`, already in use here).

### Desktop behavior
Unchanged from today.

### Mobile behavior
Unchanged from today — already stacks correctly (`col-md-6` → full width below 768px).

### Components reused
`Card`, `PageHeader`, `EmptyState`, `Button`, `MasterVisualGrid` — all unchanged.

### Components needing modification
None.

### Components created
None.

### Data fetched but currently unused
None.

### Decisions requiring your approval
4. **Optional, not proposed by default:** adding tone-colored value text to the four counters
   (e.g. Missed in a muted-danger tone, Completed in success) to aid quick scanning. I'm flagging
   this because it's a real, visible idea that came up while reviewing the page, not because I
   think it's necessary — your brief cautions against decorative/excessive color, and the counters
   already read clearly in neutral bold text. Default: **no change.** Tell me if you'd like it
   explored.

---

## 4. SuperAdminDashboardPage

### Current data available
`IPlatformDashboardSummary`: `totalTenants`, `activeTenants`, `pendingRequests`,
`approvedRequests` **(fetched, never rendered)**, `rejectedRequests`, `bookingsToday`,
`bookingsThisMonth`. Plus `ITenantRequest[]` (paginated, 5 most recent) via `useTenantRequests`.

### Current presentation
Six `StatTile`-shaped cards (today: `AdminSummaryCard`) across two rows — Total/Active/Pending/
Rejected (`col-6 col-lg-3`, 4-per-row desktop), then Bookings Today/This Month (`col-6 col-lg-3`,
2-per-row) — followed by a Recent Tenant Requests table.

### Problems with the current layout
The only real problem is the one your brief already named: `approvedRequests` is fetched by
`useDashboardSummary` and never shown. The existing 4-then-2 split was really just "however many
fit two-per-row," not a deliberate grouping — adding a 7th tile is a natural point to group by
actual meaning instead.

### What existing data should be regrouped
All five tenant-request-lifecycle numbers (Total, Active, Pending, **Approved**, Rejected) belong
together as one set; the two booking-activity numbers (Today, This Month) are a different kind of
metric and stay a separate, second set. This is a regrouping of existing fields only —
`approvedRequests` was already being fetched, it just never had a tile.

### Recommended component/layout structure

```tsx
<PageHeader title="Platform Overview" description="Monitor tenant requests and platform activity." />

<div className="row g-3 mb-3 row-cols-2 row-cols-md-3 row-cols-lg-5">
  <div className="col"><StatTile label="Total Tenants" value={summary?.totalTenants ?? 0} icon="tenant-requests" tone="primary" /></div>
  <div className="col"><StatTile label="Active Tenants" value={summary?.activeTenants ?? 0} icon="check-circle" tone="success" /></div>
  <div className="col"><StatTile label="Pending Requests" value={summary?.pendingRequests ?? 0} icon="clock" tone="warning" /></div>
  <div className="col"><StatTile label="Approved Requests" value={summary?.approvedRequests ?? 0} icon="check-circle" tone="success" /></div>
  <div className="col"><StatTile label="Rejected Requests" value={summary?.rejectedRequests ?? 0} icon="x-circle" tone="danger" /></div>
</div>

<div className="row g-3 mb-3">
  <div className="col-6 col-lg-3"><StatTile label="Bookings Today" value={summary?.bookingsToday ?? 0} icon="appointments" tone="primary" /></div>
  <div className="col-6 col-lg-3"><StatTile label="Bookings This Month" value={summary?.bookingsThisMonth ?? 0} icon="calendar" tone="neutral" /></div>
</div>

<Card>{/* Recent Tenant Requests — unchanged */}</Card>
```

`row-cols-2 row-cols-md-3 row-cols-lg-5` is Bootstrap's native equal-width-column utility — used
here specifically because 5 tiles don't divide evenly into the usual 12-column `col-lg-3`/`col-lg-4`
scheme; this avoids hand-computing fractional widths.

`Approved Requests` reuses the `check-circle` icon (same as Active Tenants) rather than introducing
a new one — matches the icon this app already uses for "approved" everywhere else (every
`RequestStatusBadge`/`TimeOffStatusBadge`/`RefundRequestStatusBadge` "Approved" state uses the same
icon), so this isn't a compromise, it's consistency with an existing convention.

### Desktop behavior
5 tenant-lifecycle tiles on one row at ≥992px (3-per-row at ≥768px, 2-per-row below that); the two
booking-activity tiles keep their existing 4-per-row-capable row (only 2 present, so effectively
2-per-row at every width ≥576px).

### Mobile behavior
2-per-row for the first group (one tile wraps to its own row: 5 is odd), full stacking not needed
until below ~360px given tile compactness. Second group unchanged (already mobile-safe today).

### Components reused
`Card`, `PageHeader`, `Button`, `TenantRequestTable`, `TenantRequestDetailsModal`, `useDashboardSummary`, `useTenantRequests` — all unchanged, no hook or prop changes.

### Components needing modification
`SuperAdminDashboardPage.tsx` — swap `AdminSummaryCard` → `StatTile`, add the `Approved Requests`
tile, change the row grouping/classes as shown above.

### Components created
None new here — `StatTile` is the one cross-cutting addition, already covered above.

### Data fetched but currently unused
`approvedRequests` — becomes used by this plan. After this change, every field on
`IPlatformDashboardSummary` is rendered somewhere.

### Decisions requiring your approval
5. **The 5-tile `row-cols` regrouping itself** — confirm you want Total/Active/Pending/Approved/
   Rejected visually grouped as one row (my recommendation) rather than the more conservative
   option of just appending Approved Requests as a lone 7th tile after the existing two rows,
   leaving the current 4-then-2 split untouched.

---

## Self-review

**Spec coverage:** every dashboard covered against all 10 requested sub-sections (current data /
current presentation / problems / regrouping / structure / desktop / mobile / reused / modified /
created / unused data), plus the cross-cutting `StatTile` task. Confirmed decisions from the
roadmap (§8) are followed: Staff Performance sorted + relabeled, `approvedRequests` surfaced, no new
metrics/charts, Admin's counters refined not replaced, Staff Dashboard not given manufactured KPIs.

**Placeholder scan:** none — every code block above is real, complete JSX, not a description of
what to build.

**No backend blocker identified.** Every field this plan renders is already present in an existing,
already-fetched interface — no DTO change, no new endpoint, no new query parameter is required for
any of the four dashboards.

---

## All five decisions needing your approval, collected

1. Revenue tile grid width on Owner Dashboard mobile (`col-4` vs `col-6 col-md-4`).
2. Whether "Total Revenue" should be visually emphasized over Online/Pay-on-Visit, or equal-weight.
3. Whether to pair Client Preferences + Quick Tools on Staff Dashboard at all.
4. Whether to explore tone-colored counter values on Admin Dashboard (default: no).
5. Whether to regroup Super Admin's tiles into a 5-tile `row-cols` row, or just append Approved
   Requests as a 7th tile after the existing 4-then-2 split.

Say the word (with answers to the above, or "use your defaults") to have me turn this into the
task-by-task execution plan and start Step 6 for real.
