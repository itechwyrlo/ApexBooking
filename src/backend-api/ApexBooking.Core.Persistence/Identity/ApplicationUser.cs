using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Persistence.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<TenantMember> _tenantMembers = [];
    public IReadOnlyCollection<TenantMember> TenantMembers => _tenantMembers.AsReadOnly();

    // Required by EF Core and UserManager internals
    private ApplicationUser()
    {
    }

    [SetsRequiredMembers]
    private ApplicationUser(
        string email,
        string firstName,
        string lastName,
        bool isPlatformAdmin)
    {
        Id = Guid.NewGuid();

        UserName = email;
        NormalizedUserName = email.ToUpperInvariant();

        Email = email;
        NormalizedEmail = email.ToUpperInvariant();

        FirstName = firstName;
        LastName = lastName;
        IsPlatformAdmin = isPlatformAdmin;
        IsActive = false;
        EmailConfirmed = false;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Regular Staff or Business Owners
    public static ApplicationUser Create(string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleBrokenException("User email cannot be empty.");
            
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new BusinessRuleBrokenException("User names cannot be empty.");

        return new ApplicationUser(email.Trim(), firstName.Trim(), lastName.Trim(), isPlatformAdmin: false);
    }

    // Internal Dev/Admin Team
    public static ApplicationUser CreatePlatformAdmin(string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleBrokenException("Admin email cannot be empty.");

        return new ApplicationUser(email.Trim(), firstName.Trim(), lastName.Trim(), isPlatformAdmin: true);
    }

    // --- Core Properties ---
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsPlatformAdmin { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? PhotoUrl { get; private set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    // --- Account state ---

    public void MarkAsActive(DateTime utcNow)
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = utcNow;
    }

    public void MarkAsInactive(DateTime utcNow)
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = utcNow;
    }

    public void MarkEmailAsConfirmed(DateTime utcNow)
    {
        if (EmailConfirmed)
            return;

        EmailConfirmed = true;
        UpdatedAt = utcNow;
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        LastLoginAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new BusinessRuleBrokenException("Name cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        UpdatedAt = utcNow;
    }

    public void UpdatePhoto(string? photoUrl, DateTime utcNow)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = utcNow;
    }

    // --- Refresh tokens ---

    public RefreshToken AddRefreshToken(string token, DateTime utcNow)
    {
        if (_refreshTokens.Any(x => x.RefreshTokenSecret == token))
            throw new BusinessRuleBrokenException("Refresh token already exists.");

        var refreshToken = new RefreshToken(Id, token, utcNow);
        _refreshTokens.Add(refreshToken);
        UpdatedAt = utcNow;

        return refreshToken;
    }

    public RefreshToken RotateRefreshToken(string currentToken, string newToken, DateTime utcNow)
    {
        var existing = _refreshTokens.FirstOrDefault(x => x.RefreshTokenSecret == currentToken)
            ?? throw new BusinessRuleBrokenException("Refresh token was not found.");

        var rotated = existing.Rotate(newToken, utcNow);
        _refreshTokens.Add(rotated);
        UpdatedAt = utcNow;

        return rotated;
    }

    public void RevokeRefreshToken(string token, DateTime utcNow)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(x => x.RefreshTokenSecret == token);

        if (refreshToken is null)
            return;

        refreshToken.Revoke();
        UpdatedAt = utcNow;
    }

    public void RevokeAllRefreshTokens(DateTime utcNow)
    {
        foreach (var refreshToken in _refreshTokens)
            refreshToken.Revoke();

        UpdatedAt = utcNow;
    }
}
