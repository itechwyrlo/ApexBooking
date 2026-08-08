# Cancellation Refund Processing

## Context

Pass #1 (cancellation tokens, [2026-08-08-customer-self-service-cancellation-design.md](2026-08-08-customer-self-service-cancellation-design.md))
shipped cancellation with no refund logic. `BookingPolicy.CancellationCutoffHours`/`LateCancellationPolicy`
and `PaymentPolicy.RefundPercent` still exist unenforced. `IPayMongoService` has exactly
one method, `CreatePaymentSourceAsync` — no refund capability.

**Real blocker found during research:** nothing in this codebase ever captures PayMongo's
own Payment resource ID. `ProcessPaymentWebhookCommandHandler.cs` only extracts `Remarks`
(our own `BOOKING_{id}` tracking token) and `Status`, via `WebhookResource`
([PaymongoContracts.cs](../../../ApexBooking.Core.Domain/Services/Paymongo/PaymongoContracts.cs)),
which has no `Id` property mapped at all. Refunding requires PayMongo's `payment_id` — this
has to be captured before any refund call is possible.

### PayMongo API (researched — [Create a Refund](https://docs.paymongo.com/reference/create-a-refund), [Refund Resource](https://docs.paymongo.com/reference/refund-resource))

```
POST https://api.paymongo.com/refunds   (HTTP Basic auth, secret key)
```

| Field | Required | Notes |
|---|---|---|
| `amount` | yes | integer, centavos, min `100` |
| `payment_id` | yes | the PayMongo Payment resource — see blocker above |
| `reason` | yes | `duplicate` / `fraudulent` / `requested_by_customer` / `others` |
| `notes` | no | max 255 chars |

Response `status`: `pending` → `processing` → `succeeded` (means "sent to our payment
partner," not necessarily settled) / `failed`.

The webhook event type already handled here is `link.payment.paid` (per the existing code
comment). Corroborating evidence (not a fully confirmed doc page — PayMongo's narrative
guides for this are 404'd post-restructure) indicates its nested `data.attributes.data`
object is the Payment resource itself (`"type": "payment"`, `"id": "pay_..."`), matching
this handler's existing treatment of that object's `status`/`remarks` fields as payment
attributes. Worth a real sandbox webhook capture to confirm the `id` field name/prefix
before relying on it in production, but not a blocker to building against.

## Decisions (confirmed with user)

- Refund eligibility is **timing-based, not actor-based** — one shared calculation used by
  both `Booking.Cancel` (staff) and `Booking.CancelByCustomer`. Since pass #1 already blocks
  customers from cancelling online past `CancellationCutoffHours`, `LateCancellationPolicy`
  in practice only ever fires for staff-initiated late cancellations — but the logic itself
  just checks timing, not who cancelled.
  - On time (before cutoff): full refund.
  - Late (after cutoff): apply `LateCancellationPolicy` (`NoRefund`/`FullRefund`/`PartialRefund`,
    the last using `PaymentPolicy.RefundPercent`).
  - Only evaluated when `RequiresUpfrontPayment && PaymentConfirmedVia == Online` — pay-in-visit
    bookings are excluded, matching pass #1's cancellation behavior.
- Triggered by `BookingCancelledDomainEvent`, same event-driven shape as existing
  notification handlers. A refund API failure **does not roll back the cancellation** —
  the booking stays cancelled either way, matching this codebase's existing "side-effect
  failure never fails the transaction" posture for notifications.
- No webhook handling for PayMongo's own async refund status transitions in this pass —
  record whatever the synchronous API response says and stop. No manual retry UI. No
  ad-hoc/standalone refund action outside the cancellation flows.

## Backend design (ApexBooking)

### Capture the Payment ID

`WebhookResource` ([PaymongoContracts.cs](../../../ApexBooking.Core.Domain/Services/Paymongo/PaymongoContracts.cs))
gains:
```csharp
[JsonPropertyName("id")]
public string Id { get; set; } = string.Empty;
```

`ProcessPaymentWebhookCommandHandler.cs` passes `payload.Data.Attributes.Data.Id` through
to `Booking.ConfirmPayment`, which gains a new parameter and stores it:
```csharp
public string? PayMongoPaymentId { get; private set; }

public void ConfirmPayment(PaymentConfirmationMethod method, string? payMongoPaymentId = null)
{
    // ...existing guard/status logic unchanged...
    PayMongoPaymentId = payMongoPaymentId;
}
```
(Optional/nullable — the arrival-scan fallback path in `Tenant.RecordBookingArrival` also
calls `ConfirmPayment`, for a booking that was never actually paid via PayMongo online, so
it correctly passes nothing.)

### Refund calculation — shared, not duplicated

New method on `Booking` (or a small static domain policy helper, implementer's call on
exact placement — logic is the important part):
```csharp
private (bool ShouldRefund, decimal Amount) EvaluateRefund(BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)
{
    if (!RequiresUpfrontPayment || PaymentConfirmedVia != PaymentConfirmationMethod.Online)
        return (false, 0m);

    var scheduledAt = ScheduledDate.ToDateTime(ScheduledStartTime);
    var cutoffHours = bookingPolicy?.CancellationCutoffHours ?? 0;
    var isOnTime = DateTime.UtcNow.AddHours(cutoffHours) <= scheduledAt;

    if (isOnTime)
        return (true, AmountDue);

    return (bookingPolicy?.LateCancellationPolicy ?? CancellationPolicy.NoRefund) switch
    {
        CancellationPolicy.FullRefund => (true, AmountDue),
        CancellationPolicy.PartialRefund => (true, AmountDue * ((paymentPolicy?.RefundPercent ?? 0m) / 100m)),
        _ => (false, 0m),
    };
}
```
Called from both `Cancel(...)` and `CancelByCustomer(...)` right before raising
`BookingCancelledDomainEvent`, which gains two fields:
```csharp
public record BookingCancelledDomainEvent(
    ...,
    bool ShouldRefund,
    decimal RefundAmount
) : IDomainEvent;
```
(Both cancel methods already have `TenantId`/`Booking` in scope to load `BookingPolicy`/`PaymentPolicy`
via — `Tenant.CancelBooking`/`CancelBookingByCustomer` already have the aggregate loaded,
so pass the sibling policies down same as `CancelBookingByCustomer` already does for the
cutoff check today.)

### `Booking` gains refund tracking

```csharp
public RefundStatus RefundStatus { get; private set; } = RefundStatus.None;
public decimal? RefundedAmount { get; private set; }
public DateTime? RefundedAt { get; private set; }

internal void RecordRefundOutcome(RefundStatus status, decimal? amount)
{
    RefundStatus = status;
    RefundedAmount = amount;
    RefundedAt = DateTime.UtcNow;
}
```
New `RefundStatus` enum: `None`, `Pending`, `Processing`, `Succeeded`, `Failed` — mirrors
PayMongo's own refund status values 1:1.

### `IPayMongoService`

```csharp
Task<PayMongoRefundResult> CreateRefundAsync(
    string tenantSecretKey,
    string payMongoPaymentId,
    decimal amountPhp,
    string reason,
    CancellationToken cancellationToken);
```
`PayMongoRefundResult(string RefundId, string Status)`. `PayMongoService.cs` implementation
mirrors `CreatePaymentSourceAsync`'s exact shape (centavo conversion, Basic-auth header,
`POST v1/refunds` — note: **not** `/refunds` bare, PayMongo's versioned paths elsewhere in
this client are all `v1/...`, matching `v1/links`; verify against the actual base path
used elsewhere in this HttpClient's `BaseAddress` setup, which already includes the host
but the version segment is per-call).

### New event handler: `ProcessRefundOnBookingCancelledHandler`

`Features/Bookings/Events/`, subscribes to `BookingCancelledDomainEvent`:
1. If `!ShouldRefund || RefundAmount <= 0`, do nothing.
2. Load the booking + tenant's `PaymentCredential.SecretKey` + `Booking.PayMongoPaymentId`.
   If the payment ID is missing (shouldn't happen for an `Online`-confirmed booking post
   this pass, but could for bookings confirmed *before* this feature shipped), log and skip
   — no payment ID, no refund possible, same "can't act on data we don't have" posture as
   `SendBookingConfirmationEmailHandler` skipping when a customer has no email.
3. Call `IPayMongoService.CreateRefundAsync(...)`, wrapped in try/catch — success calls
   `booking.RecordRefundOutcome(mapped-status, amount)`; failure calls
   `RecordRefundOutcome(RefundStatus.Failed, null)` and logs, same try/catch-absorbed shape
   as the existing notification handlers (failure here never re-throws into the pipeline).
4. Persist via `_unitOfWork` + `CompleteAsync`, matching the other event handlers'
   independent-transaction pattern.

## Non-goals

No refund-status webhook handling (`refund.updated` or similar) — synchronous API response
only. No manual retry action for a `Failed` refund. No standalone/ad-hoc refund endpoint.
No backfill for bookings confirmed before this ships (their `PayMongoPaymentId` will be
null; refunding those, if ever needed, is a manual PayMongo-dashboard action).

## Testing

- Cancel an `Online`-paid booking well before the cutoff → full refund, `RefundStatus.Succeeded`
  (against PayMongo's sandbox/test mode).
- Staff cancels past the cutoff with `LateCancellationPolicy.PartialRefund` and
  `RefundPercent = 50` → refunded amount is exactly half of `AmountDue`.
- Staff cancels past the cutoff with `LateCancellationPolicy.NoRefund` → `ShouldRefund: false`,
  no PayMongo call made at all, `RefundStatus` stays `None`.
- Pay-in-visit booking cancelled → `ShouldRefund: false` regardless of timing.
- Simulate a PayMongo API failure (bad key, network error) → booking still ends up
  `Cancelled`, `RefundStatus.Failed`, no exception propagates out of the event pipeline.
- Confirm the actual webhook payload shape against PayMongo's sandbox before relying on
  `Data.Attributes.Data.Id` in production — the one piece of this spec resting on
  corroborating (not fully doc-confirmed) evidence.
