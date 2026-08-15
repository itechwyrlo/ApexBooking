using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Application.Common.Notifications;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.TenantRequest.Events
{
    // Listens on the same TenantRequestApproveDomainEvent that drives
    // ProvisionTenantOnRequestApprovedHandler (business logic, stays synchronous — not
    // IReliableDomainEvent). This handler runs on the same synchronous path, alongside it.
    public class NotifySuperAdminOnTenantRequestApprovedHandler
        : INotificationHandler<DomainEventNotification<TenantRequestApproveDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<NotifySuperAdminOnTenantRequestApprovedHandler> _logger;

        public NotifySuperAdminOnTenantRequestApprovedHandler(
            IUnitOfWork unitOfWork,
            IApplicationUserService applicationUserService,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<NotifySuperAdminOnTenantRequestApprovedHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _applicationUserService = applicationUserService;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<TenantRequestApproveDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var createdNotifications = new List<Notification>();
            var platformAdminIds = await _applicationUserService.GetPlatformAdminIdsAsync(cancellationToken);
            foreach (var adminId in platformAdminIds)
            {
                var n = Notification.Create(
                    adminId,
                    NotificationRecipientType.SuperAdmin,
                    null,
                    NotificationEventType.TenantRequestApproved,
                    "Tenant Request Approved",
                    $"{e.BusinessName}'s registration request was approved.");
                _unitOfWork.NotificationRepository.Add(n);
                createdNotifications.Add(n);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync(createdNotifications, cancellationToken);

            _logger.LogInformation("Dispatched tenant-request-approved notification for {BusinessName}.", e.BusinessName);
        }
    }
}
