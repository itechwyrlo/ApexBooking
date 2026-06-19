using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Notifications.Commands.MarkAllRead;

internal sealed class MarkAllReadCommandHandler : ICommandHandler<MarkAllReadCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public MarkAllReadCommandHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task Handle(MarkAllReadCommand request, CancellationToken cancellationToken)
    {
        var recipientId = _contextService.GetCurrentUserId();
        await _unitOfWork.NotificationRepository.MarkAllReadAsync(recipientId, cancellationToken);
    }
}
