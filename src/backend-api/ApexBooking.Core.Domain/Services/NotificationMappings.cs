namespace ApexBooking.Core.Domain.Services;

public static class NotificationMappings
{
    // Fully qualified, not `using Entities` — this namespace has a sibling/child namespace also
    // named "Notification" (Services.Notification.Auth, .Bookings, .Tenancy), so the bare
    // identifier "Notification" resolves to that namespace before it'd ever reach the entity type.
    public static NotificationDto ToDto(this ApexBooking.Core.Domain.Entities.Notification notification)
    {
        return new NotificationDto(
            notification.NotificationId.Value,
            notification.EventType.ToString(),
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt);
    }
}
