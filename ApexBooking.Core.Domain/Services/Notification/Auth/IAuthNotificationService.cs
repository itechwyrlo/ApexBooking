using System.Threading;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.Notification.Auth
{
    public interface IAuthNotificationService
    {
        Task SendPasswordResetEmailAsync(string to, string fullName, string resetUrl, CancellationToken ct);

        Task SendAccountApprovalEmailAsync(
            string to,
            string ownerFullName,
            string businessName,
            string plan,
            string trialEndsAt,
            string setupUrl,
            string slug,
            CancellationToken ct);

        Task SendRejectionEmailAsync(string to, string ownerFullName, string businessName, string reason, CancellationToken ct);

        Task SendInvitationEmailAsync(string to, string fullName, string businessName, string role, string setupUrl, CancellationToken ct);

        Task SendInvitationResentEmailAsync(string to, string fullName, string businessName, string setupUrl, CancellationToken ct);

        Task SendTenantRequestReceivedEmailAsync(string to, string ownerFullName, string businessName, string plan, CancellationToken ct);
    }
}
