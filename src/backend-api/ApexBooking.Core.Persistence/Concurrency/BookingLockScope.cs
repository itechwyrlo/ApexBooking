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
