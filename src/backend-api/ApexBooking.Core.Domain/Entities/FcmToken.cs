using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Domain.Entities;

public class FcmToken : IAggregateRoot
{
    public FcmTokenId FcmTokenId { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected FcmToken() { }

    private FcmToken(Guid userId, string token)
    {
        FcmTokenId = new FcmTokenId(Guid.NewGuid());
        UserId = userId;
        Token = token;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static FcmToken Create(Guid userId, string token)
    {
        if (userId == Guid.Empty)
            throw new BusinessRuleBrokenException("UserId is required.");

        if (string.IsNullOrWhiteSpace(token))
            throw new BusinessRuleBrokenException("FCM token is required.");

        return new FcmToken(userId, token);
    }

    public void UpdateToken(string newToken)
    {
        if (string.IsNullOrWhiteSpace(newToken))
            throw new BusinessRuleBrokenException("FCM token is required.");

        Token = newToken;
        UpdatedAt = DateTime.UtcNow;
    }
}
