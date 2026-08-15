# Staff Dashboard — Phase 1: TenantMemberId Claim + My Daily Lineup Timeline

## Context

This is the second sub-project in the role-based dashboards rework (after [Foundation](2026-08-12-role-based-dashboards-foundation-design.md), which split the shared dashboard into per-role skeleton pages). It's the first of three phases building out the real Staff Dashboard:

1. **Phase 1** (this spec) — TenantMemberId claim infrastructure + My Daily Lineup Timeline.
2. Phase 2 — Chair Notes (Save Chair Notes tool + Client Preference View).
3. Phase 3 — Block My Time.

These three phases don't depend on each other except that all three need Phase 1's `TenantMemberId` claim to know which staff member the logged-in user is.

## Problem

`StaffDashboardPage.tsx` currently shows a placeholder `EmptyState` for "My Daily Lineup." The real version needs to fetch only the bookings assigned to the logged-in staff user. The booking-list query already supports filtering by `staffId`, but that filter expects a `TenantMemberId` (the membership/join-entity id), not the login's raw `UserId`. The JWT the frontend holds only carries `UserId` — there's no existing way (claim or endpoint) for the client to learn its own `TenantMemberId`.

## Scope

### Backend: TenantMemberId JWT claim

Add a `tenant_member_id` claim to tenant-session access tokens, mirroring the existing optional-claim pattern already used for `tenant_id`/`tenant_role`/`tenant_slug` (all four are `null` for platform-admin sessions, populated for tenant sessions).

- `ApexBooking.Core.Persistence\CustomClaimTypes\JwtClaimTypes.cs` — add `public const string TenantMemberId = "tenant_member_id";`
- `ApexBooking.Core.Domain\Services\Auth\ITokenService.cs` — add `TenantMemberId? TenantMemberId = null` to the `TokenDescriptor` record, and the same to `TokenPrincipal`.
- `ApexBooking.Core.Persistence\Services\JwtTokenService.cs`:
  - `BuildClaims` — add the claim when `descriptor.TenantMemberId is not null`, same `if` pattern as `TenantId`.
  - `MapPrincipal` — parse it back the same way `TenantId` is parsed, for symmetry (not strictly required by the refresh flow, which re-resolves membership fresh from the DB, but keeps the two directions consistent).
- `ApexBooking.Core.Application\Features\Authentication\Login\TenantLoginHandler.cs` — pass `TenantMemberId: membership.TenantMemberId` (the `membership` lookup already happens here to resolve `Role`).
- `ApexBooking.Core.Application\Features\Authentication\RefreshToken\RefreshTokenHandler.cs` — same: resolve `membership.TenantMemberId` alongside the existing `role`/`slug` resolution, pass through.
- `ApexBooking.Core.Application\Features\Authentication\ResetPassword\ResetPasswordHandler.cs` + `IApplicationUserService.cs` + `ApplicationUserService.cs` — thread an additional `tenantMemberId` parameter through the same path `role` already takes, so a freshly-reset session also carries the claim immediately rather than waiting for the next refresh.
- `PlatformAdminLoginHandler.cs` — untouched; platform admins have no tenant membership, so the claim stays absent, same as `TenantId`/`Role` today.

Two dead-code items noticed during research, not touched by this change: an orphaned duplicate `TokenDescriptor` record in `ApexBooking.Core.Application\Dtos\Descriptor\TokenDescriptor.cs` (unused anywhere), and the unused legacy `UserRole` enum in `ApexBooking.Core.Persistence\Identity\Enums\UserRole.cs`.

### Frontend: decode the claim

- `src/utils/jwt.ts` — add `TENANT_MEMBER_ID_CLAIM_KEY = 'tenant_member_id'`, add `tenantMemberId: string | null` to `IDecodedAccessToken`, decode it the same way `tenantId`/`slug` are decoded.
- `src/interfaces/IUser.ts` — add `tenantMemberId: string | null`.
- `src/contexts/AuthContext.tsx` — `buildUserFromToken` maps the new field through.

### Frontend: My Daily Lineup Timeline

- New component `src/components/dashboard/StaffLineupTimeline.tsx` — a vertical, chronological list of bookings. Props: `bookings: ITenantBooking[]`, `isLoading: boolean`. Uses the existing `EmptyState` when there are no bookings, existing `TableSkeleton`-equivalent loading treatment (or a simple loading state, since this isn't a table), and per-booking rows showing scheduled time (`formatDisplayTime`), service name, customer name, and `BookingStatusBadge` (all existing, reused as-is from `BookingTable.tsx`'s pattern). Read-only — no admit/complete/cancel actions here; those already live on Appointments/Calendar and this widget's job (per the original spec) is a personal at-a-glance view, not another place to perform booking actions.
- `StaffDashboardPage.tsx` — replace the "My Daily Lineup" `EmptyState` placeholder with real data: `useTenantBookings({ staffId: user?.tenantMemberId ?? undefined, fromDate: today, toDate: today }, {})`, where `today` is computed with the same small local `getTodayIsoDate()` helper already duplicated once in `AppointmentsPage.tsx` (a second, small, local duplicate — not worth extracting into a shared util for two call sites).

## Out of scope (deferred)

- Client Preference View and Save Chair Notes (Phase 2).
- Block My Time (Phase 3).
- Any write/action affordance on the lineup itself (admit/complete/cancel) — view-only for this phase.

## Testing

No test runner is configured in LocalFlow (see Foundation spec). Verification is manual: log in as a Staff test account, confirm the JWT (inspect via browser devtools or a debug log) carries `tenant_member_id`, and confirm `/:slug/dashboard` shows that staff member's actual bookings for today in chronological order, or the empty state if none.
