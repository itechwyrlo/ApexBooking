using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Notifications.Queries.GetLatestNotifications
{
    public record NotificationSummary(
        Guid Id,
        string EventType,
        string Title,
        string Message,
        bool IsRead,
        DateTime CreatedAt
    );

    public record GetLatestNotificationsQuery(int Limit = 20) : IQuery<IReadOnlyCollection<NotificationSummary>>;
}
