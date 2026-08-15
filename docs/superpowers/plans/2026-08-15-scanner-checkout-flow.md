# Scanner Check-In / Checkout Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a checkout step to the scanner flow (customer detail + remaining balance + confirm), fix the underlying payment-tracking gap that "remaining balance" requires, and remove redundant Complete/Collect-Payment/No-show controls from Staff's view.

**Architecture:** One scanner, two moments — `ScanArrivalResult.WasFirstAdmission` (already returned today) distinguishes a fresh admit from a checkout scan. `Booking` gets two new fields (`ServicePriceAtBooking`, `InVisitAmountCollected`) and a unified `CaptureRemainingInVisitPayment()` capture path that every payment-settling method now funnels through, replacing three previously-divergent (and one previously-impossible) code paths.

**Tech Stack:** ASP.NET Core 10 / EF Core 10 / MediatR / xUnit (backend), React + TypeScript + axios (frontend).

**Spec:** `docs/superpowers/specs/2026-08-15-scanner-checkout-flow-design.md`

## Global Constraints

- `Booking.ServicePriceAtBooking`: nullable decimal, `HasPrecision(12,2)`, snapshotted from `service.Price` inside `Tenant.PlaceBooking` for every new booking (not just deposit-policy ones)
- `Booking.InVisitAmountCollected`: non-nullable decimal, default `0m`, `HasPrecision(12,2)`, `HasDefaultValue(0m)` at the DB level so existing rows satisfy the NOT NULL constraint without a manual backfill
- `RemainingBalance` formula (implemented as `ComputeRemainingBalance()`, exposed publicly as `Booking.RemainingBalance`):
  ```
  AmountPaidOnline = PaymentConfirmedVia == Online ? AmountDue : 0
  RemainingBalance = ServicePriceAtBooking.HasValue
      ? max(0, ServicePriceAtBooking - AmountPaidOnline - InVisitAmountCollected)
      : (PaymentConfirmedVia is null ? AmountDue : 0)   // pre-migration fallback
  ```
- `PaymentConfirmedVia` is **never overwritten** once set — `??=` only, preserving refund-eligibility logic elsewhere that keys off `PaymentConfirmedVia == Online`
- `CaptureRemainingInVisitPayment()` is a no-op (not an error) when nothing remains — `CompleteService()` calls it unconditionally, matching today's behavior exactly for already-fully-paid bookings
- The new `CheckedInAt` requirement for checkout lives **only** in the new `Tenant.CheckOutBooking` path — `Tenant.CompleteBooking` (existing manual Complete button, Owner/Admin fallback) is untouched
- Revenue query's `InVisitAmountCollected` sum is scoped to `PaymentConfirmedVia == Online` rows only, to avoid double-counting pure pay-at-counter bookings (see spec's "Revenue query fix" section for why)
- New endpoints (`checkout-detail`, `checkout`) are `[Authorize(Roles = "Owner,Admin,Staff")]`, matching `scan-arrival`/`admit`
- Staff-only button removal (Complete, No-show — **not** Cancel, which stays visible for everyone) is frontend-only; the backend policy is already Owner/Admin-only today
- EF Core migration is generated, never hand-authored, and never applied by the executor — left to the user (matches every prior plan in this repo)
- No handler/controller test project exists in this repo — automated tests are scoped to the new `Booking` domain methods only (`ApexBooking.Core.Domain.UnitTests`); everything above that layer is verified by `dotnet build` and manual end-to-end checks

---

### Task 1: `Booking` domain changes — payment capture unification

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Booking.cs`
- Test: `ApexBooking.Core.Domain.UnitTests/Entities/BookingPaymentCaptureTests.cs`

**Interfaces:**
- Produces: `Booking.ServicePriceAtBooking` (`decimal?`, public get/private set), `Booking.InVisitAmountCollected` (`decimal`, public get/private set, default `0m`), `Booking.RemainingBalance` (`decimal`, public computed property), `Booking.Create(..., decimal? servicePriceAtBooking = null)` (new optional trailing parameter), `internal void ClearPendingPaymentOnArrival()` — consumed by Task 2 (`Tenant.cs`)
- Modifies existing behavior of `RecordPayInVisitPayment()` and `CompleteService()` (signatures unchanged, internal logic changed) — consumed by every existing caller unchanged

- [ ] **Step 1: Write the failing tests**

Create `ApexBooking.Core.Domain.UnitTests/Entities/BookingPaymentCaptureTests.cs`:

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

public class BookingPaymentCaptureTests
{
    private static Booking CreateBooking(bool requiresUpfrontPayment, decimal amountDue, decimal? servicePriceAtBooking)
    {
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-01",
            scheduledDate: DateOnly.FromDateTime(DateTime.UtcNow),
            scheduledStartTime: TimeOnly.FromDateTime(DateTime.UtcNow),
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: requiresUpfrontPayment,
            currencyCode: "PHP",
            amountDue: amountDue,
            servicePriceAtBooking: servicePriceAtBooking);

        booking.ClearDomainEvents();
        return booking;
    }

    [Fact]
    public void RemainingBalance_FullPaymentPaidOnline_IsZero()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 500m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");

        Assert.Equal(0m, booking.RemainingBalance);
    }

    [Fact]
    public void RemainingBalance_DepositPaidOnline_ReflectsTrueGap()
    {
        // A 100 deposit against a 500 service — AmountDue only ever held the deposit.
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 100m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");

        Assert.Equal(400m, booking.RemainingBalance);
    }

    [Fact]
    public void RecordPayInVisitPayment_DepositThenRemainder_CapturesRemainderAndKeepsOnlineFlag()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 100m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");
        booking.ClearDomainEvents();

        booking.RecordPayInVisitPayment();

        Assert.Equal(0m, booking.RemainingBalance);
        Assert.Equal(400m, booking.InVisitAmountCollected);
        Assert.Equal(PaymentConfirmationMethod.Online, booking.PaymentConfirmedVia); // never overwritten
        var captured = Assert.Single(booking.DomainEvents.OfType<PaymentCapturedDomainEvent>());
        Assert.Equal(400m, captured.AmountDue); // amount just captured, not the stale deposit snapshot
    }

    [Fact]
    public void RecordPayInVisitPayment_NothingRemaining_Throws()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 500m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");

        Assert.Throws<BusinessRuleBrokenException>(() => booking.RecordPayInVisitPayment());
    }

    [Fact]
    public void CompleteService_PayAtCounter_CapturesFullPriceAndSetsPayInVisit()
    {
        var booking = CreateBooking(requiresUpfrontPayment: false, amountDue: 300m, servicePriceAtBooking: 300m);

        booking.CompleteService();

        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(PaymentConfirmationMethod.PayInVisit, booking.PaymentConfirmedVia);
        Assert.Equal(300m, booking.InVisitAmountCollected);
        Assert.Equal(0m, booking.RemainingBalance);
        var captured = Assert.Single(booking.DomainEvents.OfType<PaymentCapturedDomainEvent>());
        Assert.Equal(300m, captured.AmountDue);
    }

    [Fact]
    public void CompleteService_AlreadyFullyPaidOnline_DoesNotRaiseAnotherPaymentEvent()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 500m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");
        booking.ClearDomainEvents();

        booking.CompleteService();

        Assert.Empty(booking.DomainEvents.OfType<PaymentCapturedDomainEvent>());
    }

    [Fact]
    public void RemainingBalance_PreMigrationBookingWithoutServicePrice_FallsBackToAmountDue()
    {
        var booking = CreateBooking(requiresUpfrontPayment: false, amountDue: 250m, servicePriceAtBooking: null);

        Assert.Equal(250m, booking.RemainingBalance);
    }

    [Fact]
    public void RemainingBalance_PreMigrationBookingAlreadyConfirmed_FallsBackToZero()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 250m, servicePriceAtBooking: null);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");

        Assert.Equal(0m, booking.RemainingBalance);
    }

    [Fact]
    public void ClearPendingPaymentOnArrival_FromPendingPayment_MovesToScheduledWithoutConfirmingPayment()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 100m, servicePriceAtBooking: 500m);

        booking.ClearPendingPaymentOnArrival();

        Assert.Equal(BookingStatus.Scheduled, booking.Status);
        Assert.Null(booking.PaymentConfirmedVia);
        Assert.Equal(500m, booking.RemainingBalance); // nothing paid online, nothing collected — full price still owed
    }

    [Fact]
    public void ClearPendingPaymentOnArrival_NotPendingPayment_Throws()
    {
        var booking = CreateBooking(requiresUpfrontPayment: false, amountDue: 300m, servicePriceAtBooking: 300m);

        Assert.Throws<BusinessRuleBrokenException>(() => booking.ClearPendingPaymentOnArrival());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter BookingPaymentCaptureTests`
Expected: build errors — `ServicePriceAtBooking`, `InVisitAmountCollected`, `RemainingBalance`, `ClearPendingPaymentOnArrival`, and the `servicePriceAtBooking` parameter don't exist yet.

- [ ] **Step 3: Add the two new fields and the `RemainingBalance` property**

In `Booking.cs`, add after `public DateTime? CheckedInAt { get; private set; }` (line 62):

```csharp
        // Full service price at booking time — always snapshotted (not just for deposit
        // policies), so RemainingBalance can be computed correctly regardless of payment policy.
        // Nullable because bookings created before this shipped won't have it.
        public decimal? ServicePriceAtBooking { get; private set; }

        // Cash collected in person, tracked separately and additively to the online-charged
        // AmountDue/PaymentConfirmedVia==Online — this is what makes a deposit-then-remainder
        // booking representable at all (see CaptureRemainingInVisitPayment).
        public decimal InVisitAmountCollected { get; private set; } = 0m;
```

Add after `TotalBlockedEndTime` (line 31):

```csharp
        public decimal RemainingBalance => ComputeRemainingBalance();
```

- [ ] **Step 4: Extend `Create()` with the new optional parameter**

Change the signature (line 76-90):

```csharp
        public static Booking Create(
            TenantId tenantId,
            BranchId branchId,
            CustomerId customerId,
            TenantMemberId staffId,
            ServiceId serviceId,
            string bookingReference,
            DateOnly scheduledDate,
            TimeOnly scheduledStartTime,
            int durationMinutes,
            int bufferAfterMinutes,
            string? customerNotes,
            bool requiresUpfrontPayment, // 🌟 Added parameter flag to route events safely
            string currencyCode,
            decimal amountDue = 0m,
            decimal? servicePriceAtBooking = null)
```

and add `ServicePriceAtBooking = servicePriceAtBooking,` to the object initializer (after `AmountDue = amountDue,` at line 119):

```csharp
                AmountDue = amountDue,
                ServicePriceAtBooking = servicePriceAtBooking,
```

- [ ] **Step 5: Add `ComputeRemainingBalance()` and `CaptureRemainingInVisitPayment()` private helpers**

Add right before `public void CompleteService()` (line 238):

```csharp
        private decimal ComputeRemainingBalance()
        {
            if (ServicePriceAtBooking is null)
                return PaymentConfirmedVia is null ? AmountDue : 0m;

            var amountPaidOnline = PaymentConfirmedVia == PaymentConfirmationMethod.Online ? AmountDue : 0m;
            return Math.Max(0m, ServicePriceAtBooking.Value - amountPaidOnline - InVisitAmountCollected);
        }

        // Shared by RecordPayInVisitPayment and CompleteService's auto-settle — the single place
        // any "what's still owed gets captured now" event fires, so both paths report the
        // accurate just-captured amount, not a stale full-booking snapshot. A no-op (not an
        // error) when nothing remains — CompleteService calls this unconditionally and relies on
        // that no-op behavior for already-fully-paid bookings.
        private void CaptureRemainingInVisitPayment()
        {
            var remaining = ComputeRemainingBalance();
            if (remaining <= 0m) return;

            InVisitAmountCollected += remaining;
            PaymentConfirmedVia ??= PaymentConfirmationMethod.PayInVisit; // never overwrites an existing Online

            AddDomainEvent(new PaymentCapturedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                AmountDue: remaining,
                CurrencyCode: this.CurrencyCode,
                Method: PaymentConfirmationMethod.PayInVisit,
                CapturedAt: UpdatedAt
            ));
        }
```

- [ ] **Step 6: Rewrite `CompleteService()`'s auto-settle block to use the shared helper**

Replace (lines 238-275):

```csharp
        public void CompleteService()
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Only active, scheduled appointments can be marked as completed.");

            Status = BookingStatus.Completed;
            ServiceCompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            // Nothing collected this booking's payment earlier (no online webhook, no
            // arrival-scan cash collection) — that only happens for pay-in-visit bookings
            // (walk-ins, no-upfront-payment public bookings), so the visit's completion IS
            // the payment-collection moment.
            if (PaymentConfirmedVia is null)
            {
                PaymentConfirmedVia = PaymentConfirmationMethod.PayInVisit;

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

            AddDomainEvent(new BookingCompletedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CustomerId: this.CustomerId.Value,
                StaffId: this.StaffId.Value,
                ServiceId: this.ServiceId.Value,
                CompletedAt: ServiceCompletedAt.Value
            ));
        }
```

with:

```csharp
        public void CompleteService()
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Only active, scheduled appointments can be marked as completed.");

            Status = BookingStatus.Completed;
            ServiceCompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            // Whatever remains unpaid at completion is collected right here — the visit's
            // completion IS the payment-collection moment for anything not already settled
            // online (pay-at-counter bookings, and any deposit-required booking's remainder).
            // A no-op when nothing remains.
            CaptureRemainingInVisitPayment();

            AddDomainEvent(new BookingCompletedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CustomerId: this.CustomerId.Value,
                StaffId: this.StaffId.Value,
                ServiceId: this.ServiceId.Value,
                CompletedAt: ServiceCompletedAt.Value
            ));
        }
```

- [ ] **Step 7: Rewrite `RecordPayInVisitPayment()` to use the new remaining-balance guard**

Replace (lines 300-320):

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

with:

```csharp
        public void RecordPayInVisitPayment()
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Payment can only be recorded for a currently scheduled appointment.");

            if (ComputeRemainingBalance() <= 0m)
                throw new BusinessRuleBrokenException("Payment has already been recorded for this appointment.");

            UpdatedAt = DateTime.UtcNow;
            CaptureRemainingInVisitPayment();
        }
```

- [ ] **Step 8: Add `ClearPendingPaymentOnArrival()`**

Add right after `RecordArrival()` (after line 234, before the `// ── Minimalist Lean Administration Domain State Machines ──` comment):

```csharp
        // Replaces the old "auto-confirm as PayInVisit at admit time" behavior — that claimed
        // payment was captured before checkout even existed, at a snapshot amount that's wrong
        // for deposit bookings. This just clears the way for check-in (PendingPayment requires
        // Scheduled status) without asserting anything about payment. Scheduled +
        // PaymentConfirmedVia == null is already the normal starting state for every
        // no-upfront-payment booking — this just reaches it via one more path. The real capture
        // now happens for real at checkout, through CaptureRemainingInVisitPayment.
        internal void ClearPendingPaymentOnArrival()
        {
            if (Status != BookingStatus.PendingPayment)
                throw new BusinessRuleBrokenException("Only bookings pending payment can be cleared for arrival.");

            Status = BookingStatus.Scheduled;
            UpdatedAt = DateTime.UtcNow;
        }
```

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter BookingPaymentCaptureTests`
Expected: 9 tests pass.

- [ ] **Step 10: Run the full existing test suite to confirm nothing broke**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests`
Expected: all tests pass, including the existing `BookingRefundTests` (which call `ConfirmPayment` and `Cancel` — unaffected by this task's changes).

- [ ] **Step 11: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/Booking.cs ApexBooking.Core.Domain.UnitTests/Entities/BookingPaymentCaptureTests.cs
git commit -m "feat: unify in-visit payment capture and fix deposit-remainder tracking"
```

---

### Task 2: `Tenant` orchestration — snapshot service price, fix arrival, add checkout

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Tenant.cs`

**Interfaces:**
- Consumes: `Booking.Create(..., servicePriceAtBooking:)`, `Booking.ClearPendingPaymentOnArrival()`, `Booking.CompleteService()` (Task 1)
- Produces: `Tenant.CheckOutBooking(Guid bookingId, BranchId scannerBranchId) : Booking` — consumed by Task 8 (`CheckoutBookingHandler`)

- [ ] **Step 1: Snapshot `service.Price` into every new booking**

In `PlaceBooking` (private method), change:

```csharp
            var booking = Booking.Create(
                tenantId: this.TenantId,
                branchId: branchId,
                customerId: customerId,
                staffId: staffId,
                serviceId: serviceId,
                bookingReference: bookingReference,
                scheduledDate: date,
                scheduledStartTime: startTime,
                durationMinutes: service.DurationMinutes, // Snapshotted history tracking protection
                bufferAfterMinutes: service.BufferAfterMinutes, // Snapshotted history tracking protection
                customerNotes: customerNotes,
                requiresUpfrontPayment: requiresUpfrontPayment,
                currencyCode: service.CurrencyCode,
                amountDue: finalAmountDue
            );
```

to:

```csharp
            var booking = Booking.Create(
                tenantId: this.TenantId,
                branchId: branchId,
                customerId: customerId,
                staffId: staffId,
                serviceId: serviceId,
                bookingReference: bookingReference,
                scheduledDate: date,
                scheduledStartTime: startTime,
                durationMinutes: service.DurationMinutes, // Snapshotted history tracking protection
                bufferAfterMinutes: service.BufferAfterMinutes, // Snapshotted history tracking protection
                customerNotes: customerNotes,
                requiresUpfrontPayment: requiresUpfrontPayment,
                currencyCode: service.CurrencyCode,
                amountDue: finalAmountDue,
                servicePriceAtBooking: service.Price
            );
```

This covers both callers of `PlaceBooking` (the public wizard via `PlaceCustomerBooking`, and walk-ins) — `service` is already resolved once at the top of this method for both paths.

- [ ] **Step 2: Replace the premature payment auto-confirm in `RecordBookingArrival`**

Change:

```csharp
        public (Booking Booking, bool WasFirstAdmission) RecordBookingArrival(BookingId bookingId, BranchId scannerBranchId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            // 🌟 Cross-branch fraud guard: the scanning branch must match the booking's own branch
            if (booking.BranchId != scannerBranchId)
                throw new BusinessRuleBrokenException("This boarding pass belongs to a different branch and cannot be scanned here.");

            if (booking.Status == BookingStatus.PendingPayment)
                booking.ConfirmPayment(PaymentConfirmationMethod.PayInVisit); // unpaid-online arrival pays in visit, cleared right here

            var wasFirstAdmission = booking.RecordArrival();
            this.UpdatedAt = DateTime.UtcNow;

            return (booking, wasFirstAdmission);
        }
```

to:

```csharp
        public (Booking Booking, bool WasFirstAdmission) RecordBookingArrival(BookingId bookingId, BranchId scannerBranchId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            // 🌟 Cross-branch fraud guard: the scanning branch must match the booking's own branch
            if (booking.BranchId != scannerBranchId)
                throw new BusinessRuleBrokenException("This boarding pass belongs to a different branch and cannot be scanned here.");

            // Clears the way for check-in without asserting payment was captured — that happens
            // for real at checkout now (see Booking.ClearPendingPaymentOnArrival).
            if (booking.Status == BookingStatus.PendingPayment)
                booking.ClearPendingPaymentOnArrival();

            var wasFirstAdmission = booking.RecordArrival();
            this.UpdatedAt = DateTime.UtcNow;

            return (booking, wasFirstAdmission);
        }
```

- [ ] **Step 3: Add `CheckOutBooking`**

Add right after `CompleteBooking` (the existing manual-complete method — search for `public void CompleteBooking(Guid bookingId)`):

```csharp
        // The new checkout-scan entry point — distinct from CompleteBooking (the existing manual
        // Complete button/command, which stays untouched and unguarded as an Owner/Admin
        // fallback for e.g. a lost QR code). This one requires the booking to have actually been
        // checked in first, and gives state-specific errors for the other terminal statuses.
        public Booking CheckOutBooking(Guid bookingId, BranchId scannerBranchId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            if (booking.BranchId != scannerBranchId)
                throw new BusinessRuleBrokenException("This boarding pass belongs to a different branch and cannot be scanned here.");

            if (booking.CheckedInAt is null)
                throw new BusinessRuleBrokenException("This booking hasn't been checked in yet.");

            if (booking.Status == BookingStatus.Completed)
                throw new BusinessRuleBrokenException("This booking has already been completed.");

            if (booking.Status == BookingStatus.Cancelled)
                throw new BusinessRuleBrokenException("This booking was cancelled.");

            if (booking.Status == BookingStatus.NoShow)
                throw new BusinessRuleBrokenException("This booking was marked as a no-show.");

            booking.CompleteService();
            this.UpdatedAt = DateTime.UtcNow;

            return booking;
        }
```

- [ ] **Step 4: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Domain/ApexBooking.Core.Domain.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Domain/Entities/Tenant.cs
git commit -m "feat: snapshot service price at booking, fix arrival payment timing, add checkout orchestration"
```

---

### Task 3: EF mapping + migration

**Files:**
- Modify: `ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs`

**Interfaces:**
- Consumes: `Booking.ServicePriceAtBooking`, `Booking.InVisitAmountCollected` (Task 1)
- Produces: `service_price_at_booking`, `in_visit_amount_collected` columns on the `bookings` table

- [ ] **Step 1: Add the column mappings**

In `Configure`, add right after the `AmountDue` mapping (in the "4b. Payment Snapshot" section):

```csharp
            builder.Property(b => b.AmountDue)
                .HasColumnName("amount_due")
                .HasPrecision(12, 2)
                .IsRequired();

            builder.Property(b => b.ServicePriceAtBooking)
                .HasColumnName("service_price_at_booking")
                .HasPrecision(12, 2);

            builder.Property(b => b.InVisitAmountCollected)
                .HasColumnName("in_visit_amount_collected")
                .HasPrecision(12, 2)
                .HasDefaultValue(0m)
                .IsRequired();
```

- [ ] **Step 2: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: `Build succeeded.`

- [ ] **Step 3: Generate the migration**

Run: `dotnet ef migrations add AddBookingPaymentTracking --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`

Open the generated migration file and confirm it contains exactly two `AddColumn` calls (`service_price_at_booking` nullable decimal(12,2), `in_visit_amount_collected` decimal(12,2) NOT NULL with a default constraint of `0`) on the `bookings` table, and the matching `DropColumn`s in `Down()`. Do not run `dotnet ef database update` — leave that to the user.

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs ApexBooking.Core.Persistence/Migrations/
git commit -m "feat: add service_price_at_booking and in_visit_amount_collected columns"
```

---

### Task 4: Repository — checkout detail lookup + revenue fix + bookings-page row extension

**Files:**
- Modify: `ApexBooking.Core.Domain/Repositories/ITenantRepository.cs`
- Modify: `ApexBooking.Core.Persistence/Repositories/TenantRepository.cs`

**Interfaces:**
- Produces: `BookingCheckoutDetailRow` record, `ITenantRepository.GetBookingCheckoutDetailAsync(TenantId, Guid bookingId, CancellationToken) : Task<BookingCheckoutDetailRow?>` — consumed by Task 6 (`GetCheckoutDetailsHandler`)
- Modifies: `TenantBookingRow` gains two raw fields (`ServicePriceAtBooking`, `InVisitAmountCollected`) — consumed by Task 5

- [ ] **Step 1: Add the new row record and interface method**

In `ITenantRepository.cs`, extend `TenantBookingRow` (find `public record TenantBookingRow(`) by adding two fields at the end, right before the closing `);`:

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
    DateTime CreatedAt,
    decimal? ServicePriceAtBooking,
    decimal InVisitAmountCollected
);
```

Add the new row record and interface method right after the `GetRevenueAsync` declaration:

```csharp
    // Powers the scanner's checkout preview panel — same flat customer/staff/service join
    // GetBookingsPageAsync already uses, scoped to a single booking instead of a page.
    Task<BookingCheckoutDetailRow?> GetBookingCheckoutDetailAsync(
        TenantId tenantId,
        Guid bookingId,
        CancellationToken cancellationToken = default);
```

and, near `TenantBookingRow`'s definition:

```csharp
public record BookingCheckoutDetailRow(
    Guid BookingId,
    string BookingReference,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string ServiceName,
    string StaffName,
    Guid BranchId,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    BookingStatus Status,
    DateTime? CheckedInAt,
    decimal AmountDue,
    decimal? ServicePriceAtBooking,
    decimal InVisitAmountCollected,
    PaymentConfirmationMethod? PaymentConfirmedVia,
    string CurrencyCode
);
```

- [ ] **Step 2: Extend `GetBookingsPageAsync`'s projection**

In `TenantRepository.cs`, add the two new fields to the `select new TenantBookingRow(...)` call (at the end, matching the record's new field order):

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
                b.CustomerId.Value,
                b.StaffId.Value,
                b.CreatedAt,
                b.ServicePriceAtBooking,
                b.InVisitAmountCollected);
```

- [ ] **Step 3: Implement `GetBookingCheckoutDetailAsync`**

Add right after `GetBookingsPageAsync`'s closing brace:

```csharp
    public async Task<BookingCheckoutDetailRow?> GetBookingCheckoutDetailAsync(
        TenantId tenantId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var rows =
            from b in context.Bookings.AsNoTracking().Where(b => b.TenantId == tenantId && b.BookingId.Value == bookingId)
            join c in context.Customers.AsNoTracking() on b.CustomerId equals c.CustomerId into customerGroup
            from c in customerGroup.DefaultIfEmpty()
            join m in context.Staffs.AsNoTracking() on b.StaffId equals m.TenantMemberId into staffGroup
            from m in staffGroup.DefaultIfEmpty()
            join s in context.Services.AsNoTracking() on b.ServiceId equals s.ServiceId into serviceGroup
            from s in serviceGroup.DefaultIfEmpty()
            select new BookingCheckoutDetailRow(
                b.BookingId.Value,
                b.BookingReference,
                c != null ? c.Contact.Name : "Unknown",
                c != null ? c.Contact.Email : null,
                c != null ? c.Contact.PhoneNumber : null,
                s != null ? s.Name : "Unknown",
                m != null ? m.FirstName + " " + m.LastName : "Unknown",
                b.BranchId.Value,
                b.ScheduledDate,
                b.ScheduledStartTime,
                b.Status,
                b.CheckedInAt,
                b.AmountDue,
                b.ServicePriceAtBooking,
                b.InVisitAmountCollected,
                b.PaymentConfirmedVia,
                b.CurrencyCode);

        return await rows.FirstOrDefaultAsync(cancellationToken);
    }
```

- [ ] **Step 4: Fix the revenue query**

In `GetRevenueAsync`, change:

```csharp
        var payInVisitAmount = await eligibleBookings
            .Where(b => b.PaymentConfirmedVia == PaymentConfirmationMethod.PayInVisit)
            .SumAsync(b => b.AmountDue - (b.RefundStatus == RefundStatus.Succeeded ? (b.RefundedAmount ?? 0) : 0), cancellationToken);
```

to:

```csharp
        var payInVisitAmount = await eligibleBookings
            .Where(b => b.PaymentConfirmedVia == PaymentConfirmationMethod.PayInVisit)
            .SumAsync(b => b.AmountDue - (b.RefundStatus == RefundStatus.Succeeded ? (b.RefundedAmount ?? 0) : 0), cancellationToken);

        // Deposit-then-remainder bookings never flip PaymentConfirmedVia to PayInVisit (it stays
        // Online, preserving refund-eligibility logic) — this is the only place their in-visit
        // remainder gets counted. Scoped to Online rows only: for a pure pay-at-counter booking,
        // InVisitAmountCollected ends up equal to AmountDue, which the branch above already
        // counts — summing it again here would double it.
        var depositRemainderAmount = await eligibleBookings
            .Where(b => b.PaymentConfirmedVia == PaymentConfirmationMethod.Online)
            .SumAsync(b => b.InVisitAmountCollected, cancellationToken);

        payInVisitAmount += depositRemainderAmount;
```

- [ ] **Step 5: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add ApexBooking.Core.Domain/Repositories/ITenantRepository.cs ApexBooking.Core.Persistence/Repositories/TenantRepository.cs
git commit -m "feat: add checkout-detail repository lookup and fix pay-in-visit revenue undercounting"
```

---

### Task 5: `TenantBookingSummary` — expose corrected payment fields

**Files:**
- Modify: `ApexBooking.Core.Application/Dtos/Response/TenantBookingSummary.cs`
- Modify: `ApexBooking.Core.Application/Features/Bookings/Queries/GetTenantBookings/GetTenantBookingsHandler.cs`

**Interfaces:**
- Consumes: `TenantBookingRow.ServicePriceAtBooking`, `TenantBookingRow.InVisitAmountCollected` (Task 4)
- Produces: `TenantBookingSummary.AmountPaidOnline`, `TenantBookingSummary.RemainingBalance` — consumed by Task 9 (frontend `ITenantBooking`)

- [ ] **Step 1: Extend the DTO**

In `TenantBookingSummary.cs`, add two fields at the end:

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
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
        DateTime CreatedAt,
        decimal AmountPaidOnline,
        decimal RemainingBalance
    );
}
```

- [ ] **Step 2: Compute the new fields in the handler's mapping**

In `GetTenantBookingsHandler.cs`, add `using ApexBooking.Core.Domain.Enums;` to the usings, then change:

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
                row.CustomerId,
                row.StaffId,
                row.CreatedAt));
```

to:

```csharp
            var mappedItems = pagedResult.data.Select(row =>
            {
                var amountPaidOnline = row.PaymentConfirmedVia == PaymentConfirmationMethod.Online ? row.AmountDue : 0m;
                var remainingBalance = row.ServicePriceAtBooking.HasValue
                    ? Math.Max(0m, row.ServicePriceAtBooking.Value - amountPaidOnline - row.InVisitAmountCollected)
                    : (row.PaymentConfirmedVia is null ? row.AmountDue : 0m);

                return new TenantBookingSummary(
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
                    row.CustomerId,
                    row.StaffId,
                    row.CreatedAt,
                    amountPaidOnline,
                    remainingBalance);
            });
```

- [ ] **Step 3: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Application/Dtos/Response/TenantBookingSummary.cs ApexBooking.Core.Application/Features/Bookings/Queries/GetTenantBookings/GetTenantBookingsHandler.cs
git commit -m "feat: expose AmountPaidOnline and RemainingBalance on TenantBookingSummary"
```

---

### Task 6: `GetCheckoutDetailsQuery`

**Files:**
- Create: `ApexBooking.Core.Application/Dtos/Response/CheckoutDetailsDto.cs`
- Create: `ApexBooking.Core.Application/Features/Bookings/Queries/GetCheckoutDetails/GetCheckoutDetailsQuery.cs`
- Create: `ApexBooking.Core.Application/Features/Bookings/Queries/GetCheckoutDetails/GetCheckoutDetailsHandler.cs`

**Interfaces:**
- Consumes: `ITenantRepository.GetBookingCheckoutDetailAsync` (Task 4), `IUnitOfWork.TenantRepository.GetAsync`, `IUserContextService.GetCurrentUserId()`, `ITenantEntity.TenantId` (all existing)
- Produces: `GetCheckoutDetailsQuery(Guid BookingId) : IQuery<CheckoutDetailsDto>` — consumed by Task 8 (`TenantController`)

- [ ] **Step 1: Create the DTO**

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record CheckoutDetailsDto(
        Guid BookingId,
        string BookingReference,
        string CustomerName,
        string? CustomerEmail,
        string? CustomerPhone,
        string ServiceName,
        string StaffName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        decimal AmountPaidOnline,
        decimal RemainingBalance,
        string CurrencyCode
    );
}
```

- [ ] **Step 2: Create the query**

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetCheckoutDetails
{
    public record GetCheckoutDetailsQuery(Guid BookingId) : IQuery<CheckoutDetailsDto>;
}
```

- [ ] **Step 3: Create the handler**

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetCheckoutDetails
{
    public class GetCheckoutDetailsHandler : IQueryHandler<GetCheckoutDetailsQuery, CheckoutDetailsDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public GetCheckoutDetailsHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<CheckoutDetailsDto> Handle(GetCheckoutDetailsQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            var scannerUserId = _userContext.GetCurrentUserId();
            var scanner = tenant?.Members.FirstOrDefault(m => m.UserId == scannerUserId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("Scanner device is not linked to an active staff account.");

            var row = await _unitOfWork.TenantRepository.GetBookingCheckoutDetailAsync(tenantId, query.BookingId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            if (row.BranchId != scanner.BranchId.Value)
                throw new BusinessRuleBrokenException("This boarding pass belongs to a different branch and cannot be scanned here.");

            if (row.CheckedInAt is null)
                throw new BusinessRuleBrokenException("This booking hasn't been checked in yet.");

            if (row.Status == BookingStatus.Completed)
                throw new BusinessRuleBrokenException("This booking has already been completed.");

            if (row.Status == BookingStatus.Cancelled)
                throw new BusinessRuleBrokenException("This booking was cancelled.");

            if (row.Status == BookingStatus.NoShow)
                throw new BusinessRuleBrokenException("This booking was marked as a no-show.");

            var amountPaidOnline = row.PaymentConfirmedVia == PaymentConfirmationMethod.Online ? row.AmountDue : 0m;
            var remainingBalance = row.ServicePriceAtBooking.HasValue
                ? Math.Max(0m, row.ServicePriceAtBooking.Value - amountPaidOnline - row.InVisitAmountCollected)
                : (row.PaymentConfirmedVia is null ? row.AmountDue : 0m);

            return new CheckoutDetailsDto(
                row.BookingId,
                row.BookingReference,
                row.CustomerName,
                row.CustomerEmail,
                row.CustomerPhone,
                row.ServiceName,
                row.StaffName,
                row.ScheduledDate,
                row.ScheduledStartTime,
                amountPaidOnline,
                remainingBalance,
                row.CurrencyCode);
        }
    }
}
```

- [ ] **Step 4: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Application/Dtos/Response/CheckoutDetailsDto.cs ApexBooking.Core.Application/Features/Bookings/Queries/GetCheckoutDetails/
git commit -m "feat: add GetCheckoutDetailsQuery"
```

---

### Task 7: `CheckoutBookingCommand`

**Files:**
- Create: `ApexBooking.Core.Application/Dtos/Response/CheckoutBookingResult.cs`
- Create: `ApexBooking.Core.Application/Features/Bookings/Commands/CheckoutBooking/CheckoutBookingCommand.cs`
- Create: `ApexBooking.Core.Application/Features/Bookings/Commands/CheckoutBooking/CheckoutBookingHandler.cs`

**Interfaces:**
- Consumes: `Tenant.CheckOutBooking`, `Booking.RemainingBalance` (Task 1, 2)
- Produces: `CheckoutBookingCommand(Guid BookingId) : ICommand<CheckoutBookingResult>` — consumed by Task 8 (`TenantController`)

- [ ] **Step 1: Create the result DTO**

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record CheckoutBookingResult(
        Guid BookingId,
        string BookingReference,
        DateTime CompletedAt,
        decimal AmountSettled,
        string CurrencyCode
    );
}
```

- [ ] **Step 2: Create the command**

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CheckoutBooking
{
    public record CheckoutBookingCommand(Guid BookingId) : ICommand<CheckoutBookingResult>;
}
```

- [ ] **Step 3: Create the handler**

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CheckoutBooking
{
    public class CheckoutBookingHandler : ICommandHandler<CheckoutBookingCommand, CheckoutBookingResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public CheckoutBookingHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<CheckoutBookingResult> Handle(CheckoutBookingCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Members, t => t.Bookings]);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Isolated tenant context could not be verified.");

            var scannerUserId = _userContext.GetCurrentUserId();
            var scanner = tenant.Members.FirstOrDefault(m => m.UserId == scannerUserId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("Scanner device is not linked to an active staff account.");

            var bookingBeforeCheckout = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == command.BookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");
            var amountSettled = bookingBeforeCheckout.RemainingBalance; // read before CheckOutBooking zeroes it out

            var booking = tenant.CheckOutBooking(command.BookingId, scanner.BranchId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new CheckoutBookingResult(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.ServiceCompletedAt!.Value,
                amountSettled,
                booking.CurrencyCode);
        }
    }
}
```

- [ ] **Step 4: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Application/Dtos/Response/CheckoutBookingResult.cs ApexBooking.Core.Application/Features/Bookings/Commands/CheckoutBooking/
git commit -m "feat: add CheckoutBookingCommand"
```

---

### Task 8: `TenantController` wiring

**Files:**
- Modify: `ApexBooking.WebApi/Controllers/TenantController.cs`

**Interfaces:**
- Consumes: `GetCheckoutDetailsQuery` (Task 6), `CheckoutBookingCommand` (Task 7)
- Produces: `GET /api/Tenant/bookings/{bookingId}/checkout-detail`, `POST /api/Tenant/bookings/{bookingId}/checkout` — consumed by Task 9 (frontend `bookingService.ts`)

- [ ] **Step 1: Add the using statements**

Add alongside the existing `Features.Bookings.*` usings:

```csharp
using ApexBooking.Core.Application.Features.Bookings.Queries.GetCheckoutDetails;
using ApexBooking.Core.Application.Features.Bookings.Commands.CheckoutBooking;
```

- [ ] **Step 2: Add the two actions**

Insert right after the existing `AdmitBooking` action, before `CompleteBooking`:

```csharp
        [HttpGet("bookings/{bookingId:guid}/checkout-detail")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(CheckoutDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCheckoutDetail([FromRoute] Guid bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCheckoutDetailsQuery(bookingId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("bookings/{bookingId:guid}/checkout")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(CheckoutBookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckoutBooking([FromRoute] Guid bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CheckoutBookingCommand(bookingId), cancellationToken);
            return Ok(result);
        }
```

- [ ] **Step 3: Build the full solution**

Run: `dotnet build ApexBooking.sln`
Expected: `Build succeeded.` (A file-lock `MSB3021`/`MSB3027` error with no `error CS` lines means a running debug session is holding the DLL — not a real failure; stop the debug session and rebuild, or trust the per-project builds from earlier tasks.)

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.WebApi/Controllers/TenantController.cs
git commit -m "feat: wire checkout-detail and checkout endpoints"
```

---

### Task 9: Frontend request/response shapes (LocalFlow)

**Files:**
- Create: `C:\Users\Wyrlo\projects\LocalFlow\src\interfaces\ICheckoutDetails.ts`
- Modify: `C:\Users\Wyrlo\projects\LocalFlow\src\interfaces\ITenantBooking.ts`
- Modify: `C:\Users\Wyrlo\projects\LocalFlow\src\services\bookingService.ts`

**Interfaces:**
- Consumes: `CheckoutDetailsDto`, `CheckoutBookingResult` (Task 6, 7), `TenantBookingSummary`'s new fields (Task 5)
- Produces: `ICheckoutDetails`, `ICheckoutBookingResult`, `getCheckoutDetails()`, `checkoutBooking()` — consumed by Task 10 (`AdmitScanModal.tsx`); `ITenantBooking.amountPaidOnline`/`.remainingBalance` — consumed by Tasks 11-12

- [ ] **Step 1: Create the checkout interfaces**

```typescript
// Mirrors ApexBooking.Core.Application.Dtos.Response.CheckoutDetailsDto
export interface ICheckoutDetails {
  bookingId: string
  bookingReference: string
  customerName: string
  customerEmail: string | null
  customerPhone: string | null
  serviceName: string
  staffName: string
  scheduledDate: string
  scheduledStartTime: string
  amountPaidOnline: number
  remainingBalance: number
  currencyCode: string
}

// Mirrors ApexBooking.Core.Application.Dtos.Response.CheckoutBookingResult
export interface ICheckoutBookingResult {
  bookingId: string
  bookingReference: string
  completedAt: string
  amountSettled: number
  currencyCode: string
}
```

- [ ] **Step 2: Add the two new fields to `ITenantBooking`**

In `ITenantBooking.ts`, add to the interface (after `noShowAt`):

```typescript
  noShowAt: string | null
  amountPaidOnline: number
  remainingBalance: number
  createdAt: string
```

- [ ] **Step 3: Add the two new service functions**

In `bookingService.ts`, add the import and two functions:

```typescript
import type { ICheckoutBookingResult, ICheckoutDetails } from '../interfaces/ICheckoutDetails'
```

```typescript
export async function getCheckoutDetails(bookingId: string): Promise<ICheckoutDetails> {
  const response = await authClient.get<ICheckoutDetails>(`/api/Tenant/bookings/${bookingId}/checkout-detail`)
  return response.data
}

export async function checkoutBooking(bookingId: string): Promise<ICheckoutBookingResult> {
  const response = await authClient.post<ICheckoutBookingResult>(`/api/Tenant/bookings/${bookingId}/checkout`)
  return response.data
}
```

- [ ] **Step 4: Typecheck**

Run (from `C:\Users\Wyrlo\projects\LocalFlow`): `npm run build`
Expected: no TypeScript errors from these three files (an unrelated pre-existing error elsewhere in the mid-refactor codebase isn't this task's concern).

- [ ] **Step 5: Commit**

```bash
cd "C:\Users\Wyrlo\projects\LocalFlow"
git add src/interfaces/ICheckoutDetails.ts src/interfaces/ITenantBooking.ts src/services/bookingService.ts
git commit -m "feat: add checkout request/response shapes and service functions"
```

---

### Task 10: `AdmitScanModal` — checkout branch

**Files:**
- Modify: `C:\Users\Wyrlo\projects\LocalFlow\src\components\appointments\AdmitScanModal.tsx`

**Interfaces:**
- Consumes: `getCheckoutDetails`, `checkoutBooking` (Task 9), existing `scanArrival`

- [ ] **Step 1: Rewrite the component**

```tsx
import { useState } from 'react'
import axios from 'axios'
import { Scanner, type IDetectedBarcode } from '@yudiel/react-qr-scanner'
import { Modal } from '../common/Modal'
import { Button } from '../common/Button'
import { useToast } from '../../hooks/useToast'
import { checkoutBooking, getCheckoutDetails, scanArrival } from '../../services/bookingService'
import type { ICheckoutDetails } from '../../interfaces/ICheckoutDetails'

interface IAdmitScanModalProps {
  isOpen: boolean
  onClose: () => void
  onAdmitted: () => void
}

function formatAmount(amount: number, currencyCode: string): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: currencyCode }).format(amount)
}

function extractErrorDetail(error: unknown): string | undefined {
  return axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
}

export function AdmitScanModal({ isOpen, onClose, onAdmitted }: IAdmitScanModalProps) {
  const { showToast } = useToast()
  const [isProcessing, setIsProcessing] = useState(false)
  const [checkoutDetails, setCheckoutDetails] = useState<ICheckoutDetails | null>(null)

  const handleScan = async (results: IDetectedBarcode[]) => {
    const token = results[0]?.rawValue
    if (!token || isProcessing || checkoutDetails) return

    setIsProcessing(true)
    try {
      const result = await scanArrival(token)
      if (result.wasFirstAdmission) {
        showToast('success', `${result.bookingReference} admitted.`)
        onAdmitted()
        onClose()
        return
      }

      // Already admitted, still scheduled — this is the checkout moment.
      const details = await getCheckoutDetails(result.bookingId)
      setCheckoutDetails(details)
    } catch (error) {
      showToast('error', extractErrorDetail(error) ?? 'This boarding pass could not be scanned. Please try again.')
    } finally {
      setIsProcessing(false)
    }
  }

  const handleConfirm = async () => {
    if (!checkoutDetails) return

    setIsProcessing(true)
    try {
      const result = await checkoutBooking(checkoutDetails.bookingId)
      showToast('success', `${result.bookingReference} completed.`)
      onAdmitted()
      handleClose()
    } catch (error) {
      showToast('error', extractErrorDetail(error) ?? 'This checkout could not be confirmed. Please try again.')
    } finally {
      setIsProcessing(false)
    }
  }

  const handleClose = () => {
    setCheckoutDetails(null)
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      title={checkoutDetails ? 'Confirm Checkout' : 'Scan Boarding Pass'}
      onClose={handleClose}
      footer={
        checkoutDetails ? (
          <div className="d-flex justify-content-end gap-2">
            <Button variant="outline-secondary" onClick={handleClose} disabled={isProcessing}>
              Cancel
            </Button>
            <Button onClick={handleConfirm} isLoading={isProcessing}>
              Confirm Checkout
            </Button>
          </div>
        ) : undefined
      }
    >
      {checkoutDetails ? (
        <dl className="row small mb-0 gy-1">
          <dt className="col-4 text-muted fw-normal">Customer</dt>
          <dd className="col-8 mb-0">{checkoutDetails.customerName}</dd>

          <dt className="col-4 text-muted fw-normal">Service</dt>
          <dd className="col-8 mb-0">{checkoutDetails.serviceName}</dd>

          <dt className="col-4 text-muted fw-normal">Team Member</dt>
          <dd className="col-8 mb-0">{checkoutDetails.staffName}</dd>

          <dt className="col-4 text-muted fw-normal">Paid online</dt>
          <dd className="col-8 mb-0">{formatAmount(checkoutDetails.amountPaidOnline, checkoutDetails.currencyCode)}</dd>

          <dt className="col-4 text-muted fw-normal">Left to pay</dt>
          <dd className="col-8 mb-0 fw-semibold">{formatAmount(checkoutDetails.remainingBalance, checkoutDetails.currencyCode)}</dd>
        </dl>
      ) : (
        <>
          <p className="text-muted small">Point the camera at the customer's QR boarding pass to check them in or check them out.</p>
          {isOpen && (
            <div className="rounded overflow-hidden">
              <Scanner onScan={handleScan} paused={isProcessing} formats={['qr_code']} />
            </div>
          )}
        </>
      )}
    </Modal>
  )
}
```

- [ ] **Step 2: Typecheck**

Run: `npm run build`
Expected: no TypeScript errors from this file.

- [ ] **Step 3: Commit**

```bash
git add src/components/appointments/AdmitScanModal.tsx
git commit -m "feat: add checkout detail panel to the scanner modal"
```

---

### Task 11: Hide redundant staff actions, fix payment summary

**Files:**
- Modify: `C:\Users\Wyrlo\projects\LocalFlow\src\components\appointments\BookingTable.tsx`
- Modify: `C:\Users\Wyrlo\projects\LocalFlow\src\components\calendar\BookingDetailPanel.tsx`

**Interfaces:**
- Consumes: `useAuth()`, `Role` (existing), `ITenantBooking.amountPaidOnline`/`.remainingBalance` (Task 9)

- [ ] **Step 1: Gate Complete/No-show in `BookingTable.tsx`**

Add imports:

```typescript
import { useAuth } from '../../hooks/useAuth'
import { Role } from '../../types/Role'
```

Inside the component, after the existing `isLoading`/`bookings.length === 0` early returns, add:

```typescript
  const { user } = useAuth()
  const canManage = (user?.roles ?? []).some((role) => role === Role.Owner || role === Role.Admin)
```

Change the action-building block from:

```typescript
            const actions: IRowAction[] = []
            if (isScheduled && !isCheckedIn) {
              actions.push(
                { label: 'Admit', icon: 'user-check', tone: 'primary', disabled: isPending, onClick: () => onAdmit(booking) },
                { label: 'No-show', icon: 'no-shows', tone: 'delete', disabled: isPending, onClick: () => onNoShow(booking) },
                { label: 'Cancel', icon: 'x-circle', tone: 'delete', disabled: isPending, onClick: () => onCancel(booking) },
              )
            } else if (isScheduled && isCheckedIn) {
              actions.push(
                { label: 'Complete', icon: 'check-circle', tone: 'primary', disabled: isPending, onClick: () => onComplete(booking) },
                { label: 'Cancel', icon: 'x-circle', tone: 'delete', disabled: isPending, onClick: () => onCancel(booking) },
              )
            }
```

to:

```typescript
            const actions: IRowAction[] = []
            if (isScheduled && !isCheckedIn) {
              actions.push({ label: 'Admit', icon: 'user-check', tone: 'primary', disabled: isPending, onClick: () => onAdmit(booking) })
              if (canManage) {
                actions.push({ label: 'No-show', icon: 'no-shows', tone: 'delete', disabled: isPending, onClick: () => onNoShow(booking) })
              }
              actions.push({ label: 'Cancel', icon: 'x-circle', tone: 'delete', disabled: isPending, onClick: () => onCancel(booking) })
            } else if (isScheduled && isCheckedIn) {
              if (canManage) {
                actions.push({ label: 'Complete', icon: 'check-circle', tone: 'primary', disabled: isPending, onClick: () => onComplete(booking) })
              }
              actions.push({ label: 'Cancel', icon: 'x-circle', tone: 'delete', disabled: isPending, onClick: () => onCancel(booking) })
            }
```

- [ ] **Step 2: Gate Complete/No-show in `BookingDetailPanel.tsx`, fix `getPaymentSummary`**

Add imports:

```typescript
import { useAuth } from '../../hooks/useAuth'
import { Role } from '../../types/Role'
```

Replace `getPaymentSummary`:

```typescript
function getPaymentSummary(booking: ITenantBooking): IPaymentSummary {
  if (booking.paymentConfirmedVia === PaymentConfirmationMethod.Online && booking.remainingBalance > 0) {
    return { label: 'Deposit paid online', tone: 'warning', detail: `${formatAmount(booking.remainingBalance, booking.currencyCode)} left to pay` }
  }

  if (booking.paymentConfirmedVia === PaymentConfirmationMethod.Online) {
    return { label: 'Paid online', tone: 'success', detail: formatAmount(booking.amountPaidOnline, booking.currencyCode) }
  }

  if (booking.paymentConfirmedVia === PaymentConfirmationMethod.PayInVisit) {
    return { label: 'Paid in visit', tone: 'success', detail: formatAmount(booking.amountDue, booking.currencyCode) }
  }

  if (booking.status === BookingStatus.PendingPayment) {
    return { label: 'Awaiting online payment', tone: 'warning', detail: formatAmount(booking.amountDue, booking.currencyCode) }
  }

  return { label: 'Pay in visit', tone: 'primary', detail: formatAmount(booking.remainingBalance, booking.currencyCode) }
}
```

Inside `BookingDetailPanel`, add after the existing `isCheckedIn` line:

```typescript
  const { user } = useAuth()
  const canManage = (user?.roles ?? []).some((role) => role === Role.Owner || role === Role.Admin)
```

Change the action-buttons block from:

```tsx
      {isScheduled && (
        <div className="d-flex flex-wrap gap-2 pt-2 border-top">
          {!isCheckedIn && (
            <Button size="sm" variant="outline-primary" isLoading={isPending} onClick={() => onAdmit(booking)}>
              Mark as Admitted
            </Button>
          )}
          {isCheckedIn && (
            <Button size="sm" variant="outline-primary" isLoading={isPending} onClick={() => onComplete(booking)}>
              Mark as Completed
            </Button>
          )}
          {!isCheckedIn && (
            <Button size="sm" variant="outline-secondary" disabled={isPending} onClick={() => onNoShow(booking)}>
              Mark as No-show
            </Button>
          )}
          <Button size="sm" variant="outline-secondary" disabled={isPending} onClick={() => onCancel(booking)}>
            Cancel
          </Button>
        </div>
      )}
```

to:

```tsx
      {isScheduled && (
        <div className="d-flex flex-wrap gap-2 pt-2 border-top">
          {!isCheckedIn && (
            <Button size="sm" variant="outline-primary" isLoading={isPending} onClick={() => onAdmit(booking)}>
              Mark as Admitted
            </Button>
          )}
          {isCheckedIn && canManage && (
            <Button size="sm" variant="outline-primary" isLoading={isPending} onClick={() => onComplete(booking)}>
              Mark as Completed
            </Button>
          )}
          {!isCheckedIn && canManage && (
            <Button size="sm" variant="outline-secondary" disabled={isPending} onClick={() => onNoShow(booking)}>
              Mark as No-show
            </Button>
          )}
          <Button size="sm" variant="outline-secondary" disabled={isPending} onClick={() => onCancel(booking)}>
            Cancel
          </Button>
        </div>
      )}
```

- [ ] **Step 3: Typecheck**

Run: `npm run build`
Expected: no TypeScript errors from these two files.

- [ ] **Step 4: Commit**

```bash
git add src/components/appointments/BookingTable.tsx src/components/calendar/BookingDetailPanel.tsx
git commit -m "feat: hide Complete/No-show from Staff, show corrected remaining balance"
```

---

### Task 12: `CollectPaymentModal` + `OwnerDashboardPage` fixes

**Files:**
- Modify: `C:\Users\Wyrlo\projects\LocalFlow\src\components\dashboard\CollectPaymentModal.tsx`
- Modify: `C:\Users\Wyrlo\projects\LocalFlow\src\pages\booking\OwnerDashboardPage.tsx`

**Interfaces:**
- Consumes: `ITenantBooking.remainingBalance` (Task 9)

- [ ] **Step 1: Show the corrected amount in `CollectPaymentModal.tsx`**

Change:

```tsx
          {selectedBooking && (
            <p className="fw-semibold mb-0">
              Amount due: {selectedBooking.amountDue.toFixed(2)} {selectedBooking.currencyCode}
            </p>
          )}
```

to:

```tsx
          {selectedBooking && (
            <p className="fw-semibold mb-0">
              Amount due: {selectedBooking.remainingBalance.toFixed(2)} {selectedBooking.currencyCode}
            </p>
          )}
```

- [ ] **Step 2: Fix the modal's filter and the scanner's no-op refetch in `OwnerDashboardPage.tsx`**

Change:

```tsx
      <CollectPaymentModal
        isOpen={isCollectPaymentModalOpen}
        bookings={todaysBookings.filter((b) => b.paymentConfirmedVia === null)}
        isSubmitting={isCollectingPayment}
        onClose={() => setIsCollectPaymentModalOpen(false)}
        onSubmit={handleCollectPayment}
      />
```

to:

```tsx
      <CollectPaymentModal
        isOpen={isCollectPaymentModalOpen}
        bookings={todaysBookings.filter((b) => b.remainingBalance > 0)}
        isSubmitting={isCollectingPayment}
        onClose={() => setIsCollectPaymentModalOpen(false)}
        onSubmit={handleCollectPayment}
      />
```

(The old `paymentConfirmedVia === null` filter would exclude a deposit-paid-online booking even though it still has a remainder owed — `remainingBalance > 0` is the correct condition now that field exists.)

Change:

```tsx
      <AdmitScanModal isOpen={isScanModalOpen} onClose={() => setIsScanModalOpen(false)} onAdmitted={() => {}} />
```

to:

```tsx
      <AdmitScanModal isOpen={isScanModalOpen} onClose={() => setIsScanModalOpen(false)} onAdmitted={refetchTodaysBookings} />
```

- [ ] **Step 3: Typecheck**

Run: `npm run build`
Expected: no TypeScript errors from these two files.

- [ ] **Step 4: Commit**

```bash
git add src/components/dashboard/CollectPaymentModal.tsx src/pages/booking/OwnerDashboardPage.tsx
git commit -m "feat: fix collect-payment filter/amount and scanner refetch on Owner dashboard"
```

---

### Task 13: Manual end-to-end verification

**Files:** none — verification only.

- [ ] **Step 1: Apply the migration**

Run: `dotnet ef database update --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`

- [ ] **Step 2: Full backend build + test**

Run: `dotnet build ApexBooking.sln` then `dotnet test ApexBooking.Core.Domain.UnitTests`
Expected: build succeeds, all tests pass.

- [ ] **Step 3: Full frontend build**

Run (from LocalFlow): `npm run build`
Expected: no errors.

- [ ] **Step 4: Exercise the full flow via `/run`, as Staff**

1. Create a booking under a `None` (pay-at-counter) policy service. Scan its QR (or use manual Admit) — confirm "admitted" toast, `checkedInAt` set, status stays `Scheduled`.
2. Scan the **same** code again (or trigger the same scan-arrival call) — confirm the checkout detail panel appears showing customer/service/staff, "Paid online" = 0, "Left to pay" = the full service price.
3. Click Confirm Checkout — confirm success toast, booking becomes `Completed`, and (as Owner/Admin) the booking's payment summary shows "Paid in visit" for the full amount.
4. Repeat with a `DepositRequired` service: confirm the deposit is charged online, then admit, then checkout — "Paid online" shows the deposit amount, "Left to pay" shows the true remainder (service price − deposit), and confirming settles it to `RemainingBalance == 0`.
5. As Staff, view `/appointments` and `/calendar` for a checked-in booking — confirm no Complete/No-show buttons appear, but Cancel still does.
6. As Owner/Admin, confirm Complete/No-show/Collect-Payment still work exactly as before for a booking that was never scanned (manual fallback path).
7. Try scanning/checking out a booking that's already `Completed` — confirm a clear "already completed" error, not a silent no-op or generic failure.
8. Check the Owner Dashboard's "Pay-on-Visit Revenue" tile after the deposit-then-remainder scenario above — confirm the remainder amount appears there (not just the online-charged deposit under "Online Revenue").
