# Refund Review & Manual Confirmation

## Context

Pass #1 ([2026-08-08-booking-cancellation-refund-processing-design.md](2026-08-08-booking-cancellation-refund-processing-design.md))
shipped fully automatic refunds: the instant an eligible online-paid booking is cancelled,
`BookingRefundDueDomainEvent` fires and `ProcessRefundOnBookingCancelledHandler` calls
PayMongo unconditionally. Live testing this session surfaced three real bugs in that path
(wrong refund endpoint path, wrong PayMongo Payment ID captured from the webhook — it was
capturing the Legacy Link's own ID, not the nested Payment resource's ID) which are now
fixed, plus one permanent external constraint: **QR Ph payments cannot be refunded via
PayMongo's API at all**, confirmed against PayMongo's own help center. GCash, Maya, GrabPay,
and cards remain refundable via the API (processed within ~24h per PayMongo).

That last discovery reframes the feature: automatic refund can never be a universal
guarantee (QR Ph will always need a human to move money manually), and separately, the
tenant explicitly wants refunds to require human sign-off by default regardless of payment
method — automatic refund becomes an opt-in tenant setting, not the default behavior.

## Decisions (confirmed with user)

- **`PaymentPolicy.AutomaticRefund`** (bool, default `false`). When `true`, behavior is
  unchanged from pass #1 (instant, automatic). When `false` (the default), a refund-eligible
  cancellation creates a `RefundRequest` awaiting human review instead of calling PayMongo.
- **Review page access: Owner and Admin only** — Staff never see or act on refund requests.
- **Owner double-confirmation gate applies to both Confirm and Reject.** Any refund decision
  an Admin makes — approve or deny — requires the Owner's sign-off before it takes effect.
  When the Owner makes the decision directly, no gate applies.
- **Customer e-wallet number (GCash/Maya) is collected only when actually needed** — never at
  booking time. It's asked for on the customer's refund-status page, and only once a specific
  `RefundRequest` actually reaches the manual-transfer state (i.e., almost always never asked,
  since QR Ph is the only case that forces it, so far).
- Auto-refund eligibility is a property of *how the customer paid*, not a runtime guess — it's
  computed once when the `RefundRequest` is created and shown to the reviewer up front, so
  Owner/Admin never has to attempt-and-discover a QR Ph payment can't be auto-refunded.

## Backend design (ApexBooking)

### Capture the payment method type (new prerequisite)

The webhook currently captures only the Payment ID. To know up front whether a payment is
auto-refundable, `ProcessPaymentWebhookCommandHandler` also needs the payment's source/method
type (e.g. `"gcash"`, `"qrph"`, `"card"`) from the same nested Payment object the ID now comes
from correctly. **The exact field name is not yet confirmed** — same situation the Payment ID
itself was in before this session's sandbox capture resolved it. Do not guess the field name;
capture a real paid-QR-Ph and a real paid-GCash sandbox webhook payload first (same technique
used this session: breakpoint on `jsonText` in `PayMongoWebhooksController`, or PayMongo
dashboard's webhook event log) and read the actual attribute before implementing.

`Booking` gains `public string? PaymentMethodType { get; private set; }`, set alongside
`PayMongoPaymentId` in `ConfirmPayment`.

### `RefundRequest` — new persistence record

Same pattern as `BookingPayment`/`TenantSubscription` (Billing #10): a persistence record, not
an aggregate root, own `ITenantEntity` scoping, no repository — queried via `Tenant` the same
way those are.

```csharp
public class RefundRequest : ITenantEntity
{
    public Guid Id { get; }
    public TenantId TenantId { get; }
    public Guid BookingId { get; }
    public decimal RequestedAmount { get; }
    public string CurrencyCode { get; }
    public bool IsAutoRefundEligible { get; }   // from Booking.PaymentMethodType at creation time

    public RefundRequestStatus Status { get; }  // see state machine below

    public Guid? DecidedByUserId { get; }
    public DateTime? DecidedAt { get; }
    public RefundDecisionAction? DecisionAction { get; }   // Confirm | Reject
    public string? RejectionReason { get; }

    public Guid? OwnerDecidedByUserId { get; }
    public DateTime? OwnerDecidedAt { get; }

    // Only ever populated for the manual-transfer path, submitted by the customer via the
    // public refund-status page (or typed in by staff if collected by phone instead).
    public string? CustomerEwalletProvider { get; }
    public string? CustomerEwalletNumber { get; }

    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }
}
```

**State machine** (`RefundRequestStatus`):

```
PendingReview
  --[Owner confirms directly, OR Admin confirms]--> AwaitingOwnerApproval (if Admin)
  --[Owner confirms directly]--> Approved
  --[Owner rejects directly]--> Rejected
  --[Admin rejects]--> AwaitingOwnerApproval

AwaitingOwnerApproval
  --[Owner approves the proposed decision]--> Approved | Rejected  (per DecisionAction)
  --[Owner denies the proposed decision]--> PendingReview   (reopened, not stuck forever)

Approved
  --[IsAutoRefundEligible]--> Processing --[PayMongo call resolves]--> Succeeded | Failed
  --[!IsAutoRefundEligible]--> AwaitingManualTransfer
      --[customer submits e-wallet details]--> (still AwaitingManualTransfer, details attached)
      --[tenant/admin marks sent]--> ManuallyRefunded

Rejected  (terminal — customer notified, no money moves)
```

### Trigger change in `Booking`

`Cancel`/`CancelByCustomer` already receive `PaymentPolicy?` for the existing refund-percent
calculation. `EvaluateRefund`'s eligible branch now also checks `paymentPolicy.AutomaticRefund`:

```csharp
if (shouldRefund)
{
    RefundStatus = RefundStatus.Pending;
    if (paymentPolicy?.AutomaticRefund == true)
        AddDomainEvent(new BookingRefundDueDomainEvent(...));       // unchanged, pass #1 path
    else
        AddDomainEvent(new BookingRefundEligibleDomainEvent(...));  // new
}
```

`BookingRefundEligibleDomainEvent` is a plain `IDomainEvent` (synchronous, no external call —
same class as `BookingCreatedDomainEvent`), carrying `TenantId`/`BookingId`/`BookingReference`/
`RefundAmount`/`CurrencyCode`/`OccurredAt`.

### New handler: `CreateRefundRequestOnEligibleHandler`

Subscribes to `BookingRefundEligibleDomainEvent`. Creates the `RefundRequest` row
(`PendingReview`, `IsAutoRefundEligible` read from `booking.PaymentMethodType`), and raises a
tenant bell notification (new `NotificationEventType.RefundReviewNeeded`) to the Owner and any
Admins — same delivery mechanism as the existing `NotifyTenantOnBookingCancelledHandler`.

### New commands (`Features/RefundRequests/`)

- `ConfirmRefundRequestCommand` / `RejectRefundRequestCommand` — callable by Owner or Admin.
  - Owner: applies the decision immediately per the state machine above.
  - Admin: moves to `AwaitingOwnerApproval`, records the tentative `DecisionAction`, raises
    `NotificationEventType.RefundApprovalNeeded` to the Owner (the notification carries the
    `RefundRequestId` so the frontend can render its own confirm/deny modal off it — the "pop
    window" is a frontend concern; the backend's job here is just making sure the notification
    payload has enough to drive it).
- `ApproveOwnerGateCommand` / `DenyOwnerGateCommand` — Owner-only, act on an
  `AwaitingOwnerApproval` request. Approve applies the tentative decision; Deny reopens it to
  `PendingReview`.
- `MarkManualRefundSentCommand` — Owner/Admin-only, valid only from `AwaitingManualTransfer`.
- `SubmitRefundEwalletDetailsCommand` — public/anonymous, same cancellation-token auth pattern
  as the existing customer cancel link (`ICancellationTokenService`), valid only once a request
  is in `AwaitingManualTransfer`.
- `GetPendingRefundRequestsQuery` — the review page's list.
- `GetRefundStatusQuery` — public/anonymous, token-authenticated, backs the customer page.

Reaching `Approved` + `IsAutoRefundEligible` raises `BookingRefundDueDomainEvent` — **the
exact same event, same handler, same PayMongo call as the automatic path.** No new refund-
calling code; only the trigger timing changes.

### `Booking.RefundStatus` gains `Rejected`

The existing `RefundStatus` enum (`None`/`Pending`/`Processing`/`Succeeded`/`Failed`) gains
one new terminal value, set when a `RefundRequest` is rejected. `RefundRequest.Status` is the
detailed workflow state; `Booking.RefundStatus` stays the coarse summary the existing
cancellation email and notification code already reads — minimal disruption to pass #1's
notification work.

### New customer-facing pieces

- `BusinessProfile.ContactPhoneNumber` (new field) — shown on the customer refund-status page,
  not a personal Owner/Admin number.
- Customer rejection email (new): `SendBookingCancellationEmailHandler`'s existing refund-note
  block gains a `Rejected` case (today it only handles `Pending`/`Succeeded`/omits `Failed`) —
  *only if* the rejection resolves before that email sends; otherwise a dedicated follow-up
  notice, implementer's call on exact timing/dedup given the outbox ordering isn't guaranteed
  (same non-guarantee pass #2's notification design already documents for refund outcomes).

## Non-goals

No changes to how the PayMongo refund call itself works (pass #1's `CreateRefundAsync`,
already fixed this session, is reused as-is). No SMS/push for the Owner approval prompt beyond
the existing bell/notification channel. No audit log beyond `RefundRequest`'s own decision
fields (append-only audit trail is a separately-flagged, not-yet-built gap from earlier in
this session — out of scope here). No bulk/batch refund actions on the review page.

## Testing

- `AutomaticRefund = false` (default): cancel an eligible online-paid booking → `RefundRequest`
  created `PendingReview`, no PayMongo call yet, tenant bell fires.
- Admin confirms → `AwaitingOwnerApproval`, Owner bell/approval-prompt fires, no PayMongo call
  yet. Owner approves → for a GCash/card/Maya payment, PayMongo is called and resolves exactly
  as pass #1's tests already cover; for a QR Ph payment, request moves to
  `AwaitingManualTransfer` with no PayMongo call attempted at all.
- Owner denies an Admin's proposed Confirm → request reopens to `PendingReview`, nothing else
  changes.
- Admin rejects, Owner approves the rejection → `Booking.RefundStatus = Rejected`, customer
  gets a rejection notice, no money moves.
- `AutomaticRefund = true`: cancellation behaves identically to pass #1 — no `RefundRequest`
  ever created, `BookingRefundDueDomainEvent` fires immediately.
- QR Ph manual path: customer submits e-wallet details via the public status page → visible to
  Owner/Admin on the review page; marking `ManuallyRefunded` moves the request to terminal
  state and updates `Booking.RefundStatus = Succeeded`.
- Staff account attempts to load the review page or call any of the new commands → rejected
  (403/not-authorized), consistent with Owner/Admin-only access.
