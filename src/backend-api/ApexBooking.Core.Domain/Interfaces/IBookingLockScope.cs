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
