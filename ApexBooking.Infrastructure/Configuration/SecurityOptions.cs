namespace ApexBooking.Infrastructure.Configuration;

public class SecurityOptions
{
    public bool RequireHttps { get; set; } = true;
    public bool SecureCookies { get; set; } = true;
    public string CookieDomain { get; set; } = "";
}
