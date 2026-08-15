# ApexBooking Project Tracker

## Current Scope

Only the Booking module is being developed. Future modules (Customer
Management, Staff Management, Inventory, Sales, Purchasing, Reports) are
out of scope until explicitly assigned.

The Super Admin Portal (see below) was explicitly assigned separately from
the Booking module. It manages the ApexBooking platform itself (tenant access
requests, platform oversight) rather than a tenant's business, and is
intentionally isolated from the tenant application â€” its own layout,
navigation, and auth pages, sharing only the common design-system
primitives (`Button`, `Card`, `Badge`, `Modal`, `EmptyState`, `Skeleton`,
`Icon`, `PageHeader`, `FormGroup`).

## Booking Module

| Feature | Status | Notes |
|---|---|---|
| Landing Page | Complete | Static marketing page at `/`. Sections: Header, Hero, Businesses We Support, Booking Features, Dashboard Preview, Pricing, How It Works, Call To Action, Footer. PWA install button wired via `useInstallPrompt`. No auth, no API integration. |
| Login Page | Complete | UI-only form at `/login` (email, password, remember me, forgot password placeholder). Client-side validation only, no auth logic, no API calls. Shares `AuthLayout` and `FormGroup` with Request Access. |
| Request Access Page | Complete | UI-only form at `/request-access` matching the `RequestAccessCommand` shape (business name, description, business type, email, contact number). Client-side validation only, no API calls, no submission logic. |
| Dashboard Navigation & Auth Integration | Complete | Config-driven sidebar/topbar/mobile off-canvas nav shell at `/app/booking/*`, role-based visibility (Tenant/Staff) via `usePermissions`. Real `AuthController` integration (login, access-request, refresh, logout) â€” access token in `sessionStorage`, refresh token is httpOnly-cookie-only. `ProtectedRoute`'s auth check is commented out behind a `SAFEGUARD` marker for dev preview; role checks (`allowedRoles`) are active. Appointments, Calendar, Clients, Services, Business Profile, and both Settings pages are placeholder pages pending future tasks. Time Offs has a working "Add Time Off" modal shell with no backend yet. |
| Staff / Add Team Member | Complete | `/app/booking/staff` replaces the placeholder with a real `StaffPage`: paginated team list (`GET /api/Tenant/team`) and an "Add Team Member" modal (`POST /api/Tenant/add-team`) with a branch picker fed by `GET /api/Tenant/branches`. Role picker only offers Admin/Staff (Owner intentionally excluded). No edit or deactivate action yet â€” no backend endpoint exists for either. New `src/services/teamService.ts` and `branchService.ts`, `useTeamMembers`/`useBranches` hooks, and `src/components/team/` (`TeamMemberTable`, `TeamRoleBadge`, `TeamStatusBadge`, `AddTeamMemberModal`). |

## Super Admin Portal

| Feature | Status | Notes |
|---|---|---|
| Super Admin Login | Complete | UI-only form at `/admin/login` (email, password). Client-side validation only (required fields, email format), no auth integration, no API calls. Uses a dedicated `SuperAdminAuthLayout` (deep-ink brand panel, "Admin" badge, platform-oriented copy) so it reads as visually distinct from the tenant `/login` while reusing the same tokens, `FormGroup`, and `Button` components. Submit shows a real loading/disabled button state via a short simulated delay (no network call) then navigates to `/admin`. |
| Super Admin Layout | Complete | Dedicated `SuperAdminLayout` + `AdminSidebar` + `AdminTopbar` at `/admin/*` â€” does not reuse the tenant `Sidebar`/`Topbar`/`DashboardLayout` or the Booking module's nav config. Reuses the generic `MobileNav` off-canvas primitive for the responsive nav shell. Sidebar is configuration-driven via `ADMIN_NAV_ITEMS` (Dashboard, Tenant Requests) plus a static Profile/Logout footer block (Logout is a plain navigation link back to `/admin/login` â€” no session to invalidate since there's no auth integration yet). |
| Super Admin Dashboard | Complete | `/admin` (index route). Four `AdminSummaryCard` stats (Total/Pending/Approved/Rejected Tenant Requests) and a "Recent Tenant Requests" widget reusing `TenantRequestTable`. No fabricated data â€” counts are real `0`s and the table renders its empty state, matching the same "no fake statistics" approach used on `BookingOverviewPage`. |
| Tenant Request Management | Complete | `/admin/tenant-requests`. Responsive `TenantRequestTable` (Business Name, Business Type, Contact Email, Contact Number, Request Date, Status, Actions) with a `RequestActionMenu` dropdown (View Details, Approve, Reject) per row and a `RequestStatusBadge`. "View Details" opens a `TenantRequestDetailsModal` (built on the shared `Modal`) showing business name/type/description, contact email/number, and status. Approve/Reject are wired through as typed callback props but intentionally left as no-ops on both admin pages â€” there's no backend to mutate, and the request list is empty by design (empty state: "No tenant requests available."), so implementing real status mutation would mean fabricating business logic against fake data. |

New reusable components added under `src/components/admin/`: `AdminSummaryCard`,
`RequestStatusBadge`, `RequestActionMenu`, `TenantRequestTable`,
`TenantRequestDetailsModal`. `src/components/common/TableSkeleton.tsx` was
added as a generic reusable skeleton-loading primitive for tables (per the
loading standards) â€” `TenantRequestTable` accepts an `isLoading` prop and
renders it, but no page currently sets `isLoading: true` since there is no
real data fetching yet, mirroring the same restraint already noted below for
`BookingOverviewPage`'s stats. Three new hand-authored icons were added
(`dashboard`, `tenant-requests`, `x-circle`) matching the existing icon
set's style, since none of the 18 existing icons fit the sidebar/summary-card
needs.

Verified with `tsc -b`, `oxlint`, `vite build`, and a Playwright pass against
the dev server (`/admin/login` incl. client validation and the submit
loading state, `/admin` dashboard, `/admin/tenant-requests`, and the mobile
off-canvas nav) â€” zero console errors.

## Booking SaaS Visual Refactor (Complete)

The frontend was audited end-to-end and re-themed from an unbranded, ERP-like
Bootstrap admin look into a modern SaaS booking product. Architecture,
routing, permissions, and role-based menu visibility were left untouched â€”
this was a presentation-layer pass only.

- **Design tokens** â€” `src/styles/theme.css` overrides compiled Bootstrap's
  CSS variables (indigo primary palette, amber "pending" accent, soft
  shadows/radii, a real type scale) instead of shipping unmodified Bootstrap
  defaults. No new npm dependency; still Bootstrap 5 utility classes
  underneath, per `Claude/Technology_Stack.md`.
- **Icons** â€” sidebar/topbar/dashboard/feature icons were previously broken
  (`/assets/icons/*.svg` referenced files that didn't exist). 18 hand-authored
  single-color stroke SVGs were added under `public/assets/icons/` (see list
  below) rather than adding an icon library, per the icon-strategy decision
  made with the user. `src/components/common/Icon.tsx` centralizes the
  `/assets/icons/<name>.svg` path convention.
- **Reusable components added** â€” `Button`, `Card`, `Badge`, `PageHeader`,
  `LoadingSpinner`, `Skeleton` in `src/components/common/`, closing the gap
  called out in `Claude/Technology_Stack.md`'s "Reusable Component
  Philosophy" (previously every page hand-wrote inline Bootstrap markup).
- **Sidebar** â€” `ISidebarNavItem` gained an optional-free, always-present
  `section: 'scheduling' | 'manage' | 'settings'` field (config-driven, no
  hardcoded menus). `Sidebar.tsx` renders section labels/dividers from that
  field. Items were reordered so Time Offs sits with Appointments/Calendar
  under "Scheduling" (previously between Business Profile and Settings) â€”
  same 8 items, same roles, no visibility change. `ModuleSwitcher` now shows
  a plain brand block instead of a permanently-disabled `<select>` when only
  one module is registered.
- **Dashboard** (`BookingOverviewPage.tsx`) â€” rebuilt as an operational
  workspace: `PageHeader` with today's date and a primary "New Appointment"
  action, then Today's Appointments / Upcoming This Week stats, Quick
  Actions, Today's Calendar, Staff Availability, Booking Status Summary, and
  Recent Customer Activity. All non-stat widgets are honest empty states
  (no fabricated data) since no backend wiring exists yet.
- **Landing page** â€” messaging shifted from "multi-tenant platform" /
  "modules" toward booking-specific value props (online booking, staff
  scheduling, customer booking experience). Added `SchedulePreviewCard`
  (real markup, not a generated image) as the hero/dashboard-preview
  signature element, replacing the placeholder PWA icon reused as a fake
  screenshot. Feature icons were also fixed â€” they previously reused
  mismatched industry SVGs (e.g. "Online Booking" showed the Salon icon).
- **Auth pages** â€” `AuthLayout`'s left panel now uses a brand gradient with
  the same `SchedulePreviewCard` instead of the reused placeholder
  illustration image. Request Access form fields grouped under "Business
  details" / "Contact details" labels. No functional/validation changes.
- **Empty states** â€” generic "Nothing here yet" copy replaced with
  page-specific booking language (e.g. "No staff members yet", "No services
  set up yet") across all placeholder module routes and Time Offs.
- Verified with `tsc -b`, `oxlint`, `vite build`, and a Playwright pass
  against the dev server (landing, login, request-access, dashboard as both
  Tenant and Staff roles, mobile nav) â€” zero console errors.

## Known Follow-Ups (not started, not in current scope)

- The 18 hand-authored icons (`appointments`, `calendar`, `clients`, `staff`,
  `services`, `business-profile`, `time-offs`, `settings`, `menu`, `clock`,
  `check-circle`, `check-circle-light`, `activity`, `user-check`, `globe`,
  `mail`, `chat`, `chart`) are placeholder-quality line icons meant to unblock
  the broken-image issue â€” swap for final brand icons whenever a designer
  delivers a real set.
- `LoadingSpinner`/`Skeleton` components exist and are ready to wire in, but
  the dashboard has no real data fetching yet (stats are still static `0`s),
  so nothing currently triggers the skeleton loading state. Wire it in once
  `BookingOverviewPage` fetches real data.
- Login and Request Access now submit to the real `AuthController` (see
  Dashboard Navigation & Auth Integration row above) â€” no forgot-password
  page yet, that remains a future task.
- `ProtectedRoute.tsx`'s `SAFEGUARD: AUTHENTICATION` block must be uncommented
  once real login is expected to gate the dashboard â€” currently `/app/booking`
  is reachable without logging in.
- The backend's CORS policy must allow credentials from the frontend's exact
  origin (not `*`) for the httpOnly refresh cookie to round-trip correctly â€”
  this is backend configuration, not addressed by this frontend change.
- No reset-password page exists yet even though `AuthController` exposes
  `POST /api/Auth/reset-password` â€” out of scope until requested.
- The Super Admin Portal has no authentication integration at all yet (no
  `SuperAdminAuthContext`, no route guard on `/admin/*`) â€” `/admin` is
  reachable without logging in, the same way `/app/booking` currently is.
  Wire a guard (and a real `Role`/permission model for super admins) once
  Super Admin authentication is assigned.
- `TenantRequestTable`'s `isLoading` prop and `TableSkeleton` exist and are
  ready to wire in, but neither admin page fetches real data yet, so the
  skeleton is never triggered â€” same situation as the Booking dashboard's
  `Skeleton` component above.
- `TenantRequestTable`'s Approve/Reject actions are UI-complete (dropdown
  items, callback props) but intentionally no-ops â€” there is no backend to
  persist a status change against yet.
