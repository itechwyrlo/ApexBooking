using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Domain.Entities;

public class TenantRequest : IAggregateRoot
{
    public TenantRequestId Id { get; private set; } = default!;
    public string BusinessName { get; private set; } = string.Empty;
    public string OwnerFullName { get; private set; } = string.Empty;
    public string OwnerEmail { get; private set; } = string.Empty;
    public string OwnerPhone { get; private set; } = string.Empty;
    public TenantPlan Plan { get; private set; }
    public string? Message { get; private set; }
    public TenantRequestStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    protected TenantRequest() { }

    private TenantRequest(
        string businessName,
        string ownerFullName,
        string ownerEmail,
        string ownerPhone,
        TenantPlan plan,
        string? message)
    {
        Id = new TenantRequestId(Guid.NewGuid());
        BusinessName = businessName;
        OwnerFullName = ownerFullName;
        OwnerEmail = ownerEmail;
        OwnerPhone = ownerPhone;
        Plan = plan;
        Message = message;
        Status = TenantRequestStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static TenantRequest Create(
        string businessName,
        string ownerFullName,
        string ownerEmail,
        string ownerPhone,
        TenantPlan plan,
        string? message = null)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new BusinessRuleBrokenException("Business name is required.");

        if (string.IsNullOrWhiteSpace(ownerFullName))
            throw new BusinessRuleBrokenException("Owner full name is required.");

        if (string.IsNullOrWhiteSpace(ownerEmail))
            throw new BusinessRuleBrokenException("Owner email is required.");

        return new TenantRequest(businessName, ownerFullName, ownerEmail, ownerPhone, plan, message);
    }

    public static void EnsureNoPendingRequest(bool alreadyExists)
    {
        if (alreadyExists)
            throw new BusinessRuleBrokenException("A pending request for this email already exists.");
    }

    public void EnsureSlugIsAvailable(bool slugTaken)
    {
        if (slugTaken)
            throw new BusinessRuleBrokenException("This booking URL slug is already taken.");
    }

    public void EnsureOwnerEmailIsAvailable(bool existsAsTenant, bool existsAsUser)
    {
        if (existsAsTenant)
            throw new BusinessRuleBrokenException("An organization with this owner email already exists.");

        if (existsAsUser)
            throw new BusinessRuleBrokenException("This email is already registered to an existing user.");
    }

    public void Approve()
    {
        if (Status != TenantRequestStatus.Pending)
            throw new BusinessRuleBrokenException("Only pending requests can be approved.");

        Status = TenantRequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != TenantRequestStatus.Pending)
            throw new BusinessRuleBrokenException("Only pending requests can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleBrokenException("Rejection reason is required.");

        Status = TenantRequestStatus.Rejected;
        RejectionReason = reason;
        ReviewedAt = DateTime.UtcNow;
    }
}

public enum TenantRequestStatus
{
    Pending,
    Approved,
    Rejected
}
