using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicTheme
{
    public class GetPublicThemeHandler : IQueryHandler<GetPublicThemeQuery, PublicThemeDto>
    {
        private static readonly PublicThemeDto DefaultTheme = new("indigo", false);

        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetPublicThemeHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<PublicThemeDto> Handle(GetPublicThemeQuery query, CancellationToken cancellationToken)
        {
            // Tenant does not implement ITenantEntity, so the global EF query filter doesn't cover
            // this lookup — scope explicitly via the ambient ITenantEntity (resolved from the
            // request's {slug} route segment for this anonymous/public endpoint).
            if (_tenantEntity.TenantId is not { } tenantId)
                return DefaultTheme;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.BusinessProfile!);

            if (tenant is null || tenant.BusinessProfile is null)
                return DefaultTheme;

            return new PublicThemeDto(tenant.BusinessProfile.ThemePaletteId, tenant.BusinessProfile.PublicPageDarkMode);
        }
    }
}
