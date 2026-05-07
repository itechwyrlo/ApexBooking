using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using Microsoft.AspNetCore.Identity;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class User : IdentityUser<Guid>, IAggregateRoot, ITenantEntity
{
    public TenantId TenantId { get; protected set; } = default!;
    public string FullName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public string? InvitationToken { get; private set; }
    public DateTime? InvitationExpiresAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? EmailConfirmationToken {get; private set;}

    private readonly List<RefreshToken> _refreshTokens = new List<RefreshToken>();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<UserResourceAssignment> _userResourceAssignments = new List<UserResourceAssignment>();
    public IReadOnlyCollection<UserResourceAssignment> UserResourceAssignments => _userResourceAssignments.AsReadOnly();

    protected User() { }

    [SetsRequiredMembers]
    public User(TenantId tenantId, string fullName, string email, UserRole role, UserStatus status = UserStatus.Active)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        FullName = fullName;
        UserName = email;
        Email = email;
        Role = role;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    public static User Create(TenantId tenantId, string fullName, string email, UserRole role)
    {
        if (tenantId == null)
            throw new BusinessRuleBrokenException("Tenant ID is required.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new BusinessRuleBrokenException("Full name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleBrokenException("Email is required.");

        return new User(tenantId, fullName, email, role);
    }

    public static User CreateInvitedUser(TenantId tenantId, string fullName, string email, UserRole role, string invitationToken)
    {
        var user = Create(tenantId, fullName, email, role);
        user.Status = UserStatus.Invited;
        user.InvitationToken = invitationToken;
        user.InvitationExpiresAt = DateTime.UtcNow.AddDays(3); // 72 hours
        return user;
    }

    public void MarkEmailVerified()
    {
        if (EmailVerifiedAt.HasValue)
            throw new BusinessRuleBrokenException("Email is already verified.");

        EmailConfirmed = true;
        EmailVerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status == UserStatus.Active)
            throw new BusinessRuleBrokenException("User is already active.");

        Status = UserStatus.Active;
        InvitationToken = null;
        InvitationExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == UserStatus.Inactive)
            throw new BusinessRuleBrokenException("User is already inactive.");

        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(UserRole newRole)
    {
        if (newRole == Role)
            throw new BusinessRuleBrokenException("User already has this role.");

        // If changing from Staff to non-Staff, remove resource assignments
        if (Role == UserRole.Staff && newRole != UserRole.Staff)
        {
            _userResourceAssignments.Clear();
        }

        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid FindUserIdByRefreshToken(string tokenValue)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
            throw new BusinessRuleBrokenException("Token value is required.");

        var refreshToken = _refreshTokens.SingleOrDefault(r => r.Token == tokenValue);
        if (refreshToken == null)
            throw new BusinessRuleBrokenException("Token not found.");

        return refreshToken.UserId;
    }

    public void AddRefreshToken(string token, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new BusinessRuleBrokenException("Token is required.");

        if (utcNow == default)
            throw new BusinessRuleBrokenException("UTC time is required.");

        // Check if token already exists
        if (_refreshTokens.Any(r => r.Token == token))
            throw new BusinessRuleBrokenException("Token already exists.");

        var refreshToken = new RefreshToken(Id, TenantId, token, utcNow);
        _refreshTokens.Add(refreshToken);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokeSpecificToken(string tokenValue)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
            throw new BusinessRuleBrokenException("Token value is required.");

        var refreshToken = _refreshTokens.SingleOrDefault(r => r.Token == tokenValue);
        if (refreshToken == null)
            throw new BusinessRuleBrokenException("Token not found.");

        refreshToken.Revoke();
        UpdatedAt = DateTime.UtcNow;
    }

    public RefreshToken RotateRefreshToken(string oldTokenValue, string newTokenValue, DateTime utcNow)
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

        // REUSE DETECTION
        if (oldToken.IsUsed || oldToken.IsRevoked)
        {
            RevokeAllRefreshTokens();
            throw new BusinessRuleBrokenException("Token reuse detected. All sessions revoked.");
        }

        // EXPIRY CHECK
        if (oldToken.ExpiryDate < utcNow)
        {
            throw new BusinessRuleBrokenException("Refresh token has expired.");
        }

        // Update old token state
        oldToken.MarkAsUsed(utcNow);

        // Create new token
        var newRefreshToken = new RefreshToken(Id, TenantId, newTokenValue, utcNow);
        _refreshTokens.Add(newRefreshToken);
        UpdatedAt = DateTime.UtcNow;

        return newRefreshToken;
    }

    public void RevokeAllRefreshTokens()
    {
        var activeTokens = _refreshTokens.Where(t => !t.IsRevoked && !t.IsUsed).ToList();
        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddConfirmationEmailToken(string token)
    {
        EmailConfirmationToken = token;
    }

    public void AddResourceAssignment(ResourceId resourceId)
    {
        if (resourceId == null)
            throw new BusinessRuleBrokenException("Resource ID is required.");

        if (Role != UserRole.Staff)
            throw new BusinessRuleBrokenException("Only staff users can have resource assignments.");

        if (_userResourceAssignments.Any(ura => ura.ResourceId == resourceId))
            throw new BusinessRuleBrokenException("User is already assigned to this resource.");

        var assignment = new UserResourceAssignment(Id, resourceId, TenantId);
        _userResourceAssignments.Add(assignment);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveResourceAssignment(ResourceId resourceId)
    {
        if (resourceId == null)
            throw new BusinessRuleBrokenException("Resource ID is required.");

        var assignment = _userResourceAssignments.FirstOrDefault(ura => ura.ResourceId == resourceId);
        if (assignment == null)
            throw new BusinessRuleBrokenException("Resource assignment not found.");

        _userResourceAssignments.Remove(assignment);
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum UserRole
{
    TenantAdmin,
    Manager,
    Staff,
    Customer
}

public enum UserStatus
{
    Invited,
    Active,
    Inactive
}
