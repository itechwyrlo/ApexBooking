# Admin Dashboard Phase 3 — Frontend (Collect Pay on Visit) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the "Collect Pay on Visit" Quick Tools placeholder into a working single-step flow: pick one of today's scheduled, not-yet-paid bookings, confirm.

**Architecture:** No new DTO fields needed — `ITenantBooking.paymentConfirmedVia`/`.status` already exist. `CollectPaymentModal` follows the `SaveChairNotesModal`/`BlockMyTimeModal` convention (parent owns the bookings list and the async submission via an `onSubmit` callback), simpler than `ReassignBookingModal` since there's no secondary in-modal fetch here. Backend companion plan: `docs/superpowers/plans/2026-08-13-admin-dashboard-phase3-backend.md` in the ApexBooking repo.

**Tech Stack:** React 19 + TypeScript.

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- This plan assumes the backend companion plan has landed.

---

### Task 1: collectPayment service function

**Files:**
- Modify: `src/services/bookingService.ts`

**Interfaces:**
- Produces: `collectPayment(bookingId: string): Promise<void>` — consumed by Task 3.

- [ ] **Step 1: Add the function**

Add after `reassignBooking`:

```ts
export async function collectPayment(bookingId: string): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/collect-payment`)
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 2: CollectPaymentModal component

**Files:**
- Create: `src/components/dashboard/CollectPaymentModal.tsx`

**Interfaces:**
- Consumes: `Modal`, `FormGroup`, `Button` (`src/components/common/`), `ITenantBooking`.
- Produces: `CollectPaymentModal` — props `{ isOpen: boolean; bookings: ITenantBooking[]; isSubmitting: boolean; onClose: () => void; onSubmit: (bookingId: string) => void }`. Consumed by Task 3.

- [ ] **Step 1: Write the component**

```tsx
import { useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface ICollectPaymentModalProps {
  isOpen: boolean
  bookings: ITenantBooking[]
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (bookingId: string) => void
}

export function CollectPaymentModal({ isOpen, bookings, isSubmitting, onClose, onSubmit }: ICollectPaymentModalProps) {
  const [bookingId, setBookingId] = useState('')

  const selectedBooking = bookings.find((b) => b.bookingId === bookingId) ?? null

  const handleClose = () => {
    setBookingId('')
    onClose()
  }

  const handleSubmit = () => {
    if (!bookingId) return
    onSubmit(bookingId)
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Collect Pay on Visit"
      description="Record a cash or card payment collected in person."
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={!bookingId}>
            Confirm Payment Collected
          </Button>
        </div>
      }
    >
      {bookings.length === 0 ? (
        <p className="text-muted mb-0">No unpaid scheduled appointments today.</p>
      ) : (
        <>
          <FormGroup label="Appointment" htmlFor="collectPaymentBooking" required>
            <select
              id="collectPaymentBooking"
              className="form-select"
              value={bookingId}
              onChange={(e) => setBookingId(e.target.value)}
              disabled={isSubmitting}
            >
              <option value="">Select an appointment…</option>
              {bookings.map((booking) => (
                <option key={booking.bookingId} value={booking.bookingId}>
                  {booking.customerName} — {booking.serviceName}
                </option>
              ))}
            </select>
          </FormGroup>
          {selectedBooking && (
            <p className="fw-semibold mb-0">
              Amount due: {selectedBooking.amountDue.toFixed(2)} {selectedBooking.currencyCode}
            </p>
          )}
        </>
      )}
    </Modal>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors attributable to this new file. (User runs this manually.)

---

### Task 3: Wire into AdminDashboardPage

**Files:**
- Modify: `src/pages/booking/AdminDashboardPage.tsx`

**Interfaces:**
- Consumes: `CollectPaymentModal` (Task 2), `collectPayment` (Task 1), `useToast` (`src/hooks/useToast.ts`).

- [ ] **Step 1: Add the imports**

Add alongside the existing imports:

```tsx
import axios from 'axios'
import { CollectPaymentModal } from '../../components/dashboard/CollectPaymentModal'
import { useToast } from '../../hooks/useToast'
import { collectPayment } from '../../services/bookingService'
```

- [ ] **Step 2: Add state and the submit handler**

Add alongside the existing `useTenantBookingCounts`/`useIdleStaff`/`useTenantBookings` calls:

```tsx
  const { showToast } = useToast()
```

Add alongside `isReassignModalOpen`:

```tsx
  const [isCollectPaymentModalOpen, setIsCollectPaymentModalOpen] = useState(false)
  const [isCollectingPayment, setIsCollectingPayment] = useState(false)

  const handleCollectPayment = async (bookingId: string) => {
    setIsCollectingPayment(true)
    try {
      await collectPayment(bookingId)
      showToast('success', 'Payment recorded.')
      refetchCounts()
      setIsCollectPaymentModalOpen(false)
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to record payment. Please try again.')
    } finally {
      setIsCollectingPayment(false)
    }
  }
```

- [ ] **Step 3: Enable the button**

Change the "Collect Pay on Visit" button (currently `disabled`) to:

```tsx
              <Button variant="outline-secondary" size="sm" icon="check-circle" onClick={() => setIsCollectPaymentModalOpen(true)}>
                Collect Pay on Visit
              </Button>
```

- [ ] **Step 4: Render the modal**

Add alongside `ReassignBookingModal`:

```tsx
      <CollectPaymentModal
        isOpen={isCollectPaymentModalOpen}
        bookings={todaysBookings.filter((b) => b.paymentConfirmedVia === null)}
        isSubmitting={isCollectingPayment}
        onClose={() => setIsCollectPaymentModalOpen(false)}
        onSubmit={handleCollectPayment}
      />
```

- [ ] **Step 5: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 6: Manual verification**

Run: `npm run dev`. As Owner/Admin, click "Collect Pay on Visit," confirm only scheduled bookings with no payment recorded appear, pick one, confirm, and verify the toast. Reload `AppointmentsPage` and confirm the booking's payment summary badge now shows Pay-in-Visit. Confirm a booking already paid online doesn't appear in the list. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: the single-step "pick a booking, confirm" flow matches the design doc; eligibility filtering (`status === Scheduled && paymentConfirmedVia === null`) matches the backend mutator's own guards exactly, so a rejected submission (race condition — someone else already recorded payment) is the only realistic failure mode, handled by the existing toast-on-error path.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `CollectPaymentModal`'s `onSubmit(bookingId)` matches `handleCollectPayment`'s signature in Task 3.
