using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.BusinessProfile
{
    public class UpdateBusinessProfileHandler : ICommandHandler<UpdateBusinessProfileCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public UpdateBusinessProfileHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(UpdateBusinessProfileCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to update business profile. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.BusinessProfile!
            );

            if (tenant == null || tenant.BusinessProfile == null)
                throw new BusinessRuleBrokenException("Failed to update business profile. Workspace context could not be resolved.");

            tenant.BusinessProfile.UpdateDetails(
                command.BusinessName,
                command.Description,
                command.LogoUrl,
                command.ContactPhoneNumber
            );

            // Persist through aggregate root context tracking
            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
