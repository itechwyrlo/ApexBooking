# Staff Dashboard — Phase 3: Block My Time

## Context

Fourth sub-project in the role-based dashboards rework, last of three Staff Dashboard phases:

1. [Foundation](2026-08-12-role-based-dashboards-foundation-design.md) — done.
2. Staff Dashboard Phase 1 ([design](2026-08-12-staff-dashboard-phase1-lineup-design.md)) — done: TenantMemberId claim + My Daily Lineup Timeline.
3. Staff Dashboard Phase 2 ([design](2026-08-12-staff-dashboard-phase2-chair-notes-design.md)) — done: Chair Notes.
4. **Staff Dashboard Phase 3** (this spec) — Block My Time.

Once this lands, the Staff Dashboard is fully built out per the original spec, and work moves to the Admin and Owner dashboards.

## Problem

The Staff Dashboard's "Block My Time" Quick Tools button has been a disabled placeholder since the Foundation sub-project. Per the original spec: "Instantly marks their personal calendar slot as 'Unavailable' for lunch or personal breaks" — a same-day, no-wait self-block.

The existing time-off system (`StaffTimeOffRequest`, `TenantMember.RequestTimeOff`/`ApproveTimeOff`/`RejectTimeOff`) already models "unavailable" in a way the scheduling engine respects (`TenantMember.IsAvailableAt`/`HasApprovedTimeOff` check `Status == Approved` + date range, indifferent to *how* that status was reached) — but normal requests start at `Requested` and need `[Authorize(Roles = "Owner")]` approval. Block My Time needs the same underlying data shape, minus the wait.

## Scope decision: always today, no date picker

"Instantly... for lunch or personal breaks" is inherently a same-day action. Block My Time doesn't ask for a date — only a start time and end time, always applied to today. This also keeps the UI meaningfully lighter than `RequestTimeOffModal` (no Full/Partial-day radio, no date-range inputs) — it's a distinct, smaller tool, not a variant of the formal request form.

## Backend

- New command `BlockMyTimeCommand(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, string? Reason) : ICommand<Guid>` — self-service only, following `RequestTimeOffCommand`'s exact precedent (no target-member parameter; resolved server-side from the authenticated caller, never client-supplied).
- New handler `BlockMyTimeCommandHandler` — replicates `RequestTimeOffHandler`'s tenant/member resolution (load `Tenant` via `ITenantEntity.TenantId`, find `tenant.Members.FirstOrDefault(m => m.UserId == currentUserId && m.IsActive)`), then:
  1. `var request = currentMember.RequestTimeOff(TimeOffType.PartialDay, command.Date, command.Date, command.StartTime, command.EndTime, command.Reason);`
  2. `currentMember.ApproveTimeOff(request.Id.Value);` — immediately, in the same handler, same transaction. Both calls are legal from the Application layer because they're both `public` methods on `TenantMember` (unlike `StaffTimeOffRequest.Approve()`, which is `internal` and only reachable from within the Domain assembly — this is exactly why the handler goes through `TenantMember`, mirroring how `ApproveTimeOffHandler` already does it for the normal Owner-approval path).
  3. `_unitOfWork.TenantRepository.Update(tenant)`, `CompleteAsync`, return `request.Id.Value`.
- New route `POST api/Tenant/team/time-off/block`, `[Authorize(Roles = "Owner,Admin,Staff")]` (same override-the-class-default pattern already used for `team/time-off` GET/POST), returns `{ id }` (201).
- No scheduling-engine changes — `TenantMember.IsAvailableAt`/`HasApprovedTimeOff` already only check `Status == Approved`, so an instantly-approved block is respected identically to an Owner-approved one.

## Frontend

- New `blockMyTime(values: { date: string; startTime: string; endTime: string; reason: string }): Promise<string>` in `timeOffService.ts`, POSTing to the new route (mirrors `requestTimeOff`'s body-shaping: `${startTime}:00` / `${endTime}:00` for `TimeOnly` binding).
- New `BlockMyTimeModal.tsx` — start time / end time (`TimeSelect`, same component `RequestTimeOffModal` uses), optional reason textarea. No type radio, no date inputs (date is always today, computed the same way `StaffDashboardPage.tsx` already computes it). Validates `startTime < endTime` (mirrors `RequestTimeOffModal`'s `validate()`).
- `StaffDashboardPage.tsx`: "Block My Time" Quick Tools button becomes enabled, opens `BlockMyTimeModal`; on success, a toast confirms it.

## Out of scope (deferred)

- Any interaction with the existing `TimeOffsPage`/team time-off list (a Block My Time entry will show up there naturally, already `Approved`, since it's the same underlying data — no special-casing needed or planned).
- Editing/cancelling a block once created.

## Testing

No test runner configured in either repo. Verification is manual: as Staff, use "Block My Time" for a short window later today, confirm the toast, then confirm that window is unavailable when booking/rescheduling against that staff member (e.g. via the public booking wizard's availability check or the walk-in staff-availability picker), and confirm it appears as an already-`Approved` entry on the Time Offs page.
