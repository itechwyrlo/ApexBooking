using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class Tenant : IAggregateRoot
{
    public TenantId TenantId { get; protected set; } = default!;
    public string Slug { get; private set; } = string.Empty;
    public string BusinessName { get; private set; } = string.Empty;
    public string OwnerFullName { get; private set; } = string.Empty;
    public string OwnerEmail { get; private set; } = string.Empty;
    public string OwnerPhone { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    // Navigation properties
    private readonly List<User> _users = new();
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();
    
    // 1:1 relationships
    public TenantProfile? TenantProfile { get; private set; }
    public TenantSettings? TenantSettings { get; private set; }

    protected Tenant() { }

    [SetsRequiredMembers]
    public Tenant(string slug, string businessName, string ownerFullName, string ownerEmail, string ownerPhone)
    {
        TenantId = new TenantId(Guid.NewGuid());
        Slug = slug.ToLowerInvariant(); // Store lowercase for case-insensitive uniqueness
        BusinessName = businessName;
        OwnerFullName = ownerFullName;
        OwnerEmail = ownerEmail;
        OwnerPhone = ownerPhone;
        Status = TenantStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Tenant Create(string slug, string businessName, string ownerFullName, string ownerEmail, string ownerPhone)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new BusinessRuleBrokenException("Tenant slug is required.");

        if (slug.Length < 3 || slug.Length > 50)
            throw new BusinessRuleBrokenException("Tenant slug must be between 3 and 50 characters.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$"))
            throw new BusinessRuleBrokenException("Tenant slug can only contain alphanumeric characters and hyphens.");

        if (string.IsNullOrWhiteSpace(businessName))
            throw new BusinessRuleBrokenException("Business name is required.");

        if (string.IsNullOrWhiteSpace(ownerFullName))
            throw new BusinessRuleBrokenException("Owner full name is required.");

        if (string.IsNullOrWhiteSpace(ownerEmail))
            throw new BusinessRuleBrokenException("Owner email is required.");

        return new Tenant(slug, businessName, ownerFullName, ownerEmail, ownerPhone);
    }

    public void MarkAsVerified()
    {
        if (Status != TenantStatus.Pending)
            throw new BusinessRuleBrokenException("Only pending tenants can be marked as verified.");

        Status = TenantStatus.Active;
        EmailVerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status != TenantStatus.Active)
            throw new BusinessRuleBrokenException("Only active tenants can be suspended.");

        Status = TenantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (Status != TenantStatus.Suspended)
            throw new BusinessRuleBrokenException("Only suspended tenants can be reactivated.");

        Status = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == TenantStatus.Deactivated)
            throw new BusinessRuleBrokenException("Tenant is already deactivated.");

        Status = TenantStatus.Deactivated;
        DeactivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddUser(User user)
    {
        if (user == null)
            throw new BusinessRuleBrokenException("User cannot be null.");

        if (_users.Any(u => u.Email == user.Email))
            throw new BusinessRuleBrokenException("User with this email already exists in tenant.");

        _users.Add(user);
        UpdatedAt = DateTime.UtcNow;
    }

    public void CreateTenantProfile(string timezone, string currencyCode)
    {
        if (TenantProfile != null)
            throw new BusinessRuleBrokenException("Tenant profile already exists.");

        TenantProfile = TenantProfile.Create(TenantId, timezone, currencyCode);
        UpdatedAt = DateTime.UtcNow;
    }

    public void CreateTenantSettings()
    {
        if (TenantSettings != null)
            throw new BusinessRuleBrokenException("Tenant settings already exist.");

        TenantSettings = TenantSettings.Create(TenantId);
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum TenantStatus
{
    Pending,
    Active,
    Suspended,
    Deactivated
}
