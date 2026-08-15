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
    // Listens on the same TenantRequestReceivedDomainEvent that drives
    // SendTenantRequestReceivedEmailHandler (which emails the requester) — this one notifies every
    // platform admin that a new request came in.
    public class NotifySuperAdminOnTenantRequestSubmittedHandler
        : INotificationHandler<DomainEventNotification<TenantRequestReceivedDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<NotifySuperAdminOnTenantRequestSubmittedHandler> _logger;

        public NotifySuperAdminOnTenantRequestSubmittedHandler(
            IUnitOfWork unitOfWork,
            IApplicationUserService applicationUserService,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<NotifySuperAdminOnTenantRequestSubmittedHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _applicationUserService = applicationUserService;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<TenantRequestReceivedDomainEvent> notification, CancellationToken cancellationToken)
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
                    NotificationEventType.TenantRequestSubmitted,
                    "New Tenant Request",
                    $"{e.BusinessName} submitted a new registration request.");
                _unitOfWork.NotificationRepository.Add(n);
                createdNotifications.Add(n);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync(createdNotifications, cancellationToken);

            _logger.LogInformation("Dispatched tenant-request-submitted notification for {BusinessName}.", e.BusinessName);
        }
    }
}
