using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job — reclaims PendingPayment bookings whose checkout has sat unpaid past
/// the stale window (design spec: 2026-08-18-payment-booking-security-hardening-design.md, Module
/// 2). Same per-tenant try/catch shape as TrialExpiryJob: one tenant's failure, including a
/// DbUpdateConcurrencyException from a webhook that just confirmed payment on one of its bookings
/// (see Booking.RowVersion), does not block the rest of the sweep.
/// </summary>
public class ExpireStalePendingBookingsJob
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpireStalePendingBookingsJob> _logger;

    public ExpireStalePendingBookingsJob(IUnitOfWork unitOfWork, ILogger<ExpireStalePendingBookingsJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Run(IJobCancellationToken cancellationToken)
    {
        var ct = cancellationToken.ShutdownToken;
        var cutoffUtc = DateTime.UtcNow - StaleAfter;

        var tenants = await _unitOfWork.TenantRepository.GetTenantsWithStalePendingBookingsAsync(cutoffUtc, ct);

        foreach (var tenant in tenants)
        {
            try
            {
                var staleBookings = tenant.Bookings
                    .Where(b => b.Status == BookingStatus.PendingPayment && b.CreatedAt <= cutoffUtc)
                    .ToList();

                foreach (var booking in staleBookings)
                {
                    booking.ExpirePendingPayment();
                }

                _unitOfWork.TenantRepository.Update(tenant);
                await _unitOfWork.CompleteAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // A webhook confirmed payment on one of this tenant's bookings between our read and
                // this SaveChanges — RowVersion caught the conflict. Those bookings are no longer
                // actually stale; nothing to do here, the next sweep re-evaluates from scratch.
                _logger.LogInformation(ex,
                    "Skipped expiring stale bookings for Tenant {TenantId}: a concurrent payment confirmation won the race.",
                    tenant.TenantId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire stale pending bookings for Tenant {TenantId}.", tenant.TenantId.Value);
            }
        }
    }
}
