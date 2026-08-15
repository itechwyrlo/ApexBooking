# Staff Dashboard Phase 3 — Frontend (Block My Time) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the Staff Dashboard's last remaining placeholder — "Block My Time" — into a working feature, per the [design doc](../specs/2026-08-12-staff-dashboard-phase3-block-my-time-design.md).

**Architecture:** A lighter-weight sibling of `RequestTimeOffModal` — just start time / end time (reusing the existing `TimeSelect` component) and an optional reason, always for today (no date picker, no Full/Partial-day choice). Submitting calls the new backend endpoint from the companion backend plan (`docs/superpowers/plans/2026-08-13-staff-dashboard-phase3-backend.md` in the ApexBooking repo), which lands the block as already-`Approved`.

**Tech Stack:** React 19 + TypeScript, existing `Modal`/`FormGroup`/`TimeSelect` components.

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- This plan assumes the backend companion plan has landed (`POST /api/Tenant/team/time-off/block` exists).
- Follows the same "modal takes raw values via `onSubmit`, parent owns the async call" shape already used by `SaveChairNotesModal` in Phase 2 (not `RequestTimeOffModal`'s self-contained-submission shape) — for consistency with the rest of this Staff Dashboard work.

---

### Task 1: blockMyTime service function

**Files:**
- Modify: `src/services/timeOffService.ts`

**Interfaces:**
- Produces: `blockMyTime(date: string, startTime: string, endTime: string, reason: string): Promise<string>` — consumed by Task 3.

- [ ] **Step 1: Add the function**

Add after `requestTimeOff`:

```ts
export async function blockMyTime(date: string, startTime: string, endTime: string, reason: string): Promise<string> {
  const response = await authClient.post<{ id: string }>('/api/Tenant/team/time-off/block', {
    date,
    startTime: `${startTime}:00`,
    endTime: `${endTime}:00`,
    reason: reason || null,
  })
  return response.data.id
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 2: BlockMyTimeModal component

**Files:**
- Create: `src/components/dashboard/BlockMyTimeModal.tsx`

**Interfaces:**
- Consumes: `Modal`, `FormGroup`, `TimeSelect`, `Button` (all `src/components/common/`), `isRequired` (`src/utils/validators.ts`).
- Produces: `BlockMyTimeModal` — props `{ isOpen: boolean; date: string; isSubmitting: boolean; onClose: () => void; onSubmit: (startTime: string, endTime: string, reason: string) => void }`. Consumed by Task 3.

- [ ] **Step 1: Write the component**

```tsx
import { useEffect, useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { TimeSelect } from '../common/TimeSelect'
import { Button } from '../common/Button'
import { isRequired } from '../../utils/validators'
import { formatDisplayDate } from '../../utils/formatDateTime'

interface IBlockMyTimeModalProps {
  isOpen: boolean
  date: string
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (startTime: string, endTime: string, reason: string) => void
}

export function BlockMyTimeModal({ isOpen, date, isSubmitting, onClose, onSubmit }: IBlockMyTimeModalProps) {
  const [startTime, setStartTime] = useState('')
  const [endTime, setEndTime] = useState('')
  const [reason, setReason] = useState('')
  const [touched, setTouched] = useState(false)

  useEffect(() => {
    if (isOpen) {
      setStartTime('')
      setEndTime('')
      setReason('')
      setTouched(false)
    }
  }, [isOpen])

  const startTimeError = touched && !isRequired(startTime) ? 'Start time is required.' : undefined
  const endTimeError = touched
    ? !isRequired(endTime)
      ? 'End time is required.'
      : startTime && endTime && startTime >= endTime
        ? 'End time must be after the start time.'
        : undefined
    : undefined

  const handleClose = () => {
    onClose()
  }

  const handleSubmit = () => {
    setTouched(true)
    if (!isRequired(startTime) || !isRequired(endTime) || startTime >= endTime) return
    onSubmit(startTime, endTime, reason.trim())
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Block My Time"
      description={`Mark part of today, ${formatDisplayDate(date)}, as unavailable.`}
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting}>
            Block Time
          </Button>
        </div>
      }
    >
      <div className="row">
        <div className="col-sm-6">
          <FormGroup label="Start Time" htmlFor="blockStartTime" required error={startTimeError}>
            <TimeSelect id="blockStartTime" isInvalid={!!startTimeError} value={startTime} onChange={setStartTime} disabled={isSubmitting} />
          </FormGroup>
        </div>
        <div className="col-sm-6">
          <FormGroup label="End Time" htmlFor="blockEndTime" required error={endTimeError}>
            <TimeSelect id="blockEndTime" isInvalid={!!endTimeError} value={endTime} onChange={setEndTime} disabled={isSubmitting} />
          </FormGroup>
        </div>
      </div>
      <FormGroup label="Reason" htmlFor="blockReason">
        <textarea
          id="blockReason"
          rows={2}
          className="form-control"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          disabled={isSubmitting}
          placeholder="e.g. Lunch break"
        />
      </FormGroup>
    </Modal>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors attributable to this new file. (User runs this manually.)

---

### Task 3: Wire into StaffDashboardPage

**Files:**
- Modify: `src/pages/booking/StaffDashboardPage.tsx`

**Interfaces:**
- Consumes: `BlockMyTimeModal` (Task 2), `blockMyTime` (Task 1).

- [ ] **Step 1: Add the imports**

Add alongside the existing imports:

```tsx
import { BlockMyTimeModal } from '../../components/dashboard/BlockMyTimeModal'
import { blockMyTime } from '../../services/timeOffService'
```

- [ ] **Step 2: Add state and the submit handler**

Add alongside the existing `isChairNotesModalOpen`/`isSavingNotes` state:

```tsx
  const [isBlockTimeModalOpen, setIsBlockTimeModalOpen] = useState(false)
  const [isBlockingTime, setIsBlockingTime] = useState(false)

  const handleBlockMyTime = async (startTime: string, endTime: string, reason: string) => {
    setIsBlockingTime(true)
    try {
      await blockMyTime(todayIso, startTime, endTime, reason)
      showToast('success', 'Your time has been blocked.')
      setIsBlockTimeModalOpen(false)
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to block your time. Please try again.')
    } finally {
      setIsBlockingTime(false)
    }
  }
```

- [ ] **Step 3: Enable the button and render the modal**

Change the "Block My Time" button (currently `disabled`) to:

```tsx
              <Button variant="outline-secondary" size="sm" icon="time-offs" onClick={() => setIsBlockTimeModalOpen(true)}>
                Block My Time
              </Button>
```

Add the modal render alongside `SaveChairNotesModal`, at the bottom of the component:

```tsx
      <BlockMyTimeModal
        isOpen={isBlockTimeModalOpen}
        date={todayIso}
        isSubmitting={isBlockingTime}
        onClose={() => setIsBlockTimeModalOpen(false)}
        onSubmit={handleBlockMyTime}
      />
```

- [ ] **Step 4: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 5: Manual verification**

Run: `npm run dev`. As a Staff test account, click "Block My Time," pick a start/end time later today, submit — confirm the success toast. Then confirm that window shows as unavailable for that staff member in the public booking wizard's availability check (or the walk-in staff-availability picker), and confirm it appears as an already-`Approved` entry on the Time Offs page without needing separate approval. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: no date picker, no Full/Partial-day choice — matches the design doc's "always today" scope decision exactly.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `BlockMyTimeModal`'s `onSubmit(startTime, endTime, reason)` signature matches `handleBlockMyTime`'s parameters in Task 3, and `blockMyTime`'s parameter order (`date, startTime, endTime, reason`) matches how Task 3 calls it.
