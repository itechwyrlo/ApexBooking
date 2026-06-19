namespace ApexBooking.Core.Application.Dtos;

public sealed record NotificationSummaryDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount
);
