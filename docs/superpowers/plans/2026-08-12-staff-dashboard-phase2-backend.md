# Staff Dashboard Phase 2 — Backend (Chair Notes) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a staff member log a short note on a booking after they complete it, and surface the most recent past note for a customer via a lightweight lookup. Companion frontend plan: `docs/superpowers/plans/2026-08-12-staff-dashboard-phase2-frontend.md` in the LocalFlow repo.

**Architecture:** A new nullable `Booking.StaffNotes` field (distinct from the existing, creation-time-only `CustomerNotes`), settable only on a `Completed` booking via a new command, following the exact `Tenant.CompleteBooking` → `Booking.CompleteService()` delegation shape already in the codebase. A new query mirrors the existing `GetCustomerBookingsPageAsync` pattern on `ITenantRepository` to fetch the latest note for a customer. Separately, the shared bookings-list DTO (`TenantBookingSummary`/`TenantBookingRow`) gains a `CustomerId` field it's missing today, so the frontend can identify which customer's notes to look up.

**Tech Stack:** .NET / C#, MediatR, EF Core.

## Global Constraints

- No test project exists in this solution. Verification per task is `dotnet build`, run manually by the user — do not run it yourself per the standing instruction for this session.
- An EF Core migration is required (new `staff_notes` column). Do not generate or apply it yourself — Task 1 ends with the exact command for the user to run manually, consistent with not rebuilding/running the project yourself this session.
- New routes broaden past the `TenantController` class-level `ManagementOnly` policy to `[Authorize(Roles = "Owner,Admin,Staff")]`, matching the existing precedent for Staff-facing operational endpoints (`customers/search`, `bookings/walk-in-staff`, `bookings/scan-arrival`, `bookings/{id}/admit`) — any staff member on shift needs these, not just Owner/Admin.

---

### Task 1: StaffNotes field on Booking

**Files:**
- Modify: `ApexBooking.Core.Domain\Entities\Booking.cs`
- Modify: `ApexBooking.Core.Domain\Entities\Tenant.cs`
- Modify: `ApexBooking.Core.Persistence\Mappings\BookingConfiguration.cs`

**Interfaces:**
- Produces: `Booking.StaffNotes` (`string?`), `Booking.SetStaffNotes(string notes)`, `Tenant.SetBookingStaffNotes(Guid bookingId, string notes)` — consumed by Task 2.

- [ ] **Step 1: Add the field and mutator**

In `Booking.cs`, add the field right after the existing `CustomerNotes` property (currently line 34):

```csharp
        public string? CustomerNotes { get; private set; }
        public string? StaffNotes { get; private set; }
```

Add the mutator right after `CompleteService()` (currently ends at line 274, before `MarkAsNoShow()`):

```csharp
        // Distinct from CustomerNotes (set once at booking creation, never touched again) — this
        // is post-service, staff-authored, and mutable. Only makes sense once the visit is done.
        public void SetStaffNotes(string notes)
        {
            if (Status != BookingStatus.Completed)
                throw new BusinessRuleBrokenException("Chair notes can only be added to a completed appointment.");

            if (string.IsNullOrWhiteSpace(notes))
                throw new BusinessRuleBrokenException("Chair notes cannot be empty.");

            StaffNotes = notes.Trim();
            UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 2: Add the Tenant delegation method**

In `Tenant.cs`, add right after `CompleteBooking(Guid bookingId)` (currently lines 502-511):

```csharp
        public void SetBookingStaffNotes(Guid bookingId, string notes)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            booking.SetStaffNotes(notes);
            this.UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 3: Map the new column**

In `BookingConfiguration.cs`, add right after the existing `CustomerNotes` mapping (currently lines 71-73):

```csharp
            builder.Property(b => b.CustomerNotes)
                .HasColumnName("customer_notes")
                .HasMaxLength(1000);

            builder.Property(b => b.StaffNotes)
                .HasColumnName("staff_notes")
                .HasMaxLength(1000);
```

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

- [ ] **Step 5: Generate the migration**

Run (from the solution root, adjust the startup-project path if needed — follow whatever pattern was used for the most recent migration, e.g. `20260812084715_AddBusinessProfileContactPhoneNumber`):

```
dotnet ef migrations add AddBookingStaffNotes --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi
```

Expected: a new migration adding a nullable `staff_notes` column to the `bookings` table. (User runs this and applies it manually — do not run `dotnet ef database update` yourself.)

---

### Task 2: SetBookingStaffNotesCommand + route

**Files:**
- Create: `ApexBooking.Core.Application\Features\Bookings\Commands\SetBookingStaffNotes\SetBookingStaffNotesCommand.cs`
- Create: `ApexBooking.Core.Application\Features\Bookings\Commands\SetBookingStaffNotes\SetBookingStaffNotesCommandHandler.cs`
- Modify: `ApexBooking.WebApi\Controllers\TenantController.cs`

**Interfaces:**
- Consumes: `Tenant.SetBookingStaffNotes` from Task 1.
- Produces: `POST api/Tenant/bookings/{bookingId}/staff-notes`, body `{ notes: string }`.

- [ ] **Step 1: Write the command**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.SetBookingStaffNotes
{
    public record SetBookingStaffNotesCommand(Guid BookingId, string Notes) : ICommand;
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

namespace ApexBooking.Core.Application.Features.Bookings.Commands.SetBookingStaffNotes
{
    public class SetBookingStaffNotesCommandHandler : ICommandHandler<SetBookingStaffNotesCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public SetBookingStaffNotesCommandHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(SetBookingStaffNotesCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to save chair notes. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Bookings);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            tenant.SetBookingStaffNotes(command.BookingId, command.Notes);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 3: Add the route**

In `TenantController.cs`, add right after `CompleteBooking` (currently lines 509-516):

```csharp
        [HttpPost("bookings/{bookingId:guid}/staff-notes")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetBookingStaffNotes([FromRoute] Guid bookingId, [FromBody] SetBookingStaffNotesCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { BookingId = bookingId }, cancellationToken);
            return NoContent();
        }
```

(Add a `using ApexBooking.Core.Application.Features.Bookings.Commands.SetBookingStaffNotes;` to the controller's usings if that namespace isn't already covered by an existing wildcard/using — check the top of the file first; this codebase's controllers typically list feature namespaces individually.)

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 3: CustomerId on the shared bookings-list DTO

**Files:**
- Modify: `ApexBooking.Core.Domain\Repositories\ITenantRepository.cs`
- Modify: `ApexBooking.Core.Persistence\Repositories\TenantRepository.cs`
- Modify: `ApexBooking.Core.Application\Dtos\Response\TenantBookingSummary.cs`
- Modify: `ApexBooking.Core.Application\Features\Bookings\Queries\GetTenantBookings\GetTenantBookingsHandler.cs`

**Interfaces:**
- Produces: `TenantBookingRow.CustomerId` (`Guid`), `TenantBookingSummary.CustomerId` (`Guid`) — consumed by the companion frontend plan's `ITenantBooking.customerId`.

- [ ] **Step 1: Add CustomerId to TenantBookingRow**

In `ITenantRepository.cs`, change the `TenantBookingRow` record (currently lines 63-85) to append `Guid CustomerId` as the last field:

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
    DateTime CreatedAt,
    Guid CustomerId
);
```

- [ ] **Step 2: Populate it in the repository projection**

In `TenantRepository.cs`, `GetBookingsPageAsync`'s projection (currently lines 100-121), add `b.CustomerId.Value` as the last constructor argument:

```csharp
            select new TenantBookingRow(
                b.BookingId.Value,
                b.BookingReference,
                c != null ? c.Contact.Name : "Unknown",
                c != null ? c.Contact.PhoneNumber : null,
                s != null ? s.Name : "Unknown",
                m != null ? m.FirstName + " " + m.LastName : "Unknown",
                br != null ? br.BranchName : "Unknown",
                b.ScheduledDate,
                b.ScheduledStartTime,
                b.DurationMinutes,
                b.Status,
                b.RequiresUpfrontPayment,
                b.AmountDue,
                b.CurrencyCode,
                b.PaymentConfirmedVia,
                b.CheckedInAt,
                b.ServiceCompletedAt,
                b.CancelledAt,
                b.CancellationReason,
                b.NoShowAt,
                b.CreatedAt,
                b.CustomerId.Value);
```

- [ ] **Step 3: Add CustomerId to TenantBookingSummary**

In `TenantBookingSummary.cs`, append `Guid CustomerId` as the last field:

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
        DateTime CreatedAt,
        Guid CustomerId
    );
```

- [ ] **Step 4: Map it through in the handler**

In `GetTenantBookingsHandler.cs`, the `TenantBookingSummary` construction (currently lines 40-61), add `row.CustomerId` as the last argument:

```csharp
            var mappedItems = pagedResult.data.Select(row => new TenantBookingSummary(
                row.BookingId,
                row.BookingReference,
                row.CustomerName,
                row.CustomerPhone,
                row.ServiceName,
                row.StaffName,
                row.BranchName,
                row.ScheduledDate,
                row.ScheduledStartTime,
                row.DurationMinutes,
                row.Status,
                row.RequiresUpfrontPayment,
                row.AmountDue,
                row.CurrencyCode,
                row.PaymentConfirmedVia,
                row.CheckedInAt,
                row.ServiceCompletedAt,
                row.CancelledAt,
                row.CancellationReason,
                row.NoShowAt,
                row.CreatedAt,
                row.CustomerId));
```

- [ ] **Step 5: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 4: GetCustomerLatestNoteQuery + route

**Files:**
- Modify: `ApexBooking.Core.Domain\Repositories\ITenantRepository.cs`
- Modify: `ApexBooking.Core.Persistence\Repositories\TenantRepository.cs`
- Create: `ApexBooking.Core.Application\Dtos\Response\CustomerLatestNoteDto.cs`
- Create: `ApexBooking.Core.Application\Features\Customers\Queries\GetCustomerLatestNote\GetCustomerLatestNoteQuery.cs`
- Create: `ApexBooking.Core.Application\Features\Customers\Queries\GetCustomerLatestNote\GetCustomerLatestNoteHandler.cs`
- Modify: `ApexBooking.WebApi\Controllers\TenantController.cs`

**Interfaces:**
- Consumes: `Booking.StaffNotes` from Task 1.
- Produces: `GET api/Tenant/customers/{customerId}/latest-note` → `CustomerLatestNoteDto` (200) or no content (204).

- [ ] **Step 1: Add the repository row type and method to the interface**

In `ITenantRepository.cs`, add the interface method right after `GetCustomerBookingsPageAsync` (currently lines 47-54):

```csharp
    // Powers the Staff Dashboard's "active appointment" note preview — the single most recent
    // staff-authored note for this customer, if any exists. Same rationale as
    // GetCustomerBookingsPageAsync: Booking is a Tenant child, so this lives here.
    Task<CustomerLatestNoteRow?> GetLatestStaffNoteAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default);
```

Add the row record right after `CustomerBookingRow` (currently lines 87-101):

```csharp
public record CustomerLatestNoteRow(string Notes, DateOnly NotedOn);
```

- [ ] **Step 2: Implement it in the repository**

In `TenantRepository.cs`, add right after `GetCustomerBookingsPageAsync` (currently ends at line 172, before `StaffHasBookingsAsync`):

```csharp
    public async Task<CustomerLatestNoteRow?> GetLatestStaffNoteAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        return await context.Bookings.AsNoTracking()
            .Where(b => b.TenantId == tenantId
                && b.CustomerId == customerId
                && b.Status == BookingStatus.Completed
                && b.StaffNotes != null)
            .OrderByDescending(b => b.ScheduledDate)
            .ThenByDescending(b => b.ScheduledStartTime)
            .Select(b => new CustomerLatestNoteRow(b.StaffNotes!, b.ScheduledDate))
            .FirstOrDefaultAsync(cancellationToken);
    }
```

- [ ] **Step 3: Write the DTO, query, and handler**

`CustomerLatestNoteDto.cs`:

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record CustomerLatestNoteDto(string Notes, DateOnly NotedOn);
}
```

`GetCustomerLatestNoteQuery.cs`:

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerLatestNote
{
    public record GetCustomerLatestNoteQuery(Guid CustomerId) : IQuery<CustomerLatestNoteDto?>;
}
```

`GetCustomerLatestNoteHandler.cs`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerLatestNote
{
    public class GetCustomerLatestNoteHandler : IQueryHandler<GetCustomerLatestNoteQuery, CustomerLatestNoteDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetCustomerLatestNoteHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<CustomerLatestNoteDto?> Handle(GetCustomerLatestNoteQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load client notes. No authenticated tenant context was found.");

            var row = await _unitOfWork.TenantRepository.GetLatestStaffNoteAsync(
                tenantId,
                new CustomerId(query.CustomerId),
                cancellationToken);

            return row is null ? null : new CustomerLatestNoteDto(row.Notes, row.NotedOn);
        }
    }
}
```

- [ ] **Step 4: Add the route**

In `TenantController.cs`, add right after `GetCustomerBookings` (currently lines 297-304):

```csharp
        // Staff included — this backs the Staff Dashboard's own "active appointment" note preview.
        [HttpGet("customers/{customerId:guid}/latest-note")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(CustomerLatestNoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetCustomerLatestNote([FromRoute] Guid customerId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCustomerLatestNoteQuery(customerId), cancellationToken);
            return result is null ? NoContent() : Ok(result);
        }
```

(Add a `using ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerLatestNote;` to the controller's usings, matching how neighboring feature namespaces are imported individually.)

- [ ] **Step 5: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: `StaffNotes` field + mutator, `SetBookingStaffNotesCommand` + route, `CustomerId` on the shared bookings DTO, and `GetCustomerLatestNoteQuery` + route are all covered per the design doc.
- **Placeholder scan**: no TBDs — every step has literal, complete code with exact surrounding context, and the migration command is given verbatim (with a note to confirm the current pattern against the most recent existing migration before running).
- **Type consistency**: `CustomerLatestNoteRow`/`CustomerLatestNoteDto` field names (`Notes`, `NotedOn`) match between the repository row and the application DTO; `TenantBookingRow.CustomerId`/`TenantBookingSummary.CustomerId` are both `Guid`, matching the companion frontend plan's expected `customerId: string`.
