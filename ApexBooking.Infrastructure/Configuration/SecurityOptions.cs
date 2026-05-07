namespace ApexBooking.Infrastructure.Configuration;

/// <summary>
/// Security-related configuration
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// Whether HTTPS is required for the application
    /// </summary>
    public bool RequireHttps { get; set; } = true;
    
    /// <summary>
    /// Cookie domain for secure cookies
    /// </summary>
    public string CookieDomain { get; set; } = "";
    
    /// <summary>
    /// Whether to include strict transport security header
    /// </summary>
    public bool EnableHsts { get; set; } = true;
    
    /// <summary>
    /// HSTS max age in seconds
    /// </summary>
    public int HstsMaxAgeSeconds { get; set; } = 31536000; // 1 year
    
    /// <summary>
    /// Whether to include X-Content-Type-Options: nosniff header
    /// </summary>
    public bool EnableContentTypeOptions { get; set; } = true;
    
    /// <summary>
    /// Whether to include X-Frame-Options: DENY header
    /// </summary>
    public bool EnableFrameOptions { get; set; } = true;
    
    /// <summary>
    /// Whether to include Content-Security-Policy header
    /// </summary>
    public bool EnableCsp { get; set; } = true;
    
    /// <summary>
    /// Content-Security-Policy header value
    /// </summary>
    public string CspPolicy { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
}
