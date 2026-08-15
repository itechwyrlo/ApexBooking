namespace ApexBooking.Core.Domain.Services;

public sealed record NotificationDto(
    Guid NotificationId,
    string EventType,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);
