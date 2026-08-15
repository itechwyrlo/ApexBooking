# Refund Review Workflow — Design Spec

Status: Approved (source: user-authored feature prompt, refined through
clarifying questions below)
Scope: Frontend for the backend's Refund Review Workflow Phase 1
(`ApexBooking` repo, `docs/superpowers/specs/2026-08-11-refund-review-and-manual-confirmation-design.md`
and `docs/superpowers/plans/2026-08-11-refund-review-workflow-phase1.md`) —
the `AutomaticRefund` payment-setting toggle, the Refund Requests review
page (list, Confirm/Reject, Owner approval gate), and a refund-review
deadline reminder. Does **not** cover the customer-facing refund-status
page, e-wallet capture, or the manual-transfer confirmation UI — those are
backend Phase 2, not built yet on either side.

## Source of Truth

Backend request/response shapes below were read directly from the
`ApexBooking` repo's actual source this session (the entity, DTOs, and
controller were authored in this same session, not guessed) —
`RefundRequest.cs`, `GetPendingRefundRequestsQuery.cs`,
`RefundRequestsController.cs`, `PaymentPolicy.cs`. See "API Contract" for
exact wire shapes, and "Backend Prerequisite" for the two fields this
design needs that don't exist in the backend yet.

## Current State

No refund-related frontend code exists yet — confirmed via `pages/`
listing (`public/CancelBookingPage.tsx` exists for the customer
cancellation flow, but nothing for refund review). The established
reference pattern for a list+action admin page is
`FailedNotificationsPage.tsx` (`Card` + table + `RowActions`, backed by
`useFailedOutboxMessages.ts` + `superAdminService.ts`) — this feature
follows the same shape. `PaymentSettingsPage.tsx` already exists and
already submits through `updatePaymentPolicy()` / `IPaymentPolicy` — this
feature extends that interface and form rather than replacing them.

## Resolved Ambiguities (via clarifying questions)

1. **Owner-gate UX** — resolved: no live popup/modal. The Owner reaches
   `AwaitingOwnerApproval` rows via the existing bell notification, landing
   on the same Refund Requests page everyone else uses — no new
   real-time-interrupt component.
2. **Notification bell** — resolved (by omission/consistency, not asked
   again after being raised): no click-through added for the two new
   notification types. `NotificationBell.tsx` today has zero click-through
   on *any* notification type; adding it only for refund ones would be an
   inconsistent special case. The deadline reminder banner (see below)
   already provides a direct "Review Now" path for the urgent case, making
   bell click-through redundant for this pass.
3. **Refund review deadline — anchor** — resolved: measured from
   `RefundRequest.CreatedAt` (when the request entered `PendingReview`),
   not the appointment's scheduled date.
4. **Refund review deadline — configurability** — resolved: a new
   per-tenant setting (`RefundReviewDeadlineDays`) in Payment Settings, not
   a fixed system constant.
5. **Refund review deadline — which statuses count** — resolved: only
   `PendingReview` (nobody has acted at all). `AwaitingOwnerApproval`
   doesn't count — an Admin already acted, so it's no longer "unchecked."
6. **Refund review deadline — popup behavior** — resolved: dismissible,
   reappears next login/page-load if still unresolved. Not a hard-blocking
   modal. The 2-day warning window itself is a **fixed** constant, not a
   second configurable setting alongside the deadline-days value.

## Backend Prerequisite (not yet built — required before this frontend work is usable)

Two additions to the already-implemented `ApexBooking.Core.Persistence`/
`Core.Application` refund workflow, following the exact same pattern
`PaymentPolicy.AutomaticRefund` already used (see backend plan Task 1):

1. `PaymentPolicy.RefundReviewDeadlineDays` (int) — new entity property + EF
   column + `UpdatePaymentPolicyCommand`/`PaymentPolicyDto` field, same
   shape as `AutomaticRefund`.
2. `RefundRequestSummaryDto` (in `GetPendingRefundRequestsQuery.cs`) gains
   `DueDate` (`DateTime`) — computed in `GetPendingRefundRequestsHandler` as
   `request.CreatedAt.AddDays(tenant's PaymentPolicy.RefundReviewDeadlineDays)`.
   Computed server-side so the frontend never needs to know the tenant's
   deadline setting itself, just compares `dueDate` to "now."

This frontend spec assumes both exist. Flagging explicitly rather than
guessing field names silently, per this session's earlier lesson on the
PayMongo webhook field-name assumption that turned out wrong.

## API Contract

Base URL: existing `VITE_API_BASE_URL` via `authClient` (bearer token +
`withCredentials`, already wired) — same client `paymentPolicyService.ts`
and `teamService.ts` already use.

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/api/Tenant/policy/payment` | — | `PaymentPolicyDto` (extended, see below) |
| PUT | `/api/Tenant/policy/payment` | `PaymentPolicyDto` (extended) | `204` |
| GET | `/api/refund-requests` | — | `RefundRequestSummaryDto[]` |
| POST | `/api/refund-requests/{id}/confirm` | — | `204` |
| POST | `/api/refund-requests/{id}/reject` | `{ reason: string }` | `204` |
| POST | `/api/refund-requests/{id}/owner-approve` | — | `204` |
| POST | `/api/refund-requests/{id}/owner-deny` | — | `204` |

`PaymentPolicyDto` (wire — camelCase; extends the existing shape with 2 new
fields):
```json
{
  "requirementType": "None | DepositRequired | FullPaymentRequired",
  "depositType": "Percentage | FixedAmount",
  "depositValue": "number",
  "refundPercent": "number",
  "automaticRefund": "boolean",
  "refundReviewDeadlineDays": "number"
}
```

`RefundRequestSummaryDto` (wire — camelCase; `RefundRequestStatus` is a
string enum via the global `JsonStringEnumConverter`):
```json
{
  "id": "guid",
  "bookingId": "guid",
  "bookingReference": "string",
  "customerName": "string",
  "requestedAmount": "number",
  "currencyCode": "string",
  "isAutoRefundEligible": "boolean",
  "status": "PendingReview | AwaitingOwnerApproval | Approved | Rejected | Processing | AwaitingManualTransfer | ManuallyRefunded | Succeeded | Failed",
  "rejectionReason": "string | null",
  "createdAt": "ISO 8601 string",
  "dueDate": "ISO 8601 string"
}
```

**Access control (confirmed from `RefundRequestsController.cs`):** `GET`,
`confirm`, `reject` require the `ManagementOnly` policy (Owner or Admin).
`owner-approve`/`owner-deny` require `[Authorize(Roles = "Owner")]`
specifically — an Admin calling either of those gets a `403`, so the
frontend must hide those two actions from Admins even though they can see
the row.

**Error shape** (`GlobalExceptionHandler`, matches the Add Team Member
spec's documented convention): `{ "status": 400, "title": "...", "detail": "...", "errors": null }`.
`reject` with an empty/missing `reason` is rejected server-side by
`RejectRefundRequestCommandValidator` (`400`, `detail` explains why) — the
frontend's own required-field check on the reason textarea is a UX nicety,
not the only guard.

## Frontend Changes

```
src/
  types/
    RefundRequestStatus.ts        # new — mirrors the 9-value backend enum
  interfaces/
    IPaymentPolicy.ts              # edit — add automaticRefund, refundReviewDeadlineDays
    IRefundRequest.ts              # new — mirrors RefundRequestSummaryDto
  services/
    paymentPolicyService.ts        # unchanged — same functions, wider DTO passes through as-is
    refundRequestService.ts        # new — getRefundRequests(), confirmRefundRequest(id),
                                    #   rejectRefundRequest(id, reason), approveOwnerGate(id), denyOwnerGate(id)
  hooks/
    useRefundRequests.ts           # new — mirrors useFailedOutboxMessages.ts (loading/error/refetch)
    useRefundReviewReminder.ts     # new — computes due-soon count from the same list data; session-only dismissal state
  components/
    refunds/
      RefundRequestTable.tsx       # new — mirrors FailedOutboxMessageTable.tsx
      RejectRefundModal.tsx        # new — built on shared Modal + FormGroup (reason textarea)
      RefundReviewReminderBanner.tsx  # new — dismissible banner, "Review Now" -> navigate('/app/booking/refunds')
  pages/
    booking/
      RefundRequestsPage.tsx       # new — mirrors FailedNotificationsPage.tsx
      settings/
        PaymentSettingsPage.tsx    # edit — add Automatic Refund toggle + Refund Review Deadline (days) field
  config/
    navigation/
      booking.nav.config.ts        # edit — new "Refunds" item, roles: [Owner, Admin], section: 'manage'
  routes/
    AppRoutes.tsx                   # edit — register /app/booking/refunds -> RefundRequestsPage
  layouts/
    DashboardLayout.tsx (or equivalent authenticated shell)  # edit — mount RefundReviewReminderBanner
```

`RefundRequestStatus.ts` mirrors `TenantMemberStatus.ts`'s confirmed
shape exactly — a `const` object of the 9 status strings plus a derived
union type via `(typeof RefundRequestStatus)[keyof typeof RefundRequestStatus]`.

### `PaymentSettingsPage.tsx` (edit)

Two new fields added to the existing form, after "Refund Allowance (%)":
- **Automatic Refund** — checkbox/switch, label "Automatically process
  refunds via PayMongo when a cancellation is eligible," help text
  clarifying the default is off (manual review).
- **Refund Review Deadline (days)** — `NumberInput` (reusing the same
  component `depositValue`/`refundPercent` already use), min 1, help text:
  "How many days staff have to review a pending refund before it's
  flagged as overdue."

Both flow through the existing `values`/`handleChange`/`handleSubmit`
state and the unchanged `updatePaymentPolicy()` call — no new submit path.

### `RefundRequestsPage.tsx` (new)

Same shape as `FailedNotificationsPage.tsx`: `PageHeader` + `Card` wrapping
`RefundRequestTable`. `useRefundRequests()` for data, `useAuth()` for the
current user's role (`Role.Owner` vs `Role.Admin`) to decide which actions
`RefundRequestTable` renders per row.

### `RefundRequestTable.tsx` (new)

Columns: Booking Reference, Customer, Amount (`{requestedAmount} {currencyCode}`),
Refund Method (`Badge`: "Auto" if `isAutoRefundEligible` else "Manual"),
Status (`Badge`, tone mapped per status — e.g. `PendingReview`→warning,
`Approved`/`Succeeded`/`ManuallyRefunded`→success, `Rejected`/`Failed`→danger,
others→neutral), Due (relative time via the existing `formatRelativeTime`,
styled danger/warning if `dueDate` is within 2 days or past), Actions.

Actions per row, computed from `status` + current role:
- `PendingReview` (Owner or Admin) → `RowActions` "Confirm" / "Reject"
  buttons. Confirm calls `confirmRefundRequest(id)` directly. Reject opens
  `RejectRefundModal`.
- `AwaitingOwnerApproval` **and current user is Owner** → "Approve" /
  "Deny" buttons, calling `approveOwnerGate(id)` / `denyOwnerGate(id)`.
- `AwaitingOwnerApproval` **and current user is Admin** → no buttons, just
  the status badge (their own proposed decision, waiting on the Owner).
- All other statuses → status badge only, no actions (Phase 2 territory).

Empty state: reuses `EmptyState` — "No refunds need review right now."

### `RejectRefundModal.tsx` (new)

Built on the shared `Modal` component. One required `FormGroup` textarea
("Reason"), submit disabled while empty (client-side nicety; the backend
validator is the real guard per the API Contract note above). On submit:
`rejectRefundRequest(id, reason)` → success closes modal + toast + calls
the page's `refetch()`; failure keeps the modal open, surfaces
`error.response?.data?.detail` in an error toast (same convention as
`AddTeamMemberModal`).

### `useRefundReviewReminder.ts` + `RefundReviewReminderBanner.tsx` (new)

The hook re-derives "due soon" from the same `RefundRequestSummaryDto[]`
shape `useRefundRequests` already fetches (own fetch call at the layout
level, since the banner must render regardless of which page the user is
on after login — not dependent on `RefundRequestsPage` being mounted).
Filters to `status === 'PendingReview'` and `dueDate` within 2 days
(including already-passed). Dismissal is plain React state, not persisted
to storage — satisfies "reappears next login" for free, since a fresh
mount (new login, or a hard page reload) naturally resets it, without
adding a session-storage mechanism for something this low-stakes.

Rendered only for `Role.Owner`/`Role.Admin` (mount-guarded the same way
`isOwner` already gates `PaymentGatewayCard` in `PaymentSettingsPage`).
Banner: count + "Review Now" (routes to `/app/booking/refunds`) + dismiss
(×). Not a blocking modal, per the resolved ambiguity above.

## Non-Goals

No customer-facing refund-status page (backend Phase 2, not built). No
e-wallet capture UI. No manual-transfer confirmation action
(`AwaitingManualTransfer`/`ManuallyRefunded` render status-only in the
table this pass). No notification-bell click-through. No blocking/modal
version of the deadline reminder. No second configurable setting for the
2-day warning window. No bulk actions on the Refund Requests table.

## Deliverable Tracking

`PROJECT_TRACKER.md`'s Booking Module table gets a new "Refund Review"
row following the existing format once implementation is complete.
