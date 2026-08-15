using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.StaffBreaks
{
    public class RemoveStaffBreakHandler : ICommandHandler<RemoveStaffBreakCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public RemoveStaffBreakHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(RemoveStaffBreakCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to remove break. No authenticated tenant context was found.");

            var currentTenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members
            );

            if (currentTenant == null)
                throw new BusinessRuleBrokenException("Failed to remove break. Isolated business workspace could not be verified.");

            var member = currentTenant.Members.FirstOrDefault(m => m.TenantMemberId.Value == command.TenantMemberId);
            if (member == null)
                throw new BusinessRuleBrokenException("Target team member record not found within this business workspace.");

            member.RemoveBreak(new StaffBreakId(command.BreakId));

            _unitOfWork.TenantRepository.Update(currentTenant);

            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
