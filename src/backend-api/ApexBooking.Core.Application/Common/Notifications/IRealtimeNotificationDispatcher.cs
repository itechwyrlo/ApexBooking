using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Common.Notifications
{
    /// <summary>
    /// Pushes already-persisted Notification rows to any connected clients via SignalR
    /// (NotificationHub). Call this AFTER IUnitOfWork.CompleteAsync succeeds — never before, since a
    /// push for data that didn't actually commit would be wrong. Non-fatal by design: a push
    /// failure never fails the caller's handler, since the notification already exists in the
    /// database and the client's next REST fetch (GetLatestNotifications) will still pick it up.
    /// </summary>
    public interface IRealtimeNotificationDispatcher
    {
        Task PushAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken);
    }
}
