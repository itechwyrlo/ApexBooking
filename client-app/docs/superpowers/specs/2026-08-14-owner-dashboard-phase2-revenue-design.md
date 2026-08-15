# Owner Dashboard — Phase 2: Total Shop Revenue

## Context

Tenth sub-project in the role-based dashboards rework, second of the Owner Dashboard phases:

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard (Phases 1–3) — done.
3. Admin Dashboard (Phases 1–4) — done.
4. Owner Dashboard Phase 1 ([design](2026-08-13-owner-dashboard-phase1-refund-log-lineup-quick-tools-design.md)) — done.
5. **Owner Dashboard Phase 2** (this spec) — Total Shop Revenue.
6. Owner Dashboard Phase 3 — Staff Performance List (last phase of the entire rework).

## Calculation

Per booking today, net collected amount is `AmountDue` minus any succeeded refund: `AmountDue - (RefundStatus == Succeeded ? RefundedAmount ?? 0 : 0)`. This self-corrects for fully-refunded bookings (nets to zero) without needing a separate exclusion. Eligible bookings: `ScheduledDate == today`, `Status != Cancelled`, `PaymentConfirmedVia != null` (something was actually collected — excludes bookings still `PendingPayment` or never confirmed). Split into two subtotals by `PaymentConfirmedVia` (`Online` / `PayInVisit`), matching the original spec's "(Online + Pay on Visit)" wording.

`ScheduledDate` is used as "today," not a payment-confirmation timestamp (none exists on `Booking`) — this matches the exact scoping convention Admin's Daily Booking Counters already established (`GetBookingCountsAsync`'s `ScheduledDate == date` filter), keeping a single consistent "today" definition across every dashboard widget in this rework.

## Backend

- New DTO `TenantRevenueDto(decimal OnlineAmount, decimal PayInVisitAmount, decimal Total, string CurrencyCode)`.
- New repository method `ITenantRepository.GetRevenueAsync(TenantId, DateOnly, CancellationToken)` — two scoped `SumAsync` calls (Online, PayInVisit) over the eligible-bookings predicate above, mirroring `GetBookingCountsAsync`'s flat-per-metric style. `CurrencyCode` taken from any matching booking today, falling back to `"PHP"` if there are none.
- New query `GetTenantRevenueQuery(DateOnly Date) : IQuery<TenantRevenueDto>`, handler sums the two subtotals into `Total`.
- New route `GET api/Tenant/bookings/revenue?date=yyyy-MM-dd`, `[Authorize(Roles = "Owner")]` — financial data, matching the Refund Log and payment-gateway-credentials precedent (not the broader Owner+Admin `ManagementOnly`).

## Frontend

- New interface `ITenantRevenue { onlineAmount, payInVisitAmount, total, currencyCode }`.
- `bookingService.ts` gains `getTenantRevenue(date): Promise<ITenantRevenue>`.
- New hook `useTenantRevenue(date)`.
- `OwnerDashboardPage.tsx`'s "Total Shop Revenue" placeholder becomes a large total figure with a small Online / Pay-on-Visit breakdown underneath.

## Out of scope (deferred)

- Staff Performance List (Phase 3, last phase of the entire rework).
- Date-range selection (today only, matching every other widget's scope decision).
- Branch filtering (tenant-wide, matching every other widget's scope decision).

## Testing

No test runner configured in either repo. Verification is manual: as Owner, confirm the total matches the sum of today's paid bookings (cross-check against `AppointmentsPage`), confirm a partially-refunded booking's contribution is netted correctly, and confirm the Online/Pay-on-Visit split adds up to the total.
