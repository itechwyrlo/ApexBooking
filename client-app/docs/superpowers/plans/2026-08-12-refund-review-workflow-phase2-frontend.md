# Refund Review Workflow Phase 2 — Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the frontend for Phase 2, per
[2026-08-12-refund-review-workflow-phase2-design.md](../specs/2026-08-12-refund-review-workflow-phase2-design.md):
pagination + payment-verification columns on the Refund Requests table, the
Mark as Sent action, a Contact Phone Number field on Business Profile, and
the new public customer refund-status page with e-wallet submission.

**Architecture:** Extends Phase 1's already-shipped Refund Requests page
using the same hook→service→table shape. The new public page follows
`CancelBookingPage.tsx`'s exact shape (state machine, `publicClient`,
`PublicBookingLayout`) since it's the closest sibling — same anonymous,
token-driven, slug-in-URL-but-not-in-API-call pattern.

**Tech Stack:** React 19, TypeScript, react-router-dom 7, axios, Bootstrap 5. No new dependencies.

## Global Constraints

- Requires the ApexBooking backend plan (`2026-08-12-refund-review-workflow-phase2-backend.md`) to be implemented and deployed first — every task here calls an endpoint that plan creates.
- The new public API calls go through `publicClient`, not `authClient` — no auth token, tenant resolves from the token itself (confirmed pattern: `getCancellableBooking`/`cancelBookingByToken` in `publicBookingService.ts`).
- The refund-status API route itself is **not** slug-prefixed (`/api/public/refund-status/{token}`) even though the frontend page URL is (`/:slug/refund-status?token=...`) — same split `CancelBookingPage`'s API calls already use.

---

### Task 1: `IRefundRequest` + service + hook — payment verification & pagination

**Files:**
- Modify: `src/interfaces/IRefundRequest.ts`
- Modify: `src/services/refundRequestService.ts`
- Modify: `src/hooks/useRefundRequests.ts`

**Interfaces:**
- Produces: `IRefundRequest.amountPaid`, `IRefundRequest.payMongoPaymentId`. `getRefundRequests({ pageNumber, pageSize })` returns `{ data: IRefundRequest[], total: number }`. `markManualRefundSent(id)`. `useRefundRequests({ pageNumber, pageSize })` exposes `total`.

- [ ] **Step 1: Extend the interface**

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
  isAutoRefundEligible: boolean
  status: RefundRequestStatus
  rejectionReason: string | null
  createdAt: string
  dueDate: string
}
```

- [ ] **Step 2: Extend the service**

`refundRequestService.ts` — replace `getRefundRequests`, add `markManualRefundSent`:

```typescript
import { authClient } from '../api/clients/authClient'
import type { IRefundRequest } from '../interfaces/IRefundRequest'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'

// Wire shape from ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests.RefundRequestSummaryDto
// matches IRefundRequest field-for-field (camelCase JSON naming policy), so no mapper is needed.

export async function getRefundRequests(params: IPageParams = {}): Promise<IPagedResult<IRefundRequest>> {
  const response = await authClient.get<IPagedResult<IRefundRequest>>('/api/refund-requests', {
    params: { pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 10 },
  })
  return response.data
}

export async function confirmRefundRequest(id: string): Promise<void> {
  await authClient.post(`/api/refund-requests/${id}/confirm`)
}

export async function rejectRefundRequest(id: string, reason: string): Promise<void> {
  await authClient.post(`/api/refund-requests/${id}/reject`, { reason })
}

export async function approveOwnerGate(id: string): Promise<void> {
  await authClient.post(`/api/refund-requests/${id}/owner-approve`)
}

export async function denyOwnerGate(id: string): Promise<void> {
  await authClient.post(`/api/refund-requests/${id}/owner-deny`)
}

export async function markManualRefundSent(id: string): Promise<void> {
  await authClient.post(`/api/refund-requests/${id}/mark-sent`)
}
```

(`IPagedResult`/`IPageParams` already exist — confirmed used by `superAdminService.ts`'s `getTenantRequests`; reuse those exact types, don't redefine them.)

- [ ] **Step 3: Extend the hook**

```typescript
import { useCallback, useEffect, useState } from 'react'
import { getRefundRequests } from '../services/refundRequestService'
import type { IRefundRequest } from '../interfaces/IRefundRequest'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseRefundRequestsResult {
  requests: IRefundRequest[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useRefundRequests(params: IPageParams = {}): IUseRefundRequestsResult {
  const [requests, setRequests] = useState<IRefundRequest[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getRefundRequests(params)
      .then((result) => {
        if (isMounted) {
          setRequests(result.data)
          setTotal(result.total)
        }
      })
      .catch(() => {
        if (isMounted) setError('Failed to load refund requests.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.pageNumber, params.pageSize, refreshToken])

  return { requests, total, isLoading, error, refetch }
}
```

- [ ] **Step 4: Typecheck**

Run: `cd C:\Users\Wyrlo\projects\LocalFlow && npx tsc -b`
Expected: errors in `RefundRequestTable.tsx`/`RefundRequestsPage.tsx` (they still use the old shape) — expected at this point, fixed in Tasks 2-3. Confirm no errors in the 3 files touched this task themselves.

- [ ] **Step 5: Commit**

```bash
git add src/interfaces/IRefundRequest.ts src/services/refundRequestService.ts src/hooks/useRefundRequests.ts
git commit -m "feat: add payment-verification fields and pagination to refund requests data layer"
```

---

### Task 2: Table — Amount Paid, PayMongo reference, Mark as Sent

**Files:**
- Modify: `src/components/refunds/RefundRequestTable.tsx`
- Create: `src/components/refunds/MarkAsSentConfirm.tsx`

**Interfaces:**
- Consumes: `IRefundRequest.amountPaid`/`payMongoPaymentId` (Task 1).
- Produces: `<RefundRequestTable ... onMarkSent={(request) => void} />`, `<MarkAsSentConfirm isOpen request onClose onConfirm isSubmitting />`.

- [ ] **Step 1: `MarkAsSentConfirm.tsx`**

```tsx
import { Modal } from '../common/Modal'
import { Button } from '../common/Button'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

interface IMarkAsSentConfirmProps {
  isOpen: boolean
  request: IRefundRequest | null
  isSubmitting: boolean
  onClose: () => void
  onConfirm: () => void
}

export function MarkAsSentConfirm({ isOpen, request, isSubmitting, onClose, onConfirm }: IMarkAsSentConfirmProps) {
  if (!request) return null

  return (
    <Modal
      isOpen={isOpen}
      title="Mark Refund as Sent"
      description={`Booking ${request.bookingReference}`}
      onClose={onClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={onConfirm} isLoading={isSubmitting}>
            Confirm Sent
          </Button>
        </div>
      }
    >
      <p className="mb-2">
        Confirm you've manually sent <strong>{request.requestedAmount.toFixed(2)} {request.currencyCode}</strong> to the customer.
      </p>
      {request.customerEwalletNumber ? (
        <p className="text-muted small mb-0">
          Customer's {request.customerEwalletProvider}: <span className="fw-semibold">{request.customerEwalletNumber}</span>
        </p>
      ) : (
        <p className="text-muted small mb-0">The customer hasn't submitted their e-wallet number yet — only proceed if you already know where to send it.</p>
      )}
    </Modal>
  )
}
```

This references `request.customerEwalletNumber`/`customerEwalletProvider`,
which aren't on `IRefundRequest` yet. Add them now, in this same step —
`RefundRequest.CustomerEwalletProvider`/`CustomerEwalletNumber` already
exist on the backend entity from Phase 1 (populated once the customer
submits them via Task 5's page), just not yet exposed on
`RefundRequestSummaryDto`. Backend Task 1 in the companion backend plan
needs one more field pair added to that DTO — add `CustomerEwalletProvider`/
`CustomerEwalletNumber` to `RefundRequestSummaryDto` and its construction
in `GetPendingRefundRequestsHandler` (both `string?`, straight passthrough
from `request.CustomerEwalletProvider`/`request.CustomerEwalletNumber`) —
then mirror it here:

```typescript
export interface IRefundRequest {
  id: string
  bookingId: string
  bookingReference: string
  customerName: string
  requestedAmount: number
  amountPaid: number
  payMongoPaymentId: string | null
  currencyCode: string
  isAutoRefundEligible: boolean
  status: RefundRequestStatus
  rejectionReason: string | null
  customerEwalletProvider: string | null
  customerEwalletNumber: string | null
  createdAt: string
  dueDate: string
}
```

- [ ] **Step 2: Table columns + action**

`RefundRequestTable.tsx` — add two columns (`Amount Paid`, `PayMongo Ref.`)
and the Mark as Sent action:

```tsx
const COLUMNS = ['Booking', 'Customer', 'Amount', 'Amount Paid', 'PayMongo Ref.', 'Method', 'Status', 'Due', '']
```

In the actions block, add a third branch:

```tsx
            } else if (request.status === RefundRequestStatus.AwaitingManualTransfer) {
              actions = [{ label: 'Mark as Sent', icon: 'check-circle', tone: 'primary', onClick: () => onMarkSent(request) }]
            }
```

New cells (inserted after the existing Amount `<td>`):

```tsx
                <td data-label="Amount Paid">
                  <span className="text-muted small">{request.amountPaid.toFixed(2)} {request.currencyCode}</span>
                </td>
                <td data-label="PayMongo Ref.">
                  {request.payMongoPaymentId ? (
                    <span className="pb-mono small text-truncate d-inline-block" style={{ maxWidth: 140 }} title={request.payMongoPaymentId}>
                      {request.payMongoPaymentId}
                    </span>
                  ) : (
                    <span className="text-muted">—</span>
                  )}
                </td>
```

Props gain `onMarkSent: (request: IRefundRequest) => void`.

- [ ] **Step 3: Typecheck and lint**

Run: `npx tsc -b && npx oxlint src/components/refunds`
Expected: no errors in these two files (page-level errors expected until Task 3).

- [ ] **Step 4: Commit**

```bash
git add src/components/refunds/RefundRequestTable.tsx src/components/refunds/MarkAsSentConfirm.tsx src/interfaces/IRefundRequest.ts
git commit -m "feat: add Amount Paid, PayMongo reference, and Mark as Sent to the refund requests table"
```

---

### Task 3: `RefundRequestsPage` — pagination + Mark as Sent wiring

**Files:**
- Modify: `src/pages/booking/RefundRequestsPage.tsx`

**Interfaces:**
- Consumes: everything from Tasks 1-2.

- [ ] **Step 1: Add pagination state, matching `StaffPage.tsx`'s exact shape**

```tsx
import { useState } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Pagination } from '../../components/common/Pagination'
import { RefundRequestTable } from '../../components/refunds/RefundRequestTable'
import { RejectRefundModal } from '../../components/refunds/RejectRefundModal'
import { MarkAsSentConfirm } from '../../components/refunds/MarkAsSentConfirm'
import { useRefundRequests } from '../../hooks/useRefundRequests'
import { useAuth } from '../../hooks/useAuth'
import { useToast } from '../../hooks/useToast'
import {
  confirmRefundRequest,
  rejectRefundRequest,
  approveOwnerGate,
  denyOwnerGate,
  markManualRefundSent,
} from '../../services/refundRequestService'
import { Role } from '../../types/Role'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

const PAGE_SIZE = 10

export function RefundRequestsPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { requests, total, isLoading, refetch } = useRefundRequests({ pageNumber, pageSize: PAGE_SIZE })
  const { user } = useAuth()
  const { showToast } = useToast()
  const [busyId, setBusyId] = useState<string | null>(null)
  const [rejectTarget, setRejectTarget] = useState<IRefundRequest | null>(null)
  const [isRejecting, setIsRejecting] = useState(false)
  const [sendTarget, setSendTarget] = useState<IRefundRequest | null>(null)
  const [isMarkingSent, setIsMarkingSent] = useState(false)

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const currentUserRole = user?.roles.includes(Role.Owner) ? Role.Owner : Role.Admin

  const runAction = async (id: string, action: () => Promise<void>, successMessage: string) => {
    setBusyId(id)
    try {
      await action()
      showToast('success', successMessage)
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Something went wrong. Please try again.')
    } finally {
      setBusyId(null)
    }
  }

  const handleConfirm = (request: IRefundRequest) => runAction(request.id, () => confirmRefundRequest(request.id), 'Refund confirmed.')
  const handleApprove = (request: IRefundRequest) => runAction(request.id, () => approveOwnerGate(request.id), 'Decision approved.')
  const handleDeny = (request: IRefundRequest) => runAction(request.id, () => denyOwnerGate(request.id), 'Decision denied — reopened for review.')

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

  const handleMarkSentConfirm = async () => {
    if (!sendTarget) return
    setIsMarkingSent(true)
    try {
      await markManualRefundSent(sendTarget.id)
      showToast('success', 'Refund marked as sent.')
      setSendTarget(null)
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to mark this refund as sent. Please try again.')
    } finally {
      setIsMarkingSent(false)
    }
  }

  return (
    <div>
      <PageHeader title="Refunds" description="Review and act on refund requests from cancelled bookings." />
      <Card>
        <RefundRequestTable
          requests={requests}
          isLoading={isLoading}
          currentUserRole={currentUserRole}
          busyId={busyId}
          onConfirm={handleConfirm}
          onReject={setRejectTarget}
          onApprove={handleApprove}
          onDeny={handleDeny}
          onMarkSent={setSendTarget}
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
      <MarkAsSentConfirm
        isOpen={sendTarget !== null}
        request={sendTarget}
        isSubmitting={isMarkingSent}
        onClose={() => setSendTarget(null)}
        onConfirm={handleMarkSentConfirm}
      />
    </div>
  )
}
```

- [ ] **Step 2: Typecheck, lint, build**

Run: `npx tsc -b && npx oxlint src && npx vite build`
Expected: no errors.

- [ ] **Step 3: Manual verification**

Run `npm run dev`, confirm the Refunds page loads, paginates correctly with 11+ seeded requests (page-size boundary), and the new columns/Mark as Sent action render for the right statuses.

- [ ] **Step 4: Commit**

```bash
git add src/pages/booking/RefundRequestsPage.tsx
git commit -m "feat: paginate Refund Requests page and wire Mark as Sent"
```

---

### Task 4: Business Profile — Contact Phone Number

**Files:**
- Modify: `src/interfaces/IBusinessProfile.ts`
- Modify: `src/services/businessProfileService.ts`
- Modify: `src/pages/booking/BusinessProfilePage.tsx`

**Interfaces:**
- Produces: `IBusinessProfile.contactPhoneNumber`, `IBusinessProfileValues.contactPhoneNumber`.

- [ ] **Step 1: Extend the interfaces**

```typescript
import type { BusinessType } from '../types/BusinessType'

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetBusinessProfile.BusinessProfileDto
export interface IBusinessProfile {
  businessName: string
  description: string | null
  businessType: BusinessType
  contactPhoneNumber: string | null
}

export interface IBusinessProfileValues {
  businessName: string
  description: string
  contactPhoneNumber: string
}
```

- [ ] **Step 2: Extend the service**

```typescript
import { authClient } from '../api/clients/authClient'
import type { IBusinessProfile, IBusinessProfileValues } from '../interfaces/IBusinessProfile'
import type { BusinessType } from '../types/BusinessType'

// Raw wire shape from ApexBooking.Core.Application.Features.Tenancy.Queries.GetBusinessProfile.BusinessProfileDto
interface IBusinessProfileWire {
  businessName: string
  description: string | null
  logoUrl: string | null
  businessType: BusinessType
  contactPhoneNumber: string | null
}

function toBusinessProfile(wire: IBusinessProfileWire): IBusinessProfile {
  return {
    businessName: wire.businessName,
    description: wire.description,
    businessType: wire.businessType,
    contactPhoneNumber: wire.contactPhoneNumber,
  }
}

export async function getBusinessProfile(): Promise<IBusinessProfile> {
  const response = await authClient.get<IBusinessProfileWire>('/api/Tenant/profile')
  return toBusinessProfile(response.data)
}

export async function updateBusinessProfile(values: IBusinessProfileValues): Promise<void> {
  await authClient.put('/api/Tenant/profile', {
    businessName: values.businessName,
    description: values.description || null,
    logoUrl: null,
    contactPhoneNumber: values.contactPhoneNumber || null,
  })
}
```

- [ ] **Step 3: Page changes**

`BusinessProfilePage.tsx` — extend `ProfileField`'s backing state and validation:

```tsx
const [values, setValues] = useState<IBusinessProfileValues>({ businessName: '', description: '', contactPhoneNumber: '' })
```

```tsx
  useEffect(() => {
    if (profile) {
      setValues({
        businessName: profile.businessName,
        description: profile.description ?? '',
        contactPhoneNumber: profile.contactPhoneNumber ?? '',
      })
    }
  }, [profile])
```

Add a new `FormGroup` after the Description field, before the read-only Business Type field:

```tsx
            <FormGroup label="Contact Phone Number" htmlFor="contactPhoneNumber">
              <input
                type="tel"
                id="contactPhoneNumber"
                name="contactPhoneNumber"
                className="form-control"
                value={values.contactPhoneNumber}
                onChange={(e) => handleFieldChange('contactPhoneNumber', e.target.value)}
              />
              <div className="form-text">
                Shown to customers on the refund-status page — a business number, not a personal one.
              </div>
            </FormGroup>
```

No validation added beyond what `validate()` already does for other
optional fields — phone formats vary too much to enforce strictly here
(matches the design spec's stated reasoning).

- [ ] **Step 4: Typecheck, lint, build**

Run: `npx tsc -b && npx oxlint src && npx vite build`
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add src/interfaces/IBusinessProfile.ts src/services/businessProfileService.ts src/pages/booking/BusinessProfilePage.tsx
git commit -m "feat: add Contact Phone Number to Business Profile"
```

---

### Task 5: Customer refund-status page

**Files:**
- Create: `src/interfaces/IRefundStatus.ts`
- Create: `src/services/refundStatusService.ts`
- Create: `src/pages/public/RefundStatusPage.tsx`
- Modify: `src/routes/AppRoutes.tsx`

**Interfaces:**
- Consumes: `publicClient` (existing), `PublicBookingLayout` (existing), `RefundRequestStatus` (Phase 1).
- Produces: public route `/:slug/refund-status`.

- [ ] **Step 1: Interface**

```typescript
import type { RefundRequestStatus } from '../types/RefundRequestStatus'

// Mirrors ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus.RefundStatusDto
export interface IRefundStatus {
  bookingReference: string
  status: RefundRequestStatus | null
  amount: number | null
  currencyCode: string
  businessContactPhoneNumber: string | null
  needsEwalletDetails: boolean
}
```

- [ ] **Step 2: Service**

Un-prefixed API path, `publicClient` (no auth) — matches `getCancellableBooking`/`cancelBookingByToken`'s exact existing pattern:

```typescript
import { publicClient } from '../api/clients/publicClient'
import type { IRefundStatus } from '../interfaces/IRefundStatus'

// Un-prefixed by design, same as getCancellableBooking in publicBookingService.ts — the token
// resolves its own tenant, not the URL's slug.

export async function getRefundStatus(token: string): Promise<IRefundStatus> {
  const response = await publicClient.get<IRefundStatus>(`/api/public/refund-status/${token}`)
  return response.data
}

export async function submitRefundEwalletDetails(token: string, provider: string, number: string): Promise<void> {
  await publicClient.post('/api/public/refund-status/ewallet', { token, provider, number })
}
```

- [ ] **Step 3: Page**

Same state-machine shape as `CancelBookingPage.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { PublicBookingLayout } from '../../layouts/PublicBookingLayout'
import { getRefundStatus, submitRefundEwalletDetails } from '../../services/refundStatusService'
import { RefundRequestStatus } from '../../types/RefundRequestStatus'
import type { IRefundStatus } from '../../interfaces/IRefundStatus'

type PageState = 'loading' | 'error' | 'ready' | 'submitting' | 'submitted'

const STATUS_COPY: Partial<Record<RefundRequestStatus, string>> = {
  [RefundRequestStatus.PendingReview]: 'Your refund is awaiting review.',
  [RefundRequestStatus.AwaitingOwnerApproval]: 'Your refund is awaiting final approval.',
  [RefundRequestStatus.Approved]: 'Your refund has been approved and is being processed.',
  [RefundRequestStatus.Processing]: 'Your refund is being processed.',
  [RefundRequestStatus.AwaitingManualTransfer]: 'Your refund has been approved — see below to receive it.',
  [RefundRequestStatus.ManuallyRefunded]: 'Your refund has been sent.',
  [RefundRequestStatus.Succeeded]: 'Your refund has been processed.',
  [RefundRequestStatus.Rejected]: 'This refund request was not approved.',
  [RefundRequestStatus.Failed]: 'There was an issue processing this refund — please contact the business.',
}

export function RefundStatusPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [state, setState] = useState<PageState>('loading')
  const [status, setStatus] = useState<IRefundStatus | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [provider, setProvider] = useState('GCash')
  const [number, setNumber] = useState('')

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

  const handleSubmitEwallet = async () => {
    if (!number.trim()) return
    setState('submitting')
    try {
      await submitRefundEwalletDetails(token, provider, number.trim())
      setState('submitted')
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      setErrorMessage(detail ?? 'Failed to submit your details. Please try again.')
      setState('ready')
    }
  }

  return (
    <PublicBookingLayout currentIndex={0} total={1} stepLabels={[]} showProgress={false}>
      {state === 'loading' && <p className="pb-muted">Loading your refund status…</p>}

      {state === 'error' && (
        <div className="text-center">
          <h1 className="pb-display fs-3 mb-2">This link isn't available</h1>
          <p className="pb-muted">{errorMessage}</p>
        </div>
      )}

      {status && state !== 'loading' && state !== 'error' && (
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

          {status.needsEwalletDetails && state !== 'submitted' && (
            <>
              {errorMessage && (
                <div className="alert alert-danger" role="alert">
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
                  onChange={(e) => setProvider(e.target.value)}
                  disabled={state === 'submitting'}
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
                  onChange={(e) => setNumber(e.target.value)}
                  disabled={state === 'submitting'}
                />
              </div>
              <button
                type="button"
                className="btn btn-primary w-100"
                onClick={handleSubmitEwallet}
                disabled={state === 'submitting' || !number.trim()}
              >
                {state === 'submitting' ? 'Submitting…' : 'Submit'}
              </button>
            </>
          )}

          {state === 'submitted' && (
            <div className="alert alert-success" role="alert">
              Thanks — we'll send your refund to this number shortly.
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

- [ ] **Step 4: Register the route**

`AppRoutes.tsx`, add the import alongside `CancelBookingPage`'s, and the
route alongside `/:slug/cancel-booking`:

```tsx
      <Route path="/:slug/refund-status" element={<RefundStatusPage />} />
```

- [ ] **Step 5: Typecheck, lint, build**

Run: `npx tsc -b && npx oxlint src && npx vite build`
Expected: no errors.

- [ ] **Step 6: Manual verification**

With a real `AwaitingManualTransfer` refund request in the dev database
(and its booking's cancellation token): visit
`/:slug/refund-status?token=...`, confirm the status text and e-wallet form
render, submit a test number, confirm success state. Visit with a booking
that has no refund at all → confirm the "no refund associated" message,
not an error.

- [ ] **Step 7: Commit**

```bash
git add src/interfaces/IRefundStatus.ts src/services/refundStatusService.ts src/pages/public/RefundStatusPage.tsx src/routes/AppRoutes.tsx
git commit -m "feat: add public refund-status page with e-wallet submission"
```

---

## Deliverable Tracking

Once verified, add a "Refund Review — Phase 2" row to
`PROJECT_TRACKER.md`'s Booking Module table.
