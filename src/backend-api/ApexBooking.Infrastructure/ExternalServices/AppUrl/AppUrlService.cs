using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ApexBooking.Infrastructure.ExternalServices;

public class AppUrlService : IAppUrlService
{
    private readonly AppSettings _appSettings;
    private readonly ApplicationUrlsSettings _applicationUrlsSettings;

    public AppUrlService(IOptions<AppSettings> appSettings, IOptions<ApplicationUrlsSettings> applicationUrlsSettings)
    {
        _appSettings = appSettings.Value;
        _applicationUrlsSettings = applicationUrlsSettings.Value;
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

    public string GetCustomerCancellationUrl(string tenantSlug, string rawToken)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return $"{base_}/{Uri.EscapeDataString(tenantSlug)}/cancel-booking?token={Uri.EscapeDataString(rawToken)}";
    }

    public string GetRefundStatusUrl(string tenantSlug, string rawToken)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return $"{base_}/{Uri.EscapeDataString(tenantSlug)}/refund-status?token={Uri.EscapeDataString(rawToken)}";
    }

    public string GetSetupAccountUrl(string token)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(base_)
            ? $"/setup-account?token={Uri.EscapeDataString(token)}"
            : $"{base_}/setup-account?token={Uri.EscapeDataString(token)}";
    }

    public string GetPasswordResetUrl(string userId, string token, string slug)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        var path = $"/{Uri.EscapeDataString(slug)}/reset-password?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
        return string.IsNullOrEmpty(base_) ? path : $"{base_}{path}";
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

    public string GetTenantDashboardUrl(string? slug)
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');

        var path = string.IsNullOrWhiteSpace(slug)
            ? "/dashboard"
            : $"/{Uri.EscapeDataString(slug)}/dashboard";

        return string.IsNullOrEmpty(base_) ? path : $"{base_}{path}";
    }

    public string GetPlatformDashboardUrl()
    {
        var base_ = _appSettings.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(base_) ? "/admin/dashboard" : $"{base_}/admin/dashboard";
    }

    public string GetBookingQrImageUrl(string ticketToken)
    {
        // Backend API's own base URL, not the frontend's — this is fetched directly by an
        // email client, never navigated to as a page.
        var base_ = _applicationUrlsSettings.BaseUrl.TrimEnd('/');
        return $"{base_}/api/public/bookings/qr/{Uri.EscapeDataString(ticketToken)}";
    }
}