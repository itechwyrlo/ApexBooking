# Admin Dashboard Phase 1 — Backend (Counters + Idle Staff) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Two new read-only queries for the Admin Dashboard: today's booking counts by operational bucket, and the list of active staff with no service assigned ("Idle Staff" — see the design doc for why this replaces the originally-worded "Unassigned Bookings Alert"). Companion frontend plan: `docs/superpowers/plans/2026-08-13-admin-dashboard-phase1-frontend.md` in the LocalFlow repo.

**Architecture:** Both queries follow the established `ITenantRepository` pattern already used for `GetBookingsPageAsync`/`GetLatestStaffNoteAsync` — no new aggregate-loading, just targeted EF Core queries against the relevant `DbSet`. Booking counts mirror `PlatformQueries.GetDashboardCountsAsync`'s style (a flat sequence of predicate-scoped `CountAsync` calls, not `GroupBy`). Idle staff is a single `Where` over `context.Staffs` (the `DbSet<TenantMember>`) checking `!m.MemberServices.Any()` — no aggregate load needed since `TenantMember.MemberServices` is a directly-navigable collection.

**Tech Stack:** .NET / C#, MediatR, EF Core.

## Global Constraints

- No test project exists in this solution. Verification per task is `dotnet build`, run manually by the user.
- No schema changes, no migration needed — both queries read existing columns/relationships only.
- Both new routes use the `TenantController` class-level default `ManagementOnly` policy (no `[Authorize]` override needed) — Staff doesn't need either endpoint.

---

### Task 1: Repository methods

**Files:**
- Modify: `ApexBooking.Core.Domain\Repositories\ITenantRepository.cs`
- Modify: `ApexBooking.Core.Persistence\Repositories\TenantRepository.cs`

**Interfaces:**
- Produces: `ITenantRepository.GetBookingCountsAsync(TenantId, DateOnly, CancellationToken) => Task<TenantBookingCountsRow>`, `ITenantRepository.GetIdleStaffAsync(TenantId, CancellationToken) => Task<IReadOnlyCollection<IdleStaffRow>>` — both consumed by Task 2.

- [ ] **Step 1: Add the row types and interface methods**

In `ITenantRepository.cs`, add the two interface methods right after `GetLatestStaffNoteAsync` (currently lines 56-62), before `StaffHasBookingsAsync`:

```csharp
    // Powers the Admin Dashboard's Daily Booking Counters — four operational buckets derived
    // from existing Status/CheckedInAt fields, not a new concept. Mirrors PlatformQueries'
    // flat-CountAsync-per-metric style rather than a single GroupBy.
    Task<TenantBookingCountsRow> GetBookingCountsAsync(
        TenantId tenantId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    // Powers the Admin Dashboard's Idle Staff alert — active team members assigned to zero
    // services, and therefore invisible to the public booking wizard (which only lists staff
    // already qualified for a service). A flat query against context.Staffs, not a full Tenant
    // aggregate load — TenantMember.MemberServices is directly navigable.
    Task<IReadOnlyCollection<IdleStaffRow>> GetIdleStaffAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
```

Add the two row records right after `CustomerLatestNoteRow` (currently line 112, the last line of the file):

```csharp
public record CustomerLatestNoteRow(string Notes, DateOnly NotedOn);

public record TenantBookingCountsRow(int Pending, int CheckedIn, int Completed, int Missed);

public record IdleStaffRow(Guid TenantMemberId, string Name, string? PhotoUrl);
```

- [ ] **Step 2: Implement both methods**

In `TenantRepository.cs`, add right after `GetLatestStaffNoteAsync` (currently ends at line 189), before `StaffHasBookingsAsync`:

```csharp
    public async Task<TenantBookingCountsRow> GetBookingCountsAsync(
        TenantId tenantId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var todaysBookings = context.Bookings.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.ScheduledDate == date);

        var pending = await todaysBookings.CountAsync(b => b.Status == BookingStatus.Scheduled && b.CheckedInAt == null, cancellationToken);
        var checkedIn = await todaysBookings.CountAsync(b => b.Status == BookingStatus.Scheduled && b.CheckedInAt != null, cancellationToken);
        var completed = await todaysBookings.CountAsync(b => b.Status == BookingStatus.Completed, cancellationToken);
        var missed = await todaysBookings.CountAsync(b => b.Status == BookingStatus.NoShow, cancellationToken);

        return new TenantBookingCountsRow(pending, checkedIn, completed, missed);
    }

    public async Task<IReadOnlyCollection<IdleStaffRow>> GetIdleStaffAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.Staffs.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.Status == TenantMemberStatus.Active && !m.MemberServices.Any())
            .Select(m => new IdleStaffRow(m.TenantMemberId.Value, m.FirstName + " " + m.LastName, m.PhotoUrl))
            .ToListAsync(cancellationToken);
    }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 2: Queries, handlers, DTOs

**Files:**
- Create: `ApexBooking.Core.Application\Dtos\Response\TenantBookingCountsDto.cs`
- Create: `ApexBooking.Core.Application\Dtos\Response\IdleStaffDto.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Queries\GetTenantBookingCounts\GetTenantBookingCountsQuery.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Queries\GetTenantBookingCounts\GetTenantBookingCountsHandler.cs`
- Create: `ApexBooking.Core.Application\Features\Staff\Queries\GetIdleStaff\GetIdleStaffQuery.cs`
- Create: `ApexBooking.Core.Application\Features\Staff\Queries\GetIdleStaff\GetIdleStaffHandler.cs`

**Interfaces:**
- Consumes: `ITenantRepository.GetBookingCountsAsync`/`GetIdleStaffAsync` from Task 1.
- Produces: `GetTenantBookingCountsQuery(DateOnly Date) : IQuery<TenantBookingCountsDto>`, `GetIdleStaffQuery : IQuery<IReadOnlyCollection<IdleStaffDto>>` — consumed by Task 3's controller routes.

- [ ] **Step 1: DTOs**

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record TenantBookingCountsDto(int Pending, int CheckedIn, int Completed, int Missed);
}
```

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record IdleStaffDto(Guid TenantMemberId, string Name, string? PhotoUrl);
}
```

- [ ] **Step 2: GetTenantBookingCounts query + handler**

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookingCounts
{
    public record GetTenantBookingCountsQuery(DateOnly Date) : IQuery<TenantBookingCountsDto>;
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookingCounts
{
    public class GetTenantBookingCountsHandler : IQueryHandler<GetTenantBookingCountsQuery, TenantBookingCountsDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetTenantBookingCountsHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<TenantBookingCountsDto> Handle(GetTenantBookingCountsQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load booking counts. No authenticated tenant context was found.");

            var counts = await _unitOfWork.TenantRepository.GetBookingCountsAsync(tenantId, query.Date, cancellationToken);

            return new TenantBookingCountsDto(counts.Pending, counts.CheckedIn, counts.Completed, counts.Missed);
        }
    }
}
```

- [ ] **Step 3: GetIdleStaff query + handler**

```csharp
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staff.Queries.GetIdleStaff
{
    public record GetIdleStaffQuery : IQuery<IReadOnlyCollection<IdleStaffDto>>;
}
```

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Staff.Queries.GetIdleStaff
{
    public class GetIdleStaffHandler : IQueryHandler<GetIdleStaffQuery, IReadOnlyCollection<IdleStaffDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetIdleStaffHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<IdleStaffDto>> Handle(GetIdleStaffQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load idle staff. No authenticated tenant context was found.");

            var rows = await _unitOfWork.TenantRepository.GetIdleStaffAsync(tenantId, cancellationToken);

            return rows.Select(r => new IdleStaffDto(r.TenantMemberId, r.Name, r.PhotoUrl)).ToList();
        }
    }
}
```

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 3: Controller routes

**Files:**
- Modify: `ApexBooking.WebApi\Controllers\TenantController.cs`

**Interfaces:**
- Consumes: `GetTenantBookingCountsQuery`, `GetIdleStaffQuery` from Task 2.
- Produces: `GET api/Tenant/bookings/counts?date=yyyy-MM-dd` → `TenantBookingCountsDto`; `GET api/Tenant/team/idle` → `IReadOnlyCollection<IdleStaffDto>`.

- [ ] **Step 1: Add usings**

Add alongside the existing feature usings near the top of the file:

```csharp
using ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookingCounts;
using ApexBooking.Core.Application.Features.Staff.Queries.GetIdleStaff;
```

- [ ] **Step 2: Add the booking-counts route**

Add right after the existing `GetBookings` action (the `[HttpGet("bookings")]` one), before `GetWalkInAvailableStaff`:

```csharp
        [HttpGet("bookings/counts")]
        [ProducesResponseType(typeof(TenantBookingCountsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBookingCounts([FromQuery] DateOnly date)
        {
            var result = await _mediator.Send(new GetTenantBookingCountsQuery(date));
            return Ok(result);
        }
```

- [ ] **Step 3: Add the idle-staff route**

Add right after the existing `GetTeam` action (the `[HttpGet("team")]` one), before `UpdateTeamMember`:

```csharp
        [HttpGet("team/idle")]
        [ProducesResponseType(typeof(IReadOnlyCollection<IdleStaffDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIdleStaff()
        {
            var result = await _mediator.Send(new GetIdleStaffQuery());
            return Ok(result);
        }
```

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: booking-counts buckets exactly match the design doc's table (Pending/Checked-In/Completed/Missed derived from `Status`/`CheckedInAt`); idle staff exactly matches the redefined scope (active + zero `MemberServices`). Both routes use the class-level default policy per the design doc's authorization note.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `TenantBookingCountsRow`/`TenantBookingCountsDto` and `IdleStaffRow`/`IdleStaffDto` field names match 1:1 between repository and application layers; the companion frontend plan's `ITenantBookingCounts`/`IIdleStaffMember` interfaces mirror these exactly (camelCase JSON naming policy, no mapper needed on the frontend).
