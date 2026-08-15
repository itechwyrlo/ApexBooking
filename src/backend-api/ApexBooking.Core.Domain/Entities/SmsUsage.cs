using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

/// <summary>
/// The per-tenant monthly SMS counter (ADR-056, 08 §3). A persistence record — <b>not</b> a domain
/// aggregate (no <c>IAggregateRoot</c>, no repository). One row per tenant per month, created lazily
/// on the first send of the month. The limit is never stored here — it comes from <c>IPlanPolicy</c>.
/// </summary>
public class SmsUsage
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; } = default!;
    public int PeriodYear { get; private set; }
    public int PeriodMonth { get; private set; }
    public int SentCount { get; private set; }

    protected SmsUsage() { }

    public SmsUsage(TenantId tenantId, int periodYear, int periodMonth)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        PeriodYear = periodYear;
        PeriodMonth = periodMonth;
        SentCount = 0;
    }

    public void Increment() => SentCount++;
}
