using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Notifications.Commands.UnregisterFcmToken;

internal sealed class UnregisterFcmTokenCommandHandler : ICommandHandler<UnregisterFcmTokenCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _userContextService;

    public UnregisterFcmTokenCommandHandler(IUnitOfWork unitOfWork, IUserContextService userContextService)
    {
        _unitOfWork = unitOfWork;
        _userContextService = userContextService;
    }

    public async Task Handle(UnregisterFcmTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _userContextService.GetCurrentUserId();
        await _unitOfWork.FcmTokenRepository.DeleteByUserIdAsync(userId, request.Token, cancellationToken);
        await _unitOfWork.CompleteAsync(cancellationToken);
    }
}
