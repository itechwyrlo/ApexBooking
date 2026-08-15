# Owner Dashboard Phase 3 — Backend (Staff Performance List) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A new query ranking every active staff member by today's completed services and net revenue generated. Companion frontend plan: `docs/superpowers/plans/2026-08-14-owner-dashboard-phase3-frontend.md` in the LocalFlow repo. This is the last backend plan in the entire role-based dashboards rework.

**Architecture:** A single query starting from `context.Staffs` (active members only), using the already-EF-mapped `TenantMember.Appointments` navigation (`TenantMemberConfiguration.cs: builder.HasMany(tm => tm.Appointments)`) as correlated subqueries for the count and the netted revenue sum — the same "use an already-mapped reverse navigation directly in LINQ" shape `GetIdleStaffAsync` already established with `MemberServices`. No manual join needed, and every active staff member appears even with zero completed bookings today.

**Tech Stack:** .NET / C#, MediatR, EF Core.

## Global Constraints

- No test project exists in this solution. Verification per task is `dotnet build`, run manually by the user.
- No schema/migration needed — reads existing `TenantMember`/`Booking` data only.
- The new route is Owner-only (`[Authorize(Roles = "Owner")]`), matching Total Shop Revenue and Refund Log — financial data.

---

### Task 1: GetStaffPerformanceAsync repository method

**Files:**
- Modify: `ApexBooking.Core.Domain\Repositories\ITenantRepository.cs`
- Modify: `ApexBooking.Core.Persistence\Repositories\TenantRepository.cs`

**Interfaces:**
- Produces: `ITenantRepository.GetStaffPerformanceAsync(TenantId, DateOnly, CancellationToken) => Task<IReadOnlyCollection<StaffPerformanceRow>>` — consumed by Task 2.

- [ ] **Step 1: Add the row type and interface method**

In `ITenantRepository.cs`, add the interface method right after `GetRevenueAsync` (currently lines 86-92), inside the interface block, before the closing `}`:

```csharp
    // Powers the Owner Dashboard's Staff Performance List — every active staff member, ranked
    // by today's completed services and net revenue generated (zero for anyone with none, so a
    // solo-operator tenant just sees their own single row with no special-casing needed).
    Task<IReadOnlyCollection<StaffPerformanceRow>> GetStaffPerformanceAsync(
        TenantId tenantId,
        DateOnly date,
        CancellationToken cancellationToken = default);
```

Add the row record right after `TenantRevenueRow` (currently the last line of the file):

```csharp
public record TenantRevenueRow(decimal OnlineAmount, decimal PayInVisitAmount, string CurrencyCode);

public record StaffPerformanceRow(Guid TenantMemberId, string Name, int ServicesCompleted, decimal RevenueGenerated, string CurrencyCode);
```

- [ ] **Step 2: Implement it**

In `TenantRepository.cs`, add right after `GetRevenueAsync` (before `StaffHasBookingsAsync`):

```csharp
    public async Task<IReadOnlyCollection<StaffPerformanceRow>> GetStaffPerformanceAsync(
        TenantId tenantId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await context.Staffs.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.Status == TenantMemberStatus.Active)
            .Select(m => new StaffPerformanceRow(
                m.TenantMemberId.Value,
                m.FirstName + " " + m.LastName,
                m.Appointments.Count(b => b.ScheduledDate == date && b.Status == BookingStatus.Completed),
                m.Appointments
                    .Where(b => b.ScheduledDate == date && b.Status == BookingStatus.Completed)
                    .Sum(b => b.AmountDue - (b.RefundStatus == RefundStatus.Succeeded ? (b.RefundedAmount ?? 0) : 0)),
                "PHP"))
            .ToListAsync(cancellationToken);
    }
```

(`CurrencyCode` is hardcoded to `"PHP"` here rather than derived per-row — unlike `GetRevenueAsync`, which reads it off a booking that's guaranteed to exist if the sum is nonzero, a staff member with zero completed bookings has no booking row to read a currency from. Matches the same `"PHP"` fallback `GetRevenueAsync` already uses when there's nothing to derive it from.)

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 2: GetStaffPerformanceQuery + handler

**Files:**
- Create: `ApexBooking.Core.Application\Dtos\Response\StaffPerformanceEntryDto.cs`
- Create: `ApexBooking.Core.Application\Features\Staff\Queries\GetStaffPerformance\GetStaffPerformanceQuery.cs`
- Create: `ApexBooking.Core.Application\Features\Staff\Queries\GetStaffPerformance\GetStaffPerformanceHandler.cs`

**Interfaces:**
- Consumes: `ITenantRepository.GetStaffPerformanceAsync` from Task 1.
- Produces: `GetStaffPerformanceQuery(DateOnly Date) : IQuery<IReadOnlyCollection<StaffPerformanceEntryDto>>` — consumed by Task 3.

- [ ] **Step 1: DTO**

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record StaffPerformanceEntryDto(Guid TenantMemberId, string Name, int ServicesCompleted, decimal RevenueGenerated, string CurrencyCode);
}
```

- [ ] **Step 2: Query**

```csharp
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staff.Queries.GetStaffPerformance
{
    public record GetStaffPerformanceQuery(DateOnly Date) : IQuery<IReadOnlyCollection<StaffPerformanceEntryDto>>;
}
```

- [ ] **Step 3: Handler**

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

namespace ApexBooking.Core.Application.Features.Staff.Queries.GetStaffPerformance
{
    public class GetStaffPerformanceHandler : IQueryHandler<GetStaffPerformanceQuery, IReadOnlyCollection<StaffPerformanceEntryDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetStaffPerformanceHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<StaffPerformanceEntryDto>> Handle(GetStaffPerformanceQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load staff performance. No authenticated tenant context was found.");

            var rows = await _unitOfWork.TenantRepository.GetStaffPerformanceAsync(tenantId, query.Date, cancellationToken);

            return rows
                .OrderByDescending(r => r.RevenueGenerated)
                .Select(r => new StaffPerformanceEntryDto(r.TenantMemberId, r.Name, r.ServicesCompleted, r.RevenueGenerated, r.CurrencyCode))
                .ToList();
        }
    }
}
```

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 3: Controller route

**Files:**
- Modify: `ApexBooking.WebApi\Controllers\TenantController.cs`

**Interfaces:**
- Consumes: `GetStaffPerformanceQuery` from Task 2.
- Produces: `GET api/Tenant/team/performance?date=yyyy-MM-dd` → `IReadOnlyCollection<StaffPerformanceEntryDto>`.

- [ ] **Step 1: Add the using**

Add alongside the existing `Staff.Queries` usings (next to `GetIdleStaff`):

```csharp
using ApexBooking.Core.Application.Features.Staff.Queries.GetStaffPerformance;
```

- [ ] **Step 2: Add the route**

Add right after `GetIdleStaff` (before `UpdateTeamMember`):

```csharp
        [HttpGet("team/performance")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StaffPerformanceEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStaffPerformance([FromQuery] DateOnly date)
        {
            var result = await _mediator.Send(new GetStaffPerformanceQuery(date));
            return Ok(result);
        }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: every-active-staff-included (zero-filled), revenue-descending sort, net-of-refund revenue calculation, and Owner-only authorization all match the design doc exactly.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `StaffPerformanceRow`/`StaffPerformanceEntryDto` field names line up 1:1; the companion frontend plan's `IStaffPerformanceEntry` mirrors this exactly (camelCase JSON, no mapper needed).
