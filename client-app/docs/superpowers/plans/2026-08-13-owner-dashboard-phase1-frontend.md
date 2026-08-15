# Owner Dashboard Phase 1 — Frontend (Refund Log, Personal Lineup, Quick Tools) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn four of `OwnerDashboardPage.tsx`'s placeholders into working features: Refund Log, My Personal Lineup, Scan Booking QR, and Cancel & Refund. ("Online Payout Status" has already been removed from the page entirely, per an explicit decision — not part of this plan.)

**Architecture:** Refund Log is genuinely new (new query, new list rendering). My Personal Lineup is an exact reuse of `StaffLineupTimeline` + `useTenantBookings({ staffId: user.tenantMemberId, ... })`, identical to the Staff Dashboard. Scan Booking QR reuses `AdmitScanModal` unchanged, identical to Admin Phase 1. Cancel & Refund is a small new picker (pick a booking) in front of the already-existing `CancelBookingModal` (which already triggers the full refund evaluation automatically). Backend companion plan: `docs/superpowers/plans/2026-08-13-owner-dashboard-phase1-backend.md` in the ApexBooking repo (Refund Log only — the other three pieces need no backend work).

**Tech Stack:** React 19 + TypeScript.

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- This plan assumes the backend companion plan has landed (for the Refund Log query only).
- `CancelBookingModal` (`src/components/appointments/CancelBookingModal.tsx`) is consumed exactly as it already exists — its prop shape is `{ booking: ITenantBooking | null; onClose: () => void; onCancelled: () => void }` (opens when `booking` is non-null), unlike this session's other new modals which take an `isOpen` boolean — do not change it.

---

### Task 1: Types and service function

**Files:**
- Create: `src/interfaces/IRefundLogEntry.ts`
- Modify: `src/services/refundRequestService.ts`

**Interfaces:**
- Produces: `IRefundLogEntry { id, bookingReference, amount, currencyCode, paymentMethodType, status, processedAt }`, `getRefundLog(limit?): Promise<IRefundLogEntry[]>`.

- [ ] **Step 1: Create IRefundLogEntry**

```ts
import type { RefundRequestStatus } from '../types/RefundRequestStatus'

// Mirrors ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog.RefundLogEntryDto
export interface IRefundLogEntry {
  id: string
  bookingReference: string
  amount: number
  currencyCode: string
  paymentMethodType: string | null
  status: RefundRequestStatus
  processedAt: string
}
```

Save as `src/interfaces/IRefundLogEntry.ts`.

- [ ] **Step 2: Add getRefundLog to refundRequestService**

In `src/services/refundRequestService.ts`, add the import and function:

```ts
import type { IRefundLogEntry } from '../interfaces/IRefundLogEntry'
```

```ts
export async function getRefundLog(limit = 20): Promise<IRefundLogEntry[]> {
  const response = await authClient.get<IRefundLogEntry[]>('/api/refund-requests/log', { params: { limit } })
  return response.data
}
```

- [ ] **Step 3: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 2: useRefundLog hook

**Files:**
- Create: `src/hooks/useRefundLog.ts`

**Interfaces:**
- Consumes: `getRefundLog` from Task 1.
- Produces: `useRefundLog() => { entries: IRefundLogEntry[]; isLoading: boolean }` — consumed by Task 4.

- [ ] **Step 1: Write the hook**

```ts
import { useEffect, useState } from 'react'
import { getRefundLog } from '../services/refundRequestService'
import type { IRefundLogEntry } from '../interfaces/IRefundLogEntry'

interface IUseRefundLogResult {
  entries: IRefundLogEntry[]
  isLoading: boolean
}

export function useRefundLog(): IUseRefundLogResult {
  const [entries, setEntries] = useState<IRefundLogEntry[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getRefundLog()
      .then((result) => {
        if (isMounted) setEntries(result)
      })
      .catch(() => {
        if (isMounted) setEntries([])
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [])

  return { entries, isLoading }
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 3: CancelRefundPickerModal component

**Files:**
- Create: `src/components/dashboard/CancelRefundPickerModal.tsx`

**Interfaces:**
- Consumes: `Modal`, `FormGroup`, `Button` (`src/components/common/`), `ITenantBooking`.
- Produces: `CancelRefundPickerModal` — props `{ isOpen: boolean; bookings: ITenantBooking[]; onClose: () => void; onPicked: (booking: ITenantBooking) => void }`. Consumed by Task 4, which then feeds the picked booking straight into the existing `CancelBookingModal`.

- [ ] **Step 1: Write the component**

```tsx
import { useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface ICancelRefundPickerModalProps {
  isOpen: boolean
  bookings: ITenantBooking[]
  onClose: () => void
  onPicked: (booking: ITenantBooking) => void
}

export function CancelRefundPickerModal({ isOpen, bookings, onClose, onPicked }: ICancelRefundPickerModalProps) {
  const [bookingId, setBookingId] = useState('')

  const handleClose = () => {
    setBookingId('')
    onClose()
  }

  const handleContinue = () => {
    const booking = bookings.find((b) => b.bookingId === bookingId)
    if (!booking) return
    setBookingId('')
    onPicked(booking)
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Cancel & Refund"
      description="Pick a scheduled appointment to cancel."
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose}>
            Close
          </Button>
          <Button onClick={handleContinue} disabled={!bookingId}>
            Continue
          </Button>
        </div>
      }
    >
      {bookings.length === 0 ? (
        <p className="text-muted mb-0">No scheduled appointments today.</p>
      ) : (
        <FormGroup label="Appointment" htmlFor="cancelRefundBooking" required>
          <select
            id="cancelRefundBooking"
            className="form-select"
            value={bookingId}
            onChange={(e) => setBookingId(e.target.value)}
          >
            <option value="">Select an appointment…</option>
            {bookings.map((booking) => (
              <option key={booking.bookingId} value={booking.bookingId}>
                {booking.customerName} — {booking.serviceName}
              </option>
            ))}
          </select>
        </FormGroup>
      )}
    </Modal>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors attributable to this new file. (User runs this manually.)

---

### Task 4: Wire into OwnerDashboardPage

**Files:**
- Modify: `src/pages/booking/OwnerDashboardPage.tsx`

**Interfaces:**
- Consumes: `useRefundLog` (Task 2), `CancelRefundPickerModal` (Task 3), `StaffLineupTimeline` (`src/components/dashboard/StaffLineupTimeline.tsx`, unchanged), `AdmitScanModal`/`CancelBookingModal` (`src/components/appointments/`, unchanged), `useAuth`, `useTenantBookings`, `formatDisplayDate` (`src/utils/formatDateTime.ts`).

- [ ] **Step 1: Replace the file contents**

Replace the full contents of `src/pages/booking/OwnerDashboardPage.tsx` with:

```tsx
import { useState } from 'react'
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'
import { StaffLineupTimeline } from '../../components/dashboard/StaffLineupTimeline'
import { CancelRefundPickerModal } from '../../components/dashboard/CancelRefundPickerModal'
import { AdmitScanModal } from '../../components/appointments/AdmitScanModal'
import { CancelBookingModal } from '../../components/appointments/CancelBookingModal'
import { useAuth } from '../../hooks/useAuth'
import { useTenantBookings } from '../../hooks/useTenantBookings'
import { useRefundLog } from '../../hooks/useRefundLog'
import { formatDisplayDate } from '../../utils/formatDateTime'
import { BookingStatus } from '../../types/BookingStatus'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

const TODAY_LABEL = new Date().toLocaleDateString(undefined, {
  weekday: 'long',
  month: 'long',
  day: 'numeric',
})

function getTodayIsoDate(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

export function OwnerDashboardPage() {
  const { user } = useAuth()
  const todayIso = getTodayIsoDate()

  const { bookings: myBookings, isLoading: isMyBookingsLoading } = useTenantBookings({
    staffId: user?.tenantMemberId ?? undefined,
    fromDate: todayIso,
    toDate: todayIso,
  })
  const { bookings: todaysBookings } = useTenantBookings({ status: BookingStatus.Scheduled, fromDate: todayIso, toDate: todayIso })
  const { entries: refundLog, isLoading: isRefundLogLoading } = useRefundLog()

  const [isScanModalOpen, setIsScanModalOpen] = useState(false)
  const [isCancelPickerOpen, setIsCancelPickerOpen] = useState(false)
  const [cancelTarget, setCancelTarget] = useState<ITenantBooking | null>(null)

  return (
    <div>
      <PageHeader title="Business Overview" description={TODAY_LABEL} />

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Total Shop Revenue</h2>
            <EmptyState
              icon="chart"
              title="No revenue yet today"
              description="Gross earnings from online and pay-on-visit bookings will total here."
            />
          </Card>
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Refund Log</h2>
            {isRefundLogLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : refundLog.length === 0 ? (
              <EmptyState
                icon="refund"
                title="No refunds yet"
                description="Processed refunds will list here with amount, date, and original payment method."
              />
            ) : (
              <ul className="list-unstyled mb-0">
                {refundLog.map((entry) => (
                  <li key={entry.id} className="d-flex justify-content-between align-items-center py-1">
                    <div>
                      <span className="fw-semibold small">{entry.bookingReference}</span>
                      <span className="text-muted small ms-2">{entry.paymentMethodType ?? 'Manual transfer'}</span>
                    </div>
                    <div className="text-end">
                      <div className="small fw-semibold">
                        {entry.amount.toFixed(2)} {entry.currencyCode}
                      </div>
                      <div className="text-muted small">{formatDisplayDate(entry.processedAt)}</div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Staff Performance</h2>
            <EmptyState
              icon="staff"
              title="No performance data yet"
              description="Your team, ranked by services completed and revenue generated, will appear here."
            />
          </Card>
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">My Personal Lineup</h2>
            <StaffLineupTimeline bookings={myBookings} isLoading={isMyBookingsLoading} />
          </Card>
        </div>
      </div>

      <div className="row g-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Quick Tools</h2>
            <div className="d-flex flex-wrap gap-2">
              <Button variant="outline-secondary" size="sm" icon="qr-code" onClick={() => setIsScanModalOpen(true)}>
                Scan Booking QR
              </Button>
              <Button variant="outline-secondary" size="sm" icon="x-circle" onClick={() => setIsCancelPickerOpen(true)}>
                Cancel &amp; Refund
              </Button>
            </div>
          </Card>
        </div>
      </div>

      <AdmitScanModal isOpen={isScanModalOpen} onClose={() => setIsScanModalOpen(false)} onAdmitted={() => {}} />

      <CancelRefundPickerModal
        isOpen={isCancelPickerOpen}
        bookings={todaysBookings}
        onClose={() => setIsCancelPickerOpen(false)}
        onPicked={(booking) => {
          setIsCancelPickerOpen(false)
          setCancelTarget(booking)
        }}
      />

      <CancelBookingModal
        booking={cancelTarget}
        onClose={() => setCancelTarget(null)}
        onCancelled={() => setCancelTarget(null)}
      />
    </div>
  )
}
```

(Only "Refund Log," "My Personal Lineup," and the two Quick Tools buttons change from the current skeleton. "Total Shop Revenue" and "Staff Performance" stay as placeholders, deferred to Phases 2 and 3. The `AdmitScanModal`'s `onAdmitted` callback is a no-op here — unlike the Admin Dashboard, this page has no booking counters to refetch.)

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 3: Manual verification**

Run: `npm run dev`. As Owner: process a refund to completion via the existing Refunds page, confirm it appears in the new Refund Log with correct amount/date/payment method. Confirm "My Personal Lineup" shows real bookings if any are assigned to the Owner's own account today, or the empty state otherwise. Use "Scan Booking QR" and confirm it works. Use "Cancel & Refund," pick a scheduled booking, confirm the existing cancel-confirmation modal opens with that booking and completes the cancellation (triggering the same refund evaluation already verified earlier this session). (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: all four in-scope widgets (Refund Log, My Personal Lineup, Scan Booking QR, Cancel & Refund) are covered; Total Shop Revenue and Staff Performance remain explicit placeholders per the design doc's phase boundaries; Online Payout Status is absent entirely (already removed).
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `CancelRefundPickerModal`'s `onPicked(booking)` feeds directly into `CancelBookingModal`'s existing `booking` prop with no adapter needed — both operate on `ITenantBooking`.
