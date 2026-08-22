namespace ApexBooking.Core.Domain.Services.EmailNotification;

/// <summary>
/// Thrown by <see cref="INotificationService"/> implementations when a send fails.
/// <see cref="IsTransient"/> distinguishes a retryable condition (provider rate limiting, a 5xx
/// response, a network timeout) from a permanent one (malformed recipient, provider rejected the
/// request as invalid) — see OutboxRelayService, which uses this to fail a permanent error
/// immediately instead of burning an outbox message's full retry budget on something that will
/// never succeed no matter how many times it's retried.
/// </summary>
public sealed class EmailDeliveryException : Exception
{
    public bool IsTransient { get; }

    public EmailDeliveryException(string message, bool isTransient, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }
}
