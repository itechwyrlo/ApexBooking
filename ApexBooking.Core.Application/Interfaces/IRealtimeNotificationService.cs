using ApexBooking.Core.Application.Dtos;

namespace ApexBooking.Core.Application.Interfaces;

public interface IRealtimeNotificationService
{
    Task SendAsync(Guid recipientId, NotificationDto notification);
}
