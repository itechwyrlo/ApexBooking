# Booking QR Boarding Pass — Generation, Email, Receipt

## Context

The public booking wizard already issues a signed **ticket token** at booking creation
([InitiateBookingHandler.cs:164-165](../../../ApexBooking.Core.Application/Features/Bookings/Commands/InitiateBooking/InitiateBookingHandler.cs#L164-L165)),
via `HmacTicketTokenService`
([HmacTicketTokenService.cs](../../../ApexBooking.Infrastructure/Ticketing/HmacTicketTokenService.cs)) —
a base64url string packing `BookingId + TenantId + BranchId`, HMAC-SHA256-signed, with
**no expiry or nonce** (pure deterministic function of those three IDs). The scan side
already fully exists and expects exactly this raw string:
`AdmitScanModal.tsx` (`@yudiel/react-qr-scanner`) reads `results[0].rawValue` and passes
it straight to `scanArrival(token)` → `ScanArrivalHandler.TryValidate` → `RecordBookingArrival`.

What's missing: **nothing ever renders this token as an actual QR image**. No QR-image
library exists anywhere in the backend (`QRCoder`/`ZXing`/equivalent — none present), and
the frontend's `SuccessStep.tsx` displays booking details but no QR at all — confirmed
against the current UI screenshot. `BookingInitiationResult.TicketToken` reaches the
browser today but is never turned into anything scannable, and the confirmation email
(`SendBookingConfirmationEmailHandler.cs` → `BookingNotificationService.cs`) doesn't
reference the token at all.

## Decisions (confirmed with user)

- One backend-generated QR image, reused everywhere (success screen, receipt, email) —
  not a second client-side rendering for the on-screen copy. The email requires a real
  rendered image regardless (no JS in email), so generating it once server-side and
  reusing it avoids installing a second QR-rendering implementation purely for the
  on-screen copy, and guarantees the emailed and on-screen codes are pixel-identical.
- QR content is the **raw `ticketToken` string, unmodified** — not wrapped in a URL. This
  is what `AdmitScanModal.tsx`'s scanner already expects (`rawValue` passed directly to
  `scanArrival`); wrapping it in a URL would require also changing the scanner to parse
  the token back out, for no benefit.
- "Download Receipt" (replacing "Add to Calendar") is **browser print-to-PDF**
  (`window.print()` + a `@media print` stylesheet), not a new PDF/canvas-screenshot
  library. Zero new frontend dependencies.
- QR is presented as **optional** on the success screen and in the email — copy makes
  clear staff can look the booking up manually too (`AdmitBookingCommand` path already
  supports this, unchanged by this spec).

## Backend design (ApexBooking)

### New capability: `IQrCodeGenerator`

New dependency: **QRCoder** (NuGet, MIT, no network calls) — the one new package this
feature needs, same "narrowly-scoped new dependency, justified because hand-rendering
isn't otherwise possible" precedent as `@vite-pwa/assets-generator` in the landing-page
refactor.

`Domain/Services/Ticketing/IQrCodeGenerator.cs`:
```csharp
public interface IQrCodeGenerator
{
    byte[] GeneratePng(string content);
}
```

`Infrastructure/Ticketing/QrCoderQrCodeGenerator.cs` — wraps `QRCoder.PngByteQRCode` at a
fixed error-correction level (`ECCLevel.M`, standard default) and module size sized for
both screen display and print legibility. Registered in `InfrastructureDependencies.cs`
alongside the existing `ITicketTokenService` registration.

### Fix: `BookingScheduledDomainEvent` is missing `BranchId`

Required so the email handler (a domain-event side effect, no ambient HTTP context) can
regenerate the same deterministic token without an extra `Bookings` load. Add the field:

```csharp
public record BookingScheduledDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    Guid CustomerId,
    Guid StaffId,
    Guid ServiceId,
    Guid BranchId,               // NEW
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    DateTime ScheduledAt
) : IReliableDomainEvent;
```

Both raise sites in [Booking.cs](../../../ApexBooking.Core.Domain/Entities/Booking.cs)
pass the booking's own `BranchId`:
- `Create()`'s else-branch, [Booking.cs:150-160](../../../ApexBooking.Core.Domain/Entities/Booking.cs#L150-L160) — add `BranchId: booking.BranchId.Value`.
- `ConfirmPayment()`, [Booking.cs:177-187](../../../ApexBooking.Core.Domain/Entities/Booking.cs#L177-L187) — add `BranchId: this.BranchId.Value`.

`NotifyTenantOnBookingConfirmedHandler.cs` (the other subscriber) ignores the new field —
no change needed there.

Accepted side effect: this event also fires for admin-created walk-ins
(`Tenant.ScheduleBooking`), so a walk-in with an email on file gets the same QR in their
confirmation email even though they're already on-site. Harmless (unused by them, same
shared template) — not worth a special case.

### `InitiateBookingHandler` returns the QR image

Right after the existing ticket-issuance line
([InitiateBookingHandler.cs:165](../../../ApexBooking.Core.Application/Features/Bookings/Commands/InitiateBooking/InitiateBookingHandler.cs#L165)):

```csharp
var ticketToken = _ticketTokenService.Issue(new TicketPayload(booking.BookingId, tenant.TenantId, branch.BranchId));
var qrCodeDataUri = $"data:image/png;base64,{Convert.ToBase64String(_qrCodeGenerator.GeneratePng(ticketToken))}";
```

`BookingInitiationResult` ([BookingInitiationResult.cs](../../../ApexBooking.Core.Application/Dtos/Response/BookingInitiationResult.cs))
gains `string TicketQrCodeDataUri`, populated alongside `TicketToken` in the return
statement. `InitiateBookingHandler` takes `IQrCodeGenerator` as a new constructor
dependency.

### `SendBookingConfirmationEmailHandler` embeds the same QR

With `BranchId` now on the event, the handler regenerates the identical token + image
(deterministic ⇒ guaranteed match to whatever the customer already saw):

```csharp
var ticketToken = _ticketTokenService.Issue(new TicketPayload(new BookingId(e.BookingId), e.TenantId, new BranchId(e.BranchId)));
var qrCodePng = _qrCodeGenerator.GeneratePng(ticketToken);
```

Passed to a new parameter on `SendBookingConfirmationEmailAsync`. Handler gains
`ITicketTokenService` + `IQrCodeGenerator` constructor dependencies.

`IBookingNotificationService.cs` — `SendBookingConfirmationEmailAsync` gains a
`byte[] qrCodePng` parameter. `BookingNotificationService.cs` embeds it inline in the
existing HTML template (no attachment plumbing, no public image-hosting endpoint):

```csharp
<img src="data:image/png;base64,{Convert.ToBase64String(qrCodePng)}" alt="Boarding pass QR code" style="width:160px;height:160px;margin:12px 0;" />
<p style="margin:0;font-size:13px;color:#555;">Show this at check-in — or just give us your name, we can look you up too.</p>
```

placed inside the existing details box, under the booking reference.

`SendThankYouEmailAsync` (the post-completion email) is untouched — a QR for admission
makes no sense after the visit is already done.

## Frontend design (LocalFlow)

### `IBookingInitiationResult.ts`

```ts
export interface IBookingInitiationResult {
  bookingId: string
  bookingReference: string
  requiresPayment: boolean
  amountToPay: number
  payMongoQrCodeUrl: string | null
  payMongoCheckoutUrl: string | null
  ticketToken: string
  ticketQrCodeDataUri: string   // NEW
}
```

(`payMongoQrCodeUrl` is an unrelated, pre-existing field — PayMongo's own *payment* QR,
not this feature.)

### `SuccessStep.tsx`

Renders the QR inside the existing `.pb-ticket` card, after the booking-reference block:

```tsx
<div className="p-4 text-center">
  <img src={result.ticketQrCodeDataUri} alt="Boarding pass QR code" width={160} height={160} />
  <p className="pb-muted small mt-2 mb-0">
    Show this at check-in — or just give your name, our staff can look you up too.
  </p>
</div>
```

### "Add to Calendar" → "Download Receipt"

- Remove the "Add to Calendar" button and its `downloadBookingIcs` call
  ([SuccessStep.tsx](../../../../LocalFlow/src/components/publicBooking/SuccessStep.tsx),
  the `d-flex flex-column flex-sm-row gap-2 mt-3` action row). `downloadBookingIcs` has
  exactly one caller (this button) — removed from `utils/publicBookingActions.ts` too
  rather than left as dead code, matching this codebase's existing convention of trimming
  now-orphaned single-caller helpers. `buildDirectionsUrl` stays (still used by "Get
  Directions", which is unchanged).
- New "Download Receipt" button:
  ```tsx
  <button type="button" className="btn pb-btn-outline flex-fill d-inline-flex align-items-center justify-content-center gap-2" onClick={() => window.print()}>
    <Icon name="download" size={16} />
    Download Receipt
  </button>
  ```
  `download.svg` doesn't exist yet in `public/assets/icons/` — add it, matching the
  existing single-color stroke style (`#475569`) shared by every icon in this set,
  including the `calendar`/`branches` icons already used in this exact button row.
- `styles/publicBooking.css` gains a `@media print` block using the standard
  "print only this element" visibility pattern — robust regardless of surrounding
  layout, no markup restructuring needed:
  ```css
  @media print {
    body * { visibility: hidden; }
    .pb-ticket, .pb-ticket * { visibility: visible; }
    .pb-ticket { position: absolute; top: 0; left: 0; width: 100%; }
  }
  ```
  so "Save as PDF" in the print dialog produces a clean ticket-only page.

## Non-goals

No change to `AdmitScanModal.tsx` or `ScanArrivalHandler` — they already correctly expect
the raw token string; this feature makes that string visible, it doesn't change what it
is. No change to the manual-admit path (`AdmitBookingCommand`). No redesign of the email
template beyond the added image block. No token expiry/nonce added (out of scope — the
existing no-expiry design is intentional, matched by `RecordBookingArrival`'s own
`Status == Scheduled` guard doing the actual "can this still be used" check, not the
token itself).

## Testing

- `HmacTicketTokenService.Issue` called twice with identical `TicketPayload` produces
  identical output — verifies the regeneration approach in the email handler is safe
  (should already hold today; add a test making it explicit).
- `SendBookingConfirmationEmailHandler`: with `BranchId` now on the event, confirms the
  regenerated token matches what `InitiateBookingHandler` would have issued for the same
  booking (same three IDs in, same token out).
- Manual: complete a public booking end-to-end — confirm the success screen shows a QR,
  the confirmation email contains the same QR embedded, "Download Receipt" opens the
  print dialog with a clean ticket-only layout, and scanning the on-screen QR via
  `AdmitScanModal` still admits the booking (regression check — the token's shape/content
  hasn't changed, only who else can see it).
