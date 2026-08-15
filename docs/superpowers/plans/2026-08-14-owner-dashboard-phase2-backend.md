# Owner Dashboard Phase 2 — Backend (Total Shop Revenue) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A new query summing today's collected revenue, split Online vs. Pay-on-Visit, netting out succeeded refunds. Companion frontend plan: `docs/superpowers/plans/2026-08-14-owner-dashboard-phase2-frontend.md` in the LocalFlow repo.

**Architecture:** Follows the exact `ITenantRepository`/flat-per-metric pattern already used for `GetBookingCountsAsync` — two scoped `SumAsync` calls (Online, Pay-on-Visit) over the same `ScheduledDate == today` window Admin's Daily Booking Counters already uses, each row's contribution computed as `AmountDue - (RefundStatus == Succeeded ? RefundedAmount ?? 0 : 0)` so a fully-refunded booking nets to zero without a separate exclusion.

**Tech Stack:** .NET / C#, MediatR, EF Core.

## Global Constraints

- No test project exists in this solution. Verification per task is `dotnet build`, run manually by the user.
- No schema/migration needed — reads existing `Booking` columns only.
- The new route is Owner-only (`[Authorize(Roles = "Owner")]`) — financial data, matching the Refund Log route and the existing payment-gateway-credentials endpoints, not the broader `ManagementOnly` (Owner+Admin) most booking routes use.

---

### Task 1: GetRevenueAsync repository method

**Files:**
- Modify: `ApexBooking.Core.Domain\Repositories\ITenantRepository.cs`
- Modify: `ApexBooking.Core.Persistence\Repositories\TenantRepository.cs`

**Interfaces:**
- Produces: `ITenantRepository.GetRevenueAsync(TenantId, DateOnly, CancellationToken) => Task<TenantRevenueRow>` — consumed by Task 2.

- [ ] **Step 1: Add the row type and interface method**

In `ITenantRepository.cs`, add the interface method right after `GetIdleStaffAsync` (currently lines 78-84), inside the interface block:

```csharp
    // Powers the Owner Dashboard's Total Shop Revenue widget — today's collected money, split
    // Online vs. Pay-on-Visit, netted against any succeeded refunds. Same ScheduledDate == today
    // window GetBookingCountsAsync already uses, for one consistent "today" across every widget.
    Task<TenantRevenueRow> GetRevenueAsync(
        TenantId tenantId,
        DateOnly date,
        CancellationToken cancellationToken = default);
```

Add the row record right after `IdleStaffRow` (currently the last line of the file):

```csharp
public record IdleStaffRow(Guid TenantMemberId, string Name, string? PhotoUrl);

public record TenantRevenueRow(decimal OnlineAmount, decimal PayInVisitAmount, string CurrencyCode);
```

- [ ] **Step 2: Implement it**

In `TenantRepository.cs`, add right after `GetIdleStaffAsync` (currently ends at line 216), before `StaffHasBookingsAsync`:

```csharp
    public async Task<TenantRevenueRow> GetRevenueAsync(
        TenantId tenantId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var eligibleBookings = context.Bookings.AsNoTracking()
            .Where(b => b.TenantId == tenantId
                && b.ScheduledDate == date
                && b.Status != BookingStatus.Cancelled
                && b.PaymentConfirmedVia != null);

        var onlineAmount = await eligibleBookings
            .Where(b => b.PaymentConfirmedVia == PaymentConfirmationMethod.Online)
            .SumAsync(b => b.AmountDue - (b.RefundStatus == RefundStatus.Succeeded ? (b.RefundedAmount ?? 0) : 0), cancellationToken);

        var payInVisitAmount = await eligibleBookings
            .Where(b => b.PaymentConfirmedVia == PaymentConfirmationMethod.PayInVisit)
            .SumAsync(b => b.AmountDue - (b.RefundStatus == RefundStatus.Succeeded ? (b.RefundedAmount ?? 0) : 0), cancellationToken);

        var currencyCode = await eligibleBookings.Select(b => b.CurrencyCode).FirstOrDefaultAsync(cancellationToken) ?? "PHP";

        return new TenantRevenueRow(onlineAmount, payInVisitAmount, currencyCode);
    }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 2: GetTenantRevenueQuery + handler

**Files:**
- Create: `ApexBooking.Core.Application\Dtos\Response\TenantRevenueDto.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Queries\GetTenantRevenue\GetTenantRevenueQuery.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Queries\GetTenantRevenue\GetTenantRevenueHandler.cs`

**Interfaces:**
- Consumes: `ITenantRepository.GetRevenueAsync` from Task 1.
- Produces: `GetTenantRevenueQuery(DateOnly Date) : IQuery<TenantRevenueDto>` — consumed by Task 3.

- [ ] **Step 1: DTO**

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record TenantRevenueDto(decimal OnlineAmount, decimal PayInVisitAmount, decimal Total, string CurrencyCode);
}
```

- [ ] **Step 2: Query**

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantRevenue
{
    public record GetTenantRevenueQuery(DateOnly Date) : IQuery<TenantRevenueDto>;
}
```

- [ ] **Step 3: Handler**

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantRevenue
{
    public class GetTenantRevenueHandler : IQueryHandler<GetTenantRevenueQuery, TenantRevenueDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetTenantRevenueHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<TenantRevenueDto> Handle(GetTenantRevenueQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load revenue. No authenticated tenant context was found.");

            var revenue = await _unitOfWork.TenantRepository.GetRevenueAsync(tenantId, query.Date, cancellationToken);

            return new TenantRevenueDto(
                revenue.OnlineAmount,
                revenue.PayInVisitAmount,
                revenue.OnlineAmount + revenue.PayInVisitAmount,
                revenue.CurrencyCode);
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
- Consumes: `GetTenantRevenueQuery` from Task 2.
- Produces: `GET api/Tenant/bookings/revenue?date=yyyy-MM-dd` → `TenantRevenueDto`.

- [ ] **Step 1: Add the using**

Add alongside the existing `Bookings.Queries` usings:

```csharp
using ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantRevenue;
```

- [ ] **Step 2: Add the route**

Add right after `GetBookingCounts` (before `GetReassignableStaff`):

```csharp
        [HttpGet("bookings/revenue")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(typeof(TenantRevenueDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenue([FromQuery] DateOnly date)
        {
            var result = await _mediator.Send(new GetTenantRevenueQuery(date));
            return Ok(result);
        }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: the net-of-refund calculation, `ScheduledDate == today` scoping, Online/Pay-on-Visit split, and Owner-only authorization all match the design doc exactly.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `TenantRevenueRow`/`TenantRevenueDto` field names line up (`OnlineAmount`, `PayInVisitAmount`, `CurrencyCode`); `Total` is computed once, in the handler, not duplicated in the repository row.
