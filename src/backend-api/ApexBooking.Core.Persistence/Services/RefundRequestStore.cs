using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Services;

public sealed class RefundRequestStore : IRefundRequestStore
{
    private static readonly RefundRequestStatus[] TerminalStatuses =
    [
        RefundRequestStatus.Refunded,
        RefundRequestStatus.Rejected
    ];

    private readonly ApexBookingDbContext _context;

    public RefundRequestStore(ApexBookingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        _context.RefundRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RefundRequests
            .IgnoreQueryFilters() // caller-supplied id is already tenant-scoped by the handler's own auth check
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<RefundRequest?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await _context.RefundRequests
            .IgnoreQueryFilters()
            .Where(r => r.BookingId == bookingId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<RefundRequest> Items, int Total)> GetPendingForTenantAsync(
        TenantId tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.RefundRequests
            .Where(r => r.TenantId == tenantId && !TerminalStatuses.Contains(r.Status))
            .OrderBy(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private static readonly RefundRequestStatus[] ProcessedStatuses =
    [
        RefundRequestStatus.Refunded
    ];

    public async Task<IReadOnlyList<RefundRequest>> GetProcessedForTenantAsync(
        TenantId tenantId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.RefundRequests
            .Where(r => r.TenantId == tenantId && ProcessedStatuses.Contains(r.Status))
            .OrderByDescending(r => r.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        _context.RefundRequests.Update(request);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
