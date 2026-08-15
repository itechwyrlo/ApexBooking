using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Notifications.Queries.GetUnreadNotificationCount
{
    public record GetUnreadNotificationCountQuery() : IQuery<int>;
}
