using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Notifications.Commands.MarkAllNotificationsRead
{
    public class MarkAllNotificationsReadHandler : ICommandHandler<MarkAllNotificationsReadCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;

        public MarkAllNotificationsReadHandler(IUnitOfWork unitOfWork, IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
        }

        public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _userContext.GetCurrentUserId();
            await _unitOfWork.NotificationRepository.MarkAllReadAsync(currentUserId, cancellationToken);
        }
    }
}
