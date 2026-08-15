namespace ApexBooking.Core.Domain.Services;

/// <summary>
/// Replays stored IReliableDomainEvent payloads through the same handlers that would have run
/// synchronously before the outbox existed. Implemented in Core.Application (uses MediatR's
/// IPublisher) — this port is what lets the Hangfire relay job live in Infrastructure without
/// Infrastructure ever referencing Core.Application directly.
/// </summary>
public interface IOutboxRelayService
{
    // Immediate-trigger path: replay a specific set of just-written messages (low latency).
    Task ReplayAsync(IReadOnlyList<Guid> outboxMessageIds, CancellationToken cancellationToken = default);

    // Recurring-sweep path: replay up to batchSize remaining Pending messages (the reliability
    // guarantee — catches anything the immediate trigger missed).
    Task ReplayPendingAsync(int batchSize, CancellationToken cancellationToken = default);
}
