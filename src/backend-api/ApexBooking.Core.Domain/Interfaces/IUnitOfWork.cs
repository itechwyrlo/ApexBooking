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

    // Same as CompleteAsync, but tolerates a unique-constraint violation as a benign "someone else
    // already committed this exact row first" race rather than propagating it — returns false
    // instead of throwing. For callers guarding an idempotency ledger (e.g. ProcessedPaymentEvent)
    // where a concurrent duplicate delivery can lose a save-time race against the very existence
    // check that already passed (TOCTOU): both callers pass the check, only one write wins, the
    // loser should treat that as "already processed", not as an error. Keeps ADO.NET/EF exception
    // types out of Core.Application — see UnitOfWork's implementation for the actual SqlException
    // classification (same pattern as AcquireBookingLockAsync's own SqlException handling).
    Task<bool> TryCompleteAsync(CancellationToken cancellationToken = default);

    // Acquires a SQL Server sp_getapplock ('Exclusive', LockOwner='Transaction') scoped to
    // resourceKey, inside a transaction on this UnitOfWork's own DbContext connection. The caller
    // must perform all of its reads/writes for the critical section through this same
    // IUnitOfWork/its repositories (NOT a second DbContext) and must call CompleteAsync() — which
    // joins this same transaction automatically — before calling CommitAsync() on the returned
    // scope. See docs/superpowers/specs/2026-08-18-payment-booking-security-hardening-design.md,
    // Module 2, for why a separate connection/transaction would make the lock ineffective.
    Task<IBookingLockScope> AcquireBookingLockAsync(string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default);
}