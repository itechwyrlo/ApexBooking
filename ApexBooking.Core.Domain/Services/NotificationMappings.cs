using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.mapper;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification notification)
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
