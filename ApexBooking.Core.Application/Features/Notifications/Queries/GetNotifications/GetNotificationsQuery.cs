using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery() : IQuery<NotificationSummaryDto>;
