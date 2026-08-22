using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.Entities;

/// <summary>
/// Idempotency ledger for PayMongo webhook deliveries — a persistence record, not a domain
/// aggregate (no IAggregateRoot, no repository; same pattern as OutboxMessage/SmsUsage).
/// PayMongoEventId carries a unique DB constraint (ProcessedPaymentEventConfiguration) as a
/// last-resort safety net against a same-event double-delivery race slipping past the
/// application-level existence check in ProcessPaymentWebhookCommandHandler.
/// </summary>
public class ProcessedPaymentEvent
{
    public Guid Id { get; private set; }
    public string PayMongoEventId { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public Guid BookingId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    protected ProcessedPaymentEvent() { }

    private ProcessedPaymentEvent(string payMongoEventId, Guid tenantId, Guid bookingId)
    {
        Id = Guid.NewGuid();
        PayMongoEventId = payMongoEventId;
        TenantId = tenantId;
        BookingId = bookingId;
        ProcessedAt = DateTime.UtcNow;
    }

    public static ProcessedPaymentEvent Create(string payMongoEventId, Guid tenantId, Guid bookingId)
    {
        if (string.IsNullOrWhiteSpace(payMongoEventId))
            throw new BusinessRuleBrokenException("PayMongo event id is required to record a processed payment event.");

        return new ProcessedPaymentEvent(payMongoEventId, tenantId, bookingId);
    }
}
