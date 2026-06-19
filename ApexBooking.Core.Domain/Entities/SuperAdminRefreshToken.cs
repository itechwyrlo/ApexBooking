using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.Entities;

public class SuperAdminRefreshToken
{
    public SuperAdminRefreshTokenId Id { get; protected set; } = default!;
    public SuperAdminId SuperAdminId { get; protected set; } = default!;
    public string Token { get; private set; } = string.Empty;
    public bool IsUsed { get; private set; } = false;
    public bool IsRevoked { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiryDate { get; private set; }

    protected SuperAdminRefreshToken() { }

    [SetsRequiredMembers]
    public SuperAdminRefreshToken(SuperAdminId superAdminId, string token, DateTime createdAt)
    {
        Id = new SuperAdminRefreshTokenId(Guid.NewGuid());
        SuperAdminId = superAdminId;
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
