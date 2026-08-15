using ApexBooking.Core.Domain.Services;
using ApexBooking.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ApexBooking.Infrastructure.ExternalServices.Realtime;

public class SignalRNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendAsync(Guid recipientId, NotificationDto notification)
    {
        return _hubContext.Clients
            .Group($"user-{recipientId}")
            .SendAsync("ReceiveNotification", notification);
    }
}
