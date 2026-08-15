# Admin Dashboard Phase 4 — Frontend (Master Visual Grid) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the last remaining placeholder on `AdminDashboardPage.tsx` — "Master Visual Grid" — with a real multi-column, per-staff view of today's bookings, per the [design doc](../specs/2026-08-13-admin-dashboard-phase4-master-visual-grid-design.md).

**Architecture:** Purely additive on the frontend — no backend changes at all (a first for this rework). A new `useActiveStaff()` hook fetches the active team roster (reusing the existing `getTeamMembers()`). A new `MasterVisualGrid` component renders one column per active staff member, each column reusing `StaffLineupTimeline` (already built in Staff Dashboard Phase 1) as-is, fed that staff member's slice of `AdminDashboardPage.tsx`'s already-fetched `todaysBookings` (already used by Reassign Barber and Collect Pay on Visit).

**Tech Stack:** React 19 + TypeScript.

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- No backend companion plan for this phase — everything needed already exists.

---

### Task 1: useActiveStaff hook

**Files:**
- Create: `src/hooks/useActiveStaff.ts`

**Interfaces:**
- Consumes: `getTeamMembers` (`src/services/teamService.ts`, already exists: `getTeamMembers(params: IPageParams = {}): Promise<IPagedResult<ITeamMember>>`).
- Produces: `useActiveStaff() => { staff: ITeamMember[]; isLoading: boolean }` — consumed by Task 3.

- [ ] **Step 1: Write the hook**

```ts
import { useEffect, useState } from 'react'
import { getTeamMembers } from '../services/teamService'
import { TenantMemberStatus } from '../types/TenantMemberStatus'
import type { ITeamMember } from '../interfaces/ITeamMember'

interface IUseActiveStaffResult {
  staff: ITeamMember[]
  isLoading: boolean
}

export function useActiveStaff(): IUseActiveStaffResult {
  const [staff, setStaff] = useState<ITeamMember[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getTeamMembers({ pageSize: 1000 })
      .then((result) => {
        if (!isMounted) return
        const activeMembers = result.data
          .filter((member) => member.status === TenantMemberStatus.Active)
          .sort((a, b) => a.firstName.localeCompare(b.firstName))
        setStaff(activeMembers)
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

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 2: MasterVisualGrid component

**Files:**
- Create: `src/components/dashboard/MasterVisualGrid.tsx`

**Interfaces:**
- Consumes: `StaffLineupTimeline` (`src/components/dashboard/StaffLineupTimeline.tsx`, unchanged), `EmptyState` (`src/components/common/EmptyState.tsx`).
- Produces: `MasterVisualGrid` — props `{ staff: ITeamMember[]; bookings: ITenantBooking[]; isLoading: boolean }`. Consumed by Task 3.

- [ ] **Step 1: Write the component**

```tsx
import { EmptyState } from '../common/EmptyState'
import { StaffLineupTimeline } from './StaffLineupTimeline'
import type { ITeamMember } from '../../interfaces/ITeamMember'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface IMasterVisualGridProps {
  staff: ITeamMember[]
  bookings: ITenantBooking[]
  isLoading: boolean
}

export function MasterVisualGrid({ staff, bookings, isLoading }: IMasterVisualGridProps) {
  if (!isLoading && staff.length === 0) {
    return (
      <EmptyState
        icon="dashboard"
        title="No active staff yet"
        description="Add team members and mark them active to see their schedules here."
      />
    )
  }

  return (
    <div className="d-flex gap-3 overflow-auto pb-2">
      {staff.map((member) => (
        <div key={member.id} style={{ minWidth: 220, flexShrink: 0 }}>
          <div className="d-flex align-items-center gap-2 mb-2">
            {member.photoUrl && <img src={member.photoUrl} width={28} height={28} className="rounded-circle" alt="" />}
            <span className="fw-semibold small">{member.fullName}</span>
          </div>
          <StaffLineupTimeline bookings={bookings.filter((b) => b.staffId === member.id)} isLoading={isLoading} />
        </div>
      ))}
    </div>
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
- Consumes: `useActiveStaff` (Task 1), `MasterVisualGrid` (Task 2). Reuses the page's existing `todaysBookings` (from `useTenantBookings`, already fetched for Reassign Barber/Collect Pay on Visit).

- [ ] **Step 1: Add the imports and the hook call**

Add alongside the existing imports:

```tsx
import { MasterVisualGrid } from '../../components/dashboard/MasterVisualGrid'
import { useActiveStaff } from '../../hooks/useActiveStaff'
```

Add alongside the existing `useTenantBookingCounts`/`useIdleStaff`/`useTenantBookings` calls:

```tsx
  const { staff: activeStaff, isLoading: isActiveStaffLoading } = useActiveStaff()
```

- [ ] **Step 2: Replace the placeholder**

Replace the "Master Visual Grid" card's contents (currently an `EmptyState`):

```tsx
      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Master Visual Grid</h2>
            <MasterVisualGrid staff={activeStaff} bookings={todaysBookings} isLoading={isActiveStaffLoading} />
          </Card>
        </div>
      </div>
```

(The `EmptyState` import in this file may become unused if nothing else in the page uses it — check before removing; as of Phase 3 it's still used by the "Idle Staff" card, so it stays.)

- [ ] **Step 3: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 4: Manual verification**

Run: `npm run dev`. As Owner/Admin, confirm the grid shows one column per active staff member (scroll horizontally if there are many), each column listing that staff member's actual bookings for today in chronological order, matching what `AppointmentsPage` shows when filtered to that staff member. Confirm a staff member with no bookings today still gets a (empty) column, and a deactivated team member gets none. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: simple-columns layout and read-only cards match both scope decisions in the design doc exactly; zero backend changes, as the design doc states.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `MasterVisualGrid`'s props match how Task 3 renders it; `StaffLineupTimeline`'s existing props (`bookings`, `isLoading`) are reused unchanged.
