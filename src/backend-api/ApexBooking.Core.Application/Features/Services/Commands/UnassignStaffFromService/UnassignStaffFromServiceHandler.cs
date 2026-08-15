using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Services.Commands.UnassignStaffFromService
{
    public class UnassignStaffFromServiceHandler : ICommandHandler<UnassignStaffFromServiceCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public UnassignStaffFromServiceHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(UnassignStaffFromServiceCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to unassign staff. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetWithServiceStaffAsync(tenantId, cancellationToken);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to unassign staff. Isolated tenant context could not be verified.");

            tenant.UnassignStaffFromService(command.ServiceId, command.TenantMemberId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
