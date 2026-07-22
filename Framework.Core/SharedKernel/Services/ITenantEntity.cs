using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.SharedKernel.Services
{
    public interface ITenantEntity
    {
        TenantId TenantId { get; }
    }
}