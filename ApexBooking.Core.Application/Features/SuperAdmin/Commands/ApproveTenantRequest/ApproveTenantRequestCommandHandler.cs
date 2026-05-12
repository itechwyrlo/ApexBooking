using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ApproveTenantRequest;

internal sealed class ApproveTenantRequestCommandHandler
    : ICommandHandler<ApproveTenantRequestCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notification;
    private readonly IAppUrlService _appUrlService;

    public ApproveTenantRequestCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notification,
        IAppUrlService appUrlService)
    {
        _unitOfWork = unitOfWork;
        _notification = notification;
        _appUrlService = appUrlService;
    }

    public async Task<BaseResponse<bool>> Handle(
        ApproveTenantRequestCommand command,
        CancellationToken cancellationToken)
    {
        var request = await _unitOfWork.TenantRequestRepository.GetAsync(
            r => r.Id == new TenantRequestId(command.RequestId));
        if (request is null)
            return BaseResponse<bool>.Failure("Request not found.");

        var slugExists = await _unitOfWork.TenantRepository.FindBySlugAsync(command.Slug);
        if (slugExists is not null)
            return BaseResponse<bool>.Failure("This booking URL slug is already taken.");

        var emailExists = await _unitOfWork.TenantRepository.FindByEmailAsync(request.OwnerEmail);
        if (emailExists is not null)
            return BaseResponse<bool>.Failure("An organization with this owner email already exists.");

        var emailTaken = await _unitOfWork.UserRepository.FindByEmailAcrossAllTenantsAsync(request.OwnerEmail);
        if (emailTaken is not null)
            return BaseResponse<bool>.Failure("This email is already registered to an existing user.");

        request.Approve();

        var tenant = Tenant.Create(
            command.Slug,
            request.BusinessName,
            request.OwnerFullName,
            request.OwnerEmail,
            request.OwnerPhone);

        tenant.CreateTenantProfile("UTC", "USD");
        tenant.CreateTenantSettings();
        tenant.CreateTenantPaymentPolicy();
        tenant.ActivateWithTrial(request.Plan, command.TrialDays);

        var admin = User.CreateInvitedUser(
            tenant.TenantId,
            request.OwnerFullName,
            request.OwnerEmail,
            UserRole.TenantAdmin,
            "pending");

        _unitOfWork.TenantRequestRepository.Update(request);
        _unitOfWork.TenantRepository.Add(tenant);
        await _unitOfWork.CompleteAsync(cancellationToken);

        var randomPassword = $"{Guid.NewGuid():N}{Guid.NewGuid():N}Aa1@";
        var createResult = await _unitOfWork.UserRepository.CreateAsync(admin, randomPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return BaseResponse<bool>.Failure($"Failed to create admin user: {errors}");
        }

        await _unitOfWork.UserRepository.AddToRoleAsync(admin, "TENANT ADMIN");

        admin = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, request.OwnerEmail);
        if (admin is null)
            return BaseResponse<bool>.Failure("Error retrieving created admin user.");

        var setupToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
            admin, "PasswordReset", "ResetPassword");

        admin.SetInvitationToken(setupToken, DateTime.UtcNow.AddHours(72));
        await _unitOfWork.CompleteAsync(cancellationToken);

        var setupUrl = _appUrlService.GetSetupAccountUrl(setupToken);

        var trialEndsAt = tenant.TrialEndsAt!.Value.ToString("MMMM d, yyyy");
        var emailBody = $@"
            <p>Hi {request.OwnerFullName},</p>
            <p>Great news — your ApexBooking account for <strong>{request.BusinessName}</strong> has been approved!</p>
            <p>You are on the <strong>{request.Plan}</strong> plan with a free trial that runs until <strong>{trialEndsAt}</strong>.</p>
            <p>Click the button below to set up your password and activate your account:</p>
            <p>
                <a href='{setupUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                    Set Up Your Account
                </a>
            </p>
            <p>This link expires in <strong>72 hours</strong>.</p>
            <p>Your booking page will be available at: <strong>/book/{tenant.Slug}</strong></p>";

        await _notification.SendEmailAsync(
            request.OwnerEmail,
            $"Your ApexBooking account is ready — set up your password",
            emailBody);

        return BaseResponse<bool>.Success(true);
    }
}
