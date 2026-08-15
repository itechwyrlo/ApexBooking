# Owner Dashboard Phase 2 — Frontend (Total Shop Revenue) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the "Total Shop Revenue" placeholder on `OwnerDashboardPage.tsx` into a real total with an Online / Pay-on-Visit breakdown.

**Architecture:** A new `useTenantRevenue(date)` hook fetches the new backend endpoint (companion plan: `docs/superpowers/plans/2026-08-14-owner-dashboard-phase2-backend.md` in the ApexBooking repo). Same `getTodayIsoDate()` local helper already used on this page.

**Tech Stack:** React 19 + TypeScript.

## Global Constraints

- No test runner configured. Verification per task is `npm run build`, run manually by the user.
- This plan assumes the backend companion plan has landed.

---

### Task 1: Types and service function

**Files:**
- Create: `src/interfaces/ITenantRevenue.ts`
- Modify: `src/services/bookingService.ts`

**Interfaces:**
- Produces: `ITenantRevenue { onlineAmount, payInVisitAmount, total, currencyCode }`, `getTenantRevenue(date): Promise<ITenantRevenue>`.

- [ ] **Step 1: Create ITenantRevenue**

```ts
// Mirrors ApexBooking.Core.Application.Dtos.Response.TenantRevenueDto
export interface ITenantRevenue {
  onlineAmount: number
  payInVisitAmount: number
  total: number
  currencyCode: string
}
```

Save as `src/interfaces/ITenantRevenue.ts`.

- [ ] **Step 2: Add getTenantRevenue to bookingService**

In `src/services/bookingService.ts`, add the import and function:

```ts
import type { ITenantRevenue } from '../interfaces/ITenantRevenue'
```

```ts
export async function getTenantRevenue(date: string): Promise<ITenantRevenue> {
  const response = await authClient.get<ITenantRevenue>('/api/Tenant/bookings/revenue', { params: { date } })
  return response.data
}
```

- [ ] **Step 3: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

---

### Task 2: useTenantRevenue hook

**Files:**
- Create: `src/hooks/useTenantRevenue.ts`

**Interfaces:**
- Consumes: `getTenantRevenue` from Task 1.
- Produces: `useTenantRevenue(date: string) => { revenue: ITenantRevenue | null; isLoading: boolean }` — consumed by Task 3.

- [ ] **Step 1: Write the hook**

```ts
import { useEffect, useState } from 'react'
import { getTenantRevenue } from '../services/bookingService'
import type { ITenantRevenue } from '../interfaces/ITenantRevenue'

interface IUseTenantRevenueResult {
  revenue: ITenantRevenue | null
  isLoading: boolean
}

export function useTenantRevenue(date: string): IUseTenantRevenueResult {
  const [revenue, setRevenue] = useState<ITenantRevenue | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getTenantRevenue(date)
      .then((result) => {
        if (isMounted) setRevenue(result)
      })
      .catch(() => {
        if (isMounted) setRevenue(null)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [date])

  return { revenue, isLoading }
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
- Consumes: `useTenantRevenue` (Task 2).

- [ ] **Step 1: Add the import and hook call**

Add alongside the existing imports:

```tsx
import { useTenantRevenue } from '../../hooks/useTenantRevenue'
```

Add alongside the existing `useTenantBookings`/`useRefundLog` calls:

```tsx
  const { revenue, isLoading: isRevenueLoading } = useTenantRevenue(todayIso)
```

- [ ] **Step 2: Replace the placeholder**

Replace the "Total Shop Revenue" card's contents (currently an `EmptyState`):

```tsx
      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Total Shop Revenue</h2>
            {isRevenueLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : !revenue ? (
              <p className="text-muted small mb-0">Failed to load today's revenue.</p>
            ) : (
              <div>
                <p className="fs-3 fw-bold mb-1">
                  {revenue.total.toFixed(2)} {revenue.currencyCode}
                </p>
                <p className="text-muted small mb-0">
                  Online: {revenue.onlineAmount.toFixed(2)} {revenue.currencyCode} &middot; Pay on Visit:{' '}
                  {revenue.payInVisitAmount.toFixed(2)} {revenue.currencyCode}
                </p>
              </div>
            )}
          </Card>
        </div>
      </div>
```

- [ ] **Step 3: Type-check**

Run: `npm run build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 4: Manual verification**

Run: `npm run dev`. As Owner, confirm the total and the Online/Pay-on-Visit breakdown match today's actual paid bookings (cross-check against `AppointmentsPage`). Confirm the two subtotals add up to the total shown. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: total + Online/Pay-on-Visit breakdown display matches the design doc exactly.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `useTenantRevenue`'s return shape matches how Task 3 destructures it.
