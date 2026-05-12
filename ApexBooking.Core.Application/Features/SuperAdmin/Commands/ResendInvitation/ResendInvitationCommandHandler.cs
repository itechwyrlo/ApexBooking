using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ResendInvitation;

internal sealed class ResendInvitationCommandHandler
    : ICommandHandler<ResendInvitationCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notification;
    private readonly IAppUrlService _appUrlService;

    public ResendInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notification,
        IAppUrlService appUrlService)
    {
        _unitOfWork = unitOfWork;
        _notification = notification;
        _appUrlService = appUrlService;
    }

    public async Task<BaseResponse<bool>> Handle(
        ResendInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(command.TenantSlug);
        if (tenant is null)
            return BaseResponse<bool>.Failure("Organization not found.");

        var user = await _unitOfWork.UserRepository.GetAllByTenantAsync(tenant.TenantId)
            .ContinueWith(t => t.Result.FirstOrDefault(u => u.Id == command.UserId));

        if (user is null)
            return BaseResponse<bool>.Failure("User not found in this organization.");

        if (user.Status != UserStatus.Invited)
            return BaseResponse<bool>.Failure("This user has already accepted their invitation.");

        // Generate a fresh setup token
        var setupToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
            user, "PasswordReset", "ResetPassword");

        user.SetInvitationToken(setupToken, DateTime.UtcNow.AddHours(72));
        await _unitOfWork.CompleteAsync(cancellationToken);

        var setupUrl = _appUrlService.GetSetupAccountUrl(setupToken);

        var emailBody = $@"
            <p>Hi {user.FullName},</p>
            <p>Your invitation to <strong>{tenant.BusinessName}</strong> on ApexBooking has been resent.</p>
            <p>Click the button below to set up your password and activate your account:</p>
            <p>
                <a href='{setupUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                    Set Up Your Account
                </a>
            </p>
            <p>This link expires in <strong>72 hours</strong>.</p>";

        await _notification.SendEmailAsync(user.Email!, $"Invitation resent — {tenant.BusinessName}", emailBody);

        return BaseResponse<bool>.Success(true);
    }
}
