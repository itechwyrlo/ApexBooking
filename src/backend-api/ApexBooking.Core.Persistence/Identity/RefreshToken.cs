using System.Diagnostics.CodeAnalysis;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Persistence.Identity;

/// <summary>
/// Represents a rotating refresh token for an authenticated user.
/// </summary>
public sealed class RefreshToken
{
    protected RefreshToken()
    {
    }

    [SetsRequiredMembers]
    public RefreshToken(
        Guid applicationUserId,
        string token,
        DateTime createdAt)
    {
        if (applicationUserId == Guid.Empty)
            throw new BusinessRuleBrokenException("Application user is required.");

        if (string.IsNullOrWhiteSpace(token))
            throw new BusinessRuleBrokenException("Refresh token is required.");

        Id = Guid.NewGuid();
        ApplicationUserId = applicationUserId;
        RefreshTokenSecret = token.Trim();
        CreatedAt = createdAt;
        ExpiryDate = createdAt.AddDays(7);
    }

    public Guid Id { get; private set; }

    public Guid ApplicationUserId { get; private set; }

    public string RefreshTokenSecret { get; private set; } = string.Empty;

    public bool IsUsed { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiryDate { get; private set; }

    public ApplicationUser ApplicationUser { get; private set; } = default!;

    public bool IsExpired(DateTime utcNow)
    {
        return utcNow >= ExpiryDate;
    }

    public bool CanBeUsed(DateTime utcNow)
    {
        return !IsRevoked &&
               !IsUsed &&
               !IsExpired(utcNow);
    }

    public void MarkAsUsed(DateTime utcNow)
    {
        if (IsRevoked)
            throw new BusinessRuleBrokenException("Refresh token has been revoked.");

        if (IsUsed)
            throw new BusinessRuleBrokenException("Refresh token has already been used.");

        if (IsExpired(utcNow))
            throw new BusinessRuleBrokenException("Refresh token has expired.");

        IsUsed = true;
    }

    public void Revoke()
    {
        if (IsRevoked)
            return;

        IsRevoked = true;
    }

    /// <summary>
    /// Consumes this token and issues its replacement. The current secret is
    /// marked as used and revoked so it can never be presented again.
    /// </summary>
    public RefreshToken Rotate(string newRefreshTokenSecret, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(newRefreshTokenSecret))
            throw new BusinessRuleBrokenException("Refresh token is required.");

        MarkAsUsed(utcNow);
        Revoke();

        return new RefreshToken(ApplicationUserId, newRefreshTokenSecret, utcNow);
    }
}