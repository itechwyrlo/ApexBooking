# Scan Feedback: Already-Admitted Boarding Pass

## Context

`Booking.RecordArrival()` is already idempotent — re-scanning an already-admitted booking
silently no-ops (`if (CheckedInAt is not null) return;`), no exception, no data corruption.
But `ScanArrivalResult` carries no signal of *which* happened, so `AdmitScanModal.tsx`
shows the identical `"${reference} admitted."` success toast whether this is the first
scan or a re-scan of an already-checked-in pass — staff get no feedback that nothing
actually changed.

## Decision (confirmed with user)

Not an error — a distinct **informational** message: `"{bookingReference} is already
admitted."` (info/neutral tone, not the danger tone used for actual scan failures like an
invalid or wrong-branch pass).

## Backend design (ApexBooking)

`Booking.RecordArrival()` returns `bool` — `true` if it admitted just now, `false` if it
was already checked in (no state change):

```csharp
internal bool RecordArrival()
{
    if (Status != BookingStatus.Scheduled)
        throw new BusinessRuleBrokenException("Only confirmed, scheduled appointments can be checked in.");

    if (CheckedInAt is not null) return false; // idempotent: re-scanning is harmless

    CheckedInAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
    return true;
}
```

`Tenant.RecordBookingArrival` captures it and passes it up (the caller already has the
`Booking` back, but the "did this call itself just admit" fact isn't otherwise
recoverable from the booking state alone once `CheckedInAt` is set either way):

```csharp
public (Booking Booking, bool WasFirstAdmission) RecordBookingArrival(BookingId bookingId, BranchId scannerBranchId)
{
    ...
    var wasFirstAdmission = booking.RecordArrival();
    this.UpdatedAt = DateTime.UtcNow;

    return (booking, wasFirstAdmission);
}
```

`ScanArrivalResult` gains the field:

```csharp
public record ScanArrivalResult(
    Guid BookingId,
    string BookingReference,
    string Status,
    DateTime CheckedInAt,
    bool WasFirstAdmission
);
```

`ScanArrivalHandler` and `AdmitBookingHandler` (both call `RecordBookingArrival`, both
already construct `ScanArrivalResult`) pass the tuple's `WasFirstAdmission` through. Same
fix covers both entry points — QR scan and the dashboard's manual admit action.

## Frontend design (LocalFlow)

`IBookingArrival` ([ITenantBooking.ts](../../../../LocalFlow/src/interfaces/ITenantBooking.ts))
gains `wasFirstAdmission: boolean`.

`AdmitScanModal.tsx`'s `handleScan`, after a successful `scanArrival` call:

```ts
if (result.wasFirstAdmission) {
  showToast('success', `${result.bookingReference} admitted.`)
} else {
  showToast('info', `${result.bookingReference} is already admitted.`)
}
```

`'info'` is already a supported `ToastVariant` ([ToastContext.tsx](../../../../LocalFlow/src/contexts/ToastContext.tsx)) alongside `'success'`/`'error'`/`'warning'` — no new tone needed.

## Non-goals

No change to the underlying admission guard (`Status != Scheduled` still throws — a
completed/cancelled booking's pass still correctly fails, loudly, as an actual error).
No change to `AdmitBookingCommand`'s own request shape — only its shared result type.

## Testing

- Scan a fresh boarding pass → `WasFirstAdmission: true`, success toast, `CheckedInAt` set.
- Scan the same pass again → `WasFirstAdmission: false`, info toast quoting the *original*
  check-in time (not a new one), booking state unchanged.
- Scan a pass for a `Completed` booking → still throws the existing "Only confirmed,
  scheduled appointments can be checked in" error, unaffected by this change.
