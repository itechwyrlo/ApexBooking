# Admin Dashboard Phase 2 — Frontend (Reassign Barber) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the "Reassign Barber" Quick Tools placeholder into a working two-step flow: pick one of today's scheduled bookings, then pick a new (qualified) staff member for it.

**Architecture:** A single self-contained modal (`ReassignBookingModal`) receives today's `Scheduled` bookings as a prop (fetched by the parent page, same as `SaveChairNotesModal`'s convention), but — unlike `SaveChairNotesModal`/`BlockMyTimeModal` — owns its own submission and toast internally, because picking a booking triggers a second, in-modal fetch (the reassignable-staff list for that specific booking) that has no natural home in the parent page. This mirrors `RequestTimeOffModal`'s self-contained shape instead. Backend companion plan: `docs/superpowers/plans/2026-08-13-admin-dashboard-phase2-backend.md` in the ApexBooking repo.

**Tech Stack:** React 19 + TypeScript.

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- This plan assumes the backend companion plan has landed.

---

### Task 1: Types and service functions

**Files:**
- Modify: `src/interfaces/ITenantBooking.ts`
- Create: `src/interfaces/IReassignableStaffMember.ts`
- Modify: `src/services/bookingService.ts`

**Interfaces:**
- Produces: `ITenantBooking.staffId: string`, `IReassignableStaffMember { tenantMemberId, name }`, `getReassignableStaff(bookingId): Promise<IReassignableStaffMember[]>`, `reassignBooking(bookingId, newStaffId): Promise<void>`.

- [ ] **Step 1: Add staffId to ITenantBooking**

In `src/interfaces/ITenantBooking.ts`, add to the `ITenantBooking` interface (matches the backend's new `TenantBookingSummary.StaffId`):

```ts
export interface ITenantBooking {
  bookingId: string
  customerId: string
  staffId: string
  bookingReference: string
  customerName: string
  customerPhone: string | null
  serviceName: string
  staffName: string
  branchName: string
  scheduledDate: string
  scheduledStartTime: string
  durationMinutes: number
  status: BookingStatus
  requiresUpfrontPayment: boolean
  amountDue: number
  currencyCode: string
  paymentConfirmedVia: PaymentConfirmationMethod | null
  checkedInAt: string | null
  serviceCompletedAt: string | null
  cancelledAt: string | null
  cancellationReason: string | null
  noShowAt: string | null
  createdAt: string
}
```

- [ ] **Step 2: Create IReassignableStaffMember**

```ts
// Mirrors ApexBooking.Core.Application.Dtos.Response.ReassignableStaffDto
export interface IReassignableStaffMember {
  tenantMemberId: string
  name: string
}
```

Save as `src/interfaces/IReassignableStaffMember.ts`.

- [ ] **Step 3: Add service functions**

In `src/services/bookingService.ts`, add the import and functions:

```ts
import type { IReassignableStaffMember } from '../interfaces/IReassignableStaffMember'
```

```ts
export async function getReassignableStaff(bookingId: string): Promise<IReassignableStaffMember[]> {
  const response = await authClient.get<IReassignableStaffMember[]>(`/api/Tenant/bookings/${bookingId}/reassignable-staff`)
  return response.data
}

export async function reassignBooking(bookingId: string, newStaffId: string): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/reassign`, { newStaffId })
}
```

- [ ] **Step 4: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 2: useReassignableStaff hook

**Files:**
- Create: `src/hooks/useReassignableStaff.ts`

**Interfaces:**
- Consumes: `getReassignableStaff` from Task 1.
- Produces: `useReassignableStaff(bookingId: string | null) => { staff: IReassignableStaffMember[]; isLoading: boolean }` — consumed by Task 3.

- [ ] **Step 1: Write the hook**

```ts
import { useEffect, useState } from 'react'
import { getReassignableStaff } from '../services/bookingService'
import type { IReassignableStaffMember } from '../interfaces/IReassignableStaffMember'

interface IUseReassignableStaffResult {
  staff: IReassignableStaffMember[]
  isLoading: boolean
}

export function useReassignableStaff(bookingId: string | null): IUseReassignableStaffResult {
  const [staff, setStaff] = useState<IReassignableStaffMember[]>([])
  const [isLoading, setIsLoading] = useState(bookingId !== null)

  useEffect(() => {
    if (!bookingId) {
      setStaff([])
      setIsLoading(false)
      return
    }

    let isMounted = true
    setIsLoading(true)

    getReassignableStaff(bookingId)
      .then((result) => {
        if (isMounted) setStaff(result)
      })
      .catch(() => {
        if (isMounted) setStaff([])
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [bookingId])

  return { staff, isLoading }
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 3: ReassignBookingModal component

**Files:**
- Create: `src/components/dashboard/ReassignBookingModal.tsx`

**Interfaces:**
- Consumes: `Modal`, `FormGroup`, `Button` (`src/components/common/`), `useToast` (`src/hooks/useToast.ts`), `useReassignableStaff` (Task 2), `reassignBooking` (Task 1).
- Produces: `ReassignBookingModal` — props `{ isOpen: boolean; bookings: ITenantBooking[]; onClose: () => void; onReassigned: () => void }`. Consumed by Task 4.

- [ ] **Step 1: Write the component**

```tsx
import { useState } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import { useToast } from '../../hooks/useToast'
import { useReassignableStaff } from '../../hooks/useReassignableStaff'
import { reassignBooking } from '../../services/bookingService'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface IReassignBookingModalProps {
  isOpen: boolean
  bookings: ITenantBooking[]
  onClose: () => void
  onReassigned: () => void
}

export function ReassignBookingModal({ isOpen, bookings, onClose, onReassigned }: IReassignBookingModalProps) {
  const { showToast } = useToast()
  const [bookingId, setBookingId] = useState('')
  const [newStaffId, setNewStaffId] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const { staff: reassignableStaff, isLoading: isStaffLoading } = useReassignableStaff(bookingId || null)

  const handleClose = () => {
    setBookingId('')
    setNewStaffId('')
    onClose()
  }

  const handleBookingChange = (value: string) => {
    setBookingId(value)
    setNewStaffId('')
  }

  const handleSubmit = async () => {
    if (!bookingId || !newStaffId) return
    setIsSubmitting(true)
    try {
      await reassignBooking(bookingId, newStaffId)
      showToast('success', 'Appointment reassigned.')
      onReassigned()
      handleClose()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to reassign this appointment. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Reassign Barber"
      description="Pick a scheduled appointment, then a new staff member."
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={!bookingId || !newStaffId}>
            Reassign
          </Button>
        </div>
      }
    >
      {bookings.length === 0 ? (
        <p className="text-muted mb-0">No scheduled appointments today.</p>
      ) : (
        <>
          <FormGroup label="Appointment" htmlFor="reassignBooking" required>
            <select
              id="reassignBooking"
              className="form-select"
              value={bookingId}
              onChange={(e) => handleBookingChange(e.target.value)}
              disabled={isSubmitting}
            >
              <option value="">Select an appointment…</option>
              {bookings.map((booking) => (
                <option key={booking.bookingId} value={booking.bookingId}>
                  {booking.customerName} — {booking.serviceName} ({booking.staffName})
                </option>
              ))}
            </select>
          </FormGroup>
          {bookingId && (
            <FormGroup label="New Staff Member" htmlFor="reassignStaff" required>
              <select
                id="reassignStaff"
                className="form-select"
                value={newStaffId}
                onChange={(e) => setNewStaffId(e.target.value)}
                disabled={isSubmitting || isStaffLoading}
              >
                <option value="">{isStaffLoading ? 'Loading…' : 'Select a staff member…'}</option>
                {reassignableStaff.map((member) => (
                  <option key={member.tenantMemberId} value={member.tenantMemberId}>
                    {member.name}
                  </option>
                ))}
              </select>
            </FormGroup>
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

### Task 4: Wire into AdminDashboardPage

**Files:**
- Modify: `src/pages/booking/AdminDashboardPage.tsx`

**Interfaces:**
- Consumes: `ReassignBookingModal` (Task 3), `useTenantBookings` (`src/hooks/useTenantBookings.ts`, already exists), `BookingStatus` (`src/types/BookingStatus.ts`).

- [ ] **Step 1: Add the imports and the today's-scheduled-bookings fetch**

Add alongside the existing imports:

```tsx
import { ReassignBookingModal } from '../../components/dashboard/ReassignBookingModal'
import { useTenantBookings } from '../../hooks/useTenantBookings'
import { BookingStatus } from '../../types/BookingStatus'
```

Add alongside the existing `useTenantBookingCounts`/`useIdleStaff` calls:

```tsx
  const { bookings: todaysBookings } = useTenantBookings({ status: BookingStatus.Scheduled, fromDate: todayIso, toDate: todayIso })
```

- [ ] **Step 2: Add modal-open state**

Add alongside `isScanModalOpen`/`isWalkInModalOpen`:

```tsx
  const [isReassignModalOpen, setIsReassignModalOpen] = useState(false)
```

- [ ] **Step 3: Enable the button**

Change the "Reassign Barber" button (currently `disabled`) to:

```tsx
              <Button variant="outline-secondary" size="sm" icon="refresh" onClick={() => setIsReassignModalOpen(true)}>
                Reassign Barber
              </Button>
```

- [ ] **Step 4: Render the modal**

Add alongside `AdmitScanModal`/`NewWalkInModal`:

```tsx
      <ReassignBookingModal
        isOpen={isReassignModalOpen}
        bookings={todaysBookings}
        onClose={() => setIsReassignModalOpen(false)}
        onReassigned={refetchCounts}
      />
```

- [ ] **Step 5: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 6: Manual verification**

Run: `npm run dev`. As Owner/Admin, click "Reassign Barber," pick one of today's scheduled bookings, confirm the second dropdown only shows staff qualified for that booking's service at its branch, reassign it, and confirm success. Reload `AppointmentsPage`/`CalendarPage` and confirm the booking now shows the new staff member's name. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: the two-step "pick a booking, then pick a staff member" flow matches the design doc's Quick-Tools-modal scope decision exactly; the reassignable-staff dropdown is scoped per-booking via `useReassignableStaff`, matching the qualification-only backend scope decision (no availability filtering visible or implied in the UI).
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `ReassignBookingModal`'s props match how Task 4 renders it; `useReassignableStaff`'s return shape matches how the modal destructures it.
