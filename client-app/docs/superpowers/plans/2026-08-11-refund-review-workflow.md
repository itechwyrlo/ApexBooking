# Refund Review Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the frontend for the refund review workflow — a Payment
Settings toggle + deadline field, the Refund Requests page (list,
Confirm/Reject, Owner approval gate), and a due-soon reminder banner —
against the `ApexBooking` backend's already-implemented Phase 1 API.

**Architecture:** Same shape as every existing list+action admin page in
this app (`FailedNotificationsPage`: hook → service → table, page-level
state for modals). No test framework exists in this repo (`package.json`
has no vitest/jest/testing-library) — verification here follows this
codebase's actual established convention: `tsc -b` (typecheck), `oxlint`
(lint), `vite build`, and a manual dev-server pass, matching every
completed feature row in `PROJECT_TRACKER.md`.

**Tech Stack:** React 19, TypeScript, react-router-dom 7, axios, Bootstrap 5 utility classes, no new dependencies.

## Global Constraints

- No new npm dependencies — reuse `Modal`/`FormGroup`/`Badge`/`RowActions`/`EmptyState`/`TableSkeleton`/`NumberInput`/`PageHeader`/`Card`/`Button` from `src/components/common/`.
- Wire shapes are camelCase (ASP.NET Core default `System.Text.Json` naming policy); enums arrive as strings (global `JsonStringEnumConverter`).
- `owner-approve`/`owner-deny` are Owner-only server-side (`[Authorize(Roles = "Owner")]`) — the UI must hide those actions from Admins, not just disable them, since Admins can still see the row.
- This plan requires Task 0 (a small backend addition in the separate `ApexBooking` repo) to be done and deployed before Tasks 3+ are usable end-to-end — Tasks 1–2 don't depend on it.

---

### Task 0: Backend prerequisite — `RefundReviewDeadlineDays` + `DueDate` (ApexBooking repo, not LocalFlow)

**Files (in `c:\Users\Wyrlo\projects\ApexBooking`):**
- Modify: `ApexBooking.Core.Domain/Entities/PaymentPolicy.cs`
- Modify: `ApexBooking.Core.Persistence/Mappings/PaymentPolicyConfiguration.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/GetPendingRefundRequestsQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/GetPendingRefundRequestsHandler.cs`

**Interfaces:**
- Produces: `PaymentPolicy.RefundReviewDeadlineDays` (int, public get), `RefundRequestSummaryDto.DueDate` (DateTime). Consumed by Task 1 (`IPaymentPolicy`) and Task 2 (`IRefundRequest`) on the LocalFlow side.

- [ ] **Step 1: Add the property to `PaymentPolicy`**

In `PaymentPolicy.cs`, next to `AutomaticRefund`:

```csharp
    public bool AutomaticRefund { get; private set; }
    // How many days staff have to act on a PendingReview RefundRequest before it's flagged
    // overdue in the UI. Purely a reminder threshold — nothing server-side auto-acts when it
    // passes.
    public int RefundReviewDeadlineDays { get; private set; }
```

In the constructor, after `AutomaticRefund = false;`:

```csharp
        AutomaticRefund = false;
        RefundReviewDeadlineDays = 7;
```

Update `UpdatePolicy`'s signature and body:

```csharp
    public void UpdatePolicy(
        PaymentRequirementType requirementType,
        DepositType depositType,
        decimal depositValue,
        decimal refundPercent,
        bool automaticRefund,
        int refundReviewDeadlineDays)
    {
        if (requirementType == PaymentRequirementType.None)
        {
            depositValue = 0m;
        }
        else
        {
            if (depositValue < 0)
                throw new BusinessRuleBrokenException("Deposit value cannot be a negative amount.");

            if (depositType == DepositType.Percentage && depositValue > 100)
                throw new BusinessRuleBrokenException("A percentage-based deposit requirement cannot exceed 100%.");
        }

        if (refundPercent < 0 || refundPercent > 100)
            throw new BusinessRuleBrokenException("Refund allowance parameters must sit strictly between 0% and 100%.");

        if (refundReviewDeadlineDays < 1)
            throw new BusinessRuleBrokenException("Refund review deadline must be at least 1 day.");

        RequirementType = requirementType;
        DepositType = depositType;
        DepositValue = depositValue;
        RefundPercent = refundPercent;
        AutomaticRefund = automaticRefund;
        RefundReviewDeadlineDays = refundReviewDeadlineDays;
        UpdatedAt = DateTime.UtcNow;
    }
```

- [ ] **Step 2: Map the column**

In `PaymentPolicyConfiguration.cs`, after the `automatic_refund` mapping:

```csharp
            builder.Property(p => p.AutomaticRefund)
                .HasColumnName("automatic_refund")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(p => p.RefundReviewDeadlineDays)
                .HasColumnName("refund_review_deadline_days")
                .HasDefaultValue(7)
                .IsRequired();
```

- [ ] **Step 3: Wire the command/handler/query/DTO**

`UpdatePaymentPolicyCommand.cs`:

```csharp
    public record UpdatePaymentPolicyCommand(
        PaymentRequirementType RequirementType,
        DepositType DepositType,
        decimal DepositValue,
        decimal RefundPercent,
        bool AutomaticRefund,
        int RefundReviewDeadlineDays
    ) : ICommand;
```

`UpdatePaymentPolicyHandler.cs`:

```csharp
            tenant.PaymentPolicy.UpdatePolicy(
                command.RequirementType,
                command.DepositType,
                command.DepositValue,
                command.RefundPercent,
                command.AutomaticRefund,
                command.RefundReviewDeadlineDays
            );
```

`GetPaymentPolicyQuery.cs`:

```csharp
    public record PaymentPolicyDto(
        PaymentRequirementType RequirementType,
        DepositType DepositType,
        decimal DepositValue,
        decimal RefundPercent,
        bool AutomaticRefund,
        int RefundReviewDeadlineDays
    );
```

`GetPaymentPolicyHandler.cs`:

```csharp
            return new PaymentPolicyDto(
                policy.RequirementType,
                policy.DepositType,
                policy.DepositValue,
                policy.RefundPercent,
                policy.AutomaticRefund,
                policy.RefundReviewDeadlineDays
            );
```

- [ ] **Step 4: Add `DueDate` to the refund-request list DTO**

`GetPendingRefundRequestsQuery.cs`:

```csharp
    public record RefundRequestSummaryDto(
        Guid Id,
        Guid BookingId,
        string BookingReference,
        string CustomerName,
        decimal RequestedAmount,
        string CurrencyCode,
        bool IsAutoRefundEligible,
        RefundRequestStatus Status,
        string? RejectionReason,
        DateTime CreatedAt,
        DateTime DueDate
    );
```

In `GetPendingRefundRequestsHandler.cs`, the handler already loads `tenant` (for `Bookings`) — add `t.PaymentPolicy!` to its `includes`, then compute the due date per row:

```csharp
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Bookings, t => t.PaymentPolicy!]);

            var deadlineDays = tenant?.PaymentPolicy?.RefundReviewDeadlineDays ?? 7;
```

and in the loop building `result.Add(...)`, append the new argument:

```csharp
                result.Add(new RefundRequestSummaryDto(
                    request.Id,
                    request.BookingId,
                    booking?.BookingReference ?? "(unknown)",
                    customer?.Contact.Name ?? "(unknown)",
                    request.RequestedAmount,
                    request.CurrencyCode,
                    request.IsAutoRefundEligible,
                    request.Status,
                    request.RejectionReason,
                    request.CreatedAt,
                    request.CreatedAt.AddDays(deadlineDays)));
```

- [ ] **Step 5: Generate the migration and verify**

Run: `dotnet build ApexBooking.sln` (0 errors expected), then
`dotnet ef migrations add AddRefundReviewDeadlineDays --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`
and `dotnet ef database update --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`.

- [ ] **Step 6: Commit** (in the `ApexBooking` repo)

```bash
git add ApexBooking.Core.Domain/Entities/PaymentPolicy.cs ApexBooking.Core.Persistence/Mappings/PaymentPolicyConfiguration.cs ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyCommand.cs ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyHandler.cs ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyQuery.cs ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyHandler.cs ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/ ApexBooking.Core.Persistence/Migrations/
git commit -m "feat: add RefundReviewDeadlineDays setting and RefundRequestSummaryDto.DueDate"
```

---

### Task 1: `IPaymentPolicy` + `PaymentSettingsPage` — the two new fields

**Files:**
- Modify: `src/interfaces/IPaymentPolicy.ts`
- Modify: `src/pages/booking/settings/PaymentSettingsPage.tsx`

**Interfaces:**
- Produces: `IPaymentPolicy.automaticRefund: boolean`, `IPaymentPolicy.refundReviewDeadlineDays: number`.

- [ ] **Step 1: Extend the interface**

```typescript
// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentPolicy.PaymentPolicyDto
export interface IPaymentPolicy {
  requirementType: PaymentRequirementType
  depositType: DepositType
  depositValue: number
  refundPercent: number
  automaticRefund: boolean
  refundReviewDeadlineDays: number
}
```

- [ ] **Step 2: Update the form's default values and validation**

In `PaymentSettingsPage.tsx`:

```typescript
const DEFAULT_VALUES: IPaymentPolicy = {
  requirementType: PaymentRequirementType.None,
  depositType: DepositType.Percentage,
  depositValue: 0,
  refundPercent: 0,
  automaticRefund: false,
  refundReviewDeadlineDays: 7,
}

interface IFormErrors {
  depositValue?: string
  refundPercent?: string
  refundReviewDeadlineDays?: string
}

function validate(values: IPaymentPolicy): IFormErrors {
  const errors: IFormErrors = {}

  if (values.requirementType !== PaymentRequirementType.None) {
    if (values.depositValue < 0) {
      errors.depositValue = 'Deposit value cannot be a negative amount.'
    } else if (values.depositType === DepositType.Percentage && values.depositValue > 100) {
      errors.depositValue = 'A percentage-based deposit requirement cannot exceed 100%.'
    }
  }

  if (values.refundPercent < 0 || values.refundPercent > 100) {
    errors.refundPercent = 'Refund allowance must be between 0% and 100%.'
  }

  if (values.refundReviewDeadlineDays < 1) {
    errors.refundReviewDeadlineDays = 'Refund review deadline must be at least 1 day.'
  }

  return errors
}
```

- [ ] **Step 3: Add the two form fields**

Insert after the "Refund Allowance (%)" `FormGroup` block, before the submit `<div>`:

```tsx
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

          <FormGroup label="Automatic Refund" htmlFor="automaticRefund">
            <div className="form-check form-switch">
              <input
                type="checkbox"
                role="switch"
                id="automaticRefund"
                className="form-check-input"
                checked={values.automaticRefund}
                onChange={(e) => handleChange({ ...values, automaticRefund: e.target.checked })}
              />
              <label className="form-check-label" htmlFor="automaticRefund">
                {values.automaticRefund ? 'On — refunds process automatically' : 'Off — refunds require staff review'}
              </label>
            </div>
            <div className="form-text">
              When off (default), a cancelled booking's refund waits in the Refunds page for staff to confirm or reject it.
            </div>
          </FormGroup>
```

- [ ] **Step 4: Typecheck and lint**

Run: `cd C:\Users\Wyrlo\projects\LocalFlow && npx tsc -b`
Expected: no errors (confirms `IPaymentPolicy`'s two new required fields are supplied everywhere it's constructed — `DEFAULT_VALUES` above, and anywhere else the type is used).

Run: `npx oxlint src/interfaces/IPaymentPolicy.ts src/pages/booking/settings/PaymentSettingsPage.tsx`
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add src/interfaces/IPaymentPolicy.ts src/pages/booking/settings/PaymentSettingsPage.tsx
git commit -m "feat: add automatic refund toggle and review deadline to Payment Settings"
```

---

### Task 2: `RefundRequestStatus` type + `IRefundRequest` interface

**Files:**
- Create: `src/types/RefundRequestStatus.ts`
- Create: `src/interfaces/IRefundRequest.ts`

**Interfaces:**
- Produces: `RefundRequestStatus` (const object + derived union type, mirrors `TenantMemberStatus.ts`), `IRefundRequest`. Consumed by Tasks 3–8.

- [ ] **Step 1: `RefundRequestStatus.ts`**

```typescript
export const RefundRequestStatus = {
  PendingReview: 'PendingReview',
  AwaitingOwnerApproval: 'AwaitingOwnerApproval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Processing: 'Processing',
  AwaitingManualTransfer: 'AwaitingManualTransfer',
  ManuallyRefunded: 'ManuallyRefunded',
  Succeeded: 'Succeeded',
  Failed: 'Failed',
} as const

export type RefundRequestStatus = (typeof RefundRequestStatus)[keyof typeof RefundRequestStatus]
```

- [ ] **Step 2: `IRefundRequest.ts`**

```typescript
import type { RefundRequestStatus } from '../types/RefundRequestStatus'

// Mirrors ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests.RefundRequestSummaryDto
export interface IRefundRequest {
  id: string
  bookingId: string
  bookingReference: string
  customerName: string
  requestedAmount: number
  currencyCode: string
  isAutoRefundEligible: boolean
  status: RefundRequestStatus
  rejectionReason: string | null
  createdAt: string
  dueDate: string
}
```

- [ ] **Step 3: Typecheck**

Run: `npx tsc -b`
Expected: no errors (both are new, unreferenced files — this just confirms they parse).

- [ ] **Step 4: Commit**

```bash
git add src/types/RefundRequestStatus.ts src/interfaces/IRefundRequest.ts
git commit -m "feat: add RefundRequestStatus and IRefundRequest"
```

---

### Task 3: `refundRequestService.ts`

**Files:**
- Create: `src/services/refundRequestService.ts`

**Interfaces:**
- Consumes: `IRefundRequest` (Task 2), `authClient` (existing).
- Produces: `getRefundRequests()`, `confirmRefundRequest(id)`, `rejectRefundRequest(id, reason)`, `approveOwnerGate(id)`, `denyOwnerGate(id)`. Consumed by Tasks 4, 5, 6.

- [ ] **Step 1: Write the service**

```typescript
import { authClient } from '../api/clients/authClient'
import type { IRefundRequest } from '../interfaces/IRefundRequest'

// Wire shape from ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests.RefundRequestSummaryDto
// matches IRefundRequest field-for-field (camelCase JSON naming policy), so no mapper is needed.

export async function getRefundRequests(): Promise<IRefundRequest[]> {
  const response = await authClient.get<IRefundRequest[]>('/api/refund-requests')
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
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b && npx oxlint src/services/refundRequestService.ts`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add src/services/refundRequestService.ts
git commit -m "feat: add refundRequestService"
```

---

### Task 4: `useRefundRequests` hook

**Files:**
- Create: `src/hooks/useRefundRequests.ts`

**Interfaces:**
- Consumes: `getRefundRequests` (Task 3).
- Produces: `useRefundRequests(): { requests: IRefundRequest[], isLoading: boolean, error: string | null, refetch: () => void }`. Consumed by Task 7 (page) and Task 8 (reminder hook).

- [ ] **Step 1: Write the hook**

```typescript
import { useCallback, useEffect, useState } from 'react'
import { getRefundRequests } from '../services/refundRequestService'
import type { IRefundRequest } from '../interfaces/IRefundRequest'

interface IUseRefundRequestsResult {
  requests: IRefundRequest[]
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useRefundRequests(): IUseRefundRequestsResult {
  const [requests, setRequests] = useState<IRefundRequest[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getRefundRequests()
      .then((result) => {
        if (isMounted) setRequests(result)
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
  }, [refreshToken])

  return { requests, isLoading, error, refetch }
}
```

- [ ] **Step 2: Typecheck**

Run: `npx tsc -b`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add src/hooks/useRefundRequests.ts
git commit -m "feat: add useRefundRequests hook"
```

---

### Task 5: `RefundRequestTable`

**Files:**
- Create: `src/components/refunds/RefundRequestTable.tsx`

**Interfaces:**
- Consumes: `IRefundRequest`, `RefundRequestStatus` (Task 2), `Badge`/`RowActions`/`EmptyState`/`TableSkeleton` (existing), `formatRelativeTime` (existing), `Role` (existing).
- Produces: `<RefundRequestTable requests isLoading currentUserRole busyId onConfirm onReject onApprove onDeny />`. Consumed by Task 7.

- [ ] **Step 1: Write the component**

```tsx
import { Badge } from '../common/Badge'
import { RowActions } from '../common/RowActions'
import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { formatRelativeTime } from '../../utils/formatDateTime'
import { RefundRequestStatus } from '../../types/RefundRequestStatus'
import { Role } from '../../types/Role'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

const COLUMNS = ['Booking', 'Customer', 'Amount', 'Method', 'Status', 'Due', 'Actions']

const STATUS_TONE: Record<RefundRequestStatus, 'neutral' | 'primary' | 'success' | 'warning' | 'danger' | 'teal'> = {
  [RefundRequestStatus.PendingReview]: 'warning',
  [RefundRequestStatus.AwaitingOwnerApproval]: 'warning',
  [RefundRequestStatus.Approved]: 'success',
  [RefundRequestStatus.Rejected]: 'danger',
  [RefundRequestStatus.Processing]: 'primary',
  [RefundRequestStatus.AwaitingManualTransfer]: 'warning',
  [RefundRequestStatus.ManuallyRefunded]: 'success',
  [RefundRequestStatus.Succeeded]: 'success',
  [RefundRequestStatus.Failed]: 'danger',
}

function formatStatus(status: RefundRequestStatus): string {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function isDueSoon(dueDate: string): boolean {
  const diffMs = new Date(dueDate).getTime() - Date.now()
  return diffMs <= 2 * 24 * 60 * 60 * 1000
}

interface IRefundRequestTableProps {
  requests: IRefundRequest[]
  isLoading?: boolean
  currentUserRole: Role
  busyId: string | null
  onConfirm: (request: IRefundRequest) => void
  onReject: (request: IRefundRequest) => void
  onApprove: (request: IRefundRequest) => void
  onDeny: (request: IRefundRequest) => void
}

export function RefundRequestTable({
  requests,
  isLoading,
  currentUserRole,
  busyId,
  onConfirm,
  onReject,
  onApprove,
  onDeny,
}: IRefundRequestTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (requests.length === 0) {
    return <EmptyState icon="check-circle" title="No refunds need review right now." description="Cancelled bookings eligible for a refund will show up here." />
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

            let actions: { label: string; icon: string; tone: 'primary' | 'delete'; onClick: () => void }[] = []
            if (request.status === RefundRequestStatus.PendingReview) {
              actions = [
                { label: 'Confirm', icon: 'check-circle', tone: 'primary', onClick: () => onConfirm(request) },
                { label: 'Reject', icon: 'x-circle', tone: 'delete', onClick: () => onReject(request) },
              ]
            } else if (request.status === RefundRequestStatus.AwaitingOwnerApproval && currentUserRole === Role.Owner) {
              actions = [
                { label: 'Approve', icon: 'check-circle', tone: 'primary', onClick: () => onApprove(request) },
                { label: 'Deny', icon: 'x-circle', tone: 'delete', onClick: () => onDeny(request) },
              ]
            }

            return (
              <tr key={request.id}>
                <td className="fw-semibold" data-label="Booking">{request.bookingReference}</td>
                <td data-label="Customer">{request.customerName}</td>
                <td data-label="Amount">{request.requestedAmount.toFixed(2)} {request.currencyCode}</td>
                <td data-label="Method">
                  <Badge tone={request.isAutoRefundEligible ? 'teal' : 'neutral'}>{request.isAutoRefundEligible ? 'Auto' : 'Manual'}</Badge>
                </td>
                <td data-label="Status">
                  <Badge tone={STATUS_TONE[request.status]}>{formatStatus(request.status)}</Badge>
                </td>
                <td data-label="Due">
                  <span className={request.status === RefundRequestStatus.PendingReview && isDueSoon(request.dueDate) ? 'text-danger fw-semibold' : 'text-muted'}>
                    {formatRelativeTime(request.dueDate)}
                  </span>
                </td>
                <td data-label="Actions">
                  <RowActions
                    actions={actions.map((action) => ({
                      ...action,
                      disabled: isBusy,
                      isLoading: isBusy,
                    }))}
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

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b && npx oxlint src/components/refunds/RefundRequestTable.tsx`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add src/components/refunds/RefundRequestTable.tsx
git commit -m "feat: add RefundRequestTable"
```

---

### Task 6: `RejectRefundModal`

**Files:**
- Create: `src/components/refunds/RejectRefundModal.tsx`

**Interfaces:**
- Consumes: `Modal`, `FormGroup` (existing).
- Produces: `<RejectRefundModal isOpen bookingReference onClose onSubmit={(reason) => void} isSubmitting />`. Consumed by Task 7.

- [ ] **Step 1: Write the component**

```tsx
import { useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'

interface IRejectRefundModalProps {
  isOpen: boolean
  bookingReference: string
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (reason: string) => void
}

export function RejectRefundModal({ isOpen, bookingReference, isSubmitting, onClose, onSubmit }: IRejectRefundModalProps) {
  const [reason, setReason] = useState('')

  const handleClose = () => {
    setReason('')
    onClose()
  }

  const handleSubmit = () => {
    if (reason.trim().length === 0) return
    onSubmit(reason.trim())
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Reject Refund"
      description={`Booking ${bookingReference}`}
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={reason.trim().length === 0}>
            Reject Refund
          </Button>
        </div>
      }
    >
      <FormGroup label="Reason" htmlFor="rejectReason" required>
        <textarea
          id="rejectReason"
          className="form-control"
          rows={3}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Why is this refund being rejected?"
        />
      </FormGroup>
    </Modal>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b && npx oxlint src/components/refunds/RejectRefundModal.tsx`
Expected: no errors. (Confirmed `Button.tsx` has no `form` prop — this component uses `Modal`'s own `footer` slot with a direct `onClick` handler instead of native form submission, not a guess.)

- [ ] **Step 3: Commit**

```bash
git add src/components/refunds/RejectRefundModal.tsx
git commit -m "feat: add RejectRefundModal"
```

---

### Task 7: `RefundRequestsPage` + nav + routing

**Files:**
- Create: `src/pages/booking/RefundRequestsPage.tsx`
- Create: `public/assets/icons/refund.svg`
- Modify: `src/config/navigation/booking.nav.config.ts`
- Modify: `src/routes/AppRoutes.tsx`

**Interfaces:**
- Consumes: `useRefundRequests` (Task 4), `RefundRequestTable` (Task 5), `RejectRefundModal` (Task 6), `confirmRefundRequest`/`rejectRefundRequest`/`approveOwnerGate`/`denyOwnerGate` (Task 3), `useAuth`, `useToast` (existing).

- [ ] **Step 1: New icon**

`public/assets/icons/refund.svg` (undo-arrow glyph, matching this repo's existing hand-authored stroke style):

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#475569" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
  <path d="M4 12a8 8 0 1 1 2.34 5.66"/>
  <path d="M4 17v-5h5"/>
</svg>
```

- [ ] **Step 2: Nav entry**

In `booking.nav.config.ts`, add after the `Team` entry (before `Services`):

```typescript
  {
    label: 'Refunds',
    href: 'refunds',
    icon: '/assets/icons/refund.svg',
    roles: [Role.Owner, Role.Admin],
    section: 'manage',
  },
```

- [ ] **Step 3: Write the page**

```tsx
import { useState } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { RefundRequestTable } from '../../components/refunds/RefundRequestTable'
import { RejectRefundModal } from '../../components/refunds/RejectRefundModal'
import { useRefundRequests } from '../../hooks/useRefundRequests'
import { useAuth } from '../../hooks/useAuth'
import { useToast } from '../../hooks/useToast'
import { confirmRefundRequest, rejectRefundRequest, approveOwnerGate, denyOwnerGate } from '../../services/refundRequestService'
import { Role } from '../../types/Role'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

export function RefundRequestsPage() {
  const { requests, isLoading, refetch } = useRefundRequests()
  const { user } = useAuth()
  const { showToast } = useToast()
  const [busyId, setBusyId] = useState<string | null>(null)
  const [rejectTarget, setRejectTarget] = useState<IRefundRequest | null>(null)
  const [isRejecting, setIsRejecting] = useState(false)

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
        />
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
    </div>
  )
}
```

- [ ] **Step 4: Register the route**

In `AppRoutes.tsx`, add the import alongside the other booking page imports, and add the route after `staff`:

```tsx
        <Route
          path="refunds"
          element={
            <ProtectedRoute allowedRoles={[Role.Owner, Role.Admin]}>
              <RefundRequestsPage />
            </ProtectedRoute>
          }
        />
```

- [ ] **Step 5: Typecheck, lint, build**

Run: `npx tsc -b && npx oxlint src && npx vite build`
Expected: no errors.

- [ ] **Step 6: Manual verification**

Run: `npm run dev`, log in as a Tenant Owner, navigate to Refunds via the sidebar, confirm the empty state renders (no backend data yet unless Task 0 + a real cancelled booking exist). Log in as Staff and confirm `/app/booking/refunds` is not reachable (redirects, matching `ProtectedRoute`'s existing `allowedRoles` behavior on `staff`/`clients`).

- [ ] **Step 7: Commit**

```bash
git add src/pages/booking/RefundRequestsPage.tsx public/assets/icons/refund.svg src/config/navigation/booking.nav.config.ts src/routes/AppRoutes.tsx
git commit -m "feat: add Refund Requests page, nav entry, and route"
```

---

### Task 8: Due-soon reminder banner

**Files:**
- Create: `src/hooks/useRefundReviewReminder.ts`
- Create: `src/components/refunds/RefundReviewReminderBanner.tsx`
- Modify: `src/layouts/DashboardLayout.tsx`

**Interfaces:**
- Consumes: `useRefundRequests` (Task 4), `RefundRequestStatus` (Task 2), `useAuth`, `Role` (existing).

- [ ] **Step 1: Write the hook**

```typescript
import { useMemo } from 'react'
import { useRefundRequests } from './useRefundRequests'
import { RefundRequestStatus } from '../types/RefundRequestStatus'

const WARNING_WINDOW_MS = 2 * 24 * 60 * 60 * 1000

interface IUseRefundReviewReminderResult {
  dueSoonCount: number
  isLoading: boolean
}

export function useRefundReviewReminder(): IUseRefundReviewReminderResult {
  const { requests, isLoading } = useRefundRequests()

  const dueSoonCount = useMemo(() => {
    const now = Date.now()
    return requests.filter(
      (request) => request.status === RefundRequestStatus.PendingReview && new Date(request.dueDate).getTime() - now <= WARNING_WINDOW_MS,
    ).length
  }, [requests])

  return { dueSoonCount, isLoading }
}
```

- [ ] **Step 2: Write the banner**

```tsx
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Icon } from '../common/Icon'
import { buildDashboardPath } from '../../config/dashboardRoutes'

interface IRefundReviewReminderBannerProps {
  count: number
}

export function RefundReviewReminderBanner({ count }: IRefundReviewReminderBannerProps) {
  const [isDismissed, setIsDismissed] = useState(false)
  const navigate = useNavigate()
  const { slug } = useParams<{ slug: string }>()

  if (count === 0 || isDismissed) return null

  return (
    <div className="alert d-flex align-items-center justify-content-between gap-3 mb-3" style={{ backgroundColor: 'var(--color-warning-subtle, #fff3cd)' }} role="alert">
      <div className="d-flex align-items-center gap-2">
        <Icon name="alert-triangle" size={18} />
        <span>
          {count} refund request{count === 1 ? '' : 's'} {count === 1 ? 'needs' : 'need'} your attention before its deadline.
        </span>
      </div>
      <div className="d-flex align-items-center gap-2">
        <button type="button" className="btn btn-warning btn-sm" onClick={() => navigate(buildDashboardPath(slug ?? '', 'refunds'))}>
          Review Now
        </button>
        <button type="button" className="btn-close" aria-label="Dismiss" onClick={() => setIsDismissed(true)} />
      </div>
    </div>
  )
}
```

Uses the existing `buildDashboardPath(slug, subPath)` helper from `src/config/dashboardRoutes.ts` (confirmed — the same one `SidebarNavItem.tsx`/`Topbar.tsx`/`SettingsLayout.tsx` already use), not a hand-built path.

- [ ] **Step 3: Mount in `DashboardLayout`**

In `DashboardLayout.tsx`, add the import and gate by role (`useAuth`), rendering just above `<Outlet />`:

```tsx
import { useAuth } from '../hooks/useAuth'
import { useRefundReviewReminder } from '../hooks/useRefundReviewReminder'
import { RefundReviewReminderBanner } from '../components/refunds/RefundReviewReminderBanner'
import { Role } from '../types/Role'
```

```tsx
export function DashboardLayout() {
  const { user } = useAuth()
  const canReviewRefunds = user !== null && (user.roles.includes(Role.Owner) || user.roles.includes(Role.Admin))
  const { dueSoonCount } = useRefundReviewReminder()
  // ...existing state/effects unchanged...
```

```tsx
        <main className="flex-grow-1 p-3 p-md-4">
          {canReviewRefunds && <RefundReviewReminderBanner count={dueSoonCount} />}
          <Outlet />
        </main>
```

Note: this calls `useRefundReviewReminder` (and therefore fetches `/api/refund-requests`) unconditionally on every dashboard mount, even for Staff who can't act on it — the hook call itself is cheap and unconditional (React hook rules), but consider gating the *fetch* behind `canReviewRefunds` if this turns out to be an unwanted extra request for Staff sessions. Not done in this pass to keep the hook simple; flag if it matters after manual testing.

- [ ] **Step 4: Typecheck, lint, build**

Run: `npx tsc -b && npx oxlint src && npx vite build`
Expected: no errors.

- [ ] **Step 5: Manual verification**

With at least one `PendingReview` refund request whose `dueDate` is within 2 days (or overdue) in the dev database: log in as Owner or Admin, confirm the banner appears on any dashboard page (not just Refunds), "Review Now" navigates to `/refunds`, dismiss (×) hides it, and a fresh page reload brings it back. Log in as Staff and confirm the banner never renders.

- [ ] **Step 6: Commit**

```bash
git add src/hooks/useRefundReviewReminder.ts src/components/refunds/RefundReviewReminderBanner.tsx src/layouts/DashboardLayout.tsx
git commit -m "feat: add refund review deadline reminder banner"
```

---

## Deliverable Tracking

Once all tasks are done and verified, add a "Refund Review" row to
`PROJECT_TRACKER.md`'s Booking Module table, matching the existing format
(Feature | Status | Notes), per this repo's established convention.
