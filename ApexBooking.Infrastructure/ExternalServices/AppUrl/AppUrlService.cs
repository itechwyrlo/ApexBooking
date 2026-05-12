using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ApexBooking.Infrastructure.ExternalServices;

public class AppUrlService : IAppUrlService
{
    private readonly AppSettings _appSettings;

    public AppUrlService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public string GetPaymentReturnUrl(string tenantSlug, Guid bookingId)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return $"{base_}/book/{tenantSlug}/payment/success?bookingId={bookingId}";
    }

    public string GetPaymentCancelUrl(string tenantSlug, Guid bookingId)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return $"{base_}/book/{tenantSlug}/new?cancelled=true&bookingId={bookingId}";
    }

    public string GetGuestCancellationUrl(string tenantSlug, string rawToken)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return $"{base_}/book/{tenantSlug}/cancel-booking?token={Uri.EscapeDataString(rawToken)}";
    }

    public string GetSetupAccountUrl(string token)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(base_)
            ? $"/setup-account?token={Uri.EscapeDataString(token)}"
            : $"{base_}/setup-account?token={Uri.EscapeDataString(token)}";
    }

    public string GetPasswordResetUrl(string token)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(base_)
            ? $"/reset-password?token={Uri.EscapeDataString(token)}"
            : $"{base_}/reset-password?token={Uri.EscapeDataString(token)}";
    }

    public string GetEmailVerificationUrl(string token)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(base_)
            ? $"/verify-account?token={Uri.EscapeDataString(token)}"
            : $"{base_}/verify-account?token={Uri.EscapeDataString(token)}";
    }

    public string GetReactivateUrl()
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(base_) ? "/reactivate" : $"{base_}/reactivate";
    }

    public string GetBillingUrl()
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(base_) ? "/billing" : $"{base_}/billing";
    }
}