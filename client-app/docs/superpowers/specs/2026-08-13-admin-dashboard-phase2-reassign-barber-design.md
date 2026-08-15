# Admin Dashboard — Phase 2: Reassign Barber

## Context

Sixth sub-project in the role-based dashboards rework, second of four Admin Dashboard phases:

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard (Phases 1–3) — done.
3. Admin Dashboard Phase 1 ([design](2026-08-13-admin-dashboard-phase1-counters-idle-staff-design.md)) — done: Daily Booking Counters, Idle Staff, Scan QR / Quick Walk-In wiring.
4. **Admin Dashboard Phase 2** (this spec) — Reassign Barber.
5. Admin Dashboard Phase 3 — Collect Pay on Visit.
6. Admin Dashboard Phase 4 — Master Visual Grid.

## Scope decision: Quick Tools modal, not a per-booking-card action

The original spec text — "a fast selector on any booking card" — implies the action lives wherever a booking is displayed. But "Reassign Barber" inherently needs to start from a *specific* booking, while the Admin Dashboard's Quick Tools button is generic (no booking pre-selected when clicked). Rather than adding a new action to the shared `BookingDetailPanel`/`BookingTable` components (used by `AppointmentsPage`/`CalendarPage`, reachable by all three roles), this follows the exact "pick from a list, then act" pattern already established and approved for Chair Notes (Staff Dashboard Phase 2's `SaveChairNotesModal`): the "Reassign Barber" Quick Tools button opens a modal listing today's currently-`Scheduled` bookings (tenant-wide, not staff-scoped, since Admin/Owner see everything) — reusing the existing `useTenantBookings` hook/filter, no new list query needed — pick one, then pick a new staff member, submit. Keeps this phase contained to `AdminDashboardPage.tsx`, consistent with the rest of this session's dashboard work, and matches Reassign Barber's Owner/Admin-only authorization (Staff never sees this tool anyway).

## Scope decision: qualification-only staff list, not live slot-availability

`GetWalkInAvailableStaffQuery` (already built, used by Quick Walk-In) does full real-time slot-availability checking (shift hours, breaks, time off, existing-booking collisions) — but it has no date/time parameter; it implicitly means "right now," which is correct for a walk-in but wrong for reassigning a booking scheduled on an arbitrary future date. Building a new query with full availability math for arbitrary dates is out of scope for this phase. Instead, the reassignable-staff list is qualification-only: active, deployed to the booking's branch, and assigned to the booking's service — the same predicate the public wizard's staff-picker (`GetBookableStaffQuery`) already uses, just resolved server-side from a `BookingId` instead of taking `branchId`/`serviceId` as caller-supplied parameters. This matches "fast selector," not a full scheduler — an Owner/Admin doing this manually is expected to notice an obvious conflict; no new automated conflict-prevention is being added.

## Backend

### StaffId on the shared bookings-list DTO

`TenantBookingRow`/`TenantBookingSummary` gains `Guid StaffId`, appended the same way `CustomerId` was added in Chair Notes — the frontend needs it to identify the currently-assigned staff member.

### Reassignment

- New mutator `Booking.SetStaffNotes`-style precedent: `Booking.Reassign(TenantMemberId newStaffId)` — guarded to `Status == BookingStatus.Scheduled` (a completed/cancelled/no-show booking can't be reassigned), sets `StaffId = newStaffId`, `UpdatedAt = DateTime.UtcNow`. No domain event — nothing currently reacts to a reassignment.
- New `Tenant.ReassignBooking(Guid bookingId, Guid newStaffId)` — the aggregate-level validation, since it needs cross-collection consistency (`Bookings`, `Members`, `Services`) that only `Tenant` can see all of at once:
  1. Find the booking in `Bookings`; throw if not found.
  2. Find the new staff member in `Members`; throw if not found or not active.
  3. Validate `newStaffMember.BranchId == booking.BranchId`.
  4. Validate the new staff is qualified: `Services.First(s => s.ServiceId == booking.ServiceId).ServiceProviders.Any(sp => sp.TenantMemberId == newStaffMember.TenantMemberId)`.
  5. Delegate: `booking.Reassign(newStaffMember.TenantMemberId)`.
- New command `ReassignBookingCommand(Guid BookingId, Guid NewStaffId) : ICommand`, handler loads the `Tenant` aggregate via the existing `GetForWalkInAvailabilityAsync` (already loads exactly `Branches` + `Members` + `Services.ServiceProviders` (ThenInclude) + `Bookings` in one query — everything this validation needs, no new aggregate-load method required), delegates to `tenant.ReassignBooking(...)`, saves.
- New route `POST api/Tenant/bookings/{bookingId:guid}/reassign`, body `{ newStaffId }`, default `ManagementOnly` policy (no `[Authorize]` override — Owner/Admin only, matching `CompleteBooking`/`CancelBooking`/`MarkNoShow`'s existing default, and matching "Reassign Barber" being an Admin Dashboard–only tool).

### Reassignable staff list

- New query `GetReassignableStaffQuery(Guid BookingId) : IQuery<IReadOnlyCollection<ReassignableStaffDto>>` — no branch/service parameters from the caller; the handler resolves them from the booking itself.
- Handler loads `Tenant` via the same `GetForWalkInAvailabilityAsync`, finds the booking, filters `Members` the same way `GetBookableStaffHandler` already does (active + same branch + qualified for the service), maps to `ReassignableStaffDto(Guid TenantMemberId, string Name)`.
- New route `GET api/Tenant/bookings/{bookingId:guid}/reassignable-staff`, default `ManagementOnly` policy.

## Frontend

- `ITenantBooking` gains `staffId: string`.
- New interface `IReassignableStaffMember { tenantMemberId: string; name: string }`.
- `bookingService.ts` gains `getReassignableStaff(bookingId): Promise<IReassignableStaffMember[]>` and `reassignBooking(bookingId, newStaffId): Promise<void>`.
- New component `ReassignBookingModal.tsx` — two-step, both inside one modal: (1) pick a booking from today's `Scheduled` bookings (a select, showing customer + service + current staff name — reusing data already available via `useTenantBookings` in `AdminDashboardPage.tsx`, passed in as a prop, no new list-fetching inside the modal), (2) once a booking is picked, fetch its reassignable-staff list and show a second select for the new staff member. Submitting calls `reassignBooking`.
- `AdminDashboardPage.tsx`: "Reassign Barber" Quick Tools button becomes enabled, opens `ReassignBookingModal` with `bookings.filter(b => b.status === BookingStatus.Scheduled)`; on success, refetches the booking counts (a reassignment doesn't change the Pending/Checked-In/Completed/Missed buckets, but refetching keeps the dashboard's data fresh with minimal extra cost, consistent with how Scan QR/Quick Walk-In already refetch counts on their own completion).

## Out of scope (deferred)

- Collect Pay on Visit (Phase 3).
- Master Visual Grid (Phase 4) — once built, it could also gain a reassign selector directly on its per-staff-column cards, reusing this phase's backend (`ReassignBookingCommand`/`GetReassignableStaffQuery`) without any backend changes.
- Live slot-availability checking for the new staff member (see scope decision above).
- Notifying the customer or either staff member (old/new) about the reassignment.

## Testing

No test runner configured in either repo. Verification is manual: as Owner/Admin, use "Reassign Barber," pick one of today's scheduled bookings, confirm only branch-deployed staff qualified for that booking's service appear as options, reassign it, and confirm the booking now shows the new staff member (e.g. by reloading `AppointmentsPage`/`CalendarPage`). Also confirm a Staff-role session gets a 403 if it somehow calls the new routes directly (they're not exposed in the Staff Dashboard UI, but the backend authorization is the real gate).
