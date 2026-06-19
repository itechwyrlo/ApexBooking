using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Services.EmailNotification;
using ApexBooking.Core.Domain.Services.Notification.Auth;

namespace ApexBooking.Infrastructure.ExternalServices.AuthNotificationService
{
    public class AuthNotificationService : IAuthNotificationService
    {
        private readonly INotificationService _notification;

        public AuthNotificationService(INotificationService notification)
        {
            _notification = notification;
        }

        public Task SendPasswordResetEmailAsync(string to, string fullName, string resetUrl, CancellationToken ct)
        {
            var body = $@"
            <h1>Reset Your Password</h1>
            <p>Hi {fullName}, click the link below to reset your password:</p>
            <a href='{resetUrl}' style='padding: 10px 20px; background: blue; color: white; text-decoration: none;'>
                Reset Password
            </a>
            <p>If the button doesn't work, copy and paste this link: {resetUrl}</p>
            <p>This link expires in 1 hour.</p>";

            return _notification.SendEmailAsync(to, "Reset Your ApexBooking Password", body);
        }

        public Task SendAccountApprovalEmailAsync(
            string to,
            string ownerFullName,
            string businessName,
            string plan,
            string trialEndsAt,
            string setupUrl,
            string slug,
            CancellationToken ct)
        {
            var body = $@"
            <p>Hi {ownerFullName},</p>
            <p>Great news — your ApexBooking account for <strong>{businessName}</strong> has been approved!</p>
            <p>You are on the <strong>{plan}</strong> plan with a free trial that runs until <strong>{trialEndsAt}</strong>.</p>
            <p>Click the button below to set up your password and activate your account:</p>
            <p>
                <a href='{setupUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                    Set Up Your Account
                </a>
            </p>
            <p>This link expires in <strong>72 hours</strong>.</p>
            <p>Your booking page will be available at: <strong>/book/{slug}</strong></p>";

            return _notification.SendEmailAsync(to, "Your ApexBooking account is ready — set up your password", body);
        }

        public Task SendRejectionEmailAsync(string to, string ownerFullName, string businessName, string reason, CancellationToken ct)
        {
            var body = $@"
            <p>Hi {ownerFullName},</p>
            <p>Thank you for your interest in ApexBooking.</p>
            <p>After reviewing your request for <strong>{businessName}</strong>, we are unable to approve your account at this time.</p>
            <p><strong>Reason:</strong> {reason}</p>
            <p>If you believe this is an error or would like to discuss further, please contact our support team.</p>";

            return _notification.SendEmailAsync(to, "Your ApexBooking access request", body);
        }

        public Task SendInvitationEmailAsync(string to, string fullName, string businessName, string role, string setupUrl, CancellationToken ct)
        {
            var body = $@"
            <p>Hi {fullName},</p>
            <p>You have been invited to join <strong>{businessName}</strong> on ApexBooking as <strong>{role}</strong>.</p>
            <p>Click the button below to set up your password and activate your account:</p>
            <p>
                <a href='{setupUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                    Set Up Your Account
                </a>
            </p>
            <p>This link expires in <strong>72 hours</strong>. If it expires, contact your administrator to resend the invitation.</p>
            <p>If you did not expect this invitation, you can safely ignore this email.</p>";

            return _notification.SendEmailAsync(to, $"You've been invited to {businessName}", body);
        }

        public Task SendInvitationResentEmailAsync(string to, string fullName, string businessName, string setupUrl, CancellationToken ct)
        {
            var body = $@"
            <p>Hi {fullName},</p>
            <p>Your invitation to <strong>{businessName}</strong> on ApexBooking has been resent.</p>
            <p>Click the button below to set up your password and activate your account:</p>
            <p>
                <a href='{setupUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                    Set Up Your Account
                </a>
            </p>
            <p>This link expires in <strong>72 hours</strong>.</p>";

            return _notification.SendEmailAsync(to, $"Invitation resent — {businessName}", body);
        }

        public Task SendTenantRequestReceivedEmailAsync(string to, string ownerFullName, string businessName, string plan, CancellationToken ct)
        {
            var body = $@"
            <p>Hi {ownerFullName},</p>
            <p>Thank you for your interest in ApexBooking. We have received your request for a <strong>{plan}</strong> account for <strong>{businessName}</strong>.</p>
            <p>Our team will review your request and get back to you shortly.</p>
            <p>If you have any questions in the meantime, please contact our support team.</p>";

            return _notification.SendEmailAsync(to, "Your ApexBooking access request has been received", body);
        }
    }
}
