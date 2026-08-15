# Admin Dashboard — Phase 3: Collect Pay on Visit

## Context

Seventh sub-project in the role-based dashboards rework, third of four Admin Dashboard phases:

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard (Phases 1–3) — done.
3. Admin Dashboard Phase 1 ([design](2026-08-13-admin-dashboard-phase1-counters-idle-staff-design.md)) — done: Daily Booking Counters, Idle Staff, Scan QR / Quick Walk-In wiring.
4. Admin Dashboard Phase 2 ([design](2026-08-13-admin-dashboard-phase2-reassign-barber-design.md)) — done: Reassign Barber.
5. **Admin Dashboard Phase 3** (this spec) — Collect Pay on Visit.
6. Admin Dashboard Phase 4 — Master Visual Grid.

## Problem

Per the original spec: "Changes an appointment's payment status from unpaid to paid_in_shop upon cash/card collection." Today, `Booking.PaymentConfirmedVia` is only ever set in two places: `ConfirmPayment(...)` (gated to `Status == PendingPayment`, the online-webhook path only) and `CompleteService()` (auto-sets `PayInVisit` at completion time, but only if nothing was captured earlier). There's no way for staff to record a cash/card payment *before* the visit ends — e.g. collecting payment at check-in for a walk-in, or mid-visit. "Collect Pay on Visit" fills that gap.

## Authorization

Owner/Admin only — same scope decision as Reassign Barber (confirmed by the business owner, staying consistent rather than broadening to Staff). Uses the `TenantController` class-level default `ManagementOnly` policy, no `[Authorize]` override, matching `CompleteBooking`/`CancelBooking`/`MarkBookingNoShow`/`ReassignBooking`.

## Backend

- New mutator `Booking.RecordPayInVisitPayment()` — guarded to `Status == BookingStatus.Scheduled` (can't record on a finished/cancelled/no-show booking) and `PaymentConfirmedVia == null` (can't double-record, or overwrite an online payment). Sets `PaymentConfirmedVia = PaymentConfirmationMethod.PayInVisit`, `UpdatedAt = DateTime.UtcNow`, and raises the same `PaymentCapturedDomainEvent` that `CompleteService()`'s auto-capture branch already raises — this mutator just lets that capture happen earlier, at the front desk, instead of implicitly at completion time. Same event, same downstream handling, no new domain-event consumers needed.
- New `Tenant.RecordBookingPayment(Guid bookingId)` — thin delegate, same shape as `Tenant.CompleteBooking`/`Tenant.SetBookingStaffNotes`.
- New command `RecordPayInVisitPaymentCommand(Guid BookingId) : ICommand`, handler loads the `Tenant` aggregate the same lightweight way `CompleteBookingCommandHandler`/`SetBookingStaffNotesCommandHandler` do (`GetAsync(predicate: t => t.TenantId == tenantId, includes: t => t.Bookings)` — no cross-collection validation needed here, unlike Reassign Barber, so no need for the heavier `GetForWalkInAvailabilityAsync`).
- New route `POST api/Tenant/bookings/{bookingId:guid}/collect-payment`, default `ManagementOnly` policy.

## Frontend

No new DTO fields needed — `ITenantBooking.paymentConfirmedVia` and `.status` already exist from prior work, sufficient to identify eligible bookings client-side.

- `bookingService.ts` gains `collectPayment(bookingId): Promise<void>`.
- New component `CollectPaymentModal.tsx` — simpler than `ReassignBookingModal` (single-step, no second selection): lists today's `Scheduled` bookings with `paymentConfirmedVia === null` (filtered from the same `todaysBookings` list `AdminDashboardPage.tsx` already fetches for Reassign Barber), pick one, shows the amount due, confirm.
- `AdminDashboardPage.tsx`: "Collect Pay on Visit" Quick Tools button (the last remaining disabled placeholder on this page besides Master Visual Grid) becomes enabled, opens `CollectPaymentModal`; on success, refetches booking counts (consistent with the other Quick Tools' refetch-on-completion behavior, even though payment collection doesn't change the Pending/Checked-In/Completed/Missed buckets — kept for consistency and because a future counters revision might care about payment status).

## Out of scope (deferred)

- Master Visual Grid (Phase 4) — the last remaining Admin Dashboard piece; once built, it could gain this same collect-payment action directly on its per-booking cards, reusing this phase's backend without changes.
- Card-vs-cash distinction — the domain model only has one `PayInVisit` value; the spec's "cash/card collection" phrasing doesn't imply the system needs to track which.
- Partial payments / split payments.

## Testing

No test runner configured in either repo. Verification is manual: as Owner/Admin, use "Collect Pay on Visit" on a scheduled walk-in with no payment recorded, confirm it succeeds and the booking's payment status shows Pay-in-Visit afterward (e.g. via `AppointmentsPage`'s payment summary badge). Confirm a booking that's already paid online doesn't appear in the picker list.
