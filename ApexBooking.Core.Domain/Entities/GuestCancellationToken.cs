using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class GuestCancellationToken : IAggregateRoot, ITenantEntity
{
    public GuestCancellationTokenId TokenId { get; private set; } = default!;
    public GuestId GuestId { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Guest? Guest { get; private set; }

    protected GuestCancellationToken() { }

    private GuestCancellationToken(
        GuestId guestId,
        TenantId tenantId,
        string tokenHash,
        DateTime expiresAt)
    {
        TokenId = new GuestCancellationTokenId(Guid.NewGuid());
        GuestId = guestId;
        TenantId = tenantId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        IsUsed = false;
        CreatedAt = DateTime.UtcNow;
    }

    public static GuestCancellationToken Create(
        GuestId guestId,
        TenantId tenantId,
        string tokenHash,
        DateTime expiresAt)
    {
        if (guestId is null)
            throw new BusinessRuleBrokenException("Guest is required.");
        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new BusinessRuleBrokenException("Token hash is required.");

        return new GuestCancellationToken(guestId, tenantId, tokenHash, expiresAt);
    }

    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new BusinessRuleBrokenException("Cancellation token has already been used.");
        if (ExpiresAt <= DateTime.UtcNow)
            throw new BusinessRuleBrokenException("Cancellation token has expired.");
        IsUsed = true;
    }

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

    public void EnsureNotUsed()
    {
        if (IsUsed)
            throw new BusinessRuleBrokenException("This cancellation link has already been used.");
    }

    public void EnsureNotExpired()
    {
        if (IsExpired)
            throw new BusinessRuleBrokenException("This cancellation link has expired.");
    }
}
