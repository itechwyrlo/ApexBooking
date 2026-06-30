using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ApproveTenantRequest;

internal sealed class ApproveTenantRequestCommandHandler : ICommandHandler<ApproveTenantRequestCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthNotificationService _authNotification;
    private readonly IAppUrlService _appUrlService;
    private readonly IPushNotificationService _pushService;
    private readonly IRealtimeNotificationService _realtimeService;

    public ApproveTenantRequestCommandHandler(
        IUnitOfWork unitOfWork,
        IAuthNotificationService authNotification,
        IAppUrlService appUrlService,
        IPushNotificationService pushService,
        IRealtimeNotificationService realtimeService)
    {
        _unitOfWork = unitOfWork;
        _authNotification = authNotification;
        _appUrlService = appUrlService;
        _pushService = pushService;
        _realtimeService = realtimeService;
    }

    public async Task Handle(ApproveTenantRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _unitOfWork.TenantRequestRepository.GetAsync(
            r => r.Id == new TenantRequestId(command.RequestId));
        if (request is null)
            throw new NotFoundException("Request not found.");

        var slugExists = await _unitOfWork.TenantRepository.FindBySlugAsync(command.Slug);
        request.EnsureSlugIsAvailable(slugExists is not null);

        var emailExists = await _unitOfWork.TenantRepository.FindByEmailAsync(request.OwnerEmail);
        var emailTaken = await _unitOfWork.UserRepository.FindByEmailAcrossAllTenantsAsync(request.OwnerEmail);
        // request.EnsureOwnerEmailIsAvailable(emailExists is not null, emailTaken is not null);

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
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }

        await _unitOfWork.UserRepository.AddToRoleAsync(admin, "TENANT ADMIN");

        admin = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, request.OwnerEmail);
        if (admin is null)
            throw new InvalidOperationException("Error retrieving created admin user.");

        var setupToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
            admin, "PasswordReset", "ResetPassword");

        admin.SetInvitationToken(setupToken, DateTime.UtcNow.AddHours(72));

        var superAdmins = await _unitOfWork.SuperAdminRepository.GetAllAsync(sa => sa.IsActive);
        var superAdminNotifications = new List<Notification>();
        foreach (var sa in superAdmins)
        {
            var n = Notification.Create(
                sa.SuperAdminId.Value,
                NotificationRecipientType.SuperAdmin,
                null,
                NotificationEventType.TenantRequestApproved,
                "Tenant Approved",
                $"Tenant {request.BusinessName} has been approved and is now active.");
            _unitOfWork.NotificationRepository.Add(n);
            superAdminNotifications.Add(n);
        }

        var tenantAdminNotification = Notification.Create(
            admin.Id,
            NotificationRecipientType.TenantAdmin,
            tenant.TenantId,
            NotificationEventType.TenantRequestApproved,
            "Account Approved",
            $"Your tenant account for {request.BusinessName} has been approved and is now active.");
        _unitOfWork.NotificationRepository.Add(tenantAdminNotification);

        await _unitOfWork.CompleteAsync(cancellationToken);

        await _pushService.SendAsync(
            superAdminNotifications.Select(n => n.RecipientId).ToList(),
            "Tenant Approved",
            $"Tenant {request.BusinessName} has been approved and is now active.",
            NotificationEventType.TenantRequestApproved.ToString(),
            cancellationToken);
        await Task.WhenAll(superAdminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));

        await _realtimeService.SendAsync(tenantAdminNotification.RecipientId, tenantAdminNotification.ToDto());

        var setupUrl = _appUrlService.GetSetupAccountUrl(setupToken);
        var trialEndsAt = tenant.TrialEndsAt!.Value.ToString("MMMM d, yyyy");

        await _authNotification.SendAccountApprovalEmailAsync(
            request.OwnerEmail,
            request.OwnerFullName,
            request.BusinessName,
            request.Plan.ToString(),
            trialEndsAt,
            setupUrl,
            tenant.Slug,
            cancellationToken);
    }
}
