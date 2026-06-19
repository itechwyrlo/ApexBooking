using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.EmailNotification;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Infrastructure.BackgroundJobs;

public class TrialExpiryJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notification;
    private readonly IAppUrlService _appUrlService;
    private readonly ILogger<TrialExpiryJob> _logger;
    private readonly IPushNotificationService _pushService;
    private readonly IRealtimeNotificationService _realtimeService;

    public TrialExpiryJob(
        IUnitOfWork unitOfWork,
        INotificationService notification,
        IAppUrlService appUrlService,
        ILogger<TrialExpiryJob> logger,
        IPushNotificationService pushService,
        IRealtimeNotificationService realtimeService)
    {
        _unitOfWork = unitOfWork;
        _notification = notification;
        _appUrlService = appUrlService;
        _logger = logger;
        _pushService = pushService;
        _realtimeService = realtimeService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await SuspendExpiredTrialsAsync(cancellationToken);
        await SendExpiryRemindersAsync(cancellationToken);
    }

    private async Task SuspendExpiredTrialsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expired = await _unitOfWork.TenantRepository.GetAllAsync(
            t => t.Status == TenantStatus.Trial && t.TrialEndsAt <= now);

        foreach (var tenant in expired)
        {
            try
            {
                tenant.Suspend();
                _unitOfWork.TenantRepository.Update(tenant);
                await _unitOfWork.CompleteAsync(cancellationToken);

                var reactivateUrl = _appUrlService.GetReactivateUrl();
                var emailBody = $@"
                    <p>Hi {tenant.OwnerFullName},</p>
                    <p>Your ApexBooking trial for <strong>{tenant.BusinessName}</strong> has ended and your account has been suspended.</p>
                    <p>Your data is preserved for <strong>30 days</strong>. To reactivate your account, please add your payment details.</p>
                    <p>
                        <a href='{reactivateUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                            Reactivate Account
                        </a>
                    </p>
                    <p>If you have any questions, please contact our support team.</p>";

                await _notification.SendEmailAsync(
                    tenant.OwnerEmail,
                    "Your ApexBooking trial has ended",
                    emailBody);

                var superAdmins = await _unitOfWork.SuperAdminRepository.GetAllAsync(sa => sa.IsActive);
                var superAdminNotifications = new List<Notification>();
                foreach (var sa in superAdmins)
                {
                    var n = Notification.Create(
                        sa.SuperAdminId.Value,
                        NotificationRecipientType.SuperAdmin,
                        null,
                        NotificationEventType.TrialExpiredSuspended,
                        "Trial Expired",
                        $"Tenant {tenant.BusinessName} trial has expired and has been suspended.");
                    _unitOfWork.NotificationRepository.Add(n);
                    superAdminNotifications.Add(n);
                }

                var tenantAdmins = await _unitOfWork.UserRepository.GetUsersByRoleAsync(tenant.TenantId, UserRole.TenantAdmin);
                var tenantAdminNotifications = new List<Notification>();
                foreach (var ta in tenantAdmins)
                {
                    var n = Notification.Create(
                        ta.Id,
                        NotificationRecipientType.TenantAdmin,
                        tenant.TenantId,
                        NotificationEventType.TrialExpiredSuspended,
                        "Account Suspended",
                        $"Your ApexBooking trial for {tenant.BusinessName} has ended and your account has been suspended.");
                    _unitOfWork.NotificationRepository.Add(n);
                    tenantAdminNotifications.Add(n);
                }

                await _unitOfWork.CompleteAsync(cancellationToken);

                await _pushService.SendAsync(
                    superAdminNotifications.Select(n => n.RecipientId).ToList(),
                    "Trial Expired",
                    $"Tenant {tenant.BusinessName} trial has expired and has been suspended.",
                    NotificationEventType.TrialExpiredSuspended.ToString(),
                    cancellationToken);
                await Task.WhenAll(superAdminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));

                await _pushService.SendAsync(
                    tenantAdminNotifications.Select(n => n.RecipientId).ToList(),
                    "Account Suspended",
                    $"Your ApexBooking trial for {tenant.BusinessName} has ended and your account has been suspended.",
                    NotificationEventType.TrialExpiredSuspended.ToString(),
                    cancellationToken);
                await Task.WhenAll(tenantAdminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to suspend tenant {TenantId} after trial expiry.", tenant.TenantId.Value);

                try
                {
                    var superAdmins = await _unitOfWork.SuperAdminRepository.GetAllAsync(sa => sa.IsActive);
                    foreach (var sa in superAdmins)
                    {
                        _unitOfWork.NotificationRepository.Add(Notification.Create(
                            sa.SuperAdminId.Value,
                            NotificationRecipientType.SuperAdmin,
                            null,
                            NotificationEventType.BackgroundJobFailed,
                            "Background Job Failed",
                            "Background job failed: TrialExpiryJob encountered an error."));
                    }
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to write failure notification for TrialExpiryJob.");
                }
            }
        }
    }

    private async Task SendExpiryRemindersAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var reminderWindow = now.AddDays(3);

        var approachingExpiry = await _unitOfWork.TenantRepository.GetAllAsync(
            t => t.Status == TenantStatus.Trial &&
                 t.TrialEndsAt > now &&
                 t.TrialEndsAt <= reminderWindow &&
                 t.TrialReminderSentAt == null);

        foreach (var tenant in approachingExpiry)
        {
            try
            {
                var trialEndsAt = tenant.TrialEndsAt!.Value.ToString("MMMM d, yyyy");
                var planName = tenant.Plan.ToString();
                var billingUrl = _appUrlService.GetBillingUrl();

                var emailBody = $@"
                    <p>Hi {tenant.OwnerFullName},</p>
                    <p>Your ApexBooking trial for <strong>{tenant.BusinessName}</strong> ends on <strong>{trialEndsAt}</strong>.</p>
                    <p>You are currently on the <strong>{planName}</strong> plan. Add your payment details now to continue without interruption.</p>
                    <p>
                        <a href='{billingUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                            Add Payment Details
                        </a>
                    </p>
                    <p>Your data will be preserved if you choose not to continue.</p>";

                await _notification.SendEmailAsync(
                    tenant.OwnerEmail,
                    "Your ApexBooking trial ends in 3 days",
                    emailBody);

                tenant.MarkTrialReminderSent();
                _unitOfWork.TenantRepository.Update(tenant);
                await _unitOfWork.CompleteAsync(cancellationToken);

                var superAdmins = await _unitOfWork.SuperAdminRepository.GetAllAsync(sa => sa.IsActive);
                var superAdminNotifications = new List<Notification>();
                foreach (var sa in superAdmins)
                {
                    var n = Notification.Create(
                        sa.SuperAdminId.Value,
                        NotificationRecipientType.SuperAdmin,
                        null,
                        NotificationEventType.TrialReminderSent,
                        "Trial Expiring Soon",
                        $"Tenant {tenant.BusinessName} trial expires in 3 days. Reminder sent.");
                    _unitOfWork.NotificationRepository.Add(n);
                    superAdminNotifications.Add(n);
                }

                var tenantAdmins = await _unitOfWork.UserRepository.GetUsersByRoleAsync(tenant.TenantId, UserRole.TenantAdmin);
                var tenantAdminNotifications = new List<Notification>();
                foreach (var ta in tenantAdmins)
                {
                    var n = Notification.Create(
                        ta.Id,
                        NotificationRecipientType.TenantAdmin,
                        tenant.TenantId,
                        NotificationEventType.TrialReminderSent,
                        "Trial Expiring Soon",
                        $"Your ApexBooking trial for {tenant.BusinessName} expires in 3 days.");
                    _unitOfWork.NotificationRepository.Add(n);
                    tenantAdminNotifications.Add(n);
                }

                await _unitOfWork.CompleteAsync(cancellationToken);

                await _pushService.SendAsync(
                    superAdminNotifications.Select(n => n.RecipientId).ToList(),
                    "Trial Expiring Soon",
                    $"Tenant {tenant.BusinessName} trial expires in 3 days. Reminder sent.",
                    NotificationEventType.TrialReminderSent.ToString(),
                    cancellationToken);
                await Task.WhenAll(superAdminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));

                await _pushService.SendAsync(
                    tenantAdminNotifications.Select(n => n.RecipientId).ToList(),
                    "Trial Expiring Soon",
                    $"Your ApexBooking trial for {tenant.BusinessName} expires in 3 days.",
                    NotificationEventType.TrialReminderSent.ToString(),
                    cancellationToken);
                await Task.WhenAll(tenantAdminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send trial reminder to tenant {TenantId}.", tenant.TenantId.Value);

                try
                {
                    var superAdmins = await _unitOfWork.SuperAdminRepository.GetAllAsync(sa => sa.IsActive);
                    foreach (var sa in superAdmins)
                    {
                        _unitOfWork.NotificationRepository.Add(Notification.Create(
                            sa.SuperAdminId.Value,
                            NotificationRecipientType.SuperAdmin,
                            null,
                            NotificationEventType.BackgroundJobFailed,
                            "Background Job Failed",
                            "Background job failed: TrialExpiryJob encountered an error."));
                    }
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to write failure notification for TrialExpiryJob.");
                }
            }
        }
    }
}
