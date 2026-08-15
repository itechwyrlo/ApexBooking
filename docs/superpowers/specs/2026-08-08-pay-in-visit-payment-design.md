# Pay-in-Visit Payment Confirmation

## Context

`PaymentConfirmationMethod` ([PaymentConfirmationMethod.cs](../../../ApexBooking.Core.Domain/Enums/PaymentConfirmationMethod.cs)) only has `Online`/`PayAtCounter`, and both existing entry points misuse it:

- **Walk-ins** (`Tenant.ScheduleBooking`, [Tenant.cs:365-377](../../../ApexBooking.Core.Domain/Entities/Tenant.cs#L365-L377)) are stamped `PayAtCounter` **immediately at creation**, before any money has changed hands, and `AmountDue` is never populated (defaults to `0`) — the service price is lost.
- **Public bookings under `PaymentRequirementType.None`** (`InitiateBookingHandler`, [InitiateBookingHandler.cs:97-121](../../../ApexBooking.Core.Application/Features/Bookings/Commands/InitiateBooking/InitiateBookingHandler.cs#L97-L121)) get `requiresUpfrontPayment=false` but `PaymentConfirmedVia` is left `null` and `AmountDue` also stays `0` — same gap, nothing at all is recorded about how/how-much the customer will pay.
- The frontend surfaces this gap directly: `BookingDetailList.tsx`'s `getPaymentSummary` shows **"No payment required"** for `!requiresUpfrontPayment` bookings — factually wrong, since the tenant still expects to collect the service price in person. A `"Pay at visit"` fallback label already exists in that function but is unreachable dead code.

This spec unifies both cases under one real "pay in visit" concept: money owed, collected in person, confirmed at the moment it's actually received rather than assumed at booking time.

## Decisions (confirmed with user)

- Rename `PaymentConfirmationMethod.PayAtCounter` → `PayInVisit` everywhere (enum, DB string value, frontend type, UI copy) — walk-ins and no-upfront-payment public bookings are the same concept, not two.
- A pay-in-visit booking's `PaymentConfirmedVia` stays `null` (unpaid/pending) from creation through the visit, and is only stamped when the service is marked `Completed` — matches "pay in visit once complete," and accurately distinguishes "will pay in visit" from "has paid."
- `AmountDue` is populated with the service's snapshotted price for pay-in-visit bookings (both walk-ins and `None`-policy public bookings), so staff/admin views and the customer's confirmation screen can show how much is owed.
- Out of scope: `Tenant.RecordBookingArrival`'s existing behavior ([Tenant.cs:470-486](../../../ApexBooking.Core.Domain/Entities/Tenant.cs#L470-L486)) is unchanged except for the enum rename. That path stamps `PayInVisit` immediately when a booking that *did* require upfront payment arrives unpaid — a real, explicit cash-collection event at check-in, not a default assumption, so it stays outside the "wait for completion" rule above.

## Backend design (ApexBooking)

### `PaymentConfirmationMethod` enum

```csharp
public enum PaymentConfirmationMethod
{
    Online,
    PayInVisit
}
```

### `Booking.Create(...)` ([Booking.cs:63-167](../../../ApexBooking.Core.Domain/Entities/Booking.cs#L63-L167))

Drops the `paymentConfirmedVia` parameter entirely. Every booking now starts with `PaymentConfirmedVia = null`, no exceptions — removing the walk-in special case that stamped it eagerly. The `requiresUpfrontPayment` branch (`PendingPayment` vs `Scheduled` status + domain events) is unchanged.

### `Tenant.PlaceBooking(...)` (private core, [Tenant.cs:392-466](../../../ApexBooking.Core.Domain/Entities/Tenant.cs#L392-L466))

Drops its own `paymentConfirmedVia` parameter (no longer forwarded to `Booking.Create`). Gains one rule, once `service` is resolved (step 1 of the existing method):

```csharp
decimal finalAmountDue = requiresUpfrontPayment ? amountDue : service.Price;
```

`finalAmountDue` is what gets passed to `Booking.Create`. This is the single place that snapshots the price for every pay-in-visit booking — both callers below get it for free, with no per-caller duplication:

- `Tenant.ScheduleBooking` ([Tenant.cs:365-377](../../../ApexBooking.Core.Domain/Entities/Tenant.cs#L365-L377)): simplifies to `PlaceBooking(..., requiresUpfrontPayment: false)` — no `amountDue` or `paymentConfirmedVia` arguments needed anymore.
- `Tenant.PlaceCustomerBooking` (public wizard entry, [Tenant.cs:380-390](../../../ApexBooking.Core.Domain/Entities/Tenant.cs#L380-L390)): unchanged signature; when called with `requiresUpfrontPayment: false` (see `InitiateBookingHandler` below), `finalAmountDue` now resolves to `service.Price` instead of the caller's `0`.

### `Booking.CompleteService()` ([Booking.cs:218-236](../../../ApexBooking.Core.Domain/Entities/Booking.cs#L218-L236))

Gains the actual "pay in visit" confirmation moment:

```csharp
public void CompleteService()
{
    if (Status != BookingStatus.Scheduled)
        throw new BusinessRuleBrokenException("Only active, scheduled appointments can be marked as completed.");

    Status = BookingStatus.Completed;
    ServiceCompletedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;

    if (PaymentConfirmedVia is null)
    {
        PaymentConfirmedVia = PaymentConfirmationMethod.PayInVisit;

        AddDomainEvent(new PaymentCapturedDomainEvent(
            TenantId: this.TenantId,
            BookingId: this.BookingId.Value,
            BookingReference: this.BookingReference,
            AmountDue: this.AmountDue,
            CurrencyCode: this.CurrencyCode,
            Method: PaymentConfirmationMethod.PayInVisit,
            CapturedAt: UpdatedAt
        ));
    }

    AddDomainEvent(new BookingCompletedDomainEvent(/* unchanged */));
}
```

`PaymentConfirmedVia` is `null` at this point only for bookings nothing collected earlier (walk-ins, `None`-policy public bookings). Any booking that required upfront payment already carries `Online` (webhook, `ProcessPaymentWebhookCommandHandler`) or `PayInVisit` (arrival-scan fallback) by the time it can reach `Scheduled` — `CompleteService()`'s new branch is a no-op for those, so this doesn't double-fire for the existing online-payment path.

Firing `PaymentCapturedDomainEvent` here reuses the existing `NotifyTenantOnPaymentCapturedHandler` ("Payment Received" owner notification, [NotifyTenantOnPaymentCapturedHandler.cs](../../../ApexBooking.Core.Application/Features/Bookings/Events/NotifyTenantOnPaymentCapturedHandler.cs)) for free — its message text is already payment-method-agnostic.

### `InitiateBookingHandler` ([InitiateBookingHandler.cs:97-121](../../../ApexBooking.Core.Application/Features/Bookings/Commands/InitiateBooking/InitiateBookingHandler.cs#L97-L121))

`amountToCharge` needs to mirror the price for the `None` path too, so `BookingInitiationResult.AmountToPay` (returned to the customer) reflects it:

```csharp
bool requiresUpfrontPayment = tenant.PaymentPolicy.RequirementType != PaymentRequirementType.None;
decimal amountToCharge = 0m;

if (requiresUpfrontPayment)
{
    // unchanged: FullPaymentRequired / DepositRequired branches
}
else
{
    amountToCharge = service.Price;
}
```

No DTO shape change — `BookingInitiationResult` already has `AmountToPay`; only what populates it changes. `RequiresPayment` stays `false` for this path (no PayMongo step), so the PayMongo credential/link-generation block (`if (requiresUpfrontPayment && amountToCharge > 0)`) is untouched — it never sees this branch.

### Data migration

No schema change (`payment_confirmed_via` stays a string column via `.HasConversion<string>()`, [BookingConfiguration.cs:97](../../../ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs#L97)). New EF migration includes a data-fix statement for any pre-existing rows, since `HasConversion<string>()` round-trips through `Enum.Parse` and would throw on an unrecognized old value:

```sql
UPDATE bookings SET payment_confirmed_via = 'PayInVisit' WHERE payment_confirmed_via = 'PayAtCounter';
```

Generated, not applied — same as the rest of this branch's migrations, per existing convention.

## Frontend design (LocalFlow)

### `types/PaymentConfirmationMethod.ts`

```ts
export const PaymentConfirmationMethod = {
  Online: 'Online',
  PayInVisit: 'PayInVisit',
} as const
```

### `components/calendar/BookingDetailList.tsx`

`getPaymentSummary` drops its `!requiresUpfrontPayment` early return (the "No payment required" case this spec is fixing) in favor of a single status/method-driven set of branches:

```ts
function getPaymentSummary(booking: ITenantBooking): IPaymentSummary {
  if (booking.paymentConfirmedVia === PaymentConfirmationMethod.Online) {
    return { label: 'Paid online', tone: 'success', detail: formatAmount(booking.amountDue, booking.currencyCode) }
  }

  if (booking.paymentConfirmedVia === PaymentConfirmationMethod.PayInVisit) {
    return { label: 'Paid in visit', tone: 'success', detail: formatAmount(booking.amountDue, booking.currencyCode) }
  }

  if (booking.status === BookingStatus.PendingPayment) {
    return { label: 'Awaiting online payment', tone: 'warning', detail: formatAmount(booking.amountDue, booking.currencyCode) }
  }

  return { label: 'Pay in visit', tone: 'primary', detail: formatAmount(booking.amountDue, booking.currencyCode) }
}
```

The final fallback (`paymentConfirmedVia === null` and not `PendingPayment`) is the pending pay-in-visit case — this was the previously-unreachable dead branch, now correctly reached and relabeled to match the renamed backend term.

### `components/clients/CustomerBookingsModal.tsx`

`PaymentDetail` gets the equivalent fix — replace the `!requiresUpfrontPayment` → "No payment required" branch with the same pending-vs-paid distinction:

```ts
function PaymentDetail({ booking }: { booking: ICustomerBooking }) {
  const amount = formatMoney(booking.amountDue, booking.currencyCode)

  if (!booking.paymentConfirmedVia) {
    const label = booking.status === BookingStatus.PendingPayment ? 'awaiting payment' : 'pay in visit'
    return <span className="text-warning small">{amount} — {label}</span>
  }

  const via = booking.paymentConfirmedVia === PaymentConfirmationMethod.Online ? 'paid online' : 'paid in visit'
  return (
    <span className="small">
      {amount} — <span className="text-success">{via}</span>
    </span>
  )
}
```

### `components/publicBooking/SuccessStep.tsx`

New case: when `!result.requiresPayment && result.amountToPay > 0`, show a line under the ticket:

```tsx
{!result.requiresPayment && result.amountToPay > 0 && (
  <div className="p-4 pb-muted small border-top">
    Pay {formatMoney(result.amountToPay, /* currency — see note below */)} at your visit.
  </div>
)}
```

`IBookingInitiationResult` doesn't carry a currency code (the public wizard never needed one on this DTO before, since `AmountToPay` was always `0` on the no-payment path). No backend DTO change for this — `SuccessStep` already receives `service: IPublicService` as a prop, which carries `currencyCode`; use `service.currencyCode` for the `formatMoney` call above.

### No other interface changes

`IBookingInitiationResult`, `ITenantBooking`, `ICustomerBooking` keep their existing fields — only what the backend populates changes, and the `PaymentConfirmationMethod` type's member rename ripples through automatically wherever it's imported.

## Non-goals

No change to `PaymentPolicy`/`PaymentRequirementType` config or its settings UI. No change to the `DepositRequired`/`FullPaymentRequired` online-payment flow, PayMongo integration, or webhook handling. No new `BookingStatus` value. No change to `Tenant.RecordBookingArrival`'s trigger condition (rename only, per Decisions above).

## Testing

- Domain: `CompleteService()` stamps `PayInVisit` + fires `PaymentCapturedDomainEvent` when `PaymentConfirmedVia` was `null`; does NOT re-stamp/re-fire when it already carries `Online` or `PayInVisit` (arrival-scan case).
- `Tenant.ScheduleBooking` (walk-in): resulting booking has `AmountDue == service.Price`, `PaymentConfirmedVia == null`, `Status == Scheduled` at creation.
- `InitiateBookingHandler` under `PaymentRequirementType.None`: `BookingInitiationResult.AmountToPay == service.Price`, `RequiresPayment == false`; resulting booking has `AmountDue == service.Price`, `PaymentConfirmedVia == null`.
- Migration: seed a booking row with the literal string `PayAtCounter`, apply migration, confirm it reads back as `PayInVisit` without throwing.
- Manual (frontend): complete a walk-in booking end-to-end (create → complete) and confirm the calendar/customer-history views show "Pay in visit" pending, then "Paid in visit" post-completion, with the owner notification firing once.
