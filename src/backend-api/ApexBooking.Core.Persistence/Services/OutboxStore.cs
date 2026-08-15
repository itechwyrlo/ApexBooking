using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Services;

/// <summary>
/// Direct-DbContext implementation of IOutboxStore — OutboxMessage is not an IAggregateRoot, so
/// this follows the same shape as SmsQuotaService rather than the generic repository pattern.
/// </summary>
public sealed class OutboxStore : IOutboxStore
{
    private readonly ApexBookingDbContext _context;

    public OutboxStore(ApexBookingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(batchSize)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<OutboxMessage?> TryClaimAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Conditional update, same technique as ISmsQuotaService.TryConsumeAsync's atomic
        // increment: if another caller (the immediate trigger vs. the recurring sweep) already
        // claimed this row first, zero rows match and we lose the race cleanly.
        var claimed = await _context.OutboxMessages
            .Where(m => m.Id == id && m.Status == OutboxMessageStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, OutboxMessageStatus.Processing), cancellationToken);

        if (claimed == 0)
            return null;

        return await _context.OutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync([id], cancellationToken);
        if (message is null)
            return;

        message.MarkProcessed();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync([id], cancellationToken);
        if (message is null)
            return;

        message.MarkFailed(error);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetFailedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Failed)
            .OrderByDescending(m => m.OccurredAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RetryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync([id], cancellationToken);
        if (message is null)
            return false;

        if (!message.TryRetry())
            return false;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
