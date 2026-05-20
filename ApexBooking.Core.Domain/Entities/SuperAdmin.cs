using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Domain.Entities;

public class SuperAdmin : IdentityUser<Guid>, IAggregateRoot
{
    public SuperAdminId SuperAdminId { get; protected set; } = default!;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<SuperAdminRefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<SuperAdminRefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    protected SuperAdmin() { }

    [SetsRequiredMembers]
    public SuperAdmin(string fullName, string email, string passwordHash)
    {
        SuperAdminId = new SuperAdminId(Guid.NewGuid());
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static SuperAdmin Create(string fullName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new BusinessRuleBrokenException("Full name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleBrokenException("Email is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new BusinessRuleBrokenException("Password hash is required.");

        return new SuperAdmin(fullName, email, passwordHash);
    }

    public void EnsureIsActive()
    {
        if (!IsActive)
            throw new BusinessRuleBrokenException("Account is not active.");
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleBrokenException("Super Admin is already inactive.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            throw new BusinessRuleBrokenException("Super Admin is already active.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new BusinessRuleBrokenException("Password hash is required.");

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRefreshToken(string token, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new BusinessRuleBrokenException("Token is required.");

        if (utcNow == default)
            throw new BusinessRuleBrokenException("UTC time is required.");

        if (_refreshTokens.Any(r => r.Token == token))
            throw new BusinessRuleBrokenException("Token already exists.");

        _refreshTokens.Add(new SuperAdminRefreshToken(SuperAdminId, token, utcNow));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RotateRefreshToken(string oldTokenValue, string newTokenValue, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(oldTokenValue))
            throw new BusinessRuleBrokenException("Old token value is required.");

        if (string.IsNullOrWhiteSpace(newTokenValue))
            throw new BusinessRuleBrokenException("New token value is required.");

        if (utcNow == default)
            throw new BusinessRuleBrokenException("UTC time is required.");

        var oldToken = _refreshTokens.SingleOrDefault(r => r.Token == oldTokenValue);
        if (oldToken == null)
            throw new BusinessRuleBrokenException("Refresh token not found.");

        if (oldToken.IsUsed || oldToken.IsRevoked)
        {
            foreach (var t in _refreshTokens)
                t.Revoke();

            throw new BusinessRuleBrokenException("Refresh token reuse detected. All tokens revoked.");
        }

        if (oldToken.ExpiryDate <= utcNow)
            throw new BusinessRuleBrokenException("Refresh token has expired.");

        oldToken.MarkAsUsed(utcNow);
        _refreshTokens.Add(new SuperAdminRefreshToken(SuperAdminId, newTokenValue, utcNow));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokeRefreshToken(string tokenValue)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
            throw new BusinessRuleBrokenException("Token value is required.");

        var token = _refreshTokens.SingleOrDefault(r => r.Token == tokenValue);
        if (token == null)
            throw new BusinessRuleBrokenException("Token not found.");

        token.Revoke();
        UpdatedAt = DateTime.UtcNow;
    }
}


