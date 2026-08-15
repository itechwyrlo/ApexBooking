using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Authentication.GetWorkspaceSummary
{
    public class GetWorkspaceSummaryHandler : IQueryHandler<GetWorkspaceSummaryQuery, GetWorkspaceSummaryResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetWorkspaceSummaryHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<GetWorkspaceSummaryResult> Handle(GetWorkspaceSummaryQuery query, CancellationToken cancellationToken)
        {
            // TenantMiddleware already resolved the {slug} route segment into the ambient tenant
            // before this handler ever runs — a slug that matches no tenant never reaches here at
            // all (the middleware responds 404 itself, same as the public booking wizard). This
            // fallback only guards the case where the route somehow had no {slug} segment at all.
            var resolvedTenantId = _tenantEntity.TenantId
                ?? throw new NotFoundException("This business workspace could not be found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == resolvedTenantId);

            // Deactivated tenants resolve fine at the middleware level (it only checks the slug
            // exists), so this endpoint enforces the same IsActive gate TenantLoginHandler does —
            // a deactivated workspace should read as "not found" here too, not a friendly welcome.
            if (tenant is null || !tenant.IsActive)
                throw new NotFoundException("This business workspace could not be found.");

            return new GetWorkspaceSummaryResult(tenant.BusinessProfile.BusinessName, tenant.BusinessProfile.Logo);
        }
    }
}
