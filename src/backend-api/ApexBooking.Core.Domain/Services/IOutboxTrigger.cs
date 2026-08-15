namespace ApexBooking.Core.Domain.Services;

/// <summary>
/// Latency optimization only — NOT the reliability guarantee. Called by UnitOfWork.CompleteAsync
/// right after a commit that produced new OutboxMessage rows, to nudge them toward near-immediate
/// processing instead of waiting for the next recurring sweep. Implemented in Infrastructure
/// (HangfireOutboxTrigger) so Core.Persistence never takes a Hangfire package reference — same
/// pattern as IDomainEventDispatcher.
///
/// Callers MUST treat failures here as non-fatal (log and continue): the business transaction has
/// already committed successfully by the time this runs, and the recurring sweep still guarantees
/// eventual delivery even if this call never fires.
/// </summary>
public interface IOutboxTrigger
{
    Task NotifyAsync(IReadOnlyList<Guid> outboxMessageIds, CancellationToken cancellationToken = default);
}
