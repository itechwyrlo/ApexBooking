# Admin Dashboard — Phase 4: Master Visual Grid

## Context

Eighth and final Admin Dashboard sub-project in the role-based dashboards rework:

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard (Phases 1–3) — done.
3. Admin Dashboard Phase 1 ([design](2026-08-13-admin-dashboard-phase1-counters-idle-staff-design.md)) — done.
4. Admin Dashboard Phase 2 ([design](2026-08-13-admin-dashboard-phase2-reassign-barber-design.md)) — done.
5. Admin Dashboard Phase 3 ([design](2026-08-13-admin-dashboard-phase3-collect-pay-on-visit-design.md)) — done.
6. **Admin Dashboard Phase 4** (this spec) — Master Visual Grid.

Once this lands, the Admin Dashboard is fully built out and the only remaining sub-project in the whole rework is the Owner Dashboard.

## Scope decisions

**Simple columns, not a proportional timeline/Gantt grid.** "Master Visual Grid" could mean a real scheduling grid (bookings positioned by actual clock time, gaps visible as blank space — duration math, pixel-per-minute layout, overlap handling) or something much simpler: one column per staff member, each just a chronological list of their bookings today. Going with the simpler reading — it satisfies the spec's literal wording ("a multi-column schedule layout where each column represents an active staff member's time slots for the day") without the much larger from-scratch build a true timeline grid would need, and it reuses an already-built component almost verbatim (see below).

**Read-only, no inline actions.** The grid is a report — "Master Visual Grid" is listed under "Reports to Show" in the original spec, separate from "Quick Tools." No Admit/Reassign/Collect Pay buttons on grid cards; those stay in the existing Quick Tools modals (Phases 1–3). A future phase could add inline actions later without any backend changes, reusing what already exists.

## Zero backend changes

This is the first phase in the entire dashboards rework that needs no backend work at all:
- The bookings data is already fetched — `AdminDashboardPage.tsx` already calls `useTenantBookings({ status: Scheduled, fromDate: today, toDate: today })` for the Reassign Barber and Collect Pay on Visit pickers (Phases 2–3). The grid reuses that exact same `todaysBookings` array, grouped client-side by `staffId` (already present on `ITenantBooking` since Phase 2).
- The per-column rendering is already built — `StaffLineupTimeline` (`src/components/dashboard/StaffLineupTimeline.tsx`, built in Staff Dashboard Phase 1) already renders exactly "a chronological list of bookings: time, service, customer, status badge" for one staff member. Reused as-is, no changes.
- The only new data need is the roster of active staff members (to know which columns to render, including staff with zero bookings today — an empty column is meaningful, it shows who's free). `teamService.getTeamMembers()` already exists and already returns `status`; fetched with a large page size (the same "fetch everything in one page" pattern `CalendarPage` already uses for its month-range bookings fetch) and filtered client-side to `status === 'Active'`.

## Frontend

- New hook `useActiveStaff()` — calls `getTeamMembers({ pageSize: 1000 })`, filters to `status === TenantMemberStatus.Active`, sorts by `firstName`. Returns `{ staff: ITeamMember[]; isLoading: boolean }`.
- New component `MasterVisualGrid.tsx` — props `{ staff: ITeamMember[]; bookings: ITenantBooking[]; isLoading: boolean }`. Renders a horizontally-scrolling row of fixed-width columns, one per staff member (name + photo header), each column body being `<StaffLineupTimeline bookings={bookings.filter(b => b.staffId === member.id)} isLoading={isLoading} />`. An empty roster (no active staff at all) falls back to the existing `EmptyState`.
- `AdminDashboardPage.tsx`: the "Master Visual Grid" placeholder is replaced with the real component, fed `staff` from the new hook and the page's already-existing `todaysBookings`.

## Out of scope (deferred)

- Proportional/time-positioned layout (see scope decision above).
- Inline per-card actions (see scope decision above).
- Branch filtering — tenant-wide for v1, consistent with every other Admin Dashboard widget's scope decision so far (Counters, Idle Staff).
- Horizontal scroll performance/virtualization for tenants with very large teams — not a concern at expected roster sizes.

## Testing

No test runner configured. Verification is manual: as Owner/Admin, confirm the grid shows one column per active team member (including any with zero bookings today, shown as an empty state), each column's bookings match what `AppointmentsPage` shows for that staff member on the same day, and confirm a deactivated team member does not get a column.
