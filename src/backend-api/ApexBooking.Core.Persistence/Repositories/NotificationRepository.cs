using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(ApexBookingDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Notification>> GetLatestAsync(
        Guid recipientId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Notification>()
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Notification>()
            .CountAsync(n => n.RecipientId == recipientId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAllReadAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        await Context.Set<Notification>()
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow),
            cancellationToken);
    }
}
