using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.TenantRequest.Events
{

    public class SendTenantRequestReceivedEmailHandler
        : INotificationHandler<DomainEventNotification<TenantRequestReceivedDomainEvent>>
    {
        private readonly IAuthNotificationService _authNotificationService;
        private readonly ILogger<SendTenantRequestReceivedEmailHandler> _logger;

        public SendTenantRequestReceivedEmailHandler(
            IAuthNotificationService authNotificationService,
            ILogger<SendTenantRequestReceivedEmailHandler> logger)
        {
            _authNotificationService = authNotificationService;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<TenantRequestReceivedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            try
            {
                await _authNotificationService.SendTenantRequestReceivedEmailAsync(
                    e.OwnerEmail,
                    $"{e.OwnerFirstName} {e.OwnerLastName}".Trim(),
                    e.BusinessName,
                    e.RequestedPlan.ToString(),
                    cancellationToken);
            }
            catch (Exception ex)
            {

                _logger.LogError(
                    ex,
                    "Failed to send the request-received acknowledgement to {OwnerEmail} for request {RequestId}.",
                    e.OwnerEmail,
                    e.RequestId);
            }
        }
    }
}
