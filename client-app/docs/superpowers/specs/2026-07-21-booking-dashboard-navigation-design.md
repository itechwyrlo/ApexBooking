# Booking Dashboard Navigation & Workspace Architecture — Design Spec

Status: Approved (source: user-authored feature prompt, refined through
clarifying questions below)
Scope: Application-wide navigation architecture, dashboard shell, the
Booking module's workspace navigation, and real integration against the
existing `AuthController` (login, refresh, logout, request-access) needed
to drive role-based menu visibility and gate/ungate the dashboard. Covers
configuration structure, folder layout, routing shape, and the auth
scaffold. Does **not** cover visual styling details, individual page
content beyond navigation, or a reset-password flow.

## Source of Truth

Content and requirements are defined by the feature prompt supplied by the
user (navigation requirements, current/future module list, Booking feature
list, role definitions, deliverables). This document records the
architectural decisions made to satisfy that prompt and resolves what it
left open.

Governing standards: `Claude/AI_ROLE_&_Core_Principles.md`,
`Claude/Technology_Stack.md`, `Claude/Progressive_Web_App_Standards.md`
(these three files together serve as `BASE_PROMPT.md`, which does not exist
as a standalone file in this project).

## Current State

No dashboard, sidebar, or authentication context exists yet. `App.tsx` only
defines `/`, `/login`, `/request-access`. Login and Request Access are
UI-only (per `PROJECT_TRACKER.md`) — no session/auth logic anywhere. An
`INavItem` interface and `config/navigation.ts` already exist but are scoped
to the Landing Page's anchor-link nav (`{ label, href }`) — unrelated shape,
not reused here. `react-router-dom` is installed. Only the Booking module is
in scope for implementation; other modules (Inventory, Customers, CRM,
Sales, Purchasing, Reports) are architected for but not built.

## Resolved Ambiguities (via clarifying questions)

1. **Role/auth data source** — no backend exists, so menu-visibility
   filtering needs a role source today. Resolved: build a full auth
   *scaffold* (`AuthContext`, `ProtectedRoute`, `authService`) rather than
   just a type contract, so the dashboard is navigable and demonstrable
   end-to-end right now.
2. **How the auth scaffold's HTTP calls behave with no real endpoints** —
   *initially* resolved as local simulation (no network call). **Superseded**
   once real endpoints were provided (see Auth API Integration section below)
   — `authService` now makes real Axios calls against `AuthController`.
3. **Reaching the dashboard without logging in** — resolved: `ProtectedRoute`
   contains the real redirect-if-unauthenticated check, written but
   commented out with a fixed marker so it's a single, greppable toggle:
   ```tsx
   // SAFEGUARD: AUTHENTICATION — uncomment when backend integration begins
   // if (!isAuthenticated) return <Navigate to="/login" replace />
   ```
   This still applies even with real endpoints wired up: it lets the
   dashboard be viewed without going through a real login while the rest of
   the flow is being integrated.
4. **Token storage** — evaluated `localStorage` vs. in-memory-only.
   Superseded by a third option once the backend's actual shape was known:
   **the refresh token is delivered as an httpOnly cookie, set and rotated
   entirely server-side** (the frontend never reads, stores, or references
   it — that's what httpOnly means). The **access token** is short-lived by
   design and stored in `sessionStorage`. See Auth API Integration below.
5. **`RequestAccessCommand`/`LoginCommand` response shapes and endpoint
   paths** — initially unknown/placeholder. Resolved with the real
   `AuthController` contract (base path `api/Auth`, actions
   `access-request`, `login`, `refresh`, `logout`, `reset-password`) and a
   confirmed local dev base URL, `http://localhost:5104`. See Auth API
   Integration below.
6. **Primary navigation pattern** — evaluated three approaches (single
   sidebar with embedded module switcher; dual icon-rail + contextual
   sidebar; top horizontal module nav + left sidebar). Chose the single
   sidebar approach — see Navigation Pattern section.
7. **Time Offs: standalone page vs. expandable group** — resolved as a
   standalone sidebar entry, not expandable. Add and List are not
   independent destinations; Add is an action that feeds the List, not a
   page a user navigates to repeatedly on its own.
8. **Settings: standalone page vs. expandable group** — resolved as a
   standalone sidebar entry that routes into a self-contained Settings
   sub-shell with its own internal sub-navigation, rather than expanding
   categories directly into the main sidebar. Keeps the main sidebar's
   length decoupled from how many settings categories exist over time.
9. **App-level dashboard vs. module dashboard** — resolved: no separate
   empty "Application Dashboard" is built today, since only one module
   exists (would be scaffolding an unrequested future feature). `/app`
   redirects to `/app/booking`, whose Overview page serves as the
   dashboard. The route/module-registry structure is what allows a true
   cross-module dashboard to be added later without touching Booking.

## Navigation Pattern

**Chosen: single persistent sidebar with an embedded module switcher.**

A left sidebar has a small module-switcher control in its header (today
lists "Booking" only). Below it, the sidebar renders the *active module's*
nav tree, generated entirely from that module's config file. There is no
second/secondary nav bar — the sidebar **is** the workspace navigation for
whichever module is active.

Two alternatives were evaluated and rejected for the current stage:

- **Dual-rail (icon rail + contextual sidebar)** — the pattern GitLab/Azure
  Portal/Zendesk use at large module counts. Rejected for now: requires
  custom CSS Bootstrap has no primitive for, and adds a second responsive
  surface to collapse on mobile, for a platform that currently ships one
  module. Revisit if the module count grows large enough to justify it —
  the module-registry/config data model underneath does not need to change
  to make that switch later.
- **Top horizontal module nav + left contextual sidebar** (Salesforce
  Lightning style) — rejected: splits navigation attention across two axes
  with only one module currently registered, and is harder to collapse
  cleanly on mobile than a single sidebar.

Scalability lives in the *config and data layer* (module registry, per-
module nav config, permission filtering), not in pre-built chrome for
modules that don't exist yet. Adding a module means: one entry in
`modules.config.ts` + one new `*.nav.config.ts` file. The `Sidebar`
component itself never changes.

## Sidebar Hierarchy (Booking module, active)

```
Sidebar
├─ Module Switcher (dropdown; today: "Booking" only)
├─ Appointments
├─ Calendar
├─ Clients                    ← Tenant only
├─ Staff                      ← Tenant only
├─ Services                   ← Tenant only
├─ Business Profile           ← Tenant only
├─ Time Offs                  ← standalone, not expandable
├─ Settings                   ← standalone, routes to its own sub-shell
└─ (user menu / logout — footer, outside nav config)
```

Every entry is a row in a config array (`{ label, href, icon, roles }`).
`Sidebar` recursively renders items and filters by `roles` at render time —
no per-item role conditionals anywhere else in the app. The item schema
also supports an optional `children` array for future modules that need
genuinely independent nested destinations; nothing in Booking uses it
today, which is intentional (no unrequested feature is being pre-built),
but its presence in the schema is what avoids a redesign later.

## Booking Workspace Hierarchy

### Time Offs

Single sidebar entry. Default view is the Time Off List; "Add Time Off" is
a primary button on that page that opens a Bootstrap `Modal` (not a
separate route/nav destination), keeping the user in list context. Expandable
nav groups are reserved for pages a user would bookmark or jump to directly
and repeatedly — Add is an action that feeds the List, not an independent
destination.

### Settings

Single sidebar entry, routing into a dedicated Settings workspace with its
own internal sub-navigation (in-page tabs or mini left-nav), driven by its
own config file:

```
Settings (sidebar entry)
  └─ Settings workspace (own layout)
       ├─ Booking Settings   (default)
       └─ Payment Settings
```

New settings categories are added by extending `settings.nav.config.ts`
only — the main sidebar's length never depends on how many settings
categories exist. Mirrors the settings-area pattern used by Stripe, GitHub,
and Linear.

## Auth API Integration

Base URL (local dev): `http://localhost:5104`, read from
`VITE_API_BASE_URL` (Vite env var — `.env` holds the real local value,
`.env.example` documents the key with no value committed).

Endpoints, all under `AuthController`'s base path (`api/Auth`):

| Method | Path | Request | Response | Notes |
|---|---|---|---|---|
| POST | `/api/Auth/access-request` | `RequestAccessCommand` (businessName, description, businessType, email, contactNumber) | 202 `{ tenantId, status }`, 400 on validation failure | Wires the existing Request Access page (UI/validation already built) to a real call |
| POST | `/api/Auth/login` | `LoginCommand` (email, password) | 200 `{ accessToken }`, 401 on invalid credentials | Refresh token is **not** in the body — see Token Handling |
| POST | `/api/Auth/refresh` | none (relies on the httpOnly cookie) | 200 `{ accessToken }`, 401 if the cookie is missing/expired | Called by the response interceptor, never called directly by page code |
| POST | `/api/Auth/logout` | none | 204 | Server clears/expires the refresh cookie; frontend also clears its own `sessionStorage` access token |
| POST | `/api/Auth/reset-password` | `ResetPasswordCommand` | 204, 400 | Exists on the backend; **out of scope for this task** — no frontend page/flow requested, noted here only so it isn't rediscovered later |

### Token Handling

- **Access token**: stored in `sessionStorage`. Short-lived by design; kept
  alive by the refresh flow rather than a long expiry.
- **Refresh token**: never read, stored, or referenced by any frontend
  code — it exists only as an httpOnly cookie set/rotated by the server on
  `login` and `refresh`. The auth Axios client is configured with
  `withCredentials: true` so the browser attaches that cookie automatically;
  the frontend has no other involvement.
- **Claims extracted client-side** from the decoded access token payload
  (hand-rolled base64url-decode + `JSON.parse` in `utils/jwt.ts` — no new
  dependency needed for this, it's plain JSON once decoded, and the frontend
  never verifies the signature, only reads claims for UI purposes):
  `sub` (userId), `email`, `TenantId` (custom claim), and role(s) under the
  literal key `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role`
  — the .NET `ClaimTypes.Role` URI, since the token is built by passing
  `Claim` objects directly into the `JwtSecurityToken(claims:)` constructor
  rather than through a `ClaimsIdentity`'s outbound claim-type mapping.

### Refresh Trigger

An Axios response interceptor: on a `401`, calls `/api/Auth/refresh` once
and retries the original request with the new access token on success; on
failure, clears `sessionStorage` and (once the `SAFEGUARD` block is
uncommented) redirects to `/login`. Concurrent 401s share a single in-flight
refresh promise so simultaneous requests don't trigger multiple refresh
calls.

### Backend Dependencies / Known Integration Risks

Not fixed by this frontend task, noted so they aren't a surprise later:

- **Cross-origin cookies**: the Vite dev server and `http://localhost:5104`
  are different origins. The httpOnly refresh cookie only round-trips
  correctly if the backend's CORS policy allows credentials from the exact
  frontend origin (not `*`) and the cookie's `SameSite`/`Secure` attributes
  are compatible with local HTTP dev. This is backend configuration.
- `access-request`'s 202/pending state and `login`'s 401 need user-facing
  feedback (toast/inline message) on the existing Request Access and Login
  pages — extends pages already built in the 2026-07-20 spec, no new page
  structure required.

## Role-Based Navigation & Auth Scaffold

- `Role` is a shared enum: `Tenant`, `Staff` (extensible).
- `AuthContext` holds `{ user, role, tenantId, isAuthenticated, login,
  logout, refreshToken }`, backed by the real `authService` calls above.
  `user`/`role`/`tenantId` are derived by decoding the access token, not
  stored separately.
- `usePermissions()` exposes `hasAccess(item: ISidebarNavItem): boolean`,
  evaluated once inside `Sidebar` while mapping the active module's nav
  config.
- `ProtectedRoute` wraps every authenticated route; the
  redirect-if-unauthenticated check is present but commented out behind the
  `SAFEGUARD: AUTHENTICATION` marker (see Resolved Ambiguities #3) so it is
  a single-file toggle.
- Centralizing the check in `Sidebar` + `ProtectedRoute` means adding a role
  or a module later only touches config files, never scattered
  `if (role === ...)` blocks in page components.

## Dashboard Layout

`/app` redirects to `/app/booking` (the only registered module). Booking's
own landing page, **Overview**, serves as the module dashboard: Quick
Actions (New Appointment, Add Client), Today's Calendar summary widget,
Recent Activity feed, key stat cards. No separate empty "Application
Dashboard" shell is built today — that would scaffold a future feature with
no content. The `/app/:moduleSlug/...` route shape plus the module registry
are what let a true cross-module dashboard be added later (new `/app` index
route + `AppDashboardPage`) without touching Booking's Overview.

## Mobile vs. Desktop Behavior

- **Desktop (`lg`+):** sidebar persistent and always visible; slim topbar
  above content shows page title/breadcrumb + user menu.
- **Mobile (`< lg`):** sidebar hidden by default; hamburger in the topbar
  opens the same `Sidebar` content inside a Bootstrap 5 `Offcanvas` — one
  config, one nav-tree component, two containers, no custom CSS needed.
- No bottom tab bar: Bootstrap has no native primitive for it, it doesn't
  scale past ~4 items, and it isn't how enterprise (vs. consumer) SaaS
  products navigate on mobile.

## Folder Organization

```
src/
  api/
    clients/
      authClient.ts              # Axios instance, baseURL from VITE_API_BASE_URL, withCredentials: true
    interceptors/
      authRefreshInterceptor.ts  # 401 → refresh-once-and-retry, shared in-flight promise
  config/
    modules.config.ts            # module registry — Booking only today
    navigation/
      booking.nav.config.ts      # Booking's sidebar items + roles
      settings.nav.config.ts     # Settings sub-nav items
    permissions.config.ts        # Role enum + role→access rules
  contexts/
    AuthContext.tsx              # auth state, backed by real authService calls
  hooks/
    useAuth.ts
    usePermissions.ts
  services/
    authService.ts               # login/refresh/logout/requestAccess — real Axios calls
  utils/
    jwt.ts                       # decodeJwt() — base64url decode + JSON.parse, no new dependency
  layouts/
    DashboardLayout.tsx          # Sidebar + Topbar + <Outlet/>
    SettingsLayout.tsx           # Settings sub-shell
  components/
    layout/
      Sidebar.tsx
      SidebarNavItem.tsx         # recursive, supports optional children
      ModuleSwitcher.tsx
      Topbar.tsx
      MobileNav.tsx              # Offcanvas wrapper around Sidebar content
    booking/                     # existing convention, feature components
  routes/
    AppRoutes.tsx
    ProtectedRoute.tsx           # safeguard lives here
  interfaces/
    ISidebarNavItem.ts           # new — distinct from existing INavItem
    IModule.ts
    IUser.ts
  types/
    Role.ts
```

`ISidebarNavItem` is a **new** interface rather than an extension of the
existing `INavItem` (landing-page anchor links) — different domain,
different shape (icons, roles, children); coupling them would make the
landing page's nav depend on dashboard-only concerns.

## Non-Goals

No reset-password page/flow, even though `AuthController` exposes the
endpoint — not requested. No app-level cross-module dashboard page. No
icon-rail/dual-sidebar chrome. No bottom mobile tab bar. No additional
roles beyond Tenant/Staff. No dark mode. No modules other than Booking are
implemented (Inventory, Customers, CRM, Sales, Purchasing, Reports remain
config-level placeholders only, if referenced at all). No backend/CORS
changes — the cross-origin cookie dependency noted above is flagged, not
fixed, by this task.

## Deliverable Tracking

`PROJECT_TRACKER.md` is updated to add a row for this architecture under the
Booking Module table, following the existing format, once implementation is
complete.
