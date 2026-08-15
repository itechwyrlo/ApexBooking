using ApexBooking.Core.Domain.Services.Tenant;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Infrastructure.ExternalServices.Tenant;

public class TenantService : ITenantService
{
    private TenantId _tenant = null!;

    public TenantId CurrentTenant
    {
        get => _tenant;
        private set
        {
            if (_tenant != value)
            {
                var oldTenant = _tenant;
                _tenant = value;
            }
        }
    }

    public void SetCurrentTenant(TenantId tenant)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant), "Tenant identifier cannot be null.");
            
        CurrentTenant = tenant;
    }
}
