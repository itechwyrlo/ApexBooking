using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class UserResourceAssignment : ITenantEntity
{
    public UserResourceAssignmentId UserResourceAssignmentId { get; protected set; } = default!;
    public Guid UserId { get; protected set; } = default!;
    public ResourceId ResourceId { get; protected set; } = default!;
    public TenantId TenantId { get; protected set; } = default!;
    public DateTime CreatedAt { get; private set; }

    protected UserResourceAssignment() { }

    [SetsRequiredMembers]
    public UserResourceAssignment(Guid userId, ResourceId resourceId, TenantId tenantId)
    {
        UserResourceAssignmentId = new UserResourceAssignmentId(Guid.NewGuid());
        UserId = userId;
        ResourceId = resourceId;
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
    }
}
