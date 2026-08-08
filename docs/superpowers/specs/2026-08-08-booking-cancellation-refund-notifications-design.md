# Cancellation & Refund-Outcome Notifications

## Context

The refund-processing feature ([2026-08-08-booking-cancellation-refund-processing-design.md](2026-08-08-booking-cancellation-refund-processing-design.md)) shipped with a real, undesigned gap: nobody is ever told anything happened.

- [NotifyTenantOnBookingCancelledHandler.cs](../../../ApexBooking.Core.Application/Features/Bookings/Events/NotifyTenantOnBookingCancelledHandler.cs) notifies the tenant **owner** in-app that a booking was cancelled — but says nothing about a refund, and never reaches the **customer** at all.
- [ProcessRefundOnBookingCancelledHandler.cs](../../../ApexBooking.Core.Application/Features/Bookings/Events/ProcessRefundOnBookingCancelledHandler.cs) records the refund outcome on the `Booking` row and logs it — nobody sees that outside a log file or a direct DB query.

This is a small, additive follow-up: one customer-facing cancellation email, one additional owner-facing bell notification once the refund actually resolves.

## Decisions (confirmed with user)

- **Customer email** — fires immediately for both cancellation paths (staff-initiated and customer self-service), off a new dedicated event. Says the booking was cancelled, and if a refund applies, describes it using whatever `Booking.RefundStatus` actually is *at send time* (`Pending`/`Succeeded`/`Failed`/absent) — not a hardcoded assumption, since the refund event and this notice event both land in the outbox in the same commit and can be relayed in either order.
- **No second customer email on refund outcome.** PayMongo's synchronous "succeeded" means "the processor accepted the request," not "money has settled" — a triumphant confirmation email would overstate certainty this system doesn't actually have.
- **Owner gets the outcome instead**, via a second bell notification once the PayMongo call resolves — the tenant owner is the one who'd actually need to chase a `Failed` refund.

## Design

### New reliable domain event

`BookingCancellationNoticeDomainEvent(TenantId, Guid BookingId, string BookingReference, DateTime CancelledAt) : IReliableDomainEvent`, added to [BookingEvents.cs](../../../ApexBooking.Core.Domain/Events/BookingEvents.cs). Raised unconditionally (regardless of refund eligibility) from `Booking.Cancel`/`CancelByCustomer`, right after the existing `BookingCancelledDomainEvent`.

Kept deliberately thin (no refund fields) — its handler re-loads the `Booking` fresh, same as every other handler in this codebase does, so it always reads whatever `RefundStatus` is current at send time rather than a stale value baked into the event payload.

It's a dedicated event rather than promoting `BookingCancelledDomainEvent` itself, for the same reason the refund event was kept separate: promoting it would silently move `NotifyTenantOnBookingCancelledHandler`'s delivery from synchronous/real-time onto the async outbox path too — an unrelated, unwanted behavior change to already-shipped code.

### `SendBookingCancellationEmailHandler` (new)

Subscribes to `DomainEventNotification<BookingCancellationNoticeDomainEvent>`. Same shape as `SendBookingConfirmationEmailHandler`: loads the tenant (`BusinessProfile`, `Bookings`, `Services`), resolves the booking, resolves the customer via `ICustomerRepository`, skips silently (with a warning log) if the customer has no email on file.

Builds a refund note from the booking's current `RefundStatus`:
- `Pending` / `Processing` → "A refund of {amount} {currency} is being processed and should reflect in your account within a few business days."
- `Succeeded` → "A refund of {amount} {currency} has been processed."
- `Failed` → omitted entirely — a failed refund is the business's problem to chase and resolve, not something to alarm the customer with in an automated email.
- `None` → omitted (pay-in-visit or unpaid booking).

`IBookingNotificationService` gains `SendBookingCancellationEmailAsync(to, customerName, businessName, serviceName, bookingReference, string? refundNote, ct)`, implemented in `BookingNotificationService.cs` matching the existing two email templates' visual style.

### Refund-outcome bell notification

`ProcessRefundOnBookingCancelledHandler` gains an `IRealtimeNotificationDispatcher` dependency (same one `NotifyTenantOnBookingCancelledHandler` already uses) and adds `t => t.Members` to its existing tenant load. After `RecordRefundOutcome` runs — on **both** the success and failure branches — it resolves the tenant owner and creates a `Notification`:
- Success: `NotificationEventType.RefundSucceeded`, "Refund Processed", "A refund of {amount} {currency} was processed for Booking {reference}."
- Failure: `NotificationEventType.RefundFailed`, "Refund Failed", "The refund for Booking {reference} could not be processed automatically. Please review it in PayMongo directly."

Two new `NotificationEventType` values (`RefundSucceeded`, `RefundFailed`) — kept as two separate values rather than one generic "RefundProcessed", matching this enum's existing granular style (`BookingCancelled`/`BookingCompleted`/`BookingNoShow` are already separate rather than a single "BookingStatusChanged").

## Non-goals

No customer-facing refund-outcome email (see Decisions). No changes to the existing `NotifyTenantOnBookingCancelledHandler` cancellation notice itself. No new UI surface (booking detail page badge, etc.) — this pass is notifications only, matching how the cancellation-notice bell already works today.

## Testing

- Staff cancels an online-paid, refund-eligible booking → customer receives one email mentioning a pending refund; owner's existing "Booking Cancelled" bell fires immediately; a second "Refund Processed" bell fires once the PayMongo call resolves (moments later, via the outbox relay).
- Customer self-cancels a pay-in-visit booking → customer receives the cancellation email with no refund note at all; no refund-outcome bell ever fires (no `BookingRefundDueDomainEvent` was raised to begin with).
- Simulate a PayMongo refund failure → owner receives a "Refund Failed" bell; customer's email (already sent, describing "Pending" at the time) is not retroactively corrected — accepted, since there's no live status page for this at time of send, this pass's stated non-goal.
- Customer has no email on file → cancellation email is skipped with a warning log, same as the existing confirmation-email handler's behavior.
