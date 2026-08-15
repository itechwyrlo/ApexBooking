using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Domain.Events;

public record TenantRequestReceivedDomainEvent(
    TenantRegistrationRequestId RequestId,
    string BusinessName,
    SubscriptionPlanType RequestedPlan,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    DateTime RequestedAt) : IReliableDomainEvent;

// Not reliable: drives ProvisionTenantOnRequestApprovedHandler, which is business logic (tenant
// provisioning), not an external call — stays on the existing synchronous post-commit dispatch.
public record TenantRequestApproveDomainEvent(
    TenantRegistrationRequestId RequestId,
    string BusinessName,
    BusinessType BusinessType,
    string RequestedSlug,
    SubscriptionPlanType RequestedPlan,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    Guid ApprovedByUserId,
    DateTime ApprovedAt) : IDomainEvent;

public record TenantRequestRejectedDomainEvent(
    TenantRegistrationRequestId RequestId,
    string BusinessName,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    string Reason,
    Guid RejectedByUserId,
    DateTime RejectedAt) : IReliableDomainEvent;
