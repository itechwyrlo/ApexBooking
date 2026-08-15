using ApexBooking.Core.Domain.Services.Tenant;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Infrastructure.ExternalServices.Context
{
    public class HttpContextTenantIdProvider : ITenantEntity
    {
        private readonly ITenantService _tenantService;

        // AsyncLocal tracks context across asynchronous call boundaries safely
        private static readonly AsyncLocal<TenantId?> _currentTenantId = new();

        public HttpContextTenantIdProvider(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public TenantId? TenantId
        {
            get
            {
                // 1. Check for an explicit ambient override (background jobs, testing, seeds)
                if (_currentTenantId.Value != null)
                {
                    return _currentTenantId.Value;
                }

                // 2. Fall back to the ambient tenant TenantMiddleware resolved for this request
                //    (from the "tenant_id" claim, or via the {slug} route segment for anonymous traffic)
                return _tenantService.CurrentTenant;
            }
        }
    }
}