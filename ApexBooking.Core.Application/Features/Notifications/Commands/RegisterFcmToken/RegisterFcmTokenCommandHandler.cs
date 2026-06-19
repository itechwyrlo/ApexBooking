using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Notifications.Commands.RegisterFcmToken;

internal sealed class RegisterFcmTokenCommandHandler : ICommandHandler<RegisterFcmTokenCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _userContextService;

    public RegisterFcmTokenCommandHandler(IUnitOfWork unitOfWork, IUserContextService userContextService)
    {
        _unitOfWork = unitOfWork;
        _userContextService = userContextService;
    }

    public async Task Handle(RegisterFcmTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _userContextService.GetCurrentUserId();

        var existing = await _unitOfWork.FcmTokenRepository.GetByUserAndTokenAsync(userId, request.Token, cancellationToken);
        if (existing is not null)
            return;

        var token = FcmToken.Create(userId, request.Token);
        _unitOfWork.FcmTokenRepository.Add(token);
        await _unitOfWork.CompleteAsync(cancellationToken);
    }
}
