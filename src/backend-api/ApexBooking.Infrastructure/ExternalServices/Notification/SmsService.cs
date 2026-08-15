using ApexBooking.Core.Domain.Services.Notification;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Infrastructure.ExternalServices.Notification;

/// <summary>
/// SMS delivery (08 §2). No SMS gateway is wired for MVP and message templating is a later spec
/// (08 §6 open #4), so this logs the intended send. Swap in the real gateway call when it lands —
/// the quota gate (<c>ISmsQuotaService</c>) already runs in the notification handlers.
/// </summary>
public sealed class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger) => _logger = logger;

    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SMS (stub — no gateway wired) to {Phone}: {Message}", toPhoneNumber, message);
        return Task.CompletedTask;
    }
}
