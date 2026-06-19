using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Notifications.Queries.GetNotifications;

internal sealed class GetNotificationsQueryHandler
    : IQueryHandler<GetNotificationsQuery, NotificationSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public GetNotificationsQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task<NotificationSummaryDto> Handle(
        GetNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        var recipientId = _contextService.GetCurrentUserId();

        var notifications = await _unitOfWork.NotificationRepository
            .GetLatestAsync(recipientId, 10, cancellationToken);

        var unreadCount = await _unitOfWork.NotificationRepository
            .GetUnreadCountAsync(recipientId, cancellationToken);

        return new NotificationSummaryDto(
            notifications.Select(n => n.ToDto()).ToList().AsReadOnly(),
            unreadCount);
    }
}
