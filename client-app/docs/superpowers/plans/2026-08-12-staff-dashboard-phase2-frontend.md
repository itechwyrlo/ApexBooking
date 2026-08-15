# Staff Dashboard Phase 2 — Frontend (Chair Notes) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the Staff Dashboard's two remaining placeholders — "Client Preferences" and "Save Chair Notes" — into working features, per the [design doc](../specs/2026-08-12-staff-dashboard-phase2-chair-notes-design.md).

**Architecture:** "Save Chair Notes" opens a modal listing the staff member's own completed bookings from today (already available in the `bookings` array Phase 1 fetches for the lineup); picking one and submitting calls a new backend endpoint. "Client Preferences" computes the single "active" appointment from that same `bookings` array (checked-in-and-in-progress, else the next upcoming one), and fetches/shows the most recent past note for that customer via a new lightweight query.

**Tech Stack:** React 19 + TypeScript, existing `Modal`/`FormGroup`/`useToast` patterns (see `RejectRefundModal.tsx` / `RefundRequestsPage.tsx` for the established shape).

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- This plan assumes the backend companion plan (`docs/superpowers/plans/2026-08-12-staff-dashboard-phase2-backend.md` in the ApexBooking repo) has landed, including its migration being applied — otherwise `customerId` will be missing from booking rows and the notes endpoints will 404/500.
- Icon `clients` (used below) is already confirmed present in `public/assets/icons/` (used in Foundation's `StaffDashboardPage.tsx` skeleton and Phase 1's plan).

---

### Task 1: Types and service functions

**Files:**
- Modify: `src/interfaces/ITenantBooking.ts`
- Create: `src/interfaces/ICustomerLatestNote.ts`
- Modify: `src/services/bookingService.ts`
- Modify: `src/services/customerService.ts`

**Interfaces:**
- Produces: `ITenantBooking.customerId: string`, `ICustomerLatestNote { notes: string; notedOn: string }`, `setBookingStaffNotes(bookingId, notes): Promise<void>`, `getCustomerLatestNote(customerId): Promise<ICustomerLatestNote | null>`.

- [ ] **Step 1: Add customerId to ITenantBooking**

In `src/interfaces/ITenantBooking.ts`, add to the `ITenantBooking` interface (matches the backend's new `TenantBookingSummary.CustomerId` field from the backend plan):

```ts
export interface ITenantBooking {
  bookingId: string
  customerId: string
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

- [ ] **Step 2: Create ICustomerLatestNote**

```ts
// Mirrors ApexBooking.Core.Application.Dtos.Response.CustomerLatestNoteDto
export interface ICustomerLatestNote {
  notes: string
  notedOn: string
}
```

Save as `src/interfaces/ICustomerLatestNote.ts`.

- [ ] **Step 3: Add setBookingStaffNotes to bookingService**

In `src/services/bookingService.ts`, add after `completeBooking`:

```ts
export async function setBookingStaffNotes(bookingId: string, notes: string): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/staff-notes`, { notes })
}
```

- [ ] **Step 4: Add getCustomerLatestNote to customerService**

In `src/services/customerService.ts`, add the import and function:

```ts
import type { ICustomerLatestNote } from '../interfaces/ICustomerLatestNote'
```

```ts
// Wire shape from ApexBooking.Core.Application.Dtos.Response.CustomerLatestNoteDto matches
// ICustomerLatestNote field-for-field — no mapper needed. 204 (no past note) maps to null.
export async function getCustomerLatestNote(customerId: string): Promise<ICustomerLatestNote | null> {
  const response = await authClient.get<ICustomerLatestNote>(`/api/Tenant/customers/${customerId}/latest-note`)
  return response.status === 204 ? null : response.data
}
```

- [ ] **Step 5: Type-check**

Run: `npm run build`
Expected: no errors from these files (existing consumers of `ITenantBooking`/`getTenantBookings` are unaffected — this is an additive field). (User runs this manually.)

---

### Task 2: useCustomerLatestNote hook

**Files:**
- Create: `src/hooks/useCustomerLatestNote.ts`

**Interfaces:**
- Consumes: `getCustomerLatestNote` from Task 1.
- Produces: `useCustomerLatestNote(customerId: string | null) => { note: ICustomerLatestNote | null; isLoading: boolean }` — consumed by Task 4.

- [ ] **Step 1: Write the hook**

```ts
import { useEffect, useState } from 'react'
import { getCustomerLatestNote } from '../services/customerService'
import type { ICustomerLatestNote } from '../interfaces/ICustomerLatestNote'

interface IUseCustomerLatestNoteResult {
  note: ICustomerLatestNote | null
  isLoading: boolean
}

export function useCustomerLatestNote(customerId: string | null): IUseCustomerLatestNoteResult {
  const [note, setNote] = useState<ICustomerLatestNote | null>(null)
  const [isLoading, setIsLoading] = useState(customerId !== null)

  useEffect(() => {
    if (!customerId) {
      setNote(null)
      setIsLoading(false)
      return
    }

    let isMounted = true
    setIsLoading(true)

    getCustomerLatestNote(customerId)
      .then((result) => {
        if (isMounted) setNote(result)
      })
      .catch(() => {
        if (isMounted) setNote(null)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [customerId])

  return { note, isLoading }
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 3: SaveChairNotesModal component

**Files:**
- Create: `src/components/dashboard/SaveChairNotesModal.tsx`

**Interfaces:**
- Consumes: `Modal`, `FormGroup`, `Button` (all `src/components/common/`), `ITenantBooking`.
- Produces: `SaveChairNotesModal` — props `{ isOpen: boolean; bookings: ITenantBooking[]; isSubmitting: boolean; onClose: () => void; onSubmit: (bookingId: string, notes: string) => void }`. Consumed by Task 4.

- [ ] **Step 1: Write the component**

```tsx
import { useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface ISaveChairNotesModalProps {
  isOpen: boolean
  bookings: ITenantBooking[]
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (bookingId: string, notes: string) => void
}

export function SaveChairNotesModal({ isOpen, bookings, isSubmitting, onClose, onSubmit }: ISaveChairNotesModalProps) {
  const [bookingId, setBookingId] = useState('')
  const [notes, setNotes] = useState('')

  const handleClose = () => {
    setBookingId('')
    setNotes('')
    onClose()
  }

  const handleSubmit = () => {
    if (!bookingId || notes.trim().length === 0) return
    onSubmit(bookingId, notes.trim())
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Save Chair Notes"
      description="Log details for a client you just finished with."
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={!bookingId || notes.trim().length === 0}>
            Save Notes
          </Button>
        </div>
      }
    >
      {bookings.length === 0 ? (
        <p className="text-muted mb-0">You haven&apos;t completed any appointments today yet.</p>
      ) : (
        <>
          <FormGroup label="Appointment" htmlFor="chairNotesBooking" required>
            <select
              id="chairNotesBooking"
              className="form-select"
              value={bookingId}
              onChange={(e) => setBookingId(e.target.value)}
              disabled={isSubmitting}
            >
              <option value="">Select a completed appointment…</option>
              {bookings.map((booking) => (
                <option key={booking.bookingId} value={booking.bookingId}>
                  {booking.customerName} — {booking.serviceName}
                </option>
              ))}
            </select>
          </FormGroup>
          <FormGroup label="Notes" htmlFor="chairNotesText" required>
            <textarea
              id="chairNotesText"
              className="form-control"
              rows={3}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="e.g. Prefers scissors over clippers, sensitive scalp…"
              disabled={isSubmitting}
            />
          </FormGroup>
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

### Task 4: Wire into StaffDashboardPage

**Files:**
- Modify: `src/pages/booking/StaffDashboardPage.tsx`

**Interfaces:**
- Consumes: `useCustomerLatestNote` (Task 2), `SaveChairNotesModal` (Task 3), `setBookingStaffNotes` (Task 1), `useToast` (`src/hooks/useToast.ts`), `formatDisplayDate` (`src/utils/formatDateTime.ts`), `BookingStatus` (`src/types/BookingStatus.ts`).

- [ ] **Step 1: Replace the file contents**

Replace the full contents of `src/pages/booking/StaffDashboardPage.tsx` with:

```tsx
import { useState } from 'react'
import axios from 'axios'
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'
import { StaffLineupTimeline } from '../../components/dashboard/StaffLineupTimeline'
import { SaveChairNotesModal } from '../../components/dashboard/SaveChairNotesModal'
import { useAuth } from '../../hooks/useAuth'
import { useTenantBookings } from '../../hooks/useTenantBookings'
import { useCustomerLatestNote } from '../../hooks/useCustomerLatestNote'
import { useToast } from '../../hooks/useToast'
import { setBookingStaffNotes } from '../../services/bookingService'
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

// The "active" appointment for the Client Preference View: whichever is currently checked in
// and in progress, or — if none is — the next upcoming one today. Neither existing means there's
// nothing to preview right now.
function findActiveBooking(bookings: ITenantBooking[]): ITenantBooking | null {
  const inProgress = bookings.find((b) => b.status === BookingStatus.Scheduled && b.checkedInAt !== null)
  if (inProgress) return inProgress

  const upcoming = bookings
    .filter((b) => b.status === BookingStatus.Scheduled && b.checkedInAt === null)
    .sort((a, b) => a.scheduledStartTime.localeCompare(b.scheduledStartTime))

  return upcoming[0] ?? null
}

export function StaffDashboardPage() {
  const { user } = useAuth()
  const { showToast } = useToast()
  const todayIso = getTodayIsoDate()
  const { bookings, isLoading } = useTenantBookings({
    staffId: user?.tenantMemberId ?? undefined,
    fromDate: todayIso,
    toDate: todayIso,
  })

  const activeBooking = findActiveBooking(bookings)
  const { note: latestNote, isLoading: isNoteLoading } = useCustomerLatestNote(activeBooking?.customerId ?? null)

  const [isChairNotesModalOpen, setIsChairNotesModalOpen] = useState(false)
  const [isSavingNotes, setIsSavingNotes] = useState(false)

  const handleSaveChairNotes = async (bookingId: string, notes: string) => {
    setIsSavingNotes(true)
    try {
      await setBookingStaffNotes(bookingId, notes)
      showToast('success', 'Chair notes saved.')
      setIsChairNotesModalOpen(false)
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to save chair notes. Please try again.')
    } finally {
      setIsSavingNotes(false)
    }
  }

  return (
    <div>
      <PageHeader title="My Day" description={TODAY_LABEL} />

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">My Daily Lineup</h2>
            <StaffLineupTimeline bookings={bookings} isLoading={isLoading} />
          </Card>
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Client Preferences</h2>
            {!activeBooking ? (
              <EmptyState
                icon="clients"
                title="No active appointment right now"
                description="Past service notes for your active client will preview here."
              />
            ) : isNoteLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : latestNote ? (
              <div>
                <p className="fw-semibold mb-1">{activeBooking.customerName}</p>
                <p className="text-muted small mb-1">Last visit: {formatDisplayDate(latestNote.notedOn)}</p>
                <p className="mb-0">{latestNote.notes}</p>
              </div>
            ) : (
              <EmptyState
                icon="clients"
                title={`No notes yet for ${activeBooking.customerName}`}
                description="Chair notes from their next completed visit will preview here."
              />
            )}
          </Card>
        </div>
      </div>

      <div className="row g-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Quick Tools</h2>
            <div className="d-flex flex-wrap gap-2">
              <Button variant="outline-secondary" size="sm" icon="time-offs" disabled>
                Block My Time
              </Button>
              <Button variant="outline-secondary" size="sm" icon="edit" onClick={() => setIsChairNotesModalOpen(true)}>
                Save Chair Notes
              </Button>
            </div>
          </Card>
        </div>
      </div>

      <SaveChairNotesModal
        isOpen={isChairNotesModalOpen}
        bookings={bookings.filter((b) => b.status === BookingStatus.Completed)}
        isSubmitting={isSavingNotes}
        onClose={() => setIsChairNotesModalOpen(false)}
        onSubmit={handleSaveChairNotes}
      />
    </div>
  )
}
```

(Only "Client Preferences" and the "Save Chair Notes" button change from Phase 1's version — "Block My Time" stays a disabled placeholder, deferred to Phase 3. "My Daily Lineup" is untouched from Phase 1.)

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 3: Manual verification**

Run: `npm run dev`. As a Staff test account: complete a booking (marks it `Completed`), click "Save Chair Notes," pick that booking from the dropdown, write a note, save — confirm the toast and that the modal closes. Then, for that same customer's *next* booking (create one via walk-in or public booking), confirm it shows up as the "active" appointment (checked-in, or the next upcoming one) and the note preview appears in "Client Preferences." Confirm a customer with no past notes shows the empty state instead. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: both scope decisions from the design doc (manual button, not auto-popup; single "active" card, not every lineup row) are reflected — `findActiveBooking` implements the exact precedence described (checked-in-in-progress, then next upcoming, then none).
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `SaveChairNotesModal`'s `onSubmit(bookingId, notes)` signature matches `handleSaveChairNotes`'s call in Task 4; `useCustomerLatestNote`'s return shape (`{ note, isLoading }`) matches how Task 4 destructures it.
