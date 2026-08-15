using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Notifications.Queries.GetUnreadNotificationCount
{
    public class GetUnreadNotificationCountHandler : IQueryHandler<GetUnreadNotificationCountQuery, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;

        public GetUnreadNotificationCountHandler(IUnitOfWork unitOfWork, IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
        }

        public async Task<int> Handle(GetUnreadNotificationCountQuery query, CancellationToken cancellationToken)
        {
            var currentUserId = _userContext.GetCurrentUserId();
            return await _unitOfWork.NotificationRepository.GetUnreadCountAsync(currentUserId, cancellationToken);
        }
    }
}
