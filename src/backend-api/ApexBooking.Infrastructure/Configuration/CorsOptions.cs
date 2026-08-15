namespace ApexBooking.Infrastructure.Configuration;

/// <summary>
/// CORS policy configuration
/// </summary>
public class CorsOptions
{
    public string AllowedOrigins { get; set; } = "";
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE" };
    public List<string> AllowedHeaders { get; set; } = new() { "Authorization", "Content-Type", "X-Requested-With" };
    public bool AllowCredentials { get; set; } = true;
    public int MaxAgeSeconds { get; set; } = 600;
}
