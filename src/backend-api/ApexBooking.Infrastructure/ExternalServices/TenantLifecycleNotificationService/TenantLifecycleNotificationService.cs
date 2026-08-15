using ApexBooking.Core.Domain.Services.EmailNotification;
using ApexBooking.Core.Domain.Services.Notification.Tenancy;

namespace ApexBooking.Infrastructure.ExternalServices.TenantLifecycleNotificationService
{
    public class TenantLifecycleNotificationService : ITenantLifecycleNotificationService
    {
        private readonly INotificationService _notification;

        public TenantLifecycleNotificationService(INotificationService notification)
        {
            _notification = notification;
        }

        public Task SendTrialExpiredEmailAsync(
            string to,
            string ownerName,
            string businessName,
            string reactivateUrl,
            CancellationToken ct)
        {
            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #0d6efd; padding-bottom: 10px;'>Your Trial Has Ended</h2>

                <p>Hi <strong>{ownerName}</strong>,</p>

                <p>Your ApexBooking trial for <strong>{businessName}</strong> has ended.</p>

                <p>You can still sign in and view your data, but accepting new bookings and paid features are paused until you add your payment details.</p>

                <p>
                    <a href='{reactivateUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                        Reactivate Account
                    </a>
                </p>

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    If you have any questions, please contact our support team.
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: "Your ApexBooking trial has ended",
                content: body);
        }

        public Task SendTrialReminderEmailAsync(
            string to,
            string ownerName,
            string businessName,
            DateTime trialEndsAtUtc,
            string billingUrl,
            CancellationToken ct)
        {
            var formattedDate = trialEndsAtUtc.ToString("MMMM d, yyyy");

            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #0d6efd; padding-bottom: 10px;'>Your Trial Ends Soon</h2>

                <p>Hi <strong>{ownerName}</strong>,</p>

                <p>Your ApexBooking trial for <strong>{businessName}</strong> ends on <strong>{formattedDate}</strong>.</p>

                <p>Add your payment details now to continue without interruption.</p>

                <p>
                    <a href='{billingUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                        Add Payment Details
                    </a>
                </p>

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    Your data will be preserved if you choose not to continue.
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: "Your ApexBooking trial ends soon",
                content: body);
        }
    }
}
