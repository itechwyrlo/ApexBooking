using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services.Tenant
{
    public interface ITenantService
    {
        TenantId CurrentTenant { get; }
        void SetCurrentTenant(TenantId tenant);
    }

}