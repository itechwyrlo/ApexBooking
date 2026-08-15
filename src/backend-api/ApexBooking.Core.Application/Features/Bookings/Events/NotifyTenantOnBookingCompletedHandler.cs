using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Application.Common.Notifications;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    // Listens on the same BookingCompletedDomainEvent that drives SendThankYouEmailOnBookingCompletedHandler
    // (the customer-facing email) — this one writes the tenant-facing dashboard notification instead.
    public class NotifyTenantOnBookingCompletedHandler
        : INotificationHandler<DomainEventNotification<BookingCompletedDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<NotifyTenantOnBookingCompletedHandler> _logger;

        public NotifyTenantOnBookingCompletedHandler(
            IUnitOfWork unitOfWork,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<NotifyTenantOnBookingCompletedHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingCompletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.Members]);

            var owner = tenant?.Members.FirstOrDefault(m => m.Role == SystemRole.Owner);
            if (owner?.UserId is not { } ownerUserId)
            {
                _logger.LogWarning(
                    "Could not resolve an Owner for Tenant {TenantId}; booking-completed notification skipped for {BookingReference}.",
                    e.TenantId, e.BookingReference);
                return;
            }

            var n = Notification.Create(
                ownerUserId,
                NotificationRecipientType.TenantAdmin,
                e.TenantId,
                NotificationEventType.BookingCompleted,
                "Booking Completed",
                $"Booking {e.BookingReference} was completed.");
            _unitOfWork.NotificationRepository.Add(n);

            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync([n], cancellationToken);
        }
    }
}
