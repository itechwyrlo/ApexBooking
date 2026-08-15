namespace ApexBooking.Core.Domain.Services.Notification;

/// <summary>Sends an SMS (08 §2). Always call <c>ISmsQuotaService.TryConsumeAsync</c> first — SMS is quota-gated.</summary>
public interface ISmsService
{
    Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);
}
