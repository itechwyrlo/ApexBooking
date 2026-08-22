# Payment & Booking Security Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden the existing PayMongo Direct Connect payment flow and booking engine: encrypt tenant credentials at rest, close the PendingPayment race condition with a transaction-owned `sp_getapplock`, add webhook idempotency with atomic ledger commits, and document the logging/legal boundary.

**Architecture:** Four largely-independent slices inside the existing Clean Architecture layering (`Core.Domain` → `Core.Application` → `Core.Persistence`/`Infrastructure` → `WebApi`), each adding one new port+adapter pair or extending an existing one. No new projects, no restructuring of existing files beyond the specific methods/classes named below.

**Tech Stack:** .NET 10 / ASP.NET Core, EF Core 10 (SQL Server), MediatR, Hangfire, ASP.NET Core Data Protection, xUnit.

**Spec:** [docs/superpowers/specs/2026-08-18-payment-booking-security-hardening-design.md](../specs/2026-08-18-payment-booking-security-hardening-design.md)

## Global Constraints

- **Do not run `git commit`, `git add`, `git reset`, `git checkout`, or `git stash` at any point in this plan.** Every task ends with a verification step, never a commit step — leave all changes uncommitted in the working tree. Do not touch any file outside the ones this plan names.
- **Do not run `dotnet ef database update`.** Migrations are generated as code (`dotnet ef migrations add ...`) and left unapplied.
- Stale `PendingPayment` cutoff: **30 minutes**. Sweep cadence: every 5 minutes.
- Lock resource key format (Module 2): `booking:{tenantId}:{staffId}:{date:yyyyMMdd}` — must match the staff+date dimension the collision check uses.
- `TenantPaymentCredential.SecretKey` and `WebhookSecret` are encrypted at rest; `PublicKey` stays plaintext.
- Data Protection key ring persists via EF Core into SQL Server (`PersistKeysToDbContext`) — no local disk, no cloud KMS.
- ToS boilerplate (Module 4) uses a `[Company Legal Name]` placeholder, not a real entity name.
- No data migration for existing `TenantPaymentCredentials` rows — dev environment, no real tenant data.
- Out of scope, do not implement: PayMongo child-account onboarding, KYC, Account-Id payment routing, Payment Links migration, Checkout Sessions, Payment Intents, merchant lifecycle webhooks, or retiring the current per-tenant credential model.

---

## File Structure

**New files:**
- `src/backend-api/ApexBooking.Core.Domain.UnitTests/Entities/BookingExpirationTests.cs` — domain tests for Task 1.
- `src/backend-api/ApexBooking.Core.Domain/Interfaces/IBookingLockScope.cs` — Task 4.
- `src/backend-api/ApexBooking.Core.Persistence/Concurrency/BookingLockScope.cs` — Task 4.
- `src/backend-api/ApexBooking.Infrastructure/BackgroundJobs/ExpireStalePendingBookingsJob.cs` — Task 7.
- `src/backend-api/ApexBooking.Core.Domain/Services/ISecretProtector.cs` — Task 8.
- `src/backend-api/ApexBooking.Infrastructure/ExternalServices/DataProtection/DataProtectionSecretProtector.cs` — Task 8.
- `src/backend-api/ApexBooking.Core.Domain/Entities/ProcessedPaymentEvent.cs` — Task 13.
- `src/backend-api/ApexBooking.Core.Persistence/Mappings/ProcessedPaymentEventConfiguration.cs` — Task 13.
- `src/backend-api/ApexBooking.Core.Domain/Services/IProcessedPaymentEventStore.cs` — Task 14.
- `src/backend-api/ApexBooking.Core.Persistence/Services/ProcessedPaymentEventStore.cs` — Task 14.
- `docs/compliance/2026-08-18-logging-boundary-and-tos-boilerplate.md` — Task 17.
- Three generated EF migrations under `src/backend-api/ApexBooking.Core.Persistence/Migrations/` (Tasks 3, 9, 13 — filenames are timestamp-prefixed by the tool).

**Modified files:**
- `src/backend-api/ApexBooking.Core.Domain/Enums/BookingStatus.cs` (Task 1)
- `src/backend-api/ApexBooking.Core.Domain/Events/BookingEvents.cs` (Task 1)
- `src/backend-api/ApexBooking.Core.Domain/Entities/Booking.cs` (Tasks 1, 3)
- `src/backend-api/ApexBooking.Core.Domain/Entities/Tenant.cs` (Task 2)
- `src/backend-api/ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs` (Task 3)
- `src/backend-api/ApexBooking.Core.Domain/Interfaces/IUnitOfWork.cs` (Task 4)
- `src/backend-api/ApexBooking.Core.Persistence/UnitOfWork.cs` (Task 4)
- `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/InitiateBooking/InitiateBookingHandler.cs` (Task 5)
- `src/backend-api/ApexBooking.Core.Domain/Repositories/ITenantRepository.cs` (Task 6)
- `src/backend-api/ApexBooking.Core.Persistence/Repositories/TenantRepository.cs` (Task 6)
- `src/backend-api/ApexBooking.Infrastructure/ApexBooking.Infrastructure.csproj` (Task 6)
- `src/backend-api/ApexBooking.Infrastructure/BackgroundJobs/HangfireServiceExtensions.cs` (Task 7)
- `src/backend-api/ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs` (Tasks 8, 9)
- `src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj` (Task 9)
- `src/backend-api/ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs` (Tasks 9, 13)
- `src/backend-api/ApexBooking.Core.Persistence/Mappings/TenantPaymentCredentialConfiguration.cs` (Task 10)
- `src/backend-api/ApexBooking.Core.Domain/Services/Paymongo/PaymongoContracts.cs` (Task 11)
- `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommand.cs` (Task 11)
- `src/backend-api/ApexBooking.Core.Domain/Services/Paymongo/IPayMongoWebhookSignatureVerifier.cs` (Task 12)
- `src/backend-api/ApexBooking.Infrastructure/ExternalServices/PayMongo/PayMongoWebhookSignatureVerifier.cs` (Task 12)
- `src/backend-api/ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs` (Task 14)
- `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommandHandler.cs` (Task 15)
- `src/backend-api/ApexBooking.WebApi/Controllers/PayMongoWebhooksController.cs` (Task 16)

---

## Module 2 — Booking concurrency & expiration

### Task 1: `BookingStatus.Expired` + `Booking.ExpirePendingPayment()` + `BookingExpiredDomainEvent`

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Domain/Enums/BookingStatus.cs`
- Modify: `src/backend-api/ApexBooking.Core.Domain/Events/BookingEvents.cs`
- Modify: `src/backend-api/ApexBooking.Core.Domain/Entities/Booking.cs`
- Test: `src/backend-api/ApexBooking.Core.Domain.UnitTests/Entities/BookingExpirationTests.cs`

**Interfaces:**
- Produces: `BookingStatus.Expired` (enum member), `Booking.ExpirePendingPayment()` (`public void`, no params, throws `BusinessRuleBrokenException` unless `Status == BookingStatus.PendingPayment`), `BookingExpiredDomainEvent : IReliableDomainEvent` (`TenantId, Guid BookingId, string BookingReference, Guid CustomerId, DateTime ExpiredAt`).

- [ ] **Step 1: Write the failing test**

Create `src/backend-api/ApexBooking.Core.Domain.UnitTests/Entities/BookingExpirationTests.cs`:

```csharp
using System;
using System.Linq;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using Xunit;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class BookingExpirationTests
{
    private static Booking CreatePendingPaymentBooking()
    {
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-02",
            scheduledDate: DateOnly.FromDateTime(DateTime.UtcNow),
            scheduledStartTime: TimeOnly.FromDateTime(DateTime.UtcNow),
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: true,
            currencyCode: "PHP",
            amountDue: 500m,
            servicePriceAtBooking: 500m);

        booking.ClearDomainEvents();
        return booking;
    }

    [Fact]
    public void ExpirePendingPayment_WhenPendingPayment_SetsExpiredAndRaisesEvent()
    {
        var booking = CreatePendingPaymentBooking();

        booking.ExpirePendingPayment();

        Assert.Equal(BookingStatus.Expired, booking.Status);
        var raised = Assert.Single(booking.DomainEvents.OfType<BookingExpiredDomainEvent>());
        Assert.Equal(booking.BookingId.Value, raised.BookingId);
        Assert.Equal(booking.TenantId, raised.TenantId);
        Assert.Equal(booking.CustomerId.Value, raised.CustomerId);
        Assert.Equal(booking.BookingReference, raised.BookingReference);
    }

    [Fact]
    public void ExpirePendingPayment_WhenNotPendingPayment_Throws()
    {
        var booking = CreatePendingPaymentBooking();
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");
        booking.ClearDomainEvents();

        Assert.Throws<BusinessRuleBrokenException>(() => booking.ExpirePendingPayment());
        Assert.Equal(BookingStatus.Scheduled, booking.Status);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test src/backend-api/ApexBooking.Core.Domain.UnitTests --filter BookingExpirationTests`
Expected: build error — `BookingStatus.Expired`, `Booking.ExpirePendingPayment`, and `BookingExpiredDomainEvent` don't exist yet.

- [ ] **Step 3: Add `Expired` to `BookingStatus`**

In `src/backend-api/ApexBooking.Core.Domain/Enums/BookingStatus.cs`, add a sixth member (string-converted column, no migration needed for an enum member):

```csharp
namespace ApexBooking.Core.Domain.Enums
{
    public enum BookingStatus
    {
        PendingPayment = 1,
        // Active calendar block (Used for both future appointments and active walk-ins)
        Scheduled = 2,

        // The service is finished, payment can be captured, and staff is freed up
        Completed = 3,

        // The customer didn't show up within the shop's buffer time; slot was opened up manually
        NoShow = 4,

        // The appointment was called off ahead of time by the user or administrator
        Cancelled = 5,

        // A PendingPayment checkout was abandoned past the stale-payment window and the slot was
        // reclaimed by ExpireStalePendingBookingsJob — see Booking.ExpirePendingPayment().
        Expired = 6
    }
}
```

- [ ] **Step 4: Add `BookingExpiredDomainEvent`**

In `src/backend-api/ApexBooking.Core.Domain/Events/BookingEvents.cs`, add after `BookingCancellationNoticeDomainEvent`:

```csharp
// Raised when a PendingPayment checkout goes stale and is reclaimed by
// ExpireStalePendingBookingsJob (Booking.ExpirePendingPayment) — drives a customer-facing notice
// that their slot was released. Reliable: the notice is an external email call.
public record BookingExpiredDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    Guid CustomerId,
    DateTime ExpiredAt
) : IReliableDomainEvent;
```

- [ ] **Step 5: Add `Booking.ExpirePendingPayment()`**

In `src/backend-api/ApexBooking.Core.Domain/Entities/Booking.cs`, add after `ClearPendingPaymentOnArrival()`:

```csharp
        // ── Stale Checkout Reclaim ──────────────────────────────────────────────
        // Called by ExpireStalePendingBookingsJob once a PendingPayment checkout has sat unpaid
        // past the stale-payment window. Frees the staff/date slot for the collision check in
        // Tenant.PlaceBooking by moving this booking out of PendingPayment entirely — a live
        // webhook landing on the same row after this point (ConfirmPayment requires
        // Status == PendingPayment) will fail its own status guard instead of silently succeeding,
        // and RowVersion (see BookingConfiguration) is what makes the reverse race — a webhook that
        // wins moments before this job's SaveChanges — surface as a safe DbUpdateConcurrencyException.
        public void ExpirePendingPayment()
        {
            if (Status != BookingStatus.PendingPayment)
                throw new BusinessRuleBrokenException("Only appointments pending payment can be expired.");

            Status = BookingStatus.Expired;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BookingExpiredDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CustomerId: this.CustomerId.Value,
                ExpiredAt: UpdatedAt
            ));
        }
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test src/backend-api/ApexBooking.Core.Domain.UnitTests --filter BookingExpirationTests`
Expected: PASS (2 tests).

- [ ] **Step 7: Leave changes uncommitted** — do not `git add`/`git commit` (see Global Constraints).

---

### Task 2: Fix the collision check to include `PendingPayment`

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Domain/Entities/Tenant.cs:434-439`

**Interfaces:**
- Consumes: nothing new.
- Produces: nothing new — this is a one-line predicate fix inside `PlaceBooking`.

No unit test is added for this specific line: exercising it requires a fully-populated `Tenant` aggregate (branch + active staff deployed to that branch + active service), and `Tenant` only exposes `AddBranch` as a simple public builder — staff and service catalog rows are attached through EF-tracked collections in the Application/Persistence layers, not through a public `Tenant.AddStaff`/`AddService` domain method, so there is no way to build that object graph through the aggregate's own public API alone. The fix is a single boolean-predicate change; it's covered end-to-end by the "Concurrent booking" manual verification step in the spec's Testing section, which cannot pass without this fix.

- [ ] **Step 1: Apply the fix**

In `src/backend-api/ApexBooking.Core.Domain/Entities/Tenant.cs`, change:

```csharp
            var newBlockEnd = startTime.AddMinutes(service.DurationMinutes + service.BufferAfterMinutes);
            bool collidesWithExistingBooking = _bookings.Any(b =>
                b.StaffId == staffId
                && b.ScheduledDate == date
                && b.Status == BookingStatus.Scheduled
                && b.ScheduledStartTime < newBlockEnd
                && b.ScheduledEndTime > startTime);
```

to:

```csharp
            var newBlockEnd = startTime.AddMinutes(service.DurationMinutes + service.BufferAfterMinutes);
            bool collidesWithExistingBooking = _bookings.Any(b =>
                b.StaffId == staffId
                && b.ScheduledDate == date
                // PendingPayment must block the slot too — otherwise two customers can both reach
                // checkout for the same staff/time before either one pays (see
                // 2026-08-18-payment-booking-security-hardening-design.md, Module 2). The
                // InitiateBookingHandler-held sp_getapplock (IUnitOfWork.AcquireBookingLockAsync)
                // is what makes this check race-free against a second concurrent request; this
                // predicate is what makes it correct once only one request is in here at a time.
                && (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.PendingPayment)
                && b.ScheduledStartTime < newBlockEnd
                && b.ScheduledEndTime > startTime);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Domain/ApexBooking.Core.Domain.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Leave changes uncommitted.**

---

### Task 3: `Booking.RowVersion` concurrency token + migration

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Domain/Entities/Booking.cs`
- Modify: `src/backend-api/ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs`
- Migration (generate only, do not apply): `src/backend-api/ApexBooking.Core.Persistence/Migrations/`

**Interfaces:**
- Produces: `Booking.RowVersion` (`public byte[] RowVersion { get; private set; }`).

- [ ] **Step 1: Add the property**

In `src/backend-api/ApexBooking.Core.Domain/Entities/Booking.cs`, add near the other audit fields (after `public DateTime UpdatedAt { get; private set; }`):

```csharp
        // EF Core concurrency token (SQL Server `rowversion`, DB-generated on every UPDATE) — lets
        // ExpireStalePendingBookingsJob and a live webhook confirming payment race safely: whichever
        // SaveChanges commits second throws DbUpdateConcurrencyException instead of silently
        // clobbering the other's write. See BookingConfiguration for the mapping.
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
```

- [ ] **Step 2: Map it as a concurrency token**

In `src/backend-api/ApexBooking.Core.Persistence/Mappings/BookingConfiguration.cs`, add inside `Configure`, right before the `// 6. Audit & Ignores` section:

```csharp
            // 5b. Concurrency token — SQL Server `rowversion`, auto-generated, never set by app code.
            builder.Property(b => b.RowVersion)
                .HasColumnName("row_version")
                .IsRowVersion();
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Generate the migration (do NOT apply it)**

Run:
```bash
dotnet ef migrations add AddBookingRowVersion --project src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj --startup-project src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj
```
Expected: a new `<timestamp>_AddBookingRowVersion.cs` + `.Designer.cs` appear under `src/backend-api/ApexBooking.Core.Persistence/Migrations/`, and `ApexBookingDbContextModelSnapshot.cs` is updated. **Do not run `dotnet ef database update`.**

- [ ] **Step 5: Inspect the generated migration**

Open the new `<timestamp>_AddBookingRowVersion.cs` and confirm `Up()` contains exactly one `AddColumn` call for `row_version` on table `bookings`, typed `rowversion`/`timestamp`, `rowVersion: true`. If it contains anything else (e.g. an unrelated column diff), stop and re-check Step 1/2 before proceeding — do not hand-edit the generated file.

- [ ] **Step 6: Leave changes uncommitted.**

---

### Task 4: `IUnitOfWork.AcquireBookingLockAsync` — transaction-owned `sp_getapplock`

**Files:**
- Create: `src/backend-api/ApexBooking.Core.Domain/Interfaces/IBookingLockScope.cs`
- Create: `src/backend-api/ApexBooking.Core.Persistence/Concurrency/BookingLockScope.cs`
- Modify: `src/backend-api/ApexBooking.Core.Domain/Interfaces/IUnitOfWork.cs`
- Modify: `src/backend-api/ApexBooking.Core.Persistence/UnitOfWork.cs`

**Interfaces:**
- Produces: `IBookingLockScope : IAsyncDisposable` with `Task CommitAsync(CancellationToken cancellationToken = default)`. `IUnitOfWork.AcquireBookingLockAsync(string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default) : Task<IBookingLockScope>`.
- Consumed by: Task 5 (`InitiateBookingHandler`).

No automated test: this exercises a real SQL Server connection/transaction (`sp_getapplock`), which this codebase has no integration-test harness for (only the pure-domain `Core.Domain.UnitTests` project exists). Covered by the spec's "Concurrent booking" manual verification step.

- [ ] **Step 1: Define the port**

Create `src/backend-api/ApexBooking.Core.Domain/Interfaces/IBookingLockScope.cs`:

```csharp
namespace ApexBooking.Core.Domain.Interfaces;

/// <summary>
/// A held `sp_getapplock` transaction scope — see IUnitOfWork.AcquireBookingLockAsync. Disposing
/// without calling CommitAsync rolls the underlying transaction back (releasing the lock along
/// with it), so `await using` around the whole critical section is enough to make failure-path
/// cleanup automatic.
/// </summary>
public interface IBookingLockScope : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction the lock lives in. sp_getapplock's Transaction-owned lock releases
    /// automatically as part of this commit — there is no separate release call.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Add the method to `IUnitOfWork`**

In `src/backend-api/ApexBooking.Core.Domain/Interfaces/IUnitOfWork.cs`, replace the file with:

```csharp
using ApexBooking.Core.Domain.Repositories;

namespace ApexBooking.Core.Domain.Interfaces;

public interface IUnitOfWork
{
    ITenantRepository TenantRepository { get; }
    ITenantRegistrationRequestRepository TenantRegistrationRequestRepository {get;}
    ICustomerRepository CustomerRepository { get; }
    INotificationRepository NotificationRepository { get; }
    IFcmTokenRepository FcmTokenRepository { get; }
    Task<int> CompleteAsync();
    Task<int> CompleteAsync(CancellationToken cancellationToken);

    // Acquires a SQL Server sp_getapplock ('Exclusive', LockOwner='Transaction') scoped to
    // resourceKey, inside a transaction on this UnitOfWork's own DbContext connection. The caller
    // must perform all of its reads/writes for the critical section through this same
    // IUnitOfWork/its repositories (NOT a second DbContext) and must call CompleteAsync() — which
    // joins this same transaction automatically — before calling CommitAsync() on the returned
    // scope. See docs/superpowers/specs/2026-08-18-payment-booking-security-hardening-design.md,
    // Module 2, for why a separate connection/transaction would make the lock ineffective.
    Task<IBookingLockScope> AcquireBookingLockAsync(string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Implement the lock scope**

Create `src/backend-api/ApexBooking.Core.Persistence/Concurrency/BookingLockScope.cs`:

```csharp
using ApexBooking.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace ApexBooking.Core.Persistence.Concurrency;

internal sealed class BookingLockScope : IBookingLockScope
{
    private readonly IDbContextTransaction _transaction;
    private bool _committed;

    public BookingLockScope(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            await _transaction.RollbackAsync();
        }

        await _transaction.DisposeAsync();
    }
}
```

- [ ] **Step 4: Implement `AcquireBookingLockAsync` in `UnitOfWork`**

In `src/backend-api/ApexBooking.Core.Persistence/UnitOfWork.cs`, add `using ApexBooking.Core.Persistence.Concurrency;` and `using Microsoft.Data.SqlClient;` to the top of the file, then add this method to the `UnitOfWork` class (after `CompleteAsync(CancellationToken)`):

```csharp
        public async Task<IBookingLockScope> AcquireBookingLockAsync(string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // sp_getapplock returns via RETURN (0/1 success, <0 failure — timeout, deadlock
                // victim, or parameter error) rather than a result set, so the failure path is
                // surfaced as a raised error inside the same batch rather than an inspected return
                // value. LockOwner='Transaction' ties the lock's lifetime to this transaction: it
                // releases automatically on commit OR rollback, with no separate release call needed.
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"DECLARE @lockResult int;
                       EXEC @lockResult = sp_getapplock
                           @Resource = {resourceKey},
                           @LockMode = 'Exclusive',
                           @LockOwner = 'Transaction',
                           @LockTimeout = {(int)timeout.TotalMilliseconds};
                       IF @lockResult < 0
                           THROW 51000, 'Could not acquire booking slot lock.', 1;",
                    cancellationToken);
            }
            catch (SqlException)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                await transaction.DisposeAsync();
                throw new BusinessRuleBrokenException("This time slot is currently being booked by someone else. Please try again.");
            }

            return new BookingLockScope(transaction);
        }
```

Add `using ApexBooking.SharedKernel.Exceptions;` to `UnitOfWork.cs` if not already present (it isn't, per the current file).

- [ ] **Step 5: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: Build succeeded, 0 errors. (`Microsoft.Data.SqlClient` is already available transitively via `Microsoft.EntityFrameworkCore.SqlServer`, referenced through the `ApexBooking.GenericRepository.EntityFramework` project reference — no new package needed.)

- [ ] **Step 6: Leave changes uncommitted.**

---

### Task 5: Wire the lock into `InitiateBookingHandler`

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/InitiateBooking/InitiateBookingHandler.cs`

**Interfaces:**
- Consumes: `IUnitOfWork.AcquireBookingLockAsync(string, TimeSpan, CancellationToken)` → `IBookingLockScope` (Task 4).

- [ ] **Step 1: Acquire the lock before loading the tenant, commit it after `CompleteAsync`**

In `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/InitiateBooking/InitiateBookingHandler.cs`, change the start of `Handle` from:

```csharp
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Booking transaction failed. No tenant context could be resolved for this request.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
```

to:

```csharp
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Booking transaction failed. No tenant context could be resolved for this request.");

            // Acquired BEFORE any read — the collision check inside PlaceCustomerBooking below is
            // only race-free while this lock is held for the entire critical section (this same
            // DbContext, through to CompleteAsync). Resource key matches the exact staff+date
            // dimension Tenant.PlaceBooking's collision predicate uses.
            var lockResourceKey = $"booking:{tenantId.Value}:{command.StaffId}:{command.ScheduledDate:yyyyMMdd}";
            await using var lockScope = await _unitOfWork.AcquireBookingLockAsync(
                lockResourceKey, TimeSpan.FromSeconds(5), cancellationToken);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
```

- [ ] **Step 2: Commit the lock scope after `CompleteAsync`**

Change:

```csharp
            // 8. Track modifications and commit atomically down to the data access layer
            _unitOfWork.TenantRepository.Update(tenant);

            // 🌟 TRANSACTION COMMIT LINE:
            // If payment wasn't required, the BookingScheduledDomainEvent raised inside the factory
            // is intercepted and executed completely during this Save method automatically!
            await _unitOfWork.CompleteAsync(cancellationToken);
```

to:

```csharp
            // 8. Track modifications and commit atomically down to the data access layer
            _unitOfWork.TenantRepository.Update(tenant);

            // 🌟 TRANSACTION COMMIT LINE:
            // If payment wasn't required, the BookingScheduledDomainEvent raised inside the factory
            // is intercepted and executed completely during this Save method automatically! This
            // SaveChangesAsync call joins the lock's already-open transaction (EF Core does not open
            // a second one while Database.CurrentTransaction is set) — it does not commit on its own.
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Commits the transaction the lock lives in — sp_getapplock's Transaction-owned lock
            // releases automatically as part of this. NOTE: the PayMongo external call above (step
            // 7, CreatePaymentSourceAsync) runs while this transaction/lock is still open — this is
            // pre-existing ordering (CompleteAsync already ran after that call before this change)
            // and is out of scope to restructure here; it does mean the lock's hold time includes
            // one outbound HTTPS call when upfront payment is required.
            await lockScope.CommitAsync(cancellationToken);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Leave changes uncommitted.**

---

### Task 6: Cross-tenant stale-bookings query

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Domain/Repositories/ITenantRepository.cs`
- Modify: `src/backend-api/ApexBooking.Core.Persistence/Repositories/TenantRepository.cs`
- Modify: `src/backend-api/ApexBooking.Infrastructure/ApexBooking.Infrastructure.csproj`

**Interfaces:**
- Produces: `ITenantRepository.GetTenantsWithStalePendingBookingsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) : Task<IReadOnlyCollection<Tenant>>`.
- Consumed by: Task 7 (`ExpireStalePendingBookingsJob`).

- [ ] **Step 1: Add the interface method**

In `src/backend-api/ApexBooking.Core.Domain/Repositories/ITenantRepository.cs`, add inside the interface, after `GetByBookingIdAsync`:

```csharp
    // Cross-tenant sweep query for ExpireStalePendingBookingsJob — same "Booking is a Tenant
    // child, escape-hatch query" rationale as GetByBookingIdAsync above, but returning every
    // matching tenant (with their full Bookings collection loaded) instead of a single one.
    // Runs with no ambient tenant (background job), so the global query filter already passes
    // everything through, but IgnoreQueryFilters() states that intent explicitly rather than
    // relying on it incidentally.
    Task<IReadOnlyCollection<Tenant>> GetTenantsWithStalePendingBookingsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Implement it**

In `src/backend-api/ApexBooking.Core.Persistence/Repositories/TenantRepository.cs`, add after `GetByBookingIdAsync`:

```csharp
    public async Task<IReadOnlyCollection<Tenant>> GetTenantsWithStalePendingBookingsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        return await context.Tenants
            .IgnoreQueryFilters()
            .Include(t => t.Bookings)
            .Where(t => t.Bookings.Any(b => b.Status == BookingStatus.PendingPayment && b.CreatedAt <= cutoffUtc))
            .ToListAsync(cancellationToken);
    }
```

- [ ] **Step 3: Add the EF Core package to Infrastructure**

`ApexBooking.Infrastructure.csproj` has no `Microsoft.EntityFrameworkCore` package today (only a `FrameworkReference` to `Microsoft.AspNetCore.App`, which does not include EF Core) — Task 7's job needs `Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException`. In `src/backend-api/ApexBooking.Infrastructure/ApexBooking.Infrastructure.csproj`, add to the existing package `ItemGroup`:

```xml
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.9" />
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Leave changes uncommitted.**

---

### Task 7: `ExpireStalePendingBookingsJob` + Hangfire registration

**Files:**
- Create: `src/backend-api/ApexBooking.Infrastructure/BackgroundJobs/ExpireStalePendingBookingsJob.cs`
- Modify: `src/backend-api/ApexBooking.Infrastructure/BackgroundJobs/HangfireServiceExtensions.cs`

**Interfaces:**
- Consumes: `ITenantRepository.GetTenantsWithStalePendingBookingsAsync` (Task 6), `Booking.ExpirePendingPayment()` (Task 1).

- [ ] **Step 1: Write the job**

Create `src/backend-api/ApexBooking.Infrastructure/BackgroundJobs/ExpireStalePendingBookingsJob.cs`:

```csharp
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job — reclaims PendingPayment bookings whose checkout has sat unpaid past
/// the stale window (design spec: 2026-08-18-payment-booking-security-hardening-design.md, Module
/// 2). Same per-tenant try/catch shape as TrialExpiryJob: one tenant's failure, including a
/// DbUpdateConcurrencyException from a webhook that just confirmed payment on one of its bookings
/// (see Booking.RowVersion), does not block the rest of the sweep.
/// </summary>
public class ExpireStalePendingBookingsJob
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpireStalePendingBookingsJob> _logger;

    public ExpireStalePendingBookingsJob(IUnitOfWork unitOfWork, ILogger<ExpireStalePendingBookingsJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Run(IJobCancellationToken cancellationToken)
    {
        var ct = cancellationToken.ShutdownToken;
        var cutoffUtc = DateTime.UtcNow - StaleAfter;

        var tenants = await _unitOfWork.TenantRepository.GetTenantsWithStalePendingBookingsAsync(cutoffUtc, ct);

        foreach (var tenant in tenants)
        {
            try
            {
                var staleBookings = tenant.Bookings
                    .Where(b => b.Status == BookingStatus.PendingPayment && b.CreatedAt <= cutoffUtc)
                    .ToList();

                foreach (var booking in staleBookings)
                {
                    booking.ExpirePendingPayment();
                }

                _unitOfWork.TenantRepository.Update(tenant);
                await _unitOfWork.CompleteAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // A webhook confirmed payment on one of this tenant's bookings between our read and
                // this SaveChanges — RowVersion caught the conflict. Those bookings are no longer
                // actually stale; nothing to do here, the next sweep re-evaluates from scratch.
                _logger.LogInformation(ex,
                    "Skipped expiring stale bookings for Tenant {TenantId}: a concurrent payment confirmation won the race.",
                    tenant.TenantId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire stale pending bookings for Tenant {TenantId}.", tenant.TenantId.Value);
            }
        }
    }
}
```

- [ ] **Step 2: Register the job**

In `src/backend-api/ApexBooking.Infrastructure/BackgroundJobs/HangfireServiceExtensions.cs`, add the job id constant, service registration, and recurring-job registration:

```csharp
    private const string OutboxRelaySweepJobId = "outbox-relay-sweep";
    private const string TrialExpirySweepJobId = "trial-expiry-sweep";
    private const string ExpireStalePendingBookingsJobId = "expire-stale-pending-bookings";
```

```csharp
        services.AddScoped<IOutboxTrigger, HangfireOutboxTrigger>();
        services.AddScoped<OutboxRelayJob>();
        services.AddScoped<TrialExpiryJob>();
        services.AddScoped<ExpireStalePendingBookingsJob>();
```

```csharp
        RecurringJob.AddOrUpdate<TrialExpiryJob>(
            TrialExpirySweepJobId,
            job => job.Run(JobCancellationToken.Null),
            Cron.Hourly(),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        // Every 5 minutes: frequent enough to reclaim a 30-minute-stale slot promptly without
        // being wasteful. Adjust if a different cadence is preferred.
        RecurringJob.AddOrUpdate<ExpireStalePendingBookingsJob>(
            ExpireStalePendingBookingsJobId,
            job => job.Run(JobCancellationToken.Null),
            Cron.MinuteInterval(5),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Infrastructure/ApexBooking.Infrastructure.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Leave changes uncommitted.**

---

## Module 1 — Credential envelope encryption

### Task 8: `ISecretProtector` port + `DataProtectionSecretProtector` adapter

**Files:**
- Create: `src/backend-api/ApexBooking.Core.Domain/Services/ISecretProtector.cs`
- Create: `src/backend-api/ApexBooking.Infrastructure/ExternalServices/DataProtection/DataProtectionSecretProtector.cs`
- Modify: `src/backend-api/ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs`

**Interfaces:**
- Produces: `ISecretProtector.Protect(string) : string`, `ISecretProtector.Unprotect(string) : string`.
- Consumed by: Task 10 (`TenantPaymentCredentialConfiguration`).

- [ ] **Step 1: Define the port**

Create `src/backend-api/ApexBooking.Core.Domain/Services/ISecretProtector.cs`:

```csharp
namespace ApexBooking.Core.Domain.Services;

/// <summary>
/// Envelope-encrypts secrets at rest — currently TenantPaymentCredential.SecretKey/WebhookSecret.
/// Backed by ASP.NET Core Data Protection (AES-256-CBC + HMAC authenticated encryption); see
/// DataProtectionSecretProtector for the concrete implementation and key-ring storage.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string ciphertext);
}
```

- [ ] **Step 2: Implement it**

Create `src/backend-api/ApexBooking.Infrastructure/ExternalServices/DataProtection/DataProtectionSecretProtector.cs`:

```csharp
using ApexBooking.Core.Domain.Services;
using Microsoft.AspNetCore.DataProtection;

namespace ApexBooking.Infrastructure.ExternalServices.DataProtection;

public sealed class DataProtectionSecretProtector : ISecretProtector
{
    // Distinct purpose string isolates this key derivation from every other Data Protection
    // consumer in the app (e.g. Identity's PasswordResetTokenProvider/EmailVerificationTokenProvider,
    // which already use the ambient IDataProtectionProvider under their own purposes) — a ciphertext
    // produced under one purpose cannot be unprotected under another.
    private const string Purpose = "ApexBooking.TenantPaymentCredentials.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
```

- [ ] **Step 3: Register it**

In `src/backend-api/ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs`, add the new using (it doesn't have one for this sub-namespace yet — it does already have `using ApexBooking.Core.Domain.Services;` on line 2, which covers `ISecretProtector` unqualified):

```csharp
using ApexBooking.Infrastructure.ExternalServices.DataProtection;
```

Then add, near the other singletons (e.g. next to `service.AddSingleton<ITicketTokenService, HmacTicketTokenService>();`):

```csharp
            service.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Infrastructure/ApexBooking.Infrastructure.csproj`
Expected: Build succeeded, 0 errors. (`IDataProtectionProvider`/`IDataProtector` resolve via the existing `FrameworkReference Include="Microsoft.AspNetCore.App"` — no new package needed here.)

- [ ] **Step 5: Leave changes uncommitted.**

---

### Task 9: Persist the Data Protection key ring in SQL Server

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
- Modify: `src/backend-api/ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs`
- Modify: `src/backend-api/ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs`
- Migration (generate only, do not apply): `src/backend-api/ApexBooking.Core.Persistence/Migrations/`

**Interfaces:**
- Consumes: `ISecretProtector` (Task 8) — injected into `ApexBookingDbContext` for Task 10's use, not this task's own DataProtection wiring.
- Produces: `ApexBookingDbContext.DataProtectionKeys : DbSet<DataProtectionKey>` (via `IDataProtectionKeyContext`).

Note on why `ISecretProtector` (not `IDataProtectionProvider`, and not the concrete `DataProtectionSecretProtector`) is what `ApexBookingDbContext` takes: `Core.Persistence` already depends on `Core.Domain` (where `ISecretProtector` lives) but must never depend on `Infrastructure` (that's backwards — `Infrastructure` depends on `Core.Domain`, not the other way around). The concrete `DataProtectionSecretProtector` is registered against `ISecretProtector` in `InfrastructureDependencies.cs`; the DI container resolves it into the DbContext's constructor at request time regardless of which `Add*Services()` call ran first in `Program.cs`.

- [ ] **Step 1: Add the package**

In `src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`, add to the existing package `ItemGroup`:

```xml
    <PackageReference Include="Microsoft.AspNetCore.DataProtection.EntityFrameworkCore" Version="10.0.9" />
```

- [ ] **Step 2: Wire the DbContext**

In `src/backend-api/ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs`:

Add usings:
```csharp
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Persistence.Mappings;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
```

Change the class declaration and constructor from:
```csharp
    public class ApexBookingDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        private readonly ITenantEntity _tenantEntity;
        private readonly ILogger<ApexBookingDbContext> _logger;
```
to:
```csharp
    public class ApexBookingDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
    {
        private readonly ITenantEntity _tenantEntity;
        private readonly ILogger<ApexBookingDbContext> _logger;
        private readonly ISecretProtector _secretProtector;
```

Add the new `DbSet` alongside the other platform entities (the `using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;` added above makes `DataProtectionKey` resolve unqualified):
```csharp
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
```

Change the constructor from:
```csharp
        public ApexBookingDbContext(DbContextOptions<ApexBookingDbContext> options, ITenantEntity tenantEntity)
           : base(options)
        {
            _tenantEntity = tenantEntity;
        }
```
to:
```csharp
        public ApexBookingDbContext(DbContextOptions<ApexBookingDbContext> options, ITenantEntity tenantEntity, ISecretProtector secretProtector)
           : base(options)
        {
            _tenantEntity = tenantEntity;
            _secretProtector = secretProtector;
        }
```

Change `OnModelCreating`'s `builder.ApplyConfigurationsFromAssembly(typeof(ApexBookingDbContext).Assembly);` line to:
```csharp
            // TenantPaymentCredentialConfiguration needs a constructor-injected ISecretProtector,
            // which the parameterless-constructor assembly scan below can't provide — excluded from
            // the scan and applied explicitly instead. Every other IEntityTypeConfiguration<> in
            // this assembly is unaffected.
            builder.ApplyConfigurationsFromAssembly(typeof(ApexBookingDbContext).Assembly,
                t => t != typeof(TenantPaymentCredentialConfiguration));
            builder.ApplyConfiguration(new TenantPaymentCredentialConfiguration(_secretProtector));
```

(This line sits just before `ApplyGlobalFilters(builder);` at the end of `OnModelCreating`, in the same place the original single-line call was.)

- [ ] **Step 3: Register `PersistKeysToDbContext`**

In `src/backend-api/ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs`, add (near the top of `AddInfrastructureService`, after `service.AddHttpContextAccessor();`):

```csharp
            // Persists the Data Protection key ring into SQL Server via ApexBookingDbContext,
            // instead of the default local-disk key store — required for correctness once this app
            // runs on more than one instance, and for TenantPaymentCredential encryption (see
            // ISecretProtector) to survive a redeploy. AddIdentity() (AuthenticationExtensions.cs)
            // already implicitly calls AddDataProtection() for its own token providers; this call
            // reconfigures that same registration's key-storage mechanism — AddDataProtection() is
            // safe to call more than once.
            service.AddDataProtection()
                .PersistKeysToDbContext<ApexBooking.Core.Persistence.Data.ApexBookingDbContext>()
                .SetApplicationName("ApexBooking");
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj`
Expected: Build succeeded, 0 errors. (Building the WebApi project exercises the full dependency graph, including the new `IDataProtectionKeyContext` implementation and the `PersistKeysToDbContext` call — building `Core.Persistence`/`Infrastructure` alone would miss issues that only show up once everything is wired together.)

- [ ] **Step 5: Generate the migration (do NOT apply it)**

Run:
```bash
dotnet ef migrations add AddDataProtectionKeys --project src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj --startup-project src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj
```
Expected: a new migration creating a `DataProtectionKeys` table (`Id`, `FriendlyName`, `Xml`). **Do not run `dotnet ef database update`.**

- [ ] **Step 6: Leave changes uncommitted.**

---

### Task 10: Encrypt `SecretKey`/`WebhookSecret` via a `ValueConverter`

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Persistence/Mappings/TenantPaymentCredentialConfiguration.cs`
- Migration (generate only, do not apply): `src/backend-api/ApexBooking.Core.Persistence/Migrations/`

**Interfaces:**
- Consumes: `ISecretProtector` (Task 8), constructor-injected per Task 9's `OnModelCreating` change.

- [ ] **Step 1: Add the converter**

Replace the contents of `src/backend-api/ApexBooking.Core.Persistence/Mappings/TenantPaymentCredentialConfiguration.cs` with:

```csharp
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ApexBooking.Core.Persistence.Mappings;

public class TenantPaymentCredentialConfiguration : IEntityTypeConfiguration<TenantPaymentCredential>
{
    private readonly ISecretProtector _secretProtector;

    public TenantPaymentCredentialConfiguration(ISecretProtector secretProtector)
    {
        _secretProtector = secretProtector;
    }

    public void Configure(EntityTypeBuilder<TenantPaymentCredential> builder)
    {
        builder.ToTable("TenantPaymentCredentials");
        builder.HasKey(c => c.TenantPaymentCredentialId);
        builder.Property(c => c.TenantPaymentCredentialId)
            .HasConversion(
                id => id.Value,
                value => new TenantPaymentCredentialId(value))
            .IsRequired();

        // SecretKey/WebhookSecret are encrypted at rest (ISecretProtector, backed by ASP.NET Core
        // Data Protection — AES-256-CBC + HMAC authenticated encryption). PublicKey stays plaintext:
        // it's the publishable key, safe to display/expose. Max length raised from 500 to 1000 —
        // Data Protection ciphertext (base64 + key-id header + auth tag) runs meaningfully larger
        // than the ~70-char raw PayMongo keys.
        var secretConverter = new ValueConverter<string, string>(
            plaintext => _secretProtector.Protect(plaintext),
            ciphertext => _secretProtector.Unprotect(ciphertext));

        var nullableSecretConverter = new ValueConverter<string?, string?>(
            plaintext => plaintext == null ? null : _secretProtector.Protect(plaintext),
            ciphertext => ciphertext == null ? null : _secretProtector.Unprotect(ciphertext));

        builder.Property(c => c.SecretKey)
            .HasConversion(secretConverter)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(c => c.PublicKey).HasMaxLength(500).IsRequired();

        builder.Property(c => c.WebhookSecret)
            .HasConversion(nullableSecretConverter)
            .HasMaxLength(1000);

        builder.Property(c => c.IsEnabled).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Generate the migration (do NOT apply it)**

Run:
```bash
dotnet ef migrations add EncryptTenantPaymentCredentials --project src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj --startup-project src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj
```
Expected: a migration altering `TenantPaymentCredentials.SecretKey`/`WebhookSecret` column length `500 → 1000` (the `ValueConverter` itself is a read/write-time transform, not a schema change — this migration should contain only the two `AlterColumn` calls). **Do not run `dotnet ef database update`** — per the Global Constraints, there is no existing plaintext data to migrate (dev-only, no real tenants).

- [ ] **Step 4: Leave changes uncommitted.**

---

## Module 3 — Webhook idempotency & safe logging

### Task 11: Capture the PayMongo event id

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Domain/Services/Paymongo/PaymongoContracts.cs`
- Modify: `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommand.cs`

**Interfaces:**
- Produces: `WebhookData.Id` (string), `ProcessPaymentWebhookCommand.PayMongoEventId` (string).

- [ ] **Step 1: Add `Id` to `WebhookData`**

In `src/backend-api/ApexBooking.Core.Domain/Services/Paymongo/PaymongoContracts.cs`, change:

```csharp
    public class WebhookData
    {
        [JsonPropertyName("attributes")]
        public WebhookAttributes Attributes { get; set; } = new();
    }
```

to:

```csharp
    public class WebhookData
    {
        // PayMongo's own Event resource id (e.g. "evt_..."), used as the idempotency ledger key
        // (see ProcessedPaymentEvent). Distinct from WebhookResource.Id below, which is the nested
        // Link/Payment resource's own id, not the delivery envelope's.
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public WebhookAttributes Attributes { get; set; } = new();
    }
```

- [ ] **Step 2: Add the field to the command**

Replace `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommand.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ProcessPaymentWebhook
{
    public record ProcessPaymentWebhookCommand(
        string RemarksToken,
        string? PayMongoPaymentId,
        string RawBody,
        string? SignatureHeader,
        string PayMongoEventId) : ICommand;
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: build FAILS — `PayMongoWebhooksController.cs` still constructs the old 4-argument record. This is expected; Task 16 fixes the call site. Confirm the only error is that call site (no other unexpected breakage).

- [ ] **Step 4: Leave changes uncommitted.**

---

### Task 12: Signature timestamp extraction

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Domain/Services/Paymongo/IPayMongoWebhookSignatureVerifier.cs`
- Modify: `src/backend-api/ApexBooking.Infrastructure/ExternalServices/PayMongo/PayMongoWebhookSignatureVerifier.cs`

**Interfaces:**
- Produces: `IPayMongoWebhookSignatureVerifier.TryGetTimestamp(string? signatureHeader, out DateTimeOffset timestamp) : bool`.

**Assumption to verify manually:** PayMongo's `t=` value is assumed to be Unix epoch seconds (matching the Stripe-derived `t=...,te=...,li=...` signature scheme this verifier already parses). Confirm against a real sandbox webhook capture during the spec's "Signature-age rejection" manual verification step — if it turns out to be milliseconds or an ISO-8601 string instead, adjust `TryGetTimestamp`'s parsing accordingly.

- [ ] **Step 1: Extend the interface**

In `src/backend-api/ApexBooking.Core.Domain/Services/Paymongo/IPayMongoWebhookSignatureVerifier.cs`:

```csharp
using System;

namespace ApexBooking.Core.Domain.Services.Paymongo
{
    public interface IPayMongoWebhookSignatureVerifier
    {
        /// <summary>
        /// Verifies a PayMongo webhook request against the tenant's own webhook signing secret.
        /// </summary>
        /// <param name="rawBody">The exact, unmodified request body bytes PayMongo signed.</param>
        /// <param name="signatureHeader">The raw "Paymongo-Signature" header value.</param>
        /// <param name="webhookSecret">The tenant's whsk_... signing secret.</param>
        /// <param name="useLiveMode">Whether to check the live-mode signature segment (li=) instead of test-mode (te=).</param>
        bool Verify(string rawBody, string? signatureHeader, string webhookSecret, bool useLiveMode);

        /// <summary>
        /// Extracts the "t=" timestamp segment from the signature header, independent of whether
        /// the signature itself verifies — used to reject stale/replayed deliveries.
        /// </summary>
        bool TryGetTimestamp(string? signatureHeader, out DateTimeOffset timestamp);
    }
}
```

- [ ] **Step 2: Implement it**

In `src/backend-api/ApexBooking.Infrastructure/ExternalServices/PayMongo/PayMongoWebhookSignatureVerifier.cs`, add this method to the `PayMongoWebhookSignatureVerifier` class (after `Verify`):

```csharp
        public bool TryGetTimestamp(string? signatureHeader, out DateTimeOffset timestamp)
        {
            timestamp = default;

            if (string.IsNullOrWhiteSpace(signatureHeader))
                return false;

            var parts = ParseSignatureHeader(signatureHeader);
            if (!parts.TryGetValue("t", out var raw) || !long.TryParse(raw, out var unixSeconds))
                return false;

            timestamp = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return true;
        }
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Infrastructure/ApexBooking.Infrastructure.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Leave changes uncommitted.**

---

### Task 13: `ProcessedPaymentEvent` ledger entity + mapping + migration

**Files:**
- Create: `src/backend-api/ApexBooking.Core.Domain/Entities/ProcessedPaymentEvent.cs`
- Create: `src/backend-api/ApexBooking.Core.Persistence/Mappings/ProcessedPaymentEventConfiguration.cs`
- Modify: `src/backend-api/ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs`
- Migration (generate only, do not apply): `src/backend-api/ApexBooking.Core.Persistence/Migrations/`

**Interfaces:**
- Produces: `ProcessedPaymentEvent.Create(string payMongoEventId, Guid tenantId, Guid bookingId) : ProcessedPaymentEvent`.
- Consumed by: Task 14 (`ProcessedPaymentEventStore`), Task 15 (handler).

- [ ] **Step 1: Write the entity**

Create `src/backend-api/ApexBooking.Core.Domain/Entities/ProcessedPaymentEvent.cs`:

```csharp
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.Entities;

/// <summary>
/// Idempotency ledger for PayMongo webhook deliveries — a persistence record, not a domain
/// aggregate (no IAggregateRoot, no repository; same pattern as OutboxMessage/SmsUsage).
/// PayMongoEventId carries a unique DB constraint (ProcessedPaymentEventConfiguration) as a
/// last-resort safety net against a same-event double-delivery race slipping past the
/// application-level existence check in ProcessPaymentWebhookCommandHandler.
/// </summary>
public class ProcessedPaymentEvent
{
    public Guid Id { get; private set; }
    public string PayMongoEventId { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public Guid BookingId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    protected ProcessedPaymentEvent() { }

    private ProcessedPaymentEvent(string payMongoEventId, Guid tenantId, Guid bookingId)
    {
        Id = Guid.NewGuid();
        PayMongoEventId = payMongoEventId;
        TenantId = tenantId;
        BookingId = bookingId;
        ProcessedAt = DateTime.UtcNow;
    }

    public static ProcessedPaymentEvent Create(string payMongoEventId, Guid tenantId, Guid bookingId)
    {
        if (string.IsNullOrWhiteSpace(payMongoEventId))
            throw new BusinessRuleBrokenException("PayMongo event id is required to record a processed payment event.");

        return new ProcessedPaymentEvent(payMongoEventId, tenantId, bookingId);
    }
}
```

- [ ] **Step 2: Write the mapping**

Create `src/backend-api/ApexBooking.Core.Persistence/Mappings/ProcessedPaymentEventConfiguration.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings;

public class ProcessedPaymentEventConfiguration : IEntityTypeConfiguration<ProcessedPaymentEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedPaymentEvent> builder)
    {
        builder.ToTable("processed_payment_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PayMongoEventId).HasColumnName("paymongo_event_id").HasMaxLength(100).IsRequired();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at").IsRequired();

        // Idempotency safety net: even if two deliveries of the same event somehow race past
        // IProcessedPaymentEventStore.ExistsAsync's check, only one insert can win here — the
        // loser's SaveChanges throws, PayMongo retries, and the retry's ExistsAsync check then
        // finds the winner's row and returns the clean no-op path.
        builder.HasIndex(e => e.PayMongoEventId).IsUnique();
    }
}
```

- [ ] **Step 3: Add the `DbSet`**

In `src/backend-api/ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs`, add alongside `RefundRequests`:

```csharp
        public DbSet<ProcessedPaymentEvent> ProcessedPaymentEvents => Set<ProcessedPaymentEvent>();
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Generate the migration (do NOT apply it)**

Run:
```bash
dotnet ef migrations add AddProcessedPaymentEvents --project src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj --startup-project src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj
```
Expected: a migration creating table `processed_payment_events` with a unique index on `paymongo_event_id`. **Do not run `dotnet ef database update`.**

- [ ] **Step 6: Leave changes uncommitted.**

---

### Task 14: `IProcessedPaymentEventStore` port + adapter

**Files:**
- Create: `src/backend-api/ApexBooking.Core.Domain/Services/IProcessedPaymentEventStore.cs`
- Create: `src/backend-api/ApexBooking.Core.Persistence/Services/ProcessedPaymentEventStore.cs`
- Modify: `src/backend-api/ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs`

**Interfaces:**
- Produces: `IProcessedPaymentEventStore.ExistsAsync(string, CancellationToken) : Task<bool>`, `IProcessedPaymentEventStore.Add(ProcessedPaymentEvent) : void`.
- Consumed by: Task 15 (handler).

- [ ] **Step 1: Define the port**

Create `src/backend-api/ApexBooking.Core.Domain/Services/IProcessedPaymentEventStore.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Domain.Services;

// Data access port for the ProcessedPaymentEvent idempotency ledger — same shape as
// IRefundRequestStore: ProcessedPaymentEvent is not an IAggregateRoot, so there's no generic
// repository for it.
public interface IProcessedPaymentEventStore
{
    Task<bool> ExistsAsync(string payMongoEventId, CancellationToken cancellationToken = default);

    // Tracks a new ledger row on the ambient DbContext WITHOUT saving — deliberately NOT symmetric
    // with IRefundRequestStore.AddAsync (which calls SaveChangesAsync immediately). The caller
    // must persist this in the SAME IUnitOfWork.CompleteAsync() call that saves the booking's
    // payment-confirmation write (see ProcessPaymentWebhookCommandHandler), so the two commit in
    // one transaction — never mark an event processed before the payment state change it belongs
    // to has actually been saved.
    void Add(ProcessedPaymentEvent processedPaymentEvent);
}
```

- [ ] **Step 2: Implement it**

Create `src/backend-api/ApexBooking.Core.Persistence/Services/ProcessedPaymentEventStore.cs`:

```csharp
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Services;

public sealed class ProcessedPaymentEventStore : IProcessedPaymentEventStore
{
    private readonly ApexBookingDbContext _context;

    public ProcessedPaymentEventStore(ApexBookingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string payMongoEventId, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessedPaymentEvents
            .AnyAsync(e => e.PayMongoEventId == payMongoEventId, cancellationToken);
    }

    public void Add(ProcessedPaymentEvent processedPaymentEvent)
    {
        _context.ProcessedPaymentEvents.Add(processedPaymentEvent);
    }
}
```

- [ ] **Step 3: Register it**

In `src/backend-api/ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs`, add next to `IRefundRequestStore`:

```csharp
            services.AddScoped<IProcessedPaymentEventStore, Services.ProcessedPaymentEventStore>();
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Leave changes uncommitted.**

---

### Task 15: Rewrite `ProcessPaymentWebhookCommandHandler`

**Files:**
- Modify: `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommandHandler.cs`

**Interfaces:**
- Consumes: `IProcessedPaymentEventStore` (Task 14), `IPayMongoWebhookSignatureVerifier.TryGetTimestamp` (Task 12), `ProcessedPaymentEvent.Create` (Task 13), `ProcessPaymentWebhookCommand.PayMongoEventId` (Task 11).

- [ ] **Step 1: Replace the handler**

Replace `src/backend-api/ApexBooking.Core.Application/Features/Bookings/Commands/ProcessPaymentWebhook/ProcessPaymentWebhookCommandHandler.cs`:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Paymongo;
using ApexBooking.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ProcessPaymentWebhook
{
    public class ProcessPaymentWebhookCommandHandler : ICommandHandler<ProcessPaymentWebhookCommand>
    {
        private static readonly TimeSpan MaxSignatureAge = TimeSpan.FromMinutes(5);

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayMongoWebhookSignatureVerifier _signatureVerifier;
        private readonly IProcessedPaymentEventStore _processedPaymentEventStore;
        private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;

        public ProcessPaymentWebhookCommandHandler(
            IUnitOfWork unitOfWork,
            IPayMongoWebhookSignatureVerifier signatureVerifier,
            IProcessedPaymentEventStore processedPaymentEventStore,
            ILogger<ProcessPaymentWebhookCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signatureVerifier = signatureVerifier;
            _processedPaymentEventStore = processedPaymentEventStore;
            _logger = logger;
        }

        public async Task Handle(ProcessPaymentWebhookCommand command, CancellationToken cancellationToken)
        {
            // 1. Invariant Guard Check: Verify this webhook resource belongs to our booking system format
            if (string.IsNullOrWhiteSpace(command.RemarksToken) || !command.RemarksToken.StartsWith("BOOKING_"))
                return; // Soft ignore if it's an unrelated payment resource or system event trace

            // 2. Extract the primitive C# Guid out from our custom string tracker.
            // Accepts both "BOOKING_{bookingId}" (legacy) and "BOOKING_{bookingId}_{branchId}" —
            // the branch segment, when present, is informational only; the booking row is authoritative.
            var segments = command.RemarksToken["BOOKING_".Length..].Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || !Guid.TryParse(segments[0], out Guid targetBookingId))
                throw new BusinessRuleBrokenException("Invalid tracking token structure detected inside incoming PayMongo webhook metadata.");

            // 3. Single Database Load: resolve the owning tenant from the booking id itself — never
            // trust a tenant id supplied directly by the payload.
            var tenant = await _unitOfWork.TenantRepository.GetByBookingIdAsync(targetBookingId, cancellationToken);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Payment confirmation failed. Target appointment record context could not be located inside our system database ledgers.");

            // 3b. Reject anything that isn't actually signed by this tenant's own PayMongo webhook
            // secret — up to this point, RemarksToken/RawBody are attacker-controlled input; nothing
            // above is safe to act on without this check passing first.
            if (tenant.PaymentCredential?.WebhookSecret is not { } webhookSecret)
                throw new BusinessRuleBrokenException("Payment confirmation failed. No webhook signing secret is configured for this business workspace.");

            var isLiveMode = tenant.PaymentCredential.SecretKey.StartsWith("sk_live_");
            if (!_signatureVerifier.Verify(command.RawBody, command.SignatureHeader, webhookSecret, isLiveMode))
                throw new BusinessRuleBrokenException("Payment confirmation failed. Webhook signature verification failed.");

            // 3c. Reject stale deliveries — a replayed or clock-skewed signature older than 5
            // minutes is rejected even though it's cryptographically valid. Checked after Verify()
            // (not before) because the timestamp only means anything once we know the signature
            // wasn't forged.
            if (!_signatureVerifier.TryGetTimestamp(command.SignatureHeader, out var signedAt) ||
                DateTimeOffset.UtcNow - signedAt > MaxSignatureAge)
                throw new BusinessRuleBrokenException("Payment confirmation failed. Webhook signature timestamp is missing or too old.");

            // 4. Idempotency check — a redelivery of an event we've already processed is a clean
            // no-op, not an error: log and return successfully so the controller keeps returning
            // HTTP 200 and PayMongo stops retrying.
            if (string.IsNullOrWhiteSpace(command.PayMongoEventId))
                throw new BusinessRuleBrokenException("Payment confirmation failed. Missing PayMongo event id.");

            if (await _processedPaymentEventStore.ExistsAsync(command.PayMongoEventId, cancellationToken))
            {
                _logger.LogInformation(
                    "PayMongo event {PayMongoEventId} for booking {BookingId} was already processed; skipping.",
                    command.PayMongoEventId, targetBookingId);
                return;
            }

            // 5. Extract the child Booking entity node out from the parent aggregate graph tree
            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == targetBookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment details missing inside parent aggregate boundary graph lines.");

            // 6. Invoke the domain state machine — PendingPayment -> Scheduled.
            booking.ConfirmPayment(PaymentConfirmationMethod.Online, command.PayMongoPaymentId);

            // 7. Track the ledger row on the SAME DbContext, WITHOUT saving yet (see
            // IProcessedPaymentEventStore.Add's doc comment) — it must commit in the same
            // transaction as the booking status change, never before it.
            _processedPaymentEventStore.Add(ProcessedPaymentEvent.Create(
                command.PayMongoEventId, tenant.TenantId.Value, booking.BookingId.Value));

            _unitOfWork.TenantRepository.Update(tenant);

            // 8. One SaveChangesAsync call commits the booking's payment confirmation and the
            // idempotency ledger row atomically.
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Leave changes uncommitted.**

---

### Task 16: Controller — pass the event id, stop logging raw payloads

**Files:**
- Modify: `src/backend-api/ApexBooking.WebApi/Controllers/PayMongoWebhooksController.cs`

**Interfaces:**
- Consumes: `ProcessPaymentWebhookCommand` 5-arg constructor (Task 11).

- [ ] **Step 1: Replace the file**

Replace `src/backend-api/ApexBooking.WebApi/Controllers/PayMongoWebhooksController.cs`:

```csharp
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.Bookings.Commands.ProcessPaymentWebhook;
using ApexBooking.Core.Domain.Services.Paymongo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/public/webhooks")]
    [AllowAnonymous]
    public class PayMongoWebhooksController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PayMongoWebhooksController> _logger;

        public PayMongoWebhooksController(IMediator mediator, ILogger<PayMongoWebhooksController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("paymongo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandlePayMongoCallback()
        {
            // 1. Read the raw text stream body message directly from the network payload frame
            using var reader = new StreamReader(Request.Body);
            var jsonText = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(jsonText))
                return BadRequest("Empty payload context.");

            try
            {
                // 2. Deserialize the payload utilizing standard loose naming policies
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var payload = JsonSerializer.Deserialize<PayMongoWebhookPayload>(jsonText, options);

                if (payload?.Data?.Attributes?.Data?.Attributes == null)
                    return BadRequest("Invalid or broken structural payload metadata schema received.");

                var resourceAttributes = payload.Data.Attributes.Data.Attributes;

                // 3. Verify that the payment transaction status registers as fully "paid" before moving forward
                if (resourceAttributes.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                {
                    // Raw body + signature header travel down to the handler, which verifies them
                    // against the tenant's own webhook secret before trusting anything above.
                    var signatureHeader = Request.Headers["Paymongo-Signature"].FirstOrDefault();
                    // The Payment resource's real id lives nested under the paid Link's own
                    // `payments` array, not at the link's top-level `id` (that's the Link's own
                    // id, e.g. "link_..." — confirmed via sandbox webhook capture 2026-08-11).
                    var payment = resourceAttributes.Payments.FirstOrDefault();
                    var payMongoPaymentId = payment?.Data.Id;
                    await _mediator.Send(new ProcessPaymentWebhookCommand(
                        resourceAttributes.Remarks, payMongoPaymentId, jsonText, signatureHeader, payload.Data.Id));
                }

                // 4. Return an immediate standard HTTP 200 OK success response back to PayMongo's servers.
                // This signals that your backend safely captured the event, preventing PayMongo from continually resending the alert.
                return Ok();
            }
            catch (Exception ex)
            {
                // DPA: never log the raw webhook payload (or any header) — it can carry
                // customer-identifying and payment-adjacent data. A SHA-256 hash plus a fresh
                // correlation id is enough to match this failure against PayMongo's own delivery
                // logs or a support ticket without persisting the content itself.
                var correlationId = Guid.NewGuid();
                var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(jsonText)));

                _logger.LogError(ex,
                    "Failed to process PayMongo webhook callback. CorrelationId: {CorrelationId}, BodySha256: {BodyHash}",
                    correlationId, bodyHash);

                // Fail safely: Deliver an HTTP 400 bad request indicator to trigger an automatic gateway fallback loop
                return BadRequest($"Failed to process payment callback event cleanly. Reference: {correlationId}");
            }
        }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj`
Expected: Build succeeded, 0 errors. This also resolves Task 11 Step 3's expected build failure.

- [ ] **Step 3: Full solution build**

Run: `dotnet build src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj` (already run above; if a solution file exists, prefer `dotnet build <solution>.sln` to catch any project this plan touched indirectly).
Expected: Build succeeded, 0 errors, 0 warnings introduced by this plan's changes.

- [ ] **Step 4: Run the full domain test suite**

Run: `dotnet test src/backend-api/ApexBooking.Core.Domain.UnitTests`
Expected: all tests PASS, including the two new `BookingExpirationTests` and every pre-existing test (confirms Tasks 1–3's changes didn't regress `BookingPaymentCaptureTests`, `BookingRefundTests`, etc.).

- [ ] **Step 5: Leave changes uncommitted.**

---

## Module 4 — Logging boundary & ToS boilerplate (docs only)

### Task 17: Logging boundary note + ToS boilerplate

**Files:**
- Create: `docs/compliance/2026-08-18-logging-boundary-and-tos-boilerplate.md`

No code, no build/test step — this is a documentation deliverable.

- [ ] **Step 1: Write the document**

Create `docs/compliance/2026-08-18-logging-boundary-and-tos-boilerplate.md` with two sections:

**Section A — Logging boundary**, stating explicitly:
- Never log the `Authorization` header, in full or in part, in any log statement, exception message, or `ILogger` scope.
- Never log a raw webhook request body. `PayMongoWebhooksController` (Task 16) now logs a SHA-256 hash of the body plus a correlation id on failure, never the body itself.
- This app has no request/response body-or-header-capturing logging middleware today (confirmed: `Program.cs` registers no `UseHttpLogging()` or equivalent) — the correct guidance is "don't add one that captures bodies/headers unfiltered," not "add one with a redaction allowlist." If body/header logging middleware is ever introduced later, it must set `HttpLoggingFields` to exclude `RequestHeaders`, `RequestBody`, and `ResponseBody` by default, and explicitly strip `Authorization` from whatever header set remains.
- `GlobalExceptionHandler` (referenced from `Program.cs`'s `AddExceptionHandler<GlobalExceptionHandler>()`) should be spot-checked as part of this task to confirm it doesn't echo request bodies or headers into its problem-details responses or logs — note the finding (or fix, if found) here.

**Section B — ToS boilerplate**, with these three sections, `[Company Legal Name]` used as the placeholder throughout:
1. **No Financial Custody / Technical Intermediary Status** — `[Company Legal Name]` never takes custody of customer funds; each tenant connects their own PayMongo merchant account and PayMongo, not `[Company Legal Name]`, is the entity that processes, settles, and holds payment funds. `[Company Legal Name]` provides the booking/scheduling software layer only.
2. **Indemnification of Billing Disputes/Refunds** — the tenant, not `[Company Legal Name]`, is responsible for billing disputes, chargebacks, and refund decisions arising from their use of their own connected PayMongo account; the tenant indemnifies `[Company Legal Name]` against claims arising from their pricing, refund policy, or service delivery.
3. **DPA 2012 Data Role Splits (PIC vs PIP)** — under the Philippines Data Privacy Act of 2012, each tenant is the Personal Information Controller (PIC) for their own customers' data; `[Company Legal Name]` acts as a Personal Information Processor (PIP), processing that data solely on the tenant's instructions to provide the booking/scheduling service, with data handling obligations (security, breach notification, deletion) flowing accordingly.

- [ ] **Step 2: Leave the file uncommitted.**

---

### Task 18: Manual verification checklist

**Files:** none — this task runs the app and checks behavior; it produces no code changes.

These are the spec's "Testing" bullets for everything that isn't covered by the domain unit tests in Task 1 — this codebase has no handler/controller/integration test suite to extend (only `Core.Domain.UnitTests`), so this is how the rest of the plan actually gets proven, not a substitute for it. **This task is deliberately not executed as part of this plan's run** — it requires the three generated migrations (Tasks 3, 9, 13) to actually be applied to a database, and per the Global Constraints this plan does not run `dotnet ef database update` against anything. Leave this checklist for you (the user) to run against your own local instance, on your own schedule, once you've reviewed the generated migrations and chosen to apply them.

- [ ] **Encryption round-trip:** configure credentials via `PUT /api/Tenant/payment-gateway` with a test `sk_test_...`/`pk_test_...` pair. Query the `TenantPaymentCredentials` table directly — `SecretKey` should be ciphertext (not starting with `sk_test_`), `PublicKey` should still be plaintext. Then run `InitiateBooking` for that tenant with a service requiring upfront payment and confirm `PayMongoService.CreatePaymentSourceAsync` receives the correct decrypted `sk_test_...` value (e.g. via a breakpoint or temporary log at the call site, removed afterward).
- [ ] **Concurrent booking:** fire two parallel `POST` requests to `InitiateBooking` for the same tenant/staff/date/time-slot combination (e.g. two terminal tabs with `curl`, or a small parallel test script). Confirm exactly one returns success and the other receives the "This time slot is no longer available" `BusinessRuleBrokenException` message — not two successful bookings, and not the lock-timeout message (which would indicate the lock held far longer than expected).
- [ ] **Webhook replay:** send the same simulated PayMongo webhook payload (same `data.id` event id) to `POST /api/public/webhooks/paymongo` twice in a row. Confirm the booking's `ConfirmPayment` side effects (status change, `PaymentCapturedDomainEvent`) happen exactly once, the second call still returns `200 OK`, and exactly one row exists in `processed_payment_events` for that event id.
- [ ] **Signature-age rejection:** send a webhook payload with a valid signature but a `t=` value more than 5 minutes in the past. Confirm it's rejected (400) and does not confirm payment. Use this same request to confirm the "assumption to verify manually" note in Task 12 — check the actual `t=` value format from a real sandbox capture, not just this synthetic test.
