using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetLatestAsync(Guid recipientId, int limit, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid recipientId, CancellationToken cancellationToken = default);
}
