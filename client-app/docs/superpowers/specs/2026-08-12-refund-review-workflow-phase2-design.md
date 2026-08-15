# Refund Review Workflow — Phase 2 Design Spec

Status: Approved (source: iterative conversation — payment-verification gaps
found while live-testing Phase 1, then formalized through clarifying
questions below)
Scope: Everything Phase 1 deliberately deferred — pagination/table
uniformity on the Refund Requests page, payment-verification detail
(amount actually paid + PayMongo reference), the "Mark as Sent" manual-
transfer action, the customer-facing refund-status page (with e-wallet
number submission), and the two remaining notifications (refund-status
link on the cancellation email, a rejection notice). Builds directly on
Phase 1's already-shipped `RefundRequest` state machine — no changes to
that state machine's transitions, only to what's visible/actionable around
it.

## Source of Truth

Backend shapes below were verified directly from the `ApexBooking` repo
this session (not assumed) — `BusinessProfile.cs`,
`UpdateBusinessProfileCommand.cs`/`Handler.cs`, `GetBusinessProfileQuery.cs`,
`TenantConfiguration.cs`'s `OwnsOne(t => t.BusinessProfile, ...)` mapping,
`TenantController.cs`. Frontend shapes verified from `IBusinessProfile.ts`,
`businessProfileService.ts`, `BusinessProfilePage.tsx`. See each section
below for exact current shapes and what extends them.

## Current State

Phase 1 shipped: `PaymentPolicy.AutomaticRefund`/`OnTimeRefundPercent`/
`LateCancellationRefundPercent`/`RefundReviewDeadlineDays`, the
`RefundRequest` state machine (`PendingReview` →
`AwaitingOwnerApproval`/`Approved`/`Rejected`/`AwaitingManualTransfer`),
the Confirm/Reject/owner-approve/owner-deny commands, and a
`RefundRequestsPage` with no pagination, no payment-verification detail,
and no action at all for `AwaitingManualTransfer` rows. Verified live: the
existing table doesn't match its booking-module siblings
(`TeamMemberTable`/`ServiceTable`/`CustomerTable`) — already corrected
(blank Actions header, `text-end` alignment, feature-specific empty-state
icon) as a small fix ahead of this spec; pagination is the one remaining
uniformity gap, folded into Task item below since Phase 2 reworks this
table anyway.

## Resolved Ambiguities (via clarifying questions)

1. **"Mark as Sent" confirmation** — resolved: plain confirm click (with a
   confirm dialog stating the amount and destination), no required
   reference/note field. Matches this app's existing lightweight-confirm
   pattern elsewhere.
2. **Business contact number** — resolved (from the original Phase 1
   design conversation): a *business* number, not a personal Owner/Admin
   number. Confirmed this session: `Tenant.OwnerContact` already has its
   own `owner_phone_number` column for the owner's personal contact — the
   new field must be named/mapped distinctly (`ContactPhoneNumber` /
   `contact_phone_number`) to avoid confusion with that existing, unrelated
   field.

## Backend Prerequisite (ApexBooking repo)

### 1. Pagination on the refund-requests list

`IRefundRequestStore.GetPendingForTenantAsync` gains `pageNumber`/`pageSize`
and returns a count alongside the page. `GetPendingRefundRequestsQuery`
gains matching `PageNumber`/`PageSize` properties;
`GetPendingRefundRequestsHandler` returns
`ApexBooking.SharedKernel.Models.QueryResult<RefundRequestSummaryDto>`
(confirmed this session — the same wrapper `GET /api/Tenant/team` already
returns, `{ data: IEnumerable<T>, total: int }`) instead of a bare
`IReadOnlyList<T>`. `RefundRequestsController`'s `GET /api/refund-requests`
action takes `pageNumber`/`pageSize` query params and passes them through.

### 2. Payment verification fields

`RefundRequestSummaryDto` gains two fields, both read from the already-
loaded `Booking` in `GetPendingRefundRequestsHandler` (no new query):
- `AmountPaid` (decimal) — `booking.AmountDue`, the original amount, as
  opposed to `RequestedAmount` (the post-policy refund amount — these
  differ whenever a refund percentage < 100% applied).
- `PayMongoPaymentId` (string?) — `booking.PayMongoPaymentId`, shown so an
  Owner/Admin can cross-check the real PayMongo dashboard before trusting
  the request.

### 3. Mark as Sent

New `MarkManualRefundSentCommand(Guid RefundRequestId)`, Owner/Admin
(`ManagementOnly` policy, same as Confirm/Reject). Handler: load via
`IRefundRequestStore`, call `RefundRequest.MarkManuallyRefunded()` (already
exists from Phase 1, currently unused — this is its first caller), then
mirror `ConfirmRefundRequestHandler.ApplyOutcomeAsync`'s pattern to load
the `Booking` and call the existing `booking.RecordRefundOutcome(RefundStatus.Succeeded, request.RequestedAmount)`
— no new `Booking` method needed, `RecordRefundOutcome` (already public
from pass #1's automatic-refund handler) is exactly the right shape for
"a refund resolved successfully," automatic or manual. New route
`POST /api/refund-requests/{id}/mark-sent`.

### 4. Customer refund-status page — backend

Reuses the existing `ICancellationTokenService`/`CancellationTokenPayload`
mechanism the cancel-booking link already uses (`BookingId` + `TenantId`,
same signing key) — not a new token type. Two new public/anonymous
endpoints, same auth pattern as `CancelBookingByTokenHandler` (resolve
token → `BookingId`/`TenantId` → set ambient tenant context via
`SetCurrentTenant` per the anonymous-handler tenant-filter gotcha already
documented in this codebase):

- `GetRefundStatusQuery(string Token)` → `RefundStatusDto(string
  BookingReference, RefundRequestStatus? Status, decimal? Amount, string
  CurrencyCode, string? BusinessContactPhoneNumber, bool
  NeedsEwalletDetails)`. `Status` is `null` if the booking was never
  refund-eligible at all (no `RefundRequest` exists) — the page shows a
  plain "this booking has no refund associated with it" state in that
  case, not an error. `NeedsEwalletDetails` is `true` only when
  `Status == AwaitingManualTransfer` — the one and only condition under
  which the e-wallet form appears.
- `SubmitRefundEwalletDetailsCommand(string Token, string Provider, string
  Number)` — validates the token the same way, loads the `RefundRequest` by
  the resolved `BookingId`, calls `RefundRequest.RecordCustomerEwalletDetails`
  (already exists from Phase 1, currently unused), throws if
  `Status != AwaitingManualTransfer` (the same guard the domain method
  itself already enforces — this command just surfaces that as a clean
  400 rather than an unhandled domain exception).

New controller `RefundStatusController` (`api/public/{slug}/refund-status`,
`[AllowAnonymous]`), mirroring `CancelBookingByTokenHandler`'s existing
controller shape.

### 5. `BusinessProfile.ContactPhoneNumber`

New optional field (`string?`, nullable — not every tenant will fill this
in immediately). `BusinessProfile.UpdateDetails` gains a 4th parameter;
`UpdateBusinessProfileCommand`/`BusinessProfileDto` both gain the field;
`TenantConfiguration.cs`'s `OwnsOne(t => t.BusinessProfile, ...)` block
gains `bp.Property(p => p.ContactPhoneNumber).HasColumnName("contact_phone_number").HasMaxLength(50);`
— deliberately not `owner_phone_number`, which already exists on
`Tenant.OwnerContact` for an unrelated purpose (confirmed this session).

### 6. Notifications

- `SendBookingCancellationEmailHandler`'s existing refund-note block gains
  a link to the refund-status page (`{frontendBaseUrl}/{slug}/refund-status?token=...`,
  built the same way the existing cancel-link URL already is, via
  `IAppUrlService`), shown whenever `Booking.RefundStatus != RefundStatus.None`.
- New event `BookingRefundRejectedDomainEvent(TenantId, Guid BookingId,
  string BookingReference, string RejectionReason, DateTime OccurredAt) :
  IReliableDomainEvent`, raised from `Booking.RejectReviewedRefund()`
  (needs the reason threaded through — currently that method takes no
  parameters; `MarkManualRefundSentCommand`'s caller already has
  `RefundRequest.RejectionReason` on hand to pass in). New handler
  `SendRefundRejectionEmailHandler`, same shape as
  `SendBookingCancellationEmailHandler`, describing the rejection reason in
  plain language.

## Frontend Changes (LocalFlow repo)

```
src/
  interfaces/
    IRefundRequest.ts          # edit — add amountPaid, payMongoPaymentId
    IBusinessProfile.ts         # edit — add contactPhoneNumber (both the read shape and IBusinessProfileValues)
    IRefundStatus.ts            # new — mirrors RefundStatusDto
  services/
    refundRequestService.ts     # edit — getRefundRequests(params) returns paged result; add markManualRefundSent(id)
    businessProfileService.ts   # edit — thread contactPhoneNumber through the wire type + PUT body, same pattern logoUrl already uses
    refundStatusService.ts      # new — getRefundStatus(slug, token), submitRefundEwalletDetails(slug, token, provider, number)
  hooks/
    useRefundRequests.ts        # edit — accept { pageNumber, pageSize }, expose total
    useRefundStatus.ts          # new — mirrors usePaymentPolicy's fetch/loading/refetch shape
  components/
    refunds/
      RefundRequestTable.tsx    # edit — Amount Paid + PayMongo reference columns, "Mark as Sent" action for AwaitingManualTransfer
      MarkAsSentConfirm.tsx     # new — confirm dialog, not a full modal (no form fields, per the resolved ambiguity)
  pages/
    booking/
      RefundRequestsPage.tsx    # edit — add Pagination, same shape as StaffPage/ServicesPage/ClientsPage
      settings/
        BusinessProfilePage.tsx # edit — add Contact Phone Number field
    public/
      RefundStatusPage.tsx      # new — mirrors CancelBookingPage.tsx's public-page shape
  routes/
    AppRoutes.tsx                # edit — register /:slug/refund-status
```

### `RefundRequestsPage.tsx` / `RefundRequestTable.tsx`

Same pagination shape as `StaffPage.tsx` (`PAGE_SIZE = 10`, `Pagination`
component, "Page X of Y (N requests)" footer). Table gains two columns
(Amount Paid, shown muted/secondary next to the existing Amount column
when they differ; PayMongo Reference, monospace, truncated with a title
tooltip for the full value) and a new action: for `AwaitingManualTransfer`
rows, "Mark as Sent" (tone `primary`, matching Confirm/Approve's existing
tone choice for positive workflow actions) opens `MarkAsSentConfirm` — a
lightweight confirm dialog stating the amount and the customer's submitted
e-wallet number (or "not yet submitted" if `NeedsEwalletDetails` info
isn't in yet — staff can still act without it if they already know where
to send it, e.g. contacted the customer directly).

### `RefundStatusPage.tsx` (new)

Public route, same layout shell as `CancelBookingPage.tsx`. Shows booking
reference, refund status in plain language (mirrors the language already
established in `SendBookingCancellationEmailHandler`'s refund-note text:
Pending/Processing → "being processed," Succeeded → "has been processed,"
Rejected → the rejection reason, `None` → no refund section at all), the
business's `ContactPhoneNumber` if set. When `NeedsEwalletDetails` is
true, a simple form (provider `<select>`: GCash/Maya, number `<input>`)
submits via `submitRefundEwalletDetails` — success replaces the form with
"Thanks, we'll send your refund to this number shortly," failure shows an
inline error and leaves the form editable.

### `BusinessProfilePage.tsx`

One new `FormGroup` for Contact Phone Number, optional, no format
validation beyond non-empty-if-provided (phone number formats vary too
much across providers/regions to validate strictly here — same
permissiveness `TeamMemberTable`'s existing `contactNumber` field already
has, per that field rendering `member.contactNumber || '—'` with no format
enforcement visible anywhere in that flow).

## Non-Goals

No SMS notification for the refund-status link (email only, matching
every other Phase 1/pass-#1 notification). No edit/undo on a submitted
e-wallet number (customer re-contacts the business directly if they made a
mistake — matches this being a rare, high-touch manual path already). No
retry/re-attempt UI for a `Failed` automatic refund (still explicitly out
of scope, unchanged from Phase 1). No bulk actions on the Refund Requests
table. No SuperAdmin visibility into tenant-level refund requests.

## Testing

- Cancel an online-paid booking with a partial on-time refund percentage
  configured → Refund Requests table shows both the requested (partial)
  amount and the original amount paid, distinctly.
- A `PendingReview` request confirmed by an Admin, approved by the Owner,
  auto-eligible → resolves exactly as Phase 1 already tested; the
  PayMongo reference shown matches what's visible in PayMongo's own
  sandbox dashboard for that payment.
- A QR Ph request reaching `AwaitingManualTransfer` → customer visits the
  refund-status link from their cancellation email, submits a GCash
  number → staff sees it on the review page, clicks Mark as Sent → row
  moves to `ManuallyRefunded`, `Booking.RefundStatus` becomes `Succeeded`.
- A request rejected with a reason → customer receives a rejection email
  containing that reason; visiting the refund-status page separately shows
  the same reason.
- A booking with no refund eligibility at all → its refund-status link (if
  visited) shows "no refund associated with this booking," not an error.
- Staff (non-Owner/Admin) role → `/refund-status` page itself is public and
  unaffected by role (customer-facing), but `/refunds` (the review page)
  and its new Mark as Sent action remain blocked, unchanged from Phase 1.
