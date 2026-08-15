# Role-Based Dashboards — Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the single, static, identical-for-everyone `/:slug/dashboard` page into three role-specific skeleton pages (Owner, Admin, Staff), each labeled with the sections/tools it will eventually hold, and wire routing so the right one renders based on the logged-in user's highest-precedence role.

**Architecture:** Three new presentational page components (`OwnerDashboardPage`, `AdminDashboardPage`, `StaffDashboardPage`) each render a `PageHeader` + a grid of `Card`s, using the existing `EmptyState` component for each not-yet-built report section and disabled `Button`s for each not-yet-built Quick Tool. A fourth component, `DashboardPage` (replacing today's `BookingOverviewPage`), is a pure dispatcher: it reads `useAuth().user.roles` and renders whichever of the three pages matches the highest-precedence role the user holds (Owner > Admin > Staff). `AppRoutes.tsx`'s existing index route is repointed to `DashboardPage`. No backend changes.

**Tech Stack:** React 19 + TypeScript, react-router-dom v7, Bootstrap 5 utility classes (no CSS framework changes needed — this plan only composes existing components).

## Global Constraints

- No backend changes in this plan — role is already available client-side via `useAuth().user.roles` (`IUser.roles: Role[]`, from `src/interfaces/IUser.ts`).
- LocalFlow has no test runner configured (verified: `package.json` has no `test` script, no vitest/jest dependency). There are no automated test steps in this plan — each task's verification step is `npm run build` (runs `tsc -b`, i.e., a type-check) plus a manual visual check in `npm run dev`. Per the user's standing preference this session, do not run `npm run build`/`npm run dev`/commit — leave those for the user to run manually after each task, unless they ask otherwise.
- Icon names must reference an existing file in `public/assets/icons/` (the `Icon` component resolves `name` straight to `/assets/icons/{name}.svg` with no fallback — a wrong name silently 404s). Icons used in this plan (`chart`, `download`, `refund`, `staff`, `clock`, `qr-code`, `x-circle`, `dashboard`, `activity`, `alert-triangle`, `plus`, `refresh`, `check-circle`, `clients`, `time-offs`, `edit`) were all confirmed present in `public/assets/icons/` during design.
- Follow the existing page-file convention: default-exported-by-name function components in `src/pages/booking/`, importing shared primitives from `src/components/common/` (`Card`, `Button`, `EmptyState`, `PageHeader`, `Icon` is used internally by `Button`/`EmptyState`, not imported directly by pages).
- Every new/changed file uses the same `TODAY_LABEL` date-formatting snippet already used in the page being replaced (`BookingOverviewPage.tsx:9-13`) — copy it verbatim into each of the three new page files (each page needs its own header description independently; this is 5 lines duplicated 3 times, not worth extracting into a shared util for 3 call sites).

---

### Task 1: Owner Dashboard skeleton page

**Files:**
- Create: `src/pages/booking/OwnerDashboardPage.tsx`

**Interfaces:**
- Consumes: `Card` (`src/components/common/Card.tsx`, accepts `className` + `children`), `Button` (`src/components/common/Button.tsx`, props used here: `variant`, `size`, `icon`, `disabled`, `children`), `EmptyState` (`src/components/common/EmptyState.tsx`, props: `icon`, `title`, `description`), `PageHeader` (`src/components/common/PageHeader.tsx`, props: `title`, `description`).
- Produces: `OwnerDashboardPage` — a zero-prop component, default export not used (named export, matching every other page in `src/pages/booking/`). Consumed by Task 4's `DashboardPage`.

- [ ] **Step 1: Write the component**

```tsx
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'

const TODAY_LABEL = new Date().toLocaleDateString(undefined, {
  weekday: 'long',
  month: 'long',
  day: 'numeric',
})

export function OwnerDashboardPage() {
  return (
    <div>
      <PageHeader title="Business Overview" description={TODAY_LABEL} />

      <div className="row g-3 mb-3">
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Total Shop Revenue</h2>
            <EmptyState
              icon="chart"
              title="No revenue yet today"
              description="Gross earnings from online and pay-on-visit bookings will total here."
            />
          </Card>
        </div>
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Online Payout Status</h2>
            <EmptyState
              icon="download"
              title="No pending payouts"
              description="Funds processed online and awaiting transfer to your bank account will show here."
            />
          </Card>
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Refund Log</h2>
            <EmptyState
              icon="refund"
              title="No refunds yet"
              description="Processed refunds will list here with amount, date, and original payment method."
            />
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
            <EmptyState
              icon="clock"
              title="No bookings assigned to you today"
              description="If you take bookings yourself, your personal client lineup for today will appear here."
            />
          </Card>
        </div>
      </div>

      <div className="row g-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Quick Tools</h2>
            <div className="d-flex flex-wrap gap-2">
              <Button variant="outline-secondary" size="sm" icon="qr-code" disabled>
                Scan Booking QR
              </Button>
              <Button variant="outline-secondary" size="sm" icon="x-circle" disabled>
                Cancel &amp; Refund
              </Button>
            </div>
          </Card>
        </div>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no TypeScript errors attributable to this new file. (User runs this manually.)

---

### Task 2: Admin Dashboard skeleton page

**Files:**
- Create: `src/pages/booking/AdminDashboardPage.tsx`

**Interfaces:**
- Consumes: same shared components as Task 1.
- Produces: `AdminDashboardPage` — zero-prop named export. Consumed by Task 4's `DashboardPage`.

- [ ] **Step 1: Write the component**

```tsx
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'

const TODAY_LABEL = new Date().toLocaleDateString(undefined, {
  weekday: 'long',
  month: 'long',
  day: 'numeric',
})

export function AdminDashboardPage() {
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
            <EmptyState
              icon="activity"
              title="No bookings yet today"
              description="Real-time tallies of Pending, Checked-In, Completed, and Missed bookings will appear here."
            />
          </Card>
        </div>
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Unassigned Bookings</h2>
            <EmptyState
              icon="alert-triangle"
              title="No unassigned bookings"
              description="Online bookings without a staff member assigned yet will be flagged here."
            />
          </Card>
        </div>
      </div>

      <div className="row g-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Quick Tools</h2>
            <div className="d-flex flex-wrap gap-2">
              <Button variant="outline-secondary" size="sm" icon="qr-code" disabled>
                Scan Booking QR
              </Button>
              <Button variant="outline-secondary" size="sm" icon="plus" disabled>
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
    </div>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no TypeScript errors attributable to this new file. (User runs this manually.)

---

### Task 3: Staff Dashboard skeleton page

**Files:**
- Create: `src/pages/booking/StaffDashboardPage.tsx`

**Interfaces:**
- Consumes: same shared components as Task 1.
- Produces: `StaffDashboardPage` — zero-prop named export. Consumed by Task 4's `DashboardPage`.

- [ ] **Step 1: Write the component**

```tsx
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'

const TODAY_LABEL = new Date().toLocaleDateString(undefined, {
  weekday: 'long',
  month: 'long',
  day: 'numeric',
})

export function StaffDashboardPage() {
  return (
    <div>
      <PageHeader title="My Day" description={TODAY_LABEL} />

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">My Daily Lineup</h2>
            <EmptyState
              icon="clock"
              title="No appointments assigned to you today"
              description="A chronological list of just your appointments for today will appear here."
            />
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

- [ ] **Step 2: Type-check**

Run: `npm run build`
Expected: no TypeScript errors attributable to this new file. (User runs this manually.)

---

### Task 4: Dispatcher + routing

**Files:**
- Create: `src/pages/booking/DashboardPage.tsx`
- Delete: `src/pages/booking/BookingOverviewPage.tsx` (fully superseded — its content was 100% static placeholder data, none of it real; see foundation design doc for the specific analysis)
- Modify: `src/routes/AppRoutes.tsx:10` (import) and `:72` (route element)

**Interfaces:**
- Consumes: `Role` (`src/types/Role.ts`, `{ Owner: 'Owner', Admin: 'Admin', Staff: 'Staff' }`), `useAuth` (`src/hooks/useAuth.ts`, returns `{ user }` where `user: IUser | null` and `IUser.roles: Role[]`), `OwnerDashboardPage`/`AdminDashboardPage`/`StaffDashboardPage` from Tasks 1–3.
- Produces: `DashboardPage` — zero-prop named export, mounted at the existing `/:slug/dashboard` index route in `AppRoutes.tsx`.

- [ ] **Step 1: Write the dispatcher**

```tsx
import { Role } from '../../types/Role'
import { useAuth } from '../../hooks/useAuth'
import { OwnerDashboardPage } from './OwnerDashboardPage'
import { AdminDashboardPage } from './AdminDashboardPage'
import { StaffDashboardPage } from './StaffDashboardPage'

// A membership can hold more than one role (IUser.roles is an array) — the
// highest-authority role a user holds decides which dashboard they land on.
const ROLE_PRECEDENCE: Role[] = [Role.Owner, Role.Admin, Role.Staff]

export function DashboardPage() {
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const primaryRole = ROLE_PRECEDENCE.find((role) => roles.includes(role))

  if (primaryRole === Role.Admin) {
    return <AdminDashboardPage />
  }
  if (primaryRole === Role.Staff) {
    return <StaffDashboardPage />
  }
  return <OwnerDashboardPage />
}
```

Save this as `src/pages/booking/DashboardPage.tsx`.

- [ ] **Step 2: Delete the superseded page**

Delete `src/pages/booking/BookingOverviewPage.tsx`.

- [ ] **Step 3: Repoint the route**

In `src/routes/AppRoutes.tsx`, change the import on line 10:

```tsx
import { BookingOverviewPage } from '../pages/booking/BookingOverviewPage'
```
to:
```tsx
import { DashboardPage } from '../pages/booking/DashboardPage'
```

And change the index route element on line 72:

```tsx
<Route index element={<BookingOverviewPage />} />
```
to:
```tsx
<Route index element={<DashboardPage />} />
```

- [ ] **Step 4: Type-check**

Run: `npm run build`
Expected: no TypeScript errors — in particular, confirm no other file still imports `BookingOverviewPage` (grep for it) now that it's deleted. (User runs this manually.)

- [ ] **Step 5: Manual verification**

Run: `npm run dev`, then for each of an Owner, an Admin, and a Staff test account: log in and navigate to `/:slug/dashboard`. Confirm:
- Owner account → sees "Business Overview" with Total Shop Revenue / Online Payout Status / Refund Log / Staff Performance / My Personal Lineup sections and the Scan Booking QR / Cancel & Refund quick tools (disabled).
- Admin account → sees "Front Desk" with Master Visual Grid / Daily Booking Counters / Unassigned Bookings sections and the four disabled quick tools.
- Staff account → sees "My Day" with My Daily Lineup / Client Preferences sections and the two disabled quick tools.
(User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: every section named in the foundation design doc (Owner: 5 report sections + 2 tools; Admin: 3 report sections + 4 tools; Staff: 2 report sections + 2 tools) has a corresponding `EmptyState`/`Button` in the task above. Role precedence, single-URL dispatch, and the `BookingOverviewPage` deletion are all covered.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `DashboardPage` imports the exact named exports (`OwnerDashboardPage`, `AdminDashboardPage`, `StaffDashboardPage`) declared in Tasks 1–3; `Role.Owner`/`Role.Admin`/`Role.Staff` match `src/types/Role.ts`'s existing values exactly.
