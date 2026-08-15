using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services;

// Data access port for RefundRequest rows — same shape as IOutboxStore/ISmsQuotaService:
// RefundRequest is not an IAggregateRoot, so there's no generic repository for it.
public interface IRefundRequestStore
{
    Task AddAsync(RefundRequest request, CancellationToken cancellationToken = default);

    Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Any status, terminal or not — for the customer-facing refund-status page, which needs to
    // show the *outcome*, not just what's still pending review.
    Task<RefundRequest?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    // Still PendingReview — the review page's list. Paged: (Items, Total).
    Task<(IReadOnlyList<RefundRequest> Items, int Total)> GetPendingForTenantAsync(
        TenantId tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // The Refund Log widget's data — refunds that were actually confirmed and sent. Rejected
    // requests aren't "processed" in this sense — no money moved for those. Most-recently-processed
    // first, capped at `limit` (no pagination UI for this widget).
    Task<IReadOnlyList<RefundRequest>> GetProcessedForTenantAsync(
        TenantId tenantId, int limit, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefundRequest request, CancellationToken cancellationToken = default);
}
