# Scanner Check-In / Checkout Flow — Design Spec

**Date:** 2026-08-15
**Status:** Approved design, ready for implementation planning

## Background

An audit (2026-08-15) of the current scanner, booking lifecycle, and pay-in-visit payment
functionality found:

- **No "Admitted" status exists.** `BookingStatus` is `PendingPayment | Scheduled | Completed |
  NoShow | Cancelled`. Arrival is a `CheckedInAt` timestamp layered on top of `Scheduled`, set by
  `Booking.RecordArrival()` (idempotent — rescanning silently no-ops today).
- **Scan and manual admit are the same domain transition.** `ScanArrivalCommand` (token-based) and
  `AdmitBookingCommand` (staff-picked) both resolve to `Tenant.RecordBookingArrival(bookingId,
  scannerBranchId)`.
- **Complete is a separate, unguarded manual action** (`CompleteBookingCommand` →
  `Tenant.CompleteBooking` → `Booking.CompleteService()`), reachable regardless of whether the
  booking was ever checked in.
- **A real, pre-existing payment-tracking gap**: `Booking.AmountDue` is a snapshot of *what was
  required upfront* — for a `DepositRequired` tenant policy, that's the deposit amount only, not
  the full service price. Nothing on `Booking` stores the full price separately. Combined with
  `RecordPayInVisitPayment()`'s guard (`PaymentConfirmedVia is not null → throw`), **there is no
  existing mechanism to ever record an in-visit remainder once a deposit was paid online** — not a
  wrong amount, a genuinely missing capture. The same gap silently undercounts the "Pay-on-Visit
  Revenue" dashboard tile, since `TenantRepository.GetRevenueAsync` sums `AmountDue` grouped by
  `PaymentConfirmedVia`.
- **Staff-role UI/backend mismatch**: `BookingTable.tsx`/`BookingDetailPanel.tsx` render
  Complete/Collect-Payment/No-show buttons for any role that can reach `/appointments` or
  `/calendar` (unrestricted routes), but those actions are Owner/Admin-only
  (`[Authorize(Policy="ManagementOnly")]`) on the backend — Staff clicking them today gets a 403
  despite the button looking usable.
- **Token has no expiry**: `HmacTicketTokenService` produces a deterministic, non-expiring
  signature over `{BookingId, TenantId, BranchId}` — unrelated to this spec, noted for completeness
  only, not being changed here.

This spec designs: (1) a checkout step in the scanner flow, (2) the payment-tracking fix that
checkout's "how much is left to pay" display actually requires, and (3) removing the
Complete/Collect-Payment UI from Staff's view to match the backend truth that already exists.

## Scope

**In scope:**
- A checkout moment in the scanner flow: scanning an already-admitted, not-yet-completed booking
  shows customer/payment detail and a Confirm button, instead of silently no-op-ing
- New `Booking.ServicePriceAtBooking` and `Booking.InVisitAmountCollected` fields, and the domain
  method changes needed to make in-visit payment capture (including deposit-then-remainder)
  actually work and report the correct amount
- Fixing `TenantRepository.GetRevenueAsync` to count in-visit remainders correctly
- Validation: clear, state-specific errors when scanning a not-yet-admitted, already-completed,
  cancelled, or no-show booking for checkout
- Hiding Complete/Collect-Payment/No-show action buttons from Staff's view in
  `BookingTable.tsx`/`BookingDetailPanel.tsx` (Owner/Admin keep them as a manual fallback)
- Showing the corrected remaining balance (not raw `AmountDue`) in `CollectPaymentModal.tsx` and
  `BookingDetailPanel.tsx`'s existing payment summary

**Out of scope (explicit):**
- Ticket token expiry/nonce — unrelated pre-existing design, not touched
- Restricting `/appointments`/`/calendar` routes by role — the button-hiding fix in this spec
  addresses the actual user-facing problem; broader route-level role restriction is a separate,
  larger frontend-routing concern not requested here
- Any change to `ConfirmPayment()` (the online/PayMongo-webhook path) — it already reports the
  correct amount and is untouched
- A full payment ledger/history entity — `InVisitAmountCollected` is the minimal field needed to
  make the existing binary + snapshot model correct for the deposit-then-remainder case, not a
  general-purpose multi-transaction payment history

## Architecture

One scanner, two moments, no mode toggle. `ScanArrivalResult.WasFirstAdmission` (already returned
today) is the signal: `true` = fresh admit, unchanged behavior; `false` = already checked in and
still `Scheduled` = checkout. The frontend branches on this flag to swap from "admitted" toast to
an inline checkout detail panel (fetched via a new read-only query) with a Confirm button (a new
write command). Already-completed/cancelled/no-show bookings already throw from
`RecordArrival()`'s existing `Status == Scheduled` guard — the new checkout query adds
state-specific messages for those cases rather than a generic one.

## Data model

Two new fields on `Booking`:

- `ServicePriceAtBooking` (`decimal?`, nullable) — the full service price, snapshotted from
  `service.Price` at creation time in **every** case (not just deposit policies).
  `InitiateBookingHandler.cs` sets this alongside the existing `amountToCharge` computation.
  Nullable because bookings created before this ships won't have it.
- `InVisitAmountCollected` (`decimal`, not null, default `0`) — cash collected in person, tracked
  **separately and additively** to the online-charged `AmountDue`/`PaymentConfirmedVia==Online`.
  This is the field that was missing entirely before this spec — it's what makes a
  deposit-then-remainder-in-visit booking representable at all.

Computed (not stored) everywhere checkout/payment logic needs it:
```
AmountPaidOnline = PaymentConfirmedVia == Online ? AmountDue : 0
RemainingBalance = ServicePriceAtBooking.HasValue
    ? max(0, ServicePriceAtBooking - AmountPaidOnline - InVisitAmountCollected)
    : (PaymentConfirmedVia is null ? AmountDue : 0)   // pre-migration fallback, no ServicePriceAtBooking to compute from
```

This correctly handles every case:
- **Full payment required, paid online**: `AmountDue == ServicePriceAtBooking`, remaining = 0
- **Pay at counter (`None`)**: `AmountDue == ServicePriceAtBooking`, `PaymentConfirmedVia` starts
  null, remaining = full price until captured in-visit — unchanged from today
- **Deposit, paid online**: `AmountDue` = deposit only, remaining = `ServicePriceAtBooking -
  deposit` — **this is the previously-untracked gap**, now correctly computed
- **Deposit-then-remainder captured in-visit**: `PaymentConfirmedVia` stays `Online` (never
  overwritten — preserves refund-eligibility logic that keys off it), `InVisitAmountCollected`
  carries the remainder, remaining = 0 once captured

## Domain method changes

**`Booking.CaptureRemainingInVisitPayment()`** (new private helper, replaces the duplicated logic
in `RecordPayInVisitPayment()` and `CompleteService()`'s auto-settle block):
```csharp
private void CaptureRemainingInVisitPayment()
{
    var remaining = ComputeRemainingBalance(); // private helper implementing the formula above
    if (remaining <= 0) return; // nothing left to capture — a no-op, not an error, from CompleteService's call site

    InVisitAmountCollected += remaining;
    PaymentConfirmedVia ??= PaymentConfirmationMethod.PayInVisit; // never overwrites an existing Online

    AddDomainEvent(new PaymentCapturedDomainEvent(
        TenantId: this.TenantId,
        BookingId: this.BookingId.Value,
        BookingReference: this.BookingReference,
        AmountDue: remaining, // the amount just captured by THIS event, not the stale full snapshot
        CurrencyCode: this.CurrencyCode,
        Method: PaymentConfirmationMethod.PayInVisit,
        CapturedAt: UpdatedAt));
}
```

- **`RecordPayInVisitPayment()`**: guard changes from `PaymentConfirmedVia is not null → throw` to
  `ComputeRemainingBalance() <= 0 → throw "Payment has already been recorded for this
  appointment."`; on success calls `CaptureRemainingInVisitPayment()`. Still callable directly by
  Owner/Admin's existing manual "Collect Payment" tool, now correctly handling the
  deposit-remainder case it couldn't before.
- **`CompleteService()`**: the existing `if (PaymentConfirmedVia is null) { ... }` block is
  replaced with an unconditional call to `CaptureRemainingInVisitPayment()` (which is itself a
  no-op when nothing remains, preserving today's behavior for fully-paid bookings exactly).
- **`Tenant.RecordBookingArrival`**: the `PendingPayment` branch stops calling
  `booking.ConfirmPayment(PayInVisit)` (which prematurely claimed payment was captured at arrival,
  before checkout, at a stale amount). Replaced with a new minimal domain method:
  ```csharp
  internal void ClearPendingPaymentOnArrival()
  {
      if (Status != BookingStatus.PendingPayment)
          throw new BusinessRuleBrokenException("Only bookings pending payment can be cleared for arrival.");
      Status = BookingStatus.Scheduled;
      UpdatedAt = DateTime.UtcNow;
  }
  ```
  This does **not** touch `PaymentConfirmedVia` or raise a payment event — it just reaches the
  same `Scheduled` + `PaymentConfirmedVia == null` state every `None`-policy/pay-at-counter booking
  already starts in today. Not a new invariant, just one more path to an already-valid state. The
  actual payment capture now correctly happens for real at checkout, through the same
  `CaptureRemainingInVisitPayment()` path as every other case.
- **New `Tenant.CheckOutBooking(bookingId, scannerBranchId)`**: mirrors
  `RecordBookingArrival`'s existing guard style —
  - Booking not found → throw
  - `booking.BranchId != scannerBranchId` → throw (cross-branch guard, same as admit)
  - `booking.CheckedInAt is null` → throw `"This booking hasn't been checked in yet."`
  - `booking.Status == BookingStatus.Completed` → throw `"This booking has already been
    completed."`
  - `booking.Status is BookingStatus.Cancelled or BookingStatus.NoShow` → throw a
    status-specific message
  - Otherwise: `booking.CompleteService()`

  This new `CheckedInAt` requirement lives **only** in this new orchestration method — the
  existing `Tenant.CompleteBooking` (used by the existing manual Complete button/command) is
  **untouched**, so Owner/Admin's fallback path for a lost QR code keeps working exactly as today.

## Backend API surface

**`GetCheckoutDetailsQuery(Guid BookingId) : IQuery<CheckoutDetailsDto>`** — new query. Handler
resolves the booking (tenant-scoped automatically via the existing EF global filter, since
`Booking : ITenantEntity`), re-validates the calling staff's own branch against the booking's
branch (same pattern as `ScanArrivalHandler`), then applies the same state checks as
`Tenant.CheckOutBooking` (read-only preview of what confirming would validate) before returning:

```csharp
public record CheckoutDetailsDto(
    Guid BookingId,
    string BookingReference,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string ServiceName,
    string StaffName,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    decimal AmountPaidOnline,
    decimal RemainingBalance,
    string CurrencyCode);
```

Backed by a new `TenantRepository.GetBookingCheckoutDetailAsync(TenantId, Guid bookingId, ct)`
repository method, following the exact flat-row-projection pattern `GetBookingsPageAsync` already
uses for `TenantBookingSummary` (join booking → customer/staff/service, project to a row DTO,
compute `AmountPaidOnline`/`RemainingBalance` in the handler).

**`CheckoutBookingCommand(Guid BookingId) : ICommand<CheckoutBookingResult>`** — new command.
Handler resolves the calling staff's own branch, calls `Tenant.CheckOutBooking(bookingId,
scannerBranchId)`, persists, returns:
```csharp
public record CheckoutBookingResult(Guid BookingId, string BookingReference, DateTime CompletedAt, decimal AmountSettled, string CurrencyCode);
```

Both live in a new `ApexBooking.Core.Application/Features/Bookings/{Queries/GetCheckoutDetails,
Commands/CheckoutBooking}` pair, exposed on `TenantController` at `GET
/api/Tenant/bookings/{bookingId}/checkout-detail` and `POST
/api/Tenant/bookings/{bookingId}/checkout`, both `[Authorize(Roles = "Owner,Admin,Staff")]` —
matching `scan-arrival`/`admit`'s existing role widening, since Staff must be able to run the full
admit→checkout scan flow.

**`TenantBookingSummary`** (existing DTO, used by `GetTenantBookingsQuery` and consumed by
`BookingTable.tsx`/`OwnerDashboardPage.tsx`/`CollectPaymentModal.tsx`) gets two new fields,
`AmountPaidOnline` and `RemainingBalance`, computed the same way, so the existing manual
Collect-Payment/booking-detail UI shows accurate figures instead of raw `AmountDue`.

## Revenue query fix

`TenantRepository.GetRevenueAsync` currently sums `AmountDue` grouped by `PaymentConfirmedVia` —
which is exactly why an in-visit remainder was invisible to revenue. Fix:
```
onlineAmount     = SUM(CASE WHEN PaymentConfirmedVia == Online     THEN AmountDue - refund ELSE 0 END)   // unchanged
payInVisitAmount = SUM(CASE WHEN PaymentConfirmedVia == PayInVisit THEN AmountDue - refund ELSE 0 END)
                  + SUM(CASE WHEN PaymentConfirmedVia == Online     THEN InVisitAmountCollected ELSE 0 END)
```
The second term is deliberately scoped to `PaymentConfirmedVia == Online` rows only — that's the
*only* case where `InVisitAmountCollected` represents money not already counted elsewhere (the
deposit-then-remainder case, where `PaymentConfirmedVia` never becomes `PayInVisit`). For a pure
pay-at-counter booking, `AmountDue == ServicePriceAtBooking` and `InVisitAmountCollected` ends up
numerically equal to that same `AmountDue` once captured — it's already fully counted by the first
branch, so summing it a second time there would double-count. Restricting the second term to
`Online`-confirmed rows avoids that exactly.

## Frontend flow

- **`AdmitScanModal.tsx`**: on a scan result, branch on `wasFirstAdmission`:
  - `true` → existing "admitted" toast + close, unchanged
  - `false` → fetch `GetCheckoutDetailsQuery` by the returned `BookingId`; on success, swap the
    modal body to an inline checkout panel (customer name/contact, service, staff, amount paid
    online, remaining balance, a Confirm button); on the query's validation error (not checked in /
    already completed / cancelled / no-show), show that specific message instead
  - Confirm button calls the new `CheckoutBookingCommand`, shows a success toast, closes, and
    triggers the same `onAdmitted`-style refetch the modal already does today
  - `OwnerDashboardPage.tsx`'s current `onAdmitted={() => {}}` no-op should be wired to an actual
    refetch while this file is touched, matching `AppointmentsPage.tsx`'s existing behavior — a
    small pre-existing gap worth fixing alongside this work since the same component is being
    edited anyway
- **`BookingTable.tsx` / `BookingDetailPanel.tsx`**: Complete/Collect-Payment/No-show action
  buttons render only when the current user's role is Owner or Admin. Staff continues to see Admit
  (via the scanner) only. Cancel's visibility is unaffected by this spec (not called out as
  redundant in the request).
- **`CollectPaymentModal.tsx`**: "Amount due" label changes to show `remainingBalance` (new field)
  instead of `amountDue`, so a deposit booking's manual collection (Owner/Admin fallback) shows the
  correct figure.
- **`BookingDetailPanel.tsx`**'s existing `getPaymentSummary()`: updated to show
  `amountPaidOnline`/`remainingBalance` instead of the raw `amountDue` snapshot for its payment
  status line.

## Validation matrix (checkout scan / `GetCheckoutDetailsQuery` + `CheckoutBookingCommand`)

| Booking state at scan/confirm time | Result |
|---|---|
| `CheckedInAt is null` (never admitted) | Error: "This booking hasn't been checked in yet." |
| `Status == Scheduled`, `CheckedInAt` set, `Status != Completed` | Valid — show/confirm checkout |
| `Status == Completed` | Error: "This booking has already been completed." |
| `Status == Cancelled` | Error: status-specific cancellation message |
| `Status == NoShow` | Error: status-specific no-show message |
| Cross-branch (`booking.BranchId != scanner's own BranchId`) | Error: existing cross-branch guard message, unchanged pattern |

Rescanning for admission (first scan already happened) continues to be idempotent — that's the
`WasFirstAdmission: false` signal driving the checkout branch, not an error.

## Migration

One new EF Core migration adding `ServicePriceAtBooking` (nullable decimal) and
`InVisitAmountCollected` (decimal, `HasDefaultValue(0m)` so existing rows and the column's NOT
NULL constraint are both satisfied without a manual backfill step) to the `Bookings` table.
Generated, not applied, per the existing convention in this repo.

## Testing

Same posture as the rest of this codebase (no handler/controller test project exists) — this
spec's domain-level changes (`Booking.CaptureRemainingInVisitPayment`,
`ComputeRemainingBalance`, `ClearPendingPaymentOnArrival`, `RecordPayInVisitPayment`,
`CompleteService`) get unit tests in `ApexBooking.Core.Domain.UnitTests` covering: full-payment
remaining=0, pay-at-counter full capture, deposit-only remaining gap, deposit-then-remainder
capture (the previously-impossible case), double-capture guard (remaining already 0 → throw),
and the `ClearPendingPaymentOnArrival` state guard. Everything above that layer (queries,
commands, controller, revenue SQL) verified manually.
