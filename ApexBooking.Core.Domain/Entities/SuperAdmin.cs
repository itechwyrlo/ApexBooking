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
}


