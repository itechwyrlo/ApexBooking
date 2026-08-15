namespace ApexBooking.Core.Domain.Interfaces
{
    public interface IAppUrlService
    {
        string GetPaymentReturnUrl(string tenantSlug, Guid bookingId);
        string GetPaymentCancelUrl(string tenantSlug, Guid bookingId);
        string GetCustomerCancellationUrl(string tenantSlug, string rawToken);
        string GetRefundStatusUrl(string tenantSlug, string rawToken);
        string GetSetupAccountUrl(string token);

        /// <summary>
        /// Slug is required — every current caller (owner/staff setup invitations) already knows
        /// it, and the link needs to land on /:slug/reset-password to match the rest of the tenant
        /// URL scheme (see the tenant-slug-routing design). No caller currently has a slug-less
        /// case; add an overload if one legitimately arises rather than making this optional.
        /// </summary>
        string GetPasswordResetUrl(string userId, string token, string slug);
        /// <summary>No current call site. Reserved for when email verification is added to tenant admin setup flow.</summary>
        string GetEmailVerificationUrl(string token);
        string GetReactivateUrl();
        string GetBillingUrl();

        /// <summary>Where a tenant user lands after completing account setup. Falls back to the bare dashboard when the tenant is unknown.</summary>
        string GetTenantDashboardUrl(string? slug);

        /// <summary>Where a platform admin lands after completing account setup.</summary>
        string GetPlatformDashboardUrl();

        /// <summary>
        /// Backend-hosted (not frontend-hosted) boarding-pass QR image, referenced directly by
        /// its URL rather than embedded — Brevo's transactional email API doesn't support inline
        /// CID images, and most email clients strip data: URI images outright, so a real hosted
        /// URL is the only approach that reliably renders in an email client.
        /// </summary>
        string GetBookingQrImageUrl(string ticketToken);
    }
}