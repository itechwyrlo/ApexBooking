using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Events;
public record TenantCreatedDomainEvent(
    TenantId TenantId,
    string Slug,
    string BusinessName,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    DateTime CreatedAt) : IReliableDomainEvent;

public record TenantApprovedDomainEvent(TenantId TenantId) : IDomainEvent;

public record TenantRejectedDomainEvent(TenantId TenantId, string Reason) : IDomainEvent;

public record TenantSuspendedDomainEvent(TenantId TenantId) : IDomainEvent;

public record TenantReactivatedDomainEvent(TenantId TenantId) : IDomainEvent;

public record TenantDeactivatedDomainEvent(TenantId TenantId) : IDomainEvent;

public record TrialStartedDomainEvent(TenantId TenantId, DateTime StartsAt, DateTime EndsAt) : IDomainEvent;

// Raised by Tenant.ExpireTrial() — drives SendTrialExpiredEmailHandler via the outbox.
public record TrialExpiredDomainEvent(TenantId TenantId) : IReliableDomainEvent;

// Raised by Tenant.MarkTrialReminderSent() — drives SendTrialReminderEmailHandler via the outbox.
public record TrialReminderSentDomainEvent(TenantId TenantId) : IReliableDomainEvent;

public record SubscriptionPlanChangedDomainEvent(TenantId TenantId, SubscriptionPlanType NewPlan) : IDomainEvent;
