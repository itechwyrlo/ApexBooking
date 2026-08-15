# Refund Manual-Transfer Redesign

## Context

Three earlier passes ([2026-08-08 processing](2026-08-08-booking-cancellation-refund-processing-design.md),
[2026-08-08 notifications](2026-08-08-booking-cancellation-refund-notifications-design.md),
[2026-08-11 review & manual confirmation](2026-08-11-refund-review-and-manual-confirmation-design.md))
built a two-tier refund system: automatic refunds via PayMongo's API for GCash/Maya/cards,
manual e-wallet transfer only for QR Ph (which PayMongo can never refund via API), with an
Owner double-confirmation gate on Admin decisions.

Debugging this session found the manual-transfer path was effectively unreachable: a
`RefundRequest` only asks the customer for e-wallet details once it reaches
`AwaitingManualTransfer`, but the only link the customer ever receives is in the
cancellation email, sent immediately at cancel time — while the request is still
`PendingReview`, before any human has reviewed it. `ConfirmRefundRequestHandler.cs` even
says so directly: *"AwaitingManualTransfer: no Booking-side event yet."* Nothing ever
notified the customer once their action was actually needed.

Rather than patch that one notification gap, this pass replaces the whole refund-execution
model: **every refund becomes a manual e-wallet transfer**, confirmed by a human with a
receipt upload as proof. PayMongo's refund API, the auto/manual eligibility split, and the
Owner-approval gate are removed. E-wallet details move from a late, easy-to-miss follow-up
step to the top of the customer's (or staff's) cancellation flow — collected in the same
request as the cancellation itself.

This pass also folds in two related settings changes: `PaymentPolicy` gains a `RefundEnabled`
flag (default `false`, including for existing tenants) that gates refund evaluation entirely,
and the Payment Settings UI hides all refund-related fields unless it's on.

## Decisions (confirmed with user)

- **All refunds are manual, human-executed, receipt-verified.** No PayMongo refund API call
  anywhere in this flow. `PaymentPolicy.AutomaticRefund`, `RefundRequest.IsAutoRefundEligible`,
  `Booking.PaymentMethodType`, `ProcessRefundOnBookingCancelledHandler`, and
  `IPayMongoService.CreateRefundAsync` are all removed as part of this work, not left dead.
- **Refunds are opt-in per tenant, off by default — including for tenants that exist today.**
  `PaymentPolicy.RefundEnabled` (bool) gates `Booking.EvaluateRefund` entirely: when `false`,
  cancelling an online-paid booking produces no `RefundRequest`, no `Pending` status, no
  refund-related UI or email, full stop. Existing tenants are backfilled to `false` — this is
  a deliberate behavior change, not just a default for new signups.
- **E-wallet details are collected up front, in the same request as the cancellation** — not
  as a later follow-up once a request happens to reach some intermediate review state. The
  cancel page/modal pre-checks eligibility (`RefundEnabled && RequiresUpfrontPayment &&
  PaymentConfirmedVia == Online`) and, when eligible, requires Provider (GCash/Maya)/Account
  Number/Account Name before the cancellation can be submitted. This applies to both the
  public customer-facing cancel page and the staff-initiated cancel modal (staff collects the
  same fields when cancelling on a customer's behalf, e.g. by phone).
- **No Owner double-confirmation gate.** Owner or Admin can act on a `RefundRequest` directly;
  the decision is final immediately. (Review page access itself stays Owner/Admin only — Staff
  still never see or act on refund requests, unchanged from the prior design.)
- **Two actions only: Confirm and Reject.** Reject requires a reason (existing validation,
  unchanged). Confirm requires uploading a receipt/screenshot of the completed transfer before
  it can complete — the upload is a hard gate on the action, not an optional follow-up.
- **The receipt is delivered to the customer as a link, not an email attachment.** Reuses the
  existing `IFileStorageService` (local disk under `wwwroot`, already backs profile photos) —
  no changes needed to `BrevoSmtpService`, which has no attachment support today.

## Backend design (ApexBooking)

### `PaymentPolicy` — new flag

```csharp
public bool RefundEnabled { get; private set; } // default false, backfilled false for existing rows
```
`UpdatePolicy(...)` gains the parameter; validated nowhere further (a plain toggle).
`UpdatePaymentPolicyCommand`/DTO gain `RefundEnabled`; **drop `AutomaticRefund`**.

### `Booking.EvaluateRefund` — new gate

```csharp
private (bool ShouldRefund, decimal Amount) EvaluateRefund(BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)
{
    if (!RequiresUpfrontPayment || PaymentConfirmedVia != PaymentConfirmationMethod.Online)
        return (false, 0m);
    if (paymentPolicy?.RefundEnabled != true)
        return (false, 0m);
    // ...unchanged on-time/late-cancellation percent calculation below...
}
```

### `Booking.Cancel` / `CancelByCustomer` — carry e-wallet details

Both gain three new parameters: `string? ewalletProvider, string? ewalletNumber, string?
ewalletName`. After `EvaluateRefund` returns `ShouldRefund: true`, the three become required:

```csharp
if (shouldRefund)
{
    if (string.IsNullOrWhiteSpace(ewalletProvider) || string.IsNullOrWhiteSpace(ewalletNumber) || string.IsNullOrWhiteSpace(ewalletName))
        throw new BusinessRuleBrokenException("E-wallet details are required to cancel a booking eligible for a refund.");

    RefundStatus = RefundStatus.Pending;
    AddDomainEvent(new BookingRefundEligibleDomainEvent(..., ewalletProvider, ewalletNumber, ewalletName));
}
```
This is what actually enforces "collected up front" — a refund-eligible cancellation cannot
reach the domain without these fields, regardless of what the UI does.

`CancelBookingCommand` (staff) and `CancelBookingByTokenCommand` (public) both gain the same
three optional fields, threaded through to the domain call.

### `RefundRequestStatus` — collapsed

```csharp
public enum RefundRequestStatus { PendingReview, Refunded, Rejected }
```
(was: `PendingReview, AwaitingOwnerApproval, Approved, Rejected, Processing,
AwaitingManualTransfer, ManuallyRefunded, Succeeded, Failed`)

`Booking.RefundStatus` collapses the same way: `None, Pending, Refunded, Rejected` (drops
`Processing`/`Failed`, which only ever existed for PayMongo's async status).

### `RefundRequest` — created with e-wallet already attached

```csharp
public static RefundRequest Create(
    TenantId tenantId, Guid bookingId, decimal requestedAmount, string currencyCode,
    string ewalletProvider, string ewalletNumber, string ewalletName)
// Status = PendingReview; CustomerEwalletProvider/Number/Name set directly, not via a later call.

public void Confirm(Guid decidedByUserId, string receiptUrl)   // PendingReview -> Refunded
public void Reject(Guid decidedByUserId, string reason)        // PendingReview -> Rejected, reason required
```
Removed: `IsAutoRefundEligible`, `OwnerDecidedByUserId/At`, `DecisionAction`,
`RecordTentativeDecision`, `ApplyDirectOwnerDecision`, `ApplyOwnerApproval`,
`ApplyOwnerDenial`, `MarkProcessing`, `MarkSucceeded`, `MarkFailed`,
`RecordCustomerEwalletDetails`, `MarkManuallyRefunded`.

`CreateRefundRequestOnEligibleHandler` (subscribes to `BookingRefundEligibleDomainEvent`)
calls `RefundRequest.Create(...)` with the event's e-wallet fields and raises the tenant bell
notification to Owner + Admin, same as today — no behavior change there beyond dropping the
now-removed `IsAutoRefundEligible` computation.

### Commands (`Features/RefundRequests/`)

- `ConfirmRefundRequestCommand(RefundRequestId, ReceiptFile)` — multipart. Handler: validate
  Owner-or-Admin, load the request, save the file via `IFileStorageService.SaveAsync` under
  `refund-receipts/{tenantId}/{refundRequestId}/{guid}.{ext}` (image content-type/size
  validated the same way `UpdateMyProfilePhotoHandler` already does), `request.Confirm(userId,
  receiptUrl)`, `booking.ConfirmRefund(amount, receiptUrl)`, raise
  `BookingRefundDecidedDomainEvent : IReliableDomainEvent`.
- `RejectRefundRequestCommand(RefundRequestId, Reason)` — Owner-or-Admin, reason required
  (domain-validated). `request.Reject(userId, reason)`, `booking.RejectRefund(reason)`, raise
  the same `BookingRefundDecidedDomainEvent` (carries an `IsConfirmed` flag or the two commands
  raise sibling events — implementer's call, one handler either way).
- **Removed:** `ApproveOwnerGateCommand`, `DenyOwnerGateCommand`, `MarkManualRefundSentCommand`,
  `SubmitRefundEwalletDetailsCommand` (+ its `RefundStatusController` endpoint).
- `GetPendingRefundRequestsQuery`/`GetRefundLogQuery` — drop `IsAutoRefundEligible`,
  `DecisionAction`; e-wallet fields are now always populated (shown from creation, not
  conditionally); add `ReceiptUrl` once confirmed.
- `GetRefundStatusQuery` — drops `NeedsEwalletDetails` (no longer meaningful — details are
  already submitted by the time a `RefundRequest` exists); keeps status/amount/receipt-link
  for the read-only public status check.

### New: `SendRefundDecisionEmailHandler`

Subscribes to `BookingRefundDecidedDomainEvent`. Same load pattern as
`SendBookingCancellationEmailHandler` (tenant → booking → customer email). Sends one of two
templates:
- Confirmed: refund amount + a link to the receipt image (`IFileStorageService`-returned URL).
- Rejected: the rejection reason.

This **replaces** the existing racy `refundNote` block in `SendBookingCancellationEmailHandler`
(which read `Booking.RefundStatus` at an arbitrary point in time and could catch it mid-flight,
or — per the original bug — never catch the real outcome at all). The cancellation email no
longer needs to say anything refund-specific beyond "we'll email you once it's reviewed"; the
decision email is now the single reliable source of truth, fired exactly when the decision
happens.

### Removed entirely

`IPayMongoService.CreateRefundAsync` + its `PayMongoService` implementation,
`ProcessRefundOnBookingCancelledHandler`, `BookingRefundDueDomainEvent`,
`PaymentPolicy.AutomaticRefund` (+ its column), `Booking.PaymentMethodType` (+ its column;
verify no other consumer before dropping — introduced solely for
`IsAutoRefundEligible`), `NotificationEventType.RefundApprovalNeeded`.

## Frontend design (LocalFlow)

### Payment Settings (`PaymentSettingsPage.tsx`)

New "Enable Refunds" switch, default off, at the top of the refund section. On-Time Refund %,
Late Cancellation Refund %, and Refund Review Deadline fields render only when it's on (same
conditional pattern already used for the deposit fields under `requiresDeposit`). The
"Automatic Refund" switch is removed. Values persist in the backend even while hidden, so
toggling back on restores prior settings.

### Public cancel page (`CancelBookingPage.tsx`)

`getCancellableBooking` response gains `isRefundEligible: boolean`. When true, the pre-cancel
screen shows three required fields alongside the existing Reason textarea: Provider (GCash/Maya
select), Account Number, Account Name. `cancelBookingByToken` carries them in the same request.
Post-cancel screen becomes a static confirmation ("your refund request has been submitted —
we'll email you once it's reviewed"); the old post-cancel `getRefundStatus` poll and inline
`EwalletSubmissionForm` render are removed from this page.

### Staff cancel modal (`CancelBookingModal.tsx`)

Same three fields, shown when the booking being cancelled is refund-eligible (derived
client-side from the booking's `RequiresUpfrontPayment`/`PaymentConfirmedVia`, already present
on `TenantBookingSummary`, plus the tenant's `PaymentPolicy.RefundEnabled` from
`usePaymentPolicy`). `CancelBookingCommand` call carries the same three fields.

### Refund review page (`RefundRequestsPage.tsx`)

Two row actions: **Confirm** (opens a modal requiring a receipt image upload before it
submits — `MarkAsSentConfirm.tsx` is repurposed into this) and **Reject** (opens a modal
requiring a reason). E-wallet details are shown directly on the row/detail view since they're
present from creation. Owner-gate UI (approve/deny banners, `RefundReviewReminderBanner.tsx`'s
gate-specific copy if any) is removed; the existing PendingReview-deadline reminder logic
otherwise stays as-is.

### Public refund-status page (`RefundStatusPage.tsx`)

Stays as a read-only status/receipt-link check. Drops the `EwalletSubmissionForm`
render/submit — nothing left to submit there once details are collected at cancel time.

## Migration

- `PaymentPolicy`: add `RefundEnabled` (bool, default `false`, backfilled `false` for existing
  rows); drop `AutomaticRefund`.
- `RefundRequest`: add `CustomerEwalletName`, `ReceiptUrl`; drop `IsAutoRefundEligible`,
  `OwnerDecidedByUserId`, `OwnerDecidedAt`, `DecisionAction`. `CustomerEwalletProvider`/`Number`
  stay, now always populated from creation instead of nullable-until-later.
- `Booking`: drop `PaymentMethodType` (pending the "no other consumer" check above).

## Non-goals

No retry/webhook handling for anything PayMongo-refund-related (removed entirely, not just
disabled). No bulk/batch confirm-reject actions on the review page. No email attachment support
in `BrevoSmtpService` (receipt is a link). No backfill/migration of existing in-flight
`RefundRequest` rows into the new 3-state machine — if any exist in a pre-redesign state at
deploy time, they're handled as a one-off manual data fix, not code.

## Testing

- `RefundEnabled = false`: cancel an online-paid booking → no `RefundRequest`, `Booking.RefundStatus` stays `None`, no refund UI shown on the cancel page.
- `RefundEnabled = true`: customer cancels an eligible booking without filling e-wallet fields → blocked client-side; a direct API call without them → domain throws.
- Customer cancels with valid e-wallet details → `RefundRequest` created `PendingReview` with those details attached; Owner/Admin bell fires.
- Staff cancels an eligible booking via the modal, providing e-wallet details on the customer's behalf → same `RefundRequest` creation path.
- Owner or Admin clicks Confirm without a receipt → blocked. With a receipt → `RefundRequest.Refunded`, `Booking.RefundStatus = Refunded`, customer receives the decision email with a working receipt link.
- Owner or Admin clicks Reject without a reason → blocked. With a reason → `RefundRequest.Rejected`, `Booking.RefundStatus = Rejected`, customer receives the rejection email with the reason.
- Staff-role user attempts to load the review page or call any refund command → rejected, consistent with Owner/Admin-only access.
- Payment Settings: toggling "Enable Refunds" off hides the four dependent fields immediately; saved values survive a toggle-off/toggle-on round trip.
