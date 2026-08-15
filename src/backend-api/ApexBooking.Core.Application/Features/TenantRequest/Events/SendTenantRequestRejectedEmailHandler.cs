using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.TenantRequest.Events
{
    public class SendTenantRequestRejectedEmailHandler
        : INotificationHandler<DomainEventNotification<TenantRequestRejectedDomainEvent>>
    {
        private readonly IAuthNotificationService _authNotificationService;
        private readonly ILogger<SendTenantRequestRejectedEmailHandler> _logger;

        public SendTenantRequestRejectedEmailHandler(
            IAuthNotificationService authNotificationService,
            ILogger<SendTenantRequestRejectedEmailHandler> logger)
        {
            _authNotificationService = authNotificationService;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<TenantRequestRejectedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            try
            {
                await _authNotificationService.SendRejectionEmailAsync(
                    e.OwnerEmail,
                    $"{e.OwnerFirstName} {e.OwnerLastName}".Trim(),
                    e.BusinessName,
                    e.Reason,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // The rejection itself is already committed; a mailer outage must not undo it.
                _logger.LogError(
                    ex,
                    "Failed to send the rejection email to {OwnerEmail} for request {RequestId}.",
                    e.OwnerEmail,
                    e.RequestId);
            }
        }
    }
}
