namespace ApexBooking.Infrastructure.Configuration;

/// <summary>
/// Rate limiting configuration for different policies. Property shape must mirror the
/// "RateLimiting" section nesting in appsettings*.json (GlobalPolicy/AuthPolicy) — the
/// options binder matches by name and silently leaves unmatched properties at their
/// defaults, so a shape mismatch here fails quietly instead of throwing.
/// </summary>
public class RateLimitingOptions
{
    public GlobalPolicyOptions GlobalPolicy { get; set; } = new();
    public AuthPolicyOptions AuthPolicy { get; set; } = new();
}

public class GlobalPolicyOptions
{
    public int WindowSeconds { get; set; } = 60;
    public int PermitLimit { get; set; } = 100;
}

public class AuthPolicyOptions
{
    public int WindowMinutes { get; set; } = 15;
    public int PermitLimit { get; set; } = 5;
}
