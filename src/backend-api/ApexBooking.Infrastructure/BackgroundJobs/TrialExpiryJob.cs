using ApexBooking.Core.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job — sweeps for tenants whose trial has expired, or is expiring within
/// ReminderWindowDays, and raises the corresponding event on the Tenant aggregate. Depends only on
/// IUnitOfWork (Core.Domain port) — this job does no emailing or notification-writing itself; that
/// happens in SendTrialExpiredEmailHandler / SendTrialReminderEmailHandler (Core.Application),
/// triggered through the outbox the same as every other reliable event now is (see the
/// "Hangfire + Outbox Core" design spec).
/// </summary>
public class TrialExpiryJob
{
    private const int ReminderWindowDays = 3;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TrialExpiryJob> _logger;

    public TrialExpiryJob(IUnitOfWork unitOfWork, ILogger<TrialExpiryJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Run(IJobCancellationToken cancellationToken)
    {
        await ExpireDueTrialsAsync(cancellationToken.ShutdownToken);
        await SendUpcomingRemindersAsync(cancellationToken.ShutdownToken);
    }

    private async Task ExpireDueTrialsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Active tenants whose trial window has closed but aren't yet marked expired.
        var expired = await _unitOfWork.TenantRepository.GetAllAsync(
            filter: t => t.IsActive && t.TrialExpiredAt == null && t.Trial != null && t.Trial.EndDate <= now);

        // Committed per-tenant, not batched — one tenant's failure must not roll back another's
        // already-successful expiry, and must not block the rest of the sweep.
        foreach (var tenant in expired)
        {
            try
            {
                tenant.ExpireTrial();
                _unitOfWork.TenantRepository.Update(tenant);
                await _unitOfWork.CompleteAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire trial for Tenant {TenantId}.", tenant.TenantId.Value);
            }
        }
    }

    private async Task SendUpcomingRemindersAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var reminderWindow = now.AddDays(ReminderWindowDays);

        var approachingExpiry = await _unitOfWork.TenantRepository.GetAllAsync(
            filter: t => t.IsActive && t.TrialExpiredAt == null && t.Trial != null &&
                         t.Trial.EndDate > now && t.Trial.EndDate <= reminderWindow &&
                         t.TrialReminderSentAt == null);

        foreach (var tenant in approachingExpiry)
        {
            try
            {
                tenant.MarkTrialReminderSent();
                _unitOfWork.TenantRepository.Update(tenant);
                await _unitOfWork.CompleteAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send trial reminder for Tenant {TenantId}.", tenant.TenantId.Value);
            }
        }
    }
}
