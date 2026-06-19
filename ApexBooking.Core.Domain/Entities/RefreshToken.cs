using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class RefreshToken : ITenantEntity
{
    public RefreshTokenId Id { get; protected set; } = default!;
    public Guid UserId { get; protected set; } = default!;
    public TenantId TenantId { get; protected set; } = default!;
    public string Token { get; private set; } = string.Empty;
    public bool IsUsed { get; private set; } = false;
    public bool IsRevoked { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiryDate { get; private set; }

    protected RefreshToken() { }

    [SetsRequiredMembers]
    public RefreshToken(Guid userId, TenantId tenantId, string token, DateTime createdAt)
    {
        Id = new RefreshTokenId(Guid.NewGuid());
        UserId = userId;
        TenantId = tenantId;
        Token = token;
        CreatedAt = createdAt;
        ExpiryDate = createdAt.AddDays(7);
    }

    public void MarkAsUsed(DateTime utcNow)
    {
        if (IsRevoked)
            throw new BusinessRuleBrokenException("Cannot use a revoked token.");

        if (IsUsed)
            throw new BusinessRuleBrokenException("Token has already been used.");

        if (ExpiryDate <= utcNow)
            throw new BusinessRuleBrokenException("Cannot use an expired token.");

        IsUsed = true;
    }

    public void Revoke()
    {
        if (IsRevoked) return;
        IsRevoked = true;
    }
}
