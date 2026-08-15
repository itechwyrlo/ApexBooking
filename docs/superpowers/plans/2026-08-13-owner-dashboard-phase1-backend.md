# Owner Dashboard Phase 1 — Backend (Refund Log) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A new query returning processed (actually-completed) refunds for the Owner Dashboard's Refund Log widget. Companion frontend plan: `docs/superpowers/plans/2026-08-13-owner-dashboard-phase1-frontend.md` in the LocalFlow repo. My Personal Lineup, Scan Booking QR, and Cancel & Refund need no backend work at all — covered entirely by the frontend plan.

**Architecture:** Mirrors the existing `GetPendingRefundRequestsQuery`/`Handler`/`IRefundRequestStore.GetPendingForTenantAsync` exactly, but inverted: a new `GetProcessedForTenantAsync` store method filters to `Succeeded`/`ManuallyRefunded` only (not the full four-state `TerminalStatuses` set the pending-query's store method excludes — `Rejected`/`Failed` aren't "processed," no money moved for those), ordered most-recent-first by `UpdatedAt` (the field that actually flips on every status transition), capped at a `limit` instead of paginated (this is a small recent-activity widget, not a full review queue).

**Tech Stack:** .NET / C#, MediatR, EF Core.

## Global Constraints

- No test project exists in this solution. Verification per task is `dotnet build`, run manually by the user.
- No schema/migration needed — reads existing `RefundRequest`/`Booking` data only.
- The new route is Owner-only (`[Authorize(Roles = "Owner")]`), matching this controller's existing `OwnerApprove`/`OwnerDeny` actions — financial data, not the broader `ManagementOnly` (Owner+Admin) used by the pending-refunds list.

---

### Task 1: GetProcessedForTenantAsync store method

**Files:**
- Modify: `ApexBooking.Core.Domain\Services\IRefundRequestStore.cs`
- Modify: `ApexBooking.Core.Persistence\Services\RefundRequestStore.cs`

**Interfaces:**
- Produces: `IRefundRequestStore.GetProcessedForTenantAsync(TenantId, int limit, CancellationToken) => Task<IReadOnlyList<RefundRequest>>` — consumed by Task 2.

- [ ] **Step 1: Add the interface method**

In `IRefundRequestStore.cs`, add right after `GetPendingForTenantAsync`:

```csharp
    // The Refund Log widget's data — refunds that actually completed (auto via PayMongo, or
    // manually sent by staff). Rejected/Failed requests aren't "processed" in this sense — no
    // money moved for those. Most-recently-processed first, capped at `limit` (no pagination UI
    // for this widget).
    Task<IReadOnlyList<RefundRequest>> GetProcessedForTenantAsync(
        TenantId tenantId, int limit, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Implement it**

In `RefundRequestStore.cs`, add a second status array and the method, right after `GetPendingForTenantAsync`:

```csharp
    private static readonly RefundRequestStatus[] ProcessedStatuses =
    [
        RefundRequestStatus.Succeeded,
        RefundRequestStatus.ManuallyRefunded
    ];

    public async Task<IReadOnlyList<RefundRequest>> GetProcessedForTenantAsync(
        TenantId tenantId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.RefundRequests
            .Where(r => r.TenantId == tenantId && ProcessedStatuses.Contains(r.Status))
            .OrderByDescending(r => r.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 2: GetRefundLogQuery + handler

**Files:**
- Create: `ApexBooking.Core.Application\Features\RefundRequests\Queries\GetRefundLog\GetRefundLogQuery.cs`
- Create: `ApexBooking.Core.Application\Features\RefundRequests\Queries\GetRefundLog\GetRefundLogHandler.cs`

**Interfaces:**
- Consumes: `IRefundRequestStore.GetProcessedForTenantAsync` from Task 1.
- Produces: `GetRefundLogQuery(int Limit = 20) : IQuery<IReadOnlyCollection<RefundLogEntryDto>>` — consumed by Task 3.

- [ ] **Step 1: Write the query + DTO**

```csharp
using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog
{
    public record RefundLogEntryDto(
        Guid Id,
        string BookingReference,
        decimal Amount,
        string CurrencyCode,
        string? PaymentMethodType,
        RefundRequestStatus Status,
        DateTime ProcessedAt
    );

    public record GetRefundLogQuery(int Limit = 20) : IQuery<IReadOnlyCollection<RefundLogEntryDto>>;
}
```

- [ ] **Step 2: Write the handler**

Mirrors `GetPendingRefundRequestsHandler`'s tenant+bookings join, reading `Booking.PaymentMethodType` (not used by the pending-refunds handler, but present on the entity) instead of `AmountDue`/`PayMongoPaymentId`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog
{
    public class GetRefundLogHandler : IQueryHandler<GetRefundLogQuery, IReadOnlyCollection<RefundLogEntryDto>>
    {
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;

        public GetRefundLogHandler(
            IRefundRequestStore refundRequestStore,
            IUnitOfWork unitOfWork,
            IUserContextService userContext)
        {
            _refundRequestStore = refundRequestStore;
            _unitOfWork = unitOfWork;
            _userContext = userContext;
        }

        public async Task<IReadOnlyCollection<RefundLogEntryDto>> Handle(GetRefundLogQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _userContext.GetCurrentTenantId();
            var requests = await _refundRequestStore.GetProcessedForTenantAsync(tenantId, query.Limit, cancellationToken);

            if (requests.Count == 0)
                return Array.Empty<RefundLogEntryDto>();

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Bookings);

            var result = new List<RefundLogEntryDto>();
            foreach (var request in requests)
            {
                var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);

                result.Add(new RefundLogEntryDto(
                    request.Id,
                    booking?.BookingReference ?? "(unknown)",
                    request.RequestedAmount,
                    request.CurrencyCode,
                    booking?.PaymentMethodType,
                    request.Status,
                    request.UpdatedAt));
            }

            return result;
        }
    }
}
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 3: Controller route

**Files:**
- Modify: `ApexBooking.WebApi\Controllers\RefundRequestsController.cs`

**Interfaces:**
- Consumes: `GetRefundLogQuery` from Task 2.
- Produces: `GET api/refund-requests/log?limit=20` → `IReadOnlyCollection<RefundLogEntryDto>`.

- [ ] **Step 1: Add the using**

Add alongside the existing feature usings:

```csharp
using ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog;
```

- [ ] **Step 2: Add the route**

Add right after the existing `GetPending` action, before `Confirm`:

```csharp
        [HttpGet("log")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(typeof(IReadOnlyCollection<RefundLogEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLog([FromQuery] int limit = 20)
        {
            var result = await _mediator.Send(new GetRefundLogQuery(limit));
            return Ok(result);
        }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: the Refund Log query, correctly scoped to actually-completed refunds (`Succeeded`/`ManuallyRefunded`, not the broader terminal-status set), is the only backend piece this phase needs — matches the design doc, which explicitly notes My Personal Lineup/Scan QR/Cancel & Refund need no backend work.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `RefundLogEntryDto` field names match what the companion frontend plan's `IRefundLogEntry` expects (camelCase JSON, no mapper needed).
