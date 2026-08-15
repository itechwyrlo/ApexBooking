# Calendar Page Refactor — Phase 1: Desktop

**Date:** 2026-08-08
**Status:** Approved
**Scope:** Presentation layer + panel interaction state. No changes to booking data, status transition logic, or filtering — reuses existing `admitBooking`/`completeBooking`/`markBookingNoShow`/`CancelBookingModal`.

## Context

First of two phases refactoring the Calendar page (`src/pages/booking/CalendarPage.tsx`). Phase 2 covers the mobile agenda view and full-screen panel; it builds on this phase's color system, stepper, and panel content rather than duplicating them.

## Data Inspection (done first, per the prompt's own instruction)

`ITenantBooking` already carries every field needed — no backend gaps to flag:
- `createdAt`, `checkedInAt`, `serviceCompletedAt`, `cancelledAt`, `noShowAt` — one timestamp per transition.
- `cancellationReason` — present, nullable.

**Key nuance:** `BookingStatus` has only 5 values (`PendingPayment`, `Scheduled`, `Completed`, `NoShow`, `Cancelled`) — there is no `Admitted` status. "Admitted" is derived: a booking stays `Scheduled` and gains a non-null `checkedInAt`. The stepper and color system both key off `status` + `checkedInAt` + `serviceCompletedAt` together, not `status` alone.

**Actionable stepper decision:** `admitBooking`, `completeBooking`, `markBookingNoShow`, and `CancelBookingModal` already exist and are used by `AppointmentsPage` today with an established `runAction`/`pendingBookingId`/toast pattern. The calendar panel reuses these exact functions and mirrors that same conditional logic (admit+no-show+cancel available when `Scheduled` and not checked in; complete+cancel available when `Scheduled` and checked in; nothing available once `Completed`/`NoShow`/`Cancelled`) — no new business logic.

## Status/Color System

Six visual states, all built from existing `theme.css` tokens (no new colors invented):

| Display state | Condition | Token |
|---|---|---|
| Awaiting Payment | `status === PendingPayment` | `--color-teal` (defined, currently unused by any badge — new `badge-tone-teal` class) |
| Scheduled | `status === Scheduled && !checkedInAt` | `--color-primary` |
| Admitted | `status === Scheduled && checkedInAt && !serviceCompletedAt` | `--color-accent` (existing `warning` tone, already amber) |
| Completed | `status === Completed` | `--color-success` |
| No-show | `status === NoShow` | neutral/muted |
| Cancelled | `status === Cancelled` | `--color-danger` |

A shared `getBookingDisplayStatus(booking)` helper computes this once and is used by the chip, the legend, and the stepper, so the three can't drift out of sync.

Payment status renders as a small dot on the chip (filled = paid via `paymentConfirmedVia`, hollow = pending), not a 7th color — keeping booking-status and payment-status as two independent visual dimensions per the prompt.

## Legend

Compact, collapsed by default behind a small "Legend" toggle button next to the filter bar. Small color swatches + labels for the six states, plus the payment-dot key. Secondary in weight — small text, no card/border treatment competing with the filter bar itself.

## Hover / Cursor States

- Day cell: `cursor: pointer` (already conditional on having bookings) + light background tint on hover, via a new CSS rule rather than the current bare Bootstrap button reset.
- Chip: its own hover (darken + 1px lift + shadow), distinct from the cell's hover so users can tell whether they're targeting the cell or a specific booking.
- "+N more" becomes a real pill (styled, own hover state) instead of plain muted text — same click target as before (opens the day list) but now visually and semantically its own element (`<button>`, proper `aria-label`).

## Detail Panel — Two-Level Navigation

Today, `handleSelectDay` and `handleSelectBooking` both feed the same `BookingDetailList`, which always renders full detail cards for every booking it's given — clicking a day with 5 bookings shows 5 full stacked cards, not a summary list. The prompt asks for a genuine two-level interaction instead:

- **Day mode** (cell or "+N more" clicked): compact summary rows (time, customer, service, status chip) in a new `BookingDayList` component. Each row is tappable.
- **Booking mode** (single chip clicked directly, or a row tapped from day mode): full detail + stepper in a renamed/reworked `BookingDetailPanel` (was `BookingDetailList`, now single-booking only). When reached *from* day mode, shows a "← Back to {date}" link at the top that returns to the day list — within the same panel, no reopening.

`CalendarPage`'s panel state becomes a discriminated union:
```ts
type IPanelState =
  | { mode: 'day'; date: string; bookings: ITenantBooking[] }
  | { mode: 'booking'; booking: ITenantBooking; backToDay: { date: string; bookings: ITenantBooking[] } | null }
```

## Booking Status Stepper (`BookingDetailPanel`)

New `BookingStatusStepper` component, built for the dashboard's own token system (`theme.css`, `--color-primary`/`--color-accent`/etc.) — not a literal reuse of the public wizard's `.pb-stepper` classes, which are scoped under `.pb-root` and use a different accent token. Same visual *language* instead: circles, current-step highlight, connecting-line fill transition on advance, current-step ring — mirroring the technique already built for the public wizard's stepper in the earlier phase.

**Linear line (3 steps):** Scheduled (`createdAt`) → Admitted (`checkedInAt`) → Completed (`serviceCompletedAt`). Each completed step shows its timestamp underneath (e.g. "Admitted — Aug 14, 3:31 PM"). Steps with no timestamp yet just don't render one — no fabricated dates.

**Branch/end states**, rendered as a distinct callout instead of the linear line (never squeezed into it):
- Cancelled: `cancelledAt` timestamp + `cancellationReason` shown below it *only if present* (nullable — a booking can be cancelled without a reason on file).
- No-show: `noShowAt` timestamp.
- Awaiting Payment (`PendingPayment`): `createdAt` only, no admit/complete actions available (mirrors `AppointmentsPage`, which also excludes `PendingPayment` from those actions).

**Actions** render as buttons below the stepper, never on the stepper dots themselves (per the prompt's explicit instruction against mis-taps): "Mark as Admitted" / "Mark as No-show" / "Cancel" when scheduled-not-checked-in; "Mark as Completed" / "Cancel" when scheduled-and-checked-in. Wired to the same `admitBooking`/`completeBooking`/`markBookingNoShow`/`CancelBookingModal` `AppointmentsPage` already uses, with the same `runAction`/toast pattern, added to `CalendarPage.tsx`.

## SidePanel Motion

`SidePanel.tsx` has the exact "instant appear, no transition" problem `Modal.tsx` had before the dashboard modal-pattern phase. Same fix, same technique: keep the component mounted briefly during close to animate out, slide-in from the right + backdrop fade over 200-250ms ease-out, reversed on close, `prefers-reduced-motion` drops the slide in favor of a plain fade.

## Filter Bar

Previous/Today/Next already use the shared `Button` component, which already inherits the global `.btn:focus-visible` rule and Bootstrap's outline-secondary hover — already compliant, no change needed. Verified rather than assumed.

## Out of Scope (Phase 1)

- Mobile agenda/list view, view-switcher toggle, full-screen bottom-sheet panel — Phase 2.
- No changes to `useTenantBookings`, `bookingService.ts`, filtering logic, or any `ITenantBooking` field.
