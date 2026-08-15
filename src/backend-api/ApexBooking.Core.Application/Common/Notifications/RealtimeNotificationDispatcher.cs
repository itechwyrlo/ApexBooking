using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Common.Notifications
{
    public sealed class RealtimeNotificationDispatcher : IRealtimeNotificationDispatcher
    {
        private readonly IRealtimeNotificationService _realtimeService;
        private readonly ILogger<RealtimeNotificationDispatcher> _logger;

        public RealtimeNotificationDispatcher(
            IRealtimeNotificationService realtimeService,
            ILogger<RealtimeNotificationDispatcher> logger)
        {
            _realtimeService = realtimeService;
            _logger = logger;
        }

        public async Task PushAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken)
        {
            foreach (var n in notifications)
            {
                try
                {
                    await _realtimeService.SendAsync(n.RecipientId, n.ToDto());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Realtime push failed for notification {NotificationId}; recipient will still see it on next fetch.",
                        n.NotificationId.Value);
                }
            }
        }
    }
}
