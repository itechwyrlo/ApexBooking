# Staff Dashboard Phase 1 — Frontend (My Daily Lineup Timeline) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the "My Daily Lineup" placeholder on `StaffDashboardPage.tsx` with a real, chronological list of the logged-in staff member's own bookings for today.

**Architecture:** Decode the new `tenant_member_id` JWT claim (backend companion plan: `docs/superpowers/plans/2026-08-12-staff-dashboard-phase1-backend.md` in the ApexBooking repo) into `IUser.tenantMemberId`, the same way `tenant_id`/`tenant_slug` are already decoded. A new `StaffLineupTimeline` component renders a vertical list from the existing `useTenantBookings` hook, filtered to `{ staffId: user.tenantMemberId, fromDate: today, toDate: today }` — no new backend query needed, this filter already exists and works.

**Tech Stack:** React 19 + TypeScript, existing `useTenantBookings`/`getTenantBookings` plumbing, Bootstrap utility classes.

## Global Constraints

- No test runner is configured in this repo (no `test` script, no vitest/jest). Verification per task is `npm run build` (`tsc -b` type-check), run manually by the user — do not run it yourself per the standing instruction for this session.
- This plan assumes the backend companion plan has landed and issues a `tenant_member_id` claim (string GUID) on tenant-session tokens.
- Icon names must reference an existing file in `public/assets/icons/` — `clock` (used below) is already confirmed present (used elsewhere this session, e.g. `OwnerDashboardPage.tsx`'s "My Personal Lineup" section).

---

### Task 1: Decode the TenantMemberId claim

**Files:**
- Modify: `src/utils/jwt.ts`
- Modify: `src/interfaces/IUser.ts`
- Modify: `src/contexts/AuthContext.tsx`

**Interfaces:**
- Produces: `IUser.tenantMemberId: string | null`, decoded from the JWT.

- [ ] **Step 1: Decode the claim**

In `src/utils/jwt.ts`, add the claim key constant, extend `IDecodedAccessToken`, and decode it:

```ts
import type { Role } from '../types/Role'

export interface IDecodedAccessToken {
  sub: string
  email: string
  tenantId: string
  isPlatformAdmin: boolean
  roles: Role[]
  slug: string | null
  tenantMemberId: string | null
}

// Claim URIs/keys written by JwtTokenService.BuildClaims (ApexBooking.Core.Persistence)
const EMAIL_CLAIM_KEY = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
const ROLE_CLAIM_KEY = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
const TENANT_ID_CLAIM_KEY = 'tenant_id'
const PLATFORM_ADMIN_CLAIM_KEY = 'platform_admin'
const TENANT_SLUG_CLAIM_KEY = 'tenant_slug'
const TENANT_MEMBER_ID_CLAIM_KEY = 'tenant_member_id'

export function decodeJwt(token: string): IDecodedAccessToken {
  const payloadSegment = token.split('.')[1]
  const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/')
  const padding = (4 - (base64.length % 4)) % 4
  const padded = base64 + '='.repeat(padding)
  const json = decodeURIComponent(
    atob(padded)
      .split('')
      .map((char) => '%' + char.charCodeAt(0).toString(16).padStart(2, '0'))
      .join(''),
  )
  const payload = JSON.parse(json) as Record<string, unknown>
  const rawRoles = payload[ROLE_CLAIM_KEY]
  const roles = Array.isArray(rawRoles) ? (rawRoles as Role[]) : rawRoles ? [rawRoles as Role] : []

  return {
    sub: String(payload.sub ?? ''),
    email: String(payload[EMAIL_CLAIM_KEY] ?? ''),
    tenantId: String(payload[TENANT_ID_CLAIM_KEY] ?? ''),
    isPlatformAdmin: payload[PLATFORM_ADMIN_CLAIM_KEY] === 'true' || payload[PLATFORM_ADMIN_CLAIM_KEY] === true,
    roles,
    slug: payload[TENANT_SLUG_CLAIM_KEY] ? String(payload[TENANT_SLUG_CLAIM_KEY]) : null,
    tenantMemberId: payload[TENANT_MEMBER_ID_CLAIM_KEY] ? String(payload[TENANT_MEMBER_ID_CLAIM_KEY]) : null,
  }
}
```

- [ ] **Step 2: Extend IUser**

In `src/interfaces/IUser.ts`:

```ts
import type { Role } from '../types/Role'

export interface IUser {
  id: string
  email: string
  tenantId: string
  isPlatformAdmin: boolean
  roles: Role[]
  slug: string | null
  tenantMemberId: string | null
}
```

- [ ] **Step 3: Map it through in AuthContext**

In `src/contexts/AuthContext.tsx`, change `buildUserFromToken` (currently lines 56-66):

```ts
function buildUserFromToken(token: string): IUser {
  const decoded = decodeJwt(token)
  return {
    id: decoded.sub,
    email: decoded.email,
    tenantId: decoded.tenantId,
    isPlatformAdmin: decoded.isPlatformAdmin,
    roles: decoded.roles,
    slug: decoded.slug,
    tenantMemberId: decoded.tenantMemberId,
  }
}
```

- [ ] **Step 4: Type-check**

Run: `npm run build`
Expected: no TypeScript errors. (User runs this manually.)

---

### Task 2: StaffLineupTimeline component

**Files:**
- Create: `src/components/dashboard/StaffLineupTimeline.tsx`

**Interfaces:**
- Consumes: `ITenantBooking` (`src/interfaces/ITenantBooking.ts`, already defines `bookingId`, `scheduledStartTime`, `serviceName`, `customerName`, `status`), `EmptyState`, `BookingStatusBadge` (`src/components/admin/BookingStatusBadge.tsx`), `formatDisplayTime` (`src/utils/formatDateTime.ts`).
- Produces: `StaffLineupTimeline` — props `{ bookings: ITenantBooking[]; isLoading?: boolean }`. Consumed by Task 3.

- [ ] **Step 1: Write the component**

```tsx
import { EmptyState } from '../common/EmptyState'
import { BookingStatusBadge } from '../admin/BookingStatusBadge'
import { formatDisplayTime } from '../../utils/formatDateTime'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface IStaffLineupTimelineProps {
  bookings: ITenantBooking[]
  isLoading?: boolean
}

export function StaffLineupTimeline({ bookings, isLoading }: IStaffLineupTimelineProps) {
  if (isLoading) {
    return <p className="text-muted small mb-0">Loading your lineup…</p>
  }

  if (bookings.length === 0) {
    return (
      <EmptyState
        icon="clock"
        title="No appointments assigned to you today"
        description="A chronological list of just your appointments for today will appear here."
      />
    )
  }

  const sorted = [...bookings].sort((a, b) => a.scheduledStartTime.localeCompare(b.scheduledStartTime))

  return (
    <ul className="list-unstyled mb-0">
      {sorted.map((booking) => (
        <li key={booking.bookingId} className="d-flex gap-3 py-2 border-bottom">
          <div className="pb-mono small text-muted" style={{ minWidth: 72 }}>
            {formatDisplayTime(booking.scheduledStartTime)}
          </div>
          <div className="flex-grow-1">
            <div className="fw-semibold">{booking.serviceName}</div>
            <div className="text-muted small">{booking.customerName}</div>
          </div>
          <div>
            <BookingStatusBadge status={booking.status} />
          </div>
        </li>
      ))}
    </ul>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no TypeScript errors attributable to this new file. (User runs this manually.)

---

### Task 3: Wire it into StaffDashboardPage

**Files:**
- Modify: `src/pages/booking/StaffDashboardPage.tsx`

**Interfaces:**
- Consumes: `useAuth` (`src/hooks/useAuth.ts`), `useTenantBookings` (`src/hooks/useTenantBookings.ts`), `StaffLineupTimeline` from Task 2.

- [ ] **Step 1: Replace the placeholder section with real data**

Replace the full contents of `src/pages/booking/StaffDashboardPage.tsx` with:

```tsx
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'
import { StaffLineupTimeline } from '../../components/dashboard/StaffLineupTimeline'
import { useAuth } from '../../hooks/useAuth'
import { useTenantBookings } from '../../hooks/useTenantBookings'

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

export function StaffDashboardPage() {
  const { user } = useAuth()
  const todayIso = getTodayIsoDate()
  const { bookings, isLoading } = useTenantBookings({
    staffId: user?.tenantMemberId ?? undefined,
    fromDate: todayIso,
    toDate: todayIso,
  })

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
            <EmptyState
              icon="clients"
              title="No client notes yet"
              description="Past service notes for your active client will preview here."
            />
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
              <Button variant="outline-secondary" size="sm" icon="edit" disabled>
                Save Chair Notes
              </Button>
            </div>
          </Card>
        </div>
      </div>
    </div>
  )
}
```

(Only the "My Daily Lineup" section changes from the Foundation sub-project's version — "Client Preferences" and "Quick Tools" stay as placeholders, deferred to Phases 2 and 3.)

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no TypeScript errors. (User runs this manually.)

- [ ] **Step 3: Manual verification**

Run: `npm run dev`. Log in as a Staff test account whose token now carries `tenant_member_id` (requires the backend companion plan to be deployed). Confirm `/:slug/dashboard` → "My Day" shows that staff member's actual bookings for today, sorted chronologically with time/service/customer/status, or the empty state if they have none today. Confirm a booking assigned to a *different* staff member does NOT appear. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: claim decoding and the real "My Daily Lineup" widget are both covered; "Client Preferences" and "Quick Tools" are explicitly left as placeholders per the Phase 1 design doc's stated scope.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `StaffLineupTimeline`'s props (`bookings: ITenantBooking[]`, `isLoading?: boolean`) match exactly what `useTenantBookings` returns (`{ bookings, isLoading, ... }`) and what Task 3 passes in.
