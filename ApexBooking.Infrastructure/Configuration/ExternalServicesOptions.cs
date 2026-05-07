namespace ApexBooking.Infrastructure.Configuration;

/// <summary>
/// External service credentials configuration
/// </summary>
public class ExternalServicesOptions
{
    public BrevoSmtpOptions BrevoSmtp { get; set; } = new();
    public GoogleCalendarOptions GoogleCalendar { get; set; } = new();
    public MicrosoftGraphOptions MicrosoftGraph { get; set; } = new();
}

public class BrevoSmtpOptions
{
    public string Key { get; set; } = default!;
}

public class GoogleCalendarOptions
{
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string RedirectUri { get; set; } = default!;
}

public class MicrosoftGraphOptions
{
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string RedirectUri { get; set; } = default!;
    public string SendAsMailbox { get; set; } = default!;
    public List<string> Scopes { get; set; } = new();
}
