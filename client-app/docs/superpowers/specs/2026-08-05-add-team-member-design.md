# Add Team Member — Design Spec

Status: Approved (source: user-authored feature prompt, refined through
clarifying questions below)
Scope: Replace the `staff` route's `ModulePlaceholderPage` with a real team
list (read) and an "Add Team Member" create flow, integrated against the
existing `TenantController` actions (`GET /api/Tenant/team`,
`GET /api/Tenant/branches`, `POST /api/Tenant/add-team`). Does **not** cover
editing a member's profile/role, deactivating a member, or resending an
invitation email — no backend action exists for any of those yet.

## Source of Truth

Backend request/response shapes below were read directly from the
`ApexBooking` repo's source (commands, handlers, domain entities, DTOs,
`Program.cs` JSON options, `GlobalExceptionHandler`) — not assumed. See
"API Contract" for exact wire shapes.

## Current State

`src/config/navigation/booking.nav.config.ts` already routes `Staff` to
`/app/booking/staff`, currently rendering `ModulePlaceholderPage` (per
`AppRoutes.tsx`). No `teamService`, `branchService`, or team-related
interfaces/components exist yet. The established reference pattern for a
list+create admin page is `TenantRequestManagementPage.tsx` (Card + table +
Previous/Next pagination + modal), backed by `useTenantRequests.ts` and
`superAdminService.ts` — this feature follows the same shape.

## Resolved Ambiguities (via clarifying questions)

1. **Feature scope** — resolved: list + add only, no edit/deactivate this
   pass, matching how other modules (e.g. Time Offs) were built
   incrementally.
2. **Role picker options** — the invite endpoint technically accepts
   `Owner`/`Admin`/`Staff` (`Tenant.InviteMember` routes `Owner` through a
   dedicated `AssignOwnerRole()` path specifically to bypass
   `TenantMember.AssignRole`'s rejection of `Owner`). Resolved: the Add Team
   Member form only exposes **Admin** and **Staff** — inviting a co-owner is
   an unusual, sensitive action better gated behind a separate flow if ever
   needed.

## API Contract

Base URL: existing `VITE_API_BASE_URL` via `authClient` (bearer token +
`withCredentials`, already wired).

| Method | Path | Query/Body | Response |
|---|---|---|---|
| GET | `/api/Tenant/team` | `pageNumber`, `pageSize` (query) | `QueryResult<TeamMemberSummary>` → `{ data: TeamMemberSummary[], total: number }` |
| GET | `/api/Tenant/branches` | none | `BranchAdminSummary[]` (no paging envelope) |
| POST | `/api/Tenant/add-team` | `{ request: AddTeamMemberRequest }` (see below — **nested**, not flat) | `201`, empty body |

`TeamMemberSummary` (wire — camelCase, enums as strings; the global
`AddJsonOptions` in `Program.cs` registers `JsonStringEnumConverter`):
```json
{
  "tenantMemberId": "guid",
  "userId": "guid | null",
  "email": "string",
  "fullName": "string",
  "contactNumber": "string",
  "photoUrl": "string | null",
  "role": "Owner | Admin | Staff",
  "customJobTitle": "string | null",
  "status": "Invited | Active | Deactivated",
  "createdAt": "ISO 8601 string"
}
```

`BranchAdminSummary` (wire):
```json
{
  "branchId": "guid",
  "branchName": "string",
  "street": "string",
  "barangay": "string | null",
  "city": "string",
  "province": "string",
  "zipCode": "string",
  "timeZoneId": "string",
  "isActive": "boolean"
}
```

`AddTeamMemberRequest` (request body, nested under `request` because
`AddTeamCommand` is `record AddTeamCommand(AddTeamMemberRequest request)`):
```json
{
  "branchId": "guid",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "contactNumber": "string | null",
  "role": "Admin | Staff",
  "customJobTitle": "string"
}
```

**Validation reality (confirmed from `TenantMember.Invite` /
`Tenant.InviteMember` domain code):** only `lastName` and `email` are
backend-enforced (`BusinessRuleBrokenException` → 400 if missing). A
duplicate email within the same tenant also throws 400. Nothing else is
validated server-side — the frontend form enforces its own required fields
(branch, first name, last name, email, role) since the backend won't catch
a missing first name or a missing branch.

**Side effects (informational, not built by this frontend task):**
`AddTeam` creates an `ApplicationUser` with a random temp password (never
returned to the caller), activates the `TenantMember` immediately (so
`status: "Invited"` may never actually appear via this endpoint in
practice), and fires an async email inviting the member to set their own
password. Email failures are logged and swallowed server-side — `201` is
still returned.

**Error shape** (`GlobalExceptionHandler`, all failures):
```json
{ "status": 400, "title": "Business Rule Violation", "detail": "A team member with the email '...' already exists in this business.", "errors": null }
```
The frontend surfaces `error.response?.data?.detail` in the failure toast
when present, falling back to a generic message otherwise.

## Known Backend Issue (flagged, not fixed here)

`AddTeamHandler`/`GetAllTeamHandler` fetch the tenant via
`TenantRepository.GetAsync(predicate: t => true, ...)` — an unfiltered
"grab the only tenant row" lookup with no caller-tenant scoping. This works
today because only one tenant exists, but is not real multi-tenant
isolation. Out of scope for this frontend task; noted so it isn't
rediscovered blind later.

## Frontend Changes

```
src/
  interfaces/
    ITeamMember.ts          # new — UI shape, mapped from TeamMemberSummary
    IBranch.ts               # new — UI shape, mapped from BranchAdminSummary
    IAddTeamMemberValues.ts  # new — Add Team Member form values
  types/
    TenantMemberStatus.ts    # new — 'Invited' | 'Active' | 'Deactivated', mirrors TenantRequestStatus.ts
  services/
    teamService.ts           # new — getTeamMembers(params), addTeamMember(values); wire→UI mapping, same convention as superAdminService.ts
    branchService.ts         # new — getBranches(); thin wrapper, reusable later by Business Profile
  hooks/
    useTeamMembers.ts        # new — mirrors useTenantRequests.ts (loading/error/refetch)
  components/
    team/
      TeamMemberTable.tsx     # new — mirrors TenantRequestTable.tsx
      AddTeamMemberModal.tsx  # new — built on shared Modal + FormGroup
  pages/
    booking/
      StaffPage.tsx           # new — replaces ModulePlaceholderPage on the staff route
  routes/
    AppRoutes.tsx              # edit — staff route renders StaffPage instead of ModulePlaceholderPage
```

Role reuses the existing `Role` type/enum (`src/types/Role.ts`) already
shared by nav/permissions — no new role type is introduced. The role
`<select>` on the Add Team Member form only renders `Role.Admin` and
`Role.Staff` options.

### `StaffPage.tsx`

Same shape as `TenantRequestManagementPage.tsx`: `PageHeader` (title +
"Add Team Member" primary button), `Card` wrapping `TeamMemberTable`,
Previous/Next pagination (`PAGE_SIZE = 10`, same pattern as tenant
requests), and `AddTeamMemberModal` mounted at the page level, opened by
the header button.

### `TeamMemberTable.tsx`

Columns: Name + Email, Role (badge), Status (badge), Contact Number, Job
Title. Accepts `isLoading` and renders the existing `TableSkeleton`
(established pattern; already unused elsewhere the same way admin tables
handle loading).

### `AddTeamMemberModal.tsx`

Fields: Branch (`<select>`, populated from `useBranches`/`getBranches()`,
required), First Name (required), Last Name (required), Email (required),
Contact Number (optional), Role (`<select>`: Admin | Staff, required),
Job Title (optional, maps to `customJobTitle`). Client-side required-field
validation via the existing `FormGroup` error-display convention. On
submit: `addTeamMember()` → success closes the modal, shows a success
toast, and calls the page's `refetch()`; failure keeps the modal open and
shows an error toast using the backend's `detail` message when present.

## Non-Goals

No edit-member or deactivate-member UI (no backend action exists yet). No
resend-invitation action. No password field anywhere (the backend issues
and emails a temp-password reset link itself). No Owner option in the role
picker. No fix to the backend's unfiltered single-tenant lookup. No new
navigation entries — the existing `Staff` sidebar item is reused as-is.

## Deliverable Tracking

`PROJECT_TRACKER.md`'s Booking Module table gets a new "Staff / Add Team
Member" row following the existing format once implementation is complete.
