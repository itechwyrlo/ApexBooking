namespace ApexBooking.Infrastructure.Configuration
{
    // Binds "ApplicationUrls" — the backend API's own public base URL, distinct from
    // AppSettings.FrontendBaseUrl (the SPA's base URL). Needed for links that must be
    // fetched directly by something outside the browser/SPA — e.g. an email client
    // loading the boarding-pass QR image, which can't go through the frontend at all.
    public class ApplicationUrlsSettings
    {
        public string BaseUrl { get; set; } = default!;
    }
}
