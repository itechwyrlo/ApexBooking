# Refund Review Workflow Phase 2 — Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the backend for Phase 2, per
[2026-08-12-refund-review-workflow-phase2-design.md](../../../LocalFlow/docs/superpowers/specs/2026-08-12-refund-review-workflow-phase2-design.md)
(LocalFlow repo): paginated refund-request listing with payment-verification
detail, the Mark-as-Sent manual-transfer action, `BusinessProfile.ContactPhoneNumber`,
the public customer refund-status page's backend, and the two remaining
notifications.

**Architecture:** Extends Phase 1's already-shipped `RefundRequest` state
machine — no changes to its transitions. New public endpoints reuse the
existing `ICancellationTokenService`/`CancellationTokenPayload` mechanism
the cancel-booking link already uses, not a new token type. New pieces
otherwise follow whichever Phase 1 file/pattern they extend most directly.

**Tech Stack:** Same as the rest of this solution — ASP.NET Core / EF Core / MediatR / FluentValidation / xUnit. No new packages.

## Global Constraints

- Public/anonymous endpoints reuse `ICancellationTokenService` — no new token type.
- `BusinessProfile.ContactPhoneNumber` maps to column `contact_phone_number` — NOT `owner_phone_number`, which already exists on `Tenant.OwnerContact` for an unrelated purpose (confirmed this session — don't collide).
- `GetPendingRefundRequestsHandler` returns `ApexBooking.SharedKernel.Models.QueryResult<RefundRequestSummaryDto>` (the same wrapper `GET /api/Tenant/team` already returns), not a bare list.
- Mark-as-Sent reuses `Booking.RecordRefundOutcome` (existing, from pass #1) — no new "manually refunded" domain method.

---

### Task 1: Pagination on the refund-requests list

**Files:**
- Modify: `ApexBooking.Core.Domain/Services/IRefundRequestStore.cs`
- Modify: `ApexBooking.Core.Persistence/Services/RefundRequestStore.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/GetPendingRefundRequestsQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/GetPendingRefundRequestsHandler.cs`
- Modify: `ApexBooking.WebApi/Controllers/RefundRequestsController.cs`

**Interfaces:**
- Produces: `IRefundRequestStore.GetPendingForTenantAsync(TenantId, int pageNumber, int pageSize, CancellationToken)` returns `(IReadOnlyList<RefundRequest> Items, int Total)`. `GetPendingRefundRequestsQuery(int PageNumber, int PageSize)`. Handler returns `QueryResult<RefundRequestSummaryDto>`.

- [ ] **Step 1: Update the store interface and implementation**

`IRefundRequestStore.cs`, replace the existing method:

```csharp
    // Everything not yet in a terminal state (Rejected/ManuallyRefunded/Succeeded/Failed) — the
    // review page's list. Paged: (Items, Total).
    Task<(IReadOnlyList<RefundRequest> Items, int Total)> GetPendingForTenantAsync(
        TenantId tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
```

`RefundRequestStore.cs`, replace the implementation:

```csharp
    public async Task<(IReadOnlyList<RefundRequest> Items, int Total)> GetPendingForTenantAsync(
        TenantId tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.RefundRequests
            .Where(r => r.TenantId == tenantId && !TerminalStatuses.Contains(r.Status))
            .OrderBy(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
```

- [ ] **Step 2: Update the query and handler**

`GetPendingRefundRequestsQuery.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests
{
    public record RefundRequestSummaryDto(
        Guid Id,
        Guid BookingId,
        string BookingReference,
        string CustomerName,
        decimal RequestedAmount,
        decimal AmountPaid,
        string? PayMongoPaymentId,
        string CurrencyCode,
        bool IsAutoRefundEligible,
        RefundRequestStatus Status,
        string? RejectionReason,
        string? CustomerEwalletProvider,
        string? CustomerEwalletNumber,
        DateTime CreatedAt,
        DateTime DueDate
    );

    public record GetPendingRefundRequestsQuery(int PageNumber = 1, int PageSize = 10) : IQuery<QueryResult<RefundRequestSummaryDto>>;
}
```

(Note: `AmountPaid`/`PayMongoPaymentId`/`CustomerEwalletProvider`/`CustomerEwalletNumber`
are all added here now, ahead of Tasks 2 and 4, to keep this a single DTO
edit rather than three. The e-wallet fields are `null` until the customer
submits them via Task 4's `SubmitRefundEwalletDetailsCommand` — the review
page's Mark as Sent action (LocalFlow frontend plan Task 2) reads them to
show staff where to send the money.)

`GetPendingRefundRequestsHandler.cs` — full replacement. Note `DueDate` is
**not** a stored field on `RefundRequest` — it's computed here from
`tenant.PaymentPolicy.RefundReviewDeadlineDays`, exactly as the existing
handler already does; this replacement preserves that, just adds paging
and the two new DTO fields on top:

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests
{
    public class GetPendingRefundRequestsHandler
        : IQueryHandler<GetPendingRefundRequestsQuery, QueryResult<RefundRequestSummaryDto>>
    {
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;

        public GetPendingRefundRequestsHandler(
            IRefundRequestStore refundRequestStore,
            IUnitOfWork unitOfWork,
            IUserContextService userContext)
        {
            _refundRequestStore = refundRequestStore;
            _unitOfWork = unitOfWork;
            _userContext = userContext;
        }

        public async Task<QueryResult<RefundRequestSummaryDto>> Handle(
            GetPendingRefundRequestsQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _userContext.GetCurrentTenantId();
            var (requests, total) = await _refundRequestStore.GetPendingForTenantAsync(
                tenantId, query.PageNumber, query.PageSize, cancellationToken);

            if (requests.Count == 0)
                return new QueryResult<RefundRequestSummaryDto>(Array.Empty<RefundRequestSummaryDto>(), total);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Bookings, t => t.PaymentPolicy!]);

            var deadlineDays = tenant?.PaymentPolicy?.RefundReviewDeadlineDays ?? 7;

            var result = new List<RefundRequestSummaryDto>();
            foreach (var request in requests)
            {
                var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);
                var customer = booking is null
                    ? null
                    : await _unitOfWork.CustomerRepository.GetAsync(predicate: c => c.CustomerId == booking.CustomerId);

                result.Add(new RefundRequestSummaryDto(
                    request.Id,
                    request.BookingId,
                    booking?.BookingReference ?? "(unknown)",
                    customer?.Contact.Name ?? "(unknown)",
                    request.RequestedAmount,
                    booking?.AmountDue ?? request.RequestedAmount,
                    booking?.PayMongoPaymentId,
                    request.CurrencyCode,
                    request.IsAutoRefundEligible,
                    request.Status,
                    request.RejectionReason,
                    request.CustomerEwalletProvider,
                    request.CustomerEwalletNumber,
                    request.CreatedAt,
                    request.CreatedAt.AddDays(deadlineDays)));
            }

            return new QueryResult<RefundRequestSummaryDto>(result, total);
        }
    }
}
```

- [ ] **Step 3: Update the controller**

`RefundRequestsController.cs`, `GetPending` action:

```csharp
        [HttpGet]
        [Authorize(Policy = "ManagementOnly")]
        [ProducesResponseType(typeof(QueryResult<RefundRequestSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPending([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _mediator.Send(new GetPendingRefundRequestsQuery(pageNumber, pageSize));
            return Ok(result);
        }
```

- [ ] **Step 4: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Domain/Services/IRefundRequestStore.cs ApexBooking.Core.Persistence/Services/RefundRequestStore.cs ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/ ApexBooking.WebApi/Controllers/RefundRequestsController.cs
git commit -m "feat: paginate refund requests list, add payment-verification fields"
```

---

### Task 2: Mark as Sent

**Files:**
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/MarkManualRefundSent/MarkManualRefundSentCommand.cs`
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/MarkManualRefundSent/MarkManualRefundSentHandler.cs`
- Modify: `ApexBooking.WebApi/Controllers/RefundRequestsController.cs`

**Interfaces:**
- Consumes: `RefundRequest.MarkManuallyRefunded()` (Phase 1, currently unused), `Booking.RecordRefundOutcome(RefundStatus, decimal?)` (pass #1, existing).
- Produces: `POST /api/refund-requests/{id}/mark-sent`.

- [ ] **Step 1: Command**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.MarkManualRefundSent
{
    public record MarkManualRefundSentCommand(Guid RefundRequestId) : ICommand;
}
```

- [ ] **Step 2: Handler**

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.MarkManualRefundSent
{
    public class MarkManualRefundSentHandler : ICommandHandler<MarkManualRefundSentCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUserContextService _userContext;

        public MarkManualRefundSentHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _userContext = userContext;
        }

        public async Task Handle(MarkManualRefundSentCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            if (request.TenantId != _userContext.GetCurrentTenantId())
                throw new BusinessRuleBrokenException("Refund request not found.");

            request.MarkManuallyRefunded();
            await _refundRequestStore.UpdateAsync(request, cancellationToken);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == request.TenantId,
                includes: [t => t.Bookings]);

            var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);
            if (booking is null)
                return;

            booking.RecordRefundOutcome(RefundStatus.Succeeded, request.RequestedAmount);
            _unitOfWork.TenantRepository.Update(tenant!);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 3: Controller action**

Add to `RefundRequestsController.cs`:

```csharp
        [HttpPost("{id:guid}/mark-sent")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> MarkSent(Guid id)
        {
            await _mediator.Send(new MarkManualRefundSentCommand(id));
            return NoContent();
        }
```

Add the `using ApexBooking.Core.Application.Features.RefundRequests.Commands.MarkManualRefundSent;` import.

- [ ] **Step 4: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Application/Features/RefundRequests/Commands/MarkManualRefundSent/ ApexBooking.WebApi/Controllers/RefundRequestsController.cs
git commit -m "feat: add MarkManualRefundSent command"
```

---

### Task 3: `BusinessProfile.ContactPhoneNumber`

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/BusinessProfile.cs`
- Modify: `ApexBooking.Core.Persistence/Mappings/TenantConfiguration.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Commands/BusinessProfile/UpdateBusinessProfileCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Commands/BusinessProfile/UpdateBusinessProfileHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Queries/GetBusinessProfile/GetBusinessProfileQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Queries/GetBusinessProfile/GetBusinessProfileHandler.cs`

**Interfaces:**
- Produces: `BusinessProfile.ContactPhoneNumber` (string?), consumed by Task 5's `GetRefundStatusQuery`.

- [ ] **Step 1: Add the property and update `UpdateDetails`**

`BusinessProfile.cs`, add the property:

```csharp
        public string? Description { get; private set; }
        // Shown to customers on the public refund-status page — deliberately separate from
        // Tenant.OwnerContact's phone number, which is the owner's personal contact, not a
        // business-facing one.
        public string? ContactPhoneNumber { get; private set; }
```

Update `UpdateDetails`:

```csharp
        public void UpdateDetails(string name, string? description, string? logoUrl, string? contactPhoneNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleBrokenException("Business name is required.");

            BusinessName = name.Trim();
            Description = description?.Trim();
            Logo = logoUrl;
            ContactPhoneNumber = contactPhoneNumber?.Trim();
        }
```

- [ ] **Step 2: EF mapping**

`TenantConfiguration.cs`, inside the `OwnsOne(t => t.BusinessProfile, bp => {...})` block, after the `Description` mapping:

```csharp
            bp.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000);
            bp.Property(p => p.ContactPhoneNumber).HasColumnName("contact_phone_number").HasMaxLength(50);
```

- [ ] **Step 3: Command/handler/query**

`UpdateBusinessProfileCommand.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.BusinessProfile
{
    public record UpdateBusinessProfileCommand(string BusinessName, string? Description, string? LogoUrl, string? ContactPhoneNumber) : ICommand;
}
```

`UpdateBusinessProfileHandler.cs`, update the call:

```csharp
            tenant.BusinessProfile.UpdateDetails(command.BusinessName, command.Description, command.LogoUrl, command.ContactPhoneNumber);
```

`GetBusinessProfileQuery.cs`:

```csharp
    public record BusinessProfileDto(string BusinessName, string? Description, string? LogoUrl, BusinessType BusinessType, string? Slug, string? ContactPhoneNumber);
```

`GetBusinessProfileHandler.cs`, verified exact current construction — add the new trailing argument:

```csharp
            return new BusinessProfileDto(
                tenant.BusinessProfile.BusinessName,
                tenant.BusinessProfile.Description,
                tenant.BusinessProfile.Logo,
                tenant.BusinessProfile.BusinessType,
                tenant.Slug,
                tenant.BusinessProfile.ContactPhoneNumber
            );
```

- [ ] **Step 4: Build and generate migration**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

Run: `dotnet ef migrations add AddBusinessProfileContactPhoneNumber --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/BusinessProfile.cs ApexBooking.Core.Persistence/Mappings/TenantConfiguration.cs ApexBooking.Core.Application/Features/Tenancy/Commands/BusinessProfile/ ApexBooking.Core.Application/Features/Tenancy/Queries/GetBusinessProfile/ ApexBooking.Core.Persistence/Migrations/
git commit -m "feat: add BusinessProfile.ContactPhoneNumber"
```

---

### Task 4: Public refund-status endpoints

**Files:**
- Create: `ApexBooking.Core.Application/Features/PublicBookings/Queries/GetRefundStatus/GetRefundStatusQuery.cs`
- Create: `ApexBooking.Core.Application/Features/PublicBookings/Queries/GetRefundStatus/GetRefundStatusHandler.cs`
- Create: `ApexBooking.Core.Application/Features/PublicBookings/Commands/SubmitRefundEwalletDetails/SubmitRefundEwalletDetailsCommand.cs`
- Create: `ApexBooking.Core.Application/Features/PublicBookings/Commands/SubmitRefundEwalletDetails/SubmitRefundEwalletDetailsHandler.cs`
- Create: `ApexBooking.Core.Application/Common/Validators/SubmitRefundEwalletDetailsCommandValidator.cs`
- Create: `ApexBooking.WebApi/Controllers/RefundStatusController.cs`
- Modify: `ApexBooking.Core.Domain/Interfaces/IAppUrlService.cs`
- Modify: `ApexBooking.Infrastructure/ExternalServices/AppUrl/AppUrlService.cs`

**Interfaces:**
- Consumes: `ICancellationTokenService.TryValidate(token, out payload)` (existing — same mechanism `CancelBookingByTokenHandler` already uses), `IRefundRequestStore` (Phase 1).
- Produces: `IAppUrlService.GetRefundStatusUrl(string tenantSlug, string rawToken)`, consumed by Task 6.

- [ ] **Step 1: `IAppUrlService.GetRefundStatusUrl`**

Add to the interface, next to `GetCustomerCancellationUrl`:

```csharp
        string GetRefundStatusUrl(string tenantSlug, string rawToken);
```

In `AppUrlService.cs`, add the implementation right after `GetCustomerCancellationUrl` — verified exact pattern (uses `_appSettings.FrontendBaseUrl`, not a field named `_frontendBaseUrl`):

```csharp
    public string GetRefundStatusUrl(string tenantSlug, string rawToken)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return $"{base_}/{Uri.EscapeDataString(tenantSlug)}/refund-status?token={Uri.EscapeDataString(rawToken)}";
    }
```

- [ ] **Step 2: `IRefundRequestStore.GetByBookingIdAsync`**

`GetPendingForTenantAsync` only returns non-terminal requests (per its own
doc comment) — a `Succeeded`/`Rejected`/`ManuallyRefunded` request (most of
the end states a customer would actually be checking on) would never be
found through it. Add a dedicated lookup instead, same shape as the
existing `GetByIdAsync` but keyed by `BookingId`, unfiltered by status.

`IRefundRequestStore.cs`, add:

```csharp
    // Any status, terminal or not — for the customer-facing refund-status page, which needs to
    // show the *outcome*, not just what's still pending review.
    Task<RefundRequest?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
```

`RefundRequestStore.cs`, add:

```csharp
    public async Task<RefundRequest?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await _context.RefundRequests
            .IgnoreQueryFilters()
            .Where(r => r.BookingId == bookingId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
```

- [ ] **Step 3: `GetRefundStatusQuery` + handler**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus
{
    public record RefundStatusDto(
        string BookingReference,
        RefundRequestStatus? Status,
        decimal? Amount,
        string CurrencyCode,
        string? BusinessContactPhoneNumber,
        bool NeedsEwalletDetails
    );

    public record GetRefundStatusQuery(string Token) : IQuery<RefundStatusDto>;
}
```

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus
{
    public class GetRefundStatusHandler : IQueryHandler<GetRefundStatusQuery, RefundStatusDto>
    {
        private readonly ICancellationTokenService _cancellationTokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;

        public GetRefundStatusHandler(
            ICancellationTokenService cancellationTokenService,
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore)
        {
            _cancellationTokenService = cancellationTokenService;
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
        }

        public async Task<RefundStatusDto> Handle(GetRefundStatusQuery query, CancellationToken cancellationToken)
        {
            if (!_cancellationTokenService.TryValidate(query.Token, out var payload))
                throw new BusinessRuleBrokenException("This refund status link is invalid or could not be verified.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == payload.TenantId,
                includes: [t => t.Bookings, t => t.BusinessProfile!]);

            var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == payload.BookingId.Value)
                ?? throw new BusinessRuleBrokenException("This refund status link could not be resolved to a booking.");

            var request = await _refundRequestStore.GetByBookingIdAsync(payload.BookingId.Value, cancellationToken);

            return new RefundStatusDto(
                booking.BookingReference,
                request?.Status,
                request?.RequestedAmount ?? booking.RefundedAmount,
                booking.CurrencyCode,
                tenant?.BusinessProfile?.ContactPhoneNumber,
                request?.Status == RefundRequestStatus.AwaitingManualTransfer
            );
        }
    }
}
```

- [ ] **Step 4: `SubmitRefundEwalletDetailsCommand` + validator + handler**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Commands.SubmitRefundEwalletDetails
{
    public record SubmitRefundEwalletDetailsCommand(string Token, string Provider, string Number) : ICommand;
}
```

```csharp
using ApexBooking.Core.Application.Features.PublicBookings.Commands.SubmitRefundEwalletDetails;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class SubmitRefundEwalletDetailsCommandValidator : AbstractValidator<SubmitRefundEwalletDetailsCommand>
{
    public SubmitRefundEwalletDetailsCommandValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
    }
}
```

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Commands.SubmitRefundEwalletDetails
{
    public class SubmitRefundEwalletDetailsHandler : ICommandHandler<SubmitRefundEwalletDetailsCommand>
    {
        private readonly ICancellationTokenService _cancellationTokenService;
        private readonly IRefundRequestStore _refundRequestStore;

        public SubmitRefundEwalletDetailsHandler(
            ICancellationTokenService cancellationTokenService,
            IRefundRequestStore refundRequestStore)
        {
            _cancellationTokenService = cancellationTokenService;
            _refundRequestStore = refundRequestStore;
        }

        public async Task Handle(SubmitRefundEwalletDetailsCommand command, CancellationToken cancellationToken)
        {
            if (!_cancellationTokenService.TryValidate(command.Token, out var payload))
                throw new BusinessRuleBrokenException("This refund status link is invalid or could not be verified.");

            var request = await _refundRequestStore.GetByBookingIdAsync(payload.BookingId.Value, cancellationToken)
                ?? throw new BusinessRuleBrokenException("No refund request found for this booking.");

            request.RecordCustomerEwalletDetails(command.Provider, command.Number);
            await _refundRequestStore.UpdateAsync(request, cancellationToken);
        }
    }
}
```

- [ ] **Step 5: Controller**

Un-prefixed by design, matching `BookingsController`'s existing
`/api/public/bookings/cancel/{token}` (`GET`) and `/api/public/bookings/cancel`
(`POST`) actions exactly — the token resolves its own tenant, so no `{slug}`
segment is needed anywhere in the API path (the *frontend page URL* still
carries the slug for branding/routing; the API call underneath it doesn't
need to):

```csharp
using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.PublicBookings.Commands.SubmitRefundEwalletDetails;
using ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/public/refund-status")]
    [AllowAnonymous]
    public class RefundStatusController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RefundStatusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> Get(string token)
        {
            var result = await _mediator.Send(new GetRefundStatusQuery(token));
            return Ok(result);
        }

        [HttpPost("ewallet")]
        public async Task<IActionResult> SubmitEwalletDetails([FromBody] SubmitRefundEwalletDetailsBody body)
        {
            await _mediator.Send(new SubmitRefundEwalletDetailsCommand(body.Token, body.Provider, body.Number));
            return NoContent();
        }
    }

    public record SubmitRefundEwalletDetailsBody(string Token, string Provider, string Number);
}
```

- [ ] **Step 6: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 7: Commit**

```bash
git add ApexBooking.Core.Domain/Interfaces/IAppUrlService.cs ApexBooking.Infrastructure/ExternalServices/AppUrl/AppUrlService.cs ApexBooking.Core.Domain/Services/IRefundRequestStore.cs ApexBooking.Core.Persistence/Services/RefundRequestStore.cs ApexBooking.Core.Application/Features/PublicBookings/ ApexBooking.Core.Application/Common/Validators/SubmitRefundEwalletDetailsCommandValidator.cs ApexBooking.WebApi/Controllers/RefundStatusController.cs
git commit -m "feat: add public refund-status query and e-wallet submission command"
```

---

### Task 5: Rejection notice email

**Files:**
- Modify: `ApexBooking.Core.Domain/Events/BookingEvents.cs`
- Modify: `ApexBooking.Core.Domain/Entities/Booking.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Commands/ConfirmRefundRequest/ConfirmRefundRequestHandler.cs`
- Modify: `ApexBooking.Core.Domain/Services/Notification/Bookings/IBookingNotificationService.cs`
- Modify: `ApexBooking.Infrastructure/ExternalServices/BookingNotificationService/BookingNotificationService.cs`
- Create: `ApexBooking.Core.Application/Features/Bookings/Events/SendRefundRejectionEmailHandler.cs`

**Interfaces:**
- Produces: `BookingRefundRejectedDomainEvent`, `Booking.RejectReviewedRefund(string reason)` (signature change — was parameterless).

- [ ] **Step 1: New domain event**

Add to `BookingEvents.cs`, after `BookingRefundEligibleDomainEvent`:

```csharp
public record BookingRefundRejectedDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    string RejectionReason,
    DateTime OccurredAt
) : IReliableDomainEvent;
```

- [ ] **Step 2: Thread the reason through `RejectReviewedRefund`**

In `Booking.cs`, replace:

```csharp
        // Called when a refund review is rejected (Owner directly, or approving an Admin's
        // tentative rejection). No PayMongo call, no event — just the terminal status.
        public void RejectReviewedRefund()
        {
            RefundStatus = RefundStatus.Rejected;
            UpdatedAt = DateTime.UtcNow;
        }
```

with:

```csharp
        // Called when a refund review is rejected (Owner directly, or approving an Admin's
        // tentative rejection). No PayMongo call — but does raise a reliable event so the
        // customer gets told why, same as the automatic-refund path already notifies on outcome.
        public void RejectReviewedRefund(string reason)
        {
            RefundStatus = RefundStatus.Rejected;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BookingRefundRejectedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                RejectionReason: reason,
                OccurredAt: DateTime.UtcNow
            ));
        }
```

- [ ] **Step 3: Update the one caller**

In `ConfirmRefundRequestHandler.cs`'s `ApplyOutcomeAsync`:

```csharp
            else if (request.Status == RefundRequestStatus.Rejected)
                booking.RejectReviewedRefund(request.RejectionReason ?? "No reason provided.");
```

- [ ] **Step 4: Notification service + handler**

`IBookingNotificationService.cs`, add:

```csharp
        Task SendRefundRejectionEmailAsync(
            string to,
            string customerName,
            string businessName,
            string bookingReference,
            string rejectionReason,
            CancellationToken ct);
```

`BookingNotificationService.cs`, add the implementation, matching the
existing templates' exact structure (verified against
`SendBookingCancellationEmailAsync`'s real current markup this session):

```csharp
        public Task SendRefundRejectionEmailAsync(
            string to,
            string customerName,
            string businessName,
            string bookingReference,
            string rejectionReason,
            CancellationToken ct)
        {
            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #d33; padding-bottom: 10px;'>About Your Refund</h2>

                <p>Hi <strong>{customerName}</strong>,</p>

                <p>After review, the refund for your appointment with <strong>{businessName}</strong> was not approved.</p>

                <div style='background: #f8f9fa; border-left: 4px solid #d33; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0 0 8px 0; font-size: 14px; color: #555;'><strong>Appointment Tracking Reference:</strong></p>
                    <p style='margin: 0 0 12px 0; font-size: 18px; font-weight: bold; color: #d33; letter-spacing: 1px;'>{bookingReference}</p>
                    <p style='margin: 0; font-size: 14px; color: #555;'><strong>Reason:</strong></p>
                    <p style='margin: 5px 0 0 0;'>{rejectionReason}</p>
                </div>

                <p>If you have questions about this decision, please contact the business directly.</p>

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    Best regards,<br>
                    The Team at <strong>{businessName}</strong>
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: $"About your refund — {businessName}",
                content: body
            );
        }
```

`SendRefundRejectionEmailHandler.cs` (new file, `Features/Bookings/Events/`):

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    public class SendRefundRejectionEmailHandler
        : INotificationHandler<DomainEventNotification<BookingRefundRejectedDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly ILogger<SendRefundRejectionEmailHandler> _logger;

        public SendRefundRejectionEmailHandler(
            IUnitOfWork unitOfWork,
            IBookingNotificationService bookingNotificationService,
            ILogger<SendRefundRejectionEmailHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _bookingNotificationService = bookingNotificationService;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingRefundRejectedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.BusinessProfile!, t => t.Bookings]);

            if (tenant?.BusinessProfile is null)
            {
                _logger.LogError("Could not resolve workspace details for Tenant {TenantId}. Refund rejection email was aborted.", e.TenantId);
                return;
            }

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == e.BookingId);
            if (booking is null)
                return;

            var customer = await _unitOfWork.CustomerRepository.GetAsync(predicate: c => c.CustomerId == booking.CustomerId);
            if (customer?.Contact.Email is not { } customerEmail)
            {
                _logger.LogWarning("Customer {CustomerId} has no email on file. Refund rejection email for {BookingReference} was skipped.", booking.CustomerId.Value, e.BookingReference);
                return;
            }

            await _bookingNotificationService.SendRefundRejectionEmailAsync(
                to: customerEmail,
                customerName: customer.Contact.Name,
                businessName: tenant.BusinessProfile.BusinessName,
                bookingReference: e.BookingReference,
                rejectionReason: e.RejectionReason,
                ct: cancellationToken
            );
        }
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add ApexBooking.Core.Domain/Events/BookingEvents.cs ApexBooking.Core.Domain/Entities/Booking.cs ApexBooking.Core.Application/Features/RefundRequests/Commands/ConfirmRefundRequest/ConfirmRefundRequestHandler.cs ApexBooking.Core.Domain/Services/Notification/Bookings/IBookingNotificationService.cs ApexBooking.Infrastructure/ExternalServices/BookingNotificationService/BookingNotificationService.cs ApexBooking.Core.Application/Features/Bookings/Events/SendRefundRejectionEmailHandler.cs
git commit -m "feat: send a rejection notice email when a refund request is rejected"
```

---

### Task 6: Refund-status link on the cancellation email

**Files:**
- Modify: `ApexBooking.Core.Domain/Services/Notification/Bookings/IBookingNotificationService.cs`
- Modify: `ApexBooking.Infrastructure/ExternalServices/BookingNotificationService/BookingNotificationService.cs`
- Modify: `ApexBooking.Core.Application/Features/Bookings/Events/SendBookingCancellationEmailHandler.cs`

**Interfaces:**
- Consumes: `IAppUrlService.GetRefundStatusUrl` (Task 4), `ICancellationTokenService` (existing, already injected pattern from `SendBookingConfirmationEmailHandler`).

- [ ] **Step 1: Extend `SendBookingCancellationEmailAsync`**

`IBookingNotificationService.cs`:

```csharp
        Task SendBookingCancellationEmailAsync(
            string to,
            string customerName,
            string businessName,
            string serviceName,
            string bookingReference,
            string? refundNote,
            string? refundStatusUrl,
            CancellationToken ct);
```

`BookingNotificationService.cs`'s `SendBookingCancellationEmailAsync` — add a conditional link block after the existing `refundBlock`, following the exact same `string.IsNullOrWhiteSpace(...) ? string.Empty : $@"..."` pattern already used for `refundBlock`, linking `refundStatusUrl` with anchor text "Check your refund status."

- [ ] **Step 2: Update the handler**

`SendBookingCancellationEmailHandler.cs` — inject `ICancellationTokenService` and `IAppUrlService` (constructor + fields, same pattern `SendBookingConfirmationEmailHandler` already uses), and before the `SendBookingCancellationEmailAsync` call:

```csharp
            string? refundStatusUrl = null;
            if (booking.RefundStatus != RefundStatus.None)
            {
                var refundToken = _cancellationTokenService.Issue(new CancellationTokenPayload(booking.BookingId, e.TenantId));
                refundStatusUrl = _appUrlService.GetRefundStatusUrl(tenant.Slug ?? string.Empty, refundToken);
            }
```

then pass `refundStatusUrl: refundStatusUrl` as the new argument to `SendBookingCancellationEmailAsync`.

Also update the comment above the existing `refundNote` switch — it currently says `// Failed: not the customer's problem to see...` without mentioning `Rejected` (added after this handler was first written); add a `RefundStatus.Rejected` case to the switch too: `RefundStatus.Rejected => "This refund was not approved — see the link below for details."` (the dedicated rejection email from Task 5 carries the actual reason; this is just a pointer, not a duplicate of that content).

- [ ] **Step 3: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Domain/Services/Notification/Bookings/IBookingNotificationService.cs ApexBooking.Infrastructure/ExternalServices/BookingNotificationService/BookingNotificationService.cs ApexBooking.Core.Application/Features/Bookings/Events/SendBookingCancellationEmailHandler.cs
git commit -m "feat: link the refund-status page from the cancellation email"
```
