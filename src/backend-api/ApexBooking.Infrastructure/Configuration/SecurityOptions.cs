namespace ApexBooking.Infrastructure.Configuration;

public class SecurityOptions
{
    public bool RequireHttps { get; set; } = true;
    public bool SecureCookies { get; set; } = true;
    public string CookieDomain { get; set; } = "";
    public string TicketSigningKey { get; set; } = "";
    // Deliberately separate from TicketSigningKey — the admission ticket token and the
    // cancellation token must never be interchangeable (see HmacCancellationTokenService).
    public string CancellationTokenSigningKey { get; set; } = "";
}
