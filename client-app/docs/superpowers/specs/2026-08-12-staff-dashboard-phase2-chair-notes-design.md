# Staff Dashboard — Phase 2: Chair Notes

## Context

Third sub-project in the role-based dashboards rework, second of three Staff Dashboard phases:

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard Phase 1 ([design](2026-08-12-staff-dashboard-phase1-lineup-design.md)) — done: TenantMemberId claim + My Daily Lineup Timeline.
3. **Staff Dashboard Phase 2** (this spec) — Chair Notes: Save Chair Notes tool + Client Preference View.
4. Staff Dashboard Phase 3 — Block My Time.

## Two scope decisions made up front

**Trigger for saving a note:** the original spec says the notes modal "pops up when a service ends." We're deliberately *not* doing that — it would mean intercepting the Complete-booking action wherever it's triggered today (`AppointmentsPage`, `CalendarPage`), both shared across all three roles, for a feature that only matters to Staff. Instead, "Save Chair Notes" stays a manual button on the Staff Dashboard: it opens a modal listing the staff member's own completed bookings from today, they pick one and write the note. Contained entirely to the Staff Dashboard.

**Scope of the preview:** "Client Preference View" shows past notes "inside the active appointment card" (singular). We're showing it for exactly one appointment — the "active" one (see below) — not annotating every row of the daily lineup. This avoids touching the shared bookings-list query's per-row shape for every consumer (Appointments/Calendar included); it's one extra, targeted lookup instead.

**"Active appointment" definition:** from today's lineup (already fetched for Phase 1's timeline), the one currently checked-in (`checkedInAt` set, status still `Scheduled`) — i.e. genuinely in progress. If none is checked in, fall back to the next upcoming `Scheduled` booking (not yet checked in), soonest first. If neither exists (nothing scheduled today, or everything's already completed/cancelled), there's no active appointment and the preview area shows its empty state.

## Backend

### New: staff-authored notes on a Booking

`Booking.CustomerNotes` (existing) is set once at creation and never touched again — it's a pre-visit note, usually from the customer. Chair notes are a *different* concept: post-service, staff-authored, mutable. New field:

- `Booking.StaffNotes` (`string?`, private set) — new nullable field.
- New mutator `Booking.SetStaffNotes(string notes)`:
  - Throws `BusinessRuleBrokenException` if `Status != BookingStatus.Completed` ("Chair notes can only be added to a completed appointment.").
  - Throws if `notes` is null/whitespace.
  - Sets `StaffNotes = notes.Trim()`, `UpdatedAt = DateTime.UtcNow`. No domain event — this is descriptive data, nothing reacts to it.
- `Tenant.SetBookingStaffNotes(Guid bookingId, string notes)` — same delegation shape as the existing `Tenant.CompleteBooking(Guid bookingId)`: find the booking in `_bookings`, call `booking.SetStaffNotes(notes)`.
- New command `SetBookingStaffNotesCommand(Guid BookingId, string Notes) : ICommand`, handler follows `CompleteBookingCommandHandler`'s exact shape (load `Tenant` with `Bookings` included, delegate, `Update`, `CompleteAsync`).
- New route `POST api/Tenant/bookings/{bookingId}/staff-notes`, body `{ notes: string }`, `[Authorize(Roles = "Owner,Admin,Staff")]` (broadened past the controller's default `ManagementOnly`, same as the existing walk-in/scan-arrival/search-customers routes — any staff on shift needs this, not just Owner/Admin).

### New: latest-note lookup for a customer

- New query `GetCustomerLatestNoteQuery(Guid CustomerId) : IQuery<CustomerLatestNoteDto?>`.
- New repository method `ITenantRepository.GetLatestStaffNoteAsync(TenantId, CustomerId, CancellationToken)`, living alongside `GetCustomerBookingsPageAsync` (same rationale already documented there: `Booking` is a `Tenant` child, not a `Customer` child, so this belongs on `ITenantRepository`). Query: bookings for this tenant + customer, `Status == Completed`, `StaffNotes != null`, ordered by `ScheduledDate`/`ScheduledStartTime` descending, first result.
- New DTO `CustomerLatestNoteDto(string Notes, DateOnly NotedOn)`.
- New route `GET api/Tenant/customers/{customerId}/latest-note` → the DTO, or `204 No Content` if there's no past note. Same broadened `[Authorize(Roles = "Owner,Admin,Staff")]` as above.

### Required plumbing: CustomerId on the shared bookings-list DTO

`TenantBookingSummary`/`TenantBookingRow` (backing `GET api/Tenant/bookings`, used by Appointments, Calendar, and Phase 1's Staff Dashboard lineup) currently has no `CustomerId` field at all — only `CustomerName`/`CustomerPhone`. The frontend needs it to know which customer to call `latest-note` for. Appended as a new field at the end of both records (additive, doesn't reorder any existing positional construction), sourced from `b.CustomerId.Value` in the existing `TenantRepository.GetBookingsPageAsync` projection (the join already has `b` in scope — no new join needed).

## Frontend

- `ITenantBooking` gains `customerId: string`.
- `bookingService.ts` gains `setBookingStaffNotes(bookingId: string, notes: string): Promise<void>`.
- `customerService.ts` gains `getCustomerLatestNote(customerId: string): Promise<ICustomerLatestNote | null>` and a new `ICustomerLatestNote` interface (`{ notes: string; notedOn: string }`), following the exact wire-mapping pattern already used there for `getCustomerBookings`.
- New hook `useCustomerLatestNote(customerId: string | null)` — same shape as `useTenantBookings` (loading/error/data), skips the fetch entirely when `customerId` is null.
- New component `SaveChairNotesModal.tsx` — props: `bookings: ITenantBooking[]` (the staff member's own completed bookings from today, computed by the parent), `onClose`, `onSaved`. A select to pick which booking, a textarea for the note, calls `setBookingStaffNotes` on submit.
- `StaffDashboardPage.tsx`:
  - Computes the "active" booking from the already-fetched `bookings` array per the definition above.
  - Renders the "Client Preferences" card using `useCustomerLatestNote(activeBooking?.customerId ?? null)` — shows the active customer's name + note preview if one exists, or an empty state (no active appointment, or no past note for this customer) otherwise.
  - "Save Chair Notes" Quick Tools button becomes enabled, opens `SaveChairNotesModal` with `bookings.filter(b => b.status === BookingStatus.Completed)`.

## Out of scope (deferred)

- Block My Time (Phase 3).
- Editing/deleting a previously-saved chair note.
- Showing notes anywhere other than the Staff Dashboard's single "active" card (e.g. not added to Appointments/Calendar/BookingDetailPanel in this phase).

## Testing

No test runner configured in either repo. Verification is manual: complete a booking as Staff, use "Save Chair Notes" to log a note, then create/view a new booking for the same customer and confirm the note preview appears on the active appointment card. Also confirm `dotnet build` / `npm run build` are clean.
