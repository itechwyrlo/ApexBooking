# Refund Manual-Transfer Redesign — Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the two-tier automatic/manual refund system with a single manual, receipt-verified refund flow; add a per-tenant `RefundEnabled` toggle (default off, including for existing tenants) that gates refund evaluation entirely; and fix the root-cause notification gap so a customer always hears from the business the moment their refund is decided.

**Architecture:** Domain-first — `PaymentPolicy`, `Booking`, and `RefundRequest` change shape, then every Application-layer command/query/handler that reads or writes them follows, then the two controllers that expose them. E-wallet details move from a late follow-up step to a required part of the cancellation request itself, enforced in the domain layer, not just the UI.

**Tech Stack:** .NET / EF Core (SQL Server), MediatR, FluentValidation, xUnit, Brevo (email), local-disk `IFileStorageService`.

**Spec:** [docs/superpowers/specs/2026-08-15-refund-manual-transfer-redesign-design.md](../specs/2026-08-15-refund-manual-transfer-redesign-design.md)

## Global Constraints

- Every new EF migration is generated via `dotnet ef migrations add`, never hand-authored, using `--project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi` (matches every existing migration in this repo).
- Do **not** run `dotnet ef database update` — leave that to the user (standing instruction in this repo: risk of a file lock from their running debug session).
- `RefundRequest` is not an `IAggregateRoot` — it's written/read only through `IRefundRequestStore`, never a generic repository (unchanged pattern).
- MediatR handlers and FluentValidation validators are assembly-scanned (`ApplicationServices.cs`) — no manual DI registration needed for new commands/handlers; deleting a handler file is sufficient to deregister it.
- HTML email bodies HTML-encode all interpolated values via the existing `H(...)` helper in `BookingNotificationService.cs` — follow this for any new email method.

---

## Task 1: `PaymentPolicy.RefundEnabled` flag

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/PaymentPolicy.cs`
- Test: `ApexBooking.Core.Domain.UnitTests/Entities/PaymentPolicyTests.cs`

**Interfaces:**
- Produces: `PaymentPolicy.RefundEnabled` (bool, defaults `false`), `PaymentPolicy.UpdatePolicy(..., bool refundEnabled, ...)` — the last positional parameter, appended after `refundReviewDeadlineDays`.

- [ ] **Step 1: Write the failing tests**

Add to `PaymentPolicyTests.cs` (keep the existing tests, update every `UpdatePolicy(...)` call site in this file to pass the new trailing `refundEnabled` argument or the file won't compile):

```csharp
[Fact]
public void NewPolicy_DefaultsRefundEnabledToFalse()
{
    var policy = new PaymentPolicy(new TenantId(Guid.NewGuid()));

    Assert.False(policy.RefundEnabled);
}

[Fact]
public void UpdatePolicy_CanEnableRefunds()
{
    var policy = new PaymentPolicy(new TenantId(Guid.NewGuid()));

    policy.UpdatePolicy(
        PaymentRequirementType.DepositRequired,
        DepositType.Percentage,
        depositValue: 50m,
        onTimeRefundPercent: 100m,
        lateCancellationRefundPercent: 0m,
        refundReviewDeadlineDays: 7,
        refundEnabled: true);

    Assert.True(policy.RefundEnabled);
}
```

Remove the two `AutomaticRefund`-named tests (`NewPolicy_DefaultsAutomaticRefundToFalse`, `UpdatePolicy_CanEnableAutomaticRefund`) — the property they test is gone.

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter FullyQualifiedName~PaymentPolicyTests`
Expected: build error — `PaymentPolicy` has no `RefundEnabled` member and `UpdatePolicy` doesn't accept the new signature yet.

- [ ] **Step 3: Update `PaymentPolicy.cs`**

Replace the `AutomaticRefund` property and its constructor/`UpdatePolicy` wiring:

```csharp
// Whether this tenant does refunds at all. Defaults false — a fresh tenant, and every tenant
// that existed before this flag shipped, starts with refunds off. A cancelled online-paid
// booking produces no RefundRequest, no Pending status, no refund UI, until a tenant
// explicitly turns this on in Payment Settings.
public bool RefundEnabled { get; private set; }
```

In the constructor, replace `AutomaticRefund = false;` with `RefundEnabled = false;`.

Replace the `UpdatePolicy` signature and body's last two lines:

```csharp
public void UpdatePolicy(
    PaymentRequirementType requirementType,
    DepositType depositType,
    decimal depositValue,
    decimal onTimeRefundPercent,
    decimal lateCancellationRefundPercent,
    int refundReviewDeadlineDays,
    bool refundEnabled)
{
    // ...unchanged validation block above...

    RequirementType = requirementType;
    DepositType = depositType;
    DepositValue = depositValue;
    OnTimeRefundPercent = onTimeRefundPercent;
    LateCancellationRefundPercent = lateCancellationRefundPercent;
    RefundReviewDeadlineDays = refundReviewDeadlineDays;
    RefundEnabled = refundEnabled;
    UpdatedAt = DateTime.UtcNow;
}
```

(Dropped the `automaticRefund` parameter entirely — no validation logic touches it.)

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter FullyQualifiedName~PaymentPolicyTests`
Expected: PASS (this will still show compile errors from other files calling the old `UpdatePolicy` signature — those are fixed in Tasks 3 and 7; this step just confirms `PaymentPolicyTests.cs` itself is correct).

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/PaymentPolicy.cs ApexBooking.Core.Domain.UnitTests/Entities/PaymentPolicyTests.cs
git commit -m "feat: replace PaymentPolicy.AutomaticRefund with RefundEnabled gate"
```

---

## Task 2: Collapse `RefundRequestStatus` and rewrite `RefundRequest`

**Files:**
- Modify: `ApexBooking.Core.Domain/Enums/RefundRequestStatus.cs`
- Modify: `ApexBooking.Core.Domain/Entities/RefundRequest.cs`
- Delete: `ApexBooking.Core.Domain/Enums/RefundDecisionAction.cs`
- Test: `ApexBooking.Core.Domain.UnitTests/Entities/RefundRequestTests.cs` (full rewrite)

**Interfaces:**
- Produces: `RefundRequestStatus { PendingReview, Refunded, Rejected }`; `RefundRequest.Create(TenantId tenantId, Guid bookingId, decimal requestedAmount, string currencyCode, string ewalletProvider, string ewalletNumber, string ewalletName)`; `RefundRequest.Confirm(Guid decidedByUserId, string receiptUrl)`; `RefundRequest.Reject(Guid decidedByUserId, string reason)`; properties `CustomerEwalletProvider`, `CustomerEwalletNumber`, `CustomerEwalletName` (all non-null after `Create`), `ReceiptUrl`, `DecidedByUserId`, `DecidedAt`, `RejectionReason`.
- Consumes: nothing new.

- [ ] **Step 1: Write the failing tests**

Replace `RefundRequestTests.cs` entirely:

```csharp
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;
using Xunit;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class RefundRequestTests
{
    private static RefundRequest CreatePendingReview() =>
        RefundRequest.Create(
            new TenantId(Guid.NewGuid()), Guid.NewGuid(), 500m, "PHP",
            ewalletProvider: "GCash", ewalletNumber: "09171234567", ewalletName: "Juan Dela Cruz");

    [Fact]
    public void Create_StartsInPendingReview_WithEwalletDetailsAttached()
    {
        var request = CreatePendingReview();

        Assert.Equal(RefundRequestStatus.PendingReview, request.Status);
        Assert.Equal("GCash", request.CustomerEwalletProvider);
        Assert.Equal("09171234567", request.CustomerEwalletNumber);
        Assert.Equal("Juan Dela Cruz", request.CustomerEwalletName);
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        Assert.Throws<BusinessRuleBrokenException>(() =>
            RefundRequest.Create(new TenantId(Guid.NewGuid()), Guid.NewGuid(), 0m, "PHP", "GCash", "09171234567", "Juan Dela Cruz"));
    }

    [Fact]
    public void Confirm_FromPendingReview_MovesToRefunded_WithReceiptUrl()
    {
        var request = CreatePendingReview();
        var userId = Guid.NewGuid();

        request.Confirm(userId, "https://files.example.com/receipts/abc.png");

        Assert.Equal(RefundRequestStatus.Refunded, request.Status);
        Assert.Equal(userId, request.DecidedByUserId);
        Assert.Equal("https://files.example.com/receipts/abc.png", request.ReceiptUrl);
        Assert.NotNull(request.DecidedAt);
    }

    [Fact]
    public void Confirm_WhenAlreadyDecided_Throws()
    {
        var request = CreatePendingReview();
        request.Confirm(Guid.NewGuid(), "https://files.example.com/receipts/abc.png");

        Assert.Throws<BusinessRuleBrokenException>(() =>
            request.Confirm(Guid.NewGuid(), "https://files.example.com/receipts/def.png"));
    }

    [Fact]
    public void Reject_FromPendingReview_MovesToRejected_WithReason()
    {
        var request = CreatePendingReview();
        var userId = Guid.NewGuid();

        request.Reject(userId, "Customer no-showed twice");

        Assert.Equal(RefundRequestStatus.Rejected, request.Status);
        Assert.Equal(userId, request.DecidedByUserId);
        Assert.Equal("Customer no-showed twice", request.RejectionReason);
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var request = CreatePendingReview();

        Assert.Throws<BusinessRuleBrokenException>(() =>
            request.Reject(Guid.NewGuid(), ""));
    }

    [Fact]
    public void Reject_WhenAlreadyDecided_Throws()
    {
        var request = CreatePendingReview();
        request.Reject(Guid.NewGuid(), "Not eligible");

        Assert.Throws<BusinessRuleBrokenException>(() =>
            request.Reject(Guid.NewGuid(), "Second reason"));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter FullyQualifiedName~RefundRequestTests`
Expected: build error — `RefundRequest.Create` still requires an `isAutoRefundEligible` bool, `Confirm`/`Reject` don't exist yet.

- [ ] **Step 3: Collapse `RefundRequestStatus.cs`**

```csharp
namespace ApexBooking.Core.Domain.Enums;

public enum RefundRequestStatus
{
    PendingReview,
    Refunded,
    Rejected
}
```

- [ ] **Step 4: Delete `RefundDecisionAction.cs`**

```bash
git rm ApexBooking.Core.Domain/Enums/RefundDecisionAction.cs
```

- [ ] **Step 5: Rewrite `RefundRequest.cs`**

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

    public RefundRequestStatus Status { get; private set; }

    public Guid? DecidedByUserId { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? ReceiptUrl { get; private set; }

    // Always populated at creation — the customer (or staff, cancelling on their behalf)
    // provides these in the same request as the cancellation itself, not as a later follow-up.
    public string CustomerEwalletProvider { get; private set; } = string.Empty;
    public string CustomerEwalletNumber { get; private set; } = string.Empty;
    public string CustomerEwalletName { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected RefundRequest() { }

    public static RefundRequest Create(
        TenantId tenantId,
        Guid bookingId,
        decimal requestedAmount,
        string currencyCode,
        string ewalletProvider,
        string ewalletNumber,
        string ewalletName)
    {
        if (requestedAmount <= 0)
            throw new BusinessRuleBrokenException("Refund request amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(ewalletProvider) || string.IsNullOrWhiteSpace(ewalletNumber) || string.IsNullOrWhiteSpace(ewalletName))
            throw new BusinessRuleBrokenException("E-wallet provider, account number, and account name are all required to create a refund request.");

        return new RefundRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BookingId = bookingId,
            RequestedAmount = requestedAmount,
            CurrencyCode = currencyCode,
            CustomerEwalletProvider = ewalletProvider,
            CustomerEwalletNumber = ewalletNumber,
            CustomerEwalletName = ewalletName,
            Status = RefundRequestStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Owner or Admin confirms the manual e-wallet transfer already happened, with a receipt as
    // proof — see ConfirmRefundRequestHandler, which saves the uploaded file before calling this.
    public void Confirm(Guid decidedByUserId, string receiptUrl)
    {
        if (Status != RefundRequestStatus.PendingReview)
            throw new BusinessRuleBrokenException("This refund request has already been decided.");

        if (string.IsNullOrWhiteSpace(receiptUrl))
            throw new BusinessRuleBrokenException("A receipt is required to confirm a refund.");

        DecidedByUserId = decidedByUserId;
        DecidedAt = DateTime.UtcNow;
        ReceiptUrl = receiptUrl;
        Status = RefundRequestStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(Guid decidedByUserId, string reason)
    {
        if (Status != RefundRequestStatus.PendingReview)
            throw new BusinessRuleBrokenException("This refund request has already been decided.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleBrokenException("A reason is required when rejecting a refund request.");

        DecidedByUserId = decidedByUserId;
        DecidedAt = DateTime.UtcNow;
        RejectionReason = reason;
        Status = RefundRequestStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter FullyQualifiedName~RefundRequestTests`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add ApexBooking.Core.Domain/Enums/RefundRequestStatus.cs ApexBooking.Core.Domain/Entities/RefundRequest.cs ApexBooking.Core.Domain.UnitTests/Entities/RefundRequestTests.cs
git rm ApexBooking.Core.Domain/Enums/RefundDecisionAction.cs 2>/dev/null || true
git commit -m "feat: collapse RefundRequest to a 3-state Confirm/Reject model with upfront e-wallet capture"
```

---

## Task 3: `Booking` — collapse `RefundStatus`, drop `PaymentMethodType`, gate `EvaluateRefund`, thread e-wallet params

**Files:**
- Modify: `ApexBooking.Core.Domain/Enums/RefundStatus.cs`
- Modify: `ApexBooking.Core.Domain/Entities/Booking.cs`
- Modify: `ApexBooking.Core.Domain/Events/BookingEvents.cs`
- Test: `ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs` (full rewrite)
- Test: `ApexBooking.Core.Domain.UnitTests/Entities/BookingPaymentCaptureTests.cs` (touch only if it asserts on `PaymentMethodType` — check before editing)

**Interfaces:**
- Consumes: `PaymentPolicy.RefundEnabled` (Task 1).
- Produces: `Booking.RefundStatus { None, Pending, Refunded, Rejected }`; `Booking.Cancel(Guid adminUserId, string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy, string? ewalletProvider, string? ewalletNumber, string? ewalletName)`; `Booking.CancelByCustomer(string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy, string? ewalletProvider, string? ewalletNumber, string? ewalletName)`; `Booking.ConfirmReviewedRefund(decimal amount, string receiptUrl)`; `Booking.RejectReviewedRefund(string reason)` (signature unchanged); `Booking.ConfirmPayment(PaymentConfirmationMethod method, string? payMongoPaymentId = null)` (drops the third parameter); `BookingRefundEligibleDomainEvent` gains `string EwalletProvider, string EwalletNumber, string EwalletName`; new `BookingRefundConfirmedDomainEvent`; `BookingRefundDueDomainEvent` removed.

- [ ] **Step 1: Check `BookingPaymentCaptureTests.cs` for `PaymentMethodType` assertions**

Run: `grep -n "PaymentMethodType" ApexBooking.Core.Domain.UnitTests/Entities/BookingPaymentCaptureTests.cs`
If any lines call `ConfirmPayment` with a third argument or assert on `booking.PaymentMethodType`, note them — Step 6 below fixes them by dropping the third argument from every `ConfirmPayment` call site and removing any `PaymentMethodType` assertion.

- [ ] **Step 2: Write the failing tests**

Replace `BookingRefundTests.cs` entirely:

```csharp
using System;
using System.Linq;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.SharedKernel.Exceptions;
using Xunit;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class BookingRefundTests
{
    private static Booking CreateOnlinePaidBooking(DateOnly scheduledDate, TimeOnly scheduledStartTime, decimal amountDue = 500m)
    {
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-01",
            scheduledDate: scheduledDate,
            scheduledStartTime: scheduledStartTime,
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: true,
            currencyCode: "PHP",
            amountDue: amountDue);

        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_test123");
        booking.ClearDomainEvents(); // isolate the assertions below to the Cancel call itself
        return booking;
    }

    private static PaymentPolicy CreatePaymentPolicy(TenantId tenantId, bool refundEnabled, decimal onTimePercent = 100m, decimal latePercent = 0m)
    {
        var policy = new PaymentPolicy(tenantId);
        policy.UpdatePolicy(
            PaymentRequirementType.None, DepositType.Percentage, 0m,
            onTimeRefundPercent: onTimePercent, lateCancellationRefundPercent: latePercent,
            refundReviewDeadlineDays: 7, refundEnabled: refundEnabled);
        return policy;
    }

    [Fact]
    public void Cancel_RefundDisabled_NeverRaisesRefund_EvenWhenOtherwiseEligible()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future));
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: false);

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, ewalletProvider: null, ewalletNumber: null, ewalletName: null);

        Assert.Equal(RefundStatus.None, booking.RefundStatus);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
    }

    [Fact]
    public void Cancel_RefundEnabled_OnTime_WithoutEwalletDetails_Throws()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future));
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true);

        Assert.Throws<BusinessRuleBrokenException>(() =>
            booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, ewalletProvider: null, ewalletNumber: null, ewalletName: null));
    }

    [Fact]
    public void Cancel_RefundEnabled_OnTime_WithEwalletDetails_RaisesEligibleEventCarryingThem()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true, onTimePercent: 80m);

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, "GCash", "09171234567", "Juan Dela Cruz");

        Assert.Equal(RefundStatus.Pending, booking.RefundStatus);
        var eligibleEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
        Assert.Equal(400m, eligibleEvent.RefundAmount);
        Assert.Equal("GCash", eligibleEvent.EwalletProvider);
        Assert.Equal("09171234567", eligibleEvent.EwalletNumber);
        Assert.Equal("Juan Dela Cruz", eligibleEvent.EwalletName);
    }

    [Fact]
    public void Cancel_RefundEnabled_ZeroOnTimePercent_DoesNotRaiseRefund_EvenWithoutEwalletDetails()
    {
        // A refund amount that clamps to zero never needs e-wallet details — nothing will
        // actually be asked for or transferred, so the "required when eligible" guard doesn't fire.
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future));
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true, onTimePercent: 0m);

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, null, null, null);

        Assert.Equal(RefundStatus.None, booking.RefundStatus);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
    }

    [Fact]
    public void Cancel_PastCutoff_PartialRefundPolicy_RaisesPercentageAmount()
    {
        var soon = DateTime.UtcNow.AddHours(2);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(soon), TimeOnly.FromDateTime(soon), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        bookingPolicy.UpdateSettings(lateCancellationPolicy: CancellationPolicy.PartialRefund);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true, latePercent: 50m);

        booking.Cancel(Guid.NewGuid(), "Late cancel", bookingPolicy, paymentPolicy, "Maya", "09179876543", "Juan Dela Cruz");

        var eligibleEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
        Assert.Equal(250m, eligibleEvent.RefundAmount);
    }

    [Fact]
    public void ConfirmReviewedRefund_SetsRefundStatusRefunded_AndRaisesConfirmedEvent()
    {
        var booking = CreateOnlinePaidBooking(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true);
        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, "GCash", "09171234567", "Juan Dela Cruz");
        booking.ClearDomainEvents();

        booking.ConfirmReviewedRefund(500m, "https://files.example.com/receipts/abc.png");

        Assert.Equal(RefundStatus.Refunded, booking.RefundStatus);
        Assert.Equal(500m, booking.RefundedAmount);
        var confirmedEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundConfirmedDomainEvent>());
        Assert.Equal(500m, confirmedEvent.RefundedAmount);
        Assert.Equal("https://files.example.com/receipts/abc.png", confirmedEvent.ReceiptUrl);
    }

    [Fact]
    public void RejectReviewedRefund_SetsRefundStatusRejected_AndRaisesRejectedEvent()
    {
        var booking = CreateOnlinePaidBooking(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true);
        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, "GCash", "09171234567", "Juan Dela Cruz");
        booking.ClearDomainEvents();

        booking.RejectReviewedRefund("Outside policy window");

        Assert.Equal(RefundStatus.Rejected, booking.RefundStatus);
        var rejectedEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundRejectedDomainEvent>());
        Assert.Equal("Outside policy window", rejectedEvent.RejectionReason);
    }

    [Fact]
    public void CancelByCustomer_PayInVisitBooking_NeverRefunds()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-02",
            scheduledDate: DateOnly.FromDateTime(future),
            scheduledStartTime: TimeOnly.FromDateTime(future),
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: false,
            currencyCode: "PHP",
            amountDue: 500m);
        booking.ClearDomainEvents();
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true);

        booking.CancelByCustomer("Change of plans", bookingPolicy, paymentPolicy, null, null, null);

        Assert.Equal(RefundStatus.None, booking.RefundStatus);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter FullyQualifiedName~BookingRefundTests`
Expected: build errors — new `Cancel`/`CancelByCustomer` parameters, `ConfirmReviewedRefund`, `BookingRefundConfirmedDomainEvent` don't exist yet.

- [ ] **Step 4: Collapse `RefundStatus.cs`**

```csharp
namespace ApexBooking.Core.Domain.Enums;

public enum RefundStatus
{
    None,
    Pending,
    Refunded,
    Rejected
}
```

- [ ] **Step 5: Update `BookingEvents.cs`**

Remove `BookingRefundDueDomainEvent` entirely. Replace `BookingRefundEligibleDomainEvent` and add `BookingRefundConfirmedDomainEvent`:

```csharp
// Raised when a cancellation qualifies for a refund (see Booking.EvaluateRefund) — creates a
// RefundRequest awaiting human review. E-wallet details travel with this event because they're
// captured up front, in the same cancellation request, not as a later follow-up.
public record BookingRefundEligibleDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    decimal RefundAmount,
    string CurrencyCode,
    string EwalletProvider,
    string EwalletNumber,
    string EwalletName,
    DateTime OccurredAt
) : IDomainEvent;

// Raised when an Owner/Admin confirms a manual e-wallet transfer with a receipt as proof —
// drives the customer's "your refund was sent" email. Reliable: the email is an external call.
public record BookingRefundConfirmedDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    decimal RefundedAmount,
    string CurrencyCode,
    string ReceiptUrl,
    DateTime OccurredAt
) : IReliableDomainEvent;
```

Leave `BookingRefundRejectedDomainEvent` untouched — it already does exactly what's needed here.

- [ ] **Step 6: Update `Booking.cs`**

Remove the `PaymentMethodType` property (line ~49) and its doc comment.

Change `ConfirmPayment`'s signature and body (drop the third parameter):

```csharp
public void ConfirmPayment(PaymentConfirmationMethod method, string? payMongoPaymentId = null)
{
    if (Status != BookingStatus.PendingPayment)
        throw new BusinessRuleBrokenException("Only appointments pending payment can have their transactions verified.");

    Status = BookingStatus.Scheduled;
    PaymentConfirmedVia = method;
    PayMongoPaymentId = payMongoPaymentId;
    UpdatedAt = DateTime.UtcNow;

    // ...unchanged BookingScheduledDomainEvent below...
}
```

Replace `Cancel` and `CancelByCustomer` (both gain three trailing parameters and the required-when-eligible guard; both drop the `AutomaticRefund` branch):

```csharp
public void Cancel(Guid adminUserId, string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
{
    if (Status != BookingStatus.Scheduled)
        throw new BusinessRuleBrokenException("Cannot cancel an appointment that is already wrapped up or resolved.");

    if (string.IsNullOrWhiteSpace(reason))
        throw new BusinessRuleBrokenException("A cancellation reason trace parameter is required.");

    Status = BookingStatus.Cancelled;
    CancellationReason = reason.Trim();
    CancelledAt = DateTime.UtcNow;
    CancelledByUserId = adminUserId;
    UpdatedAt = DateTime.UtcNow;

    AddDomainEvent(new BookingCancelledDomainEvent(
        TenantId: this.TenantId,
        BookingId: this.BookingId.Value,
        BookingReference: this.BookingReference,
        CustomerId: this.CustomerId.Value,
        Reason: CancellationReason,
        CancelledByUserId: adminUserId,
        CancelledAt: CancelledAt.Value
    ));

    AddDomainEvent(new BookingCancellationNoticeDomainEvent(
        TenantId: this.TenantId,
        BookingId: this.BookingId.Value,
        BookingReference: this.BookingReference,
        CancelledAt: CancelledAt.Value
    ));

    RaiseRefundEligibilityIfAny(bookingPolicy, paymentPolicy, ewalletProvider, ewalletNumber, ewalletName);
}

// Customer-initiated cancellation via the emailed cancel link — no staff account behind
// it, so CancelledByUserId stays null. The notice-window check (CancellationCutoffHours)
// happens one level up, in Tenant.CancelBookingByCustomer, before this is ever called.
public void CancelByCustomer(string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
{
    if (Status != BookingStatus.Scheduled)
        throw new BusinessRuleBrokenException("Cannot cancel an appointment that is already wrapped up or resolved.");

    if (string.IsNullOrWhiteSpace(reason))
        throw new BusinessRuleBrokenException("A cancellation reason trace parameter is required.");

    Status = BookingStatus.Cancelled;
    CancellationReason = reason.Trim();
    CancelledAt = DateTime.UtcNow;
    CancelledByUserId = null;
    UpdatedAt = DateTime.UtcNow;

    AddDomainEvent(new BookingCancelledDomainEvent(
        TenantId: this.TenantId,
        BookingId: this.BookingId.Value,
        BookingReference: this.BookingReference,
        CustomerId: this.CustomerId.Value,
        Reason: CancellationReason,
        CancelledByUserId: null,
        CancelledAt: CancelledAt.Value
    ));

    AddDomainEvent(new BookingCancellationNoticeDomainEvent(
        TenantId: this.TenantId,
        BookingId: this.BookingId.Value,
        BookingReference: this.BookingReference,
        CancelledAt: CancelledAt.Value
    ));

    RaiseRefundEligibilityIfAny(bookingPolicy, paymentPolicy, ewalletProvider, ewalletNumber, ewalletName);
}

// Shared by Cancel and CancelByCustomer — evaluates refund eligibility and, if eligible,
// requires e-wallet details to already be present (they're collected up front, in the same
// cancellation request) before raising the event that creates a RefundRequest.
private void RaiseRefundEligibilityIfAny(BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
{
    var (shouldRefund, refundAmount) = EvaluateRefund(bookingPolicy, paymentPolicy);
    if (!shouldRefund)
        return;

    if (string.IsNullOrWhiteSpace(ewalletProvider) || string.IsNullOrWhiteSpace(ewalletNumber) || string.IsNullOrWhiteSpace(ewalletName))
        throw new BusinessRuleBrokenException("E-wallet details are required to cancel a booking eligible for a refund.");

    RefundStatus = RefundStatus.Pending;
    AddDomainEvent(new BookingRefundEligibleDomainEvent(
        TenantId: this.TenantId,
        BookingId: this.BookingId.Value,
        BookingReference: this.BookingReference,
        RefundAmount: refundAmount,
        CurrencyCode: this.CurrencyCode,
        EwalletProvider: ewalletProvider,
        EwalletNumber: ewalletNumber,
        EwalletName: ewalletName,
        OccurredAt: CancelledAt!.Value
    ));
}
```

Add the `RefundEnabled` gate to `EvaluateRefund` (first line of the method body):

```csharp
private (bool ShouldRefund, decimal Amount) EvaluateRefund(BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)
{
    if (!RequiresUpfrontPayment || PaymentConfirmedVia != PaymentConfirmationMethod.Online)
        return (false, 0m);

    if (paymentPolicy?.RefundEnabled != true)
        return (false, 0m);

    // ...unchanged on-time/late-cancellation percent calculation below...
}
```

Replace `RecordRefundOutcome` with two purpose-specific methods (rename `ApproveReviewedRefund` to `ConfirmReviewedRefund`, add the receipt parameter, and keep `RejectReviewedRefund` as-is):

```csharp
// Called by ConfirmRefundRequestHandler once an Owner/Admin has confirmed the manual e-wallet
// transfer with a receipt uploaded as proof.
public void ConfirmReviewedRefund(decimal amount, string receiptUrl)
{
    RefundStatus = RefundStatus.Refunded;
    RefundedAmount = amount;
    RefundedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;

    AddDomainEvent(new BookingRefundConfirmedDomainEvent(
        TenantId: this.TenantId,
        BookingId: this.BookingId.Value,
        BookingReference: this.BookingReference,
        RefundedAmount: amount,
        CurrencyCode: this.CurrencyCode,
        ReceiptUrl: receiptUrl,
        OccurredAt: DateTime.UtcNow
    ));
}

// Called when a refund review is rejected — raises a reliable event so the customer gets told
// why, exactly when the decision happens.
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

Delete the old `RecordRefundOutcome` method entirely — nothing calls it anymore once Task 14 removes its only two callers.

- [ ] **Step 7: Fix `BookingPaymentCaptureTests.cs` if Step 1 found issues**

Drop the third `ConfirmPayment` argument from any call site that passes one, and delete any assertion on `booking.PaymentMethodType`.

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter "FullyQualifiedName~BookingRefundTests|FullyQualifiedName~BookingPaymentCaptureTests|FullyQualifiedName~PaymentPolicyTests"`
Expected: PASS. (The wider solution will not build yet — Application-layer callers of the old signatures are fixed in later tasks.)

- [ ] **Step 9: Commit**

```bash
git add ApexBooking.Core.Domain/Enums/RefundStatus.cs ApexBooking.Core.Domain/Entities/Booking.cs ApexBooking.Core.Domain/Events/BookingEvents.cs ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs ApexBooking.Core.Domain.UnitTests/Entities/BookingPaymentCaptureTests.cs
git commit -m "feat: gate Booking refund eligibility on RefundEnabled, require e-wallet details up front"
```

---

## Task 4: `Tenant.CancelBooking` / `CancelBookingByCustomer` pass-through

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Tenant.cs:596-622`

**Interfaces:**
- Consumes: `Booking.Cancel`/`CancelByCustomer` new signatures (Task 3).
- Produces: `Tenant.CancelBooking(Guid bookingId, Guid executionUserId, string reason, string? ewalletProvider, string? ewalletNumber, string? ewalletName)`; `Tenant.CancelBookingByCustomer(Guid bookingId, string reason, string? ewalletProvider, string? ewalletNumber, string? ewalletName)`.

- [ ] **Step 1: Update both methods**

```csharp
public void CancelBooking(Guid bookingId, Guid executionUserId, string reason, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
{
    var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId);
    if (booking == null)
        throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

    booking.Cancel(executionUserId, reason, BookingPolicy, PaymentPolicy, ewalletProvider, ewalletNumber, ewalletName);
    this.UpdatedAt = DateTime.UtcNow;
}

// Customer-initiated cancellation via the emailed cancel link. Unlike the staff path
// above, this enforces the tenant's own notice window before it's allowed at all — a
// staff member cancelling on the business's behalf isn't bound by the same cutoff.
public void CancelBookingByCustomer(Guid bookingId, string reason, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
{
    var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
        ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

    var scheduledAt = booking.ScheduledDate.ToDateTime(booking.ScheduledStartTime);
    var cutoffHours = BookingPolicy?.CancellationCutoffHours ?? 0;
    if (DateTime.UtcNow.AddHours(cutoffHours) > scheduledAt)
        throw new BusinessRuleBrokenException(
            $"This booking can no longer be cancelled online — it's within {cutoffHours} hour(s) of the appointment. Please contact the business directly.");

    booking.CancelByCustomer(reason, BookingPolicy, PaymentPolicy, ewalletProvider, ewalletNumber, ewalletName);
    this.UpdatedAt = DateTime.UtcNow;
}
```

- [ ] **Step 2: Build the Domain project**

Run: `dotnet build ApexBooking.Core.Domain`
Expected: SUCCESS — this project only depends on itself plus SharedKernel.

- [ ] **Step 3: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/Tenant.cs
git commit -m "feat: thread e-wallet details through Tenant's cancel-booking entry points"
```

---

## Task 5: Remove `PaymentMethodType` plumbing from the payment webhook path

**Files:**
- Modify: `ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommandHandler.cs`
- Modify: `ApexBooking.WebApi/Controllers/PayMongoWebhooksController.cs`

**Interfaces:**
- Consumes: `Booking.ConfirmPayment(PaymentConfirmationMethod, string?)` (Task 3, two-parameter form).

- [ ] **Step 1: Drop the field from the command**

```csharp
public record ProcessPaymentWebhookCommand(string RemarksToken, string? PayMongoPaymentId, string RawBody, string? SignatureHeader) : ICommand;
```

- [ ] **Step 2: Update the handler's `ConfirmPayment` call**

In `ProcessPaymentWebhookCommandHandler.cs`, change:
```csharp
booking.ConfirmPayment(PaymentConfirmationMethod.Online, command.PayMongoPaymentId, command.PaymentMethodType);
```
to:
```csharp
booking.ConfirmPayment(PaymentConfirmationMethod.Online, command.PayMongoPaymentId);
```

- [ ] **Step 3: Update the controller**

In `PayMongoWebhooksController.cs`, remove the `paymentMethodType` local variable (line 65) and its comment, and drop the argument from the `ProcessPaymentWebhookCommand` construction:

```csharp
var payment = resourceAttributes.Payments.FirstOrDefault();
var payMongoPaymentId = payment?.Data.Id;
await _mediator.Send(new ProcessPaymentWebhookCommand(resourceAttributes.Remarks, payMongoPaymentId, jsonText, signatureHeader));
```

- [ ] **Step 4: Build**

Run: `dotnet build ApexBooking.WebApi`
Expected: still has errors elsewhere (Tasks 6+ fix the rest) — but no error should reference `ProcessPaymentWebhookCommand`, `ConfirmPayment`'s third argument, or `paymentMethodType` anymore. Grep the build output for those three strings to confirm.

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ ApexBooking.WebApi/Controllers/PayMongoWebhooksController.cs
git commit -m "chore: drop PaymentMethodType capture from the payment webhook path"
```

---

## Task 6: EF migration + mapping config updates

**Files:**
- Modify: `ApexBooking.Core.Persistence/Mappings/PaymentPolicyConfiguration.cs`
- Modify: `ApexBooking.Core.Persistence/Mappings/RefundRequestConfiguration.cs`
- Modify: `ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs` (remove the `PaymentMethodType` mapping — locate it first)
- Modify: `ApexBooking.Core.Persistence/Services/RefundRequestStore.cs`
- Create: an EF migration under `ApexBooking.Core.Persistence/Migrations/`

**Interfaces:**
- Consumes: `PaymentPolicy.RefundEnabled` (Task 1), `RefundRequest`'s new shape (Task 2), `Booking` without `PaymentMethodType` (Task 3).

- [ ] **Step 1: Locate the `PaymentMethodType` mapping in `BookingConfiguration.cs`**

Run: `grep -n "PaymentMethodType" ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs`
Remove that `builder.Property(b => b.PaymentMethodType)...` line entirely.

- [ ] **Step 2: Update `PaymentPolicyConfiguration.cs`**

Replace the `AutomaticRefund` property mapping:

```csharp
builder.Property(p => p.RefundEnabled)
    .HasColumnName("refund_enabled")
    .HasDefaultValue(false)
    .IsRequired();
```

- [ ] **Step 3: Update `RefundRequestConfiguration.cs`**

Remove the `IsAutoRefundEligible`, `DecisionAction`, `OwnerDecidedByUserId`, `OwnerDecidedAt` property mappings. Add `ReceiptUrl` and make the e-wallet columns required (they're always populated now); add `CustomerEwalletName`:

```csharp
builder.Property(r => r.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
builder.Property(r => r.ReceiptUrl).HasColumnName("receipt_url").HasMaxLength(500);

builder.Property(r => r.CustomerEwalletProvider).HasColumnName("customer_ewallet_provider").HasMaxLength(50).IsRequired();
builder.Property(r => r.CustomerEwalletNumber).HasColumnName("customer_ewallet_number").HasMaxLength(50).IsRequired();
builder.Property(r => r.CustomerEwalletName).HasColumnName("customer_ewallet_name").HasMaxLength(200).IsRequired();
```
Keep `DecidedByUserId`/`DecidedAt` mappings as-is (still on the entity, unchanged column names).

- [ ] **Step 4: Update `RefundRequestStore.cs`**

Replace the two status arrays:

```csharp
private static readonly RefundRequestStatus[] TerminalStatuses =
[
    RefundRequestStatus.Refunded,
    RefundRequestStatus.Rejected
];
```
and
```csharp
private static readonly RefundRequestStatus[] ProcessedStatuses =
[
    RefundRequestStatus.Refunded
];
```

- [ ] **Step 5: Build the Persistence project**

Run: `dotnet build ApexBooking.Core.Persistence`
Expected: still fails — `ApexBookingDbContext`'s existing `RefundRequests`/`PaymentPolicy` DbSets are unaffected by this task, but other Application-layer callers referenced in later tasks aren't fixed yet. Confirm the *only* errors remaining are outside `ApexBooking.Core.Persistence` (this project itself should build clean once Steps 1-4 are done, since it doesn't reference `ApexBooking.Core.Application`).

- [ ] **Step 6: Generate the migration**

Run: `dotnet ef migrations add RefundManualTransferRedesign --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`

Expected: a new migration file appears under `ApexBooking.Core.Persistence/Migrations/` adding `refund_enabled` to `payment_policy`, dropping `automatic_refund`; adding `receipt_url`/`customer_ewallet_name` to `refund_requests`, dropping `is_auto_refund_eligible`/`decision_action`/`owner_decided_by_user_id`/`owner_decided_at`; dropping `PaymentMethodType` from `bookings`. **Read the generated migration file** to confirm it matches this description before moving on — if the startup project can't build (Application/WebApi still have compile errors from later tasks), this step will fail; if so, come back to it after Task 15 instead and note that in your task log.

- [ ] **Step 7: Do NOT run `dotnet ef database update`**

Per this repo's standing instruction (see Global Constraints) — leave that to the user.

- [ ] **Step 8: Commit**

```bash
git add ApexBooking.Core.Persistence/
git commit -m "feat: migrate schema for the refund manual-transfer redesign"
```

---

## Task 7: `UpdatePaymentPolicyCommand`/Handler + `GetPaymentPolicyQuery`/Handler

**Files:**
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/UpdatePaymentPolicyHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/GetPaymentPolicyHandler.cs` (check whether it maps field-by-field or via a mapper — update whichever it is)

**Interfaces:**
- Consumes: `PaymentPolicy.UpdatePolicy(..., int refundReviewDeadlineDays, bool refundEnabled)` (Task 1).

- [ ] **Step 1: Update `UpdatePaymentPolicyCommand.cs`**

```csharp
public record UpdatePaymentPolicyCommand(
    PaymentRequirementType RequirementType,
    DepositType DepositType,
    decimal DepositValue,
    decimal OnTimeRefundPercent,
    decimal LateCancellationRefundPercent,
    int RefundReviewDeadlineDays,
    bool RefundEnabled
) : ICommand;
```

- [ ] **Step 2: Update `UpdatePaymentPolicyHandler.cs`**

```csharp
tenant.PaymentPolicy.UpdatePolicy(
    command.RequirementType,
    command.DepositType,
    command.DepositValue,
    command.OnTimeRefundPercent,
    command.LateCancellationRefundPercent,
    command.RefundReviewDeadlineDays,
    command.RefundEnabled
);
```

- [ ] **Step 3: Update `GetPaymentPolicyQuery.cs`**

```csharp
public record PaymentPolicyDto(
    PaymentRequirementType RequirementType,
    DepositType DepositType,
    decimal DepositValue,
    decimal OnTimeRefundPercent,
    decimal LateCancellationRefundPercent,
    int RefundReviewDeadlineDays,
    bool RefundEnabled
);

public record GetPaymentPolicyQuery() : IQuery<PaymentPolicyDto>;
```

- [ ] **Step 4: Read and update `GetPaymentPolicyHandler.cs`**

Read the file first — if it constructs `PaymentPolicyDto` positionally from `tenant.PaymentPolicy`, replace the `AutomaticRefund` argument with `tenant.PaymentPolicy.RefundEnabled`, keeping argument order matching the DTO's new field order above.

- [ ] **Step 5: Build**

Run: `dotnet build ApexBooking.Core.Application`
Expected: errors remain elsewhere (other tasks), but none should reference `UpdatePaymentPolicyCommand`, `PaymentPolicyDto`, or `AutomaticRefund` in this feature folder anymore.

- [ ] **Step 6: Commit**

```bash
git add ApexBooking.Core.Application/Features/Tenancy/Commands/PaymentPolicy/ ApexBooking.Core.Application/Features/Tenancy/Queries/GetPaymentPolicy/
git commit -m "feat: expose RefundEnabled through the payment policy command/query"
```

---

## Task 8: `CancelBookingCommand`/Handler + `CancelBookingByTokenCommand`/Handler — thread e-wallet fields

**Files:**
- Modify: `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBooking/CancelBookingCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBooking/CancelBookingHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/PublicBookings/Commands/CancelBookingByToken/CancelBookingByTokenCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/PublicBookings/Commands/CancelBookingByToken/CancelBookingByTokenHandler.cs`

**Interfaces:**
- Consumes: `Tenant.CancelBooking`/`CancelBookingByCustomer` new signatures (Task 4).

- [ ] **Step 1: Update `CancelBookingCommand.cs`**

```csharp
public record CancelBookingCommand(
    Guid BookingId,
    string Reason,
    string? EwalletProvider,
    string? EwalletNumber,
    string? EwalletName
) : ICommand;
```

- [ ] **Step 2: Update `CancelBookingHandler.cs`**

```csharp
tenant.CancelBooking(command.BookingId, currentUserId, command.Reason, command.EwalletProvider, command.EwalletNumber, command.EwalletName);
```

- [ ] **Step 3: Update `CancelBookingByTokenCommand.cs`**

```csharp
public record CancelBookingByTokenCommand(
    string Token,
    string? Reason,
    string? EwalletProvider,
    string? EwalletNumber,
    string? EwalletName
) : ICommand;
```

- [ ] **Step 4: Update `CancelBookingByTokenHandler.cs`**

```csharp
tenant.CancelBookingByCustomer(payload.BookingId.Value, reason, command.EwalletProvider, command.EwalletNumber, command.EwalletName);
```

- [ ] **Step 5: Build**

Run: `dotnet build ApexBooking.Core.Application`
Expected: no errors referencing `CancelBookingCommand`, `CancelBookingByTokenCommand`, or their handlers.

- [ ] **Step 6: Commit**

```bash
git add ApexBooking.Core.Application/Features/Bookings/Commands/CancelBooking/ ApexBooking.Core.Application/Features/PublicBookings/Commands/CancelBookingByToken/
git commit -m "feat: accept e-wallet details on both staff and customer cancel-booking commands"
```

---

## Task 9: `GetCancellableBookingQuery`/Handler — `IsRefundEligible`

**Files:**
- Modify: `ApexBooking.Core.Application/Features/PublicBookings/Queries/GetCancellableBooking/GetCancellableBookingQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/PublicBookings/Queries/GetCancellableBooking/GetCancellableBookingHandler.cs`

**Interfaces:**
- Consumes: `PaymentPolicy.RefundEnabled` (Task 1), `Booking.RequiresUpfrontPayment`/`PaymentConfirmedVia` (existing).
- Produces: `CancellableBookingDto.IsRefundEligible`.

- [ ] **Step 1: Update `GetCancellableBookingQuery.cs`**

```csharp
public record CancellableBookingDto(
    string BookingReference,
    string ServiceName,
    string StaffName,
    string BranchName,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    bool CanCancelOnline,
    string? UnavailableReason,
    bool IsRefundEligible
);
```

- [ ] **Step 2: Update `GetCancellableBookingHandler.cs`**

Add `t => t.PaymentPolicy!` to the tenant's `includes` array (currently `[t => t.Bookings, t => t.Services, t => t.Members, t => t.Branches, t => t.BookingPolicy!]`), compute the flag, and pass it into the returned DTO:

```csharp
var isRefundEligible = tenant.PaymentPolicy?.RefundEnabled == true
    && booking.RequiresUpfrontPayment
    && booking.PaymentConfirmedVia == PaymentConfirmationMethod.Online;

return new CancellableBookingDto(
    booking.BookingReference,
    service?.Name ?? string.Empty,
    staff is not null ? $"{staff.FirstName} {staff.LastName}".Trim() : string.Empty,
    branch?.BranchName ?? string.Empty,
    booking.ScheduledDate,
    booking.ScheduledStartTime,
    canCancelOnline,
    unavailableReason,
    isRefundEligible);
```
(Needs `using ApexBooking.Core.Domain.Enums;` if `PaymentConfirmationMethod` isn't already imported — check the existing `using` block first.)

- [ ] **Step 3: Build**

Run: `dotnet build ApexBooking.Core.Application`
Expected: no errors referencing `GetCancellableBookingQuery`/`GetCancellableBookingHandler`.

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Application/Features/PublicBookings/Queries/GetCancellableBooking/
git commit -m "feat: surface refund eligibility on the public cancel-booking preview"
```

---

## Task 10: `CreateRefundRequestOnEligibleHandler` — build from the event's e-wallet fields

**Files:**
- Modify: `ApexBooking.Core.Application/Features/Bookings/Events/CreateRefundRequestOnEligibleHandler.cs`

**Interfaces:**
- Consumes: `BookingRefundEligibleDomainEvent` with `EwalletProvider`/`EwalletNumber`/`EwalletName` (Task 3), `RefundRequest.Create(...)` new signature (Task 2).

- [ ] **Step 1: Update the handler**

Replace the `RefundRequest.Create` call — drop the `booking` lookup that was only used for `PaymentMethodType`/`IsAutoRefundEligible` (the `tenant.Bookings` include and `booking` variable can go too, since nothing else in this handler used `booking`):

```csharp
public async Task Handle(DomainEventNotification<BookingRefundEligibleDomainEvent> notification, CancellationToken cancellationToken)
{
    var e = notification.DomainEvent;

    var tenant = await _unitOfWork.TenantRepository.GetAsync(
        predicate: t => t.TenantId == e.TenantId,
        includes: [t => t.Members]);

    if (tenant is null)
    {
        _logger.LogError(
            "Could not resolve Tenant {TenantId} to create a RefundRequest for Booking {BookingReference}.",
            e.TenantId, e.BookingReference);
        return;
    }

    var request = RefundRequest.Create(
        e.TenantId,
        e.BookingId,
        e.RefundAmount,
        e.CurrencyCode,
        e.EwalletProvider,
        e.EwalletNumber,
        e.EwalletName);

    await _refundRequestStore.AddAsync(request, cancellationToken);

    var recipients = tenant.Members.Where(m =>
        (m.Role == SystemRole.Owner || m.Role == SystemRole.Admin) && m.UserId.HasValue);
    var notifications = recipients
        .Select(m => Notification.Create(
            m.UserId!.Value,
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
```

- [ ] **Step 2: Build**

Run: `dotnet build ApexBooking.Core.Application`
Expected: no errors referencing `CreateRefundRequestOnEligibleHandler`.

- [ ] **Step 3: Commit**

```bash
git add ApexBooking.Core.Application/Features/Bookings/Events/CreateRefundRequestOnEligibleHandler.cs
git commit -m "feat: create RefundRequest directly with customer-submitted e-wallet details"
```

---

## Task 11: `ConfirmRefundRequestCommand`/Handler (receipt upload) + `RejectRefundRequestCommand`/Handler; remove the owner-gate/mark-sent/submit-ewallet commands

**Files:**
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Commands/ConfirmRefundRequest/ConfirmRefundRequestCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Commands/ConfirmRefundRequest/ConfirmRefundRequestHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Commands/RejectRefundRequest/RejectRefundRequestHandler.cs`
- Delete: `ApexBooking.Core.Application/Features/RefundRequests/Commands/ApproveOwnerGate/` (whole folder)
- Delete: `ApexBooking.Core.Application/Features/RefundRequests/Commands/DenyOwnerGate/` (whole folder)
- Delete: `ApexBooking.Core.Application/Features/RefundRequests/Commands/MarkManualRefundSent/` (whole folder)
- Delete: `ApexBooking.Core.Application/Features/PublicBookings/Commands/SubmitRefundEwalletDetails/` (whole folder)

**Interfaces:**
- Consumes: `IFileStorageService.SaveAsync(Stream, string fileName, string contentType, CancellationToken)` (existing, `ApexBooking.Core.Domain.Services`); `RefundRequest.Confirm`/`Reject` (Task 2); `Booking.ConfirmReviewedRefund`/`RejectReviewedRefund` (Task 3).
- Produces: `ConfirmRefundRequestCommand(Guid RefundRequestId, Stream ReceiptContent, string ReceiptContentType, string ReceiptFileExtension) : ICommand`.

- [ ] **Step 1: Update `ConfirmRefundRequestCommand.cs`**

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest
{
    // Content-type/size are validated by the controller before this is ever dispatched — same
    // edge-validation split as UpdateMyProfilePhotoCommand.
    public record ConfirmRefundRequestCommand(
        Guid RefundRequestId,
        Stream ReceiptContent,
        string ReceiptContentType,
        string ReceiptFileExtension
    ) : ICommand;
}
```

- [ ] **Step 2: Rewrite `ConfirmRefundRequestHandler.cs`**

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest
{
    public class ConfirmRefundRequestHandler : ICommandHandler<ConfirmRefundRequestCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IFileStorageService _fileStorage;
        private readonly IUserContextService _userContext;

        public ConfirmRefundRequestHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IFileStorageService fileStorage,
            IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _fileStorage = fileStorage;
            _userContext = userContext;
        }

        public async Task Handle(ConfirmRefundRequestCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            var tenantId = _userContext.GetCurrentTenantId();
            if (request.TenantId != tenantId)
                throw new BusinessRuleBrokenException("Refund request not found.");

            var userId = _userContext.GetCurrentUserId();

            var fileName = $"{tenantId!.Value}/{request.Id}/{Guid.NewGuid()}{command.ReceiptFileExtension}";
            var receiptUrl = await _fileStorage.SaveAsync(command.ReceiptContent, fileName, command.ReceiptContentType, cancellationToken);

            request.Confirm(userId, receiptUrl);
            await _refundRequestStore.UpdateAsync(request, cancellationToken);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == request.TenantId,
                includes: [t => t.Bookings]);

            var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);
            if (booking is null)
                return;

            booking.ConfirmReviewedRefund(request.RequestedAmount, receiptUrl);
            _unitOfWork.TenantRepository.Update(tenant!);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```
(Uses `refund-receipts/{tenantId}/{refundRequestId}/{guid}{ext}`-shaped naming via `IFileStorageService`, matching the spec's path convention — the leading segment differs slightly from the spec's literal string only in that it omits a redundant `refund-receipts/` prefix since `IFileStorageService.SaveAsync` already scopes storage; if the storage root needs an explicit top-level folder to keep receipt files visually separate from profile photos, prefix `fileName` with `refund-receipts/` — check `LocalDiskFileStorageService.cs`'s root path handling before deciding, and prefer whichever matches how `UpdateMyProfilePhotoHandler` scopes its own files.)

- [ ] **Step 3: Rewrite `RejectRefundRequestHandler.cs`**

```csharp
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest
{
    public class RejectRefundRequestHandler : ICommandHandler<RejectRefundRequestCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUserContextService _userContext;

        public RejectRefundRequestHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _userContext = userContext;
        }

        public async Task Handle(RejectRefundRequestCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            var tenantId = _userContext.GetCurrentTenantId();
            if (request.TenantId != tenantId)
                throw new BusinessRuleBrokenException("Refund request not found.");

            var userId = _userContext.GetCurrentUserId();
            request.Reject(userId, command.Reason);
            await _refundRequestStore.UpdateAsync(request, cancellationToken);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == request.TenantId,
                includes: [t => t.Bookings]);

            var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);
            if (booking is null)
                return;

            booking.RejectReviewedRefund(command.Reason);
            _unitOfWork.TenantRepository.Update(tenant!);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```
(`RejectRefundRequestCommand.cs` itself is unchanged — still `record RejectRefundRequestCommand(Guid RefundRequestId, string Reason) : ICommand`.)

- [ ] **Step 4: Delete the four removed feature folders**

```bash
git rm -r ApexBooking.Core.Application/Features/RefundRequests/Commands/ApproveOwnerGate/
git rm -r ApexBooking.Core.Application/Features/RefundRequests/Commands/DenyOwnerGate/
git rm -r ApexBooking.Core.Application/Features/RefundRequests/Commands/MarkManualRefundSent/
git rm -r ApexBooking.Core.Application/Features/PublicBookings/Commands/SubmitRefundEwalletDetails/
```

- [ ] **Step 5: Build**

Run: `dotnet build ApexBooking.Core.Application`
Expected: new errors will surface in `RefundRequestsController.cs` and `RefundStatusController.cs` (Task 15 fixes those) — confirm no errors remain inside the `Features/RefundRequests/Commands/` or `Features/PublicBookings/Commands/` folders themselves.

- [ ] **Step 6: Commit**

```bash
git add ApexBooking.Core.Application/Features/RefundRequests/Commands/ApproveOwnerGate/ ApexBooking.Core.Application/Features/RefundRequests/Commands/DenyOwnerGate/ ApexBooking.Core.Application/Features/RefundRequests/Commands/MarkManualRefundSent/ ApexBooking.Core.Application/Features/PublicBookings/Commands/SubmitRefundEwalletDetails/ ApexBooking.Core.Application/Features/RefundRequests/Commands/ConfirmRefundRequest/ ApexBooking.Core.Application/Features/RefundRequests/Commands/RejectRefundRequest/
git commit -m "feat: Confirm requires a receipt upload; drop the owner-gate and mark-sent/submit-ewallet commands"
```

---

## Task 12: Refund decision emails — new Confirmed handler, simplified cancellation email

**Files:**
- Modify: `ApexBooking.Core.Domain/Services/Notification/Bookings/IBookingNotificationService.cs`
- Modify: `ApexBooking.Infrastructure/ExternalServices/BookingNotificationService/BookingNotificationService.cs`
- Create: `ApexBooking.Core.Application/Features/Bookings/Events/SendRefundConfirmationEmailHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/Bookings/Events/SendBookingCancellationEmailHandler.cs`

**Interfaces:**
- Consumes: `BookingRefundConfirmedDomainEvent` (Task 3).
- Produces: `IBookingNotificationService.SendRefundConfirmationEmailAsync(string to, string customerName, string businessName, string bookingReference, decimal amount, string currencyCode, string receiptUrl, CancellationToken ct)`.

- [ ] **Step 1: Add the interface method**

In `IBookingNotificationService.cs`, add after `SendRefundRejectionEmailAsync`:

```csharp
Task SendRefundConfirmationEmailAsync(
    string to,
    string customerName,
    string businessName,
    string bookingReference,
    decimal amount,
    string currencyCode,
    string receiptUrl,
    CancellationToken ct);
```

- [ ] **Step 2: Implement it in `BookingNotificationService.cs`**

Add after `SendRefundRejectionEmailAsync`, following the same `H(...)`/`SafeUrl(...)` encoding discipline as every other method in this file:

```csharp
public Task SendRefundConfirmationEmailAsync(
    string to,
    string customerName,
    string businessName,
    string bookingReference,
    decimal amount,
    string currencyCode,
    string receiptUrl,
    CancellationToken ct)
{
    var safeReceiptUrl = SafeUrl(receiptUrl);
    var receiptBlock = safeReceiptUrl is null
        ? string.Empty
        : $@"
        <div style='text-align: center; margin: 20px 0;'>
            <a href='{safeReceiptUrl}' style='display:inline-block;padding:10px 20px;border:1px solid #198754;border-radius:6px;color:#198754;text-decoration:none;font-weight:bold;'>View your refund receipt</a>
        </div>";

    var body = $@"
    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
        <h2 style='color: #2c3e50; border-bottom: 2px solid #198754; padding-bottom: 10px;'>Your Refund Was Sent</h2>

        <p>Hi <strong>{H(customerName)}</strong>,</p>

        <p>Your refund for the appointment with <strong>{H(businessName)}</strong> has been sent.</p>

        <div style='background: #f8f9fa; border-left: 4px solid #198754; padding: 15px; margin: 20px 0; border-radius: 4px;'>
            <p style='margin: 0 0 8px 0; font-size: 14px; color: #555;'><strong>Appointment Tracking Reference:</strong></p>
            <p style='margin: 0 0 12px 0; font-size: 18px; font-weight: bold; color: #198754; letter-spacing: 1px;'>{H(bookingReference)}</p>
            <p style='margin: 0; font-size: 14px; color: #555;'><strong>Amount:</strong></p>
            <p style='margin: 5px 0 0 0;'>{amount:0.00} {H(currencyCode)}</p>
        </div>
        {receiptBlock}
        <p>If you have questions, please contact the business directly.</p>

        <p style='margin-top: 30px; font-size: 14px; color: #777;'>
            Best regards,<br>
            The Team at <strong>{H(businessName)}</strong>
        </p>
        <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
        <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
    </div>";

    return _notification.SendEmailAsync(
        to: to,
        subject: $"Your refund was sent — {businessName}",
        content: body
    );
}
```

- [ ] **Step 3: Create `SendRefundConfirmationEmailHandler.cs`**

Mirror `SendRefundRejectionEmailHandler.cs` exactly, subscribing to the new event:

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
    public class SendRefundConfirmationEmailHandler
        : INotificationHandler<DomainEventNotification<BookingRefundConfirmedDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly ILogger<SendRefundConfirmationEmailHandler> _logger;

        public SendRefundConfirmationEmailHandler(
            IUnitOfWork unitOfWork,
            IBookingNotificationService bookingNotificationService,
            ILogger<SendRefundConfirmationEmailHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _bookingNotificationService = bookingNotificationService;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingRefundConfirmedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.BusinessProfile!, t => t.Bookings]);

            if (tenant?.BusinessProfile is null)
            {
                _logger.LogError("Could not resolve workspace details for Tenant {TenantId}. Refund confirmation email was aborted.", e.TenantId);
                return;
            }

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == e.BookingId);
            if (booking is null)
                return;

            var customer = await _unitOfWork.CustomerRepository.GetAsync(predicate: c => c.CustomerId == booking.CustomerId);
            if (customer?.Contact.Email is not { } customerEmail)
            {
                _logger.LogWarning("Customer {CustomerId} has no email on file. Refund confirmation email for {BookingReference} was skipped.", booking.CustomerId.Value, e.BookingReference);
                return;
            }

            await _bookingNotificationService.SendRefundConfirmationEmailAsync(
                to: customerEmail,
                customerName: customer.Contact.Name,
                businessName: tenant.BusinessProfile.BusinessName,
                bookingReference: e.BookingReference,
                amount: e.RefundedAmount,
                currencyCode: e.CurrencyCode,
                receiptUrl: e.ReceiptUrl,
                ct: cancellationToken
            );
        }
    }
}
```

- [ ] **Step 4: Simplify `SendBookingCancellationEmailHandler.cs`**

This handler no longer needs to guess at refund status or build a status-check link — `SendRefundConfirmationEmailHandler`/`SendRefundRejectionEmailHandler` now reliably cover the actual outcome, exactly when it's decided. Replace the `refundNote`/`refundStatusUrl` block (lines ~72-91) with a single static note when a refund is pending, and drop the `ICancellationTokenService`/`IAppUrlService` dependencies if nothing else in this file uses them (check before removing the constructor parameters):

```csharp
string? refundNote = booking.RefundStatus == RefundStatus.Pending
    ? "Your refund is being reviewed — we'll email you once it's decided."
    : null;

await _bookingNotificationService.SendBookingCancellationEmailAsync(
    to: customerEmail,
    customerName: customer.Contact.Name,
    businessName: tenant.BusinessProfile.BusinessName,
    serviceName: serviceName,
    bookingReference: e.BookingReference,
    refundNote: refundNote,
    refundStatusUrl: null,
    ct: cancellationToken
);
```
Leave `IBookingNotificationService.SendBookingCancellationEmailAsync`'s signature untouched (still accepts `refundStatusUrl`, just always `null` now) — narrowing it is unnecessary churn for this pass.

- [ ] **Step 5: Build**

Run: `dotnet build ApexBooking.Core.Application ApexBooking.Infrastructure`
Expected: no errors referencing `IBookingNotificationService`, `BookingNotificationService`, `SendRefundConfirmationEmailHandler`, or `SendBookingCancellationEmailHandler`.

- [ ] **Step 6: Commit**

```bash
git add ApexBooking.Core.Domain/Services/Notification/Bookings/IBookingNotificationService.cs ApexBooking.Infrastructure/ExternalServices/BookingNotificationService/BookingNotificationService.cs ApexBooking.Core.Application/Features/Bookings/Events/SendRefundConfirmationEmailHandler.cs ApexBooking.Core.Application/Features/Bookings/Events/SendBookingCancellationEmailHandler.cs
git commit -m "feat: send a dedicated refund-confirmed email with a receipt link; simplify the cancellation email"
```

---

## Task 13: Query DTO updates — `GetPendingRefundRequests`, `GetRefundLog`, `GetRefundStatus`

**Files:**
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/GetPendingRefundRequestsQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetPendingRefundRequests/GetPendingRefundRequestsHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetRefundLog/GetRefundLogQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/RefundRequests/Queries/GetRefundLog/GetRefundLogHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/PublicBookings/Queries/GetRefundStatus/GetRefundStatusQuery.cs`
- Modify: `ApexBooking.Core.Application/Features/PublicBookings/Queries/GetRefundStatus/GetRefundStatusHandler.cs`

**Interfaces:**
- Consumes: `RefundRequest`'s new shape (Task 2).

- [ ] **Step 1: Update `GetPendingRefundRequestsQuery.cs`**

```csharp
public record RefundRequestSummaryDto(
    Guid Id,
    Guid BookingId,
    string BookingReference,
    string CustomerName,
    decimal RequestedAmount,
    decimal AmountPaid,
    string? PayMongoPaymentId,
    string CurrencyCode,
    RefundRequestStatus Status,
    string? RejectionReason,
    string CustomerEwalletProvider,
    string CustomerEwalletNumber,
    string CustomerEwalletName,
    string? ReceiptUrl,
    DateTime CreatedAt,
    DateTime DueDate
);

public record GetPendingRefundRequestsQuery(int PageNumber = 1, int PageSize = 10) : IQuery<QueryResult<RefundRequestSummaryDto>>;
```

- [ ] **Step 2: Update `GetPendingRefundRequestsHandler.cs`**

Drop the `request.IsAutoRefundEligible` argument and add `CustomerEwalletName`/`ReceiptUrl`, matching the new positional order:

```csharp
result.Add(new RefundRequestSummaryDto(
    request.Id,
    request.BookingId,
    booking?.BookingReference ?? "(unknown)",
    customer?.Contact.Name ?? "(unknown)",
    request.RequestedAmount,
    booking?.AmountDue ?? request.RequestedAmount,
    booking?.PayMongoPaymentId,
    request.CurrencyCode,
    request.Status,
    request.RejectionReason,
    request.CustomerEwalletProvider,
    request.CustomerEwalletNumber,
    request.CustomerEwalletName,
    request.ReceiptUrl,
    request.CreatedAt,
    request.CreatedAt.AddDays(deadlineDays)));
```

- [ ] **Step 3: Update `GetRefundLogQuery.cs`**

Drop `PaymentMethodType` (the `Booking` property it read is gone):

```csharp
public record RefundLogEntryDto(
    Guid Id,
    string BookingReference,
    decimal Amount,
    string CurrencyCode,
    RefundRequestStatus Status,
    DateTime ProcessedAt
);

public record GetRefundLogQuery(int Limit = 20) : IQuery<IReadOnlyCollection<RefundLogEntryDto>>;
```

- [ ] **Step 4: Update `GetRefundLogHandler.cs`**

```csharp
result.Add(new RefundLogEntryDto(
    request.Id,
    booking?.BookingReference ?? "(unknown)",
    request.RequestedAmount,
    request.CurrencyCode,
    request.Status,
    request.UpdatedAt));
```

- [ ] **Step 5: Update `GetRefundStatusQuery.cs`**

Drop `NeedsEwalletDetails` (no longer meaningful — details are submitted at cancel time), add `ReceiptUrl`:

```csharp
public record RefundStatusDto(
    string BookingReference,
    RefundRequestStatus? Status,
    decimal? Amount,
    string CurrencyCode,
    string? BusinessContactPhoneNumber,
    string? ReceiptUrl
);

public record GetRefundStatusQuery(string Token) : IQuery<RefundStatusDto>;
```

- [ ] **Step 6: Update `GetRefundStatusHandler.cs`**

```csharp
return new RefundStatusDto(
    booking.BookingReference,
    request?.Status,
    request?.RequestedAmount ?? booking.RefundedAmount,
    booking.CurrencyCode,
    tenant?.BusinessProfile?.ContactPhoneNumber,
    request?.ReceiptUrl
);
```

- [ ] **Step 7: Build**

Run: `dotnet build ApexBooking.Core.Application`
Expected: no errors referencing these three query folders.

- [ ] **Step 8: Commit**

```bash
git add ApexBooking.Core.Application/Features/RefundRequests/Queries/ ApexBooking.Core.Application/Features/PublicBookings/Queries/GetRefundStatus/
git commit -m "feat: update refund query DTOs for the collapsed 3-state model"
```

---

## Task 14: Remove PayMongo refund API and its automatic-refund handler

**Files:**
- Delete: `ApexBooking.Core.Application/Features/Bookings/Events/ProcessRefundOnBookingCancelledHandler.cs`
- Modify: `ApexBooking.Core.Domain/Services/Paymongo/IPayMongoService.cs`
- Modify: `ApexBooking.Infrastructure/ExternalServices/PayMongo/PayMongoService.cs`

**Interfaces:**
- Consumes: nothing (this task only removes code).

- [ ] **Step 1: Delete the handler**

```bash
git rm ApexBooking.Core.Application/Features/Bookings/Events/ProcessRefundOnBookingCancelledHandler.cs
```
(This was the sole subscriber to `BookingRefundDueDomainEvent`, already removed in Task 3.)

- [ ] **Step 2: Remove `CreateRefundAsync` from `IPayMongoService.cs`**

```csharp
namespace ApexBooking.Core.Domain.Services.Paymongo
{
    public interface IPayMongoService
    {
        Task<PayMongoSourceResult> CreatePaymentSourceAsync(
            string tenantSecretKey,
            Guid bookingId,
            Guid branchId,
            decimal amountPhp,
            string description,
            CancellationToken cancellationToken);
    }
}
```

- [ ] **Step 3: Remove the implementation from `PayMongoService.cs`**

Delete the entire `CreateRefundAsync` method (everything from `public async Task<PayMongoRefundResult> CreateRefundAsync(` through its closing brace).

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: only remaining errors should be in `RefundRequestsController.cs`/`RefundStatusController.cs` (Task 15) and possibly leftover `PayMongoRefundResult`/`PayMongoRefundRequest`/`PayMongoRefundResponse` contract types with no more callers — if the compiler doesn't flag those contract types as errors (unused types don't error in C#), leave them; they're harmless dead code outside this task's scope and removing them isn't required for correctness. If you want to tidy them anyway, confirm via `grep -rn "PayMongoRefund" --include=*.cs` that nothing else references them first.

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Domain/Services/Paymongo/IPayMongoService.cs ApexBooking.Infrastructure/ExternalServices/PayMongo/PayMongoService.cs
git rm ApexBooking.Core.Application/Features/Bookings/Events/ProcessRefundOnBookingCancelledHandler.cs 2>/dev/null || true
git commit -m "chore: remove the PayMongo automatic-refund API path"
```

---

## Task 15: Controllers — multipart Confirm, drop removed endpoints

**Files:**
- Modify: `ApexBooking.WebApi/Controllers/RefundRequestsController.cs`
- Modify: `ApexBooking.WebApi/Controllers/RefundStatusController.cs`

**Interfaces:**
- Consumes: `ConfirmRefundRequestCommand(Guid, Stream, string, string)` (Task 11), `RejectRefundRequestCommand` (unchanged).

- [ ] **Step 1: Rewrite `RefundRequestsController.cs`**

Follows `AccountController.UploadMyProfilePhoto`'s exact validation shape (size cap, allowed content types, extension mapping):

```csharp
using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest;
using ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests;
using ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog;
using ApexBooking.SharedKernel.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/refund-requests")]
    [Authorize]
    public class RefundRequestsController : ControllerBase
    {
        private const long MaxReceiptSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedReceiptContentTypes = ["image/jpeg", "image/png", "image/webp"];

        private readonly IMediator _mediator;

        public RefundRequestsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Policy = "ManagementOnly")]
        [ProducesResponseType(typeof(QueryResult<RefundRequestSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPending([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _mediator.Send(new GetPendingRefundRequestsQuery(pageNumber, pageSize));
            return Ok(result);
        }

        [HttpGet("log")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(typeof(IReadOnlyCollection<RefundLogEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLog([FromQuery] int limit = 20)
        {
            var result = await _mediator.Send(new GetRefundLogQuery(limit));
            return Ok(result);
        }

        [HttpPost("{id:guid}/confirm")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Confirm(Guid id, [FromForm] IFormFile receipt)
        {
            if (receipt is null || receipt.Length == 0)
                return Problem(title: "Validation Error", detail: "A receipt image is required.", statusCode: StatusCodes.Status400BadRequest);

            if (receipt.Length > MaxReceiptSizeBytes)
                return Problem(title: "Validation Error", detail: "Receipt must be 5MB or smaller.", statusCode: StatusCodes.Status400BadRequest);

            if (Array.IndexOf(AllowedReceiptContentTypes, receipt.ContentType) < 0)
                return Problem(title: "Validation Error", detail: "Receipt must be a JPEG, PNG, or WebP image.", statusCode: StatusCodes.Status400BadRequest);

            var extension = receipt.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg",
            };

            await using var stream = receipt.OpenReadStream();
            await _mediator.Send(new ConfirmRefundRequestCommand(id, stream, receipt.ContentType, extension));
            return NoContent();
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRefundRequestBody body)
        {
            await _mediator.Send(new RejectRefundRequestCommand(id, body.Reason));
            return NoContent();
        }
    }

    public record RejectRefundRequestBody(string Reason);
}
```

- [ ] **Step 2: Update `RefundStatusController.cs`**

Remove the `SubmitEwalletDetails` action and `SubmitRefundEwalletDetailsBody` record entirely:

```csharp
using System.Threading.Tasks;
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
    }
}
```

- [ ] **Step 3: Build the whole solution**

Run: `dotnet build`
Expected: SUCCESS — this is the first point where the entire solution should compile clean.

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.WebApi/Controllers/RefundRequestsController.cs ApexBooking.WebApi/Controllers/RefundStatusController.cs
git commit -m "feat: multipart receipt upload on Confirm; drop owner-gate and ewallet-submission endpoints"
```

---

## Task 16: Full verification pass

**Files:** none (verification only).

- [ ] **Step 1: Build the whole solution**

Run: `dotnet build`
Expected: SUCCESS, zero warnings-as-errors if this repo treats any as such (check for a `TreatWarningsAsErrors` setting first — if present, resolve any new warnings from this work).

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test`
Expected: SUCCESS. Pay particular attention to `ApexBooking.Core.Domain.UnitTests` (Tasks 1-3's new/rewritten tests) and any Application-layer tests that touched `RefundRequest`, `Booking.Cancel`/`CancelByCustomer`, or `PaymentPolicy.UpdatePolicy` — grep for any test file not already covered by this plan that still calls one of the old signatures:

Run: `grep -rln "AutomaticRefund\|IsAutoRefundEligible\|BookingRefundDueDomainEvent\|ApproveOwnerGate\|DenyOwnerGate\|MarkManualRefundSent\|SubmitRefundEwalletDetails\|RefundDecisionAction" --include=*.cs .`
Expected: no results outside the Migrations folder (old migration files legitimately still reference the old column names historically — that's fine) and outside `docs/` (historical spec/plan prose, not code).

- [ ] **Step 3: If the Task 6 migration wasn't generated yet (build was broken at that point), generate it now**

Run: `dotnet ef migrations add RefundManualTransferRedesign --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`
Read the generated file to confirm it matches Task 6 Step 6's description, then commit it (`git add` + `git commit -m "feat: migrate schema for the refund manual-transfer redesign"`) if this step actually ran (skip entirely if Task 6 already committed a working migration).

- [ ] **Step 4: Report status**

Summarize: build result, test result, and confirm the migration file exists and was reviewed. Do not run `dotnet ef database update` — remind the user it's pending their own run per the Global Constraints note.

---

## Self-Review Notes

- **Spec coverage:** All-manual refund model (Tasks 2, 3, 11, 14) ✓. `RefundEnabled` gate, default false including existing tenants (Tasks 1, 3, 6, 7) ✓. Up-front e-wallet collection enforced in the domain (Task 3) ✓. Staff cancel modal fields (Task 8, backend side — frontend plan covers the UI) ✓. No owner-gate (Tasks 2, 11) ✓. Confirm/Reject only, receipt required on Confirm (Tasks 2, 11, 15) ✓. Receipt delivered as a link (Task 12) ✓. Old automatic-refund machinery removed (Tasks 3, 5, 14) ✓. Migration with existing-tenant backfill to `false` (Task 6) ✓.
- **Placeholder scan:** no TBD/TODO; the one open call (Task 11's exact `fileName` prefix) is a bounded either/or resolved by reading one existing file, not an unresolved requirement.
- **Type consistency:** `RefundRequestStatus`, `RefundStatus`, `RefundRequest.Confirm/Reject`, `Booking.ConfirmReviewedRefund/RejectReviewedRefund`, `BookingRefundEligibleDomainEvent`/`BookingRefundConfirmedDomainEvent` field names all match between the task that defines them (2, 3) and every task that consumes them (4, 8, 9, 10, 11, 12, 13).
