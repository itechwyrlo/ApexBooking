using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetAppearance
{
    public class GetAppearanceHandler : IQueryHandler<GetAppearanceQuery, AppearanceDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetAppearanceHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<AppearanceDto> Handle(GetAppearanceQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load appearance settings. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.BusinessProfile!
            );

            if (tenant == null || tenant.BusinessProfile == null)
                throw new BusinessRuleBrokenException("Failed to load appearance settings. Workspace context could not be resolved.");

            return new AppearanceDto(
                tenant.BusinessProfile.ThemePaletteId,
                tenant.BusinessProfile.PublicPageDarkMode,
                tenant.Plan
            );
        }
    }
}
