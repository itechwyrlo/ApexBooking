namespace ApexBooking.Core.Domain.Services;

public interface IRealtimeNotificationService
{
    Task SendAsync(Guid recipientId, NotificationDto notification);
}
