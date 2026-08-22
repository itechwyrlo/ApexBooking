using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.Entities;

/// <summary>
/// A persistence record — not a domain aggregate (no <c>IAggregateRoot</c>, no repository; see
/// <see cref="SmsUsage"/> for the same pattern). Written in the same transaction as the business
/// row that raised the originating <see cref="Events.IReliableDomainEvent"/> (see
/// UnitOfWork.CompleteAsync), then drained by IOutboxRelayService — either immediately via
/// IOutboxTrigger, or on the next recurring sweep. Deliberately NOT an ITenantEntity: the relay
/// has no ambient tenant and must see pending rows across every tenant, so it must not be subject
/// to the global tenant query filter (see ApexBookingDbContext.ApplyGlobalFilters).
/// </summary>
public class OutboxMessage
{
    public const int MaxRetryCount = 5;

    public Guid Id { get; private set; }

    // Resolved back to the concrete IReliableDomainEvent type by OutboxRelayService via
    // Type.GetType($"{EventType}, ApexBooking.Core.Domain").
    public string EventType { get; private set; } = string.Empty;

    // JSON-serialized event payload (System.Text.Json).
    public string Payload { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    // Earliest moment this message is eligible for another attempt — null means "eligible now"
    // (the initial Pending state, and any state other than a backed-off Pending). MarkFailed sets
    // this to an exponentially-growing delay so a message that just failed doesn't get retried on
    // literally the next sweep tick a few seconds later; OutboxStore.GetPendingIdsAsync filters on
    // it. See MarkFailed's doc comment for the schedule.
    public DateTime? NextAttemptAtUtc { get; private set; }

    protected OutboxMessage() { }

    private OutboxMessage(string eventType, string payload, DateTime occurredAtUtc)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
    }

    public static OutboxMessage Create(string eventType, string payload, DateTime occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new BusinessRuleBrokenException("Outbox message event type is required.");

        if (string.IsNullOrWhiteSpace(payload))
            throw new BusinessRuleBrokenException("Outbox message payload is required.");

        return new OutboxMessage(eventType, payload, occurredAtUtc);
    }

    public void MarkProcessed()
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAtUtc = DateTime.UtcNow;
        LastError = null;
        NextAttemptAtUtc = null;
    }

    // Reverts Processing -> Pending for another attempt, unless MaxRetryCount is now exhausted, in
    // which case it lands in the terminal Failed state for manual retry (SuperAdmin failed-jobs view).
    // Each retry backs off exponentially from the recurring sweep's own polling interval (1m, 2m,
    // 4m, 8m for RetryCount 1-4) instead of being retried on the very next ~1-minute sweep tick —
    // a transient outage (e.g. Brevo returning 503/429) that outlasts a couple of minutes no longer
    // burns the entire retry budget inside the first 5 minutes.
    public void MarkFailed(string error)
    {
        RetryCount++;
        LastError = error;

        if (RetryCount >= MaxRetryCount)
        {
            Status = OutboxMessageStatus.Failed;
            NextAttemptAtUtc = null;
            return;
        }

        Status = OutboxMessageStatus.Pending;
        NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, RetryCount - 1));
    }

    // For a failure classified as non-transient (e.g. INotificationService's
    // EmailDeliveryException.IsTransient == false — a malformed recipient, a rejected payload —
    // something that will fail identically no matter how many times it's retried). Skips the
    // retry budget entirely and goes straight to the terminal Failed state, instead of consuming
    // MaxRetryCount attempts (and their backoff delays) pointlessly before a human ever sees it.
    public void MarkFailedPermanently(string error)
    {
        RetryCount++;
        LastError = error;
        Status = OutboxMessageStatus.Failed;
        NextAttemptAtUtc = null;
    }

    // SuperAdmin-triggered manual retry. Resets RetryCount to 0 — without that, a row already at
    // MaxRetryCount would fail permanently again after just one more attempt, defeating the point
    // of a deliberate retry. Only valid from the terminal Failed state.
    public bool TryRetry()
    {
        if (Status != OutboxMessageStatus.Failed)
            return false;

        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
        NextAttemptAtUtc = null;
        return true;
    }
}
