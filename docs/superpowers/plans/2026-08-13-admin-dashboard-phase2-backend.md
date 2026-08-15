# Admin Dashboard Phase 2 — Backend (Reassign Barber) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let an Owner/Admin change which staff member a `Scheduled` booking is assigned to, restricted to staff who are active, deployed to the booking's branch, and qualified for the booking's service. Companion frontend plan: `docs/superpowers/plans/2026-08-13-admin-dashboard-phase2-frontend.md` in the LocalFlow repo.

**Architecture:** `StaffId` is added to the shared bookings-list DTO (`TenantBookingRow`/`TenantBookingSummary`), the same additive, append-style change already made for `CustomerId` in Chair Notes. A new `Booking.Reassign` mutator + `Tenant.ReassignBooking` aggregate-level validation follow the exact shape of `Booking.SetStaffNotes`/`Tenant.SetBookingStaffNotes`. The qualification check (active + same branch + assigned to the service) reuses the exact predicate `GetBookableStaffHandler` already uses for the public wizard's staff-picker — resolved here from a `BookingId` instead of caller-supplied `branchId`/`serviceId`. Both the reassign command and the reassignable-staff query reuse the existing `GetForWalkInAvailabilityAsync` aggregate-load (already hydrates `Branches` + `Members` + `Services.ServiceProviders` + `Bookings` in one query) — no new aggregate-load method needed.

**Tech Stack:** .NET / C#, MediatR, EF Core.

## Global Constraints

- No test project exists in this solution. Verification per task is `dotnet build`, run manually by the user.
- No schema/migration needed — `StaffId` already exists as a column on `Booking`; this only adds it to the read-side DTOs.
- Both new routes use the `TenantController` class-level default `ManagementOnly` policy (no `[Authorize]` override) — matches `CompleteBooking`/`CancelBooking`/`MarkBookingNoShow`'s existing default (Owner/Admin only, not Staff), consistent with "Reassign Barber" being an Admin Dashboard–only tool.
- No live slot-availability checking is added — see the design doc's scope decision. The reassignable-staff list is qualification-only (active + branch + service match), same as the public wizard's staff-picker.

---

### Task 1: StaffId on the shared bookings-list DTO

**Files:**
- Modify: `ApexBooking.Core.Domain\Repositories\ITenantRepository.cs`
- Modify: `ApexBooking.Core.Persistence\Repositories\TenantRepository.cs`
- Modify: `ApexBooking.Core.Application\Dtos\Response\TenantBookingSummary.cs`
- Modify: `ApexBooking.Core.Application\Features\Bookings\Queries\GetTenantBookings\GetTenantBookingsHandler.cs`

**Interfaces:**
- Produces: `TenantBookingRow.StaffId` (`Guid`), `TenantBookingSummary.StaffId` (`Guid`) — consumed by the companion frontend plan's `ITenantBooking.staffId`.

- [ ] **Step 1: Add StaffId to TenantBookingRow**

In `ITenantRepository.cs`, change the `TenantBookingRow` record (append after `CustomerId`, before `CreatedAt`):

```csharp
public record TenantBookingRow(
    Guid BookingId,
    string BookingReference,
    string CustomerName,
    string? CustomerPhone,
    string ServiceName,
    string StaffName,
    string BranchName,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    int DurationMinutes,
    BookingStatus Status,
    bool RequiresUpfrontPayment,
    decimal AmountDue,
    string CurrencyCode,
    PaymentConfirmationMethod? PaymentConfirmedVia,
    DateTime? CheckedInAt,
    DateTime? ServiceCompletedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    DateTime? NoShowAt,
    Guid CustomerId,
    Guid StaffId,
    DateTime CreatedAt
);
```

- [ ] **Step 2: Populate it in the repository projection**

In `TenantRepository.cs`, `GetBookingsPageAsync`'s projection, change the tail to add `b.StaffId.Value` between `b.CustomerId.Value` and `b.CreatedAt`:

```csharp
                b.CustomerId.Value,
                b.StaffId.Value,
                b.CreatedAt);
```

- [ ] **Step 3: Add StaffId to TenantBookingSummary**

In `TenantBookingSummary.cs`, same append:

```csharp
    public record TenantBookingSummary(
        Guid BookingId,
        string BookingReference,
        string CustomerName,
        string? CustomerPhone,
        string ServiceName,
        string StaffName,
        string BranchName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        int DurationMinutes,
        BookingStatus Status,
        bool RequiresUpfrontPayment,
        decimal AmountDue,
        string CurrencyCode,
        PaymentConfirmationMethod? PaymentConfirmedVia,
        DateTime? CheckedInAt,
        DateTime? ServiceCompletedAt,
        DateTime? CancelledAt,
        string? CancellationReason,
        DateTime? NoShowAt,
        Guid CustomerId,
        Guid StaffId,
        DateTime CreatedAt
    );
```

- [ ] **Step 4: Map it through in the handler**

In `GetTenantBookingsHandler.cs`, the `TenantBookingSummary` construction, add `row.StaffId` between `row.CustomerId` and `row.CreatedAt`:

```csharp
                row.CustomerId,
                row.StaffId,
                row.CreatedAt));
```

- [ ] **Step 5: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 2: Booking.Reassign + Tenant.ReassignBooking

**Files:**
- Modify: `ApexBooking.Core.Domain\Entities\Booking.cs`
- Modify: `ApexBooking.Core.Domain\Entities\Tenant.cs`

**Interfaces:**
- Produces: `Booking.Reassign(TenantMemberId newStaffId)`, `Tenant.ReassignBooking(Guid bookingId, Guid newStaffId)` — consumed by Task 3.

- [ ] **Step 1: Add the Booking mutator**

In `Booking.cs`, add right after `SetStaffNotes` (currently lines 277-289), before `MarkAsNoShow`:

```csharp
        public void Reassign(TenantMemberId newStaffId)
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Only currently scheduled appointments can be reassigned.");

            StaffId = newStaffId;
            UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 2: Add the Tenant delegation + validation method**

In `Tenant.cs`, add right after `SetBookingStaffNotes` (currently lines 513-521), before `FlagBookingAsNoShow`:

```csharp
        public void ReassignBooking(Guid bookingId, Guid newStaffId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            var newStaffMember = _tenantMembers.FirstOrDefault(m => m.TenantMemberId.Value == newStaffId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("The selected staff member is not available.");

            if (newStaffMember.BranchId != booking.BranchId)
                throw new BusinessRuleBrokenException("The selected staff member is not deployed to this booking's branch.");

            var service = _services.FirstOrDefault(s => s.ServiceId == booking.ServiceId)
                ?? throw new BusinessRuleBrokenException("This booking's service could not be found.");

            if (!service.ServiceProviders.Any(sp => sp.TenantMemberId == newStaffMember.TenantMemberId))
                throw new BusinessRuleBrokenException("The selected staff member is not assigned to this booking's service.");

            booking.Reassign(newStaffMember.TenantMemberId);
            this.UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 3: ReassignBookingCommand + route

**Files:**
- Create: `ApexBooking.Core.Application\Features\Bookings\Commands\ReassignBooking\ReassignBookingCommand.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Commands\ReassignBooking\ReassignBookingCommandHandler.cs`
- Modify: `ApexBooking.WebApi\Controllers\TenantController.cs`

**Interfaces:**
- Consumes: `Tenant.ReassignBooking` from Task 2.
- Produces: `POST api/Tenant/bookings/{bookingId}/reassign`, body `{ newStaffId: Guid }`.

- [ ] **Step 1: Write the command**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ReassignBooking
{
    public record ReassignBookingCommand(Guid BookingId, Guid NewStaffId) : ICommand;
}
```

- [ ] **Step 2: Write the handler**

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ReassignBooking
{
    public class ReassignBookingCommandHandler : ICommandHandler<ReassignBookingCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public ReassignBookingCommandHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(ReassignBookingCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to reassign this appointment. No authenticated tenant context was found.");

            // GetForWalkInAvailabilityAsync already hydrates Branches + Members + Services.ServiceProviders
            // + Bookings in one query — exactly what ReassignBooking's validation needs, no new
            // aggregate-load method required.
            var tenant = await _unitOfWork.TenantRepository.GetForWalkInAvailabilityAsync(tenantId, cancellationToken);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Failed to reassign this appointment. Isolated tenant context could not be verified.");

            tenant.ReassignBooking(command.BookingId, command.NewStaffId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 3: Add the route**

In `TenantController.cs`, add right after `CompleteBooking`:

```csharp
        [HttpPost("bookings/{bookingId:guid}/reassign")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReassignBooking([FromRoute] Guid bookingId, [FromBody] ReassignBookingCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { BookingId = bookingId }, cancellationToken);
            return NoContent();
        }
```

Add `using ApexBooking.Core.Application.Features.Bookings.Commands.ReassignBooking;` alongside the other `Bookings.Commands` usings.

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 4: GetReassignableStaffQuery + route

**Files:**
- Create: `ApexBooking.Core.Application\Dtos\Response\ReassignableStaffDto.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Queries\GetReassignableStaff\GetReassignableStaffQuery.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Queries\GetReassignableStaff\GetReassignableStaffHandler.cs`
- Modify: `ApexBooking.WebApi\Controllers\TenantController.cs`

**Interfaces:**
- Produces: `GET api/Tenant/bookings/{bookingId}/reassignable-staff` → `IReadOnlyCollection<ReassignableStaffDto>`.

- [ ] **Step 1: DTO**

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record ReassignableStaffDto(Guid TenantMemberId, string Name);
}
```

- [ ] **Step 2: Query**

```csharp
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetReassignableStaff
{
    public record GetReassignableStaffQuery(Guid BookingId) : IQuery<IReadOnlyCollection<ReassignableStaffDto>>;
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

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetReassignableStaff
{
    public class GetReassignableStaffHandler : IQueryHandler<GetReassignableStaffQuery, IReadOnlyCollection<ReassignableStaffDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetReassignableStaffHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<ReassignableStaffDto>> Handle(GetReassignableStaffQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load reassignable staff. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetForWalkInAvailabilityAsync(tenantId, cancellationToken);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Failed to load reassignable staff. Isolated tenant context could not be verified.");

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == query.BookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            var service = tenant.Services.FirstOrDefault(s => s.ServiceId == booking.ServiceId);
            if (service is null)
                return Array.Empty<ReassignableStaffDto>();

            return tenant.Members
                .Where(member => member.IsActive &&
                                 member.BranchId == booking.BranchId &&
                                 service.ServiceProviders.Any(prov => prov.TenantMemberId == member.TenantMemberId))
                .OrderBy(m => m.FirstName)
                .Select(m => new ReassignableStaffDto(m.TenantMemberId.Value, $"{m.FirstName} {m.LastName}".Trim()))
                .ToList();
        }
    }
}
```

(Add `using System;` for `Array.Empty<T>()` if not already implicitly available via global usings — check the project's `.csproj` for `<ImplicitUsings>enable</ImplicitUsings>`; if enabled, `System` is already global and this using can be omitted.)

- [ ] **Step 4: Add the route**

In `TenantController.cs`, add right after `GetBookingCounts` (before `GetWalkInAvailableStaff`):

```csharp
        [HttpGet("bookings/{bookingId:guid}/reassignable-staff")]
        [ProducesResponseType(typeof(IReadOnlyCollection<ReassignableStaffDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReassignableStaff([FromRoute] Guid bookingId)
        {
            var result = await _mediator.Send(new GetReassignableStaffQuery(bookingId));
            return Ok(result);
        }
```

Add `using ApexBooking.Core.Application.Features.Bookings.Queries.GetReassignableStaff;` alongside the other `Bookings.Queries` usings.

- [ ] **Step 5: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: `StaffId` on the shared DTO, the `Reassign`/`ReassignBooking` mutators, the command+route, and the qualification-only reassignable-staff query+route are all covered per the design doc's two scope decisions (Quick-Tools-modal UX handled entirely on the frontend side; qualification-only, no live availability, handled here).
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `ReassignableStaffDto`/`ReassignBookingCommand` field names match what the companion frontend plan's `IReassignableStaffMember`/`reassignBooking()` expect; `TenantBookingRow.StaffId`/`TenantBookingSummary.StaffId` match the frontend's `ITenantBooking.staffId`.
