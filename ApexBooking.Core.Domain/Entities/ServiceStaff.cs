using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

/// <summary>
/// Child of Service. Join table between Service and Resource.
/// Cannot exist without a Service. Service owns this join.
/// If the Service is deleted, all ServiceStaff records are deleted.
/// If the Resource is deleted, only the affected rows are deleted — the Service survives.
/// TR-9.1 Step 3: used to validate that a resource is assignable to a service.
/// UC-3.1.2, TR-8.1
/// </summary>
public class ServiceStaff : ITenantEntity
{
    public ServiceStaffId ServiceStaffId { get; private set; } = default!;
    public ServiceId ServiceId { get; private set; } = default!;
    public StaffId StaffId { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    protected ServiceStaff() { }

    private ServiceStaff(ServiceId serviceId, StaffId staffId, TenantId tenantId)
    {
        ServiceStaffId = new ServiceStaffId(Guid.NewGuid());
        ServiceId = serviceId;
        StaffId = staffId;
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
    }

    internal static ServiceStaff Create(ServiceId serviceId, StaffId staffId, TenantId tenantId)
    {
        if (serviceId is null)
            throw new BusinessRuleBrokenException("Service is required.");

        if (staffId is null)
            throw new BusinessRuleBrokenException("Staff is required.");

        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");

        return new ServiceStaff(serviceId, staffId, tenantId);
    }
}