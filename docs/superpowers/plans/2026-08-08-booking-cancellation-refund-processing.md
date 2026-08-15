# Cancellation Refund Processing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When a paid-online booking is cancelled (by staff or by the customer), automatically issue a PayMongo refund sized by the tenant's cancellation-cutoff timing, and record the outcome on the booking.

**Architecture:** Domain logic (`Booking.EvaluateRefund`) computes eligibility/amount at cancel time and raises a new `IReliableDomainEvent` only when a refund is due. A dedicated outbox-backed event handler (`ProcessRefundOnBookingCancelledHandler`) calls PayMongo's Refund API and writes the outcome back onto the booking. Capturing PayMongo's own Payment ID at webhook-confirmation time is a hard prerequisite, since refunds are addressed by that ID, not by our booking ID.

**Tech Stack:** .NET, EF Core, MediatR (CQRS + domain-event notifications), the existing outbox/relay pipeline (`UnitOfWork.CompleteAsync` → `OutboxMessage` → `IOutboxRelayService`).

## Global Constraints

- Spec: [docs/superpowers/specs/2026-08-08-booking-cancellation-refund-processing-design.md](../specs/2026-08-08-booking-cancellation-refund-processing-design.md)
- Refund only applies when `RequiresUpfrontPayment && PaymentConfirmedVia == PaymentConfirmationMethod.Online` — pay-in-visit bookings are always excluded.
- A refund API failure must never roll back the cancellation itself — the booking stays `Cancelled` regardless of refund outcome.
- No refund-status webhook handling, no manual retry UI, no ad-hoc refund endpoint (see spec's Non-goals).
- Per standing session instruction: implement only — do not run `dotnet build`, `dotnet test`, `dotnet ef migrations`, or `git commit` at the end of each task. The user runs all of those manually. Test code is still written inline for documentation/future use, just not executed here.
- **Architecture correction made during planning** (not in the spec, discovered while reading the codebase): the spec described the refund trigger as "matching the existing notification-handler pattern," which at spec-writing time meant a plain synchronous `BookingCancelledDomainEvent` subscriber. Re-reading [UnitOfWork.cs](../../../ApexBooking.Core.Persistence/UnitOfWork.cs) and [IReliableDomainEvent.cs](../../../ApexBooking.Core.Domain/Events/IReliableDomainEvent.cs) found that this codebase's outbox pipeline (assumed unbuilt in stale session notes) is actually live: any event implementing `IReliableDomainEvent` gets written to the transactional outbox and delivered at-least-once (5 retries) by `OutboxRelayService`, instead of firing synchronously in-request. That is exactly the guarantee an external PayMongo call needs, and is genuinely the closer-matching existing pattern. Rather than promoting the already-shipped `BookingCancelledDomainEvent` (which would also silently move its other subscriber, `NotifyTenantOnBookingCancelledHandler`'s real-time owner notification, onto the async outbox path — an unrelated behavior change), this plan adds a new dedicated `BookingRefundDueDomainEvent : IReliableDomainEvent`, raised only when a refund is actually due. Zero risk to the existing cancellation-notification behavior, and the refund call gets real delivery guarantees.

---

### Task 1: `RefundStatus` enum + `BookingRefundDueDomainEvent`

**Files:**
- Create: `ApexBooking.Core.Domain/Enums/RefundStatus.cs`
- Modify: `ApexBooking.Core.Domain/Events/BookingEvents.cs`

**Interfaces:**
- Produces: `enum RefundStatus { None, Pending, Processing, Succeeded, Failed }` (mirrors `OutboxMessageStatus`'s naming style and PayMongo's own refund status values).
- Produces: `record BookingRefundDueDomainEvent(TenantId TenantId, Guid BookingId, string BookingReference, decimal RefundAmount, string CurrencyCode, DateTime OccurredAt) : IReliableDomainEvent`

- [ ] **Step 1: Create the enum**

```csharp
namespace ApexBooking.Core.Domain.Enums;

public enum RefundStatus
{
    None,
    Pending,
    Processing,
    Succeeded,
    Failed
}
```

- [ ] **Step 2: Add the event to `BookingEvents.cs`**

Append to the end of the file (after `PaymentCapturedDomainEvent`):

```csharp
// Raised only when a cancellation qualifies for a refund (see Booking.EvaluateRefund) — the
// PayMongo API call this drives is an external call that needs at-least-once delivery, so this
// is a dedicated IReliableDomainEvent rather than piggybacking on BookingCancelledDomainEvent,
// which also has a synchronous, non-reliable subscriber (NotifyTenantOnBookingCancelledHandler)
// that must keep its current real-time delivery timing unchanged.
public record BookingRefundDueDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    decimal RefundAmount,
    string CurrencyCode,
    DateTime OccurredAt
) : IReliableDomainEvent;
```

Add `using ApexBooking.Core.Domain.Events;`-equivalent: `IReliableDomainEvent` already lives in the same `ApexBooking.Core.Domain.Events` namespace as this file, so no new `using` is needed.

- [ ] **Step 3: Commit** (when you reach your own natural checkpoint — user commits manually per standing instruction, skip if executing solo mid-plan)

---

### Task 2: `Booking` entity — refund fields, `EvaluateRefund`, `Cancel`/`CancelByCustomer` signatures, `ConfirmPayment` payment-ID capture

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Booking.cs`

**Interfaces:**
- Consumes: `RefundStatus` (Task 1), `BookingRefundDueDomainEvent` (Task 1), `BookingPolicy`/`PaymentPolicy` (existing, same namespace `ApexBooking.Core.Domain.Entities`).
- Produces:
  - `public string? PayMongoPaymentId { get; private set; }`
  - `public RefundStatus RefundStatus { get; private set; }` (defaults `RefundStatus.None`)
  - `public decimal? RefundedAmount { get; private set; }`
  - `public DateTime? RefundedAt { get; private set; }`
  - `public void ConfirmPayment(PaymentConfirmationMethod method, string? payMongoPaymentId = null)` — signature change (was `ConfirmPayment(PaymentConfirmationMethod method)`)
  - `public void Cancel(Guid adminUserId, string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)` — signature change
  - `public void CancelByCustomer(string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)` — signature change
  - `public void RecordRefundOutcome(RefundStatus status, decimal? amount)`

- [ ] **Step 1: Add the four new properties**

Add just below the existing `PaymentConfirmedVia` property (around line 41):

```csharp
public PaymentConfirmationMethod? PaymentConfirmedVia { get; private set; }
public string? PayMongoPaymentId { get; private set; }

// Refund tracking — only ever set for bookings that were RequiresUpfrontPayment +
// PaymentConfirmedVia == Online at cancellation time. See EvaluateRefund below.
public RefundStatus RefundStatus { get; private set; } = RefundStatus.None;
public decimal? RefundedAmount { get; private set; }
public DateTime? RefundedAt { get; private set; }
```

Add `using ApexBooking.Core.Domain.Enums;` is already present at the top of the file (it already imports `Enums` for `BookingStatus`/`PaymentConfirmationMethod`), so `RefundStatus` resolves with no new using.

- [ ] **Step 2: Update `ConfirmPayment` to capture the PayMongo payment ID**

Replace:

```csharp
        // ── Webhook Gateway State Transition ───────────────────────────────────
        public void ConfirmPayment(PaymentConfirmationMethod method)
        {
            if (Status != BookingStatus.PendingPayment)
                throw new BusinessRuleBrokenException("Only appointments pending payment can have their transactions verified.");

            Status = BookingStatus.Scheduled;
            PaymentConfirmedVia = method;
            UpdatedAt = DateTime.UtcNow;
```

With:

```csharp
        // ── Webhook Gateway State Transition ───────────────────────────────────
        // payMongoPaymentId is only ever passed for the Online path (the webhook handler) — the
        // arrival-scan PayInVisit fallback (Tenant.RecordBookingArrival) has no PayMongo payment
        // to reference, so it correctly omits it and this stays null for that booking forever.
        public void ConfirmPayment(PaymentConfirmationMethod method, string? payMongoPaymentId = null)
        {
            if (Status != BookingStatus.PendingPayment)
                throw new BusinessRuleBrokenException("Only appointments pending payment can have their transactions verified.");

            Status = BookingStatus.Scheduled;
            PaymentConfirmedVia = method;
            PayMongoPaymentId = payMongoPaymentId;
            UpdatedAt = DateTime.UtcNow;
```

(Leave the rest of the method — the two `AddDomainEvent` calls — untouched.)

- [ ] **Step 3: Add `EvaluateRefund` private helper**

Add this private method directly above `Cancel` (around line 279):

```csharp
        // Timing-based, not actor-based — the same calculation is used whether a staff member or
        // the customer themselves triggered the cancellation. Only ever returns ShouldRefund: true
        // for bookings that were actually paid online; pay-in-visit bookings are never refunded.
        private (bool ShouldRefund, decimal Amount) EvaluateRefund(BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)
        {
            if (!RequiresUpfrontPayment || PaymentConfirmedVia != PaymentConfirmationMethod.Online)
                return (false, 0m);

            var scheduledAt = ScheduledDate.ToDateTime(ScheduledStartTime);
            var cutoffHours = bookingPolicy?.CancellationCutoffHours ?? 0;
            var isOnTime = DateTime.UtcNow.AddHours(cutoffHours) <= scheduledAt;

            if (isOnTime)
                return (true, AmountDue);

            return (bookingPolicy?.LateCancellationPolicy ?? CancellationPolicy.NoRefund) switch
            {
                CancellationPolicy.FullRefund => (true, AmountDue),
                CancellationPolicy.PartialRefund => (true, AmountDue * ((paymentPolicy?.RefundPercent ?? 0m) / 100m)),
                _ => (false, 0m),
            };
        }
```

- [ ] **Step 4: Wire `EvaluateRefund` into `Cancel`**

Replace:

```csharp
        public void Cancel(Guid adminUserId, string reason)
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
        }
```

With:

```csharp
        public void Cancel(Guid adminUserId, string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)
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

            var (shouldRefund, refundAmount) = EvaluateRefund(bookingPolicy, paymentPolicy);
            if (shouldRefund)
            {
                RefundStatus = RefundStatus.Pending;
                AddDomainEvent(new BookingRefundDueDomainEvent(
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

- [ ] **Step 5: Wire `EvaluateRefund` into `CancelByCustomer`**

Replace:

```csharp
        public void CancelByCustomer(string reason)
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
        }
```

With:

```csharp
        public void CancelByCustomer(string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)
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

            var (shouldRefund, refundAmount) = EvaluateRefund(bookingPolicy, paymentPolicy);
            if (shouldRefund)
            {
                RefundStatus = RefundStatus.Pending;
                AddDomainEvent(new BookingRefundDueDomainEvent(
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

- [ ] **Step 6: Add `RecordRefundOutcome`**

Add at the end of the class, just before the closing `}` of `Booking`:

```csharp
        // Called by ProcessRefundOnBookingCancelledHandler after the PayMongo refund API call
        // resolves (success or failure). Public, not internal — that handler lives in
        // ApexBooking.Core.Application, a different assembly than this entity, and this solution
        // has no InternalsVisibleTo wiring (unlike RecordArrival/ConfirmPayment's callers, which
        // all stay inside Tenant.cs, same assembly).
        public void RecordRefundOutcome(RefundStatus status, decimal? amount)
        {
            RefundStatus = status;
            RefundedAmount = amount;
            RefundedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 7: Write unit tests** (for the user to run later — not executed as part of this task)

Create/extend `ApexBooking.Core.Domain.UnitTests/Entities/BookingRefundTests.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.ValueObjects;
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

    [Fact]
    public void Cancel_OnTime_RaisesFullRefund()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId); // defaults: 24h cutoff, NoRefund late policy

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy: null);

        Assert.Equal(RefundStatus.Pending, booking.RefundStatus);
        var refundEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundDueDomainEvent>());
        Assert.Equal(500m, refundEvent.RefundAmount);
    }

    [Fact]
    public void Cancel_PastCutoff_NoRefundPolicy_DoesNotRaiseRefund()
    {
        var soon = DateTime.UtcNow.AddHours(2); // inside the default 24h cutoff
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(soon), TimeOnly.FromDateTime(soon), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId); // defaults to LateCancellationPolicy.NoRefund

        booking.Cancel(Guid.NewGuid(), "Late cancel", bookingPolicy, paymentPolicy: null);

        Assert.Equal(RefundStatus.None, booking.RefundStatus);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundDueDomainEvent>());
    }

    [Fact]
    public void Cancel_PastCutoff_PartialRefundPolicy_RaisesPercentageAmount()
    {
        var soon = DateTime.UtcNow.AddHours(2);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(soon), TimeOnly.FromDateTime(soon), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        bookingPolicy.UpdateSettings(lateCancellationPolicy: CancellationPolicy.PartialRefund);
        var paymentPolicy = new PaymentPolicy(booking.TenantId);
        paymentPolicy.UpdatePolicy(PaymentRequirementType.DepositRequired, DepositType.Percentage, 50m, refundPercent: 50m);

        booking.Cancel(Guid.NewGuid(), "Late cancel", bookingPolicy, paymentPolicy);

        var refundEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundDueDomainEvent>());
        Assert.Equal(250m, refundEvent.RefundAmount);
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

        booking.CancelByCustomer("Change of plans", bookingPolicy, paymentPolicy: null);

        Assert.Equal(RefundStatus.None, booking.RefundStatus);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundDueDomainEvent>());
    }
}
```

(Enum member names above — `PaymentRequirementType.DepositRequired`, `DepositType.Percentage` — are confirmed against [PaymentRequirementType.cs](../../../ApexBooking.Core.Domain/Enums/PaymentRequirementType.cs) and [DepositType.cs](../../../ApexBooking.Core.Domain/Enums/DepositType.cs).)

---

### Task 3: `Tenant` — pass `BookingPolicy`/`PaymentPolicy` down through both cancel entry points

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Tenant.cs`

**Interfaces:**
- Consumes: `Booking.Cancel(Guid, string, BookingPolicy?, PaymentPolicy?)`, `Booking.CancelByCustomer(string, BookingPolicy?, PaymentPolicy?)` (Task 2).
- Produces: `Tenant.CancelBooking`/`Tenant.CancelBookingByCustomer` — signatures unchanged (both already take only booking-identifying params; the policies come from `this.BookingPolicy`/`this.PaymentPolicy`, already properties on `Tenant`).

- [ ] **Step 1: Update `CancelBooking`**

Replace:

```csharp
        public void CancelBooking(Guid bookingId, Guid executionUserId, string reason)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            booking.Cancel(executionUserId, reason);
            this.UpdatedAt = DateTime.UtcNow;
        }
```

With:

```csharp
        public void CancelBooking(Guid bookingId, Guid executionUserId, string reason)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            booking.Cancel(executionUserId, reason, BookingPolicy, PaymentPolicy);
            this.UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 2: Update `CancelBookingByCustomer`**

Replace:

```csharp
            booking.CancelByCustomer(reason);
            this.UpdatedAt = DateTime.UtcNow;
        }
```

(the one inside `CancelBookingByCustomer`, directly below the cutoff-hours guard) With:

```csharp
            booking.CancelByCustomer(reason, BookingPolicy, PaymentPolicy);
            this.UpdatedAt = DateTime.UtcNow;
        }
```

---

### Task 4: Persist the new `Booking` columns

**Files:**
- Modify: `ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs`

**Interfaces:**
- Consumes: `Booking.PayMongoPaymentId`, `Booking.RefundStatus`, `Booking.RefundedAmount`, `Booking.RefundedAt` (Task 2).

- [ ] **Step 1: Add the column mappings**

Insert directly after the existing `PaymentConfirmedVia` mapping (after the block ending `.HasMaxLength(20);` around line 100):

```csharp
            builder.Property(b => b.PaymentConfirmedVia)
                .HasConversion<string>()
                .HasColumnName("payment_confirmed_via")
                .HasMaxLength(20);

            builder.Property(b => b.PayMongoPaymentId)
                .HasColumnName("paymongo_payment_id")
                .HasMaxLength(100);

            builder.Property(b => b.RefundStatus)
                .HasConversion<string>()
                .HasColumnName("refund_status")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(b => b.RefundedAmount)
                .HasColumnName("refunded_amount")
                .HasPrecision(12, 2);

            builder.Property(b => b.RefundedAt)
                .HasColumnName("refunded_at");
```

- [ ] **Step 2: Note for the user (do not execute)**

A migration is needed: `dotnet ef migrations add AddBookingRefundTracking -p ApexBooking.Core.Persistence -s ApexBooking.WebApi`, then `dotnet ef database update` — left to the user per standing instruction (file-lock risk from their running debug session).

---

### Task 5: PayMongo webhook contracts — capture the Payment ID + add refund request/response DTOs

**Files:**
- Modify: `ApexBooking.Core.Domain/Services/Paymongo/PaymongoContracts.cs`

**Interfaces:**
- Produces:
  - `WebhookResource.Id` (new property)
  - `PayMongoRefundRequest` / `RefundData` / `RefundAttributes` (request DTOs, mirroring the existing `PayMongoLinkRequest`/`LinkData`/`LinkAttributes` shape)
  - `PayMongoRefundResponse` / `RefundResponseData` / `RefundResponseAttributes` (response DTOs, mirroring `PayMongoLinkResponse`/`LinkResponseData`/`LinkResponseAttributes`)

- [ ] **Step 1: Add `Id` to `WebhookResource`**

Replace:

```csharp
    public class WebhookResource
    {
        [JsonPropertyName("attributes")]
        public ResourceAttributes Attributes { get; set; } = new();
    }
```

With:

```csharp
    public class WebhookResource
    {
        // Confirmed by corroborating evidence, not an authoritative PayMongo doc page for this
        // exact event (see the refund design spec's Context section) — this is the PayMongo
        // Payment resource's own ID (e.g. "pay_..."), captured so a later refund call can address
        // it. Worth a real sandbox webhook capture to double-check before depending on it in
        // production billing flows.
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public ResourceAttributes Attributes { get; set; } = new();
    }
```

- [ ] **Step 2: Add the refund request/response DTOs**

Append to the end of the file, before the closing namespace `}`:

```csharp
    public class PayMongoRefundRequest
    {
        [JsonPropertyName("data")]
        public RefundData Data { get; set; } = new();
    }

    public class RefundData
    {
        [JsonPropertyName("attributes")]
        public RefundAttributes Attributes { get; set; } = new();
    }

    public class RefundAttributes
    {
        [JsonPropertyName("amount")]
        public long AmountInCentavos { get; set; }

        [JsonPropertyName("payment_id")]
        public string PaymentId { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty; // one of: duplicate, fraudulent, requested_by_customer, others

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class PayMongoRefundResponse
    {
        [JsonPropertyName("data")]
        public RefundResponseData Data { get; set; } = new();
    }

    public class RefundResponseData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public RefundResponseAttributes Attributes { get; set; } = new();
    }

    public class RefundResponseAttributes
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // pending | processing | succeeded | failed
    }
```

---

### Task 6: `IPayMongoService.CreateRefundAsync` + `PayMongoService` implementation

**Files:**
- Modify: `ApexBooking.Core.Domain/Services/Paymongo/IPayMongoService.cs`
- Create: `ApexBooking.Core.Domain/Services/Paymongo/PayMongoRefundResult.cs`
- Modify: `ApexBooking.Infrastructure/ExternalServices/PayMongo/PayMongoService.cs`

**Interfaces:**
- Consumes: `PayMongoRefundRequest`/`PayMongoRefundResponse` (Task 5).
- Produces: `Task<PayMongoRefundResult> CreateRefundAsync(string tenantSecretKey, string payMongoPaymentId, decimal amountPhp, string reason, string? notes, CancellationToken cancellationToken)`, `record PayMongoRefundResult(string RefundId, RefundStatus Status)`.

- [ ] **Step 1: Add `PayMongoRefundResult`**

Match the existing `PayMongoSourceResult.cs` file's shape exactly (same directory):

```csharp
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Domain.Services.Paymongo
{
    public record PayMongoRefundResult(
        string RefundId,
        RefundStatus Status
    );
}
```

- [ ] **Step 2: Add the method to `IPayMongoService`**

Replace:

```csharp
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
```

With:

```csharp
    public interface IPayMongoService
{
    Task<PayMongoSourceResult> CreatePaymentSourceAsync(
            string tenantSecretKey,
            Guid bookingId,
            Guid branchId,
            decimal amountPhp,
            string description,
            CancellationToken cancellationToken);

    Task<PayMongoRefundResult> CreateRefundAsync(
            string tenantSecretKey,
            string payMongoPaymentId,
            decimal amountPhp,
            string reason,
            CancellationToken cancellationToken);
    }
```

- [ ] **Step 3: Implement `CreateRefundAsync` in `PayMongoService`**

Add this method to the `PayMongoService` class, after `CreatePaymentSourceAsync`:

```csharp
    public async Task<PayMongoRefundResult> CreateRefundAsync(
        string tenantSecretKey,
        string payMongoPaymentId,
        decimal amountPhp,
        string reason,
        CancellationToken cancellationToken)
    {
        long amountInCentavos = (long)Math.Round(amountPhp * 100, 0);

        var payMongoRequest = new PayMongoRefundRequest();
        payMongoRequest.Data.Attributes.AmountInCentavos = amountInCentavos;
        payMongoRequest.Data.Attributes.PaymentId = payMongoPaymentId;
        payMongoRequest.Data.Attributes.Reason = reason;

        var authBytes = Encoding.ASCII.GetBytes($"{tenantSecretKey.Trim()}:");
        var base64Auth = Convert.ToBase64String(authBytes);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/refunds")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payMongoRequest),
                Encoding.UTF8,
                "application/json"
            )
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

        var httpResponse = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"PayMongo refund failure. Status: {httpResponse.StatusCode}. Error context: {errorContent}");
        }

        var responseStream = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var payMongoResult = JsonSerializer.Deserialize<PayMongoRefundResponse>(responseStream);

        if (payMongoResult?.Data?.Attributes == null || string.IsNullOrWhiteSpace(payMongoResult.Data.Id))
            throw new Exception("Invalid structural metadata returned from PayMongo refund API gateway server.");

        var status = payMongoResult.Data.Attributes.Status.ToLowerInvariant() switch
        {
            "pending" => RefundStatus.Pending,
            "processing" => RefundStatus.Processing,
            "succeeded" => RefundStatus.Succeeded,
            "failed" => RefundStatus.Failed,
            _ => RefundStatus.Pending,
        };

        return new PayMongoRefundResult(
            RefundId: payMongoResult.Data.Id,
            Status: status
        );
    }
```

Add `using ApexBooking.Core.Domain.Enums;` to the top of `PayMongoService.cs` for `RefundStatus` (the file currently only has `using ApexBooking.Core.Domain.Services.Paymongo;`).

---

### Task 7: Capture the PayMongo Payment ID at webhook-confirmation time

**Files:**
- Modify: `ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommand.cs`
- Modify: `ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommandHandler.cs`
- Modify: `ApexBooking.WebApi/Controllers/PayMongoWebhooksController.cs`

**Interfaces:**
- Consumes: `WebhookResource.Id` (Task 5), `Booking.ConfirmPayment(PaymentConfirmationMethod, string?)` (Task 2).
- Produces: `ProcessPaymentWebhookCommand(string RemarksToken, string? PayMongoPaymentId, string RawBody, string? SignatureHeader)` — signature change.

- [ ] **Step 1: Add the field to the command**

Replace:

```csharp
    public record ProcessPaymentWebhookCommand(string RemarksToken, string RawBody, string? SignatureHeader) : ICommand;
```

With:

```csharp
    public record ProcessPaymentWebhookCommand(string RemarksToken, string? PayMongoPaymentId, string RawBody, string? SignatureHeader) : ICommand;
```

- [ ] **Step 2: Pass it through in the handler**

Replace:

```csharp
            // 5. Invoke the updated Domain State machine method!
            // This switches the status from PendingPayment to Scheduled and automatically raises your BookingScheduledDomainEvent!
            booking.ConfirmPayment(PaymentConfirmationMethod.Online);
```

With:

```csharp
            // 5. Invoke the updated Domain State machine method!
            // This switches the status from PendingPayment to Scheduled and automatically raises your BookingScheduledDomainEvent!
            booking.ConfirmPayment(PaymentConfirmationMethod.Online, command.PayMongoPaymentId);
```

- [ ] **Step 3: Extract the ID in the controller and pass it to the command**

Replace:

```csharp
                // 3. Verify that the payment transaction status registers as fully "paid" before moving forward
                if (resourceAttributes.Status.Equals("paid", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Raw body + signature header travel down to the handler, which verifies them
                    // against the tenant's own webhook secret before trusting anything above.
                    var signatureHeader = Request.Headers["Paymongo-Signature"].FirstOrDefault();
                    await _mediator.Send(new ProcessPaymentWebhookCommand(resourceAttributes.Remarks, jsonText, signatureHeader));
                }
```

With:

```csharp
                // 3. Verify that the payment transaction status registers as fully "paid" before moving forward
                if (resourceAttributes.Status.Equals("paid", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Raw body + signature header travel down to the handler, which verifies them
                    // against the tenant's own webhook secret before trusting anything above.
                    var signatureHeader = Request.Headers["Paymongo-Signature"].FirstOrDefault();
                    var payMongoPaymentId = payload.Data.Attributes.Data.Id;
                    await _mediator.Send(new ProcessPaymentWebhookCommand(resourceAttributes.Remarks, payMongoPaymentId, jsonText, signatureHeader));
                }
```

---

### Task 8: Include `BookingPolicy`/`PaymentPolicy` when loading the tenant for both cancel paths

**Files:**
- Modify: `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBooking/CancelBookingHandler.cs`
- Modify: `ApexBooking.Core.Application/Features/PublicBookings/Commands/CancelBookingByToken/CancelBookingByTokenHandler.cs`

**Interfaces:**
- Consumes: `Tenant.CancelBooking`/`Tenant.CancelBookingByCustomer` now read `this.BookingPolicy`/`this.PaymentPolicy` (Task 3) — both navigation properties must be loaded or the refund calculation silently sees `null` and falls back to `CancellationPolicy.NoRefund`/`0%`, wrongly skipping refunds that should apply.

- [ ] **Step 1: `CancelBookingHandler.cs`**

Replace:

```csharp
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Bookings);
```

With:

```csharp
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Bookings, t => t.BookingPolicy!, t => t.PaymentPolicy!]);
```

- [ ] **Step 2: `CancelBookingByTokenHandler.cs`**

Replace:

```csharp
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == payload.TenantId,
                includes: [t => t.Bookings, t => t.BookingPolicy!]);
```

With:

```csharp
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == payload.TenantId,
                includes: [t => t.Bookings, t => t.BookingPolicy!, t => t.PaymentPolicy!]);
```

---

### Task 9: `ProcessRefundOnBookingCancelledHandler` — the actual refund call

**Files:**
- Create: `ApexBooking.Core.Application/Features/Bookings/Events/ProcessRefundOnBookingCancelledHandler.cs`

**Interfaces:**
- Consumes: `BookingRefundDueDomainEvent` (Task 1), `IPayMongoService.CreateRefundAsync` (Task 6), `Booking.RecordRefundOutcome` (Task 2, `public`), `IUnitOfWork.TenantRepository.GetAsync`/`.Update`/`IUnitOfWork.CompleteAsync` (existing).
- No new interfaces produced — this is a leaf notification handler, MediatR assembly-scans and wires it automatically (see `ApplicationServices.cs`'s `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly))` — no manual DI registration needed).

- [ ] **Step 1: Write the handler**

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Paymongo;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    // Subscribes to BookingRefundDueDomainEvent — a dedicated IReliableDomainEvent, so this runs
    // via the outbox relay with at-least-once delivery (5 retries), not synchronously in-request.
    // A failed PayMongo call here never rolls back the booking's own cancellation, which already
    // committed in a separate, earlier transaction.
    public class ProcessRefundOnBookingCancelledHandler
        : INotificationHandler<DomainEventNotification<BookingRefundDueDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayMongoService _payMongoService;
        private readonly ILogger<ProcessRefundOnBookingCancelledHandler> _logger;

        public ProcessRefundOnBookingCancelledHandler(
            IUnitOfWork unitOfWork,
            IPayMongoService payMongoService,
            ILogger<ProcessRefundOnBookingCancelledHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _payMongoService = payMongoService;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingRefundDueDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.Bookings, t => t.PaymentCredential!]);

            if (tenant?.PaymentCredential?.SecretKey is not { } secretKey)
            {
                _logger.LogError(
                    "Could not resolve PayMongo credentials for Tenant {TenantId}. Refund for {BookingReference} was skipped.",
                    e.TenantId, e.BookingReference);
                return;
            }

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == e.BookingId);
            if (booking?.PayMongoPaymentId is not { } payMongoPaymentId)
            {
                _logger.LogWarning(
                    "Booking {BookingReference} has no PayMongo payment ID on file (likely confirmed before refund tracking shipped). Refund was skipped.",
                    e.BookingReference);
                return;
            }

            try
            {
                var result = await _payMongoService.CreateRefundAsync(
                    tenantSecretKey: secretKey,
                    payMongoPaymentId: payMongoPaymentId,
                    amountPhp: e.RefundAmount,
                    reason: "requested_by_customer",
                    cancellationToken: cancellationToken);

                booking.RecordRefundOutcome(result.Status, e.RefundAmount);

                _logger.LogInformation(
                    "PayMongo refund {RefundId} for Booking {BookingReference} resolved with status {Status}.",
                    result.RefundId, e.BookingReference, result.Status);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(
                    ex,
                    "PayMongo refund call failed for Booking {BookingReference}. Cancellation itself already committed and stays in effect.",
                    e.BookingReference);
                booking.RecordRefundOutcome(RefundStatus.Failed, null);
            }

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```

Note: `IPayMongoService.CreateRefundAsync`'s signature from Task 6 doesn't include a `notes` parameter (dropped as unnecessary — YAGNI, nothing in this codebase currently generates a human note beyond the cancellation reason already stored on the booking itself). If Task 6 was implemented with a `notes` parameter, pass `null`.

---

## Self-Review Notes

- **Spec coverage:** every "Backend design" bullet from the spec maps to a task — Payment ID capture (Task 5+7), shared refund calc (Task 2), `RefundStatus`/`RefundedAmount` fields (Task 2/4), `IPayMongoService` refund method (Task 6), event-driven trigger (Task 1/9), non-rollback-on-failure (Task 9's try/catch). The one deviation (dedicated reliable event instead of reusing `BookingCancelledDomainEvent`) is called out explicitly in Global Constraints, with the reasoning.
- **Type consistency check:** `RefundStatus` (Task 1) is referenced identically in Task 2 (`Booking.RefundStatus`), Task 4 (EF mapping), Task 6 (`PayMongoRefundResult.Status`), and Task 9 (`RecordRefundOutcome` call) — same enum, same casing throughout.
- **Ambiguity flagged and resolved:** the refund `reason` string sent to PayMongo wasn't specified in the spec. Resolved in Task 9 as a hardcoded `"requested_by_customer"` (PayMongo's own enum value) rather than left as a TODO — matches the domain reality (every refund here originates from a cancellation, staff-initiated ones included, since staff cancelling is still fundamentally "the customer no longer needs this booking").
- **Placeholder scan:** no TBD/TODO left in the plan — the one open item flagged during drafting (`PaymentRequirementType`/`DepositType` enum member names in Task 2's test file) was verified against the actual enum files and corrected in place before finalizing.
