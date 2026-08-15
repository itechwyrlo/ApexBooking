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
    }

    // Reverts Processing -> Pending for another attempt, unless MaxRetryCount is now exhausted, in
    // which case it lands in the terminal Failed state for manual retry (SuperAdmin failed-jobs view).
    public void MarkFailed(string error)
    {
        RetryCount++;
        LastError = error;
        Status = RetryCount >= MaxRetryCount
            ? OutboxMessageStatus.Failed
            : OutboxMessageStatus.Pending;
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
        return true;
    }
}
