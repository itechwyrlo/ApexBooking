using ApexBooking.Core.Domain.Services;
using Hangfire;

namespace ApexBooking.Infrastructure.BackgroundJobs;

/// <summary>
/// Thin Hangfire-callable wrapper — depends only on IOutboxRelayService (Core.Domain port), never
/// on Core.Application/MediatR directly, so Infrastructure's project references stay untouched.
/// See the "Hangfire + Outbox Core" design spec for why this boundary matters.
/// </summary>
public class OutboxRelayJob
{
    // Matches the design spec's "30-60s" target at standard cron's 1-minute granularity — the
    // immediate trigger (see HangfireOutboxTrigger) is what actually delivers low latency; this
    // sweep is the reliability guarantee, not the fast path.
    private const int SweepBatchSize = 50;

    private readonly IOutboxRelayService _relayService;

    public OutboxRelayJob(IOutboxRelayService relayService)
    {
        _relayService = relayService;
    }

    // Recurring-sweep entry point. DisableConcurrentExecution guards against two overlapping
    // sweeps if a batch takes longer than the poll interval — on top of, not instead of, the
    // row-level atomic claim in IOutboxStore that actually prevents double-processing.
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public Task ReplayPending(IJobCancellationToken cancellationToken) =>
        _relayService.ReplayPendingAsync(SweepBatchSize, cancellationToken.ShutdownToken);

    // Immediate-trigger entry point — enqueued by HangfireOutboxTrigger for a specific set of
    // just-written message ids.
    public Task Replay(Guid[] outboxMessageIds, IJobCancellationToken cancellationToken) =>
        _relayService.ReplayAsync(outboxMessageIds, cancellationToken.ShutdownToken);
}
