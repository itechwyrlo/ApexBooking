using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Notifications.Queries.GetLatestNotifications
{
    public class GetLatestNotificationsHandler
        : IQueryHandler<GetLatestNotificationsQuery, IReadOnlyCollection<NotificationSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;

        public GetLatestNotificationsHandler(IUnitOfWork unitOfWork, IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
        }

        public async Task<IReadOnlyCollection<NotificationSummary>> Handle(
            GetLatestNotificationsQuery query,
            CancellationToken cancellationToken)
        {
            var currentUserId = _userContext.GetCurrentUserId();

            var notifications = await _unitOfWork.NotificationRepository.GetLatestAsync(
                currentUserId, query.Limit, cancellationToken);

            return notifications
                .Select(n => new NotificationSummary(
                    n.NotificationId.Value,
                    n.EventType.ToString(),
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt))
                .ToList();
        }
    }
}
