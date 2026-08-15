# Owner Dashboard — Phase 3: Staff Performance List

## Context

Eleventh and final sub-project in the role-based dashboards rework:

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard (Phases 1–3) — done.
3. Admin Dashboard (Phases 1–4) — done.
4. Owner Dashboard Phase 1 ([design](2026-08-13-owner-dashboard-phase1-refund-log-lineup-quick-tools-design.md)) — done.
5. Owner Dashboard Phase 2 ([design](2026-08-14-owner-dashboard-phase2-revenue-design.md)) — done.
6. **Owner Dashboard Phase 3** (this spec) — Staff Performance List. Once this lands, all three role-based dashboards are fully built out.

## Scope

Per the original spec: "A high-level list ranking all connected staff members by services completed and revenue generated. (Note: If the owner is a solo operator, this list simply shows just their own name and stats)." Scoped to today, matching the "one consistent 'today' across every widget" convention every prior phase established (Daily Booking Counters, Total Shop Revenue, My Personal Lineup).

**Every active staff member appears**, including those with zero completed bookings today (shown at zero) — this is what makes the "solo operator just sees their own stats" case fall out naturally, with no special-case branching: a solo tenant simply has one active `TenantMember` row. Sorted by revenue generated, descending (services-completed count is shown alongside, not used as the primary sort key).

Revenue per staff member uses the same net-of-refund calculation Total Shop Revenue already established: `AmountDue - (RefundStatus == Succeeded ? RefundedAmount ?? 0 : 0)`, summed over that staff member's `Completed` bookings scheduled today.

## Backend

- New DTO `StaffPerformanceEntryDto(Guid TenantMemberId, string Name, int ServicesCompleted, decimal RevenueGenerated, string CurrencyCode)`.
- New repository method `ITenantRepository.GetStaffPerformanceAsync(TenantId, DateOnly, CancellationToken)` — a single query starting from `context.Staffs` (active members only), using the already-EF-mapped `TenantMember.Appointments` navigation (confirmed: `TenantMemberConfiguration.cs` has `builder.HasMany(tm => tm.Appointments)`) as correlated subqueries for the count and the netted sum, scoped to `ScheduledDate == date && Status == Completed`. Same shape as `GetIdleStaffAsync`'s use of the `MemberServices` navigation — no manual join needed.
- New query `GetStaffPerformanceQuery(DateOnly Date) : IQuery<IReadOnlyCollection<StaffPerformanceEntryDto>>`, handler sorts by `RevenueGenerated` descending.
- New route `GET api/Tenant/team/performance?date=yyyy-MM-dd`, `[Authorize(Roles = "Owner")]` — financial data, matching Revenue and Refund Log.

## Frontend

- New interface `IStaffPerformanceEntry`, new service `getStaffPerformance(date)` in `teamService.ts`, new hook `useStaffPerformance(date)`.
- `OwnerDashboardPage.tsx`'s "Staff Performance" placeholder becomes a ranked list (name, services completed, revenue), same list density as the Refund Log/Idle Staff widgets already on this page.

## Out of scope (deferred)

- Nothing — this is the last planned phase of the entire role-based dashboards rework.

## Testing

No test runner configured in either repo. Verification is manual: as Owner, confirm the list includes every active team member (even ones with zero completed bookings today), confirm the services-completed count and revenue figure match today's actual completed bookings for each staff member (cross-check against `AppointmentsPage`), and confirm sort order is revenue descending.
