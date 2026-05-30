namespace ApexBooking.Core.Domain.Interfaces
{
    public interface IAppUrlService
    {
        string GetPaymentReturnUrl(string tenantSlug, Guid bookingId);
        string GetPaymentCancelUrl(string tenantSlug, Guid bookingId);
        string GetGuestCancellationUrl(string tenantSlug, string rawToken);
        string GetSetupAccountUrl(string token);
        string GetPasswordResetUrl(string userId, string token);
        /// <summary>No current call site. Reserved for when email verification is added to tenant admin setup flow.</summary>
        string GetEmailVerificationUrl(string token);
        string GetReactivateUrl();
        string GetBillingUrl();
    }
}