# Admin Dashboard Phase 1 — Frontend (Counters + Idle Staff + Quick Tools) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn three of `AdminDashboardPage.tsx`'s placeholders into working features: Daily Booking Counters, Idle Staff (renamed from "Unassigned Bookings" — see the [design doc](../specs/2026-08-13-admin-dashboard-phase1-counters-idle-staff-design.md)), and the Scan Booking QR / Quick Walk-In Quick Tools.

**Architecture:** Two small new hooks fetch the new backend endpoints from the companion backend plan (`docs/superpowers/plans/2026-08-13-admin-dashboard-phase1-backend.md` in the ApexBooking repo). The two Quick Tools reuse `AdmitScanModal`/`NewWalkInModal` exactly as they already exist (both already used on `AppointmentsPage`, fully self-contained) — no changes to either component.

**Tech Stack:** React 19 + TypeScript.

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- This plan assumes the backend companion plan has landed.
- "Reassign Barber" and "Collect Pay on Visit" buttons stay `disabled` — out of scope for this phase (Phases 2 and 3).

---

### Task 1: Interfaces and service functions

**Files:**
- Create: `src/interfaces/ITenantBookingCounts.ts`
- Create: `src/interfaces/IIdleStaffMember.ts`
- Modify: `src/services/bookingService.ts`
- Modify: `src/services/teamService.ts`

**Interfaces:**
- Produces: `ITenantBookingCounts { pending, checkedIn, completed, missed }`, `IIdleStaffMember { tenantMemberId, name, photoUrl }`, `getTenantBookingCounts(date): Promise<ITenantBookingCounts>`, `getIdleStaff(): Promise<IIdleStaffMember[]>`.

- [ ] **Step 1: Create ITenantBookingCounts**

```ts
// Mirrors ApexBooking.Core.Application.Dtos.Response.TenantBookingCountsDto
export interface ITenantBookingCounts {
  pending: number
  checkedIn: number
  completed: number
  missed: number
}
```

Save as `src/interfaces/ITenantBookingCounts.ts`.

- [ ] **Step 2: Create IIdleStaffMember**

```ts
// Mirrors ApexBooking.Core.Application.Dtos.Response.IdleStaffDto
export interface IIdleStaffMember {
  tenantMemberId: string
  name: string
  photoUrl: string | null
}
```

Save as `src/interfaces/IIdleStaffMember.ts`.

- [ ] **Step 3: Add getTenantBookingCounts to bookingService**

In `src/services/bookingService.ts`, add the import and function:

```ts
import type { ITenantBookingCounts } from '../interfaces/ITenantBookingCounts'
```

```ts
export async function getTenantBookingCounts(date: string): Promise<ITenantBookingCounts> {
  const response = await authClient.get<ITenantBookingCounts>('/api/Tenant/bookings/counts', { params: { date } })
  return response.data
}
```

- [ ] **Step 4: Add getIdleStaff to teamService**

In `src/services/teamService.ts`, add the import and function:

```ts
import type { IIdleStaffMember } from '../interfaces/IIdleStaffMember'
```

```ts
export async function getIdleStaff(): Promise<IIdleStaffMember[]> {
  const response = await authClient.get<IIdleStaffMember[]>('/api/Tenant/team/idle')
  return response.data
}
```

- [ ] **Step 5: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 2: Hooks

**Files:**
- Create: `src/hooks/useTenantBookingCounts.ts`
- Create: `src/hooks/useIdleStaff.ts`

**Interfaces:**
- Consumes: `getTenantBookingCounts`, `getIdleStaff` from Task 1.
- Produces: `useTenantBookingCounts(date: string) => { counts: ITenantBookingCounts | null; isLoading: boolean; refetch: () => void }`, `useIdleStaff() => { staff: IIdleStaffMember[]; isLoading: boolean }` — consumed by Task 3.

- [ ] **Step 1: Write useTenantBookingCounts**

```ts
import { useCallback, useEffect, useState } from 'react'
import { getTenantBookingCounts } from '../services/bookingService'
import type { ITenantBookingCounts } from '../interfaces/ITenantBookingCounts'

interface IUseTenantBookingCountsResult {
  counts: ITenantBookingCounts | null
  isLoading: boolean
  refetch: () => void
}

export function useTenantBookingCounts(date: string): IUseTenantBookingCountsResult {
  const [counts, setCounts] = useState<ITenantBookingCounts | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getTenantBookingCounts(date)
      .then((result) => {
        if (isMounted) setCounts(result)
      })
      .catch(() => {
        if (isMounted) setCounts(null)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [date, refreshToken])

  return { counts, isLoading, refetch }
}
```

- [ ] **Step 2: Write useIdleStaff**

```ts
import { useEffect, useState } from 'react'
import { getIdleStaff } from '../services/teamService'
import type { IIdleStaffMember } from '../interfaces/IIdleStaffMember'

interface IUseIdleStaffResult {
  staff: IIdleStaffMember[]
  isLoading: boolean
}

export function useIdleStaff(): IUseIdleStaffResult {
  const [staff, setStaff] = useState<IIdleStaffMember[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getIdleStaff()
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
  }, [])

  return { staff, isLoading }
}
```

- [ ] **Step 3: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 3: Wire into AdminDashboardPage

**Files:**
- Modify: `src/pages/booking/AdminDashboardPage.tsx`

**Interfaces:**
- Consumes: `useTenantBookingCounts`, `useIdleStaff` (Task 2), `AdmitScanModal` (`src/components/appointments/AdmitScanModal.tsx`), `NewWalkInModal` (`src/components/appointments/NewWalkInModal.tsx`) — both existing, unchanged.

- [ ] **Step 1: Replace the file contents**

Replace the full contents of `src/pages/booking/AdminDashboardPage.tsx` with:

```tsx
import { useState } from 'react'
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'
import { AdmitScanModal } from '../../components/appointments/AdmitScanModal'
import { NewWalkInModal } from '../../components/appointments/NewWalkInModal'
import { useTenantBookingCounts } from '../../hooks/useTenantBookingCounts'
import { useIdleStaff } from '../../hooks/useIdleStaff'

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

export function AdminDashboardPage() {
  const todayIso = getTodayIsoDate()
  const { counts, isLoading: isCountsLoading, refetch: refetchCounts } = useTenantBookingCounts(todayIso)
  const { staff: idleStaff, isLoading: isIdleStaffLoading } = useIdleStaff()

  const [isScanModalOpen, setIsScanModalOpen] = useState(false)
  const [isWalkInModalOpen, setIsWalkInModalOpen] = useState(false)

  return (
    <div>
      <PageHeader title="Front Desk" description={TODAY_LABEL} />

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Master Visual Grid</h2>
            <EmptyState
              icon="dashboard"
              title="No staff schedules to show yet"
              description="A multi-column schedule with a column per active staff member will appear here."
            />
          </Card>
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Daily Booking Counters</h2>
            {isCountsLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : !counts ? (
              <p className="text-muted small mb-0">Failed to load today's counts.</p>
            ) : (
              <div className="row g-2 text-center">
                <div className="col-3">
                  <p className="fs-4 fw-bold mb-0">{counts.pending}</p>
                  <p className="text-muted small mb-0">Pending</p>
                </div>
                <div className="col-3">
                  <p className="fs-4 fw-bold mb-0">{counts.checkedIn}</p>
                  <p className="text-muted small mb-0">Checked-In</p>
                </div>
                <div className="col-3">
                  <p className="fs-4 fw-bold mb-0">{counts.completed}</p>
                  <p className="text-muted small mb-0">Completed</p>
                </div>
                <div className="col-3">
                  <p className="fs-4 fw-bold mb-0">{counts.missed}</p>
                  <p className="text-muted small mb-0">Missed</p>
                </div>
              </div>
            )}
          </Card>
        </div>
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Idle Staff</h2>
            {isIdleStaffLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : idleStaff.length === 0 ? (
              <EmptyState
                icon="alert-triangle"
                title="No idle staff"
                description="Every active team member is assigned to at least one service."
              />
            ) : (
              <ul className="list-unstyled mb-0">
                {idleStaff.map((member) => (
                  <li key={member.tenantMemberId} className="d-flex align-items-center gap-2 py-1">
                    <span className="fw-semibold small">{member.name}</span>
                    <span className="text-muted small">— not assigned to any service</span>
                  </li>
                ))}
              </ul>
            )}
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
              <Button variant="outline-secondary" size="sm" icon="plus" onClick={() => setIsWalkInModalOpen(true)}>
                Quick Walk-In
              </Button>
              <Button variant="outline-secondary" size="sm" icon="refresh" disabled>
                Reassign Barber
              </Button>
              <Button variant="outline-secondary" size="sm" icon="check-circle" disabled>
                Collect Pay on Visit
              </Button>
            </div>
          </Card>
        </div>
      </div>

      <AdmitScanModal isOpen={isScanModalOpen} onClose={() => setIsScanModalOpen(false)} onAdmitted={refetchCounts} />
      <NewWalkInModal
        isOpen={isWalkInModalOpen}
        onClose={() => setIsWalkInModalOpen(false)}
        onScheduled={() => {
          refetchCounts()
          setIsWalkInModalOpen(false)
        }}
      />
    </div>
  )
}
```

(Only "Daily Booking Counters," "Unassigned Bookings" → "Idle Staff," and the first two Quick Tools buttons change from Foundation's skeleton — "Master Visual Grid" and the last two Quick Tools stay as-is, deferred to later phases.)

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 3: Manual verification**

Run: `npm run dev`. As Owner/Admin, confirm the four counter tiles show correct numbers for today. Use "Scan Booking QR" to admit a booking (or "Quick Walk-In" to create one) and confirm the counters update afterward. Confirm a team member with zero assigned services shows up under "Idle Staff," and disappears once you assign them a service via the existing Team page. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: both new widgets and both Quick Tools wirings are covered; "Master Visual Grid," "Reassign Barber," "Collect Pay on Visit" are explicitly left untouched per the design doc's phase boundaries.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `useTenantBookingCounts`/`useIdleStaff`'s return shapes match exactly how Task 3 destructures them; `AdmitScanModal`/`NewWalkInModal` props (`isOpen`, `onClose`, `onAdmitted`/`onScheduled`) match their existing, unmodified definitions.
