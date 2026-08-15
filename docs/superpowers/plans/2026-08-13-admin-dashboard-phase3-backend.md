# Admin Dashboard Phase 3 — Backend (Collect Pay on Visit) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let an Owner/Admin record a cash/card payment for a scheduled booking before the visit ends (e.g. at check-in for a walk-in), instead of the payment only ever being auto-captured implicitly at `CompleteService()` time. Companion frontend plan: `docs/superpowers/plans/2026-08-13-admin-dashboard-phase3-frontend.md` in the LocalFlow repo.

**Architecture:** A new `Booking.RecordPayInVisitPayment()` mutator mirrors `CompleteService()`'s existing auto-capture branch exactly — same guard shape, same `PaymentCapturedDomainEvent`, just triggered explicitly instead of implicitly at completion. `Tenant.RecordBookingPayment(bookingId)` is a thin delegate matching `Tenant.CompleteBooking`/`Tenant.SetBookingStaffNotes`. The command/handler reuse the same lightweight aggregate-load those two already use (`GetAsync` with just `Bookings` included) — no cross-collection validation is needed here (unlike Reassign Barber), so the heavier `GetForWalkInAvailabilityAsync` isn't required.

**Tech Stack:** .NET / C#, MediatR.

## Global Constraints

- No test project exists in this solution. Verification per task is `dotnet build`, run manually by the user.
- No schema/migration needed — this only sets an existing column (`PaymentConfirmedVia`) through a new code path.
- The new route uses the `TenantController` class-level default `ManagementOnly` policy (no `[Authorize]` override) — Owner/Admin only, confirmed by the business owner to stay consistent with Reassign Barber (not broadened to Staff).

---

### Task 1: Booking.RecordPayInVisitPayment + Tenant.RecordBookingPayment

**Files:**
- Modify: `ApexBooking.Core.Domain\Entities\Booking.cs`
- Modify: `ApexBooking.Core.Domain\Entities\Tenant.cs`

**Interfaces:**
- Produces: `Booking.RecordPayInVisitPayment()`, `Tenant.RecordBookingPayment(Guid bookingId)` — consumed by Task 2.

- [ ] **Step 1: Add the Booking mutator**

In `Booking.cs`, add right after `Reassign` (currently ends at line 297), before `MarkAsNoShow`:

```csharp
        public void RecordPayInVisitPayment()
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Payment can only be recorded for a currently scheduled appointment.");

            if (PaymentConfirmedVia is not null)
                throw new BusinessRuleBrokenException("Payment has already been recorded for this appointment.");

            PaymentConfirmedVia = PaymentConfirmationMethod.PayInVisit;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new PaymentCapturedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                AmountDue: this.AmountDue,
                CurrencyCode: this.CurrencyCode,
                Method: PaymentConfirmationMethod.PayInVisit,
                CapturedAt: UpdatedAt
            ));
        }
```

- [ ] **Step 2: Add the Tenant delegation method**

In `Tenant.cs`, add right after `ReassignBooking` (currently ends at line 542), before `FlagBookingAsNoShow`:

```csharp
        public void RecordBookingPayment(Guid bookingId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            booking.RecordPayInVisitPayment();
            this.UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 3: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 2: RecordPayInVisitPaymentCommand + route

**Files:**
- Create: `ApexBooking.Core.Application\Features\Bookings\Commands\RecordPayInVisitPayment\RecordPayInVisitPaymentCommand.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Commands\RecordPayInVisitPayment\RecordPayInVisitPaymentCommandHandler.cs`
- Modify: `ApexBooking.WebApi\Controllers\TenantController.cs`

**Interfaces:**
- Consumes: `Tenant.RecordBookingPayment` from Task 1.
- Produces: `POST api/Tenant/bookings/{bookingId}/collect-payment`.

- [ ] **Step 1: Write the command**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.RecordPayInVisitPayment
{
    public record RecordPayInVisitPaymentCommand(Guid BookingId) : ICommand;
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

namespace ApexBooking.Core.Application.Features.Bookings.Commands.RecordPayInVisitPayment
{
    public class RecordPayInVisitPaymentCommandHandler : ICommandHandler<RecordPayInVisitPaymentCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public RecordPayInVisitPaymentCommandHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(RecordPayInVisitPaymentCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to record payment. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Bookings);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            tenant.RecordBookingPayment(command.BookingId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 3: Add the route**

In `TenantController.cs`, add right after `ReassignBooking` (before `SetBookingStaffNotes`):

```csharp
        [HttpPost("bookings/{bookingId:guid}/collect-payment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CollectPayment([FromRoute] Guid bookingId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new RecordPayInVisitPaymentCommand(bookingId), cancellationToken);
            return NoContent();
        }
```

Add `using ApexBooking.Core.Application.Features.Bookings.Commands.RecordPayInVisitPayment;` alongside the other `Bookings.Commands` usings.

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: the mutator's guards (`Status == Scheduled`, `PaymentConfirmedVia == null`) and the `PaymentCapturedDomainEvent` reuse match the design doc exactly; the route's authorization (default `ManagementOnly`, no Staff access) matches the business owner's explicit confirmation to stay consistent with Reassign Barber.
- **Placeholder scan**: no TBDs — every step has literal, complete code.
- **Type consistency**: `RecordPayInVisitPaymentCommand(Guid BookingId)` matches the companion frontend plan's `collectPayment(bookingId)` call shape.
