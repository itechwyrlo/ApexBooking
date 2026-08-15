using ApexBooking.Core.Domain.Services;
using Hangfire;

namespace ApexBooking.Infrastructure.BackgroundJobs;

/// <summary>
/// Implements IOutboxTrigger (Core.Domain) — the only class in the solution that calls
/// IBackgroundJobClient.Enqueue for the outbox. Core.Persistence never references Hangfire; it only
/// ever sees the IOutboxTrigger interface (constructor-injected into UnitOfWork the same way
/// IDomainEventDispatcher already is).
/// </summary>
public sealed class HangfireOutboxTrigger : IOutboxTrigger
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireOutboxTrigger(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task NotifyAsync(IReadOnlyList<Guid> outboxMessageIds, CancellationToken cancellationToken = default)
    {
        var ids = outboxMessageIds.ToArray();

        _backgroundJobClient.Enqueue<OutboxRelayJob>(
            job => job.Replay(ids, JobCancellationToken.Null));

        return Task.CompletedTask;
    }
}
