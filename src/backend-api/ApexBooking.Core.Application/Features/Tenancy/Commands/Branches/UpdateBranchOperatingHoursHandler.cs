using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.Branches
{
    public class UpdateBranchOperatingHoursHandler : ICommandHandler<UpdateBranchOperatingHoursCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public UpdateBranchOperatingHoursHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(UpdateBranchOperatingHoursCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to update branch hours. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Branches);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to update branch hours. Isolated business workspace could not be verified.");

            var branchId = new BranchId(command.BranchId);

            foreach (var item in command.OperatingHours)
            {
                tenant.SetBranchOperatingHours(branchId, item.DayOfWeek, item.StartTime, item.EndTime, item.IsOff);
            }

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
