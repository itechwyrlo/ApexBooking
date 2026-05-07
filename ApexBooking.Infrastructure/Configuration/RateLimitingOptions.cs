namespace ApexBooking.Infrastructure.Configuration;

/// <summary>
/// Rate limiting configuration for different policies
/// </summary>
public class RateLimitingOptions
{
    public int GlobalWindowSeconds { get; set; } = 60;
    public int GlobalPermitLimit { get; set; } = 100;
    
    public int AuthWindowMinutes { get; set; } = 15;
    public int AuthPermitLimit { get; set; } = 5;
}
