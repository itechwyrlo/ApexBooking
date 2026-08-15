# Refund Manual-Transfer Redesign — Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move e-wallet capture (provider, account number, account name) to the top of both cancellation flows (public customer page and staff modal); collapse the Refund Requests review page to two actions (Confirm — with a required receipt upload — and Reject); add an "Enable Refunds" toggle to Payment Settings, off by default, that hides all refund fields until turned on.

**Architecture:** Every screen here already exists — this is a shape change, not new pages. The public cancel page's post-cancel e-wallet step moves to pre-cancel; the staff cancel modal gains the same fields; the review page drops the owner-gate and mark-sent UI in favor of one receipt-upload modal.

**Tech Stack:** React 19, TypeScript, react-router-dom 7, axios, Bootstrap 5. No new dependencies — receipt upload uses the same `FormData`/`multipart/form-data` pattern already used by `accountService.uploadMyProfilePhoto`.

**Spec:** [docs/superpowers/specs/2026-08-15-refund-manual-transfer-redesign-design.md](../../../../ApexBooking/docs/superpowers/specs/2026-08-15-refund-manual-transfer-redesign-design.md) (lives in the ApexBooking repo — this plan implements its frontend half)

## Global Constraints

- Requires the ApexBooking backend plan (`2026-08-15-refund-manual-transfer-redesign-backend.md`, in the ApexBooking repo) to be implemented and deployed first — every task here calls an endpoint that plan changes or removes.
- Wire shapes match their TypeScript interfaces field-for-field (camelCase JSON naming policy) — no mappers, per this codebase's existing convention.
- Public API calls (cancel page, refund-status page) go through `publicClient`, not `authClient` — no auth token; the tenant resolves from the token itself.
- File uploads use `FormData` + `headers: { 'Content-Type': 'multipart/form-data' }`, matching `accountService.uploadMyProfilePhoto` exactly.

---

## Task 1: Payment Settings — Enable Refunds toggle

**Files:**
- Modify: `src/interfaces/IPaymentPolicy.ts`
- Modify: `src/services/paymentPolicyService.ts` (verify only — the service already round-trips whatever shape the interface declares; check no field is hardcoded before assuming no change is needed)
- Modify: `src/pages/booking/settings/PaymentSettingsPage.tsx`

**Interfaces:**
- Consumes: `PaymentPolicyDto` gains `refundEnabled`, drops `automaticRefund` (ApexBooking backend plan Task 7).
- Produces: `IPaymentPolicy.refundEnabled: boolean`.

- [ ] **Step 1: Update `IPaymentPolicy.ts`**

```typescript
import type { PaymentRequirementType } from '../types/PaymentRequirementType'
import type { DepositType } from '../types/DepositType'

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentPolicy.PaymentPolicyDto
export interface IPaymentPolicy {
  requirementType: PaymentRequirementType
  depositType: DepositType
  depositValue: number
  onTimeRefundPercent: number
  lateCancellationRefundPercent: number
  refundReviewDeadlineDays: number
  refundEnabled: boolean
}
```

- [ ] **Step 2: Verify `paymentPolicyService.ts` needs no change**

Read the file — `getPaymentPolicy`/`updatePaymentPolicy` both pass the whole object through untouched (no per-field mapping), so the interface change from Step 1 is sufficient. No edit needed here.

- [ ] **Step 3: Update `PaymentSettingsPage.tsx`**

Update `DEFAULT_VALUES` (drop `automaticRefund`, add `refundEnabled: false`):

```typescript
const DEFAULT_VALUES: IPaymentPolicy = {
  requirementType: PaymentRequirementType.None,
  depositType: DepositType.Percentage,
  depositValue: 0,
  onTimeRefundPercent: 100,
  lateCancellationRefundPercent: 0,
  refundReviewDeadlineDays: 7,
  refundEnabled: false,
}
```

`validate(values)` — the `onTimeRefundPercent`/`lateCancellationRefundPercent`/`refundReviewDeadlineDays` checks stay (they still apply once refunds are visible; validating hidden fields that still hold their last-known value is harmless and matches the existing `requiresDeposit` pattern, which validates deposit fields even when the requirement type would hide them).

Add a top-level "Enable Refunds" switch, and wrap the On-Time Refund %, Late Cancellation Refund %, and Refund Review Deadline fields (currently unconditional) in `{values.refundEnabled && (...)}`. Remove the "Automatic Refund" `FormGroup` entirely. The new field order in the form:

```tsx
<FormGroup label="Enable Refunds" htmlFor="refundEnabled">
  <div className="form-check form-switch">
    <input
      type="checkbox"
      role="switch"
      id="refundEnabled"
      className="form-check-input"
      checked={values.refundEnabled}
      onChange={(e) => handleChange({ ...values, refundEnabled: e.target.checked })}
    />
    <label className="form-check-label" htmlFor="refundEnabled">
      {values.refundEnabled ? 'On — cancelled online payments can be refunded' : 'Off — cancellations never trigger a refund'}
    </label>
  </div>
  <div className="form-text">
    When off (default), cancelling an online-paid booking never creates a refund request — no matter the timing or policy below.
  </div>
</FormGroup>

{values.refundEnabled && (
  <>
    <div className="row">
      <div className="col-sm-6">
        <FormGroup label="On-Time Refund (%)" htmlFor="onTimeRefundPercent" error={errors.onTimeRefundPercent}>
          <NumberInput
            id="onTimeRefundPercent"
            min={0}
            max={100}
            decimals={2}
            isInvalid={!!errors.onTimeRefundPercent}
            value={values.onTimeRefundPercent}
            onChange={(value) => handleChange({ ...values, onTimeRefundPercent: value })}
          />
          <div className="form-text">How much is refunded for an on-time cancellation. Defaults to 100% — lower it to retain an admin fee.</div>
        </FormGroup>
      </div>
      <div className="col-sm-6">
        <FormGroup label="Late Cancellation Refund (%)" htmlFor="lateCancellationRefundPercent" error={errors.lateCancellationRefundPercent}>
          <NumberInput
            id="lateCancellationRefundPercent"
            min={0}
            max={100}
            decimals={2}
            isInvalid={!!errors.lateCancellationRefundPercent}
            value={values.lateCancellationRefundPercent}
            onChange={(value) => handleChange({ ...values, lateCancellationRefundPercent: value })}
          />
          <div className="form-text">
            Only used when Late Cancellation Policy (Booking Settings) is "Partial Refund." Defaults to 0%.
          </div>
        </FormGroup>
      </div>
    </div>

    <FormGroup label="Refund Review Deadline (days)" htmlFor="refundReviewDeadlineDays" error={errors.refundReviewDeadlineDays}>
      <NumberInput
        id="refundReviewDeadlineDays"
        min={1}
        decimals={0}
        isInvalid={!!errors.refundReviewDeadlineDays}
        value={values.refundReviewDeadlineDays}
        onChange={(value) => handleChange({ ...values, refundReviewDeadlineDays: value })}
      />
      <div className="form-text">How many days staff have to review a pending refund before it's flagged as overdue.</div>
    </FormGroup>
  </>
)}
```

Place the "Enable Refunds" switch immediately after the "Payment Requirement" `FormGroup` (before the deposit fields), so refund settings are visually grouped together and gated as one unit.

- [ ] **Step 4: Typecheck and lint**

Run: `npm run typecheck` (or `tsc --noEmit` if no dedicated script — check `package.json` first)
Run: `npm run lint`
Expected: no errors in `IPaymentPolicy.ts` or `PaymentSettingsPage.tsx`.

- [ ] **Step 5: Manual smoke test**

Start the dev server (`npm run dev`), open Payment Settings as an Owner. Confirm: the toggle defaults off on a fresh policy; toggling it on reveals the three fields with their last-saved values; saving with it off, then reloading the page, keeps it off; toggling on, changing a value, saving, toggling off, saving again, then toggling back on shows the previously-saved value (not reset to default) — confirms values survive a hide/show round trip since they're never cleared client-side, only conditionally rendered.

- [ ] **Step 6: Commit**

```bash
git add src/interfaces/IPaymentPolicy.ts src/pages/booking/settings/PaymentSettingsPage.tsx
git commit -m "feat: replace Automatic Refund switch with an Enable Refunds toggle that gates the whole section"
```

---

## Task 2: Collapse `RefundRequestStatus` + status badge

**Files:**
- Modify: `src/types/RefundRequestStatus.ts`
- Modify: `src/components/refunds/RefundRequestStatusBadge.tsx`

**Interfaces:**
- Consumes: backend `RefundRequestStatus` enum collapsed to `PendingReview | Refunded | Rejected` (ApexBooking backend plan Task 2).

- [ ] **Step 1: Update `RefundRequestStatus.ts`**

```typescript
export const RefundRequestStatus = {
  PendingReview: 'PendingReview',
  Refunded: 'Refunded',
  Rejected: 'Rejected',
} as const

export type RefundRequestStatus = (typeof RefundRequestStatus)[keyof typeof RefundRequestStatus]
```

- [ ] **Step 2: Update `RefundRequestStatusBadge.tsx`**

```typescript
const STATUS_TONE: Record<RefundRequestStatus, BadgeTone> = {
  [RefundRequestStatus.PendingReview]: 'warning',
  [RefundRequestStatus.Refunded]: 'success',
  [RefundRequestStatus.Rejected]: 'danger',
}
```

- [ ] **Step 3: Typecheck**

Run: `npm run typecheck`
Expected: this surfaces every other file still referencing a removed status value (`AwaitingOwnerApproval`, `Approved`, `Processing`, `AwaitingManualTransfer`, `ManuallyRefunded`, `Succeeded`, `Failed`) as a compile error — that's expected; Tasks 3-5 fix them.

- [ ] **Step 4: Commit**

```bash
git add src/types/RefundRequestStatus.ts src/components/refunds/RefundRequestStatusBadge.tsx
git commit -m "feat: collapse RefundRequestStatus to PendingReview/Refunded/Rejected"
```

---

## Task 3: `IRefundRequest` + `refundRequestService` — receipt upload, drop owner-gate/mark-sent

**Files:**
- Modify: `src/interfaces/IRefundRequest.ts`
- Modify: `src/services/refundRequestService.ts`

**Interfaces:**
- Consumes: `RefundRequestSummaryDto` new shape, `ConfirmRefundRequestCommand` as multipart (ApexBooking backend plan Tasks 11, 13, 15).
- Produces: `confirmRefundRequest(id: string, receipt: File): Promise<void>`; `rejectRefundRequest(id: string, reason: string): Promise<void>` (unchanged); `getRefundRequests`/`getRefundLog` (unchanged shape, updated payload type).

- [ ] **Step 1: Update `IRefundRequest.ts`**

```typescript
import type { RefundRequestStatus } from '../types/RefundRequestStatus'

// Mirrors ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests.RefundRequestSummaryDto
export interface IRefundRequest {
  id: string
  bookingId: string
  bookingReference: string
  customerName: string
  requestedAmount: number
  amountPaid: number
  payMongoPaymentId: string | null
  currencyCode: string
  status: RefundRequestStatus
  rejectionReason: string | null
  customerEwalletProvider: string
  customerEwalletNumber: string
  customerEwalletName: string
  receiptUrl: string | null
  createdAt: string
  dueDate: string
}
```

- [ ] **Step 2: Update `refundRequestService.ts`**

```typescript
import { authClient } from '../api/clients/authClient'
import type { IRefundRequest } from '../interfaces/IRefundRequest'
import type { IRefundLogEntry } from '../interfaces/IRefundLogEntry'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'

// Wire shape from ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests.RefundRequestSummaryDto
// matches IRefundRequest field-for-field (camelCase JSON naming policy), so no mapper is needed.

export async function getRefundRequests(params: IPageParams = {}): Promise<IPagedResult<IRefundRequest>> {
  const response = await authClient.get<IPagedResult<IRefundRequest>>('/api/refund-requests', {
    params: { pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 10 },
  })
  return response.data
}

export async function confirmRefundRequest(id: string, receipt: File): Promise<void> {
  const formData = new FormData()
  formData.append('receipt', receipt)
  await authClient.post(`/api/refund-requests/${id}/confirm`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

export async function rejectRefundRequest(id: string, reason: string): Promise<void> {
  await authClient.post(`/api/refund-requests/${id}/reject`, { reason })
}

export async function getRefundLog(limit = 20): Promise<IRefundLogEntry[]> {
  const response = await authClient.get<IRefundLogEntry[]>('/api/refund-requests/log', { params: { limit } })
  return response.data
}
```
(Dropped `approveOwnerGate`, `denyOwnerGate`, `markManualRefundSent` — no longer exist backend-side.)

- [ ] **Step 3: Check `IRefundLogEntry.ts` for a `paymentMethodType` field**

Run: `grep -n "paymentMethodType" src/interfaces/IRefundLogEntry.ts`
If present, remove it — the backend DTO drops it (ApexBooking backend plan Task 13).

- [ ] **Step 4: Typecheck**

Run: `npm run typecheck`
Expected: errors remain in `RefundRequestTable.tsx`, `RefundRequestsPage.tsx`, `MarkAsSentConfirm.tsx` (Task 5 fixes these) — none should remain in `IRefundRequest.ts`/`refundRequestService.ts`/`IRefundLogEntry.ts` themselves.

- [ ] **Step 5: Commit**

```bash
git add src/interfaces/IRefundRequest.ts src/services/refundRequestService.ts src/interfaces/IRefundLogEntry.ts
git commit -m "feat: refund request service uploads a receipt on Confirm, drops owner-gate/mark-sent calls"
```

---

## Task 4: `RefundRequestTable.tsx` — two actions, always-visible e-wallet

**Files:**
- Modify: `src/components/refunds/RefundRequestTable.tsx`

**Interfaces:**
- Consumes: `IRefundRequest` new shape (Task 3), `RefundRequestStatus` collapsed (Task 2).
- Produces: `IRefundRequestTableProps` drops `currentUserRole`, `onApprove`, `onDeny`, `onMarkSent`; keeps `onConfirm`, `onReject`.

- [ ] **Step 1: Rewrite the component**

Drop the "Method" column (was `isAutoRefundEligible`-driven, that field is gone) and the `currentUserRole`/owner-gate/mark-sent branches. E-wallet is always present now (no more "Not submitted yet" case):

```tsx
import { Badge } from '../common/Badge'
import { RowActions } from '../common/RowActions'
import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RefundRequestStatusBadge } from './RefundRequestStatusBadge'
import { formatRelativeTime } from '../../utils/formatDateTime'
import { RefundRequestStatus } from '../../types/RefundRequestStatus'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

const COLUMNS = ['Booking', 'Customer', 'Amount', 'Amount Paid', 'E-Wallet', 'Status', 'Due', '']

function isDueSoon(dueDate: string): boolean {
  const diffMs = new Date(dueDate).getTime() - Date.now()
  return diffMs <= 2 * 24 * 60 * 60 * 1000
}

interface IRefundRequestTableProps {
  requests: IRefundRequest[]
  isLoading?: boolean
  busyId: string | null
  onConfirm: (request: IRefundRequest) => void
  onReject: (request: IRefundRequest) => void
}

export function RefundRequestTable({ requests, isLoading, busyId, onConfirm, onReject }: IRefundRequestTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (requests.length === 0) {
    return <EmptyState icon="refund" title="No refunds need review right now." description="Cancelled bookings eligible for a refund will show up here." />
  }

  return (
    <div className="table-responsive">
      <table className="table table-stack align-middle mb-0">
        <thead>
          <tr className="text-muted small text-uppercase">
            {COLUMNS.map((column) => (
              <th key={column} scope="col" className="fw-semibold">
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {requests.map((request) => {
            const isBusy = busyId === request.id
            const actions =
              request.status === RefundRequestStatus.PendingReview
                ? [
                    { label: 'Confirm', icon: 'check-circle', tone: 'primary' as const, onClick: () => onConfirm(request) },
                    { label: 'Reject', icon: 'x-circle', tone: 'delete' as const, onClick: () => onReject(request) },
                  ]
                : []

            return (
              <tr key={request.id}>
                <td className="fw-semibold" data-label="Booking">{request.bookingReference}</td>
                <td data-label="Customer">{request.customerName}</td>
                <td data-label="Amount">{request.requestedAmount.toFixed(2)} {request.currencyCode}</td>
                <td data-label="Amount Paid">
                  <span className="text-muted small">{request.amountPaid.toFixed(2)} {request.currencyCode}</span>
                </td>
                <td data-label="E-Wallet">
                  <span className="small">
                    {request.customerEwalletProvider}: <span className="fw-semibold">{request.customerEwalletNumber}</span>
                    <br />
                    <span className="text-muted">{request.customerEwalletName}</span>
                  </span>
                </td>
                <td data-label="Status">
                  <RefundRequestStatusBadge status={request.status} />
                </td>
                <td data-label="Due">
                  <span className={request.status === RefundRequestStatus.PendingReview && isDueSoon(request.dueDate) ? 'text-danger fw-semibold' : 'text-muted'}>
                    {formatRelativeTime(request.dueDate)}
                  </span>
                </td>
                <td className="text-end" data-label="Actions">
                  <RowActions
                    actions={actions.map((action) => ({ ...action, disabled: isBusy, isLoading: isBusy }))}
                    forceMenu={actions.length > 1}
                  />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
```
(`Badge` import is now unused if nothing else in the file uses it — remove the import if so; check before leaving it, an unused import will fail lint.)

- [ ] **Step 2: Typecheck and lint**

Run: `npm run typecheck && npm run lint`
Expected: errors remain only in `RefundRequestsPage.tsx`/`MarkAsSentConfirm.tsx` (fixed in Task 5).

- [ ] **Step 3: Commit**

```bash
git add src/components/refunds/RefundRequestTable.tsx
git commit -m "feat: refund table shows Confirm/Reject only, e-wallet details always visible"
```

---

## Task 5: Receipt-upload modal + `RefundRequestsPage.tsx` rewrite

**Files:**
- Delete: `src/components/refunds/MarkAsSentConfirm.tsx`
- Create: `src/components/refunds/ConfirmRefundReceiptModal.tsx`
- Modify: `src/pages/booking/RefundRequestsPage.tsx`

**Interfaces:**
- Consumes: `confirmRefundRequest(id, receipt: File)` (Task 3), `RefundRequestTable`'s narrowed props (Task 4).
- Produces: `IConfirmRefundReceiptModalProps { isOpen, request, isSubmitting, errorMessage, onClose, onSubmit: (receipt: File) => void }`.

- [ ] **Step 1: Delete `MarkAsSentConfirm.tsx`**

```bash
git rm src/components/refunds/MarkAsSentConfirm.tsx
```

- [ ] **Step 2: Create `ConfirmRefundReceiptModal.tsx`**

Follows this codebase's existing file-input pattern used for profile-photo upload (check `src/components/account/` for the exact input styling if one exists; otherwise a plain `<input type="file">` styled with Bootstrap's `.form-control` is consistent with every other form field in this codebase):

```tsx
import { useState } from 'react'
import { Modal } from '../common/Modal'
import { Button } from '../common/Button'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

interface IConfirmRefundReceiptModalProps {
  isOpen: boolean
  request: IRefundRequest | null
  isSubmitting: boolean
  errorMessage: string | null
  onClose: () => void
  onSubmit: (receipt: File) => void
}

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_SIZE_BYTES = 5 * 1024 * 1024

export function ConfirmRefundReceiptModal({ isOpen, request, isSubmitting, errorMessage, onClose, onSubmit }: IConfirmRefundReceiptModalProps) {
  const [file, setFile] = useState<File | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)

  if (!request) return null

  const handleClose = () => {
    setFile(null)
    setValidationError(null)
    onClose()
  }

  const handleFileChange = (selected: File | null) => {
    setValidationError(null)
    if (!selected) {
      setFile(null)
      return
    }
    if (!ALLOWED_TYPES.includes(selected.type)) {
      setValidationError('Receipt must be a JPEG, PNG, or WebP image.')
      setFile(null)
      return
    }
    if (selected.size > MAX_SIZE_BYTES) {
      setValidationError('Receipt must be 5MB or smaller.')
      setFile(null)
      return
    }
    setFile(selected)
  }

  const handleSubmit = () => {
    if (!file) return
    onSubmit(file)
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Confirm Refund"
      description={`Booking ${request.bookingReference}`}
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={!file}>
            Confirm Refund
          </Button>
        </div>
      }
    >
      <p className="mb-2">
        Confirm you&apos;ve manually sent{' '}
        <strong>{request.requestedAmount.toFixed(2)} {request.currencyCode}</strong> to the customer&apos;s{' '}
        {request.customerEwalletProvider} ({request.customerEwalletNumber}, {request.customerEwalletName}), then attach a screenshot or photo of the transfer receipt.
      </p>
      {(errorMessage || validationError) && (
        <div className="alert alert-danger" role="alert">
          {validationError ?? errorMessage}
        </div>
      )}
      <div className="mb-2">
        <label className="form-label small" htmlFor="refundReceipt">
          Receipt
        </label>
        <input
          type="file"
          id="refundReceipt"
          className="form-control"
          accept="image/jpeg,image/png,image/webp"
          disabled={isSubmitting}
          onChange={(e) => handleFileChange(e.target.files?.[0] ?? null)}
        />
      </div>
    </Modal>
  )
}
```

- [ ] **Step 3: Rewrite `RefundRequestsPage.tsx`**

```tsx
import { useState } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Pagination } from '../../components/common/Pagination'
import { RefundRequestTable } from '../../components/refunds/RefundRequestTable'
import { RejectRefundModal } from '../../components/refunds/RejectRefundModal'
import { ConfirmRefundReceiptModal } from '../../components/refunds/ConfirmRefundReceiptModal'
import { useRefundRequests } from '../../hooks/useRefundRequests'
import { useToast } from '../../hooks/useToast'
import { confirmRefundRequest, rejectRefundRequest } from '../../services/refundRequestService'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

const PAGE_SIZE = 10

export function RefundRequestsPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { requests, total, isLoading, refetch } = useRefundRequests({ pageNumber, pageSize: PAGE_SIZE })
  const { showToast } = useToast()
  const [busyId, setBusyId] = useState<string | null>(null)
  const [rejectTarget, setRejectTarget] = useState<IRefundRequest | null>(null)
  const [isRejecting, setIsRejecting] = useState(false)
  const [confirmTarget, setConfirmTarget] = useState<IRefundRequest | null>(null)
  const [isConfirming, setIsConfirming] = useState(false)
  const [confirmError, setConfirmError] = useState<string | null>(null)

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const handleConfirmSubmit = async (receipt: File) => {
    if (!confirmTarget) return
    setIsConfirming(true)
    setConfirmError(null)
    setBusyId(confirmTarget.id)
    try {
      await confirmRefundRequest(confirmTarget.id, receipt)
      showToast('success', 'Refund confirmed.')
      setConfirmTarget(null)
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      setConfirmError(detail ?? 'Failed to confirm the refund. Please try again.')
    } finally {
      setIsConfirming(false)
      setBusyId(null)
    }
  }

  const handleRejectSubmit = async (reason: string) => {
    if (!rejectTarget) return
    setIsRejecting(true)
    try {
      await rejectRefundRequest(rejectTarget.id, reason)
      showToast('success', 'Refund rejected.')
      setRejectTarget(null)
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to reject the refund. Please try again.')
    } finally {
      setIsRejecting(false)
    }
  }

  return (
    <div>
      <PageHeader title="Refunds" description="Review and act on refund requests from cancelled bookings." />
      <Card>
        <RefundRequestTable
          requests={requests}
          isLoading={isLoading}
          busyId={busyId}
          onConfirm={setConfirmTarget}
          onReject={setRejectTarget}
        />

        {!isLoading && total > 0 && (
          <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <p className="text-muted small mb-0">
              Page {pageNumber} of {totalPages} ({total} requests)
            </p>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
          </div>
        )}
      </Card>
      {rejectTarget && (
        <RejectRefundModal
          isOpen={rejectTarget !== null}
          bookingReference={rejectTarget.bookingReference}
          isSubmitting={isRejecting}
          onClose={() => setRejectTarget(null)}
          onSubmit={handleRejectSubmit}
        />
      )}
      <ConfirmRefundReceiptModal
        isOpen={confirmTarget !== null}
        request={confirmTarget}
        isSubmitting={isConfirming}
        errorMessage={confirmError}
        onClose={() => {
          setConfirmTarget(null)
          setConfirmError(null)
        }}
        onSubmit={handleConfirmSubmit}
      />
    </div>
  )
}
```
(Dropped the `useAuth`/`Role`-derived `currentUserRole` entirely — no longer needed once the owner-gate is gone.)

- [ ] **Step 4: Typecheck, lint, and build**

Run: `npm run typecheck && npm run lint && npm run build`
Expected: SUCCESS.

- [ ] **Step 5: Manual smoke test**

With a `PendingReview` refund request seeded (needs the backend plan deployed, or a direct DB row), open the Refunds page as an Owner or Admin. Confirm: only Confirm/Reject actions show; clicking Confirm opens the receipt modal with the Confirm button disabled until a file is chosen; picking an oversized or wrong-type file shows a client-side error and keeps the button disabled; a valid file submits and the row updates to `Refunded`. Reject still opens its existing reason modal unchanged.

- [ ] **Step 6: Commit**

```bash
git add src/components/refunds/ConfirmRefundReceiptModal.tsx src/pages/booking/RefundRequestsPage.tsx
git rm src/components/refunds/MarkAsSentConfirm.tsx 2>/dev/null || true
git commit -m "feat: Confirm action requires a receipt upload; drop owner-gate UI"
```

---

## Task 6: `EwalletSubmissionForm.tsx` — add Account Name

**Files:**
- Modify: `src/components/refunds/EwalletSubmissionForm.tsx`

**Interfaces:**
- Produces: `IEwalletSubmissionFormProps` gains `name: string`, `onNameChange: (name: string) => void`; `onSubmit` now also requires `name` to be non-blank.

- [ ] **Step 1: Add the field**

```tsx
interface IEwalletSubmissionFormProps {
  provider: string
  number: string
  name: string
  isSubmitting: boolean
  errorMessage: string | null
  onProviderChange: (provider: string) => void
  onNumberChange: (number: string) => void
  onNameChange: (name: string) => void
  onSubmit: () => void
}

// Shared between CancelBookingModal (staff, on a customer's behalf) and CancelBookingPage
// (customer, self-service) — same form, two entry points, so the two flows never drift apart.
export function EwalletSubmissionForm({
  provider,
  number,
  name,
  isSubmitting,
  errorMessage,
  onProviderChange,
  onNumberChange,
  onNameChange,
  onSubmit,
}: IEwalletSubmissionFormProps) {
  return (
    <>
      {errorMessage && (
        <div className="alert alert-danger pb-alert-danger" role="alert">
          {errorMessage}
        </div>
      )}
      <p className="pb-muted mb-3">Tell us where to send your refund.</p>
      <div className="mb-3">
        <label className="form-label small" htmlFor="ewalletProvider">
          E-wallet
        </label>
        <select
          id="ewalletProvider"
          className="form-select"
          value={provider}
          onChange={(e) => onProviderChange(e.target.value)}
          disabled={isSubmitting}
        >
          <option value="GCash">GCash</option>
          <option value="Maya">Maya</option>
        </select>
      </div>
      <div className="mb-3">
        <label className="form-label small" htmlFor="ewalletNumber">
          Account Number
        </label>
        <input
          type="tel"
          id="ewalletNumber"
          className="form-control"
          value={number}
          onChange={(e) => onNumberChange(e.target.value)}
          disabled={isSubmitting}
        />
      </div>
      <div className="mb-3">
        <label className="form-label small" htmlFor="ewalletName">
          Account Name
        </label>
        <input
          type="text"
          id="ewalletName"
          className="form-control"
          value={name}
          onChange={(e) => onNameChange(e.target.value)}
          disabled={isSubmitting}
        />
      </div>
      <button type="button" className="btn pb-btn-primary w-100" onClick={onSubmit} disabled={isSubmitting || !number.trim() || !name.trim()}>
        {isSubmitting ? 'Submitting…' : 'Submit'}
      </button>
    </>
  )
}
```
Note the updated doc comment — this form no longer renders standalone on `RefundStatusPage` (Task 9 makes that page read-only), only inline on the two cancel flows.

- [ ] **Step 2: Typecheck**

Run: `npm run typecheck`
Expected: errors surface at every current call site (`CancelBookingPage.tsx`, `RefundStatusPage.tsx`) — Tasks 7 and 9 fix those.

- [ ] **Step 3: Commit**

```bash
git add src/components/refunds/EwalletSubmissionForm.tsx
git commit -m "feat: add Account Name field to the shared e-wallet submission form"
```

---

## Task 7: Public cancel page — collect e-wallet details up front

**Files:**
- Modify: `src/interfaces/publicBooking/ICancellableBooking.ts`
- Modify: `src/services/publicBookingService.ts`
- Modify: `src/pages/public/CancelBookingPage.tsx`

**Interfaces:**
- Consumes: `CancellableBookingDto.isRefundEligible` (ApexBooking backend plan Task 9), `CancelBookingByTokenCommand` new fields (ApexBooking backend plan Task 8), `EwalletSubmissionForm` with `name` (Task 6).
- Produces: `cancelBookingByToken(token, reason, ewalletProvider, ewalletNumber, ewalletName)`.

- [ ] **Step 1: Update `ICancellableBooking.ts`**

```typescript
// Mirrors ApexBooking.Core.Application.Features.PublicBookings.Queries.GetCancellableBooking.CancellableBookingDto
export interface ICancellableBooking {
  bookingReference: string
  serviceName: string
  staffName: string
  branchName: string
  scheduledDate: string
  scheduledStartTime: string
  canCancelOnline: boolean
  unavailableReason: string | null
  isRefundEligible: boolean
}
```

- [ ] **Step 2: Update `cancelBookingByToken` in `publicBookingService.ts`**

```typescript
export async function cancelBookingByToken(
  token: string,
  reason: string | null,
  ewalletProvider: string | null,
  ewalletNumber: string | null,
  ewalletName: string | null,
): Promise<void> {
  await publicClient.post('/api/public/bookings/cancel', { token, reason, ewalletProvider, ewalletNumber, ewalletName })
}
```

- [ ] **Step 3: Rewrite `CancelBookingPage.tsx`**

The e-wallet fields move into the pre-cancel form (shown only when `booking.isRefundEligible`), required before the Cancel button is enabled; the post-cancel screen drops the `getRefundStatus` poll and inline form entirely:

```tsx
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { PublicBookingLayout } from '../../layouts/PublicBookingLayout'
import { Button } from '../../components/common/Button'
import { EwalletSubmissionForm } from '../../components/refunds/EwalletSubmissionForm'
import { getCancellableBooking, cancelBookingByToken } from '../../services/publicBookingService'
import { formatDisplayDate, formatDisplayTime } from '../../utils/formatDateTime'
import type { ICancellableBooking } from '../../interfaces/publicBooking/ICancellableBooking'

const UNAVAILABLE_COPY: Record<string, string> = {
  'already-cancelled': 'This booking has already been cancelled.',
  'already-completed': 'This appointment has already been completed and can no longer be cancelled.',
  'already-no-show': 'This appointment was already marked as a no-show.',
  'pending-payment': "This booking is still awaiting payment confirmation and can't be cancelled online yet.",
  'past-cutoff': "This booking can no longer be cancelled online — it's too close to the appointment time. Please contact the business directly.",
}

type PageState = 'loading' | 'error' | 'preview' | 'cancelling' | 'cancelled'

export function CancelBookingPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [state, setState] = useState<PageState>('loading')
  const [booking, setBooking] = useState<ICancellableBooking | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [reason, setReason] = useState('')
  const [ewalletProvider, setEwalletProvider] = useState('GCash')
  const [ewalletNumber, setEwalletNumber] = useState('')
  const [ewalletName, setEwalletName] = useState('')

  useEffect(() => {
    if (!token) {
      setState('error')
      setErrorMessage('This cancellation link is missing its token.')
      return
    }

    let isMounted = true

    getCancellableBooking(token)
      .then((result) => {
        if (!isMounted) return
        setBooking(result)
        setState('preview')
      })
      .catch((error) => {
        if (!isMounted) return
        const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
        setErrorMessage(detail ?? 'This cancellation link is invalid or has expired.')
        setState('error')
      })

    return () => {
      isMounted = false
    }
  }, [token])

  const needsEwallet = booking?.isRefundEligible ?? false
  const canSubmit = !needsEwallet || (ewalletNumber.trim().length > 0 && ewalletName.trim().length > 0)

  const handleCancel = async () => {
    if (!canSubmit) return
    setState('cancelling')
    try {
      await cancelBookingByToken(
        token,
        reason.trim() || null,
        needsEwallet ? ewalletProvider : null,
        needsEwallet ? ewalletNumber.trim() : null,
        needsEwallet ? ewalletName.trim() : null,
      )
      setState('cancelled')
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      setErrorMessage(detail ?? 'Failed to cancel this booking. Please try again.')
      setState('preview')
    }
  }

  return (
    <PublicBookingLayout currentIndex={0} total={1} stepLabels={[]} showProgress={false}>
      {state === 'loading' && <p className="pb-muted">Loading your booking…</p>}

      {state === 'error' && (
        <div className="text-center">
          <h1 className="pb-display fs-3 mb-2">This link isn't available</h1>
          <p className="pb-muted">{errorMessage}</p>
        </div>
      )}

      {state === 'cancelled' && (
        <div className="text-center">
          <div className="pb-badge-success mb-4">✓ Cancelled</div>
          <h1 className="pb-display fs-3 mb-2">Your booking has been cancelled</h1>
          {booking && (
            <p className="pb-muted">
              {booking.serviceName} with {booking.staffName} on {formatDisplayDate(booking.scheduledDate)} has been cancelled.
              {needsEwallet && ' Your refund request has been submitted — we’ll email you once it’s reviewed.'}
            </p>
          )}
        </div>
      )}

      {(state === 'preview' || state === 'cancelling') && booking && (
        <div>
          <h1 className="pb-display fs-3 mb-3">Cancel your booking?</h1>

          <div className="pb-ticket text-start mb-4">
            <div className="p-4">
              <div className="fw-semibold fs-5 mb-1">{booking.serviceName}</div>
              <div className="pb-muted mb-3">
                with {booking.staffName} · {booking.branchName}
              </div>
              <div className="d-flex justify-content-between pb-mono fs-6 fw-semibold">
                <span>{formatDisplayDate(booking.scheduledDate)}</span>
                <span>{formatDisplayTime(booking.scheduledStartTime)}</span>
              </div>
            </div>
            <div className="pb-ticket-divider mx-4" />
            <div className="p-4">
              <div className="pb-muted small text-uppercase mb-1" style={{ letterSpacing: '0.06em' }}>
                Booking reference
              </div>
              <div className="pb-mono pb-muted fw-semibold">{booking.bookingReference}</div>
            </div>
          </div>

          {!booking.canCancelOnline ? (
            <div className="alert alert-warning" role="alert">
              {UNAVAILABLE_COPY[booking.unavailableReason ?? ''] ?? 'This booking can no longer be cancelled online.'}
            </div>
          ) : (
            <>
              {errorMessage && (
                <div className="alert alert-danger pb-alert-danger" role="alert">
                  {errorMessage}
                </div>
              )}

              <div className="mb-3">
                <label className="form-label small" htmlFor="cancelReason">
                  Reason (optional)
                </label>
                <textarea
                  id="cancelReason"
                  className="form-control"
                  rows={3}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  disabled={state === 'cancelling'}
                />
              </div>

              {needsEwallet && (
                <EwalletSubmissionForm
                  provider={ewalletProvider}
                  number={ewalletNumber}
                  name={ewalletName}
                  isSubmitting={false}
                  errorMessage={null}
                  onProviderChange={setEwalletProvider}
                  onNumberChange={setEwalletNumber}
                  onNameChange={setEwalletName}
                  onSubmit={() => {}}
                />
              )}

              <Button variant="danger" fullWidth isLoading={state === 'cancelling'} disabled={!canSubmit} onClick={handleCancel}>
                {state === 'cancelling' ? 'Cancelling…' : 'Cancel My Booking'}
              </Button>
            </>
          )}
        </div>
      )}
    </PublicBookingLayout>
  )
}
```
`EwalletSubmissionForm` is reused here purely for its fields — its own internal submit button is dropped in favor of the page's single "Cancel My Booking" button, so `onSubmit={() => {}}` is intentional (the form's built-in button never renders because `isSubmitting` styling isn't relevant here; if this reads awkwardly once built, consider a small prop like `hideSubmitButton` on `EwalletSubmissionForm` instead — implementer's call, functionally equivalent either way).

- [ ] **Step 4: Typecheck, lint, build**

Run: `npm run typecheck && npm run lint && npm run build`
Expected: SUCCESS.

- [ ] **Step 5: Manual smoke test**

With the ApexBooking backend deployed and a tenant that has `RefundEnabled = true`: cancel an online-paid booking via the public link — confirm the e-wallet fields appear before cancelling and the Cancel button stays disabled until Number and Name are filled. Cancel a pay-in-visit booking (or one with `RefundEnabled = false`) — confirm no e-wallet fields appear and cancellation proceeds with just the reason.

- [ ] **Step 6: Commit**

```bash
git add src/interfaces/publicBooking/ICancellableBooking.ts src/services/publicBookingService.ts src/pages/public/CancelBookingPage.tsx
git commit -m "feat: collect e-wallet details before cancelling, not after"
```

---

## Task 8: Staff cancel modal — same up-front e-wallet fields

**Files:**
- Modify: `src/services/bookingService.ts`
- Modify: `src/components/appointments/CancelBookingModal.tsx`

**Interfaces:**
- Consumes: `CancelBookingCommand` new fields (ApexBooking backend plan Task 8), `usePaymentPolicy()` (existing hook), `EwalletSubmissionForm` with `name` (Task 6).
- Produces: `cancelBooking(bookingId, reason, ewalletProvider, ewalletNumber, ewalletName)`.

- [ ] **Step 1: Update `cancelBooking` in `bookingService.ts`**

```typescript
export async function cancelBooking(
  bookingId: string,
  reason: string,
  ewalletProvider: string | null,
  ewalletNumber: string | null,
  ewalletName: string | null,
): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/cancel`, { reason, ewalletProvider, ewalletNumber, ewalletName })
}
```

- [ ] **Step 2: Rewrite `CancelBookingModal.tsx`**

Eligibility is derived from the booking already on hand (`ITenantBooking.requiresUpfrontPayment`/`paymentConfirmedVia`, both already present per `src/interfaces/ITenantBooking.ts`) plus the tenant's `RefundEnabled` from `usePaymentPolicy()`:

```tsx
import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import { EwalletSubmissionForm } from '../refunds/EwalletSubmissionForm'
import { useToast } from '../../hooks/useToast'
import { usePaymentPolicy } from '../../hooks/usePaymentPolicy'
import { cancelBooking } from '../../services/bookingService'
import { isRequired } from '../../utils/validators'
import { PaymentConfirmationMethod } from '../../types/PaymentConfirmationMethod'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface ICancelBookingModalProps {
  booking: ITenantBooking | null
  onClose: () => void
  onCancelled: () => void
}

export function CancelBookingModal({ booking, onClose, onCancelled }: ICancelBookingModalProps) {
  const { showToast } = useToast()
  const { policy } = usePaymentPolicy()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [ewalletProvider, setEwalletProvider] = useState('GCash')
  const [ewalletNumber, setEwalletNumber] = useState('')
  const [ewalletName, setEwalletName] = useState('')

  useEffect(() => {
    if (booking) {
      setReason('')
      setError(null)
      setEwalletProvider('GCash')
      setEwalletNumber('')
      setEwalletName('')
    }
  }, [booking])

  const needsEwallet =
    policy?.refundEnabled === true && booking?.requiresUpfrontPayment === true && booking?.paymentConfirmedVia === PaymentConfirmationMethod.Online

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!booking) return

    if (!isRequired(reason)) {
      setError('A cancellation reason is required.')
      return
    }

    if (needsEwallet && (!ewalletNumber.trim() || !ewalletName.trim())) {
      setError('E-wallet account number and name are required for this refund-eligible booking.')
      return
    }

    setIsSubmitting(true)
    try {
      await cancelBooking(
        booking.bookingId,
        reason.trim(),
        needsEwallet ? ewalletProvider : null,
        needsEwallet ? ewalletNumber.trim() : null,
        needsEwallet ? ewalletName.trim() : null,
      )
      showToast('success', `Appointment ${booking.bookingReference} was cancelled.`)
      onCancelled()
      onClose()
    } catch (submitError) {
      const detail = axios.isAxiosError(submitError)
        ? (submitError.response?.data as { detail?: string } | undefined)?.detail
        : undefined
      showToast('error', detail ?? 'Failed to cancel this appointment. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal isOpen={booking !== null} title="Cancel Appointment" onClose={onClose}>
      <form noValidate onSubmit={handleSubmit}>
        <p className="text-muted">
          Cancelling <strong>{booking?.bookingReference}</strong> for {booking?.customerName}. This cannot be undone.
        </p>
        <FormGroup label="Reason" htmlFor="cancellationReason" required error={error ?? undefined}>
          <textarea
            id="cancellationReason"
            rows={3}
            className={`form-control ${error ? 'is-invalid' : ''}`}
            value={reason}
            onChange={(e) => {
              setReason(e.target.value)
              setError(null)
            }}
          />
        </FormGroup>
        {needsEwallet && (
          <EwalletSubmissionForm
            provider={ewalletProvider}
            number={ewalletNumber}
            name={ewalletName}
            isSubmitting={false}
            errorMessage={null}
            onProviderChange={setEwalletProvider}
            onNumberChange={setEwalletNumber}
            onNameChange={setEwalletName}
            onSubmit={() => {}}
          />
        )}
        <div className="modal-form-actions">
          <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmitting}>
            Keep Appointment
          </Button>
          <Button type="submit" variant="danger" isLoading={isSubmitting}>
            {isSubmitting ? 'Cancelling...' : 'Cancel Appointment'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
```

- [ ] **Step 3: Typecheck, lint, build**

Run: `npm run typecheck && npm run lint && npm run build`
Expected: SUCCESS.

- [ ] **Step 4: Manual smoke test**

As staff/Admin, open the appointments list, cancel a refund-eligible booking (online-paid, `RefundEnabled = true`) — confirm the e-wallet fields appear and the submit button is blocked by the modal's own validation until Reason + Number + Name are filled. Cancel a pay-in-visit booking — confirm no e-wallet fields appear.

- [ ] **Step 5: Commit**

```bash
git add src/services/bookingService.ts src/components/appointments/CancelBookingModal.tsx
git commit -m "feat: staff cancel modal collects e-wallet details for refund-eligible bookings"
```

---

## Task 9: Public refund-status page — read-only, receipt link

**Files:**
- Modify: `src/interfaces/IRefundStatus.ts`
- Modify: `src/services/refundStatusService.ts`
- Modify: `src/pages/public/RefundStatusPage.tsx`

**Interfaces:**
- Consumes: `RefundStatusDto` drops `needsEwalletDetails`, adds `receiptUrl` (ApexBooking backend plan Task 13).

- [ ] **Step 1: Update `IRefundStatus.ts`**

```typescript
import type { RefundRequestStatus } from '../types/RefundRequestStatus'

// Mirrors ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus.RefundStatusDto
export interface IRefundStatus {
  bookingReference: string
  status: RefundRequestStatus | null
  amount: number | null
  currencyCode: string
  businessContactPhoneNumber: string | null
  receiptUrl: string | null
}
```

- [ ] **Step 2: Update `refundStatusService.ts`**

```typescript
import { publicClient } from '../api/clients/publicClient'
import type { IRefundStatus } from '../interfaces/IRefundStatus'

// Un-prefixed by design, same as getCancellableBooking in publicBookingService.ts — the token
// resolves its own tenant, not the URL's slug.

export async function getRefundStatus(token: string): Promise<IRefundStatus> {
  const response = await publicClient.get<IRefundStatus>(`/api/public/refund-status/${token}`)
  return response.data
}
```
(Dropped `submitRefundEwalletDetails` — the endpoint it called no longer exists.)

- [ ] **Step 3: Rewrite `RefundStatusPage.tsx`**

Drops the e-wallet form entirely; adds a receipt link when present:

```tsx
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { PublicBookingLayout } from '../../layouts/PublicBookingLayout'
import { getRefundStatus } from '../../services/refundStatusService'
import { RefundRequestStatus } from '../../types/RefundRequestStatus'
import type { IRefundStatus } from '../../interfaces/IRefundStatus'

type PageState = 'loading' | 'error' | 'ready'

const STATUS_COPY: Partial<Record<RefundRequestStatus, string>> = {
  [RefundRequestStatus.PendingReview]: 'Your refund is awaiting review.',
  [RefundRequestStatus.Refunded]: 'Your refund has been sent.',
  [RefundRequestStatus.Rejected]: 'This refund request was not approved.',
}

export function RefundStatusPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [state, setState] = useState<PageState>('loading')
  const [status, setStatus] = useState<IRefundStatus | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    if (!token) {
      setState('error')
      setErrorMessage('This refund status link is missing its token.')
      return
    }

    let isMounted = true

    getRefundStatus(token)
      .then((result) => {
        if (!isMounted) return
        setStatus(result)
        setState('ready')
      })
      .catch((error) => {
        if (!isMounted) return
        const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
        setErrorMessage(detail ?? 'This refund status link is invalid or has expired.')
        setState('error')
      })

    return () => {
      isMounted = false
    }
  }, [token])

  return (
    <PublicBookingLayout currentIndex={0} total={1} stepLabels={[]} showProgress={false}>
      {state === 'loading' && <p className="pb-muted">Loading your refund status…</p>}

      {state === 'error' && (
        <div className="text-center">
          <h1 className="pb-display fs-3 mb-2">This link isn't available</h1>
          <p className="pb-muted">{errorMessage}</p>
        </div>
      )}

      {status && state === 'ready' && (
        <div>
          <h1 className="pb-display fs-3 mb-3">Refund Status</h1>

          <div className="pb-ticket text-start mb-4">
            <div className="p-4">
              <div className="pb-muted small text-uppercase mb-1" style={{ letterSpacing: '0.06em' }}>
                Booking reference
              </div>
              <div className="pb-mono fw-semibold mb-3">{status.bookingReference}</div>

              {status.status === null ? (
                <p className="mb-0">There's no refund associated with this booking.</p>
              ) : (
                <>
                  <p className="mb-0">{STATUS_COPY[status.status]}</p>
                  {status.amount !== null && (
                    <p className="fw-semibold fs-5 mt-2 mb-0">
                      {status.amount.toFixed(2)} {status.currencyCode}
                    </p>
                  )}
                </>
              )}
            </div>
          </div>

          {status.receiptUrl && (
            <div className="text-center mb-4">
              <a href={status.receiptUrl} target="_blank" rel="noreferrer" className="btn pb-btn-primary">
                View receipt
              </a>
            </div>
          )}

          {status.businessContactPhoneNumber && (
            <p className="pb-muted small mt-4 text-center">
              Questions? Contact the business at {status.businessContactPhoneNumber}.
            </p>
          )}
        </div>
      )}
    </PublicBookingLayout>
  )
}
```

- [ ] **Step 4: Typecheck, lint, build**

Run: `npm run typecheck && npm run lint && npm run build`
Expected: SUCCESS — this should now be the first point where the whole frontend compiles clean.

- [ ] **Step 5: Commit**

```bash
git add src/interfaces/IRefundStatus.ts src/services/refundStatusService.ts src/pages/public/RefundStatusPage.tsx
git commit -m "feat: refund status page becomes read-only with a receipt link"
```

---

## Task 10: Full verification pass

**Files:** none (verification only).

- [ ] **Step 1: Full typecheck, lint, build**

Run: `npm run typecheck && npm run lint && npm run build`
Expected: SUCCESS, zero errors, zero new warnings.

- [ ] **Step 2: Grep for stale references**

Run: `grep -rln "isAutoRefundEligible\|AwaitingOwnerApproval\|AwaitingManualTransfer\|ManuallyRefunded\|needsEwalletDetails\|approveOwnerGate\|denyOwnerGate\|markManualRefundSent\|submitRefundEwalletDetails\|automaticRefund" --include=*.tsx --include=*.ts src`
Expected: no results.

- [ ] **Step 3: Manual end-to-end smoke test**

With the ApexBooking backend deployed and a test tenant with `RefundEnabled = true`: run the full loop — public customer cancels an online-paid booking, submits e-wallet details up front → Owner sees it on the Refunds page with the e-wallet already visible → Owner clicks Confirm, uploads a receipt → customer's `RefundStatusPage` (via the emailed link, once Task 12 of the backend plan ships) shows "Refunded" with a working receipt link. Then repeat with Reject instead of Confirm — confirm the row moves to `Rejected` and no receipt link appears. Then toggle `RefundEnabled` off in Payment Settings and confirm a new cancellation shows no e-wallet fields on either the public page or the staff modal, and creates no refund request.

- [ ] **Step 4: Report status**

Summarize: typecheck/lint/build results, grep results, and the outcome of the manual smoke test.

---

## Self-Review Notes

- **Spec coverage:** GCash/Maya provider + account number + account name capture (Tasks 6, 7, 8) ✓. Up-front collection on both public and staff cancel flows (Tasks 7, 8) ✓. Confirm/Reject-only review page with required receipt upload (Tasks 4, 5) ✓. Enable Refunds toggle hiding dependent fields (Task 1) ✓. Read-only refund-status page with receipt link (Task 9) ✓.
- **Placeholder scan:** no TBD/TODO. Task 7's `onSubmit={() => {}}` on the reused form component is a deliberate, explained no-op (the page's own button drives submission), not an unresolved placeholder.
- **Type consistency:** `IRefundRequest`, `IPaymentPolicy`, `ICancellableBooking`, `IRefundStatus`, `RefundRequestStatus` field names match between the task that defines them and every task that consumes them (Tasks 1-3 define, 4-9 consume).
