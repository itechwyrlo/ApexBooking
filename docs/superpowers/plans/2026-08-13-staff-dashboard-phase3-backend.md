# Staff Dashboard Phase 3 — Backend (Block My Time) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a staff member instantly mark a short window of today as unavailable (a break), auto-approved with no Owner review, reusing the existing `StaffTimeOffRequest`/`TenantMember` time-off machinery. Companion frontend plan: `docs/superpowers/plans/2026-08-13-staff-dashboard-phase3-frontend.md` in the LocalFlow repo.

**Architecture:** A new `BlockMyTimeCommand`/handler follows `RequestTimeOffCommand`/`RequestTimeOffHandler`'s exact self-service resolution pattern (caller's own `TenantMember`, never a client-supplied target), but the handler calls both `TenantMember.RequestTimeOff(...)` *and* `TenantMember.ApproveTimeOff(...)` in the same transaction — landing the request as `Approved` immediately instead of `Requested`. No changes anywhere else: the scheduling engine (`TenantMember.IsAvailableAt`/`HasApprovedTimeOff`) already only checks `Status == Approved`, indifferent to how that status was reached.

**Tech Stack:** .NET / C#, MediatR.

## Global Constraints

- No test project exists in this solution. Verification per task is `dotnet build`, run manually by the user — do not run it yourself.
- The new route broadens past the `TenantController` class-level `ManagementOnly` policy to `[Authorize(Roles = "Owner,Admin,Staff")]`, matching the existing override already in place for `team/time-off` GET/POST (self-service actions available to every role).
- `TimeOffType.PartialDay` is hardcoded in the handler — this command has no "full day" option, unlike `RequestTimeOffCommand`.

---

### Task 1: BlockMyTimeCommand + handler

**Files:**
- Create: `ApexBooking.Core.Application\Features\TimeOffs\Commands\BlockMyTime\BlockMyTimeCommand.cs`
- Create: `ApexBooking.Core.Application\Features\TimeOffs\Commands\BlockMyTime\BlockMyTimeCommandHandler.cs`

**Interfaces:**
- Consumes: `TenantMember.RequestTimeOff(TimeOffType, DateOnly, DateOnly, TimeOnly?, TimeOnly?, string?)` and `TenantMember.ApproveTimeOff(StaffTimeOffRequestId)` (both existing, `ApexBooking.Core.Domain.Entities.TenantMember.cs:172-178` and `:180-186`).
- Produces: `POST`-able `BlockMyTimeCommand` returning `Guid` — consumed by Task 2's controller route.

- [ ] **Step 1: Write the command**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.TimeOffs.Commands.BlockMyTime
{
    // Self-service only, same as RequestTimeOffCommand — always for the caller's own TenantMember,
    // resolved server-side, never client-supplied. Always today, always a partial-day window (this
    // is a short break, not a leave request), and lands pre-approved — see the handler.
    public record BlockMyTimeCommand(
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string? Reason
    ) : ICommand<Guid>;
}
```

- [ ] **Step 2: Write the handler**

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.TimeOffs.Commands.BlockMyTime
{
    public class BlockMyTimeCommandHandler : ICommandHandler<BlockMyTimeCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public BlockMyTimeCommandHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<Guid> Handle(BlockMyTimeCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to block your time. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to block your time. Isolated tenant context could not be verified.");

            var currentUserId = _userContext.GetCurrentUserId();
            var currentMember = tenant.Members.FirstOrDefault(m => m.UserId == currentUserId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("Your account is not linked to an active staff account.");

            var request = currentMember.RequestTimeOff(
                TimeOffType.PartialDay,
                command.Date,
                command.Date,
                command.StartTime,
                command.EndTime,
                command.Reason);

            // Instant self-approval — the only difference from the normal RequestTimeOff flow.
            currentMember.ApproveTimeOff(request.Id.Value);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return request.Id.Value;
        }
    }
}
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 2: Controller route

**Files:**
- Modify: `ApexBooking.WebApi\Controllers\TenantController.cs`

**Interfaces:**
- Consumes: `BlockMyTimeCommand` from Task 1.
- Produces: `POST api/Tenant/team/time-off/block` → `{ id: Guid }` (201).

- [ ] **Step 1: Add the using**

Add alongside the existing TimeOffs feature usings (currently lines 53-56):

```csharp
using ApexBooking.Core.Application.Features.TimeOffs.Commands.BlockMyTime;
```

- [ ] **Step 2: Add the route**

Add right after the existing `RequestTimeOff` action (currently the `[HttpPost("team/time-off")]` action, immediately before the Owner-only `approve`/`reject` routes):

```csharp
        [HttpPost("team/time-off/block")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)] // Returns { id: Guid }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BlockMyTime([FromBody] BlockMyTimeCommand command)
        {
            var requestId = await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, new { id = requestId });
        }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: the command, its self-approval step, and the route are all covered per the design doc's "always today, no date picker" decision (`Date` is a required, single field — no start/end date range).
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `BlockMyTimeCommand`'s field names/types (`Date: DateOnly`, `StartTime`/`EndTime: TimeOnly`, `Reason: string?`) match what the companion frontend plan's `blockMyTime()` service function sends.
