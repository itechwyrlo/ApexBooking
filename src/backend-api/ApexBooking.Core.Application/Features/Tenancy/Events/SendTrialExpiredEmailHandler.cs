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
    public class SendTrialExpiredEmailHandler
        : INotificationHandler<DomainEventNotification<TrialExpiredDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantLifecycleNotificationService _tenantLifecycleNotificationService;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IAppUrlService _appUrlService;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<SendTrialExpiredEmailHandler> _logger;

        public SendTrialExpiredEmailHandler(
            IUnitOfWork unitOfWork,
            ITenantLifecycleNotificationService tenantLifecycleNotificationService,
            IApplicationUserService applicationUserService,
            IAppUrlService appUrlService,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<SendTrialExpiredEmailHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _tenantLifecycleNotificationService = tenantLifecycleNotificationService;
            _applicationUserService = applicationUserService;
            _appUrlService = appUrlService;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<TrialExpiredDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            // Same single-query-scoped-by-event-TenantId pattern as SendBookingConfirmationEmailHandler
            // — this handler runs as an outbox-relayed side effect, off the ambient HTTP request, so
            // there's no ambient tenant to rely on.
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.BusinessProfile!, t => t.Members]
            );

            if (tenant == null || tenant.BusinessProfile == null)
            {
                _logger.LogError("Could not resolve workspace details for Tenant {TenantId}. Trial-expired email was aborted.", e.TenantId);
                return;
            }

            var reactivateUrl = _appUrlService.GetReactivateUrl();

            await _tenantLifecycleNotificationService.SendTrialExpiredEmailAsync(
                to: tenant.OwnerContact.Email,
                ownerName: tenant.OwnerContact.FirstName,
                businessName: tenant.BusinessProfile.BusinessName,
                reactivateUrl: reactivateUrl,
                ct: cancellationToken);

            var createdNotifications = new List<Notification>();

            // Owner-only, matching the same tenant-side-admin scoping rule already established for
            // time-off (only the Owner sees/acts on tenant-management matters, not every Admin).
            var owner = tenant.Members.FirstOrDefault(m => m.Role == SystemRole.Owner);
            if (owner?.UserId is { } ownerUserId)
            {
                var ownerNotification = Notification.Create(
                    ownerUserId,
                    NotificationRecipientType.TenantAdmin,
                    tenant.TenantId,
                    NotificationEventType.TrialExpired,
                    "Your trial has ended",
                    $"Your ApexBooking trial for {tenant.BusinessProfile.BusinessName} has ended. Accepting new bookings is paused until you add payment details.");
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
                    NotificationEventType.TrialExpired,
                    "Trial Expired",
                    $"Tenant {tenant.BusinessProfile.BusinessName} trial has expired.");
                _unitOfWork.NotificationRepository.Add(adminNotification);
                createdNotifications.Add(adminNotification);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync(createdNotifications, cancellationToken);

            _logger.LogInformation(
                "Successfully dispatched trial-expired email and notifications for Tenant {TenantId}.",
                e.TenantId);
        }
    }
}
