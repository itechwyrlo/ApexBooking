using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.Core.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Services;

/// <summary>
/// Persistence implementation of the SMS quota (ADR-056, 08 §3). Reserves first, sends second, with
/// an atomic conditional increment so two concurrent sends can't both slip past the limit.
/// </summary>
public sealed class SmsQuotaService : ISmsQuotaService
{
    private readonly ApexBookingDbContext _context;
    private readonly IPlanPolicy _planPolicy;

    public SmsQuotaService(ApexBookingDbContext context, IPlanPolicy planPolicy)
    {
        _context = context;
        _planPolicy = planPolicy;
    }

    public async Task<bool> TryConsumeAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        int year = now.Year, month = now.Month;

        // Tenant is not tenant-filtered, but ignore filters defensively — this can run with no tenant context.
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);
        if (tenant is null)
            return false;

        var limit = _planPolicy.GetSmsMonthlyLimit(tenant.Plan);
        if (limit <= 0)
            return false;

        // Atomic conditional increment on the existing row.
        var updated = await _context.SmsUsages
            .Where(u => u.TenantId == tenantId && u.PeriodYear == year && u.PeriodMonth == month && u.SentCount < limit)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.SentCount, u => u.SentCount + 1), cancellationToken);
        if (updated > 0)
            return true;

        // Nothing updated: either the row is at limit, or it doesn't exist yet.
        var exists = await _context.SmsUsages
            .AnyAsync(u => u.TenantId == tenantId && u.PeriodYear == year && u.PeriodMonth == month, cancellationToken);
        if (exists)
            return false;

        var usage = new SmsUsage(tenantId, year, month);
        usage.Increment();
        _context.SmsUsages.Add(usage);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // A concurrent creator won the unique index; retry the conditional increment on their row.
            _context.Entry(usage).State = EntityState.Detached;
            var retry = await _context.SmsUsages
                .Where(u => u.TenantId == tenantId && u.PeriodYear == year && u.PeriodMonth == month && u.SentCount < limit)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.SentCount, u => u.SentCount + 1), cancellationToken);
            return retry > 0;
        }
    }
}
