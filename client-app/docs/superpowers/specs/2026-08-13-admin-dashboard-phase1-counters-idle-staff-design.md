# Admin Dashboard — Phase 1: Daily Booking Counters, Idle Staff Alert, Quick Tools Wiring

## Context

Fifth sub-project in the role-based dashboards rework, first of four Admin Dashboard phases:

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard (Phases 1–3) — done: TenantMemberId claim, My Daily Lineup, Chair Notes, Block My Time.
3. **Admin Dashboard Phase 1** (this spec) — Daily Booking Counters, Idle Staff Alert, wiring the two already-built Quick Tools.
4. Admin Dashboard Phase 2 — Reassign Barber.
5. Admin Dashboard Phase 3 — Collect Pay on Visit.
6. Admin Dashboard Phase 4 — Master Visual Grid.

`AdminDashboardPage.tsx` already exists as a skeleton (from Foundation) with section headers and disabled placeholders for all five Admin Dashboard pieces.

## Scope decision: "Unassigned Bookings Alert" redefined as "Idle Staff Alert"

The original spec text — "highlighting any online bookings that do not have a staff member assigned yet" — has no possible matches under the current booking domain model. The public booking wizard always resolves a booking to one specific, concretely-chosen `StaffId` before it's created (confirmed: `InitiateBookingCommand.StaffId` is required and non-nullable; there is no "no preference"/auto-assign path anywhere). A widget built to that literal spec would sit permanently empty.

After clarifying with the business owner, the actually-intended alert is different: **active team members who haven't been assigned to any service yet** (`TenantMember.MemberServices` is empty) — a roster/configuration gap, not a booking-triage queue. A staff member in this state is invisible to the public booking wizard (which only lists staff already qualified for a service) and can never receive a booking until an Owner/Admin assigns them to at least one service. This is real, checkable today, and needs no changes to booking creation.

The "Unassigned Bookings" card on `AdminDashboardPage.tsx` is renamed "Idle Staff" and repurposed accordingly. Adding a genuine "no preference" booking option (the literal original reading) is out of scope — it would be a booking-creation change, not a dashboard widget, and would need its own separate design if ever wanted.

## Backend

### Daily Booking Counters

New query `GetTenantBookingCountsQuery(DateOnly Date) : IQuery<TenantBookingCountsDto>`. Buckets are derived from existing `Booking` fields — no new data, no schema change:

| Bucket | Condition |
|---|---|
| Pending | `Status == Scheduled && CheckedInAt == null` |
| Checked-In | `Status == Scheduled && CheckedInAt != null` |
| Completed | `Status == Completed` |
| Missed | `Status == NoShow` |

(`Cancelled` and `PendingPayment` bookings are excluded from all four buckets — they aren't part of "today's operational schedule" in the sense this widget cares about.)

- New DTO `TenantBookingCountsDto(int Pending, int CheckedIn, int Completed, int Missed)`.
- New repository method `ITenantRepository.GetBookingCountsAsync(TenantId, DateOnly, CancellationToken)`, implemented as four separate `CountAsync` calls scoped to `TenantId` + `ScheduledDate == date`, following the exact style already used by `PlatformQueries.GetDashboardCountsAsync` (a flat sequence of predicate-scoped counts, not a single `GroupBy`).
- New route `GET api/Tenant/bookings/counts?date=yyyy-MM-dd`, default `ManagementOnly` policy (Owner/Admin/platform_admin) — no override needed, Staff doesn't need this.

### Idle Staff Alert

New query `GetIdleStaffQuery : IQuery<IReadOnlyCollection<IdleStaffDto>>`. No parameters — always scoped to the caller's own tenant, always "right now" (no date dimension; a staff member either has qualifying services assigned or doesn't).

- New DTO `IdleStaffDto(Guid TenantMemberId, string Name, string? PhotoUrl)`.
- New repository method `ITenantRepository.GetIdleStaffAsync(TenantId, CancellationToken)`: `context.Staffs.Where(m => m.TenantId == tenantId && m.IsActive && !m.MemberServices.Any())`, projected to a row DTO. `TenantMember.MemberServices` is a real, directly-navigable collection (not requiring the aggregate-load-with-ThenInclude workaround `GetWithServiceStaffAsync` needs for the reverse direction, `Service.ServiceProviders`) — this is a flat, single-entity query against `context.Staffs` (the DbSet backing `TenantMember`), not a full `Tenant` aggregate load.
- New route `GET api/Tenant/team/idle`, default `ManagementOnly` policy.

## Frontend

- New interfaces `ITenantBookingCounts` (`{ pending, checkedIn, completed, missed }`) and `IIdleStaffMember` (`{ tenantMemberId, name, photoUrl }`).
- `bookingService.ts` gains `getTenantBookingCounts(date: string): Promise<ITenantBookingCounts>`.
- `teamService.ts` gains `getIdleStaff(): Promise<IIdleStaffMember[]>`.
- Two new hooks, `useTenantBookingCounts(date)` and `useIdleStaff()`, following the established `useState`/`useEffect`/loading/error shape used throughout this session's dashboard work.
- `AdminDashboardPage.tsx`:
  - "Daily Booking Counters" placeholder replaced with four small stat tiles (Pending/Checked-In/Completed/Missed).
  - "Unassigned Bookings" card renamed "Idle Staff," placeholder replaced with a short list of idle team members (name + photo), or an empty state ("Every active team member is assigned to at least one service.") when there are none.
  - "Scan Booking QR" and "Quick Walk-In" buttons become enabled, each opening the existing `AdmitScanModal`/`NewWalkInModal` components (already built and used on `AppointmentsPage`, fully self-contained — no changes needed to either component). Their completion callbacks (`onAdmitted`/`onScheduled`) trigger a refetch of the booking counts, since admitting or creating a booking changes them.
  - "Reassign Barber" and "Collect Pay on Visit" remain disabled placeholders — Phases 2 and 3.

## Out of scope (deferred)

- Reassign Barber (Phase 2).
- Collect Pay on Visit (Phase 3).
- Master Visual Grid (Phase 4).
- Branch filtering on the counters/idle-staff widgets (tenant-wide for v1; the original spec doesn't call for per-branch scoping here).
- Any UI for an Owner/Admin to fix an idle staff member's service assignment from this card — the alert links nowhere yet; it just informs. (The existing Team page already has the tools to add service assignments.)

## Testing

No test runner configured in either repo. Verification is manual: as Owner/Admin, confirm the counters match reality for today's bookings (check one in via Scan QR or Appointments, confirm the Checked-In tile increments), create a walk-in via the dashboard's Quick Walk-In button and confirm it appears, and confirm a team member with no assigned services shows up under Idle Staff (and disappears once assigned one, via the existing Team page).
