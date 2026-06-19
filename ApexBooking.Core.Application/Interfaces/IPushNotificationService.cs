namespace ApexBooking.Core.Application.Interfaces;

public interface IPushNotificationService
{
    Task SendAsync(IReadOnlyList<Guid> recipientIds, string title, string message, string eventType, CancellationToken ct);
}
