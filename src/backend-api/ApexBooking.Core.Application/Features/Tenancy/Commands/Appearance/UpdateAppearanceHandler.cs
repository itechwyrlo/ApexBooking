using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.Appearance
{
    public class UpdateAppearanceHandler : ICommandHandler<UpdateAppearanceCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public UpdateAppearanceHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(UpdateAppearanceCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to update appearance settings. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.BusinessProfile!
            );

            if (tenant == null || tenant.BusinessProfile == null)
                throw new BusinessRuleBrokenException("Failed to update appearance settings. Workspace context could not be resolved.");

            tenant.BusinessProfile.UpdateAppearance(
                command.ThemePaletteId,
                command.PublicPageDarkMode,
                tenantCanUseDarkMode: tenant.Plan != SubscriptionPlanType.Basic
            );

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
