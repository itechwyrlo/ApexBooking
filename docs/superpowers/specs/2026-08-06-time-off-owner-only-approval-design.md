# Time-Off Approval: Owner-Only Design

## Problem

Time-off approve/reject and team-wide visibility are currently granted to both `Owner` and `Admin` roles. The business rule should be: only the tenant `Owner` can approve or reject time-off requests, and only the `Owner` can see the whole team's requests. `Admin` and `Staff` should both be limited to submitting and viewing their own requests.

## Scope

Backend repo: `ApexBooking`. Frontend repo: `LocalFlow`.

## Changes

### Backend (ApexBooking)

1. **`ApexBooking.WebApi/Controllers/TenantController.cs`**
   - Add `[Authorize(Roles = "Owner")]` to the `ApproveTimeOff` action (currently no per-action override; falls back to the class-level `ManagementOnly` policy, which also admits `Admin` and `platform_admin`).
   - Add `[Authorize(Roles = "Owner")]` to the `RejectTimeOff` action, same reasoning.
   - Update the section comment above the time-off endpoints (currently states "approve/reject stay Owner/Admin only") to reflect Owner-only.

2. **`ApexBooking.Core.Application/Features/TimeOffs/Queries/GetTimeOffRequests/GetTimeOffRequestsHandler.cs`**
   - Change the `isManagement` condition from `currentMember.Role == SystemRole.Owner || currentMember.Role == SystemRole.Admin` to `currentMember.Role == SystemRole.Owner` only, so `Admin` is scoped to `m.TenantMemberId == currentMember.TenantMemberId` like `Staff`.

No changes to `RequestTimeOffHandler`, `ApproveTimeOffHandler`, or `RejectTimeOffHandler` internals — authorization stays at the controller boundary, consistent with the rest of the codebase (no existing handler re-checks role beyond the `[Authorize]` attribute).

### Frontend (LocalFlow)

3. **`src/pages/booking/TimeOffsPage.tsx`**
   - Change `isManagement` (line 20) from `user.roles.includes(Role.Owner) || user.roles.includes(Role.Admin)` to `user.roles.includes(Role.Owner)` only. This single flag drives the page title/description, `showMemberColumn`, and `canReview` props passed into `TimeOffTable`, so Admin automatically gets the same "My Time Off" / own-requests-only experience as Staff.

No changes needed to `TimeOffTable.tsx`, `useTimeOffRequests.ts`, or `timeOffService.ts` — they're already driven by the `isManagement`/`canReview` flags passed in.

## Out of scope

- No ADR — this tightens an existing authorization rule on an existing feature; it isn't a new architectural decision.
- Platform admin (`SuperAdminOnly`) is unaffected; platform admins aren't tenant members and don't carry a `TenantId` claim, so tenant-scoped time-off actions don't apply to them regardless of this change.

## Testing

- Backend: verify `ApproveTimeOff`/`RejectTimeOff` return 403 for an Admin-role JWT and succeed for an Owner-role JWT (existing role-based auth handling, no new test infra needed).
- Backend: verify `GetTimeOffRequestsHandler` returns only the calling Admin's own requests, not the full team's.
- Frontend: manual check — log in as Admin, confirm the time-off page shows "My Time Off" (own requests only, no Approve/Reject actions).
