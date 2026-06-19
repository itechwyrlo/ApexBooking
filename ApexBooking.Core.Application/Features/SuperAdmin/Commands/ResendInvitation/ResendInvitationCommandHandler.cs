using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ResendInvitation;

internal sealed class ResendInvitationCommandHandler : ICommandHandler<ResendInvitationCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthNotificationService _authNotification;
    private readonly IAppUrlService _appUrlService;

    public ResendInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IAuthNotificationService authNotification,
        IAppUrlService appUrlService)
    {
        _unitOfWork = unitOfWork;
        _authNotification = authNotification;
        _appUrlService = appUrlService;
    }

    public async Task Handle(ResendInvitationCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(command.TenantSlug);
        if (tenant is null)
            throw new NotFoundException("Organization not found.");

        var user = await _unitOfWork.UserRepository.GetAllByTenantAsync(tenant.TenantId)
            .ContinueWith(t => t.Result.FirstOrDefault(u => u.Id == command.UserId));

        if (user is null)
            throw new NotFoundException("User not found in this organization.");

        user.EnsureNotYetActivated();

        var setupToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
            user, "PasswordReset", "ResetPassword");

        user.SetInvitationToken(setupToken, DateTime.UtcNow.AddHours(72));
        await _unitOfWork.CompleteAsync(cancellationToken);

        var setupUrl = _appUrlService.GetSetupAccountUrl(setupToken);

        await _authNotification.SendInvitationResentEmailAsync(
            user.Email!,
            user.FullName,
            tenant.BusinessName,
            setupUrl,
            cancellationToken);
    }
}
