using System;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Services;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Platform.Commands.RetryOutboxMessage
{
    public class RetryOutboxMessageHandler : ICommandHandler<RetryOutboxMessageCommand>
    {
        private readonly IOutboxStore _outboxStore;
        private readonly IOutboxTrigger _outboxTrigger;
        private readonly ILogger<RetryOutboxMessageHandler> _logger;

        public RetryOutboxMessageHandler(
            IOutboxStore outboxStore,
            IOutboxTrigger outboxTrigger,
            ILogger<RetryOutboxMessageHandler> logger)
        {
            _outboxStore = outboxStore;
            _outboxTrigger = outboxTrigger;
            _logger = logger;
        }

        public async Task Handle(RetryOutboxMessageCommand request, CancellationToken cancellationToken)
        {
            var retried = await _outboxStore.RetryAsync(request.Id, cancellationToken);
            if (!retried)
            {
                _logger.LogWarning(
                    "RetryOutboxMessage: message {Id} was not in a Failed state (already retried, processed, or doesn't exist) — ignored.",
                    request.Id);
                return;
            }

            // Same immediate-trigger mechanism the original write path uses — fires within a second
            // or two instead of waiting for the next recurring sweep (up to ~1 min). Non-fatal if it
            // fails: the row is already back to Pending, so the sweep will still pick it up.
            try
            {
                await _outboxTrigger.NotifyAsync([request.Id], cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Immediate retry trigger failed for outbox message {Id}; the recurring sweep will still pick it up.", request.Id);
            }
        }
    }
}
