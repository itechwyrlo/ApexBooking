# Owner Dashboard Phase 3 — Frontend (Staff Performance List) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the last placeholder in the entire role-based dashboards rework — "Staff Performance" on `OwnerDashboardPage.tsx` — into a real ranked list.

**Architecture:** A new `useStaffPerformance(date)` hook fetches the new backend endpoint (companion plan: `docs/superpowers/plans/2026-08-14-owner-dashboard-phase3-backend.md` in the ApexBooking repo). Same `getTodayIsoDate()` local helper already used on this page.

**Tech Stack:** React 19 + TypeScript.

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- This plan assumes the backend companion plan has landed.
- This is the last plan in the entire role-based dashboards rework — once this lands, `OwnerDashboardPage.tsx`, `AdminDashboardPage.tsx`, and `StaffDashboardPage.tsx` are all fully built out with no remaining placeholders.

---

### Task 1: Types and service function

**Files:**
- Create: `src/interfaces/IStaffPerformanceEntry.ts`
- Modify: `src/services/teamService.ts`

**Interfaces:**
- Produces: `IStaffPerformanceEntry { tenantMemberId, name, servicesCompleted, revenueGenerated, currencyCode }`, `getStaffPerformance(date): Promise<IStaffPerformanceEntry[]>`.

- [ ] **Step 1: Create IStaffPerformanceEntry**

```ts
// Mirrors ApexBooking.Core.Application.Dtos.Response.StaffPerformanceEntryDto
export interface IStaffPerformanceEntry {
  tenantMemberId: string
  name: string
  servicesCompleted: number
  revenueGenerated: number
  currencyCode: string
}
```

Save as `src/interfaces/IStaffPerformanceEntry.ts`.

- [ ] **Step 2: Add getStaffPerformance to teamService**

In `src/services/teamService.ts`, add the import and function:

```ts
import type { IStaffPerformanceEntry } from '../interfaces/IStaffPerformanceEntry'
```

```ts
export async function getStaffPerformance(date: string): Promise<IStaffPerformanceEntry[]> {
  const response = await authClient.get<IStaffPerformanceEntry[]>('/api/Tenant/team/performance', { params: { date } })
  return response.data
}
```

- [ ] **Step 3: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 2: useStaffPerformance hook

**Files:**
- Create: `src/hooks/useStaffPerformance.ts`

**Interfaces:**
- Consumes: `getStaffPerformance` from Task 1.
- Produces: `useStaffPerformance(date: string) => { entries: IStaffPerformanceEntry[]; isLoading: boolean }` — consumed by Task 3.

- [ ] **Step 1: Write the hook**

```ts
import { useEffect, useState } from 'react'
import { getStaffPerformance } from '../services/teamService'
import type { IStaffPerformanceEntry } from '../interfaces/IStaffPerformanceEntry'

interface IUseStaffPerformanceResult {
  entries: IStaffPerformanceEntry[]
  isLoading: boolean
}

export function useStaffPerformance(date: string): IUseStaffPerformanceResult {
  const [entries, setEntries] = useState<IStaffPerformanceEntry[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getStaffPerformance(date)
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
  }, [date])

  return { entries, isLoading }
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 3: Wire into OwnerDashboardPage

**Files:**
- Modify: `src/pages/booking/OwnerDashboardPage.tsx`

**Interfaces:**
- Consumes: `useStaffPerformance` (Task 2).

- [ ] **Step 1: Add the import and hook call**

Add alongside the existing imports:

```tsx
import { useStaffPerformance } from '../../hooks/useStaffPerformance'
```

Add alongside the existing `useRefundLog`/`useTenantRevenue` calls:

```tsx
  const { entries: staffPerformance, isLoading: isStaffPerformanceLoading } = useStaffPerformance(todayIso)
```

- [ ] **Step 2: Replace the placeholder**

Replace the "Staff Performance" card's contents (currently an `EmptyState`):

```tsx
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Staff Performance</h2>
            {isStaffPerformanceLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : staffPerformance.length === 0 ? (
              <EmptyState
                icon="staff"
                title="No performance data yet"
                description="Your team, ranked by services completed and revenue generated, will appear here."
              />
            ) : (
              <ul className="list-unstyled mb-0">
                {staffPerformance.map((entry) => (
                  <li key={entry.tenantMemberId} className="d-flex justify-content-between align-items-center py-1">
                    <div>
                      <span className="fw-semibold small">{entry.name}</span>
                      <span className="text-muted small ms-2">{entry.servicesCompleted} completed</span>
                    </div>
                    <div className="small fw-semibold">
                      {entry.revenueGenerated.toFixed(2)} {entry.currencyCode}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
```

- [ ] **Step 3: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 4: Manual verification**

Run: `npm run dev`. As Owner, confirm every active team member appears (including anyone with zero completed bookings today, shown at zero), sorted by revenue descending, and that the services-completed count and revenue figure for each match today's actual completed bookings (cross-check against `AppointmentsPage` filtered per staff member). (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: every-active-staff-included display, revenue-descending order (as returned by the backend, not re-sorted client-side), and services-completed/revenue both shown per the design doc.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `useStaffPerformance`'s return shape matches how Task 3 destructures it.
