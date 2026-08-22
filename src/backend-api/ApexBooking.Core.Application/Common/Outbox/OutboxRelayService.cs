using System.Text.Json;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.EmailNotification;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Common.Outbox;

/// <summary>
/// Implements IOutboxRelayService (Core.Domain). Deserializes a stored OutboxMessage back into its
/// concrete IReliableDomainEvent type and republishes it through MediatR, wrapped the same way
/// DomainEventDispatcher wraps a live event — so the existing 6 email handlers run unchanged,
/// regardless of whether they were triggered synchronously (non-reliable events, still) or via
/// this relay (reliable events).
/// </summary>
public sealed class OutboxRelayService : IOutboxRelayService
{
    private readonly IOutboxStore _store;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxRelayService> _logger;

    public OutboxRelayService(IOutboxStore store, IPublisher publisher, ILogger<OutboxRelayService> logger)
    {
        _store = store;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ReplayAsync(IReadOnlyList<Guid> outboxMessageIds, CancellationToken cancellationToken = default)
    {
        foreach (var id in outboxMessageIds)
            await ReplayOneAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReplayPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var ids = await _store.GetPendingIdsAsync(batchSize, cancellationToken).ConfigureAwait(false);

        foreach (var id in ids)
            await ReplayOneAsync(id, cancellationToken).ConfigureAwait(false);
    }

    // One message at a time, its own try/catch — a bad message must never fail the rest of the
    // batch or cause already-succeeded messages to be reprocessed (see design spec).
    private async Task ReplayOneAsync(Guid id, CancellationToken cancellationToken)
    {
        var message = await _store.TryClaimAsync(id, cancellationToken).ConfigureAwait(false);
        if (message is null)
            return; // lost the claim race to the other entry point, or no longer pending — fine, skip

        try
        {
            // ApexBooking.Core.Domain is where every IReliableDomainEvent lives — see OutboxMessage's
            // doc comment for why the assembly is named explicitly here.
            var eventType = Type.GetType($"{message.EventType}, ApexBooking.Core.Domain")
                ?? throw new InvalidOperationException($"Could not resolve outbox event type '{message.EventType}'.");

            var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType)
                ?? throw new InvalidOperationException($"Outbox message {id} payload deserialized to null.");

            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
            var notification = Activator.CreateInstance(notificationType, domainEvent);

            if (notification is INotification mediatrNotification)
                await _publisher.Publish(mediatrNotification, cancellationToken).ConfigureAwait(false);

            await _store.MarkProcessedAsync(id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A handler that throws EmailDeliveryException.IsTransient == false (a malformed
            // recipient, a rejected payload — something that will fail identically no matter how
            // many times it's retried) fails the message permanently right away instead of burning
            // its retry budget; anything else (including a plain unclassified exception) is treated
            // as transient, matching the previous behavior.
            var isTransient = ex is not EmailDeliveryException { IsTransient: false };

            _logger.LogError(ex, "Outbox message {OutboxMessageId} ({EventType}) failed to replay.", id, message.EventType);
            await _store.MarkFailedAsync(id, ex.Message, isTransient, cancellationToken).ConfigureAwait(false);
        }
    }
}
