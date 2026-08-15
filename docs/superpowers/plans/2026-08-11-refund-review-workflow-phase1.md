# Refund Review Workflow — Phase 1 (Core State Machine) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the backend core of the refund review workflow from
[2026-08-11-refund-review-and-manual-confirmation-design.md](../specs/2026-08-11-refund-review-and-manual-confirmation-design.md):
the `AutomaticRefund` setting, the `RefundRequest` state machine, and the
Owner/Admin commands that drive it (Confirm/Reject/Owner-approve/Owner-deny).
Ends with a working, testable API surface — no customer-facing page, no
e-wallet capture, no emails (that's Phase 2, a separate plan).

**Architecture:** `RefundRequest` is a standalone `ITenantEntity` persistence
record (not an `IAggregateRoot`, no generic repository — same pattern as
`OutboxMessage`/`SmsUsage`), written through a dedicated
`IRefundRequestStore` (Domain interface, direct-`DbContext` Persistence
implementation). `Booking.Cancel`/`CancelByCustomer` gain one new branch:
when `PaymentPolicy.AutomaticRefund` is `false` (the default), a refund-
eligible cancellation raises `BookingRefundEligibleDomainEvent` instead of
`BookingRefundDueDomainEvent`. A new handler turns that into a
`RefundRequest`. The existing, already-working `BookingRefundDueDomainEvent`
→ outbox → PayMongo pipeline from pass #1 is reused unchanged — Owner
approval just becomes a second place that event can originate from.

**Tech Stack:** ASP.NET Core / EF Core / MediatR / FluentValidation / xUnit,
matching the existing solution exactly. No new packages.

## Global Constraints

- `RefundRequest` is not an `IAggregateRoot` — no repository, no generic CRUD; access only through `IRefundRequestStore` (per spec §"RefundRequest — new persistence record").
- Review page / all new commands: Owner and Admin only, never Staff (per spec decisions).
- Owner double-confirmation gate applies to both Confirm and Reject (per spec decisions).
- `AutomaticRefund` defaults to `false` — never assume `true` anywhere it isn't explicitly set (per spec decisions).
- Reuse `BookingRefundDueDomainEvent`/`ProcessRefundOnBookingCancelledHandler` unchanged — no new PayMongo-calling code (per spec "Non-goals").
- Every new EF migration is generated via `dotnet ef migrations add`, never hand-authored (matches every existing migration in this repo).

---

### Task 1: `PaymentPolicy.AutomaticRefund` setting, end to end

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/PaymentPolicy.cs`
- Modify: `ApexBooking.Core.Persistence/Mappings/PaymentPolicyConfiguration.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyHandler.cs`
- Test: `ApexBooking.Core.Domain.UnitTests/Entities/PaymentPolicyTests.cs` (new file)

**Interfaces:**
- Produces: `PaymentPolicy.AutomaticRefund` (bool, public get, private set), read by `Booking.Cancel`/`CancelByCustomer` in Task 5.
- Produces: `UpdatePaymentPolicyCommand(PaymentRequirementType, DepositType, decimal, decimal, bool AutomaticRefund)`.
- Produces: `PaymentPolicyDto(PaymentRequirementType, DepositType, decimal, decimal, bool AutomaticRefund)`.

- [ ] **Step 1: Write the failing domain test**

Create `ApexBooking.Core.Domain.UnitTests/Entities/PaymentPolicyTests.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using Xunit;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class PaymentPolicyTests
{
    [Fact]
    public void NewPolicy_DefaultsAutomaticRefundToFalse()
    {
        var policy = new PaymentPolicy(new TenantId(Guid.NewGuid()));

        Assert.False(policy.AutomaticRefund);
    }

    [Fact]
    public void UpdatePolicy_CanEnableAutomaticRefund()
    {
        var policy = new PaymentPolicy(new TenantId(Guid.NewGuid()));

        policy.UpdatePolicy(
            PaymentRequirementType.DepositRequired,
            DepositType.Percentage,
            depositValue: 50m,
            refundPercent: 100m,
            automaticRefund: true);

        Assert.True(policy.AutomaticRefund);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter PaymentPolicyTests`
Expected: FAIL — `PaymentPolicy` has no `AutomaticRefund` member, and `UpdatePolicy` has no `automaticRefund` parameter (compile error).

- [ ] **Step 3: Add the property and update `UpdatePolicy`**

In `ApexBooking.Core.Domain/Entities/PaymentPolicy.cs`, add the property next to `RefundPercent`:

```csharp
    public decimal RefundPercent { get; private set; }
    // No auto-refund unless a tenant explicitly opts in — default false so a fresh tenant
    // always starts on the human-review path (see the refund-review-workflow spec).
    public bool AutomaticRefund { get; private set; }
```

Update the constructor to initialize it:

```csharp
        RefundPercent = 0m;
        AutomaticRefund = false;
```

Update `UpdatePolicy`'s signature and body:

```csharp
    public void UpdatePolicy(
        PaymentRequirementType requirementType,
        DepositType depositType,
        decimal depositValue,
        decimal refundPercent,
        bool automaticRefund)
    {
        if (requirementType == PaymentRequirementType.None)
        {
            depositValue = 0m;
        }
        else
        {
            if (depositValue < 0)
                throw new BusinessRuleBrokenException("Deposit value cannot be a negative amount.");

            if (depositType == DepositType.Percentage && depositValue > 100)
                throw new BusinessRuleBrokenException("A percentage-based deposit requirement cannot exceed 100%.");
        }

        if (refundPercent < 0 || refundPercent > 100)
            throw new BusinessRuleBrokenException("Refund allowance parameters must sit strictly between 0% and 100%.");

        RequirementType = requirementType;
        DepositType = depositType;
        DepositValue = depositValue;
        RefundPercent = refundPercent;
        AutomaticRefund = automaticRefund;
        UpdatedAt = DateTime.UtcNow;
    }
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter PaymentPolicyTests`
Expected: PASS (2 tests)

- [ ] **Step 5: Map the column**

In `ApexBooking.Core.Persistence/Mappings/PaymentPolicyConfiguration.cs`, after the `RefundPercent` mapping:

```csharp
            builder.Property(p => p.RefundPercent)
                .HasColumnName("refund_percent")
                .HasPrecision(5, 2)
                .IsRequired();

            builder.Property(p => p.AutomaticRefund)
                .HasColumnName("automatic_refund")
                .HasDefaultValue(false)
                .IsRequired();
```

- [ ] **Step 6: Wire the command/handler/query/DTO**

`UpdatePaymentPolicyCommand.cs`:

```csharp
    public record UpdatePaymentPolicyCommand(
        PaymentRequirementType RequirementType,
        DepositType DepositType,
        decimal DepositValue,
        decimal RefundPercent,
        bool AutomaticRefund
    ) : ICommand;
```

`UpdatePaymentPolicyHandler.cs`, update the call:

```csharp
            tenant.PaymentPolicy.UpdatePolicy(
                command.RequirementType,
                command.DepositType,
                command.DepositValue,
                command.RefundPercent,
                command.AutomaticRefund
            );
```

`GetPaymentPolicyQuery.cs`:

```csharp
    public record PaymentPolicyDto(
        PaymentRequirementType RequirementType,
        DepositType DepositType,
        decimal DepositValue,
        decimal RefundPercent,
        bool AutomaticRefund
    );
```

`GetPaymentPolicyHandler.cs`, update the return:

```csharp
            return new PaymentPolicyDto(
                policy.RequirementType,
                policy.DepositType,
                policy.DepositValue,
                policy.RefundPercent,
                policy.AutomaticRefund
            );
```

- [ ] **Step 7: Build the full solution**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors (this will surface any other caller of `UpdatePolicy`/`PaymentPolicyDto`/`UpdatePaymentPolicyCommand` that needs the new argument — fix any that appear).

- [ ] **Step 8: Generate the migration**

Run: `dotnet ef migrations add AddAutomaticRefundToPaymentPolicy --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`
Expected: a new migration file adding the `automatic_refund` column to `payment_policy`. Do not apply it yet — later tasks add more schema changes; apply once at the end of Task 3.

- [ ] **Step 9: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/PaymentPolicy.cs ApexBooking.Core.Persistence/Mappings/PaymentPolicyConfiguration.cs ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyCommand.cs ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyHandler.cs ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyQuery.cs ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyHandler.cs ApexBooking.Core.Domain.UnitTests/Entities/PaymentPolicyTests.cs ApexBooking.Core.Persistence/Migrations/
git commit -m "feat: add PaymentPolicy.AutomaticRefund setting"
```

---

### Task 2: `Booking.PaymentMethodType` capture (prerequisite for eligibility)

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Booking.cs`
- Test: `ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs`

**Interfaces:**
- Produces: `Booking.PaymentMethodType` (string?, public get), consumed by Task 4's `CreateRefundRequestOnEligibleHandler` to set `RefundRequest.IsAutoRefundEligible`.
- Consumes: `Booking.ConfirmPayment(PaymentConfirmationMethod method, string? payMongoPaymentId = null)` (existing signature, gains one parameter — see below).

**Note before implementing:** the spec flags that the real webhook field name for
the payment method/source type (e.g. `"gcash"`, `"qrph"`) is **not yet confirmed** —
do not guess it. Before this task, capture one real sandbox webhook payload for a
paid GCash (or Maya) booking, same technique used earlier this session (breakpoint
on `jsonText` in `PayMongoWebhooksController.HandlePayMongoCallback`, or the
PayMongo dashboard's webhook event log), and find the actual attribute inside
`data.attributes.data.attributes.payments[0].data.attributes` that names the
payment method (likely something like `source.type` or `payment_method_used` —
confirm before writing the mapping). This task's test below treats
`paymentMethodType` as an opaque string the domain layer just stores — the
Infrastructure-layer wiring of the *real* field name is Task 4, once confirmed.

- [ ] **Step 1: Write the failing test**

In `ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs`, add:

```csharp
    [Fact]
    public void ConfirmPayment_StoresPaymentMethodType()
    {
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-03",
            scheduledDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            scheduledStartTime: TimeOnly.FromDateTime(DateTime.UtcNow),
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: true,
            currencyCode: "PHP",
            amountDue: 500m);

        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_test123", "gcash");

        Assert.Equal("gcash", booking.PaymentMethodType);
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter ConfirmPayment_StoresPaymentMethodType`
Expected: FAIL — compile error, `ConfirmPayment` doesn't accept a 3rd argument.

- [ ] **Step 3: Add the property and parameter**

In `ApexBooking.Core.Domain/Entities/Booking.cs`, next to `PayMongoPaymentId`:

```csharp
        public string? PayMongoPaymentId { get; private set; }
        // The PayMongo payment source/method type (e.g. "gcash", "qrph", "card"), captured from
        // the same webhook as PayMongoPaymentId. Drives RefundRequest.IsAutoRefundEligible — some
        // methods (QR Ph, confirmed 2026-08-11) can never be refunded via PayMongo's API at all.
        public string? PaymentMethodType { get; private set; }
```

Update `ConfirmPayment`:

```csharp
        public void ConfirmPayment(PaymentConfirmationMethod method, string? payMongoPaymentId = null, string? paymentMethodType = null)
        {
            if (Status != BookingStatus.PendingPayment)
                throw new BusinessRuleBrokenException("Only appointments pending payment can have their transactions verified.");

            Status = BookingStatus.Scheduled;
            PaymentConfirmedVia = method;
            PayMongoPaymentId = payMongoPaymentId;
            PaymentMethodType = paymentMethodType;
            UpdatedAt = DateTime.UtcNow;
```

(leave the rest of the method body unchanged)

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter ConfirmPayment_StoresPaymentMethodType`
Expected: PASS

- [ ] **Step 5: Build the full solution and fix other callers**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors. `ConfirmPayment`'s new parameter is optional (defaults to `null`), so existing callers (`ProcessPaymentWebhookCommandHandler`, `Tenant.RecordBookingArrival`'s fallback path) compile unchanged — this step just confirms that.

- [ ] **Step 6: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/Booking.cs ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs
git commit -m "feat: capture PaymentMethodType on Booking.ConfirmPayment"
```

---

### Task 3: `RefundRequest` entity, status enum, and EF mapping

**Files:**
- Create: `ApexBooking.Core.Domain/Enums/RefundRequestStatus.cs`
- Create: `ApexBooking.Core.Domain/Enums/RefundDecisionAction.cs`
- Create: `ApexBooking.Core.Domain/Entities/RefundRequest.cs`
- Create: `ApexBooking.Core.Persistence/Mappings/RefundRequestConfiguration.cs`
- Modify: `ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs`
- Modify: `ApexBooking.Core.Domain/Enums/RefundStatus.cs`
- Test: `ApexBooking.Core.Domain.UnitTests/Entities/RefundRequestTests.cs` (new file)

**Interfaces:**
- Produces: `RefundRequest` with factory `RefundRequest.Create(TenantId, Guid bookingId, decimal requestedAmount, string currencyCode, bool isAutoRefundEligible)` and transition methods `RecordTentativeDecision(Guid decidedByUserId, RefundDecisionAction action, string? rejectionReason)`, `ApplyOwnerApproval(Guid ownerUserId)`, `ApplyOwnerDenial()`, `ApplyDirectOwnerDecision(Guid ownerUserId, RefundDecisionAction action, string? rejectionReason)`, `MoveToManualTransfer()`, `MarkManuallyRefunded()`. Consumed by Tasks 5–7.
- Produces: `RefundRequestStatus` enum: `PendingReview`, `AwaitingOwnerApproval`, `Approved`, `Rejected`, `Processing`, `AwaitingManualTransfer`, `ManuallyRefunded`, `Succeeded`, `Failed`.
- Produces: `RefundDecisionAction` enum: `Confirm`, `Reject`.
- Produces: `RefundStatus.Rejected` (new value on the existing enum).

- [ ] **Step 1: Write the failing tests**

Create `ApexBooking.Core.Domain.UnitTests/Entities/RefundRequestTests.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;
using Xunit;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class RefundRequestTests
{
    private static RefundRequest CreatePendingReview(bool isAutoRefundEligible = true) =>
        RefundRequest.Create(new TenantId(Guid.NewGuid()), Guid.NewGuid(), 500m, "PHP", isAutoRefundEligible);

    [Fact]
    public void Create_StartsInPendingReview()
    {
        var request = CreatePendingReview();

        Assert.Equal(RefundRequestStatus.PendingReview, request.Status);
    }

    [Fact]
    public void RecordTentativeDecision_MovesToAwaitingOwnerApproval()
    {
        var request = CreatePendingReview();
        var adminId = Guid.NewGuid();

        request.RecordTentativeDecision(adminId, RefundDecisionAction.Confirm, rejectionReason: null);

        Assert.Equal(RefundRequestStatus.AwaitingOwnerApproval, request.Status);
        Assert.Equal(adminId, request.DecidedByUserId);
        Assert.Equal(RefundDecisionAction.Confirm, request.DecisionAction);
    }

    [Fact]
    public void ApplyOwnerApproval_OnAutoEligibleConfirm_MovesToApproved()
    {
        var request = CreatePendingReview(isAutoRefundEligible: true);
        request.RecordTentativeDecision(Guid.NewGuid(), RefundDecisionAction.Confirm, rejectionReason: null);
        var ownerId = Guid.NewGuid();

        request.ApplyOwnerApproval(ownerId);

        Assert.Equal(RefundRequestStatus.Approved, request.Status);
        Assert.Equal(ownerId, request.OwnerDecidedByUserId);
    }

    [Fact]
    public void ApplyOwnerApproval_OnRejectDecision_MovesToRejected()
    {
        var request = CreatePendingReview();
        request.RecordTentativeDecision(Guid.NewGuid(), RefundDecisionAction.Reject, rejectionReason: "Customer no-showed twice");

        request.ApplyOwnerApproval(Guid.NewGuid());

        Assert.Equal(RefundRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public void ApplyOwnerDenial_ReopensToPendingReview()
    {
        var request = CreatePendingReview();
        request.RecordTentativeDecision(Guid.NewGuid(), RefundDecisionAction.Confirm, rejectionReason: null);

        request.ApplyOwnerDenial();

        Assert.Equal(RefundRequestStatus.PendingReview, request.Status);
        Assert.Null(request.DecidedByUserId);
        Assert.Null(request.DecisionAction);
    }

    [Fact]
    public void ApplyDirectOwnerDecision_Confirm_NotAutoEligible_MovesToAwaitingManualTransfer()
    {
        var request = CreatePendingReview(isAutoRefundEligible: false);

        request.ApplyDirectOwnerDecision(Guid.NewGuid(), RefundDecisionAction.Confirm, rejectionReason: null);

        Assert.Equal(RefundRequestStatus.AwaitingManualTransfer, request.Status);
    }

    [Fact]
    public void MarkManuallyRefunded_FromAwaitingManualTransfer_Succeeds()
    {
        var request = CreatePendingReview(isAutoRefundEligible: false);
        request.ApplyDirectOwnerDecision(Guid.NewGuid(), RefundDecisionAction.Confirm, rejectionReason: null);

        request.MarkManuallyRefunded();

        Assert.Equal(RefundRequestStatus.ManuallyRefunded, request.Status);
    }

    [Fact]
    public void RecordTentativeDecision_WhenNotPendingReview_Throws()
    {
        var request = CreatePendingReview();
        request.RecordTentativeDecision(Guid.NewGuid(), RefundDecisionAction.Confirm, rejectionReason: null);

        Assert.Throws<BusinessRuleBrokenException>(() =>
            request.RecordTentativeDecision(Guid.NewGuid(), RefundDecisionAction.Confirm, rejectionReason: null));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter RefundRequestTests`
Expected: FAIL — compile errors, none of these types exist yet.

- [ ] **Step 3: Create the enums**

`ApexBooking.Core.Domain/Enums/RefundRequestStatus.cs`:

```csharp
namespace ApexBooking.Core.Domain.Enums;

public enum RefundRequestStatus
{
    PendingReview,
    AwaitingOwnerApproval,
    Approved,
    Rejected,
    Processing,
    AwaitingManualTransfer,
    ManuallyRefunded,
    Succeeded,
    Failed
}
```

`ApexBooking.Core.Domain/Enums/RefundDecisionAction.cs`:

```csharp
namespace ApexBooking.Core.Domain.Enums;

public enum RefundDecisionAction
{
    Confirm,
    Reject
}
```

Add to `ApexBooking.Core.Domain/Enums/RefundStatus.cs`:

```csharp
public enum RefundStatus
{
    None,
    Pending,
    Processing,
    Succeeded,
    Failed,
    Rejected
}
```

- [ ] **Step 4: Create the `RefundRequest` entity**

`ApexBooking.Core.Domain/Entities/RefundRequest.cs`:

```csharp
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

// Persistence record, not an aggregate root — same pattern as OutboxMessage/SmsUsage. Queried
// and written only through IRefundRequestStore, never a generic repository.
public class RefundRequest : ITenantEntity
{
    public Guid Id { get; private set; }
    public TenantId? TenantId { get; private set; }
    public Guid BookingId { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public bool IsAutoRefundEligible { get; private set; }

    public RefundRequestStatus Status { get; private set; }

    public Guid? DecidedByUserId { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public RefundDecisionAction? DecisionAction { get; private set; }
    public string? RejectionReason { get; private set; }

    public Guid? OwnerDecidedByUserId { get; private set; }
    public DateTime? OwnerDecidedAt { get; private set; }

    public string? CustomerEwalletProvider { get; private set; }
    public string? CustomerEwalletNumber { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected RefundRequest() { }

    public static RefundRequest Create(
        TenantId tenantId,
        Guid bookingId,
        decimal requestedAmount,
        string currencyCode,
        bool isAutoRefundEligible)
    {
        if (requestedAmount <= 0)
            throw new BusinessRuleBrokenException("Refund request amount must be greater than zero.");

        return new RefundRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BookingId = bookingId,
            RequestedAmount = requestedAmount,
            CurrencyCode = currencyCode,
            IsAutoRefundEligible = isAutoRefundEligible,
            Status = RefundRequestStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Admin path: the decision doesn't take effect yet — it waits for the Owner.
    public void RecordTentativeDecision(Guid decidedByUserId, RefundDecisionAction action, string? rejectionReason)
    {
        if (Status != RefundRequestStatus.PendingReview)
            throw new BusinessRuleBrokenException("This refund request is no longer awaiting an initial decision.");

        if (action == RefundDecisionAction.Reject && string.IsNullOrWhiteSpace(rejectionReason))
            throw new BusinessRuleBrokenException("A reason is required when rejecting a refund request.");

        DecidedByUserId = decidedByUserId;
        DecisionAction = action;
        RejectionReason = rejectionReason;
        DecidedAt = DateTime.UtcNow;
        Status = RefundRequestStatus.AwaitingOwnerApproval;
        UpdatedAt = DateTime.UtcNow;
    }

    // Owner path: the decision takes effect immediately, no gate.
    public void ApplyDirectOwnerDecision(Guid ownerUserId, RefundDecisionAction action, string? rejectionReason)
    {
        if (Status != RefundRequestStatus.PendingReview)
            throw new BusinessRuleBrokenException("This refund request is no longer awaiting an initial decision.");

        if (action == RefundDecisionAction.Reject && string.IsNullOrWhiteSpace(rejectionReason))
            throw new BusinessRuleBrokenException("A reason is required when rejecting a refund request.");

        DecidedByUserId = ownerUserId;
        DecisionAction = action;
        RejectionReason = rejectionReason;
        DecidedAt = DateTime.UtcNow;
        OwnerDecidedByUserId = ownerUserId;
        OwnerDecidedAt = DateTime.UtcNow;
        ApplyDecision(action);
    }

    // Owner approves an Admin's tentative decision.
    public void ApplyOwnerApproval(Guid ownerUserId)
    {
        if (Status != RefundRequestStatus.AwaitingOwnerApproval)
            throw new BusinessRuleBrokenException("This refund request is not awaiting owner approval.");

        OwnerDecidedByUserId = ownerUserId;
        OwnerDecidedAt = DateTime.UtcNow;
        ApplyDecision(DecisionAction!.Value);
    }

    // Owner declines an Admin's tentative decision — reopen for reconsideration rather than
    // leaving it stuck forever.
    public void ApplyOwnerDenial()
    {
        if (Status != RefundRequestStatus.AwaitingOwnerApproval)
            throw new BusinessRuleBrokenException("This refund request is not awaiting owner approval.");

        DecidedByUserId = null;
        DecisionAction = null;
        RejectionReason = null;
        DecidedAt = null;
        Status = RefundRequestStatus.PendingReview;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyDecision(RefundDecisionAction action)
    {
        Status = action switch
        {
            RefundDecisionAction.Reject => RefundRequestStatus.Rejected,
            RefundDecisionAction.Confirm when IsAutoRefundEligible => RefundRequestStatus.Approved,
            RefundDecisionAction.Confirm => RefundRequestStatus.AwaitingManualTransfer,
            _ => throw new BusinessRuleBrokenException("Unrecognized refund decision action.")
        };
        UpdatedAt = DateTime.UtcNow;
    }

    // Called once BookingRefundDueDomainEvent has been raised for an auto-eligible Approved
    // request (see CreateRefundRequestOnEligibleHandler's counterpart in the Application layer).
    public void MarkProcessing()
    {
        if (Status != RefundRequestStatus.Approved)
            throw new BusinessRuleBrokenException("Only an approved refund request can move to processing.");

        Status = RefundRequestStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSucceeded()
    {
        Status = RefundRequestStatus.Succeeded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = RefundRequestStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    // Customer-submitted via the public refund-status page (Phase 2) — captured here so staff
    // can see it on the review page before sending the manual transfer.
    public void RecordCustomerEwalletDetails(string provider, string number)
    {
        if (Status != RefundRequestStatus.AwaitingManualTransfer)
            throw new BusinessRuleBrokenException("E-wallet details can only be submitted while a manual transfer is pending.");

        CustomerEwalletProvider = provider;
        CustomerEwalletNumber = number;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkManuallyRefunded()
    {
        if (Status != RefundRequestStatus.AwaitingManualTransfer)
            throw new BusinessRuleBrokenException("Only a request awaiting manual transfer can be marked as manually refunded.");

        Status = RefundRequestStatus.ManuallyRefunded;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter RefundRequestTests`
Expected: PASS (8 tests)

- [ ] **Step 6: EF mapping**

`ApexBooking.Core.Persistence/Mappings/RefundRequestConfiguration.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.ToTable("refund_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.TenantId)
            .HasConversion(id => id!.Value, v => new TenantId(v))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(r => r.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(r => r.RequestedAmount).HasColumnName("requested_amount").HasPrecision(12, 2).IsRequired();
        builder.Property(r => r.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(r => r.IsAutoRefundEligible).HasColumnName("is_auto_refund_eligible").IsRequired();

        builder.Property(r => r.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(30).IsRequired();

        builder.Property(r => r.DecidedByUserId).HasColumnName("decided_by_user_id");
        builder.Property(r => r.DecidedAt).HasColumnName("decided_at");
        builder.Property(r => r.DecisionAction).HasConversion<string>().HasColumnName("decision_action").HasMaxLength(20);
        builder.Property(r => r.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);

        builder.Property(r => r.OwnerDecidedByUserId).HasColumnName("owner_decided_by_user_id");
        builder.Property(r => r.OwnerDecidedAt).HasColumnName("owner_decided_at");

        builder.Property(r => r.CustomerEwalletProvider).HasColumnName("customer_ewallet_provider").HasMaxLength(50);
        builder.Property(r => r.CustomerEwalletNumber).HasColumnName("customer_ewallet_number").HasMaxLength(50);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(r => r.BookingId);
        builder.HasIndex(r => new { r.TenantId, r.Status });
    }
}
```

- [ ] **Step 7: Register the `DbSet`**

In `ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs`, next to `public DbSet<SmsUsage> SmsUsages => Set<SmsUsage>();`:

```csharp
        public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
```

- [ ] **Step 8: Build, then generate and apply the migrations**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

Run: `dotnet ef migrations add AddRefundRequests --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`
Expected: a new migration creating the `refund_requests` table.

Run: `dotnet ef database update --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`
Expected: applies both this migration and Task 1's `AddAutomaticRefundToPaymentPolicy` (still pending) to the dev database. Confirm no errors.

- [ ] **Step 9: Commit**

```bash
git add ApexBooking.Core.Domain/Enums/RefundRequestStatus.cs ApexBooking.Core.Domain/Enums/RefundDecisionAction.cs ApexBooking.Core.Domain/Enums/RefundStatus.cs ApexBooking.Core.Domain/Entities/RefundRequest.cs ApexBooking.Core.Persistence/Mappings/RefundRequestConfiguration.cs ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs ApexBooking.Core.Domain.UnitTests/Entities/RefundRequestTests.cs ApexBooking.Core.Persistence/Migrations/
git commit -m "feat: add RefundRequest entity and state machine"
```

---

### Task 4: `IRefundRequestStore`

**Files:**
- Create: `ApexBooking.Core.Domain/Services/IRefundRequestStore.cs`
- Create: `ApexBooking.Core.Persistence/Services/RefundRequestStore.cs`
- Modify: `ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs`
- Test: `ApexBooking.Core.Domain.UnitTests` — skipped here; this is a thin DbContext wrapper with no branching logic of its own (same as `OutboxStore`, which also has no dedicated unit test in this codebase — it's exercised indirectly through the handlers that use it, covered in Tasks 5–7).

**Interfaces:**
- Produces: `IRefundRequestStore` with `AddAsync(RefundRequest, CancellationToken)`, `GetByIdAsync(Guid, CancellationToken)`, `GetPendingForTenantAsync(TenantId, CancellationToken)`, `UpdateAsync(RefundRequest, CancellationToken)`. Consumed by Tasks 5–7.

- [ ] **Step 1: Create the interface**

`ApexBooking.Core.Domain/Services/IRefundRequestStore.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services;

// Data access port for RefundRequest rows — same shape as IOutboxStore/ISmsQuotaService:
// RefundRequest is not an IAggregateRoot, so there's no generic repository for it.
public interface IRefundRequestStore
{
    Task AddAsync(RefundRequest request, CancellationToken cancellationToken = default);

    Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Everything not yet in a terminal state (Rejected/ManuallyRefunded/Succeeded/Failed) — the
    // review page's list.
    Task<IReadOnlyList<RefundRequest>> GetPendingForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefundRequest request, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Implement it**

`ApexBooking.Core.Persistence/Services/RefundRequestStore.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Services;

public sealed class RefundRequestStore : IRefundRequestStore
{
    private static readonly RefundRequestStatus[] TerminalStatuses =
    [
        RefundRequestStatus.Rejected,
        RefundRequestStatus.ManuallyRefunded,
        RefundRequestStatus.Succeeded,
        RefundRequestStatus.Failed
    ];

    private readonly ApexBookingDbContext _context;

    public RefundRequestStore(ApexBookingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        _context.RefundRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RefundRequests
            .IgnoreQueryFilters() // caller-supplied id is already tenant-scoped by the handler's own auth check
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RefundRequest>> GetPendingForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.RefundRequests
            .Where(r => r.TenantId == tenantId && !TerminalStatuses.Contains(r.Status))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        _context.RefundRequests.Update(request);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 3: Register in DI**

In `ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs`, next to `services.AddScoped<IOutboxStore, Services.OutboxStore>();`:

```csharp
            services.AddScoped<IRefundRequestStore, Services.RefundRequestStore>();
```

- [ ] **Step 4: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Domain/Services/IRefundRequestStore.cs ApexBooking.Core.Persistence/Services/RefundRequestStore.cs ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs
git commit -m "feat: add IRefundRequestStore"
```

---

### Task 5: `Booking` gates on `AutomaticRefund`; new domain event

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Booking.cs`
- Modify: `ApexBooking.Core.Domain/Events/BookingEvents.cs`
- Modify: `ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs`

**Interfaces:**
- Produces: `BookingRefundEligibleDomainEvent(TenantId, Guid BookingId, string BookingReference, decimal RefundAmount, string CurrencyCode, DateTime OccurredAt) : IDomainEvent`. Consumed by Task 6's `CreateRefundRequestOnEligibleHandler`.
- Consumes: `PaymentPolicy.AutomaticRefund` (Task 1).

- [ ] **Step 1: Update existing tests to reflect the new default (they currently pass `paymentPolicy: null`, which now means "not automatic")**

In `ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs`, `Cancel_OnTime_RaisesFullRefund` currently asserts a `BookingRefundDueDomainEvent`. Replace its body:

```csharp
    [Fact]
    public void Cancel_OnTime_AutomaticRefundEnabled_RaisesFullRefundImmediately()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = new PaymentPolicy(booking.TenantId);
        paymentPolicy.UpdatePolicy(PaymentRequirementType.None, DepositType.Percentage, 0m, refundPercent: 0m, automaticRefund: true);

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy);

        Assert.Equal(RefundStatus.Pending, booking.RefundStatus);
        var refundEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundDueDomainEvent>());
        Assert.Equal(500m, refundEvent.RefundAmount);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
    }

    [Fact]
    public void Cancel_OnTime_AutomaticRefundDisabled_RaisesEligibleForReviewInstead()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = new PaymentPolicy(booking.TenantId); // AutomaticRefund defaults false

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy);

        Assert.Equal(RefundStatus.Pending, booking.RefundStatus);
        var eligibleEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
        Assert.Equal(500m, eligibleEvent.RefundAmount);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundDueDomainEvent>());
    }

    [Fact]
    public void Cancel_OnTime_NullPaymentPolicy_RaisesEligibleForReview()
    {
        // No PaymentPolicy configured at all defensively behaves the same as AutomaticRefund=false
        // — never assume automatic when the policy can't be read.
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy: null);

        Assert.Single(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundDueDomainEvent>());
    }
```

Update `Cancel_PastCutoff_PartialRefundPolicy_RaisesPercentageAmount` (it builds a real `PaymentPolicy` already) to assert the eligible event instead, since it doesn't set `automaticRefund: true`:

```csharp
        booking.Cancel(Guid.NewGuid(), "Late cancel", bookingPolicy, paymentPolicy);

        var eligibleEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
        Assert.Equal(250m, eligibleEvent.RefundAmount);
```

Also update its `paymentPolicy.UpdatePolicy(...)` call to pass the new parameter:

```csharp
        paymentPolicy.UpdatePolicy(PaymentRequirementType.DepositRequired, DepositType.Percentage, 50m, refundPercent: 50m, automaticRefund: false);
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter BookingRefundTests`
Expected: FAIL — compile errors (`BookingRefundEligibleDomainEvent` doesn't exist, `UpdatePolicy` missing 5th arg).

- [ ] **Step 3: Add the new domain event**

In `ApexBooking.Core.Domain/Events/BookingEvents.cs`, after `BookingRefundDueDomainEvent`:

```csharp
// Raised instead of BookingRefundDueDomainEvent when the tenant has NOT opted into
// PaymentPolicy.AutomaticRefund — a refund-eligible cancellation waits for a human decision
// before any PayMongo call happens. Plain IDomainEvent (synchronous, no external call — just a
// DB write), same class as BookingCreatedDomainEvent.
public record BookingRefundEligibleDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    decimal RefundAmount,
    string CurrencyCode,
    DateTime OccurredAt
) : IDomainEvent;
```

- [ ] **Step 4: Gate `Cancel` and `CancelByCustomer`**

In `ApexBooking.Core.Domain/Entities/Booking.cs`, replace the refund-raising block at the end of `Cancel`:

```csharp
            var (shouldRefund, refundAmount) = EvaluateRefund(bookingPolicy, paymentPolicy);
            if (shouldRefund)
            {
                RefundStatus = RefundStatus.Pending;
                if (paymentPolicy?.AutomaticRefund == true)
                {
                    AddDomainEvent(new BookingRefundDueDomainEvent(
                        TenantId: this.TenantId,
                        BookingId: this.BookingId.Value,
                        BookingReference: this.BookingReference,
                        RefundAmount: refundAmount,
                        CurrencyCode: this.CurrencyCode,
                        OccurredAt: CancelledAt.Value
                    ));
                }
                else
                {
                    AddDomainEvent(new BookingRefundEligibleDomainEvent(
                        TenantId: this.TenantId,
                        BookingId: this.BookingId.Value,
                        BookingReference: this.BookingReference,
                        RefundAmount: refundAmount,
                        CurrencyCode: this.CurrencyCode,
                        OccurredAt: CancelledAt.Value
                    ));
                }
            }
```

Apply the identical replacement to the equivalent block at the end of `CancelByCustomer` (same code, same condition — `CancelledAt.Value` is set in both methods).

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter BookingRefundTests`
Expected: PASS (all tests in the file, including the 3 new ones and the updated partial-refund test)

- [ ] **Step 6: Run the full Domain test suite**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests`
Expected: PASS — confirms nothing else in the Domain layer broke.

- [ ] **Step 7: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/Booking.cs ApexBooking.Core.Domain/Events/BookingEvents.cs ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs
git commit -m "feat: gate Booking refund events on PaymentPolicy.AutomaticRefund"
```

---

### Task 6: `CreateRefundRequestOnEligibleHandler` + tenant notification

**Files:**
- Create: `ApexBooking.Core.Application/Features/Bookings/Events/CreateRefundRequestOnEligibleHandler.cs`
- Modify: `ApexBooking.Core.Domain/Enums/NotificationEventType.cs`

**Interfaces:**
- Consumes: `IRefundRequestStore.AddAsync` (Task 4), `RefundRequest.Create` (Task 3), `Booking.PaymentMethodType` (Task 2), `BookingRefundEligibleDomainEvent` (Task 5).
- Produces: a persisted `RefundRequest` row + `NotificationEventType.RefundReviewNeeded` bell notification, per booking cancellation.

- [ ] **Step 1: Add the notification event type**

In `ApexBooking.Core.Domain/Enums/NotificationEventType.cs`, after `RefundFailed`:

```csharp
    RefundSucceeded,
    RefundFailed,
    RefundReviewNeeded,
```

- [ ] **Step 2: Write the handler**

`ApexBooking.Core.Application/Features/Bookings/Events/CreateRefundRequestOnEligibleHandler.cs`:

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    // Subscribes to BookingRefundEligibleDomainEvent — a plain IDomainEvent, so this runs
    // synchronously, same request as the cancellation itself (no external call involved, just a
    // DB write + a bell notification).
    public class CreateRefundRequestOnEligibleHandler
        : INotificationHandler<DomainEventNotification<BookingRefundEligibleDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<CreateRefundRequestOnEligibleHandler> _logger;

        public CreateRefundRequestOnEligibleHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<CreateRefundRequestOnEligibleHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingRefundEligibleDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.Bookings, t => t.Members]);

            if (tenant is null)
            {
                _logger.LogError(
                    "Could not resolve Tenant {TenantId} to create a RefundRequest for Booking {BookingReference}.",
                    e.TenantId, e.BookingReference);
                return;
            }

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == e.BookingId);

            var request = RefundRequest.Create(
                e.TenantId,
                e.BookingId,
                e.RefundAmount,
                e.CurrencyCode,
                isAutoRefundEligible: booking?.PaymentMethodType != "qrph");

            await _refundRequestStore.AddAsync(request, cancellationToken);

            var recipients = tenant.Members.Where(m => m.Role == SystemRole.Owner || m.Role == SystemRole.Admin);
            var notifications = recipients
                .Select(m => Notification.Create(
                    m.UserId,
                    NotificationRecipientType.TenantAdmin,
                    e.TenantId,
                    NotificationEventType.RefundReviewNeeded,
                    "Refund Review Needed",
                    $"Booking {e.BookingReference} was cancelled and is eligible for a refund of {e.RefundAmount:0.00} {e.CurrencyCode}. Please review it."))
                .ToList();

            foreach (var n in notifications)
                _unitOfWork.NotificationRepository.Add(n);

            await _unitOfWork.CompleteAsync(cancellationToken);

            if (notifications.Count > 0)
                await _realtimeDispatcher.PushAsync(notifications, cancellationToken);
        }
    }
}
```

**Note on `isAutoRefundEligible: booking?.PaymentMethodType != "qrph"`:** this hardcodes the one
confirmed-non-refundable method (QR Ph) as a denylist rather than an allowlist, so a payment
method PayMongo adds refund support for later doesn't need a code change here. Once Task 2's
prerequisite webhook capture confirms the real field value PayMongo sends for QR Ph (this plan
assumed `"qrph"` — verify against the actual captured payload before merging), update the
literal to match exactly.

- [ ] **Step 3: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 4: Manual verification (no unit test — this handler's only branching logic, the QR Ph denylist check, is a one-line string comparison; its DB/notification wiring is better covered by the integration-style test in Task 7, which exercises the full Confirm flow end to end)**

Run the WebApi locally, cancel a refund-eligible test booking with `PaymentPolicy.AutomaticRefund = false` (the default), and confirm via `SELECT * FROM refund_requests` that a `PendingReview` row was created, and that the Owner/Admin accounts received a `RefundReviewNeeded` notification.

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Application/Features/Bookings/Events/CreateRefundRequestOnEligibleHandler.cs ApexBooking.Core.Domain/Enums/NotificationEventType.cs
git commit -m "feat: create RefundRequest and notify tenant on refund-eligible cancellation"
```

---

### Task 7: `Booking` gains the two review-outcome methods

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Booking.cs`
- Modify: `ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs`

**Interfaces:**
- Produces: `Booking.ApproveReviewedRefund(decimal amount)` — raises `BookingRefundDueDomainEvent`, reusing pass #1's existing pipeline unchanged. `Booking.RejectReviewedRefund()` — sets `RefundStatus = RefundStatus.Rejected`. Both `public` (called from the Application layer, a different assembly, same reasoning as the existing `RecordRefundOutcome`). Consumed by Task 8.

- [ ] **Step 1: Write the failing tests**

Add to `ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs`:

```csharp
    [Fact]
    public void ApproveReviewedRefund_RaisesBookingRefundDueDomainEvent()
    {
        var booking = CreateOnlinePaidBooking(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy: null);
        booking.ClearDomainEvents(); // isolate to the approval call itself

        booking.ApproveReviewedRefund(500m);

        var refundEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundDueDomainEvent>());
        Assert.Equal(500m, refundEvent.RefundAmount);
    }

    [Fact]
    public void RejectReviewedRefund_SetsRefundStatusRejected()
    {
        var booking = CreateOnlinePaidBooking(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy: null);

        booking.RejectReviewedRefund();

        Assert.Equal(RefundStatus.Rejected, booking.RefundStatus);
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter "ApproveReviewedRefund_RaisesBookingRefundDueDomainEvent|RejectReviewedRefund_SetsRefundStatusRejected"`
Expected: FAIL — compile error, methods don't exist.

- [ ] **Step 3: Add the methods**

In `ApexBooking.Core.Domain/Entities/Booking.cs`, after `RecordRefundOutcome`:

```csharp
        // Called by the refund-review Application-layer handlers once an Owner (directly, or via
        // approving an Admin's tentative decision) confirms a refund that was previously deferred
        // for review — raises the exact same event pass #1's automatic path raises, so
        // ProcessRefundOnBookingCancelledHandler's PayMongo call is reused unchanged.
        public void ApproveReviewedRefund(decimal amount)
        {
            AddDomainEvent(new BookingRefundDueDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                RefundAmount: amount,
                CurrencyCode: this.CurrencyCode,
                OccurredAt: DateTime.UtcNow
            ));
            UpdatedAt = DateTime.UtcNow;
        }

        // Called when a refund review is rejected (Owner directly, or approving an Admin's
        // tentative rejection). No PayMongo call, no event — just the terminal status.
        public void RejectReviewedRefund()
        {
            RefundStatus = RefundStatus.Rejected;
            UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter "ApproveReviewedRefund_RaisesBookingRefundDueDomainEvent|RejectReviewedRefund_SetsRefundStatusRejected"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/Booking.cs ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs
git commit -m "feat: add Booking.ApproveReviewedRefund/RejectReviewedRefund"
```

---

### Task 8: Confirm/Reject commands (Owner-direct and Admin-tentative)

**Files:**
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/ConfirmRefundRequest/ConfirmRefundRequestCommand.cs`
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/ConfirmRefundRequest/ConfirmRefundRequestHandler.cs`
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/RejectRefundRequest/RejectRefundRequestCommand.cs`
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/RejectRefundRequest/RejectRefundRequestHandler.cs`
- Create: `ApexBooking.Core.Application/Common/Validators/RejectRefundRequestCommandValidator.cs`

**Interfaces:**
- Consumes: `IRefundRequestStore` (Task 4), `RefundRequest.RecordTentativeDecision`/`ApplyDirectOwnerDecision` (Task 3), `Booking.ApproveReviewedRefund`/`RejectReviewedRefund` (Task 7), `IUserContextService.GetUserRole()`/`GetCurrentUserId()`, `NotificationEventType.RefundApprovalNeeded` (new, this task).
- Produces: `ConfirmRefundRequestCommand(Guid RefundRequestId) : ICommand`, `RejectRefundRequestCommand(Guid RefundRequestId, string Reason) : ICommand`. Consumed by Task 10's controller.

- [ ] **Step 1: Add the second new notification type**

In `ApexBooking.Core.Domain/Enums/NotificationEventType.cs`, after `RefundReviewNeeded`:

```csharp
    RefundReviewNeeded,
    RefundApprovalNeeded,
```

- [ ] **Step 2: `ConfirmRefundRequestCommand`**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest
{
    public record ConfirmRefundRequestCommand(Guid RefundRequestId) : ICommand;
}
```

- [ ] **Step 3: `ConfirmRefundRequestHandler`**

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest
{
    public class ConfirmRefundRequestHandler : ICommandHandler<ConfirmRefundRequestCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUserContextService _userContext;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;

        public ConfirmRefundRequestHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IUserContextService userContext,
            IRealtimeNotificationDispatcher realtimeDispatcher)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _userContext = userContext;
            _realtimeDispatcher = realtimeDispatcher;
        }

        public async Task Handle(ConfirmRefundRequestCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            var tenantId = _userContext.GetCurrentTenantId();
            if (request.TenantId != tenantId)
                throw new BusinessRuleBrokenException("Refund request not found.");

            var userId = _userContext.GetCurrentUserId();
            var isOwner = _userContext.GetUserRole() == SystemRole.Owner.ToString();

            if (isOwner)
            {
                request.ApplyDirectOwnerDecision(userId, RefundDecisionAction.Confirm, rejectionReason: null);
                await _refundRequestStore.UpdateAsync(request, cancellationToken);
                await ApplyOutcomeToBookingAsync(request, cancellationToken);
                return;
            }

            request.RecordTentativeDecision(userId, RefundDecisionAction.Confirm, rejectionReason: null);
            await _refundRequestStore.UpdateAsync(request, cancellationToken);
            await NotifyOwnerApprovalNeededAsync(request, cancellationToken);
        }

        // Shared with RejectRefundRequestHandler and ApproveOwnerGateHandler — whenever a request
        // reaches a terminal-or-processing state, the underlying Booking needs to hear about it.
        internal static async Task ApplyOutcomeAsync(
            IUnitOfWork unitOfWork,
            RefundRequest request,
            CancellationToken cancellationToken)
        {
            var tenant = await unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == request.TenantId,
                includes: [t => t.Bookings]);

            var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);
            if (booking is null)
                return;

            if (request.Status == RefundRequestStatus.Approved)
                booking.ApproveReviewedRefund(request.RequestedAmount);
            else if (request.Status == RefundRequestStatus.Rejected)
                booking.RejectReviewedRefund();
            // AwaitingManualTransfer: no Booking-side event yet — Booking.RefundStatus stays
            // Pending until MarkManuallyRefunded resolves it (Phase 2).

            unitOfWork.TenantRepository.Update(tenant!);
            await unitOfWork.CompleteAsync(cancellationToken);
        }

        private Task ApplyOutcomeToBookingAsync(RefundRequest request, CancellationToken cancellationToken) =>
            ApplyOutcomeAsync(_unitOfWork, request, cancellationToken);

        private async Task NotifyOwnerApprovalNeededAsync(RefundRequest request, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == request.TenantId,
                includes: t => t.Members);

            var owner = tenant?.Members.FirstOrDefault(m => m.Role == SystemRole.Owner);
            if (owner is null)
                return;

            var notification = Notification.Create(
                owner.UserId,
                NotificationRecipientType.TenantAdmin,
                request.TenantId,
                NotificationEventType.RefundApprovalNeeded,
                "Refund Approval Needed",
                $"An admin proposed confirming a refund of {request.RequestedAmount:0.00} {request.CurrencyCode}. Please review and approve or deny it.");

            _unitOfWork.NotificationRepository.Add(notification);
            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync([notification], cancellationToken);
        }
    }
}
```

- [ ] **Step 4: `RejectRefundRequestCommand` + validator**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest
{
    public record RejectRefundRequestCommand(Guid RefundRequestId, string Reason) : ICommand;
}
```

`ApexBooking.Core.Application/Common/Validators/RejectRefundRequestCommandValidator.cs`:

```csharp
using ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class RejectRefundRequestCommandValidator : AbstractValidator<RejectRefundRequestCommand>
{
    public RejectRefundRequestCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required when rejecting a refund request.")
            .MaximumLength(500);
    }
}
```

- [ ] **Step 5: `RejectRefundRequestHandler`**

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using System.Linq;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest
{
    public class RejectRefundRequestHandler : ICommandHandler<RejectRefundRequestCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUserContextService _userContext;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;

        public RejectRefundRequestHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IUserContextService userContext,
            IRealtimeNotificationDispatcher realtimeDispatcher)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _userContext = userContext;
            _realtimeDispatcher = realtimeDispatcher;
        }

        public async Task Handle(RejectRefundRequestCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            var tenantId = _userContext.GetCurrentTenantId();
            if (request.TenantId != tenantId)
                throw new BusinessRuleBrokenException("Refund request not found.");

            var userId = _userContext.GetCurrentUserId();
            var isOwner = _userContext.GetUserRole() == SystemRole.Owner.ToString();

            if (isOwner)
            {
                request.ApplyDirectOwnerDecision(userId, RefundDecisionAction.Reject, command.Reason);
                await _refundRequestStore.UpdateAsync(request, cancellationToken);
                await ConfirmRefundRequestHandler.ApplyOutcomeAsync(_unitOfWork, request, cancellationToken);
                return;
            }

            request.RecordTentativeDecision(userId, RefundDecisionAction.Reject, command.Reason);
            await _refundRequestStore.UpdateAsync(request, cancellationToken);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == request.TenantId,
                includes: t => t.Members);

            var owner = tenant?.Members.FirstOrDefault(m => m.Role == SystemRole.Owner);
            if (owner is null)
                return;

            var notification = Notification.Create(
                owner.UserId,
                NotificationRecipientType.TenantAdmin,
                request.TenantId,
                NotificationEventType.RefundApprovalNeeded,
                "Refund Denial Needs Approval",
                $"An admin proposed rejecting a refund of {request.RequestedAmount:0.00} {request.CurrencyCode} ({command.Reason}). Please review and approve or deny it.");

            _unitOfWork.NotificationRepository.Add(notification);
            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync([notification], cancellationToken);
        }
    }
}
```

- [ ] **Step 6: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 7: Commit**

```bash
git add ApexBooking.Core.Application/Features/RefundRequests/Commands/ ApexBooking.Core.Application/Common/Validators/RejectRefundRequestCommandValidator.cs ApexBooking.Core.Domain/Enums/NotificationEventType.cs
git commit -m "feat: add Confirm/RejectRefundRequest commands"
```

---

### Task 9: Owner-gate commands (Approve/Deny)

**Files:**
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/ApproveOwnerGate/ApproveOwnerGateCommand.cs`
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/ApproveOwnerGate/ApproveOwnerGateHandler.cs`
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/DenyOwnerGate/DenyOwnerGateCommand.cs`
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Commands/DenyOwnerGate/DenyOwnerGateHandler.cs`

**Interfaces:**
- Consumes: `RefundRequest.ApplyOwnerApproval`/`ApplyOwnerDenial` (Task 3), `ConfirmRefundRequestHandler.ApplyOutcomeAsync` (Task 8).
- Produces: `ApproveOwnerGateCommand(Guid RefundRequestId) : ICommand`, `DenyOwnerGateCommand(Guid RefundRequestId) : ICommand`. Consumed by Task 10's controller.

- [ ] **Step 1: `ApproveOwnerGateCommand` + handler**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.ApproveOwnerGate
{
    public record ApproveOwnerGateCommand(Guid RefundRequestId) : ICommand;
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.ApproveOwnerGate
{
    // Owner-only — enforced by [Authorize(Roles = "Owner")] on the controller action, same as
    // every other owner-exclusive endpoint in this codebase (e.g. TenantController's settings
    // actions). This handler doesn't re-check the role — the same trust boundary as those.
    public class ApproveOwnerGateHandler : ICommandHandler<ApproveOwnerGateCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUserContextService _userContext;

        public ApproveOwnerGateHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _userContext = userContext;
        }

        public async Task Handle(ApproveOwnerGateCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            if (request.TenantId != _userContext.GetCurrentTenantId())
                throw new BusinessRuleBrokenException("Refund request not found.");

            request.ApplyOwnerApproval(_userContext.GetCurrentUserId());
            await _refundRequestStore.UpdateAsync(request, cancellationToken);
            await ConfirmRefundRequestHandler.ApplyOutcomeAsync(_unitOfWork, request, cancellationToken);
        }
    }
}
```

- [ ] **Step 2: `DenyOwnerGateCommand` + handler**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.DenyOwnerGate
{
    public record DenyOwnerGateCommand(Guid RefundRequestId) : ICommand;
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.DenyOwnerGate
{
    public class DenyOwnerGateHandler : ICommandHandler<DenyOwnerGateCommand>
    {
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUserContextService _userContext;

        public DenyOwnerGateHandler(IRefundRequestStore refundRequestStore, IUserContextService userContext)
        {
            _refundRequestStore = refundRequestStore;
            _userContext = userContext;
        }

        public async Task Handle(DenyOwnerGateCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            if (request.TenantId != _userContext.GetCurrentTenantId())
                throw new BusinessRuleBrokenException("Refund request not found.");

            request.ApplyOwnerDenial();
            await _refundRequestStore.UpdateAsync(request, cancellationToken);
        }
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Application/Features/RefundRequests/Commands/ApproveOwnerGate/ ApexBooking.Core.Application/Features/RefundRequests/Commands/DenyOwnerGate/
git commit -m "feat: add owner-gate Approve/Deny commands"
```

---

### Task 10: List query + controller (wires everything to HTTP)

**Files:**
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/GetPendingRefundRequestsQuery.cs`
- Create: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/GetPendingRefundRequestsHandler.cs`
- Create: `ApexBooking.WebApi/Controllers/RefundRequestsController.cs`

**Interfaces:**
- Consumes: everything from Tasks 3–9.
- Produces: `GET /api/refund-requests`, `POST /api/refund-requests/{id}/confirm`, `POST /api/refund-requests/{id}/reject`, `POST /api/refund-requests/{id}/owner-approve`, `POST /api/refund-requests/{id}/owner-deny` — the full HTTP surface Phase 2's frontend work will call.

- [ ] **Step 1: Query + DTO**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests
{
    public record RefundRequestSummaryDto(
        Guid Id,
        Guid BookingId,
        string BookingReference,
        string CustomerName,
        decimal RequestedAmount,
        string CurrencyCode,
        bool IsAutoRefundEligible,
        RefundRequestStatus Status,
        string? RejectionReason,
        DateTime CreatedAt
    );

    public record GetPendingRefundRequestsQuery() : IQuery<IReadOnlyList<RefundRequestSummaryDto>>;
}
```

- [ ] **Step 2: Handler**

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests
{
    public class GetPendingRefundRequestsHandler
        : IQueryHandler<GetPendingRefundRequestsQuery, IReadOnlyList<RefundRequestSummaryDto>>
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

        public async Task<IReadOnlyList<RefundRequestSummaryDto>> Handle(
            GetPendingRefundRequestsQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _userContext.GetCurrentTenantId();
            var requests = await _refundRequestStore.GetPendingForTenantAsync(tenantId, cancellationToken);

            if (requests.Count == 0)
                return Array.Empty<RefundRequestSummaryDto>();

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Bookings);

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
                    request.CurrencyCode,
                    request.IsAutoRefundEligible,
                    request.Status,
                    request.RejectionReason,
                    request.CreatedAt));
            }

            return result;
        }
    }
}
```

- [ ] **Step 3: Controller**

```csharp
using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.ApproveOwnerGate;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.DenyOwnerGate;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest;
using ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/refund-requests")]
    [Authorize]
    public class RefundRequestsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RefundRequestsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Policy = "ManagementOnly")]
        [ProducesResponseType(typeof(IReadOnlyList<RefundRequestSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPending()
        {
            var result = await _mediator.Send(new GetPendingRefundRequestsQuery());
            return Ok(result);
        }

        [HttpPost("{id:guid}/confirm")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Confirm(Guid id)
        {
            await _mediator.Send(new ConfirmRefundRequestCommand(id));
            return NoContent();
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRefundRequestBody body)
        {
            await _mediator.Send(new RejectRefundRequestCommand(id, body.Reason));
            return NoContent();
        }

        [HttpPost("{id:guid}/owner-approve")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> OwnerApprove(Guid id)
        {
            await _mediator.Send(new ApproveOwnerGateCommand(id));
            return NoContent();
        }

        [HttpPost("{id:guid}/owner-deny")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> OwnerDeny(Guid id)
        {
            await _mediator.Send(new DenyOwnerGateCommand(id));
            return NoContent();
        }
    }

    public record RejectRefundRequestBody(string Reason);
}
```

- [ ] **Step 4: Build**

Run: `dotnet build ApexBooking.sln`
Expected: 0 errors.

- [ ] **Step 5: Manual end-to-end verification**

With the WebApi running: cancel a refund-eligible test booking (`AutomaticRefund = false`), confirm `GET /api/refund-requests` (as Owner or Admin) lists it as `PendingReview`. As an Admin user, `POST /confirm` it → `GET` again shows `AwaitingOwnerApproval` and the Owner has a `RefundApprovalNeeded` notification. As Owner, `POST /owner-approve` it → for an auto-eligible test payment, confirm (same technique as the earlier PayMongo debugging session — breakpoints or the `outbox_messages` table) that `BookingRefundDueDomainEvent` fires and resolves through the existing pass #1 pipeline.

- [ ] **Step 6: Commit**

```bash
git add ApexBooking.Core.Application/Features/RefundRequests/Queries/ ApexBooking.WebApi/Controllers/RefundRequestsController.cs
git commit -m "feat: add refund-requests API surface (list, confirm, reject, owner gate)"
```

---

## What's deliberately not in this plan (Phase 2, separate plan)

Customer-facing refund-status page/queries, `SubmitRefundEwalletDetailsCommand`, `MarkManualRefundSentCommand`, `BusinessProfile.ContactPhoneNumber`, the rejection email, and the real webhook field name for `PaymentMethodType` becoming Infrastructure-layer wiring (Task 2 leaves it as a Domain-layer string the caller supplies — Phase 2's `ProcessPaymentWebhookCommandHandler` wiring is where the real field name from PayMongo gets read and passed in). All per the spec's phased scope note.
