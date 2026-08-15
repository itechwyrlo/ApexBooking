# Owner Dashboard — Phase 1: Refund Log, My Personal Lineup, Quick Tools

## Context

Ninth sub-project in the role-based dashboards rework, first of the Owner Dashboard phases (the last dashboard in the whole rework):

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard (Phases 1–3) — done.
3. Admin Dashboard (Phases 1–4) — done.
4. **Owner Dashboard Phase 1** (this spec) — Refund Log, My Personal Lineup, Scan Booking QR, Cancel & Refund.
5. Owner Dashboard Phase 2 — Total Shop Revenue.
6. Owner Dashboard Phase 3 — Staff Performance List.

## Decided: "Online Payout Status" is dropped entirely, not deferred

Investigated and confirmed with the business owner: there's no PayMongo API to predict a future payout date, and even an after-the-fact "last payout received" record would require a webhook whose availability isn't confirmed for the account tier — genuine new backend work with an unconfirmed foundation. Per explicit instruction, this widget is removed from `OwnerDashboardPage.tsx` entirely (not left as a placeholder) — done as part of this spec's groundwork, ahead of the rest of Phase 1. The Owner Dashboard now has four report widgets instead of five.

## Backend

### Refund Log

The refund-review system built earlier this session only has a query for *pending* refunds (`GetPendingRefundRequestsQuery`, backing the existing Refunds review page) — nothing returns terminal/processed ones.

- New method on `IRefundRequestStore` (or a filter param added to the existing store, whichever keeps `RefundRequestsController`'s existing pending-refunds usage unaffected): returns `RefundRequest`s with `Status` in `Succeeded`/`ManuallyRefunded`, most recent first, joined to `Booking` for `BookingReference`/`PaymentMethodType`/`PayMongoPaymentId` — same join shape `GetPendingRefundRequestsHandler` already does for `BookingReference`/`AmountDue`.
- New query `GetRefundLogQuery : IQuery<IReadOnlyCollection<RefundLogEntryDto>>`, DTO `RefundLogEntryDto(Guid RefundRequestId, string BookingReference, decimal Amount, string CurrencyCode, string? PaymentMethodType, DateTime ProcessedAt)` (`ProcessedAt` = `UpdatedAt`, the timestamp that actually flips when `MarkSucceeded()`/`MarkManuallyRefunded()` run).
- New route on the existing `RefundRequestsController`, Owner-only (financial data) — e.g. `GET api/refund-requests/log`.

### My Personal Lineup

No backend work — the `tenant_member_id` claim and `GetTenantBookingsQuery`'s `staffId` filter already work identically regardless of role.

### Cancel & Refund

No backend work — `Booking.Cancel()`/`CancelBookingCommand` (already built, already used by `CancelBookingModal`) already evaluates and triggers the refund automatically.

### Scan Booking QR

No backend work — reuses the existing scan-arrival endpoint via `AdmitScanModal`, unchanged.

## Frontend

- New interface `IRefundLogEntry`, new service `getRefundLog(): Promise<IRefundLogEntry[]>` in `refundRequestService.ts`, new hook `useRefundLog()`.
- `OwnerDashboardPage.tsx`'s "Refund Log" placeholder becomes a simple list (reference, amount, date, payment method) — table-free, matching the density of Admin's "Idle Staff" list — or the existing `EmptyState` when there are none.
- `OwnerDashboardPage.tsx`'s "My Personal Lineup" placeholder becomes real: `useTenantBookings({ staffId: user.tenantMemberId, fromDate: today, toDate: today })` + `StaffLineupTimeline`, exactly as the Staff Dashboard does it. Per the original spec's "conditional" framing, if there are zero bookings for the Owner today, the existing `EmptyState` covers that — no separate show/hide logic needed beyond what `StaffLineupTimeline` already does.
- "Scan Booking QR" Quick Tools button becomes enabled, opens `AdmitScanModal` (unchanged), exactly like Admin Phase 1.
- "Cancel & Refund" Quick Tools button becomes enabled: opens a small picker (list today's `Scheduled` bookings, tenant-wide — Owner sees everything, not staff-scoped), pick one, which then opens the existing `CancelBookingModal` with that booking — same two-step "pick a booking, then act" shape as Reassign Barber, except step two reuses an existing component instead of new UI.

## Out of scope (deferred)

- Total Shop Revenue (Phase 2).
- Staff Performance List (Phase 3).
- Online Payout Status — dropped entirely, not deferred (see above).
- Pagination/filtering on the Refund Log (shows recent entries only, matching the density of other dashboard list widgets built this session).

## Testing

No test runner configured in either repo. Verification is manual: process a refund to `Succeeded`/`ManuallyRefunded` via the existing Refunds page, confirm it appears in the new Refund Log with correct amount/date/method. As Owner, confirm "My Personal Lineup" shows real bookings if any are assigned to the Owner's own `TenantMemberId` today, or the empty state otherwise. Use "Scan Booking QR" and "Cancel & Refund" and confirm both work end-to-end (the latter should trigger the same refund evaluation already verified earlier this session).
