using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

/// <summary>
/// Child of Service. Join table between Service and Resource.
/// Cannot exist without a Service. Service owns this join.
/// If the Service is deleted, all ServiceResource records are deleted.
/// If the Resource is deleted, only the affected rows are deleted — the Service survives.
/// TR-9.1 Step 3: used to validate that a resource is assignable to a service.
/// UC-3.1.2, TR-8.1
/// </summary>
public class ServiceResource : ITenantEntity
{
    public ServiceResourceId ServiceResourceId { get; private set; } = default!;
    public ServiceId ServiceId { get; private set; } = default!;
    public ResourceId ResourceId { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    protected ServiceResource() { }

    private ServiceResource(ServiceId serviceId, ResourceId resourceId, TenantId tenantId)
    {
        ServiceResourceId = new ServiceResourceId(Guid.NewGuid());
        ServiceId = serviceId;
        ResourceId = resourceId;
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
    }

    internal static ServiceResource Create(ServiceId serviceId, ResourceId resourceId, TenantId tenantId)
    {
        if (serviceId is null)
            throw new BusinessRuleBrokenException("Service is required.");

        if (resourceId is null)
            throw new BusinessRuleBrokenException("Resource is required.");

        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");

        return new ServiceResource(serviceId, resourceId, tenantId);
    }
}