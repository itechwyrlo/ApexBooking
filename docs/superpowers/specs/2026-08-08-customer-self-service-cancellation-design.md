# Customer Self-Service Booking Cancellation (via Email Link)

## Context

The confirmation email has no cancellation link today. `Booking.Cancel(adminUserId, reason)`
([Booking.cs](../../../ApexBooking.Core.Domain/Entities/Booking.cs)) is staff-only — no
customer-facing path exists. `BookingPolicy.CancellationCutoffHours` and
`LateCancellationPolicy` ([BookingPolicy.cs](../../../ApexBooking.Core.Domain/Entities/BookingPolicy.cs))
already exist as tenant-configurable settings but are never read by anything.
`PaymentPolicy.RefundPercent` is similarly configured but completely unused anywhere in
the codebase.

One relevant find: `IAppUrlService.GetGuestCancellationUrl(tenantSlug, rawToken)`
([IAppUrlService.cs](../../../ApexBooking.Core.Domain/Interfaces/IAppUrlService.cs)) already
exists with **zero callers** — a leftover from before the Guest→Customer domain rename,
building a stale `/book/{slug}/cancel-booking` path that doesn't match this app's current
routing (`/:slug/book`, not `/book/{slug}`). Renamed and repurposed below rather than left
dead or duplicated.

## Decisions (confirmed with user)

- Three separate passes, this spec covers only the first: **cancel token + policy-gated
  self-service cancellation, no refund processing**. Refund processing (wiring
  `LateCancellationPolicy`/`RefundPercent` into an actual decision, plus the real PayMongo
  refund API call — which needs its own research pass, same shape as the original PayMongo
  integration work) and reschedule are separate, later specs.
- **Pay-in-visit bookings** (`PaymentConfirmedVia` null/`PayInVisit`, nothing charged yet)
  just cancel — no refund policy logic applies to them at all, in this pass or the next.
  Cancellation still respects `CancellationCutoffHours` regardless of payment status.
- The cancellation token is a **new, independent** credential — never the admission ticket
  token. That token is now printed on the receipt and embedded as a QR in the confirmation
  email; reusing it for cancellation would let anyone who obtained either one cancel a
  booking that isn't theirs to cancel.

## Backend design (ApexBooking)

### New independent token service

Same deterministic-HMAC shape as `HmacTicketTokenService`, own signing key
(`Security:CancellationTokenSigningKey`, same ≥32-char validation), own payload (just
`BookingId + TenantId` — no `BranchId` needed, cancellation has no branch-scanning fraud
concern the way admission does):

```csharp
public interface ICancellationTokenService
{
    string Issue(CancellationTokenPayload payload);
    bool TryValidate(string token, out CancellationTokenPayload payload);
}

public readonly record struct CancellationTokenPayload(BookingId BookingId, TenantId TenantId);
```

`HmacCancellationTokenService` (Infrastructure) mirrors `HmacTicketTokenService.cs`
exactly — same base64url packing, same truncated-signature approach, same no-expiry
determinism (regenerable anywhere with the three IDs, no storage table needed, same as the
admission token).

### `Booking.CancelByCustomer(string reason)`

New method, parallel to the existing staff-only `Cancel(adminUserId, reason)` — same
`Status == Scheduled` guard, same `CancellationReason`/`CancelledAt` fields, but
`CancelledByUserId` stays null (no staff account initiated this). Requires
`BookingCancelledDomainEvent.CancelledByUserId` to become `Guid?` (currently non-nullable)
— its one other raise site (`Booking.Cancel`) is unaffected, still always passes a real
value there.

### `Tenant.CancelBookingByCustomer(bookingId, reason)`

```csharp
public void CancelBookingByCustomer(Guid bookingId, string reason)
{
    var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
        ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

    var scheduledAt = booking.ScheduledDate.ToDateTime(booking.ScheduledStartTime);
    var cutoffHours = BookingPolicy?.CancellationCutoffHours ?? 0;
    if (DateTime.UtcNow.AddHours(cutoffHours) > scheduledAt)
        throw new BusinessRuleBrokenException(
            $"This booking can no longer be cancelled online — it's within {cutoffHours} hour(s) of the appointment. Please contact the business directly.");

    booking.CancelByCustomer(reason);
    this.UpdatedAt = DateTime.UtcNow;
}
```

(Timezone note: compares in UTC against `ScheduledDate`/`ScheduledStartTime`, which are
branch-local per existing convention elsewhere in this codebase — acceptable
first-pass precision for a notice-window check measured in hours, not minutes; flagged for
whoever implements to confirm against how `BranchTimeZoneConverter` is used in the
booking-creation advance-notice check, for consistency.)

### New Application layer: `Features/PublicBookings/{Queries/GetCancellableBooking,Commands/CancelBookingByToken}/`

- `GetCancellableBookingQuery(string Token)` → `CancellableBookingDto(string BookingReference, string ServiceName, string StaffName, string BranchName, DateOnly ScheduledDate, TimeOnly ScheduledStartTime, bool CanCancelOnline, string? UnavailableReason)`. Validates the token, loads the booking, computes `CanCancelOnline` using the same cutoff math as the domain guard (so the frontend can show the right state without needing to attempt-and-fail). `UnavailableReason` follows the same naming convention `GetAvailableSlotsHandler`'s `SlotUnavailabilityReason` already established elsewhere in this codebase — populated with a human-readable reason (`"past-cutoff"` / `"already-{status}"` style value, mapped to copy client-side, mirroring `BuildUnavailableReason` in `GetWalkInAvailableStaffHandler`) whenever `CanCancelOnline` is `false`, null otherwise.
- `CancelBookingByTokenCommand(string Token, string? Reason)` → validates token, resolves tenant, sets ambient tenant context (`ITenantService.SetCurrentTenant`, same pattern `GetPublicBranchesHandler`/friends rely on via `TenantMiddleware`'s slug-based resolution — except here there's no `{slug}` route segment at all, so the handler sets it directly from the token's `TenantId`, matching how `GetBookingStatusByTicketHandler` already bypasses slug-based resolution entirely), calls `tenant.CancelBookingByCustomer(bookingId, reason ?? "Cancelled by customer via online request")`.

### `IAppUrlService`

Rename `GetGuestCancellationUrl` → `GetCustomerCancellationUrl` (no callers to break), fix
the stale path:

```csharp
public string GetCustomerCancellationUrl(string tenantSlug, string rawToken)
{
    var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
    return $"{base_}/{Uri.EscapeDataString(tenantSlug)}/cancel-booking?token={Uri.EscapeDataString(rawToken)}";
}
```

### `BookingsController.cs`

Two new actions, absolute routes (un-prefixed by slug — tenant resolves from the token,
same reasoning as the existing status/qr actions):

```csharp
[HttpGet("/api/public/bookings/cancel/{token}")]
public async Task<IActionResult> GetCancellableBooking([FromRoute] string token, CancellationToken ct)
{
    var result = await _mediator.Send(new GetCancellableBookingQuery(token), ct);
    return Ok(result);
}

[HttpPost("/api/public/bookings/cancel")]
public async Task<IActionResult> CancelBooking([FromBody] CancelBookingByTokenCommand command, CancellationToken ct)
{
    await _mediator.Send(command, ct);
    return NoContent();
}
```

### `SendBookingConfirmationEmailHandler` / `BookingNotificationService`

Same deterministic-regeneration trick as the QR token: issue the cancellation token from
`(BookingId, TenantId)`, build the URL via `GetCustomerCancellationUrl(tenant.Slug, token)`,
add a "Cancel this booking" link/button to the existing HTML template — a plain `<a href>`,
no image/CID concerns here since it's just a link, not embedded content.

## Frontend design (LocalFlow)

- New page, `pages/public/CancelBookingPage.tsx`, route `/:slug/cancel-booking` (reads
  `?token=` from the query string) — mirrors `PublicBookingLayout`'s branded shell without
  the wizard machinery.
- Fetches the preview (`GET .../cancel/{token}`) on mount: shows booking details, and
  either a "Cancel My Booking" button + optional reason textarea (if `canCancelOnline`), or
  an explanatory message ("This booking can no longer be cancelled online — please contact
  [business]") when it's past the cutoff or already resolved.
- On confirm: `POST .../cancel`, then a clear success state ("Your booking has been
  cancelled").
- New interfaces (`ICancellableBooking`, mirroring `CancellableBookingDto`) and service
  functions (`getCancellableBooking`, `cancelBooking`) in a new or existing public-booking
  service file, same `publicClient` pattern as the rest of the wizard's public calls.

## Non-goals

No refund calculation or PayMongo refund API call (next spec). No reschedule (spec after
that). No change to the existing staff-side `Cancel`/`CancelBookingCommand` path. No tenant
toggle to disable self-service cancellation entirely — the link is always present in the
email, gated only by the cutoff check at click time.

## Testing

- Cancel via token well before the cutoff → succeeds, `CancelledByUserId` null,
  `BookingCancelledDomainEvent` fires (confirm existing subscribers — e.g. any
  tenant-facing "booking cancelled" notification — handle a null `CancelledByUserId`
  gracefully).
- Cancel via token inside the cutoff window → rejected with the policy-explaining message,
  booking unchanged.
- Cancel an already-`Cancelled`/`Completed`/`NoShow` booking via token → rejected,
  `CanCancelOnline: false` on the preview so the frontend never even shows the button.
- Tampered/invalid token → same "invalid or could not be verified" treatment as the
  existing ticket-token validation.
- Pay-in-visit booking cancelled online → succeeds identically to a paid one, no refund
  fields touched (there are none yet in this pass).
