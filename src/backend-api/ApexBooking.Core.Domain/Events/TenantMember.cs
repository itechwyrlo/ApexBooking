using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Events
{
    public record TeamMemberInvitedDomainEvent(
         TenantId TenantId,
         string Slug,
         Guid TenantMemberId,
         Guid ApplicationUserId,
         string Email,
         string FullName,
         SystemRole AssignedRole,
         DateTime InvitedAt
     ) : IReliableDomainEvent;

    // Raised by Tenant.DeactivateMember() — Notification-only, so plain IDomainEvent (sync path).
    public record StaffDeactivatedDomainEvent(
         TenantId TenantId,
         Guid TenantMemberId,
         string FullName,
         DateTime DeactivatedAt
     ) : IDomainEvent;
}