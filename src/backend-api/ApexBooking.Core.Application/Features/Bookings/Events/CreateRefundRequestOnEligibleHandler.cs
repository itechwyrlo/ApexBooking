using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Application.Common.Notifications;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    // Subscribes to BookingRefundEligibleDomainEvent — a plain IDomainEvent, so this runs
    // synchronously, same request as the cancellation itself (no external call involved, just a
    // DB write + a bell notification).
    public class CreateRefundRequestOnEligibleHandler
        : INotificationHandler<DomainEventNotification<BookingRefundEligibleDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<CreateRefundRequestOnEligibleHandler> _logger;

        public CreateRefundRequestOnEligibleHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<CreateRefundRequestOnEligibleHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingRefundEligibleDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.Members]);

            if (tenant is null)
            {
                _logger.LogError(
                    "Could not resolve Tenant {TenantId} to create a RefundRequest for Booking {BookingReference}.",
                    e.TenantId, e.BookingReference);
                return;
            }

            var request = RefundRequest.Create(
                e.TenantId,
                e.BookingId,
                e.RefundAmount,
                e.CurrencyCode,
                e.EwalletProvider,
                e.EwalletNumber,
                e.EwalletName);

            await _refundRequestStore.AddAsync(request, cancellationToken);

            var recipients = tenant.Members.Where(m =>
                (m.Role == SystemRole.Owner || m.Role == SystemRole.Admin) && m.UserId.HasValue);
            var notifications = recipients
                .Select(m => Notification.Create(
                    m.UserId!.Value,
                    NotificationRecipientType.TenantAdmin,
                    e.TenantId,
                    NotificationEventType.RefundReviewNeeded,
                    "Refund Review Needed",
                    $"Booking {e.BookingReference} was cancelled and is eligible for a refund of {e.RefundAmount:0.00} {e.CurrencyCode}. Please review it."))
                .ToList();

            foreach (var n in notifications)
                _unitOfWork.NotificationRepository.Add(n);

            await _unitOfWork.CompleteAsync(cancellationToken);

            if (notifications.Count > 0)
                await _realtimeDispatcher.PushAsync(notifications, cancellationToken);
        }
    }
}
