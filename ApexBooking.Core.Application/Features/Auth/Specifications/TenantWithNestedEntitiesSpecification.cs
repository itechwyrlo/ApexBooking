using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Auth.Specifications
{
    public class TenantWithNestedEntitiesSpecification : BaseSpecification<Tenant>
    {
        public TenantWithNestedEntitiesSpecification(TenantId tenantId)
        {
            Criteria = tenant => tenant.TenantId == tenantId;
            
            // Include TenantProfile
            AddInclude(tenant => tenant
                .Include(t => t.TenantProfile));
                
            // Include TenantSettings  
            AddInclude(tenant => tenant
                .Include(t => t.TenantSettings));
        }
    }
}
