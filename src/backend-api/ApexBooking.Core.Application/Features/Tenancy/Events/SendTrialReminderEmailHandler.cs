using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Application.Common.Notifications;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Notification.Tenancy;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Tenancy.Events
{
    public class SendTrialReminderEmailHandler
        : INotificationHandler<DomainEventNotification<TrialReminderSentDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantLifecycleNotificationService _tenantLifecycleNotificationService;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IAppUrlService _appUrlService;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<SendTrialReminderEmailHandler> _logger;

        public SendTrialReminderEmailHandler(
            IUnitOfWork unitOfWork,
            ITenantLifecycleNotificationService tenantLifecycleNotificationService,
            IApplicationUserService applicationUserService,
            IAppUrlService appUrlService,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<SendTrialReminderEmailHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _tenantLifecycleNotificationService = tenantLifecycleNotificationService;
            _applicationUserService = applicationUserService;
            _appUrlService = appUrlService;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<TrialReminderSentDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.BusinessProfile!, t => t.Members]
            );

            if (tenant == null || tenant.BusinessProfile == null || tenant.Trial == null)
            {
                _logger.LogError("Could not resolve workspace/trial details for Tenant {TenantId}. Trial-reminder email was aborted.", e.TenantId);
                return;
            }

            var billingUrl = _appUrlService.GetBillingUrl();

            await _tenantLifecycleNotificationService.SendTrialReminderEmailAsync(
                to: tenant.OwnerContact.Email,
                ownerName: tenant.OwnerContact.FirstName,
                businessName: tenant.BusinessProfile.BusinessName,
                trialEndsAtUtc: tenant.Trial.EndDate,
                billingUrl: billingUrl,
                ct: cancellationToken);

            var createdNotifications = new List<Notification>();

            var owner = tenant.Members.FirstOrDefault(m => m.Role == SystemRole.Owner);
            if (owner?.UserId is { } ownerUserId)
            {
                var ownerNotification = Notification.Create(
                    ownerUserId,
                    NotificationRecipientType.TenantAdmin,
                    tenant.TenantId,
                    NotificationEventType.TrialReminderSent,
                    "Trial Expiring Soon",
                    $"Your ApexBooking trial for {tenant.BusinessProfile.BusinessName} ends on {tenant.Trial.EndDate:MMMM d, yyyy}.");
                _unitOfWork.NotificationRepository.Add(ownerNotification);
                createdNotifications.Add(ownerNotification);
            }

            var platformAdminIds = await _applicationUserService.GetPlatformAdminIdsAsync(cancellationToken);
            foreach (var adminId in platformAdminIds)
            {
                var adminNotification = Notification.Create(
                    adminId,
                    NotificationRecipientType.SuperAdmin,
                    null,
                    NotificationEventType.TrialReminderSent,
                    "Trial Expiring Soon",
                    $"Tenant {tenant.BusinessProfile.BusinessName} trial expires on {tenant.Trial.EndDate:MMMM d, yyyy}.");
                _unitOfWork.NotificationRepository.Add(adminNotification);
                createdNotifications.Add(adminNotification);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync(createdNotifications, cancellationToken);

            _logger.LogInformation(
                "Successfully dispatched trial-reminder email and notifications for Tenant {TenantId}.",
                e.TenantId);
        }
    }
}
