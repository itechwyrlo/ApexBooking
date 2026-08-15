namespace ApexBooking.Core.Domain.Services.Notification.Tenancy
{
    public interface ITenantLifecycleNotificationService
    {
        Task SendTrialExpiredEmailAsync(
            string to,
            string ownerName,
            string businessName,
            string reactivateUrl,
            CancellationToken ct);

        Task SendTrialReminderEmailAsync(
            string to,
            string ownerName,
            string businessName,
            DateTime trialEndsAtUtc,
            string billingUrl,
            CancellationToken ct);
    }
}
