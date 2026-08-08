# Walk-In Auto-Admit on Creation

## Context

Walk-in bookings (`Tenant.ScheduleBooking`, created via the dashboard's "New Walk-in"
modal) never call `Booking.RecordArrival()` — `CheckedInAt` stays `null` regardless of
which slot was picked, so staff must separately admit (scan or manual admit) a customer
who, in the common case, is already standing at the counter being served immediately.

`GetWalkInAvailableStaffHandler.cs` already distinguishes two cases per staff member:
`IsAvailableNow` (can start on this customer literally right now) vs
`RecommendedTimeRaw`/`AlternateTimes` (which can be a later-today slot if the staff member
is currently busy). "Walk-in" doesn't always mean "starting this instant" — a customer can
walk in and be booked for an opening 45 minutes from now.

## Decision (confirmed with user)

Auto-admit only when the picked slot is genuinely the immediate one — not a blanket
"all walk-ins auto-admit." Condition, computed entirely from data the frontend already
has (no new API call): the selected staff member's `isAvailableNow === true` **and** the
chosen time equals that staff's `recommendedTimeRaw` (not one of their later "alternate"
times, which a busy-but-eligible staff member can also offer).

## Backend design (ApexBooking)

`ScheduleBookingCommand` gains a field:

```csharp
public record ScheduleBookingCommand(
    Guid BranchId,
    Guid StaffId,
    Guid ServiceId,
    string CustomerFirstName,
    string CustomerLastName,
    string? CustomerEmail,
    string? CustomerPhone,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    string? CustomerNotes,
    bool AdmitImmediately
) : ICommand<Guid>;
```

`ScheduleBookingHandler` forwards it: `tenant.ScheduleBooking(..., admitImmediately: command.AdmitImmediately)`.

`Tenant.ScheduleBooking` gains `admitImmediately`, forwarded into the shared `PlaceBooking`
(which gains an optional `bool admitImmediately = false` — the public wizard's
`PlaceCustomerBooking` never passes it, so that path is unaffected). Inside `PlaceBooking`,
right after `Booking.Create(...)` and adding it to `_bookings`:

```csharp
if (admitImmediately)
    booking.RecordArrival();
```

`RecordArrival()` is already `internal` (accessible from `Tenant`, same project), already
guarded (`Status == Scheduled`, which a freshly-created walk-in always is), already
idempotent. No change needed to `Booking.cs` itself.

## Frontend design (LocalFlow)

`IScheduleBookingValues` gains `admitImmediately: boolean`.

`NewWalkInModal.tsx`'s `handleSubmit`, right before calling `scheduleBooking`:

```ts
const admitImmediately = selectedStaff?.isAvailableNow === true && selectedTime === selectedStaff.recommendedTimeRaw
await scheduleBooking({ ...values, admitImmediately, scheduledDate: date, scheduledStartTime: selectedTime })
```

## Non-goals

No change to the public booking wizard (`PlaceCustomerBooking`), `RecordBookingArrival`'s
arrival-scan path, or the manual admit flow — all unaffected. No new UI control (checkbox,
toggle) — the condition is derived, not user-facing, matching how the "Recommended" badge
on `WalkInStaffCard` is already derived rather than manually set.

## Testing

- Walk-in created via "available now" recommended slot → `CheckedInAt` set at creation.
- Walk-in created via an alternate (later-today) slot on a busy-but-eligible staff member →
  `CheckedInAt` stays null, admitted later through the normal flow.
- Public wizard booking (any payment policy) → unaffected, `CheckedInAt` still null at
  creation as before.
